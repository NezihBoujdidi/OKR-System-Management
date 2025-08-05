namespace NXM.Tensai.Back.OKR.Application.Common.Models;

public class CollaboratorPerformanceDto
{
    public Guid CollaboratorId { get; set; }
    public int Performance { get; set; }
}

public class CollaboratorPerformanceRangeDto
{
    public Guid CollaboratorId { get; set; }
    public int PerformanceAllTime { get; set; }
    public int PerformanceLast30Days { get; set; }
    public int PerformanceLast3Months { get; set; }
}
