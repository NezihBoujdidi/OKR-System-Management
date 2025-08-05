namespace NXM.Tensai.Back.OKR.Domain;

public interface IOKRSessionRepository : IRepository<OKRSession>
{
    Task<IEnumerable<OKRSession>> GetByOrganizationIdAsync(Guid organizationId);
    Task<List<OKRSession>> GetByIdsAsync(IEnumerable<Guid> ids);
}
