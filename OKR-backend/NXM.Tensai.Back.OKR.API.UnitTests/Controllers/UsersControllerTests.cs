using Bogus;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Language.Flow;
using NXM.Tensai.Back.OKR.Application.Common.Exceptions;
using NXM.Tensai.Back.OKR.Application;
using NXM.Tensai.Back.OKR.Domain;
using NXM.Tensai.Back.OKR.Domain.Enums;
using ValidationException = NXM.Tensai.Back.OKR.Application.Common.Exceptions.ValidationException;

namespace NXM.Tensai.Back.OKR.API.UnitTests.Controllers;

public class UsersControllerTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ILogger<UsersController>> _mockLogger;
    private readonly UsersController _controller;    
    private readonly Faker<CreateUserCommand> _createUserFaker;
    private readonly Faker<UpdateUserCommand> _updateUserFaker;
    private readonly Faker<UserDto> _userDtoFaker;
    private readonly Faker<UserWithRoleDto> _userWithRoleDtoFaker;

    public UsersControllerTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<UsersController>>();
        _controller = new UsersController(_mockMediator.Object, _mockLogger.Object);        // Setup fakers for consistent test data generation
        _createUserFaker = new Faker<CreateUserCommand>()
            .RuleFor(c => c.FirstName, f => f.Name.FirstName())
            .RuleFor(c => c.LastName, f => f.Name.LastName())
            .RuleFor(c => c.UserName, f => f.Internet.UserName())
            .RuleFor(c => c.Email, f => f.Internet.Email())
            .RuleFor(c => c.SupabaseId, f => f.Random.AlphaNumeric(28))
            .RuleFor(c => c.Password, f => f.Internet.Password())
            .RuleFor(c => c.ConfirmPassword, (f, u) => u.Password)
            .RuleFor(c => c.Role, f => RoleType.Collaborator)
            .RuleFor(c => c.DateOfBirth, f => f.Date.Past(30))
            .RuleFor(c => c.Address, f => f.Address.FullAddress())
            .RuleFor(c => c.Position, f => f.Name.JobTitle())
            .RuleFor(c => c.IsEnabled, f => true)
            .RuleFor(c => c.Gender, f => f.PickRandom<Gender>());        _updateUserFaker = new Faker<UpdateUserCommand>()
            .RuleFor(c => c.FirstName, f => f.Name.FirstName())
            .RuleFor(c => c.LastName, f => f.Name.LastName())
            .RuleFor(c => c.Email, f => f.Internet.Email())
            .RuleFor(c => c.Address, f => f.Address.FullAddress())
            .RuleFor(c => c.Position, f => f.Name.JobTitle())
            .RuleFor(c => c.DateOfBirth, f => f.Date.Past(30))
            .RuleFor(c => c.ProfilePictureUrl, f => f.Internet.Avatar())
            .RuleFor(c => c.IsNotificationEnabled, f => f.Random.Bool())
            .RuleFor(c => c.IsEnabled, f => true)
            .RuleFor(c => c.Gender, f => f.PickRandom<Gender>())
            .RuleFor(c => c.OrganizationId, f => f.Random.Guid());        _userDtoFaker = new Faker<UserDto>()
            .RuleFor(u => u.Id, f => f.Random.Guid())
            .RuleFor(u => u.SupabaseId, f => f.Random.AlphaNumeric(28))
            .RuleFor(u => u.FirstName, f => f.Name.FirstName())
            .RuleFor(u => u.LastName, f => f.Name.LastName())
            .RuleFor(u => u.Email, f => f.Internet.Email())
            .RuleFor(u => u.OrganizationId, f => f.Random.Guid())
            .RuleFor(u => u.DateOfBirth, f => f.Date.Past(30))
            .RuleFor(u => u.Address, f => f.Address.FullAddress())
            .RuleFor(u => u.Position, f => f.Name.JobTitle())
            .RuleFor(u => u.IsEnabled, f => true)            .RuleFor(u => u.IsNotificationEnabled, f => f.Random.Bool())
            .RuleFor(u => u.Gender, f => f.PickRandom<Gender>())
            .RuleFor(u => u.ProfilePictureUrl, f => f.Internet.Avatar());
            
        _userWithRoleDtoFaker = new Faker<UserWithRoleDto>()
            .RuleFor(u => u.Id, f => f.Random.Guid())
            .RuleFor(u => u.SupabaseId, f => f.Random.AlphaNumeric(28))
            .RuleFor(u => u.FirstName, f => f.Name.FirstName())
            .RuleFor(u => u.LastName, f => f.Name.LastName())
            .RuleFor(u => u.Email, f => f.Internet.Email())
            .RuleFor(u => u.OrganizationId, f => f.Random.Guid())
            .RuleFor(u => u.DateOfBirth, f => f.Date.Past(30))
            .RuleFor(u => u.Address, f => f.Address.FullAddress())
            .RuleFor(u => u.Position, f => f.Name.JobTitle())
            .RuleFor(u => u.IsEnabled, f => true)
            .RuleFor(u => u.IsNotificationEnabled, f => f.Random.Bool())
            .RuleFor(u => u.Gender, f => f.PickRandom<Gender>())
            .RuleFor(u => u.ProfilePictureUrl, f => f.Internet.Avatar())
            .RuleFor(u => u.Role, f => "Collaborator")
            .RuleFor(u => u.CreatedDate, f => f.Date.Past(1))
            .RuleFor(u => u.ModifiedDate, f => f.Date.Recent());
    }

    #region CreateUser Tests    
    
    [Fact]
    public async Task CreateUser_WithValidCommand_Should_ReturnOkResult()
    {
        // Arrange
        var command = _createUserFaker.Generate();
        var expectedUserDto = _userDtoFaker.Generate();

        _mockMediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUserDto);

        // Act
        var result = await _controller.CreateUser(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be(expectedUserDto);
        
        // Verify mediator was called with the expected command
        _mockMediator.Verify(m => m.Send(command, It.IsAny<CancellationToken>()), Times.Once);
    }    [Fact]
    public async Task CreateUser_WithValidationException_Should_ReturnBadRequestResult()
    {
        // Arrange
        var command = _createUserFaker.Generate();
        var validationErrors = new Dictionary<string, string[]>
        {
            { "Email", new[] { "Email is required", "Email format is invalid" } },
            { "FirstName", new[] { "FirstName is required" } }
        };
        
        // Create the validation exception with the specific constructor that takes a dictionary
        var validationException = new ValidationException(validationErrors);

        // Setup the mock to throw the exception when Send is called
        _mockMediator
            .Setup(m => m.Send(It.IsAny<CreateUserCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(validationException);
        
        // Act
        var result = await _controller.CreateUser(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.StatusCode.Should().Be(400); // Verify status code is 400 Bad Request
        badRequestResult.Value.Should().BeEquivalentTo(validationErrors); // Check that the errors are returned
        
        // Verify mediator was called
        _mockMediator.Verify(m => m.Send(It.IsAny<CreateUserCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateUser_WithRoleAssignmentException_Should_ReturnInternalServerError()
    {
        // Arrange
        var command = _createUserFaker.Generate();
        var errorMessage = "Failed to assign role to user";
        var roleAssignmentException = new RoleAssignmentException(errorMessage);

        _mockMediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ThrowsAsync(roleAssignmentException);

        // Act
        var result = await _controller.CreateUser(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = result as ObjectResult;
        statusCodeResult!.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be(errorMessage);
        
        // Verify mediator was called with the expected command
        _mockMediator.Verify(m => m.Send(command, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region UpdateUser Tests

    [Fact]
    public async Task UpdateUser_WithValidCommand_Should_ReturnOkResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = _updateUserFaker.Generate();
        var expectedUser = _userDtoFaker.Generate();
        expectedUser.Id = userId;

        _mockMediator.Setup(m => m.Send(It.IsAny<UpdateUserCommandWithId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _controller.UpdateUser(userId, command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedUser);
          // Verify mediator was called with the correct parameter
        _mockMediator.Verify(m => m.Send(
            It.Is<UpdateUserCommandWithId>(cmd => 
                cmd.Id == userId && 
                cmd.Command == command),
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }    [Fact]
    public async Task UpdateUser_WithValidationException_Should_ReturnBadRequestResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = _updateUserFaker.Generate();
        var validationErrors = new Dictionary<string, string[]>
        {
            { "Email", new[] { "Email format is invalid" } }
        };
        
        // Create the validation exception with the specific constructor that takes a dictionary
        var validationException = new ValidationException(validationErrors);

        // Setup the mock to throw the exception when Send is called with any UpdateUserCommandWithId
        _mockMediator
            .Setup(m => m.Send(It.IsAny<UpdateUserCommandWithId>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(validationException);
        
        // Act
        var result = await _controller.UpdateUser(userId, command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.StatusCode.Should().Be(400);
        badRequestResult.Value.Should().BeEquivalentTo(validationErrors);
    }

    [Fact]    public async Task UpdateUser_WithEntityNotFoundException_Should_ReturnNotFoundResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = _updateUserFaker.Generate();
        var errorMessage = $"User with ID {userId} not found";
        var entityNotFoundException = new EntityNotFoundException(errorMessage);

        _mockMediator.Setup(m => m.Send(It.IsAny<UpdateUserCommandWithId>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(entityNotFoundException);

        // Act
        var result = await _controller.UpdateUser(userId, command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.Value.Should().Be(entityNotFoundException.Message);
    }

    #endregion

    #region GetUserById Tests    
    
    [Fact]
    public async Task GetUserById_WithValidId_Should_ReturnOkResultWithUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedUser = _userWithRoleDtoFaker.Generate();
        expectedUser.Id = userId;

        _mockMediator.Setup(m => m.Send(It.IsAny<GetUserByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _controller.GetUserById(userId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedUser);
        
        // Verify mediator was called with the correct query
        _mockMediator.Verify(m => m.Send(
            It.Is<GetUserByIdQuery>(q => q.Id == userId),
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task GetUserById_WithNonExistentId_Should_ReturnNotFoundResult()
    {        // Arrange
        var userId = Guid.NewGuid();
        var errorMessage = $"User with ID {userId} not found";
        var entityNotFoundException = new EntityNotFoundException(errorMessage);

        _mockMediator.Setup(m => m.Send(It.IsAny<GetUserByIdQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(entityNotFoundException);

        // Act
        var result = await _controller.GetUserById(userId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.Value.Should().Be(entityNotFoundException.Message);
    }

    #endregion

    #region GetUserByEmail Tests

    [Fact]
    public async Task GetUserByEmail_WithValidEmail_Should_ReturnOkResultWithUser()
    {        // Arrange
        var email = "test@example.com";
        var expectedUser = _userWithRoleDtoFaker.Generate();
        expectedUser.Email = email;

        _mockMediator.Setup(m => m.Send(It.IsAny<GetUserByEmailQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _controller.GetUserByEmail(email, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedUser);
        
        // Verify mediator was called with the correct query
        _mockMediator.Verify(m => m.Send(
            It.Is<GetUserByEmailQuery>(q => q.Email == email),
            It.IsAny<CancellationToken>()),            Times.Once);
    }    [Fact]
    public async Task GetUserByEmail_WithInvalidEmail_Should_ReturnBadRequestResult()
    {
        // Arrange
        var invalidEmail = "not-an-email";
        var validationErrors = new Dictionary<string, string[]>
        {
            { "Email", new[] { "Invalid email format" } }
        };
        var validationException = new ValidationException(validationErrors);

        _mockMediator.Setup(m => m.Send(It.IsAny<GetUserByEmailQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(validationException);

        // Act
        var result = await _controller.GetUserByEmail(invalidEmail, CancellationToken.None);
        
        // Assert
        // First check that it's of type IActionResult (parent type)
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.StatusCode.Should().Be(400);
        badRequestResult.Value.Should().BeEquivalentTo(validationErrors);
    }

    [Fact]
    public async Task GetUserByEmail_WithNonExistentEmail_Should_ReturnNotFoundResult()
    {        // Arrange
        var email = "nonexistent@example.com";
        var errorMessage = $"User with email {email} not found";
        var entityNotFoundException = new EntityNotFoundException(errorMessage);

        _mockMediator.Setup(m => m.Send(It.IsAny<GetUserByEmailQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(entityNotFoundException);

        // Act
        var result = await _controller.GetUserByEmail(email, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.Value.Should().Be(entityNotFoundException.Message);
    }

    [Fact]
    public async Task GetUserByEmail_WithUnexpectedException_Should_ReturnInternalServerError()
    {
        // Arrange
        var email = "test@example.com";
        var exception = new Exception("Unexpected database error");

        _mockMediator.Setup(m => m.Send(It.IsAny<GetUserByEmailQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.GetUserByEmail(email, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = result as ObjectResult;
        statusCodeResult!.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("An unexpected error occurred.");
    }

    #endregion

    #region Disable/Enable User Tests

    [Fact]
    public async Task DisableUserById_WithValidId_Should_ReturnOkResultWithDisabledUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var disabledUser = _userDtoFaker.Generate();
        disabledUser.Id = userId;
        disabledUser.IsEnabled = false;

        _mockMediator.Setup(m => m.Send(It.IsAny<DisableUserByIdCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(disabledUser);

        // Act
        var result = await _controller.DisableUserById(userId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(disabledUser);
        
        // Verify mediator was called with the correct command
        _mockMediator.Verify(m => m.Send(
            It.Is<DisableUserByIdCommand>(c => c.UserId == userId),
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task EnableUserById_WithValidId_Should_ReturnOkResultWithEnabledUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var enabledUser = _userDtoFaker.Generate();
        enabledUser.Id = userId;
        enabledUser.IsEnabled = true;

        _mockMediator.Setup(m => m.Send(It.IsAny<EnableUserByIdCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(enabledUser);

        // Act
        var result = await _controller.EnableUserById(userId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(enabledUser);
        
        // Verify mediator was called with the correct command
        _mockMediator.Verify(m => m.Send(
            It.Is<EnableUserByIdCommand>(c => c.UserId == userId),
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    #endregion

    #region GetUsersByOrganizationId Tests

    [Fact]
    public async Task GetUsersByOrganizationId_WithValidId_Should_ReturnOkResultWithUsers()
    {        // Arrange
        var organizationId = Guid.NewGuid();
        var expectedUsers = _userWithRoleDtoFaker.Generate(5).ToList();
        
        foreach (var user in expectedUsers)
        {
            user.OrganizationId = organizationId;
        }

        _mockMediator.Setup(m => m.Send(It.IsAny<GetUsersByOrganizationIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUsers);

        // Act
        var result = await _controller.GetUsersByOrganizationId(organizationId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedUsers);
        
        // Verify mediator was called with the correct query
        _mockMediator.Verify(m => m.Send(
            It.Is<GetUsersByOrganizationIdQuery>(q => q.OrganizationId == organizationId),
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    #endregion

    #region GetOrganizationAdminEmail Tests

    [Fact]
    public async Task GetOrganizationAdminEmail_WithValidId_Should_ReturnOkResultWithEmail()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var expectedEmail = "admin@example.com";

        _mockMediator.Setup(m => m.Send(It.IsAny<GetOrganizationAdminEmailQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEmail);

        // Act
        var result = await _controller.GetOrganizationAdminEmail(organizationId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be(expectedEmail);
        
        // Verify mediator was called with the correct query
        _mockMediator.Verify(m => m.Send(
            It.Is<GetOrganizationAdminEmailQuery>(q => q.OrganizationId == organizationId),
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task GetOrganizationAdminEmail_WithNonExistentId_Should_ReturnNotFoundResult()
    {
        // Arrange
        var organizationId = Guid.NewGuid();        
        var errorMessage = $"Organization with ID {organizationId} not found";
        var entityNotFoundException = new EntityNotFoundException(errorMessage);

        _mockMediator.Setup(m => m.Send(It.IsAny<GetOrganizationAdminEmailQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(entityNotFoundException);

        // Act
        var result = await _controller.GetOrganizationAdminEmail(organizationId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.Value.Should().Be(entityNotFoundException.Message);
    }

    #endregion
}
