using System.Collections.Generic;
using System.Linq;

namespace NXM.Tensai.Back.OKR.Domain;

public class KeyResult : BaseOKREntity
{
    public Guid UserId { get; set; }
    public Guid ObjectiveId { get; set; }
    public Objective Objective { get; set; } = null!;
    public int Progress { get; set; }

    // Add this method for progress calculation
    public void RecalculateProgress(IEnumerable<KeyResultTask> tasks)
    {
        if (tasks == null || !tasks.Any())
        {
            this.Progress = 0;
            return;
        }
        var completed = tasks.Count(t => t.Status == NXM.Tensai.Back.OKR.Domain.Status.Completed);
        this.Progress = (int)System.Math.Round((double)completed * 100 / tasks.Count());
    }
}
