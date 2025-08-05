using Bogus;
using FluentAssertions;
using Moq;
using NXM.Tensai.Back.OKR.Application.Common.Exceptions;
using NXM.Tensai.Back.OKR.Domain;
using NXM.Tensai.Back.OKR.Domain.Entities;
using NXM.Tensai.Back.OKR.Domain.Interfaces.Repositories;
using Xunit;

namespace NXM.Tensai.Back.OKR.Application.UnitTests.Features.TeamUsers.Commands;

public class MoveMemberFromTeamToTeamCommandHandlerTests
{
    private readonly Mock<ITeamUserRepository> _teamUserRepositoryMock;
    private readonly Mock<ITeamRepository> _teamRepositoryMock;
    private readonly Mock<IKeyResultTaskRepository> _keyResultTaskRepositoryMock;
    private readonly MoveMemberFromTeamToTeamCommandHandler _handler;
    private readonly Faker _faker;

    public MoveMemberFromTeamToTeamCommandHandlerTests()
    {
        _teamUserRepositoryMock = new Mock<ITeamUserRepository>();
        _teamRepositoryMock = new Mock<ITeamRepository>();
        _keyResultTaskRepositoryMock = new Mock<IKeyResultTaskRepository>();
        _handler = new MoveMemberFromTeamToTeamCommandHandler(
            _teamUserRepositoryMock.Object,
            _teamRepositoryMock.Object,
            _keyResultTaskRepositoryMock.Object);
        _faker = new Faker();
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldMoveMemberFromTeamToTeam()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var sourceTeamId = Guid.NewGuid();
        var newTeamId = Guid.NewGuid();
        var command = new MoveMemberFromTeamToTeamCommand
        {
            MemberId = memberId,
            SourceTeamId = sourceTeamId,
            NewTeamId = newTeamId
        };

        var sourceTeam = new Team
        {
            Id = sourceTeamId,
            Name = _faker.Company.CompanyName(),
            IsDeleted = false
        };

        var newTeam = new Team
        {
            Id = newTeamId,
            Name = _faker.Company.CompanyName(),
            IsDeleted = false
        };

        var teamUser = new TeamUser
        {
            TeamId = sourceTeamId,
            UserId = memberId
        };

        var keyResultTasks = new List<KeyResultTask>
        {
            new KeyResultTask
            {
                CollaboratorId = Guid.NewGuid(), // Different collaborator
                Status = Status.InProgress,
                IsDeleted = false
            }
        };

        _teamRepositoryMock.Setup(x => x.GetByIdAsync(sourceTeamId)).ReturnsAsync(sourceTeam);
        _teamRepositoryMock.Setup(x => x.GetByIdAsync(newTeamId)).ReturnsAsync(newTeam);
        _teamUserRepositoryMock.Setup(x => x.GetByTeamAndUserIdAsync(sourceTeamId, memberId)).ReturnsAsync(teamUser);
        _keyResultTaskRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(keyResultTasks);
        _teamUserRepositoryMock.Setup(x => x.DeleteAsync(teamUser)).Returns(Task.CompletedTask);
        _teamUserRepositoryMock.Setup(x => x.GetByTeamAndUserIdAsync(newTeamId, memberId)).ReturnsAsync((TeamUser?)null);
        _teamUserRepositoryMock.Setup(x => x.AddAsync(It.IsAny<TeamUser>())).ReturnsAsync((TeamUser tu) => tu);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _teamRepositoryMock.Verify(x => x.GetByIdAsync(sourceTeamId), Times.Once);
        _teamRepositoryMock.Verify(x => x.GetByIdAsync(newTeamId), Times.Once);
        _teamUserRepositoryMock.Verify(x => x.GetByTeamAndUserIdAsync(sourceTeamId, memberId), Times.Once);
        _keyResultTaskRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
        _teamUserRepositoryMock.Verify(x => x.DeleteAsync(teamUser), Times.Once);
        _teamUserRepositoryMock.Verify(x => x.GetByTeamAndUserIdAsync(newTeamId, memberId), Times.Once);
        _teamUserRepositoryMock.Verify(x => x.AddAsync(It.Is<TeamUser>(tu => tu.TeamId == newTeamId && tu.UserId == memberId)), Times.Once);
    }

    [Fact]
    public async Task Handle_SourceTeamNotFound_ShouldThrowException()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var sourceTeamId = Guid.NewGuid();
        var newTeamId = Guid.NewGuid();
        var command = new MoveMemberFromTeamToTeamCommand
        {
            MemberId = memberId,
            SourceTeamId = sourceTeamId,
            NewTeamId = newTeamId
        };

        var newTeam = new Team
        {
            Id = newTeamId,
            Name = _faker.Company.CompanyName(),
            IsDeleted = false
        };

        _teamRepositoryMock.Setup(x => x.GetByIdAsync(sourceTeamId)).ReturnsAsync((Team?)null);
        _teamRepositoryMock.Setup(x => x.GetByIdAsync(newTeamId)).ReturnsAsync(newTeam);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Be("Source or destination team not found.");
        _teamRepositoryMock.Verify(x => x.GetByIdAsync(sourceTeamId), Times.Once);
        _teamRepositoryMock.Verify(x => x.GetByIdAsync(newTeamId), Times.Once);
        _teamUserRepositoryMock.Verify(x => x.GetByTeamAndUserIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NewTeamNotFound_ShouldThrowException()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var sourceTeamId = Guid.NewGuid();
        var newTeamId = Guid.NewGuid();
        var command = new MoveMemberFromTeamToTeamCommand
        {
            MemberId = memberId,
            SourceTeamId = sourceTeamId,
            NewTeamId = newTeamId
        };

        var sourceTeam = new Team
        {
            Id = sourceTeamId,
            Name = _faker.Company.CompanyName(),
            IsDeleted = false
        };

        _teamRepositoryMock.Setup(x => x.GetByIdAsync(sourceTeamId)).ReturnsAsync(sourceTeam);
        _teamRepositoryMock.Setup(x => x.GetByIdAsync(newTeamId)).ReturnsAsync((Team?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Be("Source or destination team not found.");
        _teamRepositoryMock.Verify(x => x.GetByIdAsync(sourceTeamId), Times.Once);
        _teamRepositoryMock.Verify(x => x.GetByIdAsync(newTeamId), Times.Once);
        _teamUserRepositoryMock.Verify(x => x.GetByTeamAndUserIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Handle_MemberNotInSourceTeam_ShouldThrowException()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var sourceTeamId = Guid.NewGuid();
        var newTeamId = Guid.NewGuid();
        var command = new MoveMemberFromTeamToTeamCommand
        {
            MemberId = memberId,
            SourceTeamId = sourceTeamId,
            NewTeamId = newTeamId
        };

        var sourceTeam = new Team
        {
            Id = sourceTeamId,
            Name = _faker.Company.CompanyName(),
            IsDeleted = false
        };

        var newTeam = new Team
        {
            Id = newTeamId,
            Name = _faker.Company.CompanyName(),
            IsDeleted = false
        };

        _teamRepositoryMock.Setup(x => x.GetByIdAsync(sourceTeamId)).ReturnsAsync(sourceTeam);
        _teamRepositoryMock.Setup(x => x.GetByIdAsync(newTeamId)).ReturnsAsync(newTeam);
        _teamUserRepositoryMock.Setup(x => x.GetByTeamAndUserIdAsync(sourceTeamId, memberId)).ReturnsAsync((TeamUser?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Be("Member not found in the source team.");
        _teamRepositoryMock.Verify(x => x.GetByIdAsync(sourceTeamId), Times.Once);
        _teamRepositoryMock.Verify(x => x.GetByIdAsync(newTeamId), Times.Once);
        _teamUserRepositoryMock.Verify(x => x.GetByTeamAndUserIdAsync(sourceTeamId, memberId), Times.Once);
        _keyResultTaskRepositoryMock.Verify(x => x.GetAllAsync(), Times.Never);
    }

    [Fact]
    public async Task Handle_MemberHasOngoingTasks_ShouldThrowUserHasOngoingTaskException()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var sourceTeamId = Guid.NewGuid();
        var newTeamId = Guid.NewGuid();
        var command = new MoveMemberFromTeamToTeamCommand
        {
            MemberId = memberId,
            SourceTeamId = sourceTeamId,
            NewTeamId = newTeamId
        };

        var sourceTeam = new Team
        {
            Id = sourceTeamId,
            Name = _faker.Company.CompanyName(),
            IsDeleted = false
        };

        var newTeam = new Team
        {
            Id = newTeamId,
            Name = _faker.Company.CompanyName(),
            IsDeleted = false
        };

        var teamUser = new TeamUser
        {
            TeamId = sourceTeamId,
            UserId = memberId
        };

        var keyResultTasks = new List<KeyResultTask>
        {
            new KeyResultTask
            {
                CollaboratorId = memberId, // Same collaborator with ongoing task
                Status = Status.InProgress,
                IsDeleted = false
            }
        };

        _teamRepositoryMock.Setup(x => x.GetByIdAsync(sourceTeamId)).ReturnsAsync(sourceTeam);
        _teamRepositoryMock.Setup(x => x.GetByIdAsync(newTeamId)).ReturnsAsync(newTeam);
        _teamUserRepositoryMock.Setup(x => x.GetByTeamAndUserIdAsync(sourceTeamId, memberId)).ReturnsAsync(teamUser);
        _keyResultTaskRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(keyResultTasks);

        // Act & Assert
        await Assert.ThrowsAsync<UserHasOngoingTaskException>(() => 
            _handler.Handle(command, CancellationToken.None));

        _teamRepositoryMock.Verify(x => x.GetByIdAsync(sourceTeamId), Times.Once);
        _teamRepositoryMock.Verify(x => x.GetByIdAsync(newTeamId), Times.Once);
        _teamUserRepositoryMock.Verify(x => x.GetByTeamAndUserIdAsync(sourceTeamId, memberId), Times.Once);
        _keyResultTaskRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
        _teamUserRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<TeamUser>()), Times.Never);
        _teamUserRepositoryMock.Verify(x => x.AddAsync(It.IsAny<TeamUser>()), Times.Never);
    }

    [Fact]
    public async Task Handle_MemberAlreadyInNewTeam_ShouldNotAddDuplicate()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var sourceTeamId = Guid.NewGuid();
        var newTeamId = Guid.NewGuid();
        var command = new MoveMemberFromTeamToTeamCommand
        {
            MemberId = memberId,
            SourceTeamId = sourceTeamId,
            NewTeamId = newTeamId
        };

        var sourceTeam = new Team
        {
            Id = sourceTeamId,
            Name = _faker.Company.CompanyName(),
            IsDeleted = false
        };

        var newTeam = new Team
        {
            Id = newTeamId,
            Name = _faker.Company.CompanyName(),
            IsDeleted = false
        };

        var sourceTeamUser = new TeamUser
        {
            TeamId = sourceTeamId,
            UserId = memberId
        };

        var existingNewTeamUser = new TeamUser
        {
            TeamId = newTeamId,
            UserId = memberId
        };

        var keyResultTasks = new List<KeyResultTask>
        {
            new KeyResultTask
            {
                CollaboratorId = Guid.NewGuid(), // Different collaborator
                Status = Status.InProgress,
                IsDeleted = false
            }
        };

        _teamRepositoryMock.Setup(x => x.GetByIdAsync(sourceTeamId)).ReturnsAsync(sourceTeam);
        _teamRepositoryMock.Setup(x => x.GetByIdAsync(newTeamId)).ReturnsAsync(newTeam);
        _teamUserRepositoryMock.Setup(x => x.GetByTeamAndUserIdAsync(sourceTeamId, memberId)).ReturnsAsync(sourceTeamUser);
        _keyResultTaskRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(keyResultTasks);
        _teamUserRepositoryMock.Setup(x => x.DeleteAsync(sourceTeamUser)).Returns(Task.CompletedTask);
        _teamUserRepositoryMock.Setup(x => x.GetByTeamAndUserIdAsync(newTeamId, memberId)).ReturnsAsync(existingNewTeamUser);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _teamRepositoryMock.Verify(x => x.GetByIdAsync(sourceTeamId), Times.Once);
        _teamRepositoryMock.Verify(x => x.GetByIdAsync(newTeamId), Times.Once);
        _teamUserRepositoryMock.Verify(x => x.GetByTeamAndUserIdAsync(sourceTeamId, memberId), Times.Once);
        _keyResultTaskRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
        _teamUserRepositoryMock.Verify(x => x.DeleteAsync(sourceTeamUser), Times.Once);
        _teamUserRepositoryMock.Verify(x => x.GetByTeamAndUserIdAsync(newTeamId, memberId), Times.Once);
        _teamUserRepositoryMock.Verify(x => x.AddAsync(It.IsAny<TeamUser>()), Times.Never); // Should not add duplicate
    }

    [Fact]
    public async Task Handle_DatabaseErrorDuringDelete_ShouldThrowException()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var sourceTeamId = Guid.NewGuid();
        var newTeamId = Guid.NewGuid();
        var command = new MoveMemberFromTeamToTeamCommand
        {
            MemberId = memberId,
            SourceTeamId = sourceTeamId,
            NewTeamId = newTeamId
        };

        var sourceTeam = new Team
        {
            Id = sourceTeamId,
            Name = _faker.Company.CompanyName(),
            IsDeleted = false
        };

        var newTeam = new Team
        {
            Id = newTeamId,
            Name = _faker.Company.CompanyName(),
            IsDeleted = false
        };

        var teamUser = new TeamUser
        {
            TeamId = sourceTeamId,
            UserId = memberId
        };

        var keyResultTasks = new List<KeyResultTask>();

        _teamRepositoryMock.Setup(x => x.GetByIdAsync(sourceTeamId)).ReturnsAsync(sourceTeam);
        _teamRepositoryMock.Setup(x => x.GetByIdAsync(newTeamId)).ReturnsAsync(newTeam);
        _teamUserRepositoryMock.Setup(x => x.GetByTeamAndUserIdAsync(sourceTeamId, memberId)).ReturnsAsync(teamUser);
        _keyResultTaskRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(keyResultTasks);
        _teamUserRepositoryMock.Setup(x => x.DeleteAsync(teamUser)).ThrowsAsync(new Exception("Database error during delete"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Be("Database error during delete");
        _teamUserRepositoryMock.Verify(x => x.DeleteAsync(teamUser), Times.Once);
        _teamUserRepositoryMock.Verify(x => x.AddAsync(It.IsAny<TeamUser>()), Times.Never);
    }
}
