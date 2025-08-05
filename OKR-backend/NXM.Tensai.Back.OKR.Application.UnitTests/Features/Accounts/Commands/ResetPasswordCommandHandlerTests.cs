using Bogus;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Moq;
using NXM.Tensai.Back.OKR.Application.Common.Exceptions;
using NXM.Tensai.Back.OKR.Domain.Entities;
using Xunit;
using ValidationException = FluentValidation.ValidationException;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace NXM.Tensai.Back.OKR.Application.UnitTests.Features.Accounts.Commands;

public class ResetPasswordCommandHandlerTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IValidator<ResetPasswordCommand>> _validatorMock;
    private readonly ResetPasswordCommandHandler _handler;
    private readonly Faker _faker;

    public ResetPasswordCommandHandlerTests()
    {
        _userManagerMock = new Mock<UserManager<User>>(
            Mock.Of<IUserStore<User>>(), null, null, null, null, null, null, null, null);
        _validatorMock = new Mock<IValidator<ResetPasswordCommand>>();
        _handler = new ResetPasswordCommandHandler(
            _userManagerMock.Object,
            _validatorMock.Object);
        _faker = new Faker();
    }

    [Fact]
    public async Task Handle_WithValidRequest_Should_ResetPasswordSuccessfully()
    {
        // Arrange
        var command = new ResetPasswordCommand
        {
            Email = _faker.Internet.Email(),
            Token = _faker.Random.AlphaNumeric(64),
            Password = _faker.Internet.Password(),
            ConfirmPassword = _faker.Internet.Password()
        };
        command.ConfirmPassword = command.Password; // Ensure passwords match

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = command.Email,
            EmailConfirmed = true
        };

        var resetResult = IdentityResult.Success;

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.ResetPasswordAsync(user, command.Token, command.Password))
            .ReturnsAsync(resetResult);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _userManagerMock.Verify(x => x.FindByEmailAsync(command.Email), Times.Once);
        _userManagerMock.Verify(x => x.ResetPasswordAsync(user, command.Token, command.Password), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidValidation_Should_ThrowValidationException()
    {
        // Arrange
        var command = new ResetPasswordCommand
        {
            Email = "",
            Token = "",
            Password = "",
            ConfirmPassword = "different"
        };

        var validationFailures = new List<ValidationFailure>
        {
            new("Email", "Email is required."),
            new("Token", "Token is required."),
            new("Password", "Password is required."),
            new("ConfirmPassword", "Passwords do not match.")
        };

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>();

        _userManagerMock.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Never);
        _userManagerMock.Verify(x => x.ResetPasswordAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_Should_ThrowUserNotFoundException()
    {
        // Arrange
        var command = new ResetPasswordCommand
        {
            Email = _faker.Internet.Email(),
            Token = _faker.Random.AlphaNumeric(64),
            Password = _faker.Internet.Password(),
            ConfirmPassword = _faker.Internet.Password()
        };
        command.ConfirmPassword = command.Password;

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync((User)null);

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<UserNotFoundException>();

        _userManagerMock.Verify(x => x.FindByEmailAsync(command.Email), Times.Once);
        _userManagerMock.Verify(x => x.ResetPasswordAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithFailedPasswordReset_Should_ThrowPasswordResetException()
    {
        // Arrange
        var command = new ResetPasswordCommand
        {
            Email = _faker.Internet.Email(),
            Token = _faker.Random.AlphaNumeric(64),
            Password = _faker.Internet.Password(),
            ConfirmPassword = _faker.Internet.Password()
        };
        command.ConfirmPassword = command.Password;

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = command.Email,
            EmailConfirmed = true
        };

        var identityErrors = new[]
        {
            new IdentityError { Code = "InvalidToken", Description = "Invalid token." },
            new IdentityError { Code = "PasswordTooWeak", Description = "Password is too weak." }
        };

        var resetResult = IdentityResult.Failed(identityErrors);

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.ResetPasswordAsync(user, command.Token, command.Password))
            .ReturnsAsync(resetResult);        // Act & Assert
        var exception = await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<PasswordResetException>();
        exception.WithMessage("*Invalid token*");
        exception.And.Message.Should().Contain("Password is too weak");

        _userManagerMock.Verify(x => x.FindByEmailAsync(command.Email), Times.Once);
        _userManagerMock.Verify(x => x.ResetPasswordAsync(user, command.Token, command.Password), Times.Once);
    }

    [Theory]
    [InlineData("", "valid-token", "Password123!", "Password123!")]
    [InlineData("test@email.com", "", "Password123!", "Password123!")]
    [InlineData("test@email.com", "valid-token", "", "Password123!")]
    [InlineData("test@email.com", "valid-token", "Password123!", "DifferentPassword")]
    public async Task Handle_WithInvalidFields_Should_ThrowValidationException(
        string email, string token, string password, string confirmPassword)
    {
        // Arrange
        var command = new ResetPasswordCommand
        {
            Email = email,
            Token = token,
            Password = password,
            ConfirmPassword = confirmPassword
        };

        var validationFailures = new List<ValidationFailure>
        {
            new("Field", "Field is invalid.")
        };

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>();

        _userManagerMock.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Never);
        _userManagerMock.Verify(x => x.ResetPasswordAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithUserManagerException_Should_PropagateException()
    {
        // Arrange
        var command = new ResetPasswordCommand
        {
            Email = _faker.Internet.Email(),
            Token = _faker.Random.AlphaNumeric(64),
            Password = _faker.Internet.Password(),
            ConfirmPassword = _faker.Internet.Password()
        };
        command.ConfirmPassword = command.Password;

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = command.Email,
            EmailConfirmed = true
        };

        var expectedException = new InvalidOperationException("Database connection failed");

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.ResetPasswordAsync(user, command.Token, command.Password))
            .ThrowsAsync(expectedException);

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database connection failed");

        _userManagerMock.Verify(x => x.FindByEmailAsync(command.Email), Times.Once);
        _userManagerMock.Verify(x => x.ResetPasswordAsync(user, command.Token, command.Password), Times.Once);
    }

    [Fact]
    public async Task Handle_WithMultipleIdentityErrors_Should_CombineErrorMessages()
    {
        // Arrange
        var command = new ResetPasswordCommand
        {
            Email = _faker.Internet.Email(),
            Token = _faker.Random.AlphaNumeric(64),
            Password = _faker.Internet.Password(),
            ConfirmPassword = _faker.Internet.Password()
        };
        command.ConfirmPassword = command.Password;

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = command.Email,
            EmailConfirmed = true
        };

        var identityErrors = new[]
        {
            new IdentityError { Code = "Error1", Description = "First error." },
            new IdentityError { Code = "Error2", Description = "Second error." },
            new IdentityError { Code = "Error3", Description = "Third error." }
        };

        var resetResult = IdentityResult.Failed(identityErrors);

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.ResetPasswordAsync(user, command.Token, command.Password))
            .ReturnsAsync(resetResult);

        // Act & Assert
        var exception = await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<PasswordResetException>();

        exception.Which.Message.Should().Contain("First error");
        exception.Which.Message.Should().Contain("Second error");
        exception.Which.Message.Should().Contain("Third error");
    }

    [Fact]
    public async Task Handle_WithTokenExpired_Should_ThrowPasswordResetException()
    {
        // Arrange
        var command = new ResetPasswordCommand
        {
            Email = _faker.Internet.Email(),
            Token = "expired-token",
            Password = _faker.Internet.Password(),
            ConfirmPassword = _faker.Internet.Password()
        };
        command.ConfirmPassword = command.Password;

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = command.Email,
            EmailConfirmed = true
        };

        var identityErrors = new[]
        {
            new IdentityError { Code = "InvalidToken", Description = "Token has expired." }
        };

        var resetResult = IdentityResult.Failed(identityErrors);

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.ResetPasswordAsync(user, command.Token, command.Password))
            .ReturnsAsync(resetResult);

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<PasswordResetException>()
            .WithMessage("*Token has expired*");
    }
}
