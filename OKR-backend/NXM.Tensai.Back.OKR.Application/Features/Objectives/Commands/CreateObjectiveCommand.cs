namespace NXM.Tensai.Back.OKR.Application;

public class CreateObjectiveCommand : IRequest <Guid>
{
    public Guid OKRSessionId { get; init; }
    public Guid UserId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime StartedDate { get; init; }
    public DateTime EndDate { get; init; }
    public Status? Status { get; set; }
    public Priority? Priority { get; set; }
    public Guid ResponsibleTeamId { get; set; }
    public bool IsDeleted { get; set; } = false;
    public int Progress  { get; set; }
}

public class CreateObjectiveCommandValidator : AbstractValidator<CreateObjectiveCommand>
{
    public CreateObjectiveCommandValidator()
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

public class CreateObjectiveCommandHandler : IRequestHandler<CreateObjectiveCommand, Guid>
{
    private readonly IObjectiveRepository _objectiveRepository;
    private readonly IValidator<CreateObjectiveCommand> _validator;
    private readonly IOKRSessionRepository _okrSessionRepository;

    public CreateObjectiveCommandHandler(IObjectiveRepository objectiveRepository, IValidator<CreateObjectiveCommand> validator, IOKRSessionRepository okrSessionRepository)
    {
        _objectiveRepository = objectiveRepository;
        _validator = validator;
        _okrSessionRepository = okrSessionRepository;
    }

    public async Task<Guid> Handle(CreateObjectiveCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var objective = request.ToEntity();
        await _objectiveRepository.AddAsync(objective);

        // Recalculate OKRSession progress
        var okrSession = await _okrSessionRepository.GetByIdAsync(request.OKRSessionId);
        if (okrSession != null)
        {
            var allObjectives = await _objectiveRepository.GetBySessionIdAsync(request.OKRSessionId);
            okrSession.RecalculateProgress(allObjectives);
            await _okrSessionRepository.UpdateAsync(okrSession);
        }

        return objective.Id;
    }
}
