namespace NXM.Tensai.Back.OKR.Application.Common.Models;

public class CreateSubscriptionRequestDTO
{
    public Guid OrganizationId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string PlanId { get; set; }
    
    // For payment processing with Stripe Elements
    public string PaymentMethodId { get; set; }
} 