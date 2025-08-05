namespace NXM.Tensai.Back.OKR.Application;

public class OrganizationDto
{
    public Guid Id { get; set; }
    public string EncodedId { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Country { get; set; }
    public string? Industry { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public int? Size { get; set; }
    public bool IsActive { get; set; }
    public string? SubscriptionPlan { get; set; } // Added for organization subscription plan name
}
