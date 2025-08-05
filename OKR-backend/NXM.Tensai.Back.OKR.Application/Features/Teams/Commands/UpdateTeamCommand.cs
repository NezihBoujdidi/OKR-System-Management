namespace NXM.Tensai.Back.OKR.Application;

public record UpdateTeamCommandWithId(Guid Id, UpdateTeamCommand Command) : IRequest;

public class UpdateTeamCommand
{
    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;
    public Guid OrganizationId { get; init; }
    public Guid? TeamManagerId { get; init; }


}

public class UpdateTeamCommandValidator : AbstractValidator<UpdateTeamCommand>
{
    public UpdateTeamCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Team name is required.")
            .MaximumLength(100).WithMessage("Team name must be at most 100 characters long.");
        RuleFor(x => x.OrganizationId).NotEmpty().WithMessage("Organization ID is required.");
        RuleFor(x => x.TeamManagerId)
            .Must(id => id == null || id != Guid.Empty)
            .WithMessage("Team Manager ID must be either null or a valid non-empty GUID.");
        RuleFor(x => x.Description).MaximumLength(200).WithMessage("Team Description shouldn't be more than 200 characters.");
    }
}

public class UpdateTeamCommandHandler : IRequestHandler<UpdateTeamCommandWithId>
{
    private readonly ITeamRepository _teamRepository;
    private readonly IValidator<UpdateTeamCommand> _validator;

    public UpdateTeamCommandHandler(ITeamRepository teamRepository, IValidator<UpdateTeamCommand> validator)
    {
        _teamRepository = teamRepository;
        _validator = validator;
    }

    public async Task Handle(UpdateTeamCommandWithId request, CancellationToken cancellationToken)
    {
        var (id, command) = request;
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var team = await _teamRepository.GetByIdAsync(id);
        if (team == null)
        {
            throw new NotFoundException(nameof(Team), id);
        }

        command.UpdateEntity(team);

        await _teamRepository.UpdateAsync(team);
    }
}

