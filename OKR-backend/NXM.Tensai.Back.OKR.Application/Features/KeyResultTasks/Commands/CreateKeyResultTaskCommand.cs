namespace NXM.Tensai.Back.OKR.Application;

public class CreateKeyResultTaskCommand : IRequest
{
    public Guid KeyResultId { get; init; }
    public Guid UserId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime StartedDate { get; set; }
    public DateTime EndDate { get; init; }
    public Guid CollaboratorId { get; set; }
    public int Progress { get; set; }
    public Priority? Priority { get; set; }
}

public class CreateKeyResultTaskCommandValidator : AbstractValidator<CreateKeyResultTaskCommand>
{
    public CreateKeyResultTaskCommandValidator()
    {
        RuleFor(x => x.KeyResultId).NotEmpty().WithMessage("Key Result ID is required.");
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required.");
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(100).WithMessage("Title must be at most 100 characters long.");
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.EndDate).GreaterThan(DateTime.UtcNow).WithMessage("Due date must be in the future.");
    }
}

public class CreateKeyResultTaskCommandHandler : IRequestHandler<CreateKeyResultTaskCommand>
{
    private readonly IKeyResultTaskRepository _keyResultTaskRepository;
    private readonly IValidator<CreateKeyResultTaskCommand> _validator;
    private readonly IKeyResultRepository _keyResultRepository;
    private readonly IObjectiveRepository _objectiveRepository;
    private readonly IOKRSessionRepository _okrSessionRepository;

    public CreateKeyResultTaskCommandHandler(
        IKeyResultTaskRepository keyResultTaskRepository,
        IValidator<CreateKeyResultTaskCommand> validator,
        IKeyResultRepository keyResultRepository,
        IObjectiveRepository objectiveRepository,
        IOKRSessionRepository okrSessionRepository)
    {
        _keyResultTaskRepository = keyResultTaskRepository;
        _validator = validator;
        _keyResultRepository = keyResultRepository;
        _objectiveRepository = objectiveRepository;
        _okrSessionRepository = okrSessionRepository;
    }

    public async Task Handle(CreateKeyResultTaskCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var keyResultTask = request.ToEntity();
        await _keyResultTaskRepository.AddAsync(keyResultTask);

        // Recalculate KeyResult progress
        var keyResult = await _keyResultRepository.GetByIdAsync(request.KeyResultId);
        if (keyResult != null)
        {
            var allTasks = await _keyResultTaskRepository.GetByKeyResultAsync(request.KeyResultId);
            keyResult.RecalculateProgress(allTasks);
            await _keyResultRepository.UpdateAsync(keyResult);

            // Recalculate Objective progress
            var objective = await _objectiveRepository.GetByIdAsync(keyResult.ObjectiveId);
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
}
