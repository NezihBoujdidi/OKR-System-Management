namespace NXM.Tensai.Back.OKR.Application;

public static class ObjectiveMapper
{
    public static Objective ToEntity(this CreateObjectiveCommand command)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        return new Objective
        {
            Id = Guid.NewGuid(),
            OKRSessionId = command.OKRSessionId,
            UserId = command.UserId,
            Title = command.Title,
            Description = command.Description,
            StartedDate = command.StartedDate,
            EndDate = command.EndDate,
            Status = command.Status,
            Priority = command.Priority,
            ResponsibleTeamId = command.ResponsibleTeamId,
            IsDeleted = command.IsDeleted,
            Progress = command.Progress,
            CreatedDate = DateTime.UtcNow
        };
    }

    public static void UpdateEntity(this UpdateObjectiveCommand command, Objective entity)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        entity.OKRSessionId = command.OKRSessionId;
        entity.UserId = command.UserId;
        entity.Title = command.Title;
        entity.Description = command.Description;
        entity.StartedDate = command.StartedDate;
        entity.EndDate = command.EndDate;
        entity.ModifiedDate = DateTime.UtcNow;
        entity.ResponsibleTeamId = command.ResponsibleTeamId;
        entity.Status = command.Status;
        entity.Priority = command.Priority;
        entity.Progress = command.Progress ?? 0;

    }

    public static ObjectiveDto ToDto(this Objective objective)
    {
        if (objective == null) throw new ArgumentNullException(nameof(objective));

        return new ObjectiveDto
        {
            Id = objective.Id,
            OKRSessionId = objective.OKRSessionId,
            UserId = objective.UserId,
            ResponsibleTeamId = objective.ResponsibleTeamId,
            Title = objective.Title,
            Description = objective.Description,
            StartedDate = objective.StartedDate,
            EndDate = objective.EndDate,
            CreatedDate = objective.CreatedDate,
            ModifiedDate = objective.ModifiedDate,
            Progress = objective.Progress,
            IsDeleted = objective.IsDeleted,
            Status = objective.Status,
            Priority = objective.Priority
        };
    }
}
