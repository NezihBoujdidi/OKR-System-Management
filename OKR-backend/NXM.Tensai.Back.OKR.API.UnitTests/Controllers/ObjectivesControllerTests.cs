namespace NXM.Tensai.Back.OKR.API.UnitTests.Controllers;

public class ObjectivesControllerTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ILogger<ObjectivesController>> _mockLogger;
    private readonly ObjectivesController _controller;
    private readonly Faker<CreateObjectiveCommand> _commandFaker;

    public ObjectivesControllerTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<ObjectivesController>>();
        _controller = new ObjectivesController(_mockMediator.Object, _mockLogger.Object);

        _commandFaker = new Faker<CreateObjectiveCommand>()
            .RuleFor(c => c.OKRSessionId, f => f.Random.Guid())
            .RuleFor(c => c.UserId, f => f.Random.Guid())
            .RuleFor(c => c.Title, f => f.Lorem.Sentence(3))
            .RuleFor(c => c.Description, f => f.Lorem.Sentence())
            .RuleFor(c => c.StartedDate, f => f.Date.Recent())
            .RuleFor(c => c.EndDate, f => f.Date.Future())
            .RuleFor(c => c.Status, f => f.PickRandom<Status>())
            .RuleFor(c => c.Priority, f => f.PickRandom<Priority>())
            .RuleFor(c => c.ResponsibleTeamId, f => f.Random.Guid());
    }

    [Fact]
    public async Task CreateObjective_WithValidCommand_Should_ReturnOkResult()
    {
        // Arrange
        var command = _commandFaker.Generate();
        var expectedId = Guid.NewGuid();

        _mockMediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedId);

        // Act
        var result = await _controller.CreateObjective(command);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be("Objective created successfully.");
    }

    [Fact]
    public async Task CreateObjective_WithValidationException_Should_ReturnBadRequestResult()
    {
        // Arrange
        var command = _commandFaker.Generate();
        var validationFailure = new FluentValidation.Results.ValidationFailure("Title", "Title is required");
        var validationException = new FluentValidation.ValidationException(new[] { validationFailure });

        _mockMediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ThrowsAsync(validationException);

        // Act
        var result = await _controller.CreateObjective(command);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateObjective_WithGeneralException_Should_ReturnInternalServerError()
    {
        // Arrange
        var command = _commandFaker.Generate();
        var exception = new Exception("Something went wrong");

        _mockMediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.CreateObjective(command);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task CreateObjective_Should_LogInformation_When_Called()
    {
        // Arrange
        var command = _commandFaker.Generate();
        var expectedId = Guid.NewGuid();

        _mockMediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedId);

        // Act
        await _controller.CreateObjective(command);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CreateObjective attempt")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateObjective_Should_LogSuccess_When_Successful()
    {
        // Arrange
        var command = _commandFaker.Generate();
        var expectedId = Guid.NewGuid();

        _mockMediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedId);

        // Act
        await _controller.CreateObjective(command);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CreateObjective successful")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateObjective_Should_LogWarning_When_ValidationFails()
    {
        // Arrange
        var command = _commandFaker.Generate();
        var validationFailure = new FluentValidation.Results.ValidationFailure("Title", "Title is required");
        var validationException = new FluentValidation.ValidationException(new[] { validationFailure });

        _mockMediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ThrowsAsync(validationException);

        // Act
        await _controller.CreateObjective(command);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Validation failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateObjective_Should_LogError_When_ExceptionOccurs()
    {
        // Arrange
        var command = _commandFaker.Generate();
        var exception = new Exception("Something went wrong");

        _mockMediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        await _controller.CreateObjective(command);        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("An unexpected error occurred")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
