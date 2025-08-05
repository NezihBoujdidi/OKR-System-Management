namespace NXM.Tensai.Back.OKR.Application.Common.Models;

public class EmployeeGrowthStatsDto
{
    public List<YearlyGrowthDto> Yearly { get; set; } = new();
    public List<MonthlyGrowthDto> Monthly { get; set; } = new();
}

public class YearlyGrowthDto
{
    public int Year { get; set; }
    public int Count { get; set; }
}

public class MonthlyGrowthDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int Count { get; set; }
}
