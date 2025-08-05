using NXM.Tensai.Back.OKR.Application;
using NXM.Tensai.Back.OKR.Domain;
using Microsoft.EntityFrameworkCore;

namespace NXM.Tensai.Back.OKR.Infrastructure;

public class OKRStatsService : IOKRStatsService
{
    private readonly OKRDbContext _context;

    public OKRStatsService(OKRDbContext context)
    {
        _context = context;
    }

    public async Task<(int ActiveNow, int ActiveLastMonth)> GetActiveOKRSessionStatsByOrganizationIdAsync(Guid organizationId)
    {
        var now = DateTime.UtcNow;
        var startOfThisMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var startOfLastMonth = startOfThisMonth.AddMonths(-1);
        var endOfLastMonth = startOfThisMonth.AddTicks(-1);

        // Active now (in progress, not deleted)
        var activeNow = await _context.Set<OKRSession>()
            .Where(x => x.OrganizationId == organizationId && x.Status == Status.InProgress && !x.IsDeleted)
            .CountAsync();

        // Active last month (in progress, not deleted, and started before end of last month and ended after start of last month)
        var activeLastMonth = await _context.Set<OKRSession>()
            .Where(x => x.OrganizationId == organizationId
                && x.Status == Status.InProgress
                && !x.IsDeleted
                && x.StartedDate <= endOfLastMonth
                && x.EndDate >= startOfLastMonth)
            .CountAsync();

        return (activeNow, activeLastMonth);
    }
}
