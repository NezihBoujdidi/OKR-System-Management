using NXM.Tensai.Back.OKR.Application.Common.Models;

namespace NXM.Tensai.Back.OKR.Application;

public interface ISubscriptionService
{
    Task<SubscriptionResponseDTO> CreateSubscriptionAsync(CreateSubscriptionRequestDTO request);
    Task<SubscriptionResponseDTO> GetSubscriptionByOrganizationIdAsync(Guid organizationId);
    Task<SubscriptionResponseDTO> CancelSubscriptionAsync(Guid organizationId);
    Task<SubscriptionResponseDTO> UpdateSubscriptionAsync(Guid organizationId, string newPlanId);
    Task<List<BillingHistoryItemDto>> GetBillingHistoryAsync(Guid organizationId);
    Task<SuperAdminDashboardDto> GetSuperAdminStatsAsync();

} 