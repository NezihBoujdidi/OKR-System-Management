namespace NXM.Tensai.Back.OKR.Infrastructure;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly OKRDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(OKRDbContext context)
    {
        _context = context;
        _dbSet = _context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(Guid id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }

    public async Task<PaginatedList<T>> GetPagedAsync(
        int pageIndex,
        int pageSize,
        Expression<Func<T, bool>> predicate = null,
        params Expression<Func<T, object>>[] includes)
    {
        IQueryable<T> query = _dbSet;

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        var count = await query.CountAsync();
        var items = await query.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PaginatedList<T>(items, count, pageIndex, pageSize);
    }

    public async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities)
    {
        await _dbSet.AddRangeAsync(entities);
        await _context.SaveChangesAsync();
        return entities;
    }

    public async Task UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(T entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<int> CountAsync(Expression<Func<T, bool>> predicate = null)
    {
        if (predicate != null)
        {
            return await _dbSet.CountAsync(predicate);
        }

        return await _dbSet.CountAsync();
    }

    public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.AnyAsync(predicate);
    }
}
