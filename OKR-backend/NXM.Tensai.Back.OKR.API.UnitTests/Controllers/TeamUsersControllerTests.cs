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

public class TeamUsersControllerTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ILogger<TeamUsersController>> _mockLogger;
    private readonly TeamUsersController _controller;
    private readonly Faker<GetUsersByTeamIdQuery> _getUsersByTeamIdQueryFaker;
    private readonly Faker<RemoveUserFromTeamCommand> _removeUserFromTeamCommandFaker;
    private readonly Faker<MoveMemberFromTeamToTeamCommand> _moveMemberCommandFaker;
    private readonly Faker<AddUsersToTeamCommand> _addUsersToTeamCommandFaker;
    private readonly Faker<UserWithRoleDto> _userWithRoleDtoFaker;
    private readonly Faker<AddUsersToTeamResult> _addUsersToTeamResultFaker;
    private readonly Faker<TeamUsersController.MoveMemberFromTeamToTeamRequest> _moveMemberRequestFaker;
    private readonly Faker<TeamUsersController.AddUsersToTeamRequest> _addUsersRequestFaker;

    public TeamUsersControllerTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<TeamUsersController>>();
        _controller = new TeamUsersController(_mockMediator.Object, _mockLogger.Object);

        // Setup fakers for consistent test data generation
        _getUsersByTeamIdQueryFaker = new Faker<GetUsersByTeamIdQuery>()
            .RuleFor(q => q.TeamId, f => f.Random.Guid());

        _removeUserFromTeamCommandFaker = new Faker<RemoveUserFromTeamCommand>()
            .RuleFor(c => c.TeamId, f => f.Random.Guid())
            .RuleFor(c => c.UserId, f => f.Random.Guid());

        _moveMemberCommandFaker = new Faker<MoveMemberFromTeamToTeamCommand>()
            .RuleFor(c => c.MemberId, f => f.Random.Guid())
            .RuleFor(c => c.SourceTeamId, f => f.Random.Guid())
            .RuleFor(c => c.NewTeamId, f => f.Random.Guid());

        _addUsersToTeamCommandFaker = new Faker<AddUsersToTeamCommand>()
            .RuleFor(c => c.TeamId, f => f.Random.Guid())
            .RuleFor(c => c.UserIds, f => f.Make(3, () => f.Random.Guid()));

        _userWithRoleDtoFaker = new Faker<UserWithRoleDto>()
            .RuleFor(u => u.Id, f => f.Random.Guid())
            .RuleFor(u => u.FirstName, f => f.Name.FirstName())
            .RuleFor(u => u.LastName, f => f.Name.LastName())
            .RuleFor(u => u.Email, f => f.Internet.Email())
            .RuleFor(u => u.Address, f => f.Address.FullAddress())
            .RuleFor(u => u.Position, f => f.Name.JobTitle())
            .RuleFor(u => u.DateOfBirth, f => f.Date.Past(30, DateTime.Now.AddYears(-18)))
            .RuleFor(u => u.ProfilePictureUrl, f => f.Internet.Avatar())
            .RuleFor(u => u.IsNotificationEnabled, f => f.Random.Bool())
            .RuleFor(u => u.IsEnabled, f => f.Random.Bool())
            .RuleFor(u => u.Gender, f => f.PickRandom<Gender>())
            .RuleFor(u => u.OrganizationId, f => f.Random.Guid())
            .RuleFor(u => u.CreatedDate, f => f.Date.Past(1))
            .RuleFor(u => u.ModifiedDate, f => f.Date.Recent())
            .RuleFor(u => u.Role, f => f.PickRandom("Collaborator", "TeamManager", "Admin"));

        _addUsersToTeamResultFaker = new Faker<AddUsersToTeamResult>()
            .RuleFor(r => r.AddedUserIds, f => f.Make(2, () => f.Random.Guid()))
            .RuleFor(r => r.NotFoundUserIds, f => f.Make(1, () => f.Random.Guid()))
            .RuleFor(r => r.Message, f => f.Lorem.Sentence());

        _moveMemberRequestFaker = new Faker<TeamUsersController.MoveMemberFromTeamToTeamRequest>()
            .RuleFor(r => r.MemberId, f => f.Random.Guid())
            .RuleFor(r => r.SourceTeamId, f => f.Random.Guid())
            .RuleFor(r => r.NewTeamId, f => f.Random.Guid());

        _addUsersRequestFaker = new Faker<TeamUsersController.AddUsersToTeamRequest>()
            .RuleFor(r => r.TeamId, f => f.Random.Guid())
            .RuleFor(r => r.UserIds, f => f.Make(3, () => f.Random.Guid()));
    }

    #region GetUsersByTeamId Tests

    [Fact]
    public async Task GetUsersByTeamId_WithValidTeamId_Should_ReturnOkResultWithUsers()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var expectedUsers = _userWithRoleDtoFaker.Generate(3);

        _mockMediator.Setup(m => m.Send(It.IsAny<GetUsersByTeamIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUsers);

        // Act
        var result = await _controller.GetUsersByTeamId(teamId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedUsers);

        // Verify mediator was called with the correct query
        _mockMediator.Verify(m => m.Send(
            It.Is<GetUsersByTeamIdQuery>(q => q.TeamId == teamId),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetUsersByTeamId_WithValidationException_Should_ReturnBadRequestResult()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var validationErrors = new Dictionary<string, string[]>
        {
            { "TeamId", new[] { "Team ID must not be empty" } }
        };
        var validationException = new ValidationException(validationErrors);

        _mockMediator.Setup(m => m.Send(It.IsAny<GetUsersByTeamIdQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(validationException);

        // Act
        var result = await _controller.GetUsersByTeamId(teamId);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.StatusCode.Should().Be(400);
        badRequestResult.Value.Should().BeEquivalentTo(validationErrors);
    }

    [Fact]
    public async Task GetUsersByTeamId_WithEntityNotFoundException_Should_ReturnNotFoundResult()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var errorMessage = $"No users found for Team ID {teamId}.";
        var notFoundException = new EntityNotFoundException(errorMessage);

        _mockMediator.Setup(m => m.Send(It.IsAny<GetUsersByTeamIdQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(notFoundException);

        // Act
        var result = await _controller.GetUsersByTeamId(teamId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.Value.Should().Be(errorMessage);
    }

    [Fact]
    public async Task GetUsersByTeamId_WithUnexpectedException_Should_ReturnInternalServerError()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var exception = new Exception("Unexpected database error");

        _mockMediator.Setup(m => m.Send(It.IsAny<GetUsersByTeamIdQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.GetUsersByTeamId(teamId);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = result as ObjectResult;
        statusCodeResult!.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("An unexpected error occurred.");
    }

    #endregion

    #region RemoveUserFromTeam Tests

    [Fact]
    public async Task RemoveUserFromTeam_WithValidIds_Should_ReturnNoContentResult()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _mockMediator.Setup(m => m.Send(It.IsAny<RemoveUserFromTeamCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.RemoveUserFromTeam(teamId, userId);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        // Verify mediator was called with the correct command
        _mockMediator.Verify(m => m.Send(
            It.Is<RemoveUserFromTeamCommand>(c => c.TeamId == teamId && c.UserId == userId),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RemoveUserFromTeam_WithValidationException_Should_ReturnBadRequestResult()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var validationErrors = new Dictionary<string, string[]>
        {
            { "TeamId", new[] { "Team ID is required" } },
            { "UserId", new[] { "User ID is required" } }
        };
        var validationException = new ValidationException(validationErrors);

        _mockMediator.Setup(m => m.Send(It.IsAny<RemoveUserFromTeamCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(validationException);

        // Act
        var result = await _controller.RemoveUserFromTeam(teamId, userId);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.StatusCode.Should().Be(400);
        badRequestResult.Value.Should().BeEquivalentTo(validationErrors);
    }

    [Fact]
    public async Task RemoveUserFromTeam_WithEntityNotFoundException_Should_ReturnNotFoundResult()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var errorMessage = "TeamUser not found.";
        var notFoundException = new EntityNotFoundException(errorMessage);

        _mockMediator.Setup(m => m.Send(It.IsAny<RemoveUserFromTeamCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(notFoundException);

        // Act
        var result = await _controller.RemoveUserFromTeam(teamId, userId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.Value.Should().Be(errorMessage);
    }

    [Fact]
    public async Task RemoveUserFromTeam_WithUnexpectedException_Should_ReturnInternalServerError()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var exception = new Exception("Unexpected database error");

        _mockMediator.Setup(m => m.Send(It.IsAny<RemoveUserFromTeamCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.RemoveUserFromTeam(teamId, userId);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = result as ObjectResult;
        statusCodeResult!.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("An unexpected error occurred.");
    }

    #endregion

    #region MoveMemberFromTeamToTeam Tests

    [Fact]
    public async Task MoveMemberFromTeamToTeam_WithValidRequest_Should_ReturnNoContentResult()
    {
        // Arrange
        var request = _moveMemberRequestFaker.Generate();

        _mockMediator.Setup(m => m.Send(It.IsAny<MoveMemberFromTeamToTeamCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.MoveMemberFromTeamToTeam(request);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        // Verify mediator was called with the correct command
        _mockMediator.Verify(m => m.Send(
            It.Is<MoveMemberFromTeamToTeamCommand>(c => 
                c.MemberId == request.MemberId && 
                c.SourceTeamId == request.SourceTeamId && 
                c.NewTeamId == request.NewTeamId),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task MoveMemberFromTeamToTeam_WithValidationException_Should_ReturnBadRequestResult()
    {
        // Arrange
        var request = _moveMemberRequestFaker.Generate();
        var validationErrors = new Dictionary<string, string[]>
        {
            { "MemberId", new[] { "Member ID is required" } },
            { "SourceTeamId", new[] { "Source team ID is required" } }
        };
        var validationException = new ValidationException(validationErrors);

        _mockMediator.Setup(m => m.Send(It.IsAny<MoveMemberFromTeamToTeamCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(validationException);

        // Act
        var result = await _controller.MoveMemberFromTeamToTeam(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.StatusCode.Should().Be(400);
        badRequestResult.Value.Should().BeEquivalentTo(validationErrors);
    }

    [Fact]
    public async Task MoveMemberFromTeamToTeam_WithEntityNotFoundException_Should_ReturnNotFoundResult()
    {
        // Arrange
        var request = _moveMemberRequestFaker.Generate();
        var errorMessage = "Member not found in the source team.";
        var notFoundException = new EntityNotFoundException(errorMessage);

        _mockMediator.Setup(m => m.Send(It.IsAny<MoveMemberFromTeamToTeamCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(notFoundException);

        // Act
        var result = await _controller.MoveMemberFromTeamToTeam(request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.Value.Should().Be(errorMessage);
    }

    [Fact]
    public async Task MoveMemberFromTeamToTeam_WithUserHasOngoingTaskException_Should_ReturnBadRequestResult()
    {
        // Arrange
        var request = _moveMemberRequestFaker.Generate();
        var errorMessage = "User has an ongoing task. Please reassign it before moving the user to another team.";
        var ongoingTaskException = new UserHasOngoingTaskException(errorMessage);

        _mockMediator.Setup(m => m.Send(It.IsAny<MoveMemberFromTeamToTeamCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(ongoingTaskException);

        // Act
        var result = await _controller.MoveMemberFromTeamToTeam(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().Be(errorMessage);
    }

    [Fact]
    public async Task MoveMemberFromTeamToTeam_WithUnexpectedException_Should_ReturnInternalServerError()
    {
        // Arrange
        var request = _moveMemberRequestFaker.Generate();
        var exception = new Exception("Unexpected database error");

        _mockMediator.Setup(m => m.Send(It.IsAny<MoveMemberFromTeamToTeamCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.MoveMemberFromTeamToTeam(request);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = result as ObjectResult;
        statusCodeResult!.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("An unexpected error occurred.");
    }

    #endregion

    #region AddUsersToTeam Tests

    [Fact]
    public async Task AddUsersToTeam_WithValidRequest_Should_ReturnOkResultWithResult()
    {
        // Arrange
        var request = _addUsersRequestFaker.Generate();
        var expectedResult = _addUsersToTeamResultFaker.Generate();

        _mockMediator.Setup(m => m.Send(It.IsAny<AddUsersToTeamCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.AddUsersToTeam(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedResult);

        // Verify mediator was called with the correct command
        _mockMediator.Verify(m => m.Send(
            It.Is<AddUsersToTeamCommand>(c => 
                c.TeamId == request.TeamId && 
                c.UserIds.SequenceEqual(request.UserIds)),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddUsersToTeam_WithValidationException_Should_ReturnBadRequestResult()
    {
        // Arrange
        var request = _addUsersRequestFaker.Generate();
        var validationErrors = new Dictionary<string, string[]>
        {
            { "TeamId", new[] { "Team ID is required" } },
            { "UserIds", new[] { "At least one user ID is required" } }
        };
        var validationException = new ValidationException(validationErrors);

        _mockMediator.Setup(m => m.Send(It.IsAny<AddUsersToTeamCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(validationException);

        // Act
        var result = await _controller.AddUsersToTeam(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.StatusCode.Should().Be(400);
        badRequestResult.Value.Should().BeEquivalentTo(validationErrors);
    }

    [Fact]
    public async Task AddUsersToTeam_WithEntityNotFoundException_Should_ReturnNotFoundResult()
    {
        // Arrange
        var request = _addUsersRequestFaker.Generate();
        var errorMessage = "Team not found or is deleted.";
        var notFoundException = new EntityNotFoundException(errorMessage);

        _mockMediator.Setup(m => m.Send(It.IsAny<AddUsersToTeamCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(notFoundException);

        // Act
        var result = await _controller.AddUsersToTeam(request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.Value.Should().Be(errorMessage);
    }

    [Fact]
    public async Task AddUsersToTeam_WithUnexpectedException_Should_ReturnInternalServerError()
    {
        // Arrange
        var request = _addUsersRequestFaker.Generate();
        var exception = new Exception("Unexpected database error");

        _mockMediator.Setup(m => m.Send(It.IsAny<AddUsersToTeamCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.AddUsersToTeam(request);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = result as ObjectResult;
        statusCodeResult!.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("An unexpected error occurred.");
    }

    #endregion
}
