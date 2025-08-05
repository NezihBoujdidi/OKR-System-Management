using Bogus;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using NXM.Tensai.Back.OKR.Application.Common.Exceptions;
using NXM.Tensai.Back.OKR.Domain.Entities;
using NXM.Tensai.Back.OKR.Domain.Interfaces.Repositories;
using Xunit;
using ValidationException = FluentValidation.ValidationException;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace NXM.Tensai.Back.OKR.Application.UnitTests.Features.TeamUsers.Commands;

public class RemoveUserFromTeamCommandHandlerTests
{
    private readonly Mock<ITeamUserRepository> _teamUserRepositoryMock;
    private readonly Mock<IValidator<RemoveUserFromTeamCommand>> _validatorMock;
    private readonly RemoveUserFromTeamCommandHandler _handler;
    private readonly Faker _faker;

    public RemoveUserFromTeamCommandHandlerTests()
    {
        _teamUserRepositoryMock = new Mock<ITeamUserRepository>();
        _validatorMock = new Mock<IValidator<RemoveUserFromTeamCommand>>();
        _handler = new RemoveUserFromTeamCommandHandler(
            _teamUserRepositoryMock.Object,
            _validatorMock.Object);
        _faker = new Faker();
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldRemoveUserFromTeam()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var command = new RemoveUserFromTeamCommand
        {
            TeamId = teamId,
            UserId = userId
        };

        var teamUser = new TeamUser
        {
            TeamId = teamId,
            UserId = userId
        };

        var validationResult = new ValidationResult();
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        _teamUserRepositoryMock.Setup(x => x.GetByTeamAndUserIdAsync(teamId, userId))
            .ReturnsAsync(teamUser);
        _teamUserRepositoryMock.Setup(x => x.DeleteAsync(teamUser))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _validatorMock.Verify(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);
        _teamUserRepositoryMock.Verify(x => x.GetByTeamAndUserIdAsync(teamId, userId), Times.Once);
        _teamUserRepositoryMock.Verify(x => x.DeleteAsync(teamUser), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidationFails_ShouldThrowValidationException()
    {
        // Arrange
        var command = new RemoveUserFromTeamCommand
        {
            TeamId = Guid.Empty,
            UserId = Guid.Empty
        };

        var validationErrors = new List<ValidationFailure>
        {
            new ValidationFailure("TeamId", "TeamId is required"),
            new ValidationFailure("UserId", "UserId is required")
        };
        var validationResult = new ValidationResult(validationErrors);
        
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => 
            _handler.Handle(command, CancellationToken.None));

        exception.Errors.Should().HaveCount(2);
        _validatorMock.Verify(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);
        _teamUserRepositoryMock.Verify(x => x.GetByTeamAndUserIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
        _teamUserRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<TeamUser>()), Times.Never);
    }

    [Fact]
    public async Task Handle_TeamUserNotFound_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var command = new RemoveUserFromTeamCommand
        {
            TeamId = teamId,
            UserId = userId
        };

        var validationResult = new ValidationResult();
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        _teamUserRepositoryMock.Setup(x => x.GetByTeamAndUserIdAsync(teamId, userId))
            .ReturnsAsync((TeamUser?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() => 
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Be("TeamUser not found.");
        _validatorMock.Verify(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);
        _teamUserRepositoryMock.Verify(x => x.GetByTeamAndUserIdAsync(teamId, userId), Times.Once);
        _teamUserRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<TeamUser>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DatabaseError_ShouldThrowException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var command = new RemoveUserFromTeamCommand
        {
            TeamId = teamId,
            UserId = userId
        };

        var validationResult = new ValidationResult();
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        _teamUserRepositoryMock.Setup(x => x.GetByTeamAndUserIdAsync(teamId, userId))
            .ThrowsAsync(new Exception("Database connection error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Be("Database connection error");
        _validatorMock.Verify(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);
        _teamUserRepositoryMock.Verify(x => x.GetByTeamAndUserIdAsync(teamId, userId), Times.Once);
    }

    [Fact]
    public async Task Handle_DeleteAsyncFails_ShouldThrowException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var command = new RemoveUserFromTeamCommand
        {
            TeamId = teamId,
            UserId = userId
        };

        var teamUser = new TeamUser
        {
            TeamId = teamId,
            UserId = userId
        };

        var validationResult = new ValidationResult();
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        _teamUserRepositoryMock.Setup(x => x.GetByTeamAndUserIdAsync(teamId, userId))
            .ReturnsAsync(teamUser);
        _teamUserRepositoryMock.Setup(x => x.DeleteAsync(teamUser))
            .ThrowsAsync(new Exception("Failed to delete team user"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Be("Failed to delete team user");
        _validatorMock.Verify(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);
        _teamUserRepositoryMock.Verify(x => x.GetByTeamAndUserIdAsync(teamId, userId), Times.Once);
        _teamUserRepositoryMock.Verify(x => x.DeleteAsync(teamUser), Times.Once);
    }
}
