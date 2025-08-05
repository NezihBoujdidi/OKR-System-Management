using NXM.Tensai.Back.OKR.Application.Common.Models;

namespace NXM.Tensai.Back.OKR.Application;

public interface IGlobalStatsService
{
    Task<GlobalOrgOkrStatsDto> GetGlobalOrgOkrStatsAsync();
    Task<UserGrowthStatsDto> GetUserGrowthStatsAsync();
    Task<UserRolesCountDto> GetUserRolesCountAsync();
    Task<OrgPaidPlanCountDto> GetOrgPaidPlanCountAsync();
}
