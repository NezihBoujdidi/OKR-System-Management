namespace NXM.Tensai.Back.OKR.Domain;

public interface IObjectiveRepository : IRepository<Objective>
{
    // Add any specific methods for Objective if needed
    public Task<IEnumerable<Objective>> GetBySessionIdAsync(Guid okrSessionId);
    public Task<IEnumerable<Objective>> GetObjectivesByTeamIdAsync(Guid teamId);

}

