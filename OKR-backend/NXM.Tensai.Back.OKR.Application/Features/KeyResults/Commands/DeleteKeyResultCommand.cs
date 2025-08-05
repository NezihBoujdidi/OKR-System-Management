namespace NXM.Tensai.Back.OKR.Application;

public record DeleteKeyResultCommand(Guid Id) : IRequest;

public class DeleteKeyResultCommandValidator : AbstractValidator<DeleteKeyResultCommand>
{
    public DeleteKeyResultCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Key Result ID must not be empty.");
    }
}

public class DeleteKeyResultCommandHandler : IRequestHandler<DeleteKeyResultCommand>
{
    private readonly IKeyResultRepository _keyResultRepository;
    private readonly IObjectiveRepository _objectiveRepository;
    private readonly IOKRSessionRepository _okrSessionRepository;

    public DeleteKeyResultCommandHandler(
        IKeyResultRepository keyResultRepository,
        IObjectiveRepository objectiveRepository,
        IOKRSessionRepository okrSessionRepository)
    {
        _keyResultRepository = keyResultRepository;
        _objectiveRepository = objectiveRepository;
        _okrSessionRepository = okrSessionRepository;
    }

    public async Task Handle(DeleteKeyResultCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await new DeleteKeyResultCommandValidator().ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var keyResult = await _keyResultRepository.GetByIdAsync(request.Id);
        if (keyResult == null)
        {
            throw new NotFoundException(nameof(KeyResult), request.Id);
        }

        // Soft delete: set IsDeleted to true and update ModifiedDate
        keyResult.IsDeleted = true;
        keyResult.ModifiedDate = DateTime.UtcNow;
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
