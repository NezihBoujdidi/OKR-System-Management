namespace NXM.Tensai.Back.OKR.Domain;

public class BaseOKREntity : BaseEntity
{
    public string Title { get; set; } = null!;
    public string? Description { get; set; } = null!;
    public Status? Status { get; set; }
    public Priority? Priority { get; set; }
    public DateTime StartedDate { get; set; }
    public DateTime EndDate { get; set; }
}
