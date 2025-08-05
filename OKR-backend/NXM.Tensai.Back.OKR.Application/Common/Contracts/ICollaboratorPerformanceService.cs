using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NXM.Tensai.Back.OKR.Application.Common.Models;

namespace NXM.Tensai.Back.OKR.Application;

public interface ICollaboratorPerformanceService
{
    Task<List<CollaboratorPerformanceDto>> GetCollaboratorPerformanceListAsync(Guid organizationId);
    Task<List<CollaboratorPerformanceRangeDto>> GetCollaboratorPerformanceListWithRangesAsync(Guid organizationId);
    Task<List<CollaboratorPerformanceDto>> GetCollaboratorPerformanceListAsync(Guid organizationId, DateTime? from, DateTime? to);
}
