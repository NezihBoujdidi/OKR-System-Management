using NXM.Tensai.Back.OKR.Application.Common.Models;

namespace NXM.Tensai.Back.OKR.Application;

public interface ISubscriptionPlanService
{
    Task<List<SubscriptionPlanDTO>> GetAllPlansAsync(bool includeInactive = false);
    Task<SubscriptionPlanDTO> GetPlanByIdAsync(Guid id);
    Task<SubscriptionPlanDTO> GetPlanByPlanIdAsync(string planId);
    Task<SubscriptionPlanDTO> CreatePlanAsync(CreateSubscriptionPlanDTO dto);
    Task<SubscriptionPlanDTO> UpdatePlanAsync(Guid id, UpdateSubscriptionPlanDTO dto);
    Task<bool> DeletePlanAsync(Guid id);
    Task<bool> ActivatePlanAsync(Guid id);
    Task<bool> DeactivatePlanAsync(Guid id);
} 