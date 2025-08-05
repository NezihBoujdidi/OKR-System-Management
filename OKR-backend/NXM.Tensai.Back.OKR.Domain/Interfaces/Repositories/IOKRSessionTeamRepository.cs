namespace NXM.Tensai.Back.OKR.Domain;

public interface IOKRSessionTeamRepository : IRepository<OKRSessionTeam>
{
    Task<List<OKRSessionTeam>> GetBySessionIdAsync(Guid okrSessionId);
    Task<List<Guid>> GetSessionIdsByTeamIdAsync(Guid teamId);
}
