using NXM.Tensai.Back.OKR.Application.Common.Models;

namespace NXM.Tensai.Back.OKR.Application;

public interface IManagerObjectiveService
{
    Task<List<ObjectiveDto>> GetObjectivesByManagerIdAsync(Guid managerId);
}