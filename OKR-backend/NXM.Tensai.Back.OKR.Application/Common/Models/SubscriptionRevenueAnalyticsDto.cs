using System;
using System.Collections.Generic;

namespace NXM.Tensai.Back.OKR.Application.Common.Models;

public class SubscriptionRevenueAnalyticsDto
{
    public decimal TotalRevenue { get; set; }
    public double ConversionRate { get; set; }
    public List<RevenueByPlanDto> RevenueByPlan { get; set; }
}

public class RevenueByPlanDto
{
    public string PlanType { get; set; }
    public decimal Revenue { get; set; }
}
