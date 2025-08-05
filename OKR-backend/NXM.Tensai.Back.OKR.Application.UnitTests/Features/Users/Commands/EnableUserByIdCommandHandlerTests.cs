using ValidationException = FluentValidation.ValidationException;

namespace NXM.Tensai.Back.OKR.Application.UnitTests.Features.Users.Commands;

public class EnableUserByIdCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IValidator<EnableUserByIdCommand>> _validatorMock;
    private readonly EnableUserByIdCommandHandler _handler;
    private readonly Faker<User> _userFaker;
    private readonly Faker<UserDto> _userDtoFaker;

    public EnableUserByIdCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _validatorMock = new Mock<IValidator<EnableUserByIdCommand>>();
        _handler = new EnableUserByIdCommandHandler(_userRepositoryMock.Object, _validatorMock.Object);

        _userFaker = new Faker<User>()
            .RuleFor(x => x.Id, f => f.Random.Guid())
            .RuleFor(x => x.Email, f => f.Internet.Email())
            .RuleFor(x => x.FirstName, f => f.Name.FirstName())
            .RuleFor(x => x.LastName, f => f.Name.LastName())
            .RuleFor(x => x.UserName, f => f.Internet.UserName())
            .RuleFor(x => x.Address, f => f.Address.FullAddress())
            .RuleFor(x => x.Position, f => f.Name.JobTitle())
            .RuleFor(x => x.DateOfBirth, f => f.Date.Past(30))
            .RuleFor(x => x.IsEnabled, f => false) // Initially disabled
            .RuleFor(x => x.Gender, f => f.PickRandom<Gender>())
            .RuleFor(x => x.SupabaseId, f => f.Random.Guid().ToString())
            .RuleFor(x => x.ProfilePictureUrl, f => f.Internet.Avatar())
            .RuleFor(x => x.IsNotificationEnabled, f => f.Random.Bool())
            .RuleFor(x => x.OrganizationId, f => f.Random.Guid());

        _userDtoFaker = new Faker<UserDto>()
            .RuleFor(x => x.Id, f => f.Random.Guid())
            .RuleFor(x => x.Email, f => f.Internet.Email())
            .RuleFor(x => x.FirstName, f => f.Name.FirstName())
            .RuleFor(x => x.LastName, f => f.Name.LastName())
            .RuleFor(x => x.Address, f => f.Address.FullAddress())
            .RuleFor(x => x.Position, f => f.Name.JobTitle())
            .RuleFor(x => x.DateOfBirth, f => f.Date.Past(30))
            .RuleFor(x => x.IsEnabled, f => true) // Enabled after operation
            .RuleFor(x => x.Gender, f => f.PickRandom<Gender>())
            .RuleFor(x => x.SupabaseId, f => f.Random.Guid().ToString())
            .RuleFor(x => x.ProfilePictureUrl, f => f.Internet.Avatar())
            .RuleFor(x => x.IsNotificationEnabled, f => f.Random.Bool())
            .RuleFor(x => x.OrganizationId, f => f.Random.Guid());
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldEnableUserSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new EnableUserByIdCommand(userId);
        var existingUser = _userFaker.Generate();
        existingUser.IsEnabled = false; // Ensure user is initially disabled
        
        var validationResult = new FluentValidation.Results.ValidationResult();
        
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(existingUser);
        
        _userRepositoryMock.Setup(x => x.UpdateAsync(existingUser))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<UserDto>();
        
        _validatorMock.Verify(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);
        _userRepositoryMock.Verify(x => x.GetByIdAsync(userId), Times.Once);
        _userRepositoryMock.Verify(x => x.UpdateAsync(existingUser), Times.Once);
        
        // Verify user was enabled and modified date was updated
        existingUser.IsEnabled.Should().BeTrue();
        existingUser.ModifiedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Handle_InvalidCommand_ShouldThrowValidationException()
    {
        // Arrange
        var command = new EnableUserByIdCommand(Guid.Empty);
        
        var validationFailures = new List<FluentValidation.Results.ValidationFailure>
        {
            new("UserId", "User ID is required.")
        };
        var validationResult = new FluentValidation.Results.ValidationResult(validationFailures);
        
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act & Assert
        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .Where(ex => ex.Errors.Count() == 1);
        
        _validatorMock.Verify(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);
        _userRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UserNotFound_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new EnableUserByIdCommand(userId);
        
        var validationResult = new FluentValidation.Results.ValidationResult();
        
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<EntityNotFoundException>()
            .WithMessage($"User with ID {userId} not found.");
        
        _validatorMock.Verify(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);
        _userRepositoryMock.Verify(x => x.GetByIdAsync(userId), Times.Once);
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AlreadyEnabledUser_ShouldStillEnableAndUpdateModifiedDate()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new EnableUserByIdCommand(userId);
        var existingUser = _userFaker.Generate();
        existingUser.IsEnabled = true; // User is already enabled
        var originalModifiedDate = DateTime.UtcNow.AddDays(-1);
        existingUser.ModifiedDate = originalModifiedDate;
        
        var validationResult = new FluentValidation.Results.ValidationResult();
        
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(existingUser);
        
        _userRepositoryMock.Setup(x => x.UpdateAsync(existingUser))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<UserDto>();
        
        // Verify user remains enabled but modified date is updated
        existingUser.IsEnabled.Should().BeTrue();
        existingUser.ModifiedDate.Should().BeAfter(originalModifiedDate);
        existingUser.ModifiedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        
        _userRepositoryMock.Verify(x => x.UpdateAsync(existingUser), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidUserId_ShouldCallRepositoryWithCorrectId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new EnableUserByIdCommand(userId);
        var existingUser = _userFaker.Generate();
        
        var validationResult = new FluentValidation.Results.ValidationResult();
        
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(existingUser);
        
        _userRepositoryMock.Setup(x => x.UpdateAsync(existingUser))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _userRepositoryMock.Verify(x => x.GetByIdAsync(userId), Times.Once);
        _userRepositoryMock.Verify(x => x.GetByIdAsync(It.Is<Guid>(id => id == userId)), Times.Once);
    }

    [Theory]
    [InlineData("userRepository")]
    [InlineData("validator")]
    public void Constructor_WithNullDependencies_ShouldThrowArgumentNullException(string nullParam)
    {
        // Act & Assert
        if (nullParam == "userRepository")
        {
            var act = () => new EnableUserByIdCommandHandler(null!, _validatorMock.Object);
            act.Should().Throw<ArgumentNullException>().WithParameterName("userRepository");
        }
        else if (nullParam == "validator")
        {
            var act = () => new EnableUserByIdCommandHandler(_userRepositoryMock.Object, null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("validator");
        }
    }
}
