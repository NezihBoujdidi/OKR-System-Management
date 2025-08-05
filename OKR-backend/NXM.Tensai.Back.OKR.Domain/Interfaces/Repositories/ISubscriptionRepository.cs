namespace NXM.Tensai.Back.OKR.Domain;

public interface ISubscriptionRepository : IRepository<Subscription>
{
    Task<Subscription?> GetByOrganizationIdAsync(Guid organizationId);
}
