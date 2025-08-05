namespace NXM.Tensai.Back.OKR.Infrastructure;

public class ObjectiveRepository : Repository<Objective>, IObjectiveRepository
{   
    private readonly OKRDbContext _context;
    public ObjectiveRepository(OKRDbContext context) : base(context)
    {
        _context = context;
    }
    // Add any specific methods for Objective if needed
    public async Task<IEnumerable<Objective>> GetBySessionIdAsync(Guid okrSessionId)
    {
        return await _context.Set<Objective>()
            .Where(o => o.OKRSessionId == okrSessionId)
            .Where(o => !o.IsDeleted)
            .ToListAsync();
    }

    public async Task<IEnumerable<Objective>> GetObjectivesByTeamIdAsync(Guid teamId)
    {
        return await _context.Set<Objective>()
            .Where(o => o.ResponsibleTeamId == teamId)
            .ToListAsync();
    }
}
