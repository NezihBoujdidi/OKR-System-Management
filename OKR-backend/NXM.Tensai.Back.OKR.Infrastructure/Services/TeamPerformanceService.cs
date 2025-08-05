using NXM.Tensai.Back.OKR.Application;
using NXM.Tensai.Back.OKR.Application.Common.Models;
using NXM.Tensai.Back.OKR.Domain;
using Microsoft.EntityFrameworkCore;

namespace NXM.Tensai.Back.OKR.Infrastructure;

public class TeamPerformanceService : ITeamPerformanceService
{
    private readonly OKRDbContext _context;
    private readonly ICollaboratorPerformanceService _collaboratorPerformanceService;

    public TeamPerformanceService(OKRDbContext context, ICollaboratorPerformanceService collaboratorPerformanceService)
    {
        _context = context;
        _collaboratorPerformanceService = collaboratorPerformanceService;
    }

    public async Task<List<TeamPerformanceBarDto>> GetTeamPerformanceBarChartAsync(Guid organizationId)
    {
        var teams = await _context.Set<Team>()
            .Where(t => t.OrganizationId == organizationId && !t.IsDeleted)
            .ToListAsync();

        var teamIds = teams.Select(t => t.Id).ToList();

        var teamUsers = await _context.Set<TeamUser>()
            .Where(tu => teamIds.Contains(tu.TeamId))
            .ToListAsync();

        var allCollabPerformances = await _collaboratorPerformanceService.GetCollaboratorPerformanceListWithRangesAsync(organizationId);

        var result = new List<TeamPerformanceBarDto>();

        foreach (var team in teams)
        {
            var collabIds = teamUsers.Where(tu => tu.TeamId == team.Id).Select(tu => tu.UserId).Distinct().ToList();

            var teamCollabPerformances = allCollabPerformances.Where(cp => collabIds.Contains(cp.CollaboratorId)).ToList();

            int count = teamCollabPerformances.Count;
            double avgAllTime = count > 0 ? teamCollabPerformances.Average(cp => cp.PerformanceAllTime) : 0;
            double avgLast30 = count > 0 ? teamCollabPerformances.Average(cp => cp.PerformanceLast30Days) : 0;
            double avgLast3 = count > 0 ? teamCollabPerformances.Average(cp => cp.PerformanceLast3Months) : 0;

            result.Add(new TeamPerformanceBarDto
            {
                TeamId = team.Id,
                TeamName = team.Name,
                PerformanceAllTime = (int)Math.Round(avgAllTime),
                PerformanceLast30Days = (int)Math.Round(avgLast30),
                PerformanceLast3Months = (int)Math.Round(avgLast3)
            });
        }

        return result;
    }
}
