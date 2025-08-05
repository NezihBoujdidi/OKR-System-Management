using Bogus;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Moq;
using NXM.Tensai.Back.OKR.Application.Common.Exceptions;
using NXM.Tensai.Back.OKR.Domain;
using NXM.Tensai.Back.OKR.Domain.Entities;
using NXM.Tensai.Back.OKR.Domain.Interfaces.Repositories;
using Xunit;
using ValidationException = FluentValidation.ValidationException;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace NXM.Tensai.Back.OKR.Application.UnitTests.Features.Accounts.Commands;

public class RegisterUserCommandHandlerTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<RoleManager<Role>> _roleManagerMock;
    private readonly Mock<IValidator<RegisterUserCommand>> _validatorMock;
    private readonly Mock<ITeamRepository> _teamRepositoryMock;
    private readonly Mock<ITeamUserRepository> _teamUserRepositoryMock;
    private readonly RegisterUserCommandHandler _handler;
    private readonly Faker _faker;

    public RegisterUserCommandHandlerTests()
    {
        _userManagerMock = new Mock<UserManager<User>>(
            Mock.Of<IUserStore<User>>(), null, null, null, null, null, null, null, null);
        _roleManagerMock = new Mock<RoleManager<Role>>(
            Mock.Of<IRoleStore<Role>>(), null, null, null, null);
        _validatorMock = new Mock<IValidator<RegisterUserCommand>>();
        _teamRepositoryMock = new Mock<ITeamRepository>();
        _teamUserRepositoryMock = new Mock<ITeamUserRepository>();
        _handler = new RegisterUserCommandHandler(
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _validatorMock.Object,
            _teamRepositoryMock.Object,
            _teamUserRepositoryMock.Object);
        _faker = new Faker();
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateUserAndReturnId()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var command = new RegisterUserCommand
        {
            SupabaseId = _faker.Random.Guid().ToString(),
            Email = _faker.Internet.Email(),
            FirstName = _faker.Name.FirstName(),
            LastName = _faker.Name.LastName(),
            Address = _faker.Address.FullAddress(),
            DateOfBirth = _faker.Date.Past(30, DateTime.Now.AddYears(-18)),
            Gender = _faker.PickRandom<Gender>(),
            PhoneNumber = _faker.Phone.PhoneNumber(),
            Position = _faker.Name.JobTitle(),
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            RoleName = RoleType.Collaborator.ToString(),
            IsEnabled = true,
            OrganizationID = organizationId
        };

        var role = new Role { Name = command.RoleName };
        var validationResult = new ValidationResult();

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync((User?)null);
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), command.Password))
            .ReturnsAsync(IdentityResult.Success);
        _roleManagerMock.Setup(x => x.FindByNameAsync(command.RoleName))
            .ReturnsAsync(role);
        _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), command.RoleName))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBe(Guid.Empty);
        _validatorMock.Verify(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);
        _userManagerMock.Verify(x => x.FindByEmailAsync(command.Email), Times.Once);
        _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<User>(), command.Password), Times.Once);
        _roleManagerMock.Verify(x => x.FindByNameAsync(command.RoleName), Times.Once);
        _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<User>(), command.RoleName), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidationFails_ShouldThrowValidationException()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Email = "", // Invalid email
            FirstName = "", // Invalid first name
            Password = "weak", // Weak password
            ConfirmPassword = "different" // Password mismatch
        };

        var validationErrors = new List<ValidationFailure>
        {
            new ValidationFailure("Email", "Email is required."),
            new ValidationFailure("FirstName", "First name is required."),
            new ValidationFailure("ConfirmPassword", "Passwords do not match.")
        };
        var validationResult = new ValidationResult(validationErrors);
        
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => 
            _handler.Handle(command, CancellationToken.None));

        exception.Errors.Should().HaveCount(3);
        _validatorMock.Verify(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);
        _userManagerMock.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UserAlreadyExists_ShouldThrowUserCreationException()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Email = _faker.Internet.Email(),
            FirstName = _faker.Name.FirstName(),
            LastName = _faker.Name.LastName(),
            RoleName = RoleType.Collaborator.ToString()
        };

        var existingUser = new User { Email = command.Email };
        var validationResult = new ValidationResult();

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(existingUser);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UserCreationException>(() => 
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("User with this email already exists");
        _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_OrganizationAdminRole_ShouldSetIsEnabledToFalse()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            SupabaseId = _faker.Random.Guid().ToString(),
            Email = _faker.Internet.Email(),
            FirstName = _faker.Name.FirstName(),
            LastName = _faker.Name.LastName(),
            RoleName = RoleType.OrganizationAdmin.ToString(),
            Password = "Password123!"
        };

        var role = new Role { Name = command.RoleName };
        var validationResult = new ValidationResult();

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync((User?)null);
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), command.Password))
            .ReturnsAsync(IdentityResult.Success);
        _roleManagerMock.Setup(x => x.FindByNameAsync(command.RoleName))
            .ReturnsAsync(role);
        _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), command.RoleName))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBe(Guid.Empty);
        _userManagerMock.Verify(x => x.CreateAsync(It.Is<User>(u => u.IsEnabled == false), command.Password), Times.Once);
    }

    [Fact]
    public async Task Handle_NonOrganizationAdminRole_ShouldSetIsEnabledToTrue()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            SupabaseId = _faker.Random.Guid().ToString(),
            Email = _faker.Internet.Email(),
            FirstName = _faker.Name.FirstName(),
            LastName = _faker.Name.LastName(),
            RoleName = RoleType.Collaborator.ToString(),
            Password = "Password123!"
        };

        var role = new Role { Name = command.RoleName };
        var validationResult = new ValidationResult();

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync((User?)null);
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), command.Password))
            .ReturnsAsync(IdentityResult.Success);
        _roleManagerMock.Setup(x => x.FindByNameAsync(command.RoleName))
            .ReturnsAsync(role);
        _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), command.RoleName))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBe(Guid.Empty);
        _userManagerMock.Verify(x => x.CreateAsync(It.Is<User>(u => u.IsEnabled == true), command.Password), Times.Once);
    }

    [Fact]
    public async Task Handle_WithTeamId_ShouldAddUserToTeam()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var command = new RegisterUserCommand
        {
            SupabaseId = _faker.Random.Guid().ToString(),
            Email = _faker.Internet.Email(),
            FirstName = _faker.Name.FirstName(),
            LastName = _faker.Name.LastName(),
            RoleName = RoleType.Collaborator.ToString(),
            Password = "Password123!",
            TeamId = teamId,
            OrganizationID = organizationId
        };

        var team = new Team
        {
            Id = teamId,
            OrganizationId = organizationId,
            Name = _faker.Company.CompanyName()
        };
        var role = new Role { Name = command.RoleName };
        var validationResult = new ValidationResult();

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync((User?)null);
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), command.Password))
            .ReturnsAsync(IdentityResult.Success);
        _roleManagerMock.Setup(x => x.FindByNameAsync(command.RoleName))
            .ReturnsAsync(role);
        _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), command.RoleName))
            .ReturnsAsync(IdentityResult.Success);
        _teamRepositoryMock.Setup(x => x.GetByIdAsync(teamId))
            .ReturnsAsync(team);
        _teamUserRepositoryMock.Setup(x => x.GetByTeamAndUserIdAsync(teamId, It.IsAny<Guid>()))
            .ReturnsAsync((TeamUser?)null);
        _teamUserRepositoryMock.Setup(x => x.AddAsync(It.IsAny<TeamUser>()))
            .ReturnsAsync((TeamUser tu) => tu);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBe(Guid.Empty);
        _teamRepositoryMock.Verify(x => x.GetByIdAsync(teamId), Times.Once);
        _teamUserRepositoryMock.Verify(x => x.AddAsync(It.Is<TeamUser>(tu => tu.TeamId == teamId)), Times.Once);
    }

    [Fact]
    public async Task Handle_TeamNotFound_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var command = new RegisterUserCommand
        {
            Email = _faker.Internet.Email(),
            RoleName = RoleType.Collaborator.ToString(),
            TeamId = teamId
        };

        var role = new Role { Name = command.RoleName };
        var validationResult = new ValidationResult();

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync((User?)null);
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _roleManagerMock.Setup(x => x.FindByNameAsync(command.RoleName))
            .ReturnsAsync(role);
        _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), command.RoleName))
            .ReturnsAsync(IdentityResult.Success);
        _teamRepositoryMock.Setup(x => x.GetByIdAsync(teamId))
            .ReturnsAsync((Team?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() => 
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain($"Team with ID {teamId} not found");
        _teamUserRepositoryMock.Verify(x => x.AddAsync(It.IsAny<TeamUser>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RoleNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Email = _faker.Internet.Email(),
            RoleName = "NonExistentRole"
        };

        var validationResult = new ValidationResult();

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync((User?)null);
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _roleManagerMock.Setup(x => x.FindByNameAsync(command.RoleName))
            .ReturnsAsync((Role?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain($"Role with name {command.RoleName} not found");
        _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UserCreationFails_ShouldThrowUserCreationException()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Email = _faker.Internet.Email(),
            RoleName = RoleType.Collaborator.ToString(),
            Password = "Password123!"
        };

        var validationResult = new ValidationResult();
        var createResult = IdentityResult.Failed(new IdentityError { Description = "Password too weak" });

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync((User?)null);
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), command.Password))
            .ReturnsAsync(createResult);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UserCreationException>(() => 
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("Password too weak");
        _roleManagerMock.Verify(x => x.FindByNameAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RoleAssignmentFails_ShouldThrowRoleAssignmentException()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Email = _faker.Internet.Email(),
            RoleName = RoleType.Collaborator.ToString(),
            Password = "Password123!"
        };

        var role = new Role { Name = command.RoleName };
        var validationResult = new ValidationResult();
        var roleResult = IdentityResult.Failed(new IdentityError { Description = "Role assignment failed" });

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync((User?)null);
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), command.Password))
            .ReturnsAsync(IdentityResult.Success);
        _roleManagerMock.Setup(x => x.FindByNameAsync(command.RoleName))
            .ReturnsAsync(role);
        _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), command.RoleName))
            .ReturnsAsync(roleResult);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<RoleAssignmentException>(() => 
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain($"Failed to assign role {command.RoleName} to user");
    }
}
