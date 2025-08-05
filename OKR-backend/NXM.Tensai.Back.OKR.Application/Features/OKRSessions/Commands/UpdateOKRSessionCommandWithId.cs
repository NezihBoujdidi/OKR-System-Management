namespace NXM.Tensai.Back.OKR.Application;

public record UpdateOKRSessionCommandWithId(Guid Id, UpdateOKRSessionCommand Command) : IRequest;

public class UpdateOKRSessionCommand
{
    public Guid UserId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime StartedDate { get; init; }
    public DateTime EndDate { get; init; }
    public string? Color { get; set; }
    public Status? Status { get; set; }
    public List<Guid> TeamIds { get; set; } = new List<Guid>();
}

public class UpdateOKRSessionCommandValidator : AbstractValidator<UpdateOKRSessionCommand>
{
    public UpdateOKRSessionCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(100).WithMessage("Title must be at most 100 characters long.");
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.");
        RuleFor(x => x.Color)
            .NotEmpty().WithMessage("Color is required.");
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.StartedDate).LessThan(x => x.EndDate).WithMessage("Start date must be before end date.");
    }
}

public class UpdateOKRSessionCommandHandler : IRequestHandler<UpdateOKRSessionCommandWithId>
{
    private readonly IOKRSessionRepository _okrSessionRepository;
    private readonly IOKRSessionTeamRepository _okrSessionTeamRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IValidator<UpdateOKRSessionCommand> _validator;

    public UpdateOKRSessionCommandHandler(
        IOKRSessionRepository okrSessionRepository,
        IOKRSessionTeamRepository okrSessionTeamRepository,
        ITeamRepository teamRepository,
        IValidator<UpdateOKRSessionCommand> validator)
    {
        _okrSessionRepository = okrSessionRepository;
        _okrSessionTeamRepository = okrSessionTeamRepository;
        _teamRepository = teamRepository;
        _validator = validator;
    }

    public async Task Handle(UpdateOKRSessionCommandWithId request, CancellationToken cancellationToken)
    {
        var (id, command) = request;
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var okrSession = await _okrSessionRepository.GetByIdAsync(id);
        if (okrSession == null)
        {
            throw new NotFoundException(nameof(OKRSession), id);
        }

        // Validate that all teams exist
        foreach (var teamId in command.TeamIds)
        {
            var team = await _teamRepository.GetByIdAsync(teamId);
            if (team == null)
            {
                throw new ValidationException($"Team with ID {teamId} does not exist.");
            }
        }

        // Update OKRSession entity
        command.UpdateEntity(okrSession);
        await _okrSessionRepository.UpdateAsync(okrSession);

        // Update OKRSessionTeam join table
        var existingLinks = await _okrSessionTeamRepository.GetBySessionIdAsync(id);
        var existingTeamIds = existingLinks.Select(x => x.TeamId).ToList();
        var newTeamIds = command.TeamIds.Distinct().ToList();

        // Remove links for teams no longer assigned
        foreach (var link in existingLinks.Where(x => !newTeamIds.Contains(x.TeamId)))
        {
            await _okrSessionTeamRepository.DeleteAsync(link);
        }

        // Add links for new teams
        foreach (var teamId in newTeamIds.Where(x => !existingTeamIds.Contains(x)))
        {
            var newLink = new OKRSessionTeam { OKRSessionId = id, TeamId = teamId };
            await _okrSessionTeamRepository.AddAsync(newLink);
        }
    }
}
