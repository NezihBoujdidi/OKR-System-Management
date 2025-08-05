namespace NXM.Tensai.Back.OKR.Domain;

public class KeyResultTask : BaseOKREntity
{
    public Guid UserId { get; set; } // who created the task
    public Guid KeyResultId { get; set; }
    public KeyResult KeyResult { get; set; } = null!;
    public int Progress { get; set; } = 0;
    public Guid CollaboratorId { get; set; }
}
