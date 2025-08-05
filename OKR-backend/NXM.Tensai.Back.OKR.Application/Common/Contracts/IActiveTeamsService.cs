namespace NXM.Tensai.Back.OKR.Application;

public interface IActiveTeamsService
{
    Task<(int ActiveNow, int ActiveLastMonth)> GetActiveTeamsStatsByOrganizationIdAsync(Guid organizationId);
}
