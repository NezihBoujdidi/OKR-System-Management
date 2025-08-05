using NXM.Tensai.Back.OKR.Application;
// in Application/Features/Admin/Queries/GetDashboardStatsQuery.cs
public class GetDashboardStatsQuery : IRequest<SuperAdminDashboardDto> { }

public class GetDashboardStatsQueryHandler 
  : IRequestHandler<GetDashboardStatsQuery, SuperAdminDashboardDto>
{
  private readonly ISubscriptionService _subscriptionService;

  public GetDashboardStatsQueryHandler(ISubscriptionService s) 
    => _subscriptionService = s;

  public async Task<SuperAdminDashboardDto> Handle(
    GetDashboardStatsQuery q, CancellationToken _) 
  {
    return await _subscriptionService.GetSuperAdminStatsAsync();
  }
}