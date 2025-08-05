namespace NXM.Tensai.Back.OKR.Infrastructure;

public class TeamRepository : Repository<Team>, ITeamRepository
{
    public TeamRepository(OKRDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Team>> GetTeamsByManagerIdAsync(Guid managerId)
    {
        return await _dbSet.Where(t => t.TeamManagerId == managerId)
                          .ToListAsync();
    }
    
    public async Task<IEnumerable<Team>> GetTeamsByOrganizationIdAsync(Guid organizationId)
    {
        return await _dbSet.Where(t => t.OrganizationId == organizationId)
                          .ToListAsync();
    }

    public async Task<IEnumerable<Team>> GetByIdsAsync(List<Guid> teamIds)
    {
        return await _dbSet.Where(t => teamIds.Contains(t.Id))
                          .ToListAsync();
    }
}
