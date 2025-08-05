namespace NXM.Tensai.Back.OKR.Application;

public interface IOKRStatsService
{
    Task<(int ActiveNow, int ActiveLastMonth)> GetActiveOKRSessionStatsByOrganizationIdAsync(Guid organizationId);
}
