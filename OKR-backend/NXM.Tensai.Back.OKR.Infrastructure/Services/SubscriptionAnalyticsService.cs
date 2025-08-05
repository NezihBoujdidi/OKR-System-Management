using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NXM.Tensai.Back.OKR.Application;
using NXM.Tensai.Back.OKR.Application.Common.Models;
using NXM.Tensai.Back.OKR.Domain;

namespace NXM.Tensai.Back.OKR.Infrastructure;

public class SubscriptionAnalyticsService : ISubscriptionAnalyticsService
{
    private readonly OKRDbContext _context;

    public SubscriptionAnalyticsService(OKRDbContext context)
    {
        _context = context;
    }

    public async Task<SubscriptionRevenueAnalyticsDto> GetRevenueAnalyticsAsync()
    {
        var subscriptions = await _context.Set<Subscription>().ToListAsync();
        var organizations = await _context.Set<Organization>().ToListAsync();

        decimal totalRevenue = subscriptions.Sum(s => s.Amount);

        int totalOrgs = organizations.Count;
        int paidOrgs = subscriptions
            .Where(s => s.IsActive)
            .Select(s => s.OrganizationId)
            .Distinct()
            .Count();

        double conversionRate = totalOrgs > 0 ? (double)paidOrgs / totalOrgs : 0;

        var revenueByPlan = subscriptions
            .GroupBy(s => s.Plan.ToString())
            .Select(g => new RevenueByPlanDto
            {
                PlanType = g.Key,
                Revenue = g.Sum(x => x.Amount)
            })
            .ToList();

        return new SubscriptionRevenueAnalyticsDto
        {
            TotalRevenue = totalRevenue,
            ConversionRate = conversionRate,
            RevenueByPlan = revenueByPlan
        };
    }
}
