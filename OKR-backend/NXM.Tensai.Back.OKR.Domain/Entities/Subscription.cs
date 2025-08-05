namespace NXM.Tensai.Back.OKR.Domain;

public class Subscription : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string StripeCustomerId { get; set; }
    public string StripeSubscriptionId { get; set; }
    public SubscriptionPlan Plan { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string Status { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "usd";
    public string LastPaymentIntentId { get; set; }
} 