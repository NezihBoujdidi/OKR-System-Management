namespace NXM.Tensai.Back.OKR.Domain;

public interface IKeyResultTaskRepository : IRepository<KeyResultTask>
{
    // Add any specific methods for KeyResultTask if needed
    public Task<IEnumerable<KeyResultTask>> GetByKeyResultAsync(Guid keyResultId);
}

