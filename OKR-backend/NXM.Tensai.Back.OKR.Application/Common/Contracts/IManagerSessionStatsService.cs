using NXM.Tensai.Back.OKR.Application.Common.Models;
namespace NXM.Tensai.Back.OKR.Application;

public interface IManagerSessionStatsService
{
    Task<ManagerSessionStatsDto> GetManagerSessionStatsAsync(Guid managerId);
}
