namespace NXM.Tensai.Back.OKR.Application;

public static class TeamMapper
{
    public static Team ToEntity(this CreateTeamCommand command)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        return new Team
        {
            Id = Guid.NewGuid(),
            Name = command.Name,
            OrganizationId = command.OrganizationId,
            TeamManagerId = command.TeamManagerId,
            Description = command.Description,
            CreatedDate = DateTime.UtcNow
        };
    }

    public static void UpdateEntity(this UpdateTeamCommand command, Team entity)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        entity.Name = command.Name;
        entity.Description = command.Description;
        entity.OrganizationId = command.OrganizationId;
        entity.TeamManagerId = command.TeamManagerId;
        entity.ModifiedDate = DateTime.UtcNow;
    }

    public static TeamDto ToDto(this Team team)
    {
        if (team == null) throw new ArgumentNullException(nameof(team));

        return new TeamDto
        {
            Id = team.Id,
            Name = team.Name,
            Description = team.Description,
            OrganizationId = team.OrganizationId,
            TeamManagerId = team.TeamManagerId,
            CreatedDate = team.CreatedDate,
            ModifiedDate = team.ModifiedDate
        };
    }

    public static IEnumerable<TeamDto> ToDto(this IEnumerable<Team> teams)
    {
        if (teams == null) throw new ArgumentNullException(nameof(teams));

        return teams.Select(team => team.ToDto()).ToList();
    }
}
