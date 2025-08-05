using Bogus;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NXM.Tensai.Back.OKR.Application.Common.Exceptions;
using NXM.Tensai.Back.OKR.Application;
using NXM.Tensai.Back.OKR.Domain;
using ValidationException = NXM.Tensai.Back.OKR.Application.Common.Exceptions.ValidationException;

namespace NXM.Tensai.Back.OKR.API.UnitTests.Controllers;

public class TeamsControllerTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ILogger<TeamsController>> _mockLogger;
    private readonly TeamsController _controller;
    private readonly Faker<CreateTeamCommand> _createTeamFaker;
    private readonly Faker<UpdateTeamCommand> _updateTeamFaker;
    private readonly Faker<TeamDto> _teamDtoFaker;
    private readonly Faker<SearchTeamsQuery> _searchTeamsQueryFaker;
    private readonly Faker<PaginatedListResult<TeamDto>> _paginatedTeamResultFaker;

    public TeamsControllerTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<TeamsController>>();
        _controller = new TeamsController(_mockMediator.Object, _mockLogger.Object);

        // Setup fakers for consistent test data generation
        _createTeamFaker = new Faker<CreateTeamCommand>()
            .RuleFor(c => c.Name, f => f.Company.CompanyName())
            .RuleFor(c => c.Description, f => f.Lorem.Sentence())
            .RuleFor(c => c.OrganizationId, f => f.Random.Guid())
            .RuleFor(c => c.TeamManagerId, f => f.Random.Guid());

        _updateTeamFaker = new Faker<UpdateTeamCommand>()
            .RuleFor(c => c.Name, f => f.Company.CompanyName())
            .RuleFor(c => c.Description, f => f.Lorem.Sentence())
            .RuleFor(c => c.OrganizationId, f => f.Random.Guid())
            .RuleFor(c => c.TeamManagerId, f => f.Random.Guid());        _teamDtoFaker = new Faker<TeamDto>()
            .RuleFor(t => t.Id, f => f.Random.Guid())
            .RuleFor(t => t.Name, f => f.Company.CompanyName())
            .RuleFor(t => t.Description, f => f.Lorem.Sentence())
            .RuleFor(t => t.OrganizationId, f => f.Random.Guid())
            .RuleFor(t => t.TeamManagerId, f => f.Random.Guid())
            .RuleFor(t => t.CreatedDate, f => f.Date.Past(1))
            .RuleFor(t => t.ModifiedDate, f => f.Date.Recent());

        _searchTeamsQueryFaker = new Faker<SearchTeamsQuery>()
            .RuleFor(q => q.Name, f => f.Company.CompanyName())
            .RuleFor(q => q.OrganizationId, f => f.Random.Guid())
            .RuleFor(q => q.Page, f => f.Random.Int(1, 5))
            .RuleFor(q => q.PageSize, f => f.Random.Int(5, 20));

        _paginatedTeamResultFaker = new Faker<PaginatedListResult<TeamDto>>()
            .CustomInstantiator(f =>
            {
                var teams = _teamDtoFaker.Generate(f.Random.Int(1, 5));
                return new PaginatedListResult<TeamDto>(teams, teams.Count, 1, 1);
            });
    }

    #region CreateTeam Tests

    [Fact]
    public async Task CreateTeam_WithValidCommand_Should_ReturnOkResult()
    {
        // Arrange
        var command = _createTeamFaker.Generate();
        var expectedTeamId = Guid.NewGuid();

        _mockMediator.Setup(m => m.Send(It.IsAny<CreateTeamCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTeamId);

        // Act
        var result = await _controller.CreateTeam(command);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be(expectedTeamId);

        // Verify mediator was called with the expected command
        _mockMediator.Verify(m => m.Send(It.IsAny<CreateTeamCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateTeam_WithValidationException_Should_ReturnBadRequestResult()
    {
        // Arrange
        var command = _createTeamFaker.Generate();
        var validationErrors = new Dictionary<string, string[]>
        {
            { "Name", new[] { "Team name is required", "Team name must be at most 100 characters long" } },
            { "OrganizationId", new[] { "Organization ID is required" } }
        };
        var validationException = new ValidationException(validationErrors);

        _mockMediator.Setup(m => m.Send(It.IsAny<CreateTeamCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(validationException);

        // Act
        var result = await _controller.CreateTeam(command);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.StatusCode.Should().Be(400);
        badRequestResult.Value.Should().BeEquivalentTo(validationErrors);

        // Verify mediator was called
        _mockMediator.Verify(m => m.Send(It.IsAny<CreateTeamCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateTeam_WithUnexpectedException_Should_ReturnInternalServerError()
    {
        // Arrange
        var command = _createTeamFaker.Generate();
        var exception = new Exception("Unexpected database error");

        _mockMediator.Setup(m => m.Send(It.IsAny<CreateTeamCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.CreateTeam(command);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = result as ObjectResult;
        statusCodeResult!.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("An unexpected error occurred.");
    }

    #endregion

    #region UpdateTeam Tests

    [Fact]
    public async Task UpdateTeam_WithValidCommand_Should_ReturnOkResult()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var command = _updateTeamFaker.Generate();

        _mockMediator.Setup(m => m.Send(It.IsAny<UpdateTeamCommandWithId>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateTeam(teamId, command);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be("Team updated successfully.");

        // Verify mediator was called with the correct parameter
        _mockMediator.Verify(m => m.Send(
            It.Is<UpdateTeamCommandWithId>(cmd =>
                cmd.Id == teamId &&
                cmd.Command == command),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateTeam_WithValidationException_Should_ReturnBadRequestResult()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var command = _updateTeamFaker.Generate();
        var validationErrors = new Dictionary<string, string[]>
        {
            { "Name", new[] { "Team name format is invalid" } }
        };
        var validationException = new ValidationException(validationErrors);

        _mockMediator.Setup(m => m.Send(It.IsAny<UpdateTeamCommandWithId>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(validationException);

        // Act
        var result = await _controller.UpdateTeam(teamId, command);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.StatusCode.Should().Be(400);
        badRequestResult.Value.Should().BeEquivalentTo(validationErrors);
    }

    [Fact]
    public async Task UpdateTeam_WithNotFoundException_Should_ReturnNotFoundResult()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var command = _updateTeamFaker.Generate();
        var errorMessage = $"Team with ID {teamId} not found";
        var notFoundException = new NotFoundException(errorMessage);

        _mockMediator.Setup(m => m.Send(It.IsAny<UpdateTeamCommandWithId>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(notFoundException);

        // Act
        var result = await _controller.UpdateTeam(teamId, command);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.Value.Should().Be(notFoundException.Message);
    }

    [Fact]
    public async Task UpdateTeam_WithUnexpectedException_Should_ReturnInternalServerError()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var command = _updateTeamFaker.Generate();
        var exception = new Exception("Unexpected database error");

        _mockMediator.Setup(m => m.Send(It.IsAny<UpdateTeamCommandWithId>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.UpdateTeam(teamId, command);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = result as ObjectResult;
        statusCodeResult!.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("An unexpected error occurred.");
    }

    #endregion

    #region DeleteTeam Tests

    [Fact]
    public async Task DeleteTeam_WithValidId_Should_ReturnOkResult()
    {
        // Arrange
        var teamId = Guid.NewGuid();

        _mockMediator.Setup(m => m.Send(It.IsAny<DeleteTeamCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteTeam(teamId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be("Team deleted successfully.");

        // Verify mediator was called with the correct command
        _mockMediator.Verify(m => m.Send(
            It.Is<DeleteTeamCommand>(c => c.Id == teamId),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteTeam_WithNotFoundException_Should_ReturnNotFoundResult()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var errorMessage = $"Team with ID {teamId} not found";
        var notFoundException = new NotFoundException(errorMessage);

        _mockMediator.Setup(m => m.Send(It.IsAny<DeleteTeamCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(notFoundException);

        // Act
        var result = await _controller.DeleteTeam(teamId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.Value.Should().Be(notFoundException.Message);
    }

    [Fact]
    public async Task DeleteTeam_WithUnexpectedException_Should_ReturnInternalServerError()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var exception = new Exception("Unexpected database error");

        _mockMediator.Setup(m => m.Send(It.IsAny<DeleteTeamCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.DeleteTeam(teamId);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = result as ObjectResult;
        statusCodeResult!.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("An unexpected error occurred.");
    }    
    
    #endregion

    #region GetAllTeams Tests

    [Fact]
    public async Task GetAllTeams_WithValidQuery_Should_ReturnOkResultWithTeams()
    {
        // Arrange
        var query = _searchTeamsQueryFaker.Generate();
        var expectedResult = _paginatedTeamResultFaker.Generate();

        _mockMediator.Setup(m => m.Send(It.IsAny<SearchTeamsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetAllTeams(query);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedResult);

        // Verify mediator was called with the correct query
        _mockMediator.Verify(m => m.Send(
            It.IsAny<SearchTeamsQuery>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAllTeams_WithValidationException_Should_ReturnBadRequestResult()
    {
        // Arrange
        var query = _searchTeamsQueryFaker.Generate();
        var validationErrors = new Dictionary<string, string[]>
        {
            { "OrganizationId", new[] { "Organization ID format is invalid" } }
        };
        var validationException = new ValidationException(validationErrors);

        _mockMediator.Setup(m => m.Send(It.IsAny<SearchTeamsQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(validationException);

        // Act
        var result = await _controller.GetAllTeams(query);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.StatusCode.Should().Be(400);
        badRequestResult.Value.Should().BeEquivalentTo(validationErrors);
    }

    [Fact]
    public async Task GetAllTeams_WithUnexpectedException_Should_ReturnInternalServerError()
    {
        // Arrange
        var query = _searchTeamsQueryFaker.Generate();
        var exception = new Exception("Unexpected database error");

        _mockMediator.Setup(m => m.Send(It.IsAny<SearchTeamsQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.GetAllTeams(query);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = result as ObjectResult;
        statusCodeResult!.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("An unexpected error occurred.");
    }

    #endregion

    #region GetTeamById Tests

    [Fact]
    public async Task GetTeamById_WithValidId_Should_ReturnOkResultWithTeam()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var expectedTeam = _teamDtoFaker.Generate();
        expectedTeam.Id = teamId;

        _mockMediator.Setup(m => m.Send(It.IsAny<GetTeamByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTeam);

        // Act
        var result = await _controller.GetTeamById(teamId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedTeam);

        // Verify mediator was called with the correct query
        _mockMediator.Verify(m => m.Send(
            It.Is<GetTeamByIdQuery>(q => q.Id == teamId),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetTeamById_WithValidationException_Should_ReturnBadRequestResult()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var validationErrors = new Dictionary<string, string[]>
        {
            { "Id", new[] { "Team ID must not be empty" } }
        };
        var validationException = new ValidationException(validationErrors);

        _mockMediator.Setup(m => m.Send(It.IsAny<GetTeamByIdQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(validationException);

        // Act
        var result = await _controller.GetTeamById(teamId);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.StatusCode.Should().Be(400);
        badRequestResult.Value.Should().BeEquivalentTo(validationErrors);
    }

    [Fact]
    public async Task GetTeamById_WithNotFoundException_Should_ReturnNotFoundResult()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var errorMessage = $"Team with ID {teamId} not found";
        var notFoundException = new NotFoundException(errorMessage);

        _mockMediator.Setup(m => m.Send(It.IsAny<GetTeamByIdQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(notFoundException);

        // Act
        var result = await _controller.GetTeamById(teamId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.Value.Should().Be(notFoundException.Message);
    }

    [Fact]
    public async Task GetTeamById_WithUnexpectedException_Should_ReturnInternalServerError()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var exception = new Exception("Unexpected database error");

        _mockMediator.Setup(m => m.Send(It.IsAny<GetTeamByIdQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.GetTeamById(teamId);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = result as ObjectResult;
        statusCodeResult!.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("An unexpected error occurred.");
    }

    #endregion

    #region GetTeamsByManagerId Tests

    [Fact]
    public async Task GetTeamsByManagerId_WithValidId_Should_ReturnOkResultWithTeams()
    {
        // Arrange
        var managerId = Guid.NewGuid();
        var expectedTeams = _teamDtoFaker.Generate(2).ToList();

        _mockMediator.Setup(m => m.Send(It.IsAny<GetTeamsByManagerIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTeams);

        // Act
        var result = await _controller.GetTeamsByManagerId(managerId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedTeams);

        // Verify mediator was called with the correct query
        _mockMediator.Verify(m => m.Send(
            It.Is<GetTeamsByManagerIdQuery>(q => q.ManagerId == managerId),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetTeamsByManagerId_WithValidationException_Should_ReturnBadRequestResult()
    {
        // Arrange
        var managerId = Guid.NewGuid();
        var validationErrors = new Dictionary<string, string[]>
        {
            { "ManagerId", new[] { "Manager ID format is invalid" } }
        };
        var validationException = new ValidationException(validationErrors);

        _mockMediator.Setup(m => m.Send(It.IsAny<GetTeamsByManagerIdQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(validationException);

        // Act
        var result = await _controller.GetTeamsByManagerId(managerId);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.StatusCode.Should().Be(400);
        badRequestResult.Value.Should().BeEquivalentTo(validationErrors);
    }

    [Fact]
    public async Task GetTeamsByManagerId_WithEntityNotFoundException_Should_ReturnNotFoundResult()
    {
        // Arrange
        var managerId = Guid.NewGuid();
        var errorMessage = $"No teams found for manager ID: {managerId}";
        var entityNotFoundException = new EntityNotFoundException(errorMessage);

        _mockMediator.Setup(m => m.Send(It.IsAny<GetTeamsByManagerIdQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(entityNotFoundException);

        // Act
        var result = await _controller.GetTeamsByManagerId(managerId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.Value.Should().Be(entityNotFoundException.Message);
    }

    [Fact]
    public async Task GetTeamsByManagerId_WithUnexpectedException_Should_ReturnInternalServerError()
    {
        // Arrange
        var managerId = Guid.NewGuid();
        var exception = new Exception("Unexpected database error");

        _mockMediator.Setup(m => m.Send(It.IsAny<GetTeamsByManagerIdQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.GetTeamsByManagerId(managerId);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = result as ObjectResult;
        statusCodeResult!.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("An unexpected error occurred.");
    }

    #endregion

    #region GetTeamsByCollaboratorId Tests

    [Fact]
    public async Task GetTeamsByCollaboratorId_WithValidId_Should_ReturnOkResultWithTeams()
    {
        // Arrange
        var collaboratorId = Guid.NewGuid();
        var expectedTeams = _teamDtoFaker.Generate(2).ToList();

        _mockMediator.Setup(m => m.Send(It.IsAny<GetTeamsByCollaboratorIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTeams);

        // Act
        var result = await _controller.GetTeamsByCollaboratorId(collaboratorId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedTeams);

        // Verify mediator was called with the correct query
        _mockMediator.Verify(m => m.Send(
            It.Is<GetTeamsByCollaboratorIdQuery>(q => q.CollaboratorId == collaboratorId),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetTeamsByCollaboratorId_WithValidationException_Should_ReturnBadRequestResult()
    {
        // Arrange
        var collaboratorId = Guid.NewGuid();
        var validationErrors = new Dictionary<string, string[]>
        {
            { "CollaboratorId", new[] { "Collaborator ID format is invalid" } }
        };
        var validationException = new ValidationException(validationErrors);

        _mockMediator.Setup(m => m.Send(It.IsAny<GetTeamsByCollaboratorIdQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(validationException);

        // Act
        var result = await _controller.GetTeamsByCollaboratorId(collaboratorId);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.StatusCode.Should().Be(400);
        badRequestResult.Value.Should().BeEquivalentTo(validationErrors);
    }

    [Fact]
    public async Task GetTeamsByCollaboratorId_WithEntityNotFoundException_Should_ReturnNotFoundResult()
    {
        // Arrange
        var collaboratorId = Guid.NewGuid();
        var errorMessage = $"No teams found for collaborator ID: {collaboratorId}";
        var entityNotFoundException = new EntityNotFoundException(errorMessage);

        _mockMediator.Setup(m => m.Send(It.IsAny<GetTeamsByCollaboratorIdQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(entityNotFoundException);

        // Act
        var result = await _controller.GetTeamsByCollaboratorId(collaboratorId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.Value.Should().Be(entityNotFoundException.Message);
    }

    [Fact]
    public async Task GetTeamsByCollaboratorId_WithUnexpectedException_Should_ReturnInternalServerError()
    {
        // Arrange
        var collaboratorId = Guid.NewGuid();
        var exception = new Exception("Unexpected database error");

        _mockMediator.Setup(m => m.Send(It.IsAny<GetTeamsByCollaboratorIdQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.GetTeamsByCollaboratorId(collaboratorId);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = result as ObjectResult;
        statusCodeResult!.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("An unexpected error occurred.");
    }

    #endregion

    #region GetTeamsByOrganizationId Tests

    [Fact]
    public async Task GetTeamsByOrganizationId_WithValidId_Should_ReturnOkResultWithTeams()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var expectedTeams = _teamDtoFaker.Generate(3).ToList();

        _mockMediator.Setup(m => m.Send(It.IsAny<GetTeamsByOrganizationIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTeams);

        // Act
        var result = await _controller.GetTeamsByOrganizationId(organizationId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedTeams);

        // Verify mediator was called with the correct query
        _mockMediator.Verify(m => m.Send(
            It.Is<GetTeamsByOrganizationIdQuery>(q => q.OrganizationId == organizationId),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetTeamsByOrganizationId_WithValidationException_Should_ReturnBadRequestResult()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var validationErrors = new Dictionary<string, string[]>
        {
            { "OrganizationId", new[] { "Organization ID format is invalid" } }
        };
        var validationException = new ValidationException(validationErrors);

        _mockMediator.Setup(m => m.Send(It.IsAny<GetTeamsByOrganizationIdQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(validationException);

        // Act
        var result = await _controller.GetTeamsByOrganizationId(organizationId);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.StatusCode.Should().Be(400);
        badRequestResult.Value.Should().BeEquivalentTo(validationErrors);
    }

    [Fact]
    public async Task GetTeamsByOrganizationId_WithEntityNotFoundException_Should_ReturnNotFoundResult()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var errorMessage = $"No teams found for organization ID: {organizationId}";
        var entityNotFoundException = new EntityNotFoundException(errorMessage);

        _mockMediator.Setup(m => m.Send(It.IsAny<GetTeamsByOrganizationIdQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(entityNotFoundException);

        // Act
        var result = await _controller.GetTeamsByOrganizationId(organizationId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.Value.Should().Be(entityNotFoundException.Message);
    }

    [Fact]
    public async Task GetTeamsByOrganizationId_WithUnexpectedException_Should_ReturnInternalServerError()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var exception = new Exception("Unexpected database error");

        _mockMediator.Setup(m => m.Send(It.IsAny<GetTeamsByOrganizationIdQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.GetTeamsByOrganizationId(organizationId);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = result as ObjectResult;
        statusCodeResult!.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("An unexpected error occurred.");
    }

    #endregion

    #region GetTeamsByUserId Tests

    [Fact]
    public async Task GetTeamsByUserId_WithValidId_Should_ReturnOkResultWithTeams()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedTeams = _teamDtoFaker.Generate(2).ToList();

        _mockMediator.Setup(m => m.Send(It.IsAny<GetTeamsByUserIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTeams);

        // Act
        var result = await _controller.GetTeamsByUserId(userId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedTeams);

        // Verify mediator was called with the correct query
        _mockMediator.Verify(m => m.Send(
            It.Is<GetTeamsByUserIdQuery>(q => q.UserId == userId),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region GetTeamManagerByTeamId Tests

    [Fact]
    public async Task GetTeamManagerByTeamId_WithValidId_Should_ReturnOkResultWithManager()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var expectedManager = new UserDto
        {
            Id = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com"
        };

        _mockMediator.Setup(m => m.Send(It.IsAny<GetTeamManagerByTeamIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedManager);

        // Act
        var result = await _controller.GetTeamManagerByTeamId(teamId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedManager);

        // Verify mediator was called with the correct query
        _mockMediator.Verify(m => m.Send(
            It.Is<GetTeamManagerByTeamIdQuery>(q => q.Id == teamId),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region GetTeamsBySessionId Tests

    [Fact]
    public async Task GetTeamsBySessionId_WithValidId_Should_ReturnOkResultWithTeams()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var expectedTeams = _teamDtoFaker.Generate(2).ToList();

        _mockMediator.Setup(m => m.Send(It.IsAny<GetTeamsByOKRSessionIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTeams);

        // Act
        var result = await _controller.GetTeamsBySessionId(sessionId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedTeams);

        // Verify mediator was called with the correct query
        _mockMediator.Verify(m => m.Send(
            It.Is<GetTeamsByOKRSessionIdQuery>(q => q.OKRSessionId == sessionId),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
