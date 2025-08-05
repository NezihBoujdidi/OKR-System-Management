namespace NXM.Tensai.Back.OKR.Application;

public class OKRSessionDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartedDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<Guid> TeamIds { get; set; } = new List<Guid>();
    public Guid UserId { get; set; }
    public bool IsActive { get; set; }
    public bool Approved { get; set; }
    public bool IsDeleted { get; set; }
    public string? Color { get; set; }
    public string? Status { get; set; }
    public int? Progress { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
}
