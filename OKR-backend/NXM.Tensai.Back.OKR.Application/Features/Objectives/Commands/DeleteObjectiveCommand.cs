namespace NXM.Tensai.Back.OKR.Application;

public record DeleteObjectiveCommand(Guid Id) : IRequest;

public class DeleteObjectiveCommandValidator : AbstractValidator<DeleteObjectiveCommand>
{
    public DeleteObjectiveCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Objective ID must not be empty.");
    }
}

public class DeleteObjectiveCommandHandler : IRequestHandler<DeleteObjectiveCommand>
{
    private readonly IObjectiveRepository _objectiveRepository;
    private readonly IOKRSessionRepository _okrSessionRepository;

    public DeleteObjectiveCommandHandler(IObjectiveRepository objectiveRepository, IOKRSessionRepository okrSessionRepository)
    {
        _objectiveRepository = objectiveRepository;
        _okrSessionRepository = okrSessionRepository;
    }

    public async Task Handle(DeleteObjectiveCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await new DeleteObjectiveCommandValidator().ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var objective = await _objectiveRepository.GetByIdAsync(request.Id);
        if (objective == null)
        {
            throw new NotFoundException(nameof(Objective), request.Id);
        }

        // Soft delete: set IsDeleted to true and update ModifiedDate
        objective.IsDeleted = true;
        objective.ModifiedDate = DateTime.UtcNow;
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
