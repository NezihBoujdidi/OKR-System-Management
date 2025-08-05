namespace NXM.Tensai.Back.OKR.Application.Common.Models;

public class TeamPerformanceBarDto
{
    public Guid TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public int PerformanceAllTime { get; set; }
    public int PerformanceLast30Days { get; set; }
    public int PerformanceLast3Months { get; set; }
}
