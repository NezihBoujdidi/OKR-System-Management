using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace NXM.Tensai.Back.OKR.Application;

public class InviteUserCommand : IRequest<InviteUserResponse>
{
    public string Email { get; init; } = null!;
    public RoleType Role { get; init; } = RoleType.Collaborator; // Default role is Collaborator
    public Guid OrganizationId { get; init; }
    public Guid? TeamId { get; init; }
}

public class InviteUserResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public string Email { get; set; }
    public string Role { get; set; }
    public string InviteId { get; set; }
}

public class InviteUserCommandValidator : AbstractValidator<InviteUserCommand>
{
    public InviteUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");
        
        RuleFor(x => x.OrganizationId)
            .NotEqual(Guid.Empty).WithMessage("Organization ID is required.");
        
        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Invalid role.");
    }
}

public class InviteUserCommandHandler : IRequestHandler<InviteUserCommand, InviteUserResponse>
{
    private readonly ISupabaseClient _supabaseClient;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IValidator<InviteUserCommand> _validator;
    private readonly ILogger<InviteUserCommandHandler> _logger;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly ITeamUserRepository _teamUserRepository;

    public InviteUserCommandHandler(
        ISupabaseClient supabaseClient,
        IOrganizationRepository organizationRepository,
        ITeamRepository teamRepository,
        IValidator<InviteUserCommand> validator,
        ILogger<InviteUserCommandHandler> logger,
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        ITeamUserRepository teamUserRepository)
    {
        _supabaseClient = supabaseClient ?? throw new ArgumentNullException(nameof(supabaseClient));
        _organizationRepository = organizationRepository ?? throw new ArgumentNullException(nameof(organizationRepository));
        _teamRepository = teamRepository ?? throw new ArgumentNullException(nameof(teamRepository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        _teamUserRepository = teamUserRepository ?? throw new ArgumentNullException(nameof(teamUserRepository));
    }

    public async Task<InviteUserResponse> Handle(InviteUserCommand request, CancellationToken cancellationToken)
    {
        // Validate command
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // Verify organization exists
        var organization = await _organizationRepository.GetByIdAsync(request.OrganizationId);
        if (organization == null)
        {
            throw new EntityNotFoundException($"Organization with ID {request.OrganizationId} not found.");
        }

        // Verify team exists if provided
        if (request.TeamId.HasValue)
        {
            var team = await _teamRepository.GetByIdAsync(request.TeamId.Value);
            if (team == null)
            {
                throw new EntityNotFoundException($"Team with ID {request.TeamId} not found.");
            }

            // Verify team belongs to the specified organization
            if (team.OrganizationId != request.OrganizationId)
            {
                throw new ValidationException($"Team with ID {request.TeamId} does not belong to organization with ID {request.OrganizationId}.");
            }
        }

        // Register user with random data if not exists
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user != null)
        {
            // Use ValidationFailure so ex.Errors is not empty
            throw new ValidationException(new List<FluentValidation.Results.ValidationFailure> {
                new FluentValidation.Results.ValidationFailure(nameof(request.Email), "User already invited")
            });
        }
        user = new User
        {
            Email = request.Email,
            UserName = request.Email,
            FirstName = "User" + Guid.NewGuid().ToString("N").Substring(0, 8),
            LastName = "Invited",
            Address = "Random Address " + Guid.NewGuid().ToString("N").Substring(0, 4),
            Position = "Unknown",
            DateOfBirth = DateTime.UtcNow.AddYears(-25),
            Gender = Gender.Male, // Always Male
            IsEnabled = false, 
            OrganizationId = request.OrganizationId,
            SupabaseId = Guid.NewGuid().ToString()
        };
        var password = Guid.NewGuid().ToString("N") + "!aA1"; // random strong password
        var createResult = await _userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            throw new ValidationException("Failed to create invited user: " + string.Join(", ", createResult.Errors.Select(e => e.Description)));
        }
        var roleName = request.Role.ToString();
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null)
            throw new ValidationException($"Role {roleName} does not exist.");
        var roleResult = await _userManager.AddToRoleAsync(user, roleName);
        if (!roleResult.Succeeded)
        {
            throw new ValidationException("Failed to assign role to invited user: " + string.Join(", ", roleResult.Errors.Select(e => e.Description)));
        }

        // If TeamId is provided, create TeamUser entry
        if (request.TeamId.HasValue)
        {
            // Only create if not already present
            var existingTeamUser = await _teamUserRepository.GetByTeamAndUserIdAsync(request.TeamId.Value, user.Id);
            if (existingTeamUser == null)
            {
                var teamUser = new TeamUser
                {
                    TeamId = request.TeamId.Value,
                    UserId = user.Id
                };
                await _teamUserRepository.AddAsync(teamUser);
            }
        }

        _logger.LogInformation("Inviting user {Email} to organization {OrganizationName} ({OrganizationId}) with role {Role}",
            request.Email, organization.Name, organization.Id, request.Role);
        
        // Send invitation via Supabase
        var result = await _supabaseClient.InviteUserByEmailAsync(
            request.Email,
            request.Role.ToString(),
            request.OrganizationId,
            request.TeamId);

        if (result.Success)
        {
            _logger.LogInformation("Successfully invited {Email} to organization {OrganizationId}", 
                request.Email, request.OrganizationId);
        }
        else
        {
            _logger.LogWarning("Failed to invite {Email} to organization {OrganizationId}: {ErrorMessage}", 
                request.Email, request.OrganizationId, result.Message);
        }

        // Return response
        return new InviteUserResponse
        {
            Success = result.Success,
            Message = result.Message,
            Email = request.Email,
            Role = request.Role.ToString(),
            InviteId = result.InviteId
        };
    }
}

