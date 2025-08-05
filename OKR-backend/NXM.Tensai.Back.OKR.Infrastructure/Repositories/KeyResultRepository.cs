namespace NXM.Tensai.Back.OKR.Infrastructure;

public class KeyResultRepository : Repository<KeyResult>, IKeyResultRepository
{
    public KeyResultRepository(OKRDbContext context) : base(context)
    {
    }

     public async Task<IEnumerable<KeyResult>> GetByObjectiveAsync(Guid objectiveId)
    {
        return await _context.Set<KeyResult>()
            .Where(o => o.ObjectiveId == objectiveId)
            .ToListAsync();
    }

    // Add any specific methods for KeyResult if needed
}
