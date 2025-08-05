namespace NXM.Tensai.Back.OKR.Infrastructure;

public class KeyResultTaskRepository : Repository<KeyResultTask>, IKeyResultTaskRepository
{
    public KeyResultTaskRepository(OKRDbContext context) : base(context)
    {
    }
      public async Task<IEnumerable<KeyResultTask>> GetByKeyResultAsync(Guid keyResultId)
    {
        return await _context.Set<KeyResultTask>()
            .Where(o => o.KeyResultId == keyResultId)
            .ToListAsync();
    }

    // Add any specific methods for KeyResultTask if needed
}
