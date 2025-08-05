using System;

namespace NXM.Tensai.Back.OKR.Application.Common.Models;

public class CollaboratorTaskStatusStatsDto
{
    public Guid CollaboratorId { get; set; }
    public int NotStarted { get; set; }
    public int InProgress { get; set; }
    public int Completed { get; set; }
    public int Overdue { get; set; }
}
