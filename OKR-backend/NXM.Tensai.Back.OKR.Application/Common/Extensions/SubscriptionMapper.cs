using NXM.Tensai.Back.OKR.Application.Common.Models;
using NXM.Tensai.Back.OKR.Domain;

namespace NXM.Tensai.Back.OKR.Application;

public static class SubscriptionMapper
{
    public static SubscriptionResponseDTO ToResponse(this Subscription subscription)
    {
        return new SubscriptionResponseDTO
        {
            Id = subscription.Id,
            OrganizationId = subscription.OrganizationId,
            CreatedByUserId = subscription.CreatedByUserId,
            Plan = subscription.Plan.ToString(),
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            IsActive = subscription.IsActive,
            Status = subscription.Status,
            Amount = subscription.Amount,
            Currency = subscription.Currency
        };
    }

    public static IEnumerable<SubscriptionResponseDTO> ToResponse(this IEnumerable<Subscription> subscriptions)
    {
        return subscriptions.Select(subscription => subscription.ToResponse()).ToList();
    }

    public static SubscriptionDto ToDto(this Subscription subscription)
    {
        return new SubscriptionDto
        {
            Id = subscription.Id,
            OrganizationId = subscription.OrganizationId,
            CreatedByUserId = subscription.CreatedByUserId,
            Plan = subscription.Plan.ToString(),
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            IsActive = subscription.IsActive,
            Status = subscription.Status,
            Amount = subscription.Amount,
            Currency = subscription.Currency
        };
    }

    public static IEnumerable<SubscriptionDto> ToDto(this IEnumerable<Subscription> subscriptions)
    {
        return subscriptions.Select(subscription => subscription.ToDto()).ToList();
    }
}