namespace NXM.Tensai.Back.OKR.Domain;

public interface ITeamUserRepository : IRepository<TeamUser>
{
    Task<IEnumerable<Team>> GetTeamsByCollaboratorIdAsync(Guid collaboratorId);
    Task<IEnumerable<User>> GetUsersByTeamIdAsync(Guid teamId);
    Task<List<Team>> GetTeamsByUserIdAsync(Guid userId);
    Task<TeamUser?> GetByTeamAndUserIdAsync(Guid teamId, Guid userId);
}

