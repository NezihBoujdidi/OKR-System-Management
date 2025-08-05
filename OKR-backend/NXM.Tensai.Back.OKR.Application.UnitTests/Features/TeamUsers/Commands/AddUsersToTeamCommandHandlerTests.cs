using Bogus;
using FluentAssertions;
using Moq;
using NXM.Tensai.Back.OKR.Application.Common.Exceptions;
using NXM.Tensai.Back.OKR.Domain.Entities;
using NXM.Tensai.Back.OKR.Domain.Interfaces.Repositories;
using Xunit;

namespace NXM.Tensai.Back.OKR.Application.UnitTests.Features.TeamUsers.Commands;

public class AddUsersToTeamCommandHandlerTests
{
    private readonly Mock<ITeamUserRepository> _teamUserRepositoryMock;
    private readonly Mock<ITeamRepository> _teamRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly AddUsersToTeamCommandHandler _handler;
    private readonly Faker _faker;

    public AddUsersToTeamCommandHandlerTests()
    {
        _teamUserRepositoryMock = new Mock<ITeamUserRepository>();
        _teamRepositoryMock = new Mock<ITeamRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _handler = new AddUsersToTeamCommandHandler(
            _teamUserRepositoryMock.Object,
            _teamRepositoryMock.Object,
            _userRepositoryMock.Object);
        _faker = new Faker();
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldAddAllUsersToTeam()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var userIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var command = new AddUsersToTeamCommand
        {
            TeamId = teamId,
            UserIds = userIds
        };        var team = new Team
        {
            Id = teamId,
            Name = _faker.Company.CompanyName(),
            IsDeleted = false
        };

        var users = userIds.Select(id => new User
        { 
            Id = id, 
            FirstName = _faker.Name.FirstName(),
            LastName = _faker.Name.LastName(),
            Email = _faker.Internet.Email()
        }).ToList();

        _teamRepositoryMock.Setup(x => x.GetByIdAsync(teamId)).ReturnsAsync(team);
        
        foreach (var user in users)
        {
            _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id)).ReturnsAsync(user);
            _teamUserRepositoryMock.Setup(x => x.GetByTeamAndUserIdAsync(teamId, user.Id)).ReturnsAsync((TeamUser?)null);
            _teamUserRepositoryMock.Setup(x => x.AddAsync(It.IsAny<TeamUser>())).ReturnsAsync((TeamUser tu) => tu);
        }

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AddedUserIds.Should().HaveCount(3);
        result.AddedUserIds.Should().BeEquivalentTo(userIds);
        result.NotFoundUserIds.Should().BeEmpty();
        result.Message.Should().Be("All users added to team successfully.");

        _teamRepositoryMock.Verify(x => x.GetByIdAsync(teamId), Times.Once);
        foreach (var userId in userIds)
        {
            _userRepositoryMock.Verify(x => x.GetByIdAsync(userId), Times.Once);
            _teamUserRepositoryMock.Verify(x => x.GetByTeamAndUserIdAsync(teamId, userId), Times.Once);
            _teamUserRepositoryMock.Verify(x => x.AddAsync(It.Is<TeamUser>(tu => tu.TeamId == teamId && tu.UserId == userId)), Times.Once);
        }
    }

    [Fact]
    public async Task Handle_TeamNotFound_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var userIds = new List<Guid> { Guid.NewGuid() };
        var command = new AddUsersToTeamCommand
        {
            TeamId = teamId,
            UserIds = userIds
        };

        _teamRepositoryMock.Setup(x => x.GetByIdAsync(teamId)).ReturnsAsync((Team?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() => 
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Be("Team not found or is deleted.");
        _teamRepositoryMock.Verify(x => x.GetByIdAsync(teamId), Times.Once);
        _userRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _teamUserRepositoryMock.Verify(x => x.AddAsync(It.IsAny<TeamUser>()), Times.Never);
    }

    [Fact]
    public async Task Handle_TeamIsDeleted_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var userIds = new List<Guid> { Guid.NewGuid() };
        var command = new AddUsersToTeamCommand
        {
            TeamId = teamId,
            UserIds = userIds
        };        var deletedTeam = new Team
        {
            Id = teamId,
            Name = _faker.Company.CompanyName(),
            IsDeleted = true
        };

        _teamRepositoryMock.Setup(x => x.GetByIdAsync(teamId)).ReturnsAsync(deletedTeam);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() => 
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Be("Team not found or is deleted.");
        _teamRepositoryMock.Verify(x => x.GetByIdAsync(teamId), Times.Once);
        _userRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _teamUserRepositoryMock.Verify(x => x.AddAsync(It.IsAny<TeamUser>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SomeUsersNotFound_ShouldAddExistingUsersAndReportMissing()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var existingUserId = Guid.NewGuid();
        var missingUserId = Guid.NewGuid();
        var userIds = new List<Guid> { existingUserId, missingUserId };
        var command = new AddUsersToTeamCommand
        {
            TeamId = teamId,
            UserIds = userIds
        };        var team = new Team
        {
            Id = teamId,
            Name = _faker.Company.CompanyName(),
            IsDeleted = false
        };

        var existingUser = new User
        { 
            Id = existingUserId, 
            FirstName = _faker.Name.FirstName(),
            LastName = _faker.Name.LastName(),
            Email = _faker.Internet.Email()
        };

        _teamRepositoryMock.Setup(x => x.GetByIdAsync(teamId)).ReturnsAsync(team);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(existingUserId)).ReturnsAsync(existingUser);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(missingUserId)).ReturnsAsync((User?)null);
        _teamUserRepositoryMock.Setup(x => x.GetByTeamAndUserIdAsync(teamId, existingUserId)).ReturnsAsync((TeamUser?)null);
        _teamUserRepositoryMock.Setup(x => x.AddAsync(It.IsAny<TeamUser>())).ReturnsAsync((TeamUser tu) => tu);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AddedUserIds.Should().HaveCount(1);
        result.AddedUserIds.Should().Contain(existingUserId);
        result.NotFoundUserIds.Should().HaveCount(1);
        result.NotFoundUserIds.Should().Contain(missingUserId);
        result.Message.Should().Contain("Added users to team, but the following user IDs do not exist:");
        result.Message.Should().Contain(missingUserId.ToString());

        _teamRepositoryMock.Verify(x => x.GetByIdAsync(teamId), Times.Once);
        _userRepositoryMock.Verify(x => x.GetByIdAsync(existingUserId), Times.Once);
        _userRepositoryMock.Verify(x => x.GetByIdAsync(missingUserId), Times.Once);
        _teamUserRepositoryMock.Verify(x => x.AddAsync(It.Is<TeamUser>(tu => tu.TeamId == teamId && tu.UserId == existingUserId)), Times.Once);
    }

    [Fact]
    public async Task Handle_UserAlreadyInTeam_ShouldSkipDuplicateUser()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var command = new AddUsersToTeamCommand
        {
            TeamId = teamId,
            UserIds = new List<Guid> { userId }
        };        var team = new Team
        {
            Id = teamId,
            Name = _faker.Company.CompanyName(),
            IsDeleted = false
        };

        var user = new User
        { 
            Id = userId, 
            FirstName = _faker.Name.FirstName(),
            LastName = _faker.Name.LastName(),
            Email = _faker.Internet.Email()
        };

        var existingTeamUser = new TeamUser
        {
            TeamId = teamId,
            UserId = userId
        };

        _teamRepositoryMock.Setup(x => x.GetByIdAsync(teamId)).ReturnsAsync(team);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _teamUserRepositoryMock.Setup(x => x.GetByTeamAndUserIdAsync(teamId, userId)).ReturnsAsync(existingTeamUser);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AddedUserIds.Should().BeEmpty();
        result.NotFoundUserIds.Should().BeEmpty();
        result.Message.Should().Be("All users added to team successfully.");

        _teamRepositoryMock.Verify(x => x.GetByIdAsync(teamId), Times.Once);
        _userRepositoryMock.Verify(x => x.GetByIdAsync(userId), Times.Once);
        _teamUserRepositoryMock.Verify(x => x.GetByTeamAndUserIdAsync(teamId, userId), Times.Once);
        _teamUserRepositoryMock.Verify(x => x.AddAsync(It.IsAny<TeamUser>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmptyUserIdsList_ShouldReturnSuccessMessage()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var command = new AddUsersToTeamCommand
        {
            TeamId = teamId,
            UserIds = new List<Guid>()
        };        var team = new Team
        {
            Id = teamId,
            Name = _faker.Company.CompanyName(),
            IsDeleted = false
        };

        _teamRepositoryMock.Setup(x => x.GetByIdAsync(teamId)).ReturnsAsync(team);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AddedUserIds.Should().BeEmpty();
        result.NotFoundUserIds.Should().BeEmpty();
        result.Message.Should().Be("All users added to team successfully.");

        _teamRepositoryMock.Verify(x => x.GetByIdAsync(teamId), Times.Once);
        _userRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _teamUserRepositoryMock.Verify(x => x.AddAsync(It.IsAny<TeamUser>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UnexpectedError_ShouldThrowException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var command = new AddUsersToTeamCommand
        {
            TeamId = teamId,
            UserIds = new List<Guid> { userId }
        };

        _teamRepositoryMock.Setup(x => x.GetByIdAsync(teamId)).ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Be("Database error");
        _teamRepositoryMock.Verify(x => x.GetByIdAsync(teamId), Times.Once);
    }
}
