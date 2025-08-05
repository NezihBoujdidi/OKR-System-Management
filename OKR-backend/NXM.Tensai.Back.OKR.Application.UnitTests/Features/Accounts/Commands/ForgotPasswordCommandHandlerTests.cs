using Bogus;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Moq;
using NXM.Tensai.Back.OKR.Application.Common.Exceptions;
using NXM.Tensai.Back.OKR.Domain.Entities;
using NXM.Tensai.Back.OKR.Domain.Interfaces;
using Xunit;
using ValidationException = FluentValidation.ValidationException;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace NXM.Tensai.Back.OKR.Application.UnitTests.Features.Accounts.Commands;

public class ForgotPasswordCommandHandlerTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IEmailSender> _emailSenderMock;
    private readonly Mock<IValidator<ForgotPasswordCommand>> _validatorMock;
    private readonly ForgotPasswordCommandHandler _handler;
    private readonly Faker _faker;

    public ForgotPasswordCommandHandlerTests()
    {
        _userManagerMock = new Mock<UserManager<User>>(
            Mock.Of<IUserStore<User>>(), null, null, null, null, null, null, null, null);
        _emailSenderMock = new Mock<IEmailSender>();
        _validatorMock = new Mock<IValidator<ForgotPasswordCommand>>();
        _handler = new ForgotPasswordCommandHandler(
            _userManagerMock.Object,
            _emailSenderMock.Object,
            _validatorMock.Object);
        _faker = new Faker();
    }

    [Fact]
    public async Task Handle_WithValidRequest_Should_SendResetPasswordEmail()
    {
        // Arrange
        var command = new ForgotPasswordCommand
        {
            Email = _faker.Internet.Email()
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = command.Email,
            EmailConfirmed = true
        };

        var resetToken = _faker.Random.AlphaNumeric(64);

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(user))
            .ReturnsAsync(resetToken);

        _emailSenderMock.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _userManagerMock.Verify(x => x.FindByEmailAsync(command.Email), Times.Once);
        _userManagerMock.Verify(x => x.GeneratePasswordResetTokenAsync(user), Times.Once);
        _emailSenderMock.Verify(x => x.SendEmailAsync(
            command.Email,
            "Reset your password",
            It.Is<string>(body => body.Contains("Reset your password"))), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidValidation_Should_ThrowValidationException()
    {
        // Arrange
        var command = new ForgotPasswordCommand
        {
            Email = ""
        };

        var validationFailures = new List<ValidationFailure>
        {
            new("Email", "Email is required.")
        };

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("*Email is required*");

        _userManagerMock.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Never);
        _emailSenderMock.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_Should_ThrowUserNotFoundException()
    {
        // Arrange
        var command = new ForgotPasswordCommand
        {
            Email = _faker.Internet.Email()
        };

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync((User)null);

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<UserNotFoundException>();

        _userManagerMock.Verify(x => x.FindByEmailAsync(command.Email), Times.Once);
        _userManagerMock.Verify(x => x.GeneratePasswordResetTokenAsync(It.IsAny<User>()), Times.Never);
        _emailSenderMock.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithEmailSenderFailure_Should_PropagateException()
    {
        // Arrange
        var command = new ForgotPasswordCommand
        {
            Email = _faker.Internet.Email()
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = command.Email,
            EmailConfirmed = true
        };

        var resetToken = _faker.Random.AlphaNumeric(64);
        var expectedException = new InvalidOperationException("Email service unavailable");

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(user))
            .ReturnsAsync(resetToken);

        _emailSenderMock.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Email service unavailable");

        _userManagerMock.Verify(x => x.FindByEmailAsync(command.Email), Times.Once);
        _userManagerMock.Verify(x => x.GeneratePasswordResetTokenAsync(user), Times.Once);
    }

    [Fact]
    public async Task Handle_WithUserManagerFailure_Should_PropagateException()
    {
        // Arrange
        var command = new ForgotPasswordCommand
        {
            Email = _faker.Internet.Email()
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = command.Email,
            EmailConfirmed = true
        };

        var expectedException = new InvalidOperationException("User manager error");

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(user))
            .ThrowsAsync(expectedException);

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("User manager error");

        _userManagerMock.Verify(x => x.FindByEmailAsync(command.Email), Times.Once);
        _userManagerMock.Verify(x => x.GeneratePasswordResetTokenAsync(user), Times.Once);
        _emailSenderMock.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("invalid-email")]
    [InlineData("test@")]
    [InlineData("@domain.com")]
    public async Task Handle_WithInvalidEmailFormats_Should_ThrowValidationException(string invalidEmail)
    {
        // Arrange
        var command = new ForgotPasswordCommand
        {
            Email = invalidEmail
        };

        var validationFailures = new List<ValidationFailure>
        {
            new("Email", "Invalid email format.")
        };

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>();

        _userManagerMock.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Never);
        _emailSenderMock.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithValidRequest_Should_GenerateCorrectResetLink()
    {
        // Arrange
        var command = new ForgotPasswordCommand
        {
            Email = _faker.Internet.Email()
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = command.Email,
            EmailConfirmed = true
        };

        var resetToken = "test-token+special/chars";
        var expectedEncodedToken = Uri.EscapeDataString(resetToken);

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(user))
            .ReturnsAsync(resetToken);

        _emailSenderMock.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _emailSenderMock.Verify(x => x.SendEmailAsync(
            command.Email,
            "Reset your password",
            It.Is<string>(body => 
                body.Contains($"userId={user.Id}") && 
                body.Contains($"token={expectedEncodedToken}"))), Times.Once);
    }
}
