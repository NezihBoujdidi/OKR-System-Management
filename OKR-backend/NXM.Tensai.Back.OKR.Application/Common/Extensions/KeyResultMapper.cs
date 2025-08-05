namespace NXM.Tensai.Back.OKR.Application;

public static class KeyResultMapper
{
    public static KeyResult ToEntity(this CreateKeyResultCommand command)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        return new KeyResult
        {
            Id = Guid.NewGuid(),
            ObjectiveId = command.ObjectiveId,
            UserId = command.UserId,
            Title = command.Title,
            Description = command.Description,
            StartedDate = command.StartedDate,
            EndDate = command.EndDate,
            CreatedDate = DateTime.UtcNow,
            Progress = command.Progress,
            Status = command.Status,
            IsDeleted = false
        };
    }

    public static void UpdateEntity(this UpdateKeyResultCommand command, KeyResult entity)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        entity.ObjectiveId = command.ObjectiveId;
        entity.UserId = command.UserId;
        entity.Title = command.Title;
        entity.Description = command.Description;
        entity.StartedDate = command.StartedDate;
        entity.EndDate = command.EndDate;
        entity.ModifiedDate = DateTime.UtcNow;
        entity.Progress = command.Progress;
    }

    public static KeyResultDto ToDto(this KeyResult keyResult)
    {
        if (keyResult == null) throw new ArgumentNullException(nameof(keyResult));

        return new KeyResultDto
        {
            Id = keyResult.Id,
            ObjectiveId = keyResult.ObjectiveId,
            UserId = keyResult.UserId,
            Title = keyResult.Title,
            Description = keyResult.Description,
            StartedDate = keyResult.StartedDate,
            EndDate = keyResult.EndDate,
            CreatedDate = keyResult.CreatedDate,
            ModifiedDate = keyResult.ModifiedDate,
            Progress = keyResult.Progress
        };
    }
}
