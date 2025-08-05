using Bogus;
using FluentAssertions;
using FluentValidation;
using ValidationException = FluentValidation.ValidationException;

namespace NXM.Tensai.Back.OKR.Application.UnitTests.Features.Teams.Commands;

public class UpdateTeamCommandHandlerTests
{
    private readonly Mock<ITeamRepository> _teamRepositoryMock;
    private readonly Mock<IValidator<UpdateTeamCommand>> _validatorMock;
    private readonly UpdateTeamCommandHandler _handler;
    private readonly Faker<UpdateTeamCommand> _commandFaker;
    private readonly Faker<Team> _teamFaker;

    public UpdateTeamCommandHandlerTests()
    {
        _teamRepositoryMock = new Mock<ITeamRepository>();
        _validatorMock = new Mock<IValidator<UpdateTeamCommand>>();
        _handler = new UpdateTeamCommandHandler(_teamRepositoryMock.Object, _validatorMock.Object);

        _commandFaker = new Faker<UpdateTeamCommand>()
            .RuleFor(x => x.Name, f => f.Company.CompanyName())
            .RuleFor(x => x.Description, f => f.Lorem.Sentence())
            .RuleFor(x => x.OrganizationId, f => f.Random.Guid())
            .RuleFor(x => x.TeamManagerId, f => f.Random.Guid());

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
    public async Task Handle_ValidCommand_ShouldUpdateTeamSuccessfully()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var command = _commandFaker.Generate();
        var requestWithId = new UpdateTeamCommandWithId(teamId, command);
        var existingTeam = _teamFaker.Generate();
        
        var validationResult = new FluentValidation.Results.ValidationResult();
        
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        
        _teamRepositoryMock.Setup(x => x.GetByIdAsync(teamId))
            .ReturnsAsync(existingTeam);
        
        _teamRepositoryMock.Setup(x => x.UpdateAsync(existingTeam))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(requestWithId, CancellationToken.None);

        // Assert
        _validatorMock.Verify(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);
        _teamRepositoryMock.Verify(x => x.GetByIdAsync(teamId), Times.Once);
        _teamRepositoryMock.Verify(x => x.UpdateAsync(It.Is<Team>(t =>
            t.Name == command.Name &&
            t.Description == command.Description &&
            t.OrganizationId == command.OrganizationId &&
            t.TeamManagerId == command.TeamManagerId &&
            t.ModifiedDate.HasValue)), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidCommand_ShouldThrowValidationException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var command = _commandFaker.Generate();
        var requestWithId = new UpdateTeamCommandWithId(teamId, command);
        var validationFailures = new List<FluentValidation.Results.ValidationFailure>
        {
            new("Name", "Team name is required."),
            new("OrganizationId", "Organization ID is required.")
        };
        var validationResult = new FluentValidation.Results.ValidationResult(validationFailures);
        
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act & Assert
        await _handler.Invoking(x => x.Handle(requestWithId, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .Where(ex => ex.Errors.Count() == 2);
        
        _validatorMock.Verify(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);
        _teamRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _teamRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Team>()), Times.Never);
    }

    [Fact]
    public async Task Handle_TeamNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var command = _commandFaker.Generate();
        var requestWithId = new UpdateTeamCommandWithId(teamId, command);
        
        var validationResult = new FluentValidation.Results.ValidationResult();
        
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        
        _teamRepositoryMock.Setup(x => x.GetByIdAsync(teamId))
            .ReturnsAsync((Team?)null);

        // Act & Assert
        await _handler.Invoking(x => x.Handle(requestWithId, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{nameof(Team)}*{teamId}*");
        
        _teamRepositoryMock.Verify(x => x.GetByIdAsync(teamId), Times.Once);
        _teamRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Team>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UpdateAsyncThrowsException_ShouldPropagateException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var command = _commandFaker.Generate();
        var requestWithId = new UpdateTeamCommandWithId(teamId, command);
        var existingTeam = _teamFaker.Generate();
        var expectedException = new Exception("Database update failed");
        
        var validationResult = new FluentValidation.Results.ValidationResult();
        
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        
        _teamRepositoryMock.Setup(x => x.GetByIdAsync(teamId))
            .ReturnsAsync(existingTeam);
        
        _teamRepositoryMock.Setup(x => x.UpdateAsync(existingTeam))
            .ThrowsAsync(expectedException);

        // Act & Assert
        await _handler.Invoking(x => x.Handle(requestWithId, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("Database update failed");
        
        _teamRepositoryMock.Verify(x => x.UpdateAsync(existingTeam), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullTeamManagerId_ShouldUpdateSuccessfully()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var baseCommand = _commandFaker.Generate();
        var command = new UpdateTeamCommand
        {
            Name = baseCommand.Name,
            Description = baseCommand.Description,
            OrganizationId = baseCommand.OrganizationId,
            TeamManagerId = null
        };
        var requestWithId = new UpdateTeamCommandWithId(teamId, command);
        var existingTeam = _teamFaker.Generate();
        
        var validationResult = new FluentValidation.Results.ValidationResult();
        
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        
        _teamRepositoryMock.Setup(x => x.GetByIdAsync(teamId))
            .ReturnsAsync(existingTeam);
        
        _teamRepositoryMock.Setup(x => x.UpdateAsync(existingTeam))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(requestWithId, CancellationToken.None);

        // Assert
        _teamRepositoryMock.Verify(x => x.UpdateAsync(It.Is<Team>(t =>
            t.TeamManagerId == null)), Times.Once);
    }

    [Fact]
    public async Task Handle_TeamMapper_ShouldUpdateAllProperties()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var command = _commandFaker.Generate();
        var requestWithId = new UpdateTeamCommandWithId(teamId, command);
        var existingTeam = _teamFaker.Generate();
        var originalCreatedDate = existingTeam.CreatedDate;
        
        var validationResult = new FluentValidation.Results.ValidationResult();
        
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        
        _teamRepositoryMock.Setup(x => x.GetByIdAsync(teamId))
            .ReturnsAsync(existingTeam);
        
        _teamRepositoryMock.Setup(x => x.UpdateAsync(existingTeam))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(requestWithId, CancellationToken.None);

        // Assert
        existingTeam.Name.Should().Be(command.Name);
        existingTeam.Description.Should().Be(command.Description);
        existingTeam.OrganizationId.Should().Be(command.OrganizationId);
        existingTeam.TeamManagerId.Should().Be(command.TeamManagerId);
        existingTeam.ModifiedDate.Should().NotBeNull();
        existingTeam.ModifiedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        existingTeam.CreatedDate.Should().Be(originalCreatedDate); // Should not change
    }

    [Fact]
    public async Task Handle_ValidTeamId_ShouldCallRepositoryWithCorrectId()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var command = _commandFaker.Generate();
        var requestWithId = new UpdateTeamCommandWithId(teamId, command);
        var existingTeam = _teamFaker.Generate();
        
        var validationResult = new FluentValidation.Results.ValidationResult();
        
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        
        _teamRepositoryMock.Setup(x => x.GetByIdAsync(teamId))
            .ReturnsAsync(existingTeam);
        
        _teamRepositoryMock.Setup(x => x.UpdateAsync(existingTeam))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(requestWithId, CancellationToken.None);        // Assert
        _teamRepositoryMock.Verify(x => x.GetByIdAsync(It.Is<Guid>(id => id == teamId)), Times.Once);
    }
}
