using ValidationException = FluentValidation.ValidationException;

namespace NXM.Tensai.Back.OKR.Application.UnitTests.Features.Users.Commands;

public class UpdateUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IValidator<UpdateUserCommand>> _validatorMock;
    private readonly UpdateUserCommandHandler _handler;
    private readonly Faker<UpdateUserCommand> _commandFaker;
    private readonly Faker<User> _userFaker;
    private readonly Faker<UserDto> _userDtoFaker;

    public UpdateUserCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _validatorMock = new Mock<IValidator<UpdateUserCommand>>();
        _handler = new UpdateUserCommandHandler(_userRepositoryMock.Object, _validatorMock.Object);

        _commandFaker = new Faker<UpdateUserCommand>()
            .RuleFor(x => x.Email, f => f.Internet.Email())
            .RuleFor(x => x.FirstName, f => f.Name.FirstName())
            .RuleFor(x => x.LastName, f => f.Name.LastName())
            .RuleFor(x => x.Address, f => f.Address.FullAddress())
            .RuleFor(x => x.Position, f => f.Name.JobTitle())
            .RuleFor(x => x.DateOfBirth, f => f.Date.Past(30))
            .RuleFor(x => x.ProfilePictureUrl, f => f.Internet.Avatar())
            .RuleFor(x => x.IsNotificationEnabled, f => f.Random.Bool())
            .RuleFor(x => x.IsEnabled, f => f.Random.Bool())
            .RuleFor(x => x.Gender, f => f.PickRandom<Gender>())
            .RuleFor(x => x.OrganizationId, f => f.Random.Guid());

        _userFaker = new Faker<User>()
            .RuleFor(x => x.Id, f => f.Random.Guid())
            .RuleFor(x => x.Email, f => f.Internet.Email())
            .RuleFor(x => x.FirstName, f => f.Name.FirstName())
            .RuleFor(x => x.LastName, f => f.Name.LastName())
            .RuleFor(x => x.UserName, f => f.Internet.UserName())
            .RuleFor(x => x.Address, f => f.Address.FullAddress())
            .RuleFor(x => x.Position, f => f.Name.JobTitle())
            .RuleFor(x => x.DateOfBirth, f => f.Date.Past(30))
            .RuleFor(x => x.IsEnabled, f => f.Random.Bool())
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
            .RuleFor(x => x.IsEnabled, f => f.Random.Bool())
            .RuleFor(x => x.Gender, f => f.PickRandom<Gender>())
            .RuleFor(x => x.SupabaseId, f => f.Random.Guid().ToString())
            .RuleFor(x => x.ProfilePictureUrl, f => f.Internet.Avatar())
            .RuleFor(x => x.IsNotificationEnabled, f => f.Random.Bool())
            .RuleFor(x => x.OrganizationId, f => f.Random.Guid());
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldUpdateUserSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = _commandFaker.Generate();
        var requestWithId = new UpdateUserCommandWithId(userId, command);
        var existingUser = _userFaker.Generate();
        var userDto = _userDtoFaker.Generate();
        
        var validationResult = new FluentValidation.Results.ValidationResult();
        
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(existingUser);
        
        _userRepositoryMock.Setup(x => x.UpdateAsync(existingUser))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(requestWithId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<UserDto>();
        
        _validatorMock.Verify(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);
        _userRepositoryMock.Verify(x => x.GetByIdAsync(userId), Times.Once);
        _userRepositoryMock.Verify(x => x.UpdateAsync(existingUser), Times.Once);
        
        // Verify user properties were updated
        existingUser.FirstName.Should().Be(command.FirstName);
        existingUser.LastName.Should().Be(command.LastName);
        existingUser.Email.Should().Be(command.Email);
        existingUser.Address.Should().Be(command.Address);
        existingUser.Position.Should().Be(command.Position);
        existingUser.IsEnabled.Should().Be(command.IsEnabled);
        existingUser.Gender.Should().Be(command.Gender);
        existingUser.OrganizationId.Should().Be(command.OrganizationId);
        existingUser.ModifiedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Handle_InvalidCommand_ShouldThrowValidationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = _commandFaker.Generate();
        var requestWithId = new UpdateUserCommandWithId(userId, command);
        
        var validationFailures = new List<FluentValidation.Results.ValidationFailure>
        {
            new("FirstName", "First name is required."),
            new("Email", "Invalid email format.")
        };
        var validationResult = new FluentValidation.Results.ValidationResult(validationFailures);
        
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act & Assert
        await _handler.Invoking(x => x.Handle(requestWithId, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .Where(ex => ex.Errors.Count() == 2);
        
        _validatorMock.Verify(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);
        _userRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UserNotFound_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = _commandFaker.Generate();
        var requestWithId = new UpdateUserCommandWithId(userId, command);
        
        var validationResult = new FluentValidation.Results.ValidationResult();
        
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await _handler.Invoking(x => x.Handle(requestWithId, CancellationToken.None))
            .Should().ThrowAsync<EntityNotFoundException>()
            .WithMessage($"User with ID {userId} not found.");
        
        _validatorMock.Verify(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);
        _userRepositoryMock.Verify(x => x.GetByIdAsync(userId), Times.Once);
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DateOfBirthInLocalTime_ShouldConvertToUtc()
    {        // Arrange
        var userId = Guid.NewGuid();
        var localDateTime = new DateTime(1990, 5, 15, 10, 30, 0, DateTimeKind.Local);
        var baseCommand = _commandFaker.Generate();
        var command = new UpdateUserCommand
        {
            FirstName = baseCommand.FirstName,
            LastName = baseCommand.LastName,
            Email = baseCommand.Email,
            Address = baseCommand.Address,
            Position = baseCommand.Position,
            DateOfBirth = localDateTime,
            ProfilePictureUrl = baseCommand.ProfilePictureUrl,
            IsNotificationEnabled = baseCommand.IsNotificationEnabled,
            IsEnabled = baseCommand.IsEnabled,
            Gender = baseCommand.Gender,
            OrganizationId = baseCommand.OrganizationId
        };
        var requestWithId = new UpdateUserCommandWithId(userId, command);
        var existingUser = _userFaker.Generate();
        
        var validationResult = new FluentValidation.Results.ValidationResult();
        
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(existingUser);
        
        _userRepositoryMock.Setup(x => x.UpdateAsync(existingUser))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(requestWithId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        existingUser.DateOfBirth.Kind.Should().Be(DateTimeKind.Utc);
        existingUser.DateOfBirth.Should().Be(DateTime.SpecifyKind(localDateTime, DateTimeKind.Utc));
    }

    [Fact]
    public async Task Handle_DateOfBirthInUtc_ShouldRemainUtc()
    {        // Arrange
        var userId = Guid.NewGuid();
        var utcDateTime = new DateTime(1990, 5, 15, 10, 30, 0, DateTimeKind.Utc);
        var baseCommand = _commandFaker.Generate();
        var command = new UpdateUserCommand
        {
            FirstName = baseCommand.FirstName,
            LastName = baseCommand.LastName,
            Email = baseCommand.Email,
            Address = baseCommand.Address,
            Position = baseCommand.Position,
            DateOfBirth = utcDateTime,
            ProfilePictureUrl = baseCommand.ProfilePictureUrl,
            IsNotificationEnabled = baseCommand.IsNotificationEnabled,
            IsEnabled = baseCommand.IsEnabled,
            Gender = baseCommand.Gender,
            OrganizationId = baseCommand.OrganizationId
        };
        var requestWithId = new UpdateUserCommandWithId(userId, command);
        var existingUser = _userFaker.Generate();
        
        var validationResult = new FluentValidation.Results.ValidationResult();
        
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(existingUser);
        
        _userRepositoryMock.Setup(x => x.UpdateAsync(existingUser))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(requestWithId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        existingUser.DateOfBirth.Kind.Should().Be(DateTimeKind.Utc);
        existingUser.DateOfBirth.Should().Be(utcDateTime);
    }

    [Fact]
    public async Task Handle_UpdateUserName_ShouldSetUserNameToEmail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = _commandFaker.Generate();
        var requestWithId = new UpdateUserCommandWithId(userId, command);
        var existingUser = _userFaker.Generate();
        
        var validationResult = new FluentValidation.Results.ValidationResult();
        
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(existingUser);
        
        _userRepositoryMock.Setup(x => x.UpdateAsync(existingUser))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(requestWithId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        existingUser.UserName.Should().Be(command.Email);
        existingUser.Email.Should().Be(command.Email);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Constructor_WithNullOrInvalidDependencies_ShouldThrowArgumentNullException(string? nullParam)
    {
        // Act & Assert
        if (nullParam == "userRepository")
        {
            var act = () => new UpdateUserCommandHandler(null!, _validatorMock.Object);
            act.Should().Throw<ArgumentNullException>().WithParameterName("userRepository");
        }
        else if (nullParam == "validator")
        {
            var act = () => new UpdateUserCommandHandler(_userRepositoryMock.Object, null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("validator");
        }
    }
}
