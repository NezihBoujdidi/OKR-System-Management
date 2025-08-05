namespace NXM.Tensai.Back.OKR.Application.UnitTests.Features.Objectives.Commands;

public class CreateObjectiveCommandHandlerTests
{
    private readonly Mock<IObjectiveRepository> _mockObjectiveRepository;
    private readonly Mock<IValidator<CreateObjectiveCommand>> _mockValidator;
    private readonly Mock<IOKRSessionRepository> _mockOKRSessionRepository;
    private readonly CreateObjectiveCommandHandler _handler;
    private readonly Faker<CreateObjectiveCommand> _commandFaker;
    private readonly Faker<Objective> _objectiveFaker;
    private readonly Faker<OKRSession> _okrSessionFaker;

    public CreateObjectiveCommandHandlerTests()
    {
        _mockObjectiveRepository = new Mock<IObjectiveRepository>();
        _mockValidator = new Mock<IValidator<CreateObjectiveCommand>>();
        _mockOKRSessionRepository = new Mock<IOKRSessionRepository>();
        _handler = new CreateObjectiveCommandHandler(_mockObjectiveRepository.Object, _mockValidator.Object, _mockOKRSessionRepository.Object);

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

        _objectiveFaker = new Faker<Objective>()
            .RuleFor(o => o.Id, f => f.Random.Guid())
            .RuleFor(o => o.Title, f => f.Lorem.Sentence(3))
            .RuleFor(o => o.Description, f => f.Lorem.Sentence())
            .RuleFor(o => o.Status, f => f.PickRandom<Status>())
            .RuleFor(o => o.Priority, f => f.PickRandom<Priority>())
            .RuleFor(o => o.StartedDate, f => f.Date.Recent())
            .RuleFor(o => o.EndDate, f => f.Date.Future())
            .RuleFor(o => o.OKRSessionId, f => f.Random.Guid())
            .RuleFor(o => o.UserId, f => f.Random.Guid())
            .RuleFor(o => o.ResponsibleTeamId, f => f.Random.Guid())
            .RuleFor(o => o.Progress, f => f.Random.Int(0, 100));

        _okrSessionFaker = new Faker<OKRSession>()
            .RuleFor(s => s.Id, f => f.Random.Guid())
            .RuleFor(s => s.Title, f => f.Lorem.Sentence(3))
            .RuleFor(s => s.Description, f => f.Lorem.Sentence())
            .RuleFor(s => s.Status, f => f.PickRandom<Status>())
            .RuleFor(s => s.Priority, f => f.PickRandom<Priority>())
            .RuleFor(s => s.StartedDate, f => f.Date.Recent())
            .RuleFor(s => s.EndDate, f => f.Date.Future());
    }

    [Fact]
    public async Task Handle_WithValidCommand_Should_ReturnObjectiveId()
    {
        // Arrange
        var command = _commandFaker.Generate();
        var validationResult = new FluentValidation.Results.ValidationResult();
        var expectedObjective = command.ToEntity();

        _mockValidator.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _mockObjectiveRepository.Setup(r => r.AddAsync(It.IsAny<Objective>()))
            .ReturnsAsync(expectedObjective);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _mockObjectiveRepository.Verify(r => r.AddAsync(It.IsAny<Objective>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidCommand_Should_ThrowValidationException()
    {
        // Arrange
        var command = _commandFaker.Generate();
        var validationFailure = new FluentValidation.Results.ValidationFailure("Title", "Title is required");
        var validationResult = new FluentValidation.Results.ValidationResult(new[] { validationFailure });

        _mockValidator.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act & Assert
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => 
            _handler.Handle(command, CancellationToken.None));

        _mockObjectiveRepository.Verify(r => r.AddAsync(It.IsAny<Objective>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_UpdateOKRSessionProgress_When_SessionExists()
    {
        // Arrange
        var command = _commandFaker.Generate();
        var validationResult = new FluentValidation.Results.ValidationResult();
        var okrSession = _okrSessionFaker.Generate();
        var objectives = _objectiveFaker.Generate(3);

        _mockValidator.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _mockOKRSessionRepository.Setup(r => r.GetByIdAsync(command.OKRSessionId))
            .ReturnsAsync(okrSession);

        _mockObjectiveRepository.Setup(r => r.GetBySessionIdAsync(command.OKRSessionId))
            .ReturnsAsync(objectives);

        _mockObjectiveRepository.Setup(r => r.AddAsync(It.IsAny<Objective>()))
            .ReturnsAsync(_objectiveFaker.Generate());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockOKRSessionRepository.Verify(r => r.GetByIdAsync(command.OKRSessionId), Times.Once);
        _mockObjectiveRepository.Verify(r => r.GetBySessionIdAsync(command.OKRSessionId), Times.Once);
        _mockOKRSessionRepository.Verify(r => r.UpdateAsync(okrSession), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_NotUpdateOKRSessionProgress_When_SessionDoesNotExist()
    {
        // Arrange
        var command = _commandFaker.Generate();
        var validationResult = new FluentValidation.Results.ValidationResult();

        _mockValidator.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _mockOKRSessionRepository.Setup(r => r.GetByIdAsync(command.OKRSessionId))
            .ReturnsAsync((OKRSession?)null);

        _mockObjectiveRepository.Setup(r => r.AddAsync(It.IsAny<Objective>()))
            .ReturnsAsync(_objectiveFaker.Generate());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockOKRSessionRepository.Verify(r => r.GetByIdAsync(command.OKRSessionId), Times.Once);
        _mockObjectiveRepository.Verify(r => r.GetBySessionIdAsync(It.IsAny<Guid>()), Times.Never);
        _mockOKRSessionRepository.Verify(r => r.UpdateAsync(It.IsAny<OKRSession>()), Times.Never);
    }
}
