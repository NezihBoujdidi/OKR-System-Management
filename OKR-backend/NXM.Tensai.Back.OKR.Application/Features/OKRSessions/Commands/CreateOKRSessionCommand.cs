using NXM.Tensai.Back.OKR.Domain;

namespace NXM.Tensai.Back.OKR.Application;

public class CreateOKRSessionCommand : IRequest<Guid>
{
    public string Title { get; init; } = string.Empty;
    public Guid OrganizationId { get; init; }
    public string? Description { get; init; }
    public DateTime StartedDate { get; init; }
    public DateTime EndDate { get; init; }
    public List<Guid> TeamIds { get; init; } = new List<Guid>();
    public Guid UserId { get; init; }
    public string? Color { get; init; }
    public Status? Status { get; init; }
}

public class CreateOKRSessionCommandValidator : AbstractValidator<CreateOKRSessionCommand>
{
    public CreateOKRSessionCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500);

        RuleFor(x => x.StartedDate)
            .NotEmpty()
            .Must((command, startDate) => startDate <= command.EndDate)
            .WithMessage("Start date must be before or equal to end date");

        RuleFor(x => x.EndDate)
            .NotEmpty();

        RuleFor(x => x.TeamIds)
            .NotNull();

        RuleFor(x => x.UserId)
            .NotEmpty();
    }
}

public class CreateOKRSessionCommandHandler : IRequestHandler<CreateOKRSessionCommand, Guid>
{
    private readonly IOKRSessionRepository _okrSessionRepository;
    private readonly IOKRSessionTeamRepository _okrSessionTeamRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IValidator<CreateOKRSessionCommand> _validator;

    public CreateOKRSessionCommandHandler(
        IOKRSessionRepository okrSessionRepository,
        IOKRSessionTeamRepository okrSessionTeamRepository,
        ITeamRepository teamRepository,
        IValidator<CreateOKRSessionCommand> validator)
    {
        _okrSessionRepository = okrSessionRepository;
        _okrSessionTeamRepository = okrSessionTeamRepository;
        _teamRepository = teamRepository;
        _validator = validator;
    }

    public async Task<Guid> Handle(CreateOKRSessionCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // Validate that all teams exist
        foreach (var teamId in request.TeamIds)
        {
            var team = await _teamRepository.GetByIdAsync(teamId);
            if (team == null)
            {
                throw new ValidationException($"Team with ID {teamId} does not exist.");
            }
        }

        var okrSession = request.ToEntity();
        await _okrSessionRepository.AddAsync(okrSession);

        // Create OKRSessionTeam entries for each team
        foreach (var teamId in request.TeamIds)
        {
            var okrSessionTeam = new OKRSessionTeam
            {
                OKRSessionId = okrSession.Id,
                TeamId = teamId
            };
            await _okrSessionTeamRepository.AddAsync(okrSessionTeam);
        }

        return okrSession.Id;
    }
}
