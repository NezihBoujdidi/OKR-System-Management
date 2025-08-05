namespace NXM.Tensai.Back.OKR.Infrastructure;

public class OKRSessionRepository : Repository<OKRSession>, IOKRSessionRepository
{
    public OKRSessionRepository(OKRDbContext context) : base(context)
    {
    }

    // Add any specific methods for OKRSession if needed
    public async Task<IEnumerable<OKRSession>> GetByOrganizationIdAsync(Guid organizationId)
    {
        return await _context.Set<OKRSession>()
            .Where(s => s.OrganizationId == organizationId && !s.IsDeleted)
            .ToListAsync();
    }
    public async Task<List<OKRSession>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        return await _context.OKRSessions
            .Where(x => ids.Contains(x.Id))
            .ToListAsync();
    }
}
