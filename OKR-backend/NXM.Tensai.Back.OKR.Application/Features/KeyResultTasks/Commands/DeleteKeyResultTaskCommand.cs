namespace NXM.Tensai.Back.OKR.Application;

public record DeleteKeyResultTaskCommand(Guid Id) : IRequest;

public class DeleteKeyResultTaskCommandValidator : AbstractValidator<DeleteKeyResultTaskCommand>
{
    public DeleteKeyResultTaskCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Key Result Task ID must not be empty.");
    }
}

public class DeleteKeyResultTaskCommandHandler : IRequestHandler<DeleteKeyResultTaskCommand>
{
    private readonly IKeyResultTaskRepository _keyResultTaskRepository;
    private readonly IKeyResultRepository _keyResultRepository;
    private readonly IObjectiveRepository _objectiveRepository;
    private readonly IOKRSessionRepository _okrSessionRepository;

    public DeleteKeyResultTaskCommandHandler(
        IKeyResultTaskRepository keyResultTaskRepository,
        IKeyResultRepository keyResultRepository,
        IObjectiveRepository objectiveRepository,
        IOKRSessionRepository okrSessionRepository)
    {
        _keyResultTaskRepository = keyResultTaskRepository;
        _keyResultRepository = keyResultRepository;
        _objectiveRepository = objectiveRepository;
        _okrSessionRepository = okrSessionRepository;
    }

    public async Task Handle(DeleteKeyResultTaskCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await new DeleteKeyResultTaskCommandValidator().ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var keyResultTask = await _keyResultTaskRepository.GetByIdAsync(request.Id);
        if (keyResultTask == null)
        {
            throw new NotFoundException(nameof(KeyResultTask), request.Id);
        }

        // Soft delete: set IsDeleted to true and update ModifiedDate
        keyResultTask.IsDeleted = true;
        keyResultTask.ModifiedDate = DateTime.UtcNow;
        await _keyResultTaskRepository.UpdateAsync(keyResultTask);

        // Recalculate KeyResult progress
        var keyResult = await _keyResultRepository.GetByIdAsync(keyResultTask.KeyResultId);
        if (keyResult != null)
        {
            var allTasks = await _keyResultTaskRepository.GetByKeyResultAsync(keyResultTask.KeyResultId);
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
