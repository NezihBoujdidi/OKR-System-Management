namespace NXM.Tensai.Back.OKR.Application;

public static class OKRSessionMapper
{
    public static OKRSession ToEntity(this CreateOKRSessionCommand command)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        return new OKRSession
        {
            Id = Guid.NewGuid(),
            OrganizationId = command.OrganizationId,
            Title = command.Title,
            Description = command.Description ?? string.Empty,
            StartedDate = command.StartedDate,
            EndDate = command.EndDate,
            UserId = command.UserId,
            IsActive = true,
            Approved = false,
            IsDeleted = false,
            Color = command.Color,
            Progress = 0,
            Status = command.Status, // Add Status from command
            CreatedDate = DateTime.UtcNow
        };
    }

    public static void UpdateEntity(this UpdateOKRSessionCommand command, OKRSession entity)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        entity.UserId = command.UserId;
        entity.Title = command.Title;
        entity.Description = command.Description ?? string.Empty;
        entity.Status = command.Status;
        entity.Color = command.Color;
        entity.StartedDate = command.StartedDate;
        entity.EndDate = command.EndDate;
        entity.ModifiedDate = DateTime.UtcNow;
    }

    public static OKRSessionDto ToDto(this OKRSession okrSession)
    {
        if (okrSession == null) throw new ArgumentNullException(nameof(okrSession));

        return new OKRSessionDto
        {
            Id = okrSession.Id,
            OrganizationId = okrSession.OrganizationId,
            Title = okrSession.Title,
            Description = okrSession.Description,
            StartedDate = okrSession.StartedDate,
            EndDate = okrSession.EndDate,
            UserId = okrSession.UserId,
            IsActive = okrSession.IsActive,
            Approved = okrSession.Approved,
            IsDeleted = okrSession.IsDeleted,
            Color = okrSession.Color,
            Status = okrSession.Status?.ToString(),
            Progress = okrSession.Progress,
            CreatedDate = okrSession.CreatedDate,
            ModifiedDate = okrSession.ModifiedDate
        };
    }

    public static IEnumerable<OKRSessionDto> ToDto(this IEnumerable<OKRSession> okrSessions)
    {
        if (okrSessions == null) throw new ArgumentNullException(nameof(okrSessions));
        return okrSessions.Select(okrSession => okrSession.ToDto()).ToList();
    }
}
