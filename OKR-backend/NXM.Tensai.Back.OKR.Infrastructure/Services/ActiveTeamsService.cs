using NXM.Tensai.Back.OKR.Application.Common.Models;
using NXM.Tensai.Back.OKR.Application;
using NXM.Tensai.Back.OKR.Domain;
using Microsoft.EntityFrameworkCore;

namespace NXM.Tensai.Back.OKR.Infrastructure;

public class ActiveTeamsService : IActiveTeamsService
{
    private readonly OKRDbContext _context;

    public ActiveTeamsService(OKRDbContext context)
    {
        _context = context;
    }

    public async Task<(int ActiveNow, int ActiveLastMonth)> GetActiveTeamsStatsByOrganizationIdAsync(Guid organizationId)
    {
        var now = DateTime.UtcNow;
        var startOfThisMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var startOfLastMonth = startOfThisMonth.AddMonths(-1);
        var endOfLastMonth = startOfThisMonth.AddTicks(-1);

        // Active OKR sessions now
        var activeSessionIdsNow = await _context.OKRSessions
            .Where(x => x.OrganizationId == organizationId && x.Status == Status.InProgress && !x.IsDeleted)
            .Select(x => x.Id)
            .ToListAsync();

        // Active OKR sessions last month
        var activeSessionIdsLastMonth = await _context.OKRSessions
            .Where(x => x.OrganizationId == organizationId
                && x.Status == Status.InProgress
                && !x.IsDeleted
                && x.StartedDate <= endOfLastMonth
                && x.EndDate >= startOfLastMonth)
            .Select(x => x.Id)
            .ToListAsync();

        // Teams linked to active sessions now
        var activeTeamIdsNow = await _context.OKRSessionTeams
            .Where(x => activeSessionIdsNow.Contains(x.OKRSessionId))
            .Select(x => x.TeamId)
            .Distinct()
            .ToListAsync();

        // Teams linked to active sessions last month
        var activeTeamIdsLastMonth = await _context.OKRSessionTeams
            .Where(x => activeSessionIdsLastMonth.Contains(x.OKRSessionId))
            .Select(x => x.TeamId)
            .Distinct()
            .ToListAsync();

        return (activeTeamIdsNow.Count, activeTeamIdsLastMonth.Count);
    }
}
