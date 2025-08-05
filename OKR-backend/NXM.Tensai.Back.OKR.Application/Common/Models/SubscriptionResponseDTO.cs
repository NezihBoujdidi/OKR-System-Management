namespace NXM.Tensai.Back.OKR.Application.Common.Models;

public class SubscriptionResponseDTO
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string Plan { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public string Status { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; }
} 