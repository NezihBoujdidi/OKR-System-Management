using NXM.Tensai.Back.OKR.Application;
using NXM.Tensai.Back.OKR.Application.Common.Models;
using NXM.Tensai.Back.OKR.Domain;
using NXM.Tensai.Back.OKR.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace NXM.Tensai.Back.OKR.Infrastructure;

public class GlobalStatsService : IGlobalStatsService
{
    private readonly OKRDbContext _context;
    private readonly UserManager<User> _userManager;

    public GlobalStatsService(OKRDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<GlobalOrgOkrStatsDto> GetGlobalOrgOkrStatsAsync()
    {
        var orgCount = await _context.Organizations.Where(o => !o.IsDeleted).CountAsync();
        var okrSessionCount = await _context.OKRSessions.Where(s => !s.IsDeleted).CountAsync();
        return new GlobalOrgOkrStatsDto
        {
            OrganizationCount = orgCount,
            OKRSessionCount = okrSessionCount
        };
    }

    public async Task<UserGrowthStatsDto> GetUserGrowthStatsAsync()
    {
        var now = DateTime.UtcNow;

        // Yearly: last 5 years (including this year)
        var fiveYearsAgo = DateTime.SpecifyKind(now.AddYears(-4), DateTimeKind.Utc);
        var yearlyRaw = await _context.Users
            .Where(u => u.CreatedDate >= new DateTime(fiveYearsAgo.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc))
            .GroupBy(u => u.CreatedDate.Year)
            .Select(g => new { Year = g.Key, Count = g.Count() })
            .OrderBy(g => g.Year)
            .ToListAsync();

        var yearly = new List<YearlyGrowthDto>();
        int startYear = fiveYearsAgo.Year;
        int endYear = now.Year;
        int cumulative = 0;
        for (int year = startYear; year <= endYear; year++)
        {
            var found = yearlyRaw.FirstOrDefault(y => y.Year == year);
            int count = found?.Count ?? 0;
            cumulative += count;
            yearly.Add(new YearlyGrowthDto { Year = year, Count = cumulative });
        }

        // Monthly: last 12 months (including this month)
        var oneYearAgo = DateTime.SpecifyKind(now.AddMonths(-11), DateTimeKind.Utc);
        var monthlyRaw = await _context.Users
            .Where(u => u.CreatedDate >= new DateTime(oneYearAgo.Year, oneYearAgo.Month, 1, 0, 0, 0, DateTimeKind.Utc))
            .GroupBy(u => new { u.CreatedDate.Year, u.CreatedDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .OrderBy(g => g.Year).ThenBy(g => g.Month)
            .ToListAsync();

        var monthly = new List<MonthlyGrowthDto>();
        var current = new DateTime(oneYearAgo.Year, oneYearAgo.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        int monthlyCumulative = 0;
        while (current <= end)
        {
            var found = monthlyRaw.FirstOrDefault(m => m.Year == current.Year && m.Month == current.Month);
            int count = found?.Count ?? 0;
            monthlyCumulative += count;
            monthly.Add(new MonthlyGrowthDto { Year = current.Year, Month = current.Month, Count = monthlyCumulative });
            current = current.AddMonths(1);
        }

        return new UserGrowthStatsDto
        {
            Yearly = yearly,
            Monthly = monthly
        };
    }

    public async Task<UserRolesCountDto> GetUserRolesCountAsync()
    {
        var users = await _context.Users.ToListAsync();
        int orgAdmins = 0, teamManagers = 0, collaborators = 0;

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains(RoleType.OrganizationAdmin.ToString()))
                orgAdmins++;
            else if (roles.Contains(RoleType.TeamManager.ToString()))
                teamManagers++;
            else if (roles.Contains(RoleType.Collaborator.ToString()))
                collaborators++;
        }

        return new UserRolesCountDto
        {
            OrganizationAdmins = orgAdmins,
            TeamManagers = teamManagers,
            Collaborators = collaborators
        };
    }

    public async Task<OrgPaidPlanCountDto> GetOrgPaidPlanCountAsync()
    {
        var paidPlans = new[] { SubscriptionPlan.Basic, SubscriptionPlan.Professional, SubscriptionPlan.Enterprise };
        var paidOrgIds = await _context.Set<Subscription>()
            .Where(s => paidPlans.Contains(s.Plan) && !s.IsDeleted)
            .Select(s => s.OrganizationId)
            .Distinct()
            .ToListAsync();

        // Only count organizations that are not deleted
        var paidOrgCount = await _context.Organizations
            .Where(o => !o.IsDeleted && paidOrgIds.Contains(o.Id))
            .CountAsync();

        return new OrgPaidPlanCountDto
        {
            PaidOrganizations = paidOrgCount
        };
    }
}
