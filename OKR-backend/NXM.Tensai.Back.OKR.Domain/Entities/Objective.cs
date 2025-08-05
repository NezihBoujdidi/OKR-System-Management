using System.Globalization;

namespace NXM.Tensai.Back.OKR.Domain;

public class Objective : BaseOKREntity
{
    public Guid OKRSessionId { get; set; }
    public OKRSession OKRSession { get; set; } = null!;
    public Guid UserId { get; set; }
    public Guid ResponsibleTeamId { get; set; }
    public Team ResponsibleTeam { get; set; } = null!; // team working on the objective
    public int Progress  { get; set; }

    public void RecalculateProgress(IEnumerable<KeyResult> keyResults)
    {
        if (keyResults == null || !keyResults.Any())
        {
            this.Progress = 0;
            this.Status = NXM.Tensai.Back.OKR.Domain.Status.NotStarted;
            return;
        }
        this.Progress = (int)System.Math.Round(keyResults.Average(kr => kr.Progress));

        // Set status based on key results
        if (keyResults.All(kr => kr.Progress == 100))
            this.Status = NXM.Tensai.Back.OKR.Domain.Status.Completed;
        else if (keyResults.Any(kr => kr.Progress > 0))
            this.Status = NXM.Tensai.Back.OKR.Domain.Status.InProgress;
        else
            this.Status = NXM.Tensai.Back.OKR.Domain.Status.NotStarted;

        // Mark as Overdue if end date has passed and not completed
        if (this.EndDate < DateTime.UtcNow && this.Status != NXM.Tensai.Back.OKR.Domain.Status.Completed)
            this.Status = NXM.Tensai.Back.OKR.Domain.Status.Overdue;
    }
}
