using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ValidationException = FluentValidation.ValidationException;

namespace NXM.Tensai.Back.OKR.Application.UnitTests.Features.Users.Commands;

public class CreateUserCommandHandlerTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IValidator<CreateUserCommand>> _validatorMock;
    private readonly CreateUserCommandHandler _handler;
    private readonly Faker<CreateUserCommand> _commandFaker;
    private readonly Faker<User> _userFaker;
    private readonly Faker<UserDto> _userDtoFaker;

    public CreateUserCommandHandlerTests()
    {
        _userManagerMock = MockUserManager();
        _userRepositoryMock = new Mock<IUserRepository>();
        _validatorMock = new Mock<IValidator<CreateUserCommand>>();
        _handler = new CreateUserCommandHandler(_userManagerMock.Object, _userRepositoryMock.Object, _validatorMock.Object);

        _commandFaker = new Faker<CreateUserCommand>()
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
            .RuleFor(x => x.Password, f => f.Internet.Password())
            .RuleFor(x => x.ConfirmPassword, (f, cmd) => cmd.Password)
            .RuleFor(x => x.Role, f => f.PickRandom<RoleType>());

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
            .RuleFor(x => x.SupabaseId, f => f.Random.Guid().ToString());

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
            .RuleFor(x => x.SupabaseId, f => f.Random.Guid().ToString());
    }

    private static Mock<UserManager<User>> MockUserManager()
    {
        var store = new Mock<IUserStore<User>>();
        var mgr = new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
        mgr.Object.UserValidators.Add(new UserValidator<User>());
        mgr.Object.PasswordValidators.Add(new PasswordValidator<User>());
        return mgr;
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateUserSuccessfully()
    {
        // Arrange
        var command = _commandFaker.Generate();
        var createdUser = _userFaker.Generate();
        var userDto = _userDtoFaker.Generate();
        
        var validationResult = new FluentValidation.Results.ValidationResult();
        
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        
        _userRepositoryMock.Setup(x => x.GetUserByEmailAsync(command.Email))
            .ReturnsAsync((User?)null);
        
        _userRepositoryMock.Setup(x => x.AddAsync(It.IsAny<User>()))
            .ReturnsAsync(createdUser);
        
        _userManagerMock.Setup(x => x.AddPasswordAsync(It.IsAny<User>(), command.Password))
            .ReturnsAsync(IdentityResult.Success);
        
        _userManagerMock.Setup(x => x.AddToRoleAsync(createdUser, command.Role.ToString()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<UserDto>();
        
        _validatorMock.Verify(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);
        _userRepositoryMock.Verify(x => x.GetUserByEmailAsync(command.Email), Times.Once);
        _userRepositoryMock.Verify(x => x.AddAsync(It.Is<User>(u => 
            u.Email == command.Email && 
            u.FirstName == command.FirstName && 
            u.LastName == command.LastName)), Times.Once);
        _userManagerMock.Verify(x => x.AddPasswordAsync(It.IsAny<User>(), command.Password), Times.Once);
        _userManagerMock.Verify(x => x.AddToRoleAsync(createdUser, command.Role.ToString()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidCommand_ShouldThrowValidationException()
    {
        // Arrange
        var command = _commandFaker.Generate();
        var validationFailures = new List<FluentValidation.Results.ValidationFailure>
        {
            new("FirstName", "First name is required."),
            new("Email", "Invalid email format.")
        };
        var validationResult = new FluentValidation.Results.ValidationResult(validationFailures);
        
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act & Assert
        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .Where(ex => ex.Errors.Count() == 2);
        
        _validatorMock.Verify(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);
        _userRepositoryMock.Verify(x => x.GetUserByEmailAsync(It.IsAny<string>()), Times.Never);
        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExistingUser_ShouldThrowUserCreationException()
    {
        // Arrange
        var command = _commandFaker.Generate();
        var existingUser = _userFaker.Generate();
        
        var validationResult = new FluentValidation.Results.ValidationResult();
        
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        
        _userRepositoryMock.Setup(x => x.GetUserByEmailAsync(command.Email))
            .ReturnsAsync(existingUser);

        // Act & Assert
        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<UserCreationException>()
            .WithMessage($"Username '{command.Email}' is already taken.");
        
        _validatorMock.Verify(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);
        _userRepositoryMock.Verify(x => x.GetUserByEmailAsync(command.Email), Times.Once);
        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PasswordAdditionFails_ShouldStillProceed()
    {
        // Arrange
        var command = _commandFaker.Generate();
        var createdUser = _userFaker.Generate();
        
        var validationResult = new FluentValidation.Results.ValidationResult();
        
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        
        _userRepositoryMock.Setup(x => x.GetUserByEmailAsync(command.Email))
            .ReturnsAsync((User?)null);
        
        _userRepositoryMock.Setup(x => x.AddAsync(It.IsAny<User>()))
            .ReturnsAsync(createdUser);
        
        _userManagerMock.Setup(x => x.AddPasswordAsync(It.IsAny<User>(), command.Password))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password too weak" }));
        
        _userManagerMock.Setup(x => x.AddToRoleAsync(createdUser, command.Role.ToString()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _userManagerMock.Verify(x => x.AddPasswordAsync(It.IsAny<User>(), command.Password), Times.Once);
        _userManagerMock.Verify(x => x.AddToRoleAsync(createdUser, command.Role.ToString()), Times.Once);
    }

    [Fact]
    public async Task Handle_RoleAssignmentFails_ShouldThrowRoleAssignmentException()
    {
        // Arrange
        var command = _commandFaker.Generate();
        var createdUser = _userFaker.Generate();
        
        var validationResult = new FluentValidation.Results.ValidationResult();
        
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        
        _userRepositoryMock.Setup(x => x.GetUserByEmailAsync(command.Email))
            .ReturnsAsync((User?)null);
        
        _userRepositoryMock.Setup(x => x.AddAsync(It.IsAny<User>()))
            .ReturnsAsync(createdUser);
        
        _userManagerMock.Setup(x => x.AddPasswordAsync(It.IsAny<User>(), command.Password))
            .ReturnsAsync(IdentityResult.Success);
        
        _userManagerMock.Setup(x => x.AddToRoleAsync(createdUser, command.Role.ToString()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Role assignment failed" }));

        // Act & Assert
        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<RoleAssignmentException>();
        
        _userManagerMock.Verify(x => x.AddToRoleAsync(createdUser, command.Role.ToString()), Times.Once);
    }    [Fact]
    public async Task Handle_WithSupabaseId_ShouldSetSupabaseId()
    {
        // Arrange
        var baseCommand = _commandFaker.Generate();
        var command = new CreateUserCommand
        {
            Email = baseCommand.Email,
            FirstName = baseCommand.FirstName,
            LastName = baseCommand.LastName,
            UserName = baseCommand.UserName,
            Address = baseCommand.Address,
            Position = baseCommand.Position,
            DateOfBirth = baseCommand.DateOfBirth,
            IsEnabled = baseCommand.IsEnabled,
            Gender = baseCommand.Gender,
            Password = baseCommand.Password,
            ConfirmPassword = baseCommand.ConfirmPassword,
            Role = baseCommand.Role,
            SupabaseId = "supabase-123"
        };
        var createdUser = _userFaker.Generate();
        
        var validationResult = new FluentValidation.Results.ValidationResult();
        
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        
        _userRepositoryMock.Setup(x => x.GetUserByEmailAsync(command.Email))
            .ReturnsAsync((User?)null);
        
        _userRepositoryMock.Setup(x => x.AddAsync(It.IsAny<User>()))
            .ReturnsAsync(createdUser);
        
        _userManagerMock.Setup(x => x.AddPasswordAsync(It.IsAny<User>(), command.Password))
            .ReturnsAsync(IdentityResult.Success);
        
        _userManagerMock.Setup(x => x.AddToRoleAsync(createdUser, command.Role.ToString()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _userRepositoryMock.Verify(x => x.AddAsync(It.Is<User>(u => u.SupabaseId == "supabase-123")), Times.Once);
    }    [Fact]
    public async Task Handle_WithoutSupabaseId_ShouldSetEmptySupabaseId()
    {
        // Arrange
        var baseCommand = _commandFaker.Generate();
        var command = new CreateUserCommand
        {
            Email = baseCommand.Email,
            FirstName = baseCommand.FirstName,
            LastName = baseCommand.LastName,
            UserName = baseCommand.UserName,
            Address = baseCommand.Address,
            Position = baseCommand.Position,
            DateOfBirth = baseCommand.DateOfBirth,
            IsEnabled = baseCommand.IsEnabled,
            Gender = baseCommand.Gender,
            Password = baseCommand.Password,
            ConfirmPassword = baseCommand.ConfirmPassword,
            Role = baseCommand.Role,
            SupabaseId = null
        };
        var createdUser = _userFaker.Generate();
        
        var validationResult = new FluentValidation.Results.ValidationResult();
        
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        
        _userRepositoryMock.Setup(x => x.GetUserByEmailAsync(command.Email))
            .ReturnsAsync((User?)null);
        
        _userRepositoryMock.Setup(x => x.AddAsync(It.IsAny<User>()))
            .ReturnsAsync(createdUser);
        
        _userManagerMock.Setup(x => x.AddPasswordAsync(It.IsAny<User>(), command.Password))
            .ReturnsAsync(IdentityResult.Success);
        
        _userManagerMock.Setup(x => x.AddToRoleAsync(createdUser, command.Role.ToString()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _userRepositoryMock.Verify(x => x.AddAsync(It.Is<User>(u => u.SupabaseId == string.Empty)), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Constructor_WithNullOrInvalidDependencies_ShouldThrowArgumentNullException(string? nullParam)
    {
        // Act & Assert
        if (nullParam == "userRepository")
        {
            var act = () => new CreateUserCommandHandler(_userManagerMock.Object, null!, _validatorMock.Object);
            act.Should().Throw<ArgumentNullException>().WithParameterName("userRepository");
        }
        else if (nullParam == "validator")
        {
            var act = () => new CreateUserCommandHandler(_userManagerMock.Object, _userRepositoryMock.Object, null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("validator");
        }
    }
}
