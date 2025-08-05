using NXM.Tensai.Back.OKR.Application;
using NXM.Tensai.Back.OKR.Application.Common.Models;
using NXM.Tensai.Back.OKR.Domain;
using Microsoft.EntityFrameworkCore;

namespace NXM.Tensai.Back.OKR.Infrastructure;

public class ManagerSessionStatsService : IManagerSessionStatsService
{
    private readonly OKRDbContext _context;

    public ManagerSessionStatsService(OKRDbContext context)
    {
        _context = context;
    }

    public async Task<ManagerSessionStatsDto> GetManagerSessionStatsAsync(Guid managerId)
    {
        // Get teams managed by this manager
        var teamIds = await _context.Teams
            .Where(t => t.TeamManagerId == managerId && !t.IsDeleted)
            .Select(t => t.Id)
            .ToListAsync();

        if (!teamIds.Any())
            return new ManagerSessionStatsDto { ActiveSessions = 0, DelayedSessions = 0 };

        // Get session IDs linked to these teams
        var sessionIds = await _context.OKRSessionTeams
            .Where(st => teamIds.Contains(st.TeamId))
            .Select(st => st.OKRSessionId)
            .Distinct()
            .ToListAsync();

        if (!sessionIds.Any())
            return new ManagerSessionStatsDto { ActiveSessions = 0, DelayedSessions = 0 };

        var now = DateTime.UtcNow;

        // Get sessions
        var sessions = await _context.OKRSessions
            .Where(s => sessionIds.Contains(s.Id) && !s.IsDeleted)
            .ToListAsync();

        var activeSessions = sessions.Count(s => s.Status == Status.InProgress);
        var delayedSessions = sessions.Count(s => s.EndDate < now && s.Status != Status.Completed);

        return new ManagerSessionStatsDto
        {
            ActiveSessions = activeSessions,
            DelayedSessions = delayedSessions
        };
    }
}
