namespace NXM.Tensai.Back.OKR.Application;

public class RegisterUserCommand : IRequest<Guid>
{
    public string SupabaseId { get; set; } = null!;
    public string Email { get; init; } = null!;
    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;
    public string Address { get; init; } = null!;
    public DateTime DateOfBirth { get; init; }
    public Gender Gender { get; init; }
    public string PhoneNumber { get; init; } = null!;
    public string Position { get; set; } = null!;
    public string Password { get; init; } = null!;
    public string ConfirmPassword { get; init; } = null!;
    public string RoleName { get; set; } = null!;
    public bool IsEnabled { get; set; }
    public Guid? TeamId { get; init; }
    public Guid? OrganizationID { get; set; }
}

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.FirstName).NotEmpty().WithMessage("First name is required.");
        RuleFor(x => x.LastName).NotEmpty().WithMessage("Last name is required.");
        RuleFor(x => x.Address).NotEmpty().WithMessage("Address is required.");
        RuleFor(x => x.DateOfBirth).NotEmpty().WithMessage("Date of birth is required.");
        RuleFor(x => x.Position).NotEmpty().WithMessage("Position is required.");
        RuleFor(x => x.Gender).IsInEnum().WithMessage("Gender is required.");
        RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage("Phone number is required.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.");
        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Passwords do not match.");
        // Add validation for SupabaseId when using Supabase authentication
        RuleFor(x => x.SupabaseId).NotEmpty().WithMessage("SupabaseId is required.");
    }
}

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Guid>
{
    private readonly UserManager<User> _userManager;
    private readonly IValidator<RegisterUserCommand> _validator;
    private readonly RoleManager<Role> _roleManager;
    private readonly ITeamRepository _teamRepository;
    private readonly ITeamUserRepository _teamUserRepository;

    public RegisterUserCommandHandler(
        UserManager<User> userManager, 
        RoleManager<Role> roleManager, 
        IValidator<RegisterUserCommand> validator,
        ITeamRepository teamRepository,
        ITeamUserRepository teamUserRepository)
    {
        _userManager = userManager;
        _validator = validator;
        _roleManager = roleManager;
        _teamRepository = teamRepository;
        _teamUserRepository = teamUserRepository;
    }

    public async Task<Guid> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            /* // Update all fields except OrganizationId, Email, and Role
            existingUser.FirstName = request.FirstName;
            existingUser.LastName = request.LastName;
            existingUser.Address = request.Address;
            // Ensure DateOfBirth is UTC
            existingUser.DateOfBirth = request.DateOfBirth.Kind == DateTimeKind.Utc
                ? request.DateOfBirth
                : DateTime.SpecifyKind(request.DateOfBirth, DateTimeKind.Utc);
            existingUser.Gender = request.Gender;
            existingUser.PhoneNumber = request.PhoneNumber;
            existingUser.Position = request.Position;
            existingUser.PasswordHash = _userManager.PasswordHasher.HashPassword(existingUser, request.Password);
            existingUser.IsEnabled = request.IsEnabled;
            existingUser.SupabaseId = request.SupabaseId;
            // Do not update OrganizationId, Email, or Role

            var updateResult = await _userManager.UpdateAsync(existingUser);
            if (!updateResult.Succeeded)
            {
                throw new UserCreationException(string.Join(", ", updateResult.Errors.Select(e => e.Description)));
            }
            return existingUser.Id; */
            throw new UserCreationException("User with this email already exists and is registered.");
        }

        
        var user = request.ToEntity();

        user.IsEnabled = request.RoleName
            .Equals(RoleType.OrganizationAdmin.ToString(), StringComparison.OrdinalIgnoreCase) ? false : true;

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            throw new UserCreationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        var role = await _roleManager.FindByNameAsync(request.RoleName);
        if (role == null)
        {
            throw new KeyNotFoundException($"Role with name {request.RoleName} not found.");
        }

        string roleToAssign = request.RoleName ?? RoleType.OrganizationAdmin.ToString();
        var roleResult = await _userManager.AddToRoleAsync(user, roleToAssign);
        if (!roleResult.Succeeded)
        {
            throw new RoleAssignmentException($"Failed to assign role {roleToAssign} to user.");
        }

        if (request.TeamId.HasValue)
        {
            var team = await _teamRepository.GetByIdAsync(request.TeamId.Value);
            if (team == null)
            {
                throw new EntityNotFoundException($"Team with ID {request.TeamId} not found.");
            }

            // Verify team belongs to the specified organization
            if (team.OrganizationId != request.OrganizationID)
            {
                throw new ValidationException($"Team with ID {request.TeamId} does not belong to organization with ID {request.OrganizationID}.");
            }
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

            // If the role is TeamManager, update the team's TeamManagerId
            if (request.RoleName != null && 
                request.RoleName.Equals(RoleType.TeamManager.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                team.TeamManagerId = user.Id;
                // Remove appended sentence from description if present
                var marker = "Manager is invited, still didn't accept invite.";
                if (!string.IsNullOrEmpty(team.Description) && team.Description.Contains(marker))
                {
                    team.Description = team.Description.Replace(marker, "").Trim();
                }
                await _teamRepository.UpdateAsync(team);
            }
        }

        // Email confirmation is handled by Supabase
        return user.Id;
    }
}