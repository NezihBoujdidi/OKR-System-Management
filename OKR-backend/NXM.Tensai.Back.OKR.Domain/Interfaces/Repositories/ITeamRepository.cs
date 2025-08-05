namespace NXM.Tensai.Back.OKR.Domain;

public interface ITeamRepository : IRepository<Team>
{
    Task<IEnumerable<Team>> GetTeamsByManagerIdAsync(Guid managerId);
    Task<IEnumerable<Team>> GetTeamsByOrganizationIdAsync(Guid organizationId);
    Task<IEnumerable<Team>> GetByIdsAsync(List<Guid> teamIds);
}

