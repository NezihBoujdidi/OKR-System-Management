using System.Collections.Generic;

namespace NXM.Tensai.Back.OKR.Application.Common.Models;

public class UserGrowthStatsDto
{
    public List<YearlyGrowthDto> Yearly { get; set; } = new();
    public List<MonthlyGrowthDto> Monthly { get; set; } = new();
}

