namespace NXM.Tensai.Back.OKR.Domain;

public class Organization : BaseEntity
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Country { get; set; }
    public string? Industry { get; set; }
    public int? Size { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
}
