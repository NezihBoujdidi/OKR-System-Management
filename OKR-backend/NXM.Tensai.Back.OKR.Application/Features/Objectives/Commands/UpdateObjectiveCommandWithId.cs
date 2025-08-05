namespace NXM.Tensai.Back.OKR.Application;

public record UpdateObjectiveCommandWithId(Guid Id, UpdateObjectiveCommand Command) : IRequest;

public class UpdateObjectiveCommand
{
      public Guid OKRSessionId { get; init; }
      public Guid UserId { get; init; }
      public string Title { get; init; } = string.Empty;
      public string? Description { get; init; }
      public DateTime StartedDate { get; init; }
      public DateTime EndDate { get; init; }
      public Guid ResponsibleTeamId { get; set; }
      public Status Status { get; set; }
      public Priority Priority { get; set; }
      public int? Progress { get; set; }
}

public class UpdateObjectiveCommandValidator : AbstractValidator<UpdateObjectiveCommand>
{
    public UpdateObjectiveCommandValidator()
    {
        RuleFor(x => x.OKRSessionId).NotEmpty().WithMessage("OKR Session ID is required.");
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required.");
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(100).WithMessage("Title must be at most 100 characters long.");
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.StartedDate).LessThan(x => x.EndDate).WithMessage("Start date must be before end date.");
    }
}

public class UpdateObjectiveCommandHandler : IRequestHandler<UpdateObjectiveCommandWithId>
{
    private readonly IObjectiveRepository _objectiveRepository;
    private readonly IValidator<UpdateObjectiveCommand> _validator;

    public UpdateObjectiveCommandHandler(IObjectiveRepository objectiveRepository, IValidator<UpdateObjectiveCommand> validator)
    {
        _objectiveRepository = objectiveRepository;
        _validator = validator;
    }

    public async Task Handle(UpdateObjectiveCommandWithId request, CancellationToken cancellationToken)
    {
        var (id, command) = request;
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var objective = await _objectiveRepository.GetByIdAsync(id);
        if (objective == null)
        {
            throw new NotFoundException(nameof(Objective), id);
        }

        command.UpdateEntity(objective);

        await _objectiveRepository.UpdateAsync(objective);
    }
}
