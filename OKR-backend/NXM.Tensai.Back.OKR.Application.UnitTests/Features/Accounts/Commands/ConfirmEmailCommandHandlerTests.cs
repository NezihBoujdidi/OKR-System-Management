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

public class ConfirmEmailCommandHandlerTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IValidator<ConfirmEmailCommand>> _validatorMock;
    private readonly ConfirmEmailCommandHandler _handler;
    private readonly Faker _faker;

    public ConfirmEmailCommandHandlerTests()
    {
        _userManagerMock = new Mock<UserManager<User>>(
            Mock.Of<IUserStore<User>>(), null, null, null, null, null, null, null, null);
        _validatorMock = new Mock<IValidator<ConfirmEmailCommand>>();
        _handler = new ConfirmEmailCommandHandler(
            _userManagerMock.Object,
            _validatorMock.Object);
        _faker = new Faker();
    }

    [Fact]
    public async Task Handle_WithValidRequest_Should_ConfirmEmailSuccessfully()
    {
        // Arrange
        var command = new ConfirmEmailCommand
        {
            UserId = Guid.NewGuid().ToString(),
            Token = _faker.Random.AlphaNumeric(64)
        };

        var user = new User
        {
            Id = Guid.Parse(command.UserId),
            Email = _faker.Internet.Email(),
            EmailConfirmed = false
        };

        var confirmResult = IdentityResult.Success;

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _userManagerMock.Setup(x => x.FindByIdAsync(command.UserId))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.ConfirmEmailAsync(user, command.Token))
            .ReturnsAsync(confirmResult);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _userManagerMock.Verify(x => x.FindByIdAsync(command.UserId), Times.Once);
        _userManagerMock.Verify(x => x.ConfirmEmailAsync(user, command.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidValidation_Should_ThrowValidationException()
    {
        // Arrange
        var command = new ConfirmEmailCommand
        {
            UserId = "",
            Token = ""
        };

        var validationFailures = new List<ValidationFailure>
        {
            new("UserId", "User ID is required."),
            new("Token", "Token is required.")
        };

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>();

        _userManagerMock.Verify(x => x.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _userManagerMock.Verify(x => x.ConfirmEmailAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_Should_ThrowUserNotFoundException()
    {
        // Arrange
        var command = new ConfirmEmailCommand
        {
            UserId = Guid.NewGuid().ToString(),
            Token = _faker.Random.AlphaNumeric(64)
        };

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _userManagerMock.Setup(x => x.FindByIdAsync(command.UserId))
            .ReturnsAsync((User)null);

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<UserNotFoundException>();

        _userManagerMock.Verify(x => x.FindByIdAsync(command.UserId), Times.Once);
        _userManagerMock.Verify(x => x.ConfirmEmailAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithFailedEmailConfirmation_Should_ThrowEmailConfirmationException()
    {
        // Arrange
        var command = new ConfirmEmailCommand
        {
            UserId = Guid.NewGuid().ToString(),
            Token = _faker.Random.AlphaNumeric(64)
        };

        var user = new User
        {
            Id = Guid.Parse(command.UserId),
            Email = _faker.Internet.Email(),
            EmailConfirmed = false
        };

        var identityErrors = new[]
        {
            new IdentityError { Code = "InvalidToken", Description = "Invalid confirmation token." },
            new IdentityError { Code = "TokenExpired", Description = "Confirmation token has expired." }
        };

        var confirmResult = IdentityResult.Failed(identityErrors);

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _userManagerMock.Setup(x => x.FindByIdAsync(command.UserId))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.ConfirmEmailAsync(user, command.Token))
            .ReturnsAsync(confirmResult);

        // Act & Assert        // Act & Assert
        var exception = await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<EmailConfirmationException>();
        exception.WithMessage("*Invalid confirmation token*");
        exception.And.Message.Should().Contain("Confirmation token has expired");

        _userManagerMock.Verify(x => x.FindByIdAsync(command.UserId), Times.Once);
        _userManagerMock.Verify(x => x.ConfirmEmailAsync(user, command.Token), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task Handle_WithInvalidUserId_Should_ThrowValidationException(string invalidUserId)
    {
        // Arrange
        var command = new ConfirmEmailCommand
        {
            UserId = invalidUserId,
            Token = _faker.Random.AlphaNumeric(64)
        };

        var validationFailures = new List<ValidationFailure>
        {
            new("UserId", "User ID is required.")
        };

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>();

        _userManagerMock.Verify(x => x.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _userManagerMock.Verify(x => x.ConfirmEmailAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task Handle_WithInvalidToken_Should_ThrowValidationException(string invalidToken)
    {
        // Arrange
        var command = new ConfirmEmailCommand
        {
            UserId = Guid.NewGuid().ToString(),
            Token = invalidToken
        };

        var validationFailures = new List<ValidationFailure>
        {
            new("Token", "Token is required.")
        };

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>();

        _userManagerMock.Verify(x => x.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _userManagerMock.Verify(x => x.ConfirmEmailAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithUserManagerException_Should_PropagateException()
    {
        // Arrange
        var command = new ConfirmEmailCommand
        {
            UserId = Guid.NewGuid().ToString(),
            Token = _faker.Random.AlphaNumeric(64)
        };

        var user = new User
        {
            Id = Guid.Parse(command.UserId),
            Email = _faker.Internet.Email(),
            EmailConfirmed = false
        };

        var expectedException = new InvalidOperationException("Database connection failed");

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _userManagerMock.Setup(x => x.FindByIdAsync(command.UserId))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.ConfirmEmailAsync(user, command.Token))
            .ThrowsAsync(expectedException);

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database connection failed");

        _userManagerMock.Verify(x => x.FindByIdAsync(command.UserId), Times.Once);
        _userManagerMock.Verify(x => x.ConfirmEmailAsync(user, command.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_WithMultipleIdentityErrors_Should_CombineErrorMessages()
    {
        // Arrange
        var command = new ConfirmEmailCommand
        {
            UserId = Guid.NewGuid().ToString(),
            Token = _faker.Random.AlphaNumeric(64)
        };

        var user = new User
        {
            Id = Guid.Parse(command.UserId),
            Email = _faker.Internet.Email(),
            EmailConfirmed = false
        };

        var identityErrors = new[]
        {
            new IdentityError { Code = "Error1", Description = "First error." },
            new IdentityError { Code = "Error2", Description = "Second error." },
            new IdentityError { Code = "Error3", Description = "Third error." }
        };

        var confirmResult = IdentityResult.Failed(identityErrors);

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _userManagerMock.Setup(x => x.FindByIdAsync(command.UserId))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.ConfirmEmailAsync(user, command.Token))
            .ReturnsAsync(confirmResult);

        // Act & Assert
        var exception = await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<EmailConfirmationException>();

        exception.Which.Message.Should().Contain("First error");
        exception.Which.Message.Should().Contain("Second error");
        exception.Which.Message.Should().Contain("Third error");
    }

    [Fact]
    public async Task Handle_WithAlreadyConfirmedEmail_Should_StillProcessSuccessfully()
    {
        // Arrange
        var command = new ConfirmEmailCommand
        {
            UserId = Guid.NewGuid().ToString(),
            Token = _faker.Random.AlphaNumeric(64)
        };

        var user = new User
        {
            Id = Guid.Parse(command.UserId),
            Email = _faker.Internet.Email(),
            EmailConfirmed = true // Already confirmed
        };

        var confirmResult = IdentityResult.Success;

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _userManagerMock.Setup(x => x.FindByIdAsync(command.UserId))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.ConfirmEmailAsync(user, command.Token))
            .ReturnsAsync(confirmResult);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _userManagerMock.Verify(x => x.FindByIdAsync(command.UserId), Times.Once);
        _userManagerMock.Verify(x => x.ConfirmEmailAsync(user, command.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidGuidUserId_Should_StillAttemptToFindUser()
    {
        // Arrange
        var command = new ConfirmEmailCommand
        {
            UserId = "not-a-guid",
            Token = _faker.Random.AlphaNumeric(64)
        };

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _userManagerMock.Setup(x => x.FindByIdAsync(command.UserId))
            .ReturnsAsync((User)null);

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<UserNotFoundException>();

        _userManagerMock.Verify(x => x.FindByIdAsync(command.UserId), Times.Once);
        _userManagerMock.Verify(x => x.ConfirmEmailAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }
}
