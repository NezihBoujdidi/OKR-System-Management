namespace NXM.Tensai.Back.OKR.Domain;

public class SubscriptionPlanEntity : BaseEntity
{
    public string PlanId { get; set; } // e.g. "basic", "professional"
    public string Name { get; set; } // e.g. "Basic Plan"
    public string Description { get; set; }
    public decimal Price { get; set; }
    public string Interval { get; set; } = "month"; // billing interval
    public SubscriptionPlan PlanType { get; set; } // Enum reference
    public string StripeProductId { get; set; }
    public string StripePriceId { get; set; }
    public bool IsActive { get; set; } = true;
    public List<SubscriptionPlanFeature> Features { get; set; } = new();
} 