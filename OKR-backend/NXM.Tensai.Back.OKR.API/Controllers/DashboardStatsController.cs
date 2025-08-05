using Microsoft.AspNetCore.Mvc;
using MediatR;
using NXM.Tensai.Back.OKR.Application;
using NXM.Tensai.Back.OKR.Application.Features.Teams.Queries;
using NXM.Tensai.Back.OKR.Application.Features.OKRSessions.Queries;

namespace NXM.Tensai.Back.OKR.API;

[Route("api/dashboard-stats")]
[ApiController]
public class DashboardStatsController : ControllerBase
{
    private readonly IOKRStatsService _okrStatsService;
    private readonly ICollaboratorPerformanceService _collaboratorPerformanceService;
    private readonly IEmployeeGrowthStatsService _employeeGrowthStatsService;
    private readonly ITeamPerformanceService _teamPerformanceService;
    private readonly ICollaboratorTaskStatusStatsService _collaboratorTaskStatusStatsService;
    private readonly IActiveTeamsService _activeTeamsService;
    private readonly IManagerSessionStatsService _managerSessionStatsService;
    private readonly ICollaboratorMonthlyPerformanceService _collaboratorMonthlyPerformanceService;
    private readonly IGlobalStatsService _globalStatsService;
    private readonly ILogger<DashboardStatsController> _logger;
    private readonly ICollaboratorTaskDetailsService _collaboratorTaskDetailsService;
    private readonly ISubscriptionAnalyticsService _subscriptionAnalyticsService;
    private readonly IMediator _mediator;
    private readonly IManagerObjectiveService _managerObjectiveService;

    public DashboardStatsController(
        IOKRStatsService okrStatsService,
        ICollaboratorPerformanceService collaboratorPerformanceService,
        IEmployeeGrowthStatsService employeeGrowthStatsService,
        ITeamPerformanceService teamPerformanceService,
        ICollaboratorTaskStatusStatsService collaboratorTaskStatusStatsService,
        IActiveTeamsService activeTeamsService,
        IManagerSessionStatsService managerSessionStatsService,
        ICollaboratorMonthlyPerformanceService collaboratorMonthlyPerformanceService,
        IGlobalStatsService globalStatsService,
        ICollaboratorTaskDetailsService collaboratorTaskDetailsService,
        ISubscriptionAnalyticsService subscriptionAnalyticsService,
        IMediator mediator,
        ILogger<DashboardStatsController> logger, 
        IManagerObjectiveService managerObjectiveService
        /* removed IManagerObjectiveService managerObjectiveService */)
    {
        _okrStatsService = okrStatsService;
        _collaboratorPerformanceService = collaboratorPerformanceService;
        _employeeGrowthStatsService = employeeGrowthStatsService;
        _teamPerformanceService = teamPerformanceService;
        _collaboratorTaskStatusStatsService = collaboratorTaskStatusStatsService;
        _activeTeamsService = activeTeamsService;
        _managerSessionStatsService = managerSessionStatsService;
        _collaboratorMonthlyPerformanceService = collaboratorMonthlyPerformanceService;
        _globalStatsService = globalStatsService;
        _collaboratorTaskDetailsService = collaboratorTaskDetailsService;
        _subscriptionAnalyticsService = subscriptionAnalyticsService;
        _mediator = mediator;
        _logger = logger;
        _managerObjectiveService = managerObjectiveService;
    }

    [HttpGet("manager-objectives/{managerId:guid}")]
    public async Task<IActionResult> GetObjectivesByManagerId(Guid managerId)
    {
        _logger.LogInformation("GetObjectivesByManagerId called for managerId: {ManagerId}", managerId);
        var objectives = await _managerObjectiveService.GetObjectivesByManagerIdAsync(managerId);
        return Ok(objectives);
    }

    [HttpGet("active-okrs/{organizationId:guid}")]
    public async Task<IActionResult> GetActiveOKRs(Guid organizationId)
    {
        _logger.LogInformation("GetActiveOKRs called for organizationId: {OrganizationId}", organizationId);
        var (activeNow, activeLastMonth) = await _okrStatsService.GetActiveOKRSessionStatsByOrganizationIdAsync(organizationId);
        return Ok(new { ActiveOKRSessionCount = activeNow, ActiveOKRSessionCountLastMonth = activeLastMonth });
    }

    [HttpGet("collaborator-performance/{organizationId:guid}")]
    public async Task<IActionResult> GetCollaboratorPerformance(Guid organizationId)
    {
        _logger.LogInformation("GetCollaboratorPerformance called for organizationId: {OrganizationId}", organizationId);
        var result = await _collaboratorPerformanceService.GetCollaboratorPerformanceListWithRangesAsync(organizationId);
        return Ok(result);
    }

    [HttpGet("employee-growth/{organizationId:guid}")]
    public async Task<IActionResult> GetEmployeeGrowthStats(Guid organizationId)
    {
        _logger.LogInformation("GetEmployeeGrowthStats called for organizationId: {OrganizationId}", organizationId);
        var result = await _employeeGrowthStatsService.GetEmployeeGrowthStatsAsync(organizationId);
        return Ok(result);
    }

    [HttpGet("team-performance/{organizationId:guid}")]
    public async Task<IActionResult> GetTeamPerformanceBarChart(Guid organizationId)
    {
        _logger.LogInformation("GetTeamPerformanceBarChart called for organizationId: {OrganizationId}", organizationId);
        var result = await _teamPerformanceService.GetTeamPerformanceBarChartAsync(organizationId);
        return Ok(result);
    }

    [HttpGet("collaborator-task-status-stats/{organizationId:guid}")]
    public async Task<IActionResult> GetCollaboratorTaskStatusStats(Guid organizationId)
    {
        _logger.LogInformation("GetCollaboratorTaskStatusStats called for organizationId: {OrganizationId}", organizationId);
        var result = await _collaboratorTaskStatusStatsService.GetCollaboratorTaskStatusStatsAsync(organizationId);
        return Ok(result);
    }

    [HttpGet("active-teams/{organizationId:guid}")]
    public async Task<IActionResult> GetActiveTeams(Guid organizationId)
    {
        _logger.LogInformation("GetActiveTeams called for organizationId: {OrganizationId}", organizationId);
        var (activeNow, activeLastMonth) = await _activeTeamsService.GetActiveTeamsStatsByOrganizationIdAsync(organizationId);
        return Ok(new { ActiveTeamsCount = activeNow, ActiveTeamsCountLastMonth = activeLastMonth });
    }

    [HttpGet("manager-session-stats/{managerId:guid}")]
    public async Task<IActionResult> GetManagerSessionStats(Guid managerId)
    {
        _logger.LogInformation("GetManagerSessionStats called for managerId: {ManagerId}", managerId);
        var stats = await _managerSessionStatsService.GetManagerSessionStatsAsync(managerId);
        return Ok(stats);
    }

    [HttpGet("collaborator-monthly-performance/{organizationId:guid}/{collaboratorId:guid}")]
    public async Task<IActionResult> GetCollaboratorMonthlyPerformance(Guid organizationId, Guid collaboratorId)
    {
        _logger.LogInformation("GetCollaboratorMonthlyPerformance called for organizationId: {OrganizationId}, collaboratorId: {CollaboratorId}", organizationId, collaboratorId);
        var result = await _collaboratorMonthlyPerformanceService.GetCollaboratorMonthlyPerformanceAsync(organizationId, collaboratorId);
        return Ok(result);
    }

    [HttpGet("global-org-okr-stats")]
    public async Task<IActionResult> GetGlobalOrgOkrStats()
    {
        var result = await _globalStatsService.GetGlobalOrgOkrStatsAsync();
        return Ok(result);
    }

    [HttpGet("user-growth-stats")]
    public async Task<IActionResult> GetUserGrowthStats()
    {
        var result = await _globalStatsService.GetUserGrowthStatsAsync();
        return Ok(result);
    }

    [HttpGet("user-roles-count")]
    public async Task<IActionResult> GetUserRolesCount()
    {
        var result = await _globalStatsService.GetUserRolesCountAsync();
        return Ok(result);
    }

    [HttpGet("paid-org-count")]
    public async Task<IActionResult> GetPaidOrgCount()
    {
        var result = await _globalStatsService.GetOrgPaidPlanCountAsync();
        return Ok(result);
    }

    [HttpGet("collaborator-task-details/{collaboratorId:guid}")]
    public async Task<IActionResult> GetCollaboratorTaskDetails(Guid collaboratorId)
    {
        _logger.LogInformation("GetCollaboratorTaskDetails called for collaboratorId: {CollaboratorId}", collaboratorId);
        var result = await _collaboratorTaskDetailsService.GetTaskDetailsAsync(collaboratorId);
        return Ok(result);
    }

    [HttpGet("subscription-revenue-analytics")]
    public async Task<IActionResult> GetSubscriptionRevenueAnalytics()
    {
        var result = await _subscriptionAnalyticsService.GetRevenueAnalyticsAsync();
        return Ok(result);
    }

    [HttpGet("teams-with-collaborators/{organizationId:guid}")]
    public async Task<IActionResult> GetTeamsWithCollaborators(Guid organizationId)
    {
        var result = await _mediator.Send(new GetTeamsWithCollaboratorsQuery(organizationId));
        return Ok(result);
    }

    [HttpGet("ongoing-okr-tasks/{organizationId:guid}")]
    public async Task<IActionResult> GetOngoingOKRTasks(Guid organizationId)
    {
        var result = await _mediator.Send(new GetOngoingOKRTasksQuery(organizationId));
        return Ok(result);
    }
}
