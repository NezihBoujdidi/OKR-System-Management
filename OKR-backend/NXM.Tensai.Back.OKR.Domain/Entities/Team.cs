namespace NXM.Tensai.Back.OKR.Domain;

public class Team : BaseEntity
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public Guid? TeamManagerId { get; set; }
    public User? TeamManager { get; set; } = null!;
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
}
