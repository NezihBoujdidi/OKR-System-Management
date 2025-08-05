namespace NXM.Tensai.Back.OKR.Application;

public class CreateKeyResultCommand : IRequest
{
    public Guid ObjectiveId { get; init; }
    public Guid UserId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime StartedDate { get; init; }
    public DateTime EndDate { get; init; }
    public Status? Status { get; set; }
    public int Progress  { get; set; }
    public bool IsDeleted { get; set; } = false;

}

public class CreateKeyResultCommandValidator : AbstractValidator<CreateKeyResultCommand>
{
    public CreateKeyResultCommandValidator()
    {
        RuleFor(x => x.ObjectiveId).NotEmpty().WithMessage("Objective ID is required.");
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required.");
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(100).WithMessage("Title must be at most 100 characters long.");
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.StartedDate).LessThan(x => x.EndDate).WithMessage("Start date must be before end date.");
    }
}

public class CreateKeyResultCommandHandler : IRequestHandler<CreateKeyResultCommand>
{
    private readonly IKeyResultRepository _keyResultRepository;
    private readonly IValidator<CreateKeyResultCommand> _validator;
    private readonly IObjectiveRepository _objectiveRepository;
    private readonly IOKRSessionRepository _okrSessionRepository;

    public CreateKeyResultCommandHandler(
        IKeyResultRepository keyResultRepository,
        IValidator<CreateKeyResultCommand> validator,
        IObjectiveRepository objectiveRepository,
        IOKRSessionRepository okrSessionRepository)
    {
        _keyResultRepository = keyResultRepository;
        _validator = validator;
        _objectiveRepository = objectiveRepository;
        _okrSessionRepository = okrSessionRepository;
    }

    public async Task Handle(CreateKeyResultCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var keyResult = request.ToEntity();
        await _keyResultRepository.AddAsync(keyResult);

        // Recalculate Objective progress
        var objective = await _objectiveRepository.GetByIdAsync(request.ObjectiveId);
        if (objective != null)
        {
            var allKeyResults = await _keyResultRepository.GetByObjectiveAsync(objective.Id);
            objective.RecalculateProgress(allKeyResults);
            await _objectiveRepository.UpdateAsync(objective);

            // Recalculate OKRSession progress
            var okrSession = await _okrSessionRepository.GetByIdAsync(objective.OKRSessionId);
            if (okrSession != null)
            {
                var allObjectives = await _objectiveRepository.GetBySessionIdAsync(objective.OKRSessionId);
                okrSession.RecalculateProgress(allObjectives);
                await _okrSessionRepository.UpdateAsync(okrSession);
            }
        }
    }
}
