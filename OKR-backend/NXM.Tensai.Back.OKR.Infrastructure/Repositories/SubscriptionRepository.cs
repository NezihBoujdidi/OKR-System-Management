using Microsoft.EntityFrameworkCore;
using NXM.Tensai.Back.OKR.Domain;
using NXM.Tensai.Back.OKR.Infrastructure.Persistence;

namespace NXM.Tensai.Back.OKR.Infrastructure;

public class SubscriptionRepository : Repository<Subscription>, ISubscriptionRepository
{
    public SubscriptionRepository(OKRDbContext context) : base(context)
    {
    }

    public async Task<Subscription?> GetByOrganizationIdAsync(Guid organizationId)
    {
        return await _dbSet.FirstOrDefaultAsync(s => s.OrganizationId == organizationId);
    }
}
