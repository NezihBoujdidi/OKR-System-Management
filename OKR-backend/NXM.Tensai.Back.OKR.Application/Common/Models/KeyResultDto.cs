namespace NXM.Tensai.Back.OKR.Application;

public class KeyResultDto
{
    public Guid Id { get; set; }
    public Guid ObjectiveId { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartedDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public Status? Status { get; set; }
    public int Progress { get; set; }
}
