namespace NXM.Tensai.Back.OKR.Application;

public class TeamDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public string? Description { get; init; } 
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public Guid? TeamManagerId { get; set; }
}
