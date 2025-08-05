namespace NXM.Tensai.Back.OKR.Infrastructure;

public class OKRSessionTeamRepository : Repository<OKRSessionTeam>, IOKRSessionTeamRepository
{
    public OKRSessionTeamRepository(OKRDbContext context) : base(context)
    {
    }

    public async Task<List<OKRSessionTeam>> GetBySessionIdAsync(Guid okrSessionId)
    {
        return await _context.OKRSessionTeams
            .Where(x => x.OKRSessionId == okrSessionId)
            .ToListAsync();
    }
    public async Task<List<Guid>> GetSessionIdsByTeamIdAsync(Guid teamId)
    {
        return await _context.OKRSessionTeams
            .Where(x => x.TeamId == teamId)
            .Select(x => x.OKRSessionId)
            .Distinct()
            .ToListAsync();
    }
}

