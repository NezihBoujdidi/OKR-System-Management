using MediatR;
using FluentValidation;
using NXM.Tensai.Back.OKR.Domain;
using NXM.Tensai.Back.OKR.Domain.Interfaces.Repositories;

namespace NXM.Tensai.Back.OKR.Application.Features.KeyResultTasks.Commands;

public class ToggleKeyResultTaskStatusCommand : IRequest<KeyResultTask>
{
    public Guid KeyResultTaskId { get; init; }
    public Guid KeyResultId { get; init; }
    public Guid ObjectiveId { get; init; }
    public bool Complete { get; init; }
}

public class ToggleKeyResultTaskStatusCommandValidator : AbstractValidator<ToggleKeyResultTaskStatusCommand>
{
    public ToggleKeyResultTaskStatusCommandValidator()
    {
        RuleFor(x => x.KeyResultTaskId).NotEmpty();
        RuleFor(x => x.KeyResultId).NotEmpty();
        RuleFor(x => x.ObjectiveId).NotEmpty();
    }
}

public class ToggleKeyResultTaskStatusCommandHandler : IRequestHandler<ToggleKeyResultTaskStatusCommand, KeyResultTask>
{
    private readonly IKeyResultTaskRepository _taskRepository;
    private readonly IKeyResultRepository _keyResultRepository;
    private readonly IObjectiveRepository _objectiveRepository;
    private readonly IValidator<ToggleKeyResultTaskStatusCommand> _validator;
    private readonly IOKRSessionRepository _okrSessionRepository;

    public ToggleKeyResultTaskStatusCommandHandler(
        IKeyResultTaskRepository taskRepository,
        IKeyResultRepository keyResultRepository,
        IObjectiveRepository objectiveRepository,
        IValidator<ToggleKeyResultTaskStatusCommand> validator,
        IOKRSessionRepository okrSessionRepository)
    {
        _taskRepository = taskRepository;
        _keyResultRepository = keyResultRepository;
        _objectiveRepository = objectiveRepository;
        _validator = validator;
        _okrSessionRepository = okrSessionRepository;
    }

    public async Task<KeyResultTask> Handle(ToggleKeyResultTaskStatusCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var task = await _taskRepository.GetByIdAsync(request.KeyResultTaskId);
        if (task == null)
            throw new NotFoundException(nameof(KeyResultTask), request.KeyResultTaskId);

        if (request.Complete)
        {
            task.Status = Status.Completed;
            task.Progress = 100;
        }
        else
        {
            var today = DateTime.UtcNow.Date;
            if (today > task.EndDate.Date)
            {
                task.Status = Status.Overdue;
            }
            else
            {
                task.Status = Status.NotStarted;
            }
            task.Progress = 0;
        }
        await _taskRepository.UpdateAsync(task);

        var keyResult = await _keyResultRepository.GetByIdAsync(request.KeyResultId);
        if (keyResult == null)
            throw new NotFoundException(nameof(KeyResult), request.KeyResultId);
        var allTasks = await _taskRepository.GetByKeyResultAsync(request.KeyResultId);
        keyResult.RecalculateProgress(allTasks);
        await _keyResultRepository.UpdateAsync(keyResult);

        var objective = await _objectiveRepository.GetByIdAsync(request.ObjectiveId);
        if (objective == null)
            throw new NotFoundException(nameof(Objective), request.ObjectiveId);
        var allKeyResults = await _keyResultRepository.GetByObjectiveAsync(request.ObjectiveId);
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

        return task;
    }
}
