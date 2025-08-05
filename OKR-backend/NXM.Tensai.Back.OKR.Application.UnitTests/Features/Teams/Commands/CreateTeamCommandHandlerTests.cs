using Bogus;
using FluentAssertions;
using FluentValidation;
using ValidationException = FluentValidation.ValidationException;

namespace NXM.Tensai.Back.OKR.Application.UnitTests.Features.Teams.Commands;

public class CreateTeamCommandHandlerTests
{
    private readonly Mock<ITeamRepository> _teamRepositoryMock;
    private readonly Mock<ITeamUserRepository> _teamUserRepositoryMock;
    private readonly Mock<IValidator<CreateTeamCommand>> _validatorMock;
    private readonly CreateTeamCommandHandler _handler;
    private readonly Faker<CreateTeamCommand> _commandFaker;
    private readonly Faker<Team> _teamFaker;

    public CreateTeamCommandHandlerTests()
    {
        _teamRepositoryMock = new Mock<ITeamRepository>();
        _teamUserRepositoryMock = new Mock<ITeamUserRepository>();
        _validatorMock = new Mock<IValidator<CreateTeamCommand>>();
        _handler = new CreateTeamCommandHandler(_teamRepositoryMock.Object, _teamUserRepositoryMock.Object, _validatorMock.Object);

        _commandFaker = new Faker<CreateTeamCommand>()
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
    public async Task Handle_ValidCommand_ShouldCreateTeamSuccessfully()
    {
        // Arrange
        var command = _commandFaker.Generate();
        var createdTeam = _teamFaker.Generate();
        
        var validationResult = new FluentValidation.Results.ValidationResult();
        
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        
        _teamRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Team>()))
            .ReturnsAsync(createdTeam);
        
        _teamUserRepositoryMock.Setup(x => x.AddAsync(It.IsAny<TeamUser>()))
            .ReturnsAsync((TeamUser teamUser) => teamUser);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _validatorMock.Verify(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);
        _teamRepositoryMock.Verify(x => x.AddAsync(It.Is<Team>(t => 
            t.Name == command.Name &&
            t.OrganizationId == command.OrganizationId &&
            t.TeamManagerId == command.TeamManagerId &&
            t.Description == command.Description)), Times.Once);
        _teamUserRepositoryMock.Verify(x => x.AddAsync(It.Is<TeamUser>(tu =>
            tu.UserId == command.TeamManagerId)), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommandWithoutManager_ShouldCreateTeamWithInviteMessage()
    {
        // Arrange
        var baseCommand = _commandFaker.Generate();
        var command = new CreateTeamCommand
        {
            Name = baseCommand.Name,
            Description = baseCommand.Description,
            OrganizationId = baseCommand.OrganizationId,
            TeamManagerId = null
        };
        var createdTeam = _teamFaker.Generate();
        
        var validationResult = new FluentValidation.Results.ValidationResult();
        
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        
        _teamRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Team>()))
            .ReturnsAsync(createdTeam);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _teamRepositoryMock.Verify(x => x.AddAsync(It.Is<Team>(t => 
            t.Description.Contains("Manager is invited, still didn't accept invite."))), Times.Once);
        _teamUserRepositoryMock.Verify(x => x.AddAsync(It.IsAny<TeamUser>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InvalidCommand_ShouldThrowValidationException()
    {
        // Arrange
        var command = _commandFaker.Generate();
        var validationFailures = new List<FluentValidation.Results.ValidationFailure>
        {
            new("Name", "Team name is required."),
            new("OrganizationId", "Organization ID is required.")
        };
        var validationResult = new FluentValidation.Results.ValidationResult(validationFailures);
        
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act & Assert
        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .Where(ex => ex.Errors.Count() == 2);
        
        _validatorMock.Verify(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);
        _teamRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Team>()), Times.Never);
        _teamUserRepositoryMock.Verify(x => x.AddAsync(It.IsAny<TeamUser>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RepositoryThrowsException_ShouldPropagateException()
    {
        // Arrange
        var command = _commandFaker.Generate();
        var validationResult = new FluentValidation.Results.ValidationResult();
        var expectedException = new Exception("Database connection failed");
        
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        
        _teamRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Team>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("Database connection failed");
        
        _teamRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Team>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyDescriptionWithoutManager_ShouldAppendInviteMessage()
    {
        // Arrange
        var baseCommand = _commandFaker.Generate();
        var command = new CreateTeamCommand
        {
            Name = baseCommand.Name,
            Description = "", // Empty description
            OrganizationId = baseCommand.OrganizationId,
            TeamManagerId = null
        };
        var createdTeam = _teamFaker.Generate();
        
        var validationResult = new FluentValidation.Results.ValidationResult();
        
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        
        _teamRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Team>()))
            .ReturnsAsync(createdTeam);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _teamRepositoryMock.Verify(x => x.AddAsync(It.Is<Team>(t => 
            t.Description == "Manager is invited, still didn't accept invite.")), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingDescriptionWithoutManager_ShouldAppendInviteMessageWithSpace()
    {
        // Arrange
        var baseCommand = _commandFaker.Generate();
        var originalDescription = "This is a test team";
        var command = new CreateTeamCommand
        {
            Name = baseCommand.Name,
            Description = originalDescription,
            OrganizationId = baseCommand.OrganizationId,
            TeamManagerId = null
        };
        var createdTeam = _teamFaker.Generate();
        
        var validationResult = new FluentValidation.Results.ValidationResult();
        
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        
        _teamRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Team>()))
            .ReturnsAsync(createdTeam);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _teamRepositoryMock.Verify(x => x.AddAsync(It.Is<Team>(t => 
            t.Description == $"{originalDescription} Manager is invited, still didn't accept invite.")), Times.Once);
    }

    [Fact]
    public async Task Handle_TeamUserRepositoryThrowsException_ShouldPropagateException()
    {
        // Arrange
        var command = _commandFaker.Generate();
        var createdTeam = _teamFaker.Generate();
        var validationResult = new FluentValidation.Results.ValidationResult();
        var expectedException = new Exception("TeamUser insertion failed");
        
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        
        _teamRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Team>()))
            .ReturnsAsync(createdTeam);
        
        _teamUserRepositoryMock.Setup(x => x.AddAsync(It.IsAny<TeamUser>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("TeamUser insertion failed");
        
        _teamRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Team>()), Times.Once);
        _teamUserRepositoryMock.Verify(x => x.AddAsync(It.IsAny<TeamUser>()), Times.Once);    }
}
