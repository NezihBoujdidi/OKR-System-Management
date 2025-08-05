using NXM.Tensai.Back.OKR.Application;
using NXM.Tensai.Back.OKR.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace NXM.Tensai.Back.OKR.Infrastructure;

public class EmployeeGrowthStatsService : IEmployeeGrowthStatsService
{
    private readonly OKRDbContext _context;

    public EmployeeGrowthStatsService(OKRDbContext context)
    {
        _context = context;
    }

    public async Task<EmployeeGrowthStatsDto> GetEmployeeGrowthStatsAsync(Guid organizationId)
    {
        var now = DateTime.UtcNow;

        // Yearly: last 5 years (including this year)
        var fiveYearsAgo = DateTime.SpecifyKind(now.AddYears(-4), DateTimeKind.Utc);
        var yearlyRaw = await _context.Users
            .Where(u => u.OrganizationId == organizationId && u.CreatedDate >= new DateTime(fiveYearsAgo.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc))
            .GroupBy(u => u.CreatedDate.Year)
            .Select(g => new { Year = g.Key, Count = g.Count() })
            .OrderBy(g => g.Year)
            .ToListAsync();

        // Fill missing years and calculate cumulative
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
            .Where(u => u.OrganizationId == organizationId && u.CreatedDate >= new DateTime(oneYearAgo.Year, oneYearAgo.Month, 1, 0, 0, 0, DateTimeKind.Utc))
            .GroupBy(u => new { u.CreatedDate.Year, u.CreatedDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .OrderBy(g => g.Year).ThenBy(g => g.Month)
            .ToListAsync();

        // Fill missing months and calculate cumulative
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

        return new EmployeeGrowthStatsDto
        {
            Yearly = yearly,
            Monthly = monthly
        };
    }
}
