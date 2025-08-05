using NXM.Tensai.Back.OKR.Application;
public class SuperAdminDashboardDto
{
    public int ActiveSubscriptions { get; set; }

    // Monthly Recurring Revenue
    public decimal Mrr { get; set; }

    // Annual Recurring Revenue
    public decimal Arr { get; set; }

    // Average Revenue Per User
    public decimal Arpu { get; set; }

    // Churn Rate over last 30 days (percentage)
    public double ChurnRate { get; set; }

    // Distribution of active subscriptions by plan
    public List<PlanDistributionItemDto> PlanDistribution { get; set; }

    // Optionally, keep time-based revenue if desired
    // public decimal RevenueThisWeek { get; set; }
    // public decimal RevenueThisMonth { get; set; }
    // public decimal RevenueThisYear { get; set; }
}