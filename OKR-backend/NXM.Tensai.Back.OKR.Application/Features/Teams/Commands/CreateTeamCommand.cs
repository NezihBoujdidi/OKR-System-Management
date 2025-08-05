namespace NXM.Tensai.Back.OKR.Application;

public class CreateTeamCommand : IRequest<Guid>
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; } = string.Empty;
    public Guid OrganizationId { get; init; }
    public Guid? TeamManagerId { get; init; }

    
}

public class CreateTeamCommandValidator : AbstractValidator<CreateTeamCommand>
{
    public CreateTeamCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Team name is required.")
            .MaximumLength(100).WithMessage("Team name must be at most 100 characters long.");
        RuleFor(x => x.OrganizationId).NotEmpty().WithMessage("Organization ID is required.");
        RuleFor(x => x.Description).MaximumLength(200).WithMessage("Team Description shouldn't be more than 200 characters.");
    }
}

public class CreateTeamCommandHandler : IRequestHandler<CreateTeamCommand, Guid>
{
    private readonly ITeamRepository _teamRepository;
    private readonly ITeamUserRepository _teamUserRepository; // Add this
    private readonly IValidator<CreateTeamCommand> _validator;

    public CreateTeamCommandHandler(
        ITeamRepository teamRepository,
        ITeamUserRepository teamUserRepository, // Add this
        IValidator<CreateTeamCommand> validator)
    {
        _teamRepository = teamRepository;
        _teamUserRepository = teamUserRepository; // Add this
        _validator = validator;
    }

    public async Task<Guid> Handle(CreateTeamCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // If no TeamManagerId, append manager invite note to description
        string description = request.Description ?? string.Empty;
        if (!request.TeamManagerId.HasValue)
        {
            if (!string.IsNullOrWhiteSpace(description))
                description += " ";
            description += "Manager is invited, still didn't accept invite.";
        }

        // Use a custom entity creation to inject the description
        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            OrganizationId = request.OrganizationId,
            TeamManagerId = request.TeamManagerId,
            Description = description,
            CreatedDate = DateTime.UtcNow
        };

        await _teamRepository.AddAsync(team);

        // Attach TeamManagerId to TeamUser table if present
        if (request.TeamManagerId.HasValue)
        {
            await _teamUserRepository.AddAsync(new TeamUser
            {
                TeamId = team.Id,
                UserId = request.TeamManagerId.Value
            });
        }

        return team.Id;
    }
}

