using Bogus;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NXM.Tensai.Back.OKR.API;
using NXM.Tensai.Back.OKR.Application;
using NXM.Tensai.Back.OKR.Application.Common.Models;
using NXM.Tensai.Back.OKR.Application.Features.Teams.Queries;
using NXM.Tensai.Back.OKR.Application.Features.OKRSessions.Queries;
using NXM.Tensai.Back.OKR.Domain.Enums;
using Xunit;

namespace NXM.Tensai.Back.OKR.API.UnitTests.Controllers;

public class DashboardStatsControllerTests
{
    private readonly Mock<IOKRStatsService> _mockOkrStatsService;
    private readonly Mock<ICollaboratorPerformanceService> _mockCollaboratorPerformanceService;
    private readonly Mock<IEmployeeGrowthStatsService> _mockEmployeeGrowthStatsService;
    private readonly Mock<ITeamPerformanceService> _mockTeamPerformanceService;
    private readonly Mock<ICollaboratorTaskStatusStatsService> _mockCollaboratorTaskStatusStatsService;
    private readonly Mock<IActiveTeamsService> _mockActiveTeamsService;
    private readonly Mock<IManagerSessionStatsService> _mockManagerSessionStatsService;
    private readonly Mock<ICollaboratorMonthlyPerformanceService> _mockCollaboratorMonthlyPerformanceService;
    private readonly Mock<IGlobalStatsService> _mockGlobalStatsService;
    private readonly Mock<ILogger<DashboardStatsController>> _mockLogger;
    private readonly Mock<ICollaboratorTaskDetailsService> _mockCollaboratorTaskDetailsService;
    private readonly Mock<ISubscriptionAnalyticsService> _mockSubscriptionAnalyticsService;
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<IManagerObjectiveService> _mockManagerObjectiveService;
    private readonly DashboardStatsController _controller;
    private readonly Faker _faker;

    public DashboardStatsControllerTests()
    {
        _mockOkrStatsService = new Mock<IOKRStatsService>();
        _mockCollaboratorPerformanceService = new Mock<ICollaboratorPerformanceService>();
        _mockEmployeeGrowthStatsService = new Mock<IEmployeeGrowthStatsService>();
        _mockTeamPerformanceService = new Mock<ITeamPerformanceService>();
        _mockCollaboratorTaskStatusStatsService = new Mock<ICollaboratorTaskStatusStatsService>();
        _mockActiveTeamsService = new Mock<IActiveTeamsService>();
        _mockManagerSessionStatsService = new Mock<IManagerSessionStatsService>();
        _mockCollaboratorMonthlyPerformanceService = new Mock<ICollaboratorMonthlyPerformanceService>();
        _mockGlobalStatsService = new Mock<IGlobalStatsService>();
        _mockLogger = new Mock<ILogger<DashboardStatsController>>();
        _mockCollaboratorTaskDetailsService = new Mock<ICollaboratorTaskDetailsService>();
        _mockSubscriptionAnalyticsService = new Mock<ISubscriptionAnalyticsService>();
        _mockMediator = new Mock<IMediator>();
        _mockManagerObjectiveService = new Mock<IManagerObjectiveService>();

        _controller = new DashboardStatsController(
            _mockOkrStatsService.Object,
            _mockCollaboratorPerformanceService.Object,
            _mockEmployeeGrowthStatsService.Object,
            _mockTeamPerformanceService.Object,
            _mockCollaboratorTaskStatusStatsService.Object,
            _mockActiveTeamsService.Object,
            _mockManagerSessionStatsService.Object,
            _mockCollaboratorMonthlyPerformanceService.Object,
            _mockGlobalStatsService.Object,
            _mockCollaboratorTaskDetailsService.Object,
            _mockSubscriptionAnalyticsService.Object,
            _mockMediator.Object,
            _mockLogger.Object,
            _mockManagerObjectiveService.Object);

        _faker = new Faker();
    }

    #region GetObjectivesByManagerId Tests    
    [Fact]
    public async Task GetObjectivesByManagerId_WithValidManagerId_ReturnsOkWithObjectives()
    {
        // Arrange
        var managerId = _faker.Random.Guid();
        var objectives = new List<ObjectiveDto>
        {
            new ObjectiveDto 
            { 
                Id = _faker.Random.Guid(), 
                Title = _faker.Lorem.Sentence(),
                Description = _faker.Lorem.Paragraph(),
                StartedDate = _faker.Date.Past(),
                EndDate = _faker.Date.Future(),                Status = Status.InProgress,
                Priority = Priority.High,
                Progress = _faker.Random.Int(0, 100),
                UserId = _faker.Random.Guid(),
                OKRSessionId = _faker.Random.Guid(),
                ResponsibleTeamId = _faker.Random.Guid(),
                CreatedDate = _faker.Date.Past(),
                ModifiedDate = _faker.Date.Recent()
            },
            new ObjectiveDto 
            { 
                Id = _faker.Random.Guid(), 
                Title = _faker.Lorem.Sentence(),
                Description = _faker.Lorem.Paragraph(),
                StartedDate = _faker.Date.Past(),
                EndDate = _faker.Date.Future(),                Status = Status.Completed,
                Priority = Priority.Medium,
                Progress = _faker.Random.Int(0, 100),
                UserId = _faker.Random.Guid(),
                OKRSessionId = _faker.Random.Guid(),
                ResponsibleTeamId = _faker.Random.Guid(),
                CreatedDate = _faker.Date.Past(),
                ModifiedDate = _faker.Date.Recent()
            }
        };

        _mockManagerObjectiveService
            .Setup(x => x.GetObjectivesByManagerIdAsync(managerId))
            .ReturnsAsync(objectives);

        // Act
        var result = await _controller.GetObjectivesByManagerId(managerId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(objectives);

        _mockManagerObjectiveService.Verify(
            x => x.GetObjectivesByManagerIdAsync(managerId),
            Times.Once);
    }

    [Fact]
    public async Task GetObjectivesByManagerId_WhenServiceThrows_PropagatesException()
    {
        // Arrange
        var managerId = _faker.Random.Guid();
        var expectedException = new InvalidOperationException("Test exception");

        _mockManagerObjectiveService
            .Setup(x => x.GetObjectivesByManagerIdAsync(managerId))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.GetObjectivesByManagerId(managerId));

        exception.Should().Be(expectedException);
    }

    #endregion

    #region GetActiveOKRs Tests

    [Fact]
    public async Task GetActiveOKRs_WithValidOrganizationId_ReturnsOkWithStats()
    {
        // Arrange
        var organizationId = _faker.Random.Guid();
        var activeNow = _faker.Random.Int(1, 100);
        var activeLastMonth = _faker.Random.Int(1, 100);

        _mockOkrStatsService
            .Setup(x => x.GetActiveOKRSessionStatsByOrganizationIdAsync(organizationId))
            .ReturnsAsync((activeNow, activeLastMonth));

        // Act
        var result = await _controller.GetActiveOKRs(organizationId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var responseValue = okResult!.Value;
        
        responseValue.Should().BeEquivalentTo(new 
        { 
            ActiveOKRSessionCount = activeNow, 
            ActiveOKRSessionCountLastMonth = activeLastMonth 
        });

        _mockOkrStatsService.Verify(
            x => x.GetActiveOKRSessionStatsByOrganizationIdAsync(organizationId),
            Times.Once);
    }

    [Fact]
    public async Task GetActiveOKRs_WhenServiceThrows_PropagatesException()
    {
        // Arrange
        var organizationId = _faker.Random.Guid();
        var expectedException = new InvalidOperationException("Test exception");

        _mockOkrStatsService
            .Setup(x => x.GetActiveOKRSessionStatsByOrganizationIdAsync(organizationId))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.GetActiveOKRs(organizationId));

        exception.Should().Be(expectedException);
    }

    #endregion

    #region GetCollaboratorPerformance Tests

    [Fact]
    public async Task GetCollaboratorPerformance_WithValidOrganizationId_ReturnsOkWithPerformance()
    {        // Arrange
        var organizationId = _faker.Random.Guid();
        var performanceData = new List<CollaboratorPerformanceRangeDto>
        {
            new CollaboratorPerformanceRangeDto 
            { 
                CollaboratorId = _faker.Random.Guid(), 
                PerformanceAllTime = _faker.Random.Int(0, 100),
                PerformanceLast30Days = _faker.Random.Int(0, 100),
                PerformanceLast3Months = _faker.Random.Int(0, 100)
            },
            new CollaboratorPerformanceRangeDto 
            { 
                CollaboratorId = _faker.Random.Guid(), 
                PerformanceAllTime = _faker.Random.Int(0, 100),
                PerformanceLast30Days = _faker.Random.Int(0, 100),
                PerformanceLast3Months = _faker.Random.Int(0, 100)
            }
        };

        _mockCollaboratorPerformanceService
            .Setup(x => x.GetCollaboratorPerformanceListWithRangesAsync(organizationId))
            .ReturnsAsync(performanceData);

        // Act
        var result = await _controller.GetCollaboratorPerformance(organizationId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(performanceData);

        _mockCollaboratorPerformanceService.Verify(
            x => x.GetCollaboratorPerformanceListWithRangesAsync(organizationId),
            Times.Once);
    }

    [Fact]
    public async Task GetCollaboratorPerformance_WhenServiceThrows_PropagatesException()
    {
        // Arrange
        var organizationId = _faker.Random.Guid();
        var expectedException = new InvalidOperationException("Test exception");

        _mockCollaboratorPerformanceService
            .Setup(x => x.GetCollaboratorPerformanceListWithRangesAsync(organizationId))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.GetCollaboratorPerformance(organizationId));

        exception.Should().Be(expectedException);
    }

    #endregion

    #region GetEmployeeGrowthStats Tests    
    [Fact]
    public async Task GetEmployeeGrowthStats_WithValidOrganizationId_ReturnsOkWithStats()
    {
        // Arrange
        var organizationId = _faker.Random.Guid();
        var growthStats = new EmployeeGrowthStatsDto
        {
            Yearly = new List<YearlyGrowthDto>
            {
                new YearlyGrowthDto { Year = 2023, Count = 50 },
                new YearlyGrowthDto { Year = 2024, Count = 100 }
            },
            Monthly = new List<MonthlyGrowthDto>
            {
                new MonthlyGrowthDto { Year = 2024, Month = 11, Count = 95 },
                new MonthlyGrowthDto { Year = 2024, Month = 12, Count = 100 }
            }
        };

        _mockEmployeeGrowthStatsService
            .Setup(x => x.GetEmployeeGrowthStatsAsync(organizationId))
            .ReturnsAsync(growthStats);

        // Act
        var result = await _controller.GetEmployeeGrowthStats(organizationId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(growthStats);

        _mockEmployeeGrowthStatsService.Verify(
            x => x.GetEmployeeGrowthStatsAsync(organizationId),
            Times.Once);
    }

    [Fact]
    public async Task GetEmployeeGrowthStats_WhenServiceThrows_PropagatesException()
    {
        // Arrange
        var organizationId = _faker.Random.Guid();
        var expectedException = new InvalidOperationException("Test exception");

        _mockEmployeeGrowthStatsService
            .Setup(x => x.GetEmployeeGrowthStatsAsync(organizationId))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.GetEmployeeGrowthStats(organizationId));

        exception.Should().Be(expectedException);
    }

    #endregion

    #region GetTeamPerformanceBarChart Tests    
    [Fact]
    public async Task GetTeamPerformanceBarChart_WithValidOrganizationId_ReturnsOkWithChart()
    {
        // Arrange
        var organizationId = _faker.Random.Guid();
        var chartData = new List<TeamPerformanceBarDto>
        {
            new TeamPerformanceBarDto 
            { 
                TeamId = _faker.Random.Guid(), 
                TeamName = _faker.Company.CompanyName(),
                PerformanceAllTime = _faker.Random.Int(0, 100),
                PerformanceLast30Days = _faker.Random.Int(0, 100),
                PerformanceLast3Months = _faker.Random.Int(0, 100)
            },
            new TeamPerformanceBarDto 
            { 
                TeamId = _faker.Random.Guid(), 
                TeamName = _faker.Company.CompanyName(),
                PerformanceAllTime = _faker.Random.Int(0, 100),
                PerformanceLast30Days = _faker.Random.Int(0, 100),
                PerformanceLast3Months = _faker.Random.Int(0, 100)
            }
        };

        _mockTeamPerformanceService
            .Setup(x => x.GetTeamPerformanceBarChartAsync(organizationId))
            .ReturnsAsync(chartData);

        // Act
        var result = await _controller.GetTeamPerformanceBarChart(organizationId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(chartData);

        _mockTeamPerformanceService.Verify(
            x => x.GetTeamPerformanceBarChartAsync(organizationId),
            Times.Once);
    }

    [Fact]
    public async Task GetTeamPerformanceBarChart_WhenServiceThrows_PropagatesException()
    {
        // Arrange
        var organizationId = _faker.Random.Guid();
        var expectedException = new InvalidOperationException("Test exception");

        _mockTeamPerformanceService
            .Setup(x => x.GetTeamPerformanceBarChartAsync(organizationId))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.GetTeamPerformanceBarChart(organizationId));

        exception.Should().Be(expectedException);
    }

    #endregion

    #region GetCollaboratorTaskStatusStats Tests   
    [Fact]
    public async Task GetCollaboratorTaskStatusStats_WithValidOrganizationId_ReturnsOkWithStats()
    {
        // Arrange
        var organizationId = _faker.Random.Guid();
        var taskStats = new List<CollaboratorTaskStatusStatsDto>
        {
            new CollaboratorTaskStatusStatsDto
            {
                CollaboratorId = _faker.Random.Guid(),
                NotStarted = _faker.Random.Int(0, 10),
                InProgress = _faker.Random.Int(0, 10),
                Completed = _faker.Random.Int(0, 10),
                Overdue = _faker.Random.Int(0, 10)
            },
            new CollaboratorTaskStatusStatsDto
            {
                CollaboratorId = _faker.Random.Guid(),
                NotStarted = _faker.Random.Int(0, 10),
                InProgress = _faker.Random.Int(0, 10),
                Completed = _faker.Random.Int(0, 10),
                Overdue = _faker.Random.Int(0, 10)
            }
        };

        _mockCollaboratorTaskStatusStatsService
            .Setup(x => x.GetCollaboratorTaskStatusStatsAsync(organizationId))
            .ReturnsAsync(taskStats);

        // Act
        var result = await _controller.GetCollaboratorTaskStatusStats(organizationId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(taskStats);

        _mockCollaboratorTaskStatusStatsService.Verify(
            x => x.GetCollaboratorTaskStatusStatsAsync(organizationId),
            Times.Once);
    }

    [Fact]
    public async Task GetCollaboratorTaskStatusStats_WhenServiceThrows_PropagatesException()
    {
        // Arrange
        var organizationId = _faker.Random.Guid();
        var expectedException = new InvalidOperationException("Test exception");

        _mockCollaboratorTaskStatusStatsService
            .Setup(x => x.GetCollaboratorTaskStatusStatsAsync(organizationId))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.GetCollaboratorTaskStatusStats(organizationId));

        exception.Should().Be(expectedException);
    }

    #endregion

    #region GetActiveTeams Tests

    [Fact]
    public async Task GetActiveTeams_WithValidOrganizationId_ReturnsOkWithStats()
    {
        // Arrange
        var organizationId = _faker.Random.Guid();
        var activeNow = _faker.Random.Int(1, 100);
        var activeLastMonth = _faker.Random.Int(1, 100);

        _mockActiveTeamsService
            .Setup(x => x.GetActiveTeamsStatsByOrganizationIdAsync(organizationId))
            .ReturnsAsync((activeNow, activeLastMonth));

        // Act
        var result = await _controller.GetActiveTeams(organizationId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var responseValue = okResult!.Value;
        
        responseValue.Should().BeEquivalentTo(new 
        { 
            ActiveTeamsCount = activeNow, 
            ActiveTeamsCountLastMonth = activeLastMonth 
        });

        _mockActiveTeamsService.Verify(
            x => x.GetActiveTeamsStatsByOrganizationIdAsync(organizationId),
            Times.Once);
    }

    [Fact]
    public async Task GetActiveTeams_WhenServiceThrows_PropagatesException()
    {
        // Arrange
        var organizationId = _faker.Random.Guid();
        var expectedException = new InvalidOperationException("Test exception");

        _mockActiveTeamsService
            .Setup(x => x.GetActiveTeamsStatsByOrganizationIdAsync(organizationId))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.GetActiveTeams(organizationId));

        exception.Should().Be(expectedException);
    }

    #endregion

    #region GetManagerSessionStats Tests   
    [Fact]
    public async Task GetManagerSessionStats_WithValidManagerId_ReturnsOkWithStats()
    {
        // Arrange
        var managerId = _faker.Random.Guid();
        var sessionStats = new ManagerSessionStatsDto 
        { 
            ActiveSessions = _faker.Random.Int(0, 50), 
            DelayedSessions = _faker.Random.Int(0, 20) 
        };

        _mockManagerSessionStatsService
            .Setup(x => x.GetManagerSessionStatsAsync(managerId))
            .ReturnsAsync(sessionStats);

        // Act
        var result = await _controller.GetManagerSessionStats(managerId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(sessionStats);

        _mockManagerSessionStatsService.Verify(
            x => x.GetManagerSessionStatsAsync(managerId),
            Times.Once);
    }

    [Fact]
    public async Task GetManagerSessionStats_WhenServiceThrows_PropagatesException()
    {
        // Arrange
        var managerId = _faker.Random.Guid();
        var expectedException = new InvalidOperationException("Test exception");

        _mockManagerSessionStatsService
            .Setup(x => x.GetManagerSessionStatsAsync(managerId))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.GetManagerSessionStats(managerId));

        exception.Should().Be(expectedException);
    }

    #endregion

    #region GetCollaboratorMonthlyPerformance Tests    
    [Fact]
    public async Task GetCollaboratorMonthlyPerformance_WithValidIds_ReturnsOkWithPerformance()
    {
        // Arrange
        var organizationId = _faker.Random.Guid();
        var collaboratorId = _faker.Random.Guid();
        var monthlyPerformance = new List<CollaboratorMonthlyPerformanceDto>
        {
            new CollaboratorMonthlyPerformanceDto 
            { 
                Year = 2024, 
                Month = 11, 
                Performance = _faker.Random.Int(0, 100) 
            },
            new CollaboratorMonthlyPerformanceDto 
            { 
                Year = 2024, 
                Month = 12, 
                Performance = _faker.Random.Int(0, 100) 
            }
        };

        _mockCollaboratorMonthlyPerformanceService
            .Setup(x => x.GetCollaboratorMonthlyPerformanceAsync(organizationId, collaboratorId))
            .ReturnsAsync(monthlyPerformance);

        // Act
        var result = await _controller.GetCollaboratorMonthlyPerformance(organizationId, collaboratorId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(monthlyPerformance);

        _mockCollaboratorMonthlyPerformanceService.Verify(
            x => x.GetCollaboratorMonthlyPerformanceAsync(organizationId, collaboratorId),
            Times.Once);
    }

    [Fact]
    public async Task GetCollaboratorMonthlyPerformance_WhenServiceThrows_PropagatesException()
    {
        // Arrange
        var organizationId = _faker.Random.Guid();
        var collaboratorId = _faker.Random.Guid();
        var expectedException = new InvalidOperationException("Test exception");

        _mockCollaboratorMonthlyPerformanceService
            .Setup(x => x.GetCollaboratorMonthlyPerformanceAsync(organizationId, collaboratorId))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.GetCollaboratorMonthlyPerformance(organizationId, collaboratorId));

        exception.Should().Be(expectedException);
    }

    #endregion

    #region GetGlobalOrgOkrStats Tests    
    [Fact]
    public async Task GetGlobalOrgOkrStats_ReturnsOkWithStats()
    {
        // Arrange
        var globalStats = new GlobalOrgOkrStatsDto 
        { 
            OrganizationCount = _faker.Random.Int(1, 1000), 
            OKRSessionCount = _faker.Random.Int(1, 10000) 
        };

        _mockGlobalStatsService
            .Setup(x => x.GetGlobalOrgOkrStatsAsync())
            .ReturnsAsync(globalStats);

        // Act
        var result = await _controller.GetGlobalOrgOkrStats();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(globalStats);

        _mockGlobalStatsService.Verify(
            x => x.GetGlobalOrgOkrStatsAsync(),
            Times.Once);
    }

    [Fact]
    public async Task GetGlobalOrgOkrStats_WhenServiceThrows_PropagatesException()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Test exception");

        _mockGlobalStatsService
            .Setup(x => x.GetGlobalOrgOkrStatsAsync())
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.GetGlobalOrgOkrStats());

        exception.Should().Be(expectedException);
    }

    #endregion

    #region GetUserGrowthStats Tests    
    [Fact]
    public async Task GetUserGrowthStats_ReturnsOkWithStats()
    {
        // Arrange
        var userGrowthStats = new UserGrowthStatsDto
        {
            Yearly = new List<YearlyGrowthDto>
            {
                new YearlyGrowthDto { Year = 2023, Count = 500 },
                new YearlyGrowthDto { Year = 2024, Count = 750 }
            },
            Monthly = new List<MonthlyGrowthDto>
            {
                new MonthlyGrowthDto { Year = 2024, Month = 11, Count = 720 },
                new MonthlyGrowthDto { Year = 2024, Month = 12, Count = 750 }
            }
        };

        _mockGlobalStatsService
            .Setup(x => x.GetUserGrowthStatsAsync())
            .ReturnsAsync(userGrowthStats);

        // Act
        var result = await _controller.GetUserGrowthStats();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(userGrowthStats);

        _mockGlobalStatsService.Verify(
            x => x.GetUserGrowthStatsAsync(),
            Times.Once);
    }

    [Fact]
    public async Task GetUserGrowthStats_WhenServiceThrows_PropagatesException()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Test exception");

        _mockGlobalStatsService
            .Setup(x => x.GetUserGrowthStatsAsync())
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.GetUserGrowthStats());

        exception.Should().Be(expectedException);
    }

    #endregion

    #region GetUserRolesCount Tests    
    [Fact]
    public async Task GetUserRolesCount_ReturnsOkWithCounts()
    {
        // Arrange
        var rolesCounts = new UserRolesCountDto 
        { 
            OrganizationAdmins = _faker.Random.Int(1, 100), 
            TeamManagers = _faker.Random.Int(1, 500), 
            Collaborators = _faker.Random.Int(1, 1000) 
        };

        _mockGlobalStatsService
            .Setup(x => x.GetUserRolesCountAsync())
            .ReturnsAsync(rolesCounts);

        // Act
        var result = await _controller.GetUserRolesCount();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(rolesCounts);

        _mockGlobalStatsService.Verify(
            x => x.GetUserRolesCountAsync(),
            Times.Once);
    }

    [Fact]
    public async Task GetUserRolesCount_WhenServiceThrows_PropagatesException()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Test exception");

        _mockGlobalStatsService
            .Setup(x => x.GetUserRolesCountAsync())
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.GetUserRolesCount());

        exception.Should().Be(expectedException);
    }

    #endregion

    #region GetPaidOrgCount Tests    
    [Fact]
    public async Task GetPaidOrgCount_ReturnsOkWithCount()
    {
        // Arrange
        var paidOrgCount = new OrgPaidPlanCountDto { PaidOrganizations = _faker.Random.Int(1, 1000) };

        _mockGlobalStatsService
            .Setup(x => x.GetOrgPaidPlanCountAsync())
            .ReturnsAsync(paidOrgCount);

        // Act
        var result = await _controller.GetPaidOrgCount();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(paidOrgCount);

        _mockGlobalStatsService.Verify(
            x => x.GetOrgPaidPlanCountAsync(),
            Times.Once);
    }

    [Fact]
    public async Task GetPaidOrgCount_WhenServiceThrows_PropagatesException()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Test exception");

        _mockGlobalStatsService
            .Setup(x => x.GetOrgPaidPlanCountAsync())
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.GetPaidOrgCount());

        exception.Should().Be(expectedException);
    }

    #endregion

    #region GetCollaboratorTaskDetails Tests    
    [Fact]
    public async Task GetCollaboratorTaskDetails_WithValidCollaboratorId_ReturnsOkWithDetails()
    {
        // Arrange
        var collaboratorId = _faker.Random.Guid();
        var taskDetails = new CollaboratorTaskDetailsDto
        {
            RecentCompletedTasks = new List<KeyResultTaskDto>
            {
                new KeyResultTaskDto 
                { 
                    Id = _faker.Random.Guid(), 
                    Title = _faker.Lorem.Sentence(), 
                    Status = Status.Completed,
                    KeyResultId = _faker.Random.Guid(),
                    UserId = _faker.Random.Guid(),
                    CollaboratorId = collaboratorId,
                    StartedDate = _faker.Date.Past(),
                    EndDate = _faker.Date.Future(),
                    Progress = _faker.Random.Int(0, 100),
                    Priority = Priority.High
                }
            },
            InProgressTasks = new List<KeyResultTaskDto>
            {
                new KeyResultTaskDto 
                { 
                    Id = _faker.Random.Guid(), 
                    Title = _faker.Lorem.Sentence(), 
                    Status = Status.InProgress,
                    KeyResultId = _faker.Random.Guid(),
                    UserId = _faker.Random.Guid(),
                    CollaboratorId = collaboratorId,
                    StartedDate = _faker.Date.Past(),
                    EndDate = _faker.Date.Future(),
                    Progress = _faker.Random.Int(0, 100),
                    Priority = Priority.Medium
                }
            },
            OverdueTasks = new List<KeyResultTaskDto>
            {
                new KeyResultTaskDto 
                { 
                    Id = _faker.Random.Guid(), 
                    Title = _faker.Lorem.Sentence(), 
                    Status = Status.Overdue,
                    KeyResultId = _faker.Random.Guid(),
                    UserId = _faker.Random.Guid(),
                    CollaboratorId = collaboratorId,
                    StartedDate = _faker.Date.Past(),
                    EndDate = _faker.Date.Past(),
                    Progress = _faker.Random.Int(0, 80),
                    Priority = Priority.Urgent
                }
            }
        };

        _mockCollaboratorTaskDetailsService
            .Setup(x => x.GetTaskDetailsAsync(collaboratorId))
            .ReturnsAsync(taskDetails);

        // Act
        var result = await _controller.GetCollaboratorTaskDetails(collaboratorId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(taskDetails);

        _mockCollaboratorTaskDetailsService.Verify(
            x => x.GetTaskDetailsAsync(collaboratorId),
            Times.Once);
    }

    [Fact]
    public async Task GetCollaboratorTaskDetails_WhenServiceThrows_PropagatesException()
    {
        // Arrange
        var collaboratorId = _faker.Random.Guid();
        var expectedException = new InvalidOperationException("Test exception");

        _mockCollaboratorTaskDetailsService
            .Setup(x => x.GetTaskDetailsAsync(collaboratorId))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.GetCollaboratorTaskDetails(collaboratorId));

        exception.Should().Be(expectedException);
    }

    #endregion

    #region GetSubscriptionRevenueAnalytics Tests    
    [Fact]
    public async Task GetSubscriptionRevenueAnalytics_ReturnsOkWithAnalytics()
    {
        // Arrange
        var revenueAnalytics = new SubscriptionRevenueAnalyticsDto 
        { 
            TotalRevenue = _faker.Random.Decimal(1000, 100000), 
            ConversionRate = _faker.Random.Double(0.1, 0.9),
            RevenueByPlan = new List<RevenueByPlanDto>
            {
                new RevenueByPlanDto { PlanType = "Basic", Revenue = _faker.Random.Decimal(500, 5000) },
                new RevenueByPlanDto { PlanType = "Professional", Revenue = _faker.Random.Decimal(1000, 10000) },
                new RevenueByPlanDto { PlanType = "Enterprise", Revenue = _faker.Random.Decimal(5000, 50000) }
            }
        };

        _mockSubscriptionAnalyticsService
            .Setup(x => x.GetRevenueAnalyticsAsync())
            .ReturnsAsync(revenueAnalytics);

        // Act
        var result = await _controller.GetSubscriptionRevenueAnalytics();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(revenueAnalytics);

        _mockSubscriptionAnalyticsService.Verify(
            x => x.GetRevenueAnalyticsAsync(),
            Times.Once);
    }

    [Fact]
    public async Task GetSubscriptionRevenueAnalytics_WhenServiceThrows_PropagatesException()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Test exception");

        _mockSubscriptionAnalyticsService
            .Setup(x => x.GetRevenueAnalyticsAsync())
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.GetSubscriptionRevenueAnalytics());

        exception.Should().Be(expectedException);
    }

    #endregion

    #region GetTeamsWithCollaborators Tests   
    [Fact]
    public async Task GetTeamsWithCollaborators_WithValidOrganizationId_ReturnsOkWithTeams()
    {
        // Arrange
        var organizationId = _faker.Random.Guid();
        var teamsWithCollaborators = new TeamsWithCollaboratorsResultDto
        {
            Teams = new List<TeamWithCollaboratorsDto>
            {
                new TeamWithCollaboratorsDto 
                { 
                    Id = _faker.Random.Guid(), 
                    Name = _faker.Company.CompanyName(), 
                    Description = _faker.Lorem.Sentence(),
                    TeamManagerId = _faker.Random.Guid(),
                    OrganizationId = organizationId,
                    Collaborators = new List<UserWithRoleDto>
                    {
                        new UserWithRoleDto 
                        { 
                            Id = _faker.Random.Guid(), 
                            FirstName = _faker.Name.FirstName(),
                            LastName = _faker.Name.LastName(),
                            Email = _faker.Internet.Email(),
                            Role = "Collaborator"
                        }
                    }
                },
                new TeamWithCollaboratorsDto 
                { 
                    Id = _faker.Random.Guid(), 
                    Name = _faker.Company.CompanyName(), 
                    Description = _faker.Lorem.Sentence(),
                    TeamManagerId = _faker.Random.Guid(),
                    OrganizationId = organizationId,
                    Collaborators = new List<UserWithRoleDto>
                    {
                        new UserWithRoleDto 
                        { 
                            Id = _faker.Random.Guid(), 
                            FirstName = _faker.Name.FirstName(),
                            LastName = _faker.Name.LastName(),
                            Email = _faker.Internet.Email(),
                            Role = "Collaborator"
                        }
                    }
                }
            }
        };

        _mockMediator
            .Setup(x => x.Send(It.Is<GetTeamsWithCollaboratorsQuery>(q => q.OrganizationId == organizationId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(teamsWithCollaborators);

        // Act
        var result = await _controller.GetTeamsWithCollaborators(organizationId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(teamsWithCollaborators);

        _mockMediator.Verify(
            x => x.Send(It.Is<GetTeamsWithCollaboratorsQuery>(q => q.OrganizationId == organizationId), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetTeamsWithCollaborators_WhenMediatorThrows_PropagatesException()
    {
        // Arrange
        var organizationId = _faker.Random.Guid();
        var expectedException = new InvalidOperationException("Test exception");

        _mockMediator
            .Setup(x => x.Send(It.Is<GetTeamsWithCollaboratorsQuery>(q => q.OrganizationId == organizationId), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.GetTeamsWithCollaborators(organizationId));

        exception.Should().Be(expectedException);
    }

    #endregion

    #region GetOngoingOKRTasks Tests    
    [Fact]
    public async Task GetOngoingOKRTasks_WithValidOrganizationId_ReturnsOkWithTasks()
    {
        // Arrange
        var organizationId = _faker.Random.Guid();
        var ongoingTasks = new OngoingOKRTasksResultDto
        {
            OKRSessions = new List<OngoingOKRSessionDto>
            {
                new OngoingOKRSessionDto
                {
                    Id = _faker.Random.Guid(),
                    Title = _faker.Lorem.Sentence(),
                    Description = _faker.Lorem.Paragraph(),
                    StartedDate = _faker.Date.Past(),
                    EndDate = _faker.Date.Future(),
                    Priority = Priority.High,
                    Status = Status.InProgress,
                    Progress = _faker.Random.Int(0, 80),
                    Objectives = new List<OngoingObjectiveDto>
                    {
                        new OngoingObjectiveDto
                        {
                            Id = _faker.Random.Guid(),
                            Title = _faker.Lorem.Sentence(),
                            Description = _faker.Lorem.Paragraph(),
                            TeamId = _faker.Random.Guid(),
                            StartedDate = _faker.Date.Past(),
                            EndDate = _faker.Date.Future(),
                            Priority = Priority.Medium,
                            Status = Status.InProgress,
                            Progress = _faker.Random.Int(0, 70),
                            KeyResults = new List<OngoingKeyResultDto>
                            {
                                new OngoingKeyResultDto
                                {
                                    Id = _faker.Random.Guid(),
                                    Title = _faker.Lorem.Sentence(),
                                    Description = _faker.Lorem.Paragraph(),
                                    StartedDate = _faker.Date.Past(),
                                    EndDate = _faker.Date.Future(),
                                    Priority = Priority.High,
                                    Status = Status.InProgress,
                                    Progress = _faker.Random.Int(0, 60),
                                    Tasks = new List<OngoingTaskDto>
                                    {
                                        new OngoingTaskDto
                                        {
                                            Id = _faker.Random.Guid(),
                                            Title = _faker.Lorem.Sentence(),
                                            Description = _faker.Lorem.Paragraph(),
                                            Priority = Priority.Medium,
                                            Status = Status.InProgress,
                                            Progress = _faker.Random.Int(0, 50),
                                            StartedDate = _faker.Date.Past(),
                                            EndDate = _faker.Date.Future(),
                                            CollaboratorId = _faker.Random.Guid()
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        _mockMediator
            .Setup(x => x.Send(It.Is<GetOngoingOKRTasksQuery>(q => q.OrganizationId == organizationId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ongoingTasks);

        // Act
        var result = await _controller.GetOngoingOKRTasks(organizationId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(ongoingTasks);

        _mockMediator.Verify(
            x => x.Send(It.Is<GetOngoingOKRTasksQuery>(q => q.OrganizationId == organizationId), It.IsAny<CancellationToken>()),
            Times.Once);
    }
            
    

    [Fact]
    public async Task GetOngoingOKRTasks_WhenMediatorThrows_PropagatesException()
    {
        // Arrange
        var organizationId = _faker.Random.Guid();
        var expectedException = new InvalidOperationException("Test exception");

        _mockMediator
            .Setup(x => x.Send(It.Is<GetOngoingOKRTasksQuery>(q => q.OrganizationId == organizationId), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.GetOngoingOKRTasks(organizationId));

        exception.Should().Be(expectedException);
    }

    #endregion
}
