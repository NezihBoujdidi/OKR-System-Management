using System;
using System.Threading.Tasks;
using NXM.Tensai.Back.OKR.Application.Common.Models;

namespace NXM.Tensai.Back.OKR.Application;

public interface ISubscriptionAnalyticsService
{
    Task<SubscriptionRevenueAnalyticsDto> GetRevenueAnalyticsAsync();
}
