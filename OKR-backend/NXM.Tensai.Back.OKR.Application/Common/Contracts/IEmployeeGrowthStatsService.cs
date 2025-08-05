using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using NXM.Tensai.Back.OKR.Application.Common.Models;

namespace NXM.Tensai.Back.OKR.Application;

public interface IEmployeeGrowthStatsService 
{
    Task<EmployeeGrowthStatsDto> GetEmployeeGrowthStatsAsync(Guid organizationId);
}
