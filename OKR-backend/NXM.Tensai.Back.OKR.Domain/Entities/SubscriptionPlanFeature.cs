namespace NXM.Tensai.Back.OKR.Domain;

public class SubscriptionPlanFeature : BaseEntity
{
    public Guid SubscriptionPlanId { get; set; }
    public string Description { get; set; }
    public SubscriptionPlanEntity Plan { get; set; }
} 