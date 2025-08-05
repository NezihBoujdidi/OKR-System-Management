namespace NXM.Tensai.Back.OKR.Application;

public static class KeyResultTaskMapper
{
    public static KeyResultTask ToEntity(this CreateKeyResultTaskCommand command)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        return new KeyResultTask
        {
            Id = Guid.NewGuid(),
            KeyResultId = command.KeyResultId,
            UserId = command.UserId,
            Title = command.Title,
            Description = command.Description,
            StartedDate = command.StartedDate,
            EndDate = command.EndDate, // Changed DueDate to EndDate
            CreatedDate = DateTime.UtcNow,
            CollaboratorId = command.CollaboratorId,
            Progress = command.Progress,
            Priority = command.Priority
        };
    }

    public static void UpdateEntity(this UpdateKeyResultTaskCommand command, KeyResultTask entity)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        entity.KeyResultId = command.KeyResultId;
        entity.UserId = command.UserId;
        entity.Title = command.Title;
        entity.Description = command.Description;
        entity.StartedDate = command.StartedDate;
        entity.EndDate = command.EndDate; // Changed DueDate to EndDate
        entity.ModifiedDate = DateTime.UtcNow;
        entity.Progress = command.Progress;
        entity.Priority = command.Priority;
        entity.IsDeleted = command.IsDeleted;
    }

    public static KeyResultTaskDto ToDto(this KeyResultTask keyResultTask)
    {
        if (keyResultTask == null) throw new ArgumentNullException(nameof(keyResultTask));

        return new KeyResultTaskDto
        {
            Id = keyResultTask.Id,
            KeyResultId = keyResultTask.KeyResultId,
            UserId = keyResultTask.UserId,
            Title = keyResultTask.Title,
            Description = keyResultTask.Description,
            StartedDate = keyResultTask.StartedDate,
            EndDate = keyResultTask.EndDate, // Changed DueDate to EndDate
            CreatedDate = keyResultTask.CreatedDate,
            ModifiedDate = keyResultTask.ModifiedDate,
            CollaboratorId = keyResultTask.CollaboratorId,
            Progress = keyResultTask.Progress,
            Priority = keyResultTask.Priority,
            Status = keyResultTask.Status,
            IsDeleted = keyResultTask.IsDeleted
        };
    }
}
