namespace NXM.Tensai.Back.OKR.Domain;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<PaginatedList<T>> GetPagedAsync(int pageIndex, int pageSize, Expression<Func<T, bool>> predicate = null, params Expression<Func<T, object>>[] includes);
    Task<T> AddAsync(T entity);
    Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
    Task<int> CountAsync(Expression<Func<T, bool>> predicate = null);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
}

