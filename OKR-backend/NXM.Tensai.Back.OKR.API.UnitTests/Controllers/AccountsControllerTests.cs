using Bogus;
using FluentAssertions;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NXM.Tensai.Back.OKR.API.Controllers;
using NXM.Tensai.Back.OKR.Application;
using NXM.Tensai.Back.OKR.Application.Common.Exceptions;
using NXM.Tensai.Back.OKR.Domain;
using NXM.Tensai.Back.OKR.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using ValidationException = FluentValidation.ValidationException;
using UserCreationException = NXM.Tensai.Back.OKR.Application.UserCreationException;

namespace NXM.Tensai.Back.OKR.API.UnitTests.Controllers;

public class AccountsControllerTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ILogger<AccountsController>> _mockLogger;
    private readonly AccountsController _controller;
    private readonly Faker _faker;

    // Command Fakers
    private readonly Faker<RegisterUserCommand> _registerUserCommandFaker;
    private readonly Faker<LoginUserCommand> _loginUserCommandFaker;
    private readonly Faker<GenerateInvitationLinkCommand> _generateInvitationLinkCommandFaker;
    private readonly Faker<ForgotPasswordCommand> _forgotPasswordCommandFaker;
    private readonly Faker<ResetPasswordCommand> _resetPasswordCommandFaker;
    private readonly Faker<ConfirmEmailCommand> _confirmEmailCommandFaker;
    private readonly Faker<RefreshTokenCommand> _refreshTokenCommandFaker;

    // Query Fakers
    private readonly Faker<ValidateKeyQuery> _validateKeyQueryFaker;

    // Response Fakers
    private readonly Faker<LoginResponse> _loginResponseFaker;
    private readonly Faker<RefreshTokenResponse> _refreshTokenResponseFaker;
    private readonly Faker<ValidateKeyDto> _validateKeyDtoFaker;

    public AccountsControllerTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<AccountsController>>();
        _controller = new AccountsController(_mockMediator.Object, _mockLogger.Object);
        _faker = new Faker();

        // Initialize command fakers
        _registerUserCommandFaker = new Faker<RegisterUserCommand>()
            .RuleFor(x => x.SupabaseId, f => f.Random.Guid().ToString())
            .RuleFor(x => x.Email, f => f.Internet.Email())
            .RuleFor(x => x.FirstName, f => f.Name.FirstName())
            .RuleFor(x => x.LastName, f => f.Name.LastName())
            .RuleFor(x => x.Address, f => f.Address.FullAddress())
            .RuleFor(x => x.DateOfBirth, f => f.Date.Past(50, DateTime.Now.AddYears(-18)))
            .RuleFor(x => x.Gender, f => f.PickRandom<Gender>())
            .RuleFor(x => x.PhoneNumber, f => f.Phone.PhoneNumber())
            .RuleFor(x => x.Position, f => f.Name.JobTitle())
            .RuleFor(x => x.Password, f => f.Internet.Password())
            .RuleFor(x => x.ConfirmPassword, (f, cmd) => cmd.Password)
            .RuleFor(x => x.RoleName, f => f.PickRandom("OrganizationAdmin", "TeamManager", "Collaborator"))
            .RuleFor(x => x.IsEnabled, f => f.Random.Bool())
            .RuleFor(x => x.TeamId, f => f.Random.Bool() ? f.Random.Guid() : null)
            .RuleFor(x => x.OrganizationID, f => f.Random.Guid());

        _loginUserCommandFaker = new Faker<LoginUserCommand>()
            .RuleFor(x => x.Email, f => f.Internet.Email())
            .RuleFor(x => x.SupabaseId, f => f.Random.Guid().ToString());

        _generateInvitationLinkCommandFaker = new Faker<GenerateInvitationLinkCommand>()
            .RuleFor(x => x.Email, f => f.Internet.Email())
            .RuleFor(x => x.RoleName, f => f.PickRandom("OrganizationAdmin", "TeamManager", "Collaborator"))
            .RuleFor(x => x.OrganizationId, f => f.Random.Guid())
            .RuleFor(x => x.TeamId, f => f.Random.Bool() ? f.Random.Guid() : null);

        _forgotPasswordCommandFaker = new Faker<ForgotPasswordCommand>()
            .RuleFor(x => x.Email, f => f.Internet.Email());

        _resetPasswordCommandFaker = new Faker<ResetPasswordCommand>()
            .RuleFor(x => x.Email, f => f.Internet.Email())
            .RuleFor(x => x.Token, f => f.Random.String(50))
            .RuleFor(x => x.Password, f => f.Internet.Password())
            .RuleFor(x => x.ConfirmPassword, (f, cmd) => cmd.Password);

        _confirmEmailCommandFaker = new Faker<ConfirmEmailCommand>()
            .RuleFor(x => x.UserId, f => f.Random.Guid().ToString())
            .RuleFor(x => x.Token, f => f.Random.String(50));

        _refreshTokenCommandFaker = new Faker<RefreshTokenCommand>()
            .RuleFor(x => x.Token, f => f.Random.String(100));

        _validateKeyQueryFaker = new Faker<ValidateKeyQuery>()
            .RuleFor(x => x.Key, f => f.Random.String(50));

        // Initialize response fakers
        _loginResponseFaker = new Faker<LoginResponse>()
            .RuleFor(x => x.Token, f => f.Random.String(200))
            .RuleFor(x => x.RefreshToken, f => f.Random.String(100))
            .RuleFor(x => x.Expires, f => f.Date.Future());

        _refreshTokenResponseFaker = new Faker<RefreshTokenResponse>()
            .RuleFor(x => x.Token, f => f.Random.String(200))
            .RuleFor(x => x.RefreshToken, f => f.Random.String(100))
            .RuleFor(x => x.Expires, f => f.Date.Future());        _validateKeyDtoFaker = new Faker<ValidateKeyDto>()
            .RuleFor(x => x.ExpirationDate, f => f.Date.Future())
            .RuleFor(x => x.Token, f => f.Random.String(50));
    }

    #region Register Tests

    [Fact]
    public async Task Register_WithValidCommand_Should_ReturnOkResultWithUserId()
    {
        // Arrange
        var command = _registerUserCommandFaker.Generate();
        var userId = _faker.Random.Guid();

        _mockMediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);

        // Act
        var result = await _controller.Register(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(new { UserId = userId });
    }

    [Fact]
    public async Task Register_WithValidationException_Should_ReturnBadRequestResult()
    {
        // Arrange
        var command = _registerUserCommandFaker.Generate();
        var validationFailures = new List<ValidationFailure>
        {
            new ValidationFailure("Email", "Email is required"),
            new ValidationFailure("Password", "Password is required")
        };
        var validationException = new ValidationException(validationFailures);        _mockMediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ThrowsAsync(validationException);

        // Act
        var result = await _controller.Register(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        
        // The controller returns ex.Errors which is IList<ValidationFailure>
        badRequestResult!.Value.Should().BeEquivalentTo(validationFailures);
    }

    [Fact]
    public async Task Register_WithUserCreationException_Should_ReturnBadRequestResult()
    {
        // Arrange
        var command = _registerUserCommandFaker.Generate();
        var errorMessage = "User with this email already exists";
        var userCreationException = new UserCreationException(errorMessage);

        _mockMediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ThrowsAsync(userCreationException);

        // Act
        var result = await _controller.Register(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().Be(errorMessage);
    }

    [Fact]
    public async Task Register_WithRoleAssignmentException_Should_ReturnInternalServerError()
    {
        // Arrange
        var command = _registerUserCommandFaker.Generate();
        var errorMessage = "Failed to assign role";
        var roleAssignmentException = new RoleAssignmentException(errorMessage);

        _mockMediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ThrowsAsync(roleAssignmentException);

        // Act
        var result = await _controller.Register(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = result as ObjectResult;
        statusCodeResult!.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be(errorMessage);
    }

    [Fact]
    public async Task Register_WithEmailException_Should_ReturnInternalServerError()
    {
        // Arrange
        var command = _registerUserCommandFaker.Generate();
        var errorMessage = "Email sending failed";
        var emailException = new EmailException(errorMessage);

        _mockMediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ThrowsAsync(emailException);

        // Act
        var result = await _controller.Register(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = result as ObjectResult;
        statusCodeResult!.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be(errorMessage);
    }

    [Fact]
    public async Task Register_WithUnexpectedException_Should_ReturnInternalServerError()
    {
        // Arrange
        var command = _registerUserCommandFaker.Generate();
        var exception = new Exception("Unexpected database error");

        _mockMediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.Register(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = result as ObjectResult;
        statusCodeResult!.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("An unexpected error occurred.");
    }

    #endregion

    #region ValidateKey Tests

    [Fact]
    public async Task ValidateKey_WithValidQuery_Should_ReturnOkResultWithDto()
    {
        // Arrange
        var query = _validateKeyQueryFaker.Generate();
        var validateKeyDto = _validateKeyDtoFaker.Generate();
        validateKeyDto.ExpirationDate = DateTime.UtcNow.AddDays(1); // Not expired

        _mockMediator.Setup(m => m.Send(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validateKeyDto);

        // Act
        var result = await _controller.ValidateKey(query);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(validateKeyDto);
    }

    [Fact]
    public async Task ValidateKey_WithExpiredKey_Should_ReturnBadRequestResult()
    {
        // Arrange
        var query = _validateKeyQueryFaker.Generate();
        var validateKeyDto = _validateKeyDtoFaker.Generate();
        validateKeyDto.ExpirationDate = DateTime.UtcNow.AddDays(-1); // Expired

        _mockMediator.Setup(m => m.Send(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validateKeyDto);

        // Act
        var result = await _controller.ValidateKey(query);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().Be("The invitation link has expired.");
    }

    [Fact]
    public async Task ValidateKey_WithValidationException_Should_ReturnBadRequestResult()
    {
        // Arrange
        var query = _validateKeyQueryFaker.Generate();
        var validationFailures = new List<ValidationFailure>
        {
            new ValidationFailure("Key", "Key is required")
        };        var validationException = new ValidationException(validationFailures);

        _mockMediator.Setup(m => m.Send(query, It.IsAny<CancellationToken>()))
            .ThrowsAsync(validationException);

        // Act
        var result = await _controller.ValidateKey(query);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        
        // The controller returns ex.Errors which is IList<ValidationFailure>
        badRequestResult!.Value.Should().BeEquivalentTo(validationFailures);
    }

    [Fact]
    public async Task ValidateKey_WithUnexpectedException_Should_ReturnInternalServerError()
    {
        // Arrange
        var query = _validateKeyQueryFaker.Generate();
        var exception = new Exception("Unexpected database error");

        _mockMediator.Setup(m => m.Send(query, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.ValidateKey(query);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = result as ObjectResult;
        statusCodeResult!.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("An unexpected error occurred.");
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_WithValidCommand_Should_ReturnOkResultWithLoginResponse()
    {
        // Arrange
        var command = _loginUserCommandFaker.Generate();
        var loginResponse = _loginResponseFaker.Generate();

        _mockMediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(loginResponse);

        // Act
        var result = await _controller.Login(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(loginResponse);
    }

    [Fact]
    public async Task Login_WithValidationException_Should_ReturnBadRequestResult()
    {
        // Arrange
        var command = _loginUserCommandFaker.Generate();
        var validationFailures = new List<ValidationFailure>
        {
            new ValidationFailure("Email", "Email is required"),
            new ValidationFailure("SupabaseId", "Supabase ID is required")
        };
        var validationException = new ValidationException(validationFailures);

        _mockMediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ThrowsAsync(validationException);        // Act
        var result = await _controller.Login(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        
        // The controller returns ex.Errors which is IList<ValidationFailure>
        badRequestResult!.Value.Should().BeEquivalentTo(validationFailures);
    }

    [Fact]
    public async Task Login_WithAccountDisabledException_Should_ReturnUnauthorizedResult()
    {
        // Arrange
        var command = _loginUserCommandFaker.Generate();
        var errorMessage = "Account is disabled";
        var accountDisabledException = new AccountDisabledException(errorMessage);

        _mockMediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ThrowsAsync(accountDisabledException);

        // Act
        var result = await _controller.Login(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = result as UnauthorizedObjectResult;
        unauthorizedResult!.Value.Should().Be(errorMessage);
    }

    [Fact]
    public async Task Login_WithInvalidCredentialsException_Should_ReturnUnauthorizedResult()
    {
        // Arrange
        var command = _loginUserCommandFaker.Generate();
        var errorMessage = "Invalid credentials";
        var invalidCredentialsException = new InvalidCredentialsException(errorMessage);

        _mockMediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ThrowsAsync(invalidCredentialsException);

        // Act
        var result = await _controller.Login(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = result as UnauthorizedObjectResult;
        unauthorizedResult!.Value.Should().Be(errorMessage);
    }

    [Fact]
    public async Task Login_WithUnexpectedException_Should_ReturnInternalServerError()
    {
        // Arrange
        var command = _loginUserCommandFaker.Generate();
        var exception = new Exception("Unexpected database error");

        _mockMediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.Login(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = result as ObjectResult;
        statusCodeResult!.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("An unexpected error occurred.");
    }

    #endregion

    #region GenerateInvitationLink Tests

    [Fact]
    public async Task GenerateInvitationLink_WithValidCommand_Should_ReturnOkResult()
    {
        // Arrange
        var command = _generateInvitationLinkCommandFaker.Generate();

        _mockMediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.GenerateInvitationLink(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task GenerateInvitationLink_WithValidationException_Should_ReturnBadRequestResult()
    {
        // Arrange
        var command = _generateInvitationLinkCommandFaker.Generate();
        var validationFailures = new List<ValidationFailure>
        {
            new ValidationFailure("Email", "Email is required"),
            new ValidationFailure("RoleName", "Role name is required")
        };
        var validationException = new ValidationException(validationFailures);

        _mockMediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ThrowsAsync(validationException);

        // Act
        var result = await _controller.GenerateInvitationLink(command, CancellationToken.None);        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeOfType<List<ValidationFailure>>()
            .Which.Should().BeEquivalentTo(validationFailures);
    }

    [Fact]
    public async Task GenerateInvitationLink_WithUnexpectedException_Should_ReturnInternalServerError()
    {
        // Arrange
        var command = _generateInvitationLinkCommandFaker.Generate();
        var exception = new Exception("Unexpected database error");

        _mockMediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.GenerateInvitationLink(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = result as ObjectResult;
        statusCodeResult!.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("An unexpected error occurred.");
    }

    #endregion

    #region ForgotPassword Tests

    [Fact]
    public async Task ForgotPassword_WithValidCommand_Should_ReturnOkResult()
    {
        // Arrange
        var command = _forgotPasswordCommandFaker.Generate();

        _mockMediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ForgotPassword(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task ForgotPassword_WithValidationException_Should_ReturnBadRequestResult()
    {
        // Arrange
        var command = _forgotPasswordCommandFaker.Generate();
        var validationFailures = new List<ValidationFailure>
        {
            new ValidationFailure("Email", "Email is required")
        };
        var validationException = new ValidationException(validationFailures);

        _mockMediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ThrowsAsync(validationException);

        // Act       
        var result = await _controller.ForgotPassword(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        
        // The controller returns ex.Errors which is IList<ValidationFailure>
        badRequestResult!.Value.Should().BeEquivalentTo(validationFailures);
    }

    [Fact]
    public async Task ForgotPassword_WithUserNotFoundException_Should_ReturnNotFoundResult()
    {
        // Arrange
        var command = _forgotPasswordCommandFaker.Generate();
        var errorMessage = "User not found";
        var userNotFoundException = new UserNotFoundException(errorMessage);

        _mockMediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ThrowsAsync(userNotFoundException);

        // Act
        var result = await _controller.ForgotPassword(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.Value.Should().Be(errorMessage);
    }

    [Fact]
    public async Task ForgotPassword_WithUnexpectedException_Should_ReturnInternalServerError()
    {
        // Arrange
        var command = _forgotPasswordCommandFaker.Generate();
        var exception = new Exception("Unexpected database error");

        _mockMediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.ForgotPassword(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = result as ObjectResult;
        statusCodeResult!.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("An unexpected error occurred.");
    }

    #endregion

    #region ResetPassword Tests

    [Fact]
    public async Task ResetPassword_WithValidCommand_Should_ReturnOkResult()
    {
        // Arrange
        var command = _resetPasswordCommandFaker.Generate();

        _mockMediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ResetPassword(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task ResetPassword_WithValidationException_Should_ReturnBadRequestResult()
    {
        // Arrange
        var command = _resetPasswordCommandFaker.Generate();
        var validationFailures = new List<ValidationFailure>
        {
            new ValidationFailure("Email", "Email is required"),
            new ValidationFailure("Token", "Token is required"),
            new ValidationFailure("Password", "Password is required")
        };
        var validationException = new ValidationException(validationFailures);

        _mockMediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ThrowsAsync(validationException);        // Act
        var result = await _controller.ResetPassword(command, CancellationToken.None);

        // Assert        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeOfType<List<ValidationFailure>>()
            .Which.Should().BeEquivalentTo(validationFailures);
    }

    [Fact]
    public async Task ResetPassword_WithUserNotFoundException_Should_ReturnNotFoundResult()
    {
        // Arrange
        var command = _resetPasswordCommandFaker.Generate();
        var errorMessage = "User not found";
        var userNotFoundException = new UserNotFoundException(errorMessage);

        _mockMediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ThrowsAsync(userNotFoundException);

        // Act
        var result = await _controller.ResetPassword(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.Value.Should().Be(errorMessage);
    }

    [Fact]
    public async Task ResetPassword_WithPasswordResetException_Should_ReturnInternalServerError()
    {
        // Arrange
        var command = _resetPasswordCommandFaker.Generate();
        var errorMessage = "Password reset failed";
        var passwordResetException = new PasswordResetException(errorMessage);

        _mockMediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ThrowsAsync(passwordResetException);

        // Act
        var result = await _controller.ResetPassword(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = result as ObjectResult;
        statusCodeResult!.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be(errorMessage);
    }

    [Fact]
    public async Task ResetPassword_WithUnexpectedException_Should_ReturnInternalServerError()
    {
        // Arrange
        var command = _resetPasswordCommandFaker.Generate();
        var exception = new Exception("Unexpected database error");

        _mockMediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.ResetPassword(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = result as ObjectResult;
        statusCodeResult!.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("An unexpected error occurred.");
    }

    #endregion

    #region ConfirmEmail Tests

    [Fact]
    public async Task ConfirmEmail_WithValidCommand_Should_ReturnOkResult()
    {
        // Arrange
        var command = _confirmEmailCommandFaker.Generate();

        _mockMediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ConfirmEmail(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task ConfirmEmail_WithValidationException_Should_ReturnBadRequestResult()
    {
        // Arrange
        var command = _confirmEmailCommandFaker.Generate();
        var validationFailures = new List<ValidationFailure>
        {
            new ValidationFailure("UserId", "User ID is required"),
            new ValidationFailure("Token", "Token is required")
        };
        var validationException = new ValidationException(validationFailures);        _mockMediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ThrowsAsync(validationException);

        // Act
        var result = await _controller.ConfirmEmail(command, CancellationToken.None);

        // Assert        
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        
        // The controller returns ex.Errors which is IList<ValidationFailure>
        badRequestResult!.Value.Should().BeEquivalentTo(validationFailures);
    }

    [Fact]
    public async Task ConfirmEmail_WithUserNotFoundException_Should_ReturnNotFoundResult()
    {
        // Arrange
        var command = _confirmEmailCommandFaker.Generate();
        var errorMessage = "User not found";
        var userNotFoundException = new UserNotFoundException(errorMessage);

        _mockMediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ThrowsAsync(userNotFoundException);

        // Act
        var result = await _controller.ConfirmEmail(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.Value.Should().Be(errorMessage);
    }

    [Fact]
    public async Task ConfirmEmail_WithEmailConfirmationException_Should_ReturnInternalServerError()
    {
        // Arrange
        var command = _confirmEmailCommandFaker.Generate();
        var errorMessage = "Email confirmation failed";
        var emailConfirmationException = new EmailConfirmationException(errorMessage);

        _mockMediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ThrowsAsync(emailConfirmationException);

        // Act
        var result = await _controller.ConfirmEmail(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = result as ObjectResult;
        statusCodeResult!.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be(errorMessage);
    }

    [Fact]
    public async Task ConfirmEmail_WithUnexpectedException_Should_ReturnInternalServerError()
    {
        // Arrange
        var command = _confirmEmailCommandFaker.Generate();
        var exception = new Exception("Unexpected database error");

        _mockMediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.ConfirmEmail(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = result as ObjectResult;
        statusCodeResult!.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("An unexpected error occurred.");
    }

    #endregion

    #region RefreshToken Tests

    [Fact]
    public async Task RefreshToken_WithValidCommand_Should_ReturnOkResultWithRefreshTokenResponse()
    {
        // Arrange
        var command = _refreshTokenCommandFaker.Generate();
        var refreshTokenResponse = _refreshTokenResponseFaker.Generate();

        _mockMediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshTokenResponse);

        // Act
        var result = await _controller.RefreshToken(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(refreshTokenResponse);
    }

    [Fact]
    public async Task RefreshToken_WithValidationException_Should_ReturnBadRequestResult()
    {
        // Arrange
        var command = _refreshTokenCommandFaker.Generate();
        var validationFailures = new List<ValidationFailure>
        {
            new ValidationFailure("Token", "Token is required")
        };
        var validationException = new ValidationException(validationFailures);

        _mockMediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ThrowsAsync(validationException);

        // Act
        var result = await _controller.RefreshToken(command, CancellationToken.None);        // Assert        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeOfType<List<ValidationFailure>>()
            .Which.Should().BeEquivalentTo(validationFailures);
    }

    [Fact]
    public async Task RefreshToken_WithInvalidRefreshTokenException_Should_ReturnUnauthorizedResult()
    {
        // Arrange
        var command = _refreshTokenCommandFaker.Generate();
        var errorMessage = "Invalid refresh token";
        var invalidRefreshTokenException = new InvalidRefreshTokenException(errorMessage);

        _mockMediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ThrowsAsync(invalidRefreshTokenException);

        // Act
        var result = await _controller.RefreshToken(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = result as UnauthorizedObjectResult;
        unauthorizedResult!.Value.Should().Be(errorMessage);
    }

    [Fact]
    public async Task RefreshToken_WithUnexpectedException_Should_ReturnInternalServerError()
    {
        // Arrange
        var command = _refreshTokenCommandFaker.Generate();
        var exception = new Exception("Unexpected database error");

        _mockMediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.RefreshToken(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = result as ObjectResult;
        statusCodeResult!.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("An unexpected error occurred.");
    }

    #endregion
}
