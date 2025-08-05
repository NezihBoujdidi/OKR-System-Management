using Bogus;
using FluentAssertions;
using FluentValidation;
using ValidationException = FluentValidation.ValidationException;

namespace NXM.Tensai.Back.OKR.Application.UnitTests.Features.Teams.Commands;

public class DeleteTeamCommandHandlerTests
{
    private readonly Mock<ITeamRepository> _teamRepositoryMock;
    private readonly DeleteTeamCommandHandler _handler;
    private readonly Faker<Team> _teamFaker;

    public DeleteTeamCommandHandlerTests()
    {
        _teamRepositoryMock = new Mock<ITeamRepository>();
        _handler = new DeleteTeamCommandHandler(_teamRepositoryMock.Object);

        _teamFaker = new Faker<Team>()
            .RuleFor(x => x.Id, f => f.Random.Guid())
            .RuleFor(x => x.Name, f => f.Company.CompanyName())
            .RuleFor(x => x.Description, f => f.Lorem.Sentence())
            .RuleFor(x => x.OrganizationId, f => f.Random.Guid())
            .RuleFor(x => x.TeamManagerId, f => f.Random.Guid())
            .RuleFor(x => x.CreatedDate, f => f.Date.Recent())
            .RuleFor(x => x.IsDeleted, f => false);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldSoftDeleteTeamSuccessfully()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var command = new DeleteTeamCommand(teamId);
        var existingTeam = _teamFaker.Generate();
        existingTeam.IsDeleted = false;
        
        _teamRepositoryMock.Setup(x => x.GetByIdAsync(teamId))
            .ReturnsAsync(existingTeam);
        
        _teamRepositoryMock.Setup(x => x.UpdateAsync(existingTeam))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        existingTeam.IsDeleted.Should().BeTrue();
        _teamRepositoryMock.Verify(x => x.GetByIdAsync(teamId), Times.Once);
        _teamRepositoryMock.Verify(x => x.UpdateAsync(It.Is<Team>(t => t.IsDeleted == true)), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidCommand_ShouldThrowValidationException()
    {
        // Arrange
        var command = new DeleteTeamCommand(Guid.Empty);

        // Act & Assert
        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .Where(ex => ex.Errors.Any(e => e.ErrorMessage == "Team ID must not be empty."));
        
        _teamRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _teamRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Team>()), Times.Never);
    }

    [Fact]
    public async Task Handle_TeamNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var command = new DeleteTeamCommand(teamId);
        
        _teamRepositoryMock.Setup(x => x.GetByIdAsync(teamId))
            .ReturnsAsync((Team?)null);

        // Act & Assert
        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{nameof(Team)}*{teamId}*");
        
        _teamRepositoryMock.Verify(x => x.GetByIdAsync(teamId), Times.Once);
        _teamRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Team>()), Times.Never);
    }

    [Fact]
    public async Task Handle_GetByIdAsyncThrowsException_ShouldPropagateException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var command = new DeleteTeamCommand(teamId);
        var expectedException = new Exception("Database connection failed");
        
        _teamRepositoryMock.Setup(x => x.GetByIdAsync(teamId))
            .ThrowsAsync(expectedException);

        // Act & Assert
        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("Database connection failed");
        
        _teamRepositoryMock.Verify(x => x.GetByIdAsync(teamId), Times.Once);
        _teamRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Team>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UpdateAsyncThrowsException_ShouldPropagateException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var command = new DeleteTeamCommand(teamId);
        var existingTeam = _teamFaker.Generate();
        var expectedException = new Exception("Database update failed");
        
        _teamRepositoryMock.Setup(x => x.GetByIdAsync(teamId))
            .ReturnsAsync(existingTeam);
        
        _teamRepositoryMock.Setup(x => x.UpdateAsync(existingTeam))
            .ThrowsAsync(expectedException);

        // Act & Assert
        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("Database update failed");
        
        _teamRepositoryMock.Verify(x => x.UpdateAsync(existingTeam), Times.Once);
    }

    [Fact]
    public async Task Handle_AlreadyDeletedTeam_ShouldStillUpdateSuccessfully()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var command = new DeleteTeamCommand(teamId);
        var existingTeam = _teamFaker.Generate();
        existingTeam.IsDeleted = true; // Already deleted
        
        _teamRepositoryMock.Setup(x => x.GetByIdAsync(teamId))
            .ReturnsAsync(existingTeam);
        
        _teamRepositoryMock.Setup(x => x.UpdateAsync(existingTeam))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        existingTeam.IsDeleted.Should().BeTrue();
        _teamRepositoryMock.Verify(x => x.UpdateAsync(It.Is<Team>(t => t.IsDeleted == true)), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidTeamId_ShouldCallRepositoryWithCorrectId()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var command = new DeleteTeamCommand(teamId);
        var existingTeam = _teamFaker.Generate();
        
        _teamRepositoryMock.Setup(x => x.GetByIdAsync(teamId))
            .ReturnsAsync(existingTeam);
        
        _teamRepositoryMock.Setup(x => x.UpdateAsync(existingTeam))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _teamRepositoryMock.Verify(x => x.GetByIdAsync(It.Is<Guid>(id => id == teamId)), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidationFails_ShouldNotCallRepository()
    {
        // Arrange
        var command = new DeleteTeamCommand(Guid.Empty);

        // Act & Assert
        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>();
        
        _teamRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _teamRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Team>()), Times.Never);
    }

    [Fact]
    public async Task Handle_MultipleValidationFailures_ShouldThrowValidationExceptionWithAllErrors()
    {
        // Arrange - Since DeleteTeamCommand only validates the ID, we can't easily create multiple validation failures
        // This test validates the single validation rule
        var command = new DeleteTeamCommand(Guid.Empty);

        // Act & Assert
        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .Where(ex => ex.Errors.Count() == 1 && 
                        ex.Errors.First().ErrorMessage == "Team ID must not be empty.");    }

    [Fact]
    public async Task Handle_SoftDelete_ShouldNotActuallyDeleteFromDatabase()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var command = new DeleteTeamCommand(teamId);
        var existingTeam = _teamFaker.Generate();
        
        _teamRepositoryMock.Setup(x => x.GetByIdAsync(teamId))
            .ReturnsAsync(existingTeam);
        
        _teamRepositoryMock.Setup(x => x.UpdateAsync(existingTeam))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        // Verify that UpdateAsync is called (soft delete) but DeleteAsync is never called
        _teamRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Team>()), Times.Once);
        _teamRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<Team>()), Times.Never);
    }
}
