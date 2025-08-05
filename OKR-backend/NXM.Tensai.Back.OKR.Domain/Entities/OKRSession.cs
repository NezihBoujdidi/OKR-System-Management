namespace NXM.Tensai.Back.OKR.Domain;

public class OKRSession : BaseOKREntity
{
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool Approved { get; set; } = false;
    public string? Color { get; set; }
    public int? Progress { get; set; }

    public void RecalculateProgress(IEnumerable<Objective> objectives)
    {
        if (objectives == null || !objectives.Any())
        {
            this.Progress = 0;
            return;
        }
        this.Progress = (int)System.Math.Round(objectives.Average(o => o.Progress));
    }
}
