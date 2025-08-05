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

public class LoginUserCommandHandlerTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly Mock<IValidator<LoginUserCommand>> _validatorMock;
    private readonly LoginUserCommandHandler _handler;
    private readonly Faker _faker;

    public LoginUserCommandHandlerTests()
    {
        _userManagerMock = new Mock<UserManager<User>>(
            Mock.Of<IUserStore<User>>(), null, null, null, null, null, null, null, null);
        _jwtServiceMock = new Mock<IJwtService>();
        _validatorMock = new Mock<IValidator<LoginUserCommand>>();
        _handler = new LoginUserCommandHandler(
            _userManagerMock.Object,
            _jwtServiceMock.Object,
            _validatorMock.Object);
        _faker = new Faker();
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldReturnLoginResponse()
    {
        // Arrange
        var command = new LoginUserCommand
        {
            Email = _faker.Internet.Email(),
            SupabaseId = _faker.Random.Guid().ToString()
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = command.Email,
            SupabaseId = command.SupabaseId,
            IsEnabled = true,
            RefreshTokens = new List<RefreshToken>()
        };

        var validationResult = new ValidationResult();
        var jwtToken = "jwt-token";
        var refreshToken = "refresh-token";
        var expires = DateTime.UtcNow.AddDays(7);

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);
        _jwtServiceMock.Setup(x => x.GenerateJwtToken(user))
            .ReturnsAsync(jwtToken);
        _jwtServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns(refreshToken);
        _userManagerMock.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().Be(jwtToken);
        result.RefreshToken.Should().Be(refreshToken);
        result.Expires.Should().BeCloseTo(expires, TimeSpan.FromMinutes(1));

        _validatorMock.Verify(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);
        _userManagerMock.Verify(x => x.FindByEmailAsync(command.Email), Times.Once);
        _jwtServiceMock.Verify(x => x.GenerateJwtToken(user), Times.Once);
        _jwtServiceMock.Verify(x => x.GenerateRefreshToken(), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidationFails_ShouldThrowValidationException()
    {
        // Arrange
        var command = new LoginUserCommand
        {
            Email = "", // Invalid email
            SupabaseId = "" // Invalid SupabaseId
        };

        var validationErrors = new List<ValidationFailure>
        {
            new ValidationFailure("Email", "Email is required."),
            new ValidationFailure("SupabaseId", "Supabase ID is required.")
        };
        var validationResult = new ValidationResult(validationErrors);
        
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => 
            _handler.Handle(command, CancellationToken.None));

        exception.Errors.Should().HaveCount(2);
        _validatorMock.Verify(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);
        _userManagerMock.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UserNotFound_ShouldThrowInvalidCredentialsException()
    {
        // Arrange
        var command = new LoginUserCommand
        {
            Email = _faker.Internet.Email(),
            SupabaseId = _faker.Random.Guid().ToString()
        };

        var validationResult = new ValidationResult();
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync((User?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidCredentialsException>(() => 
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Be("User not found");
        _validatorMock.Verify(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);
        _userManagerMock.Verify(x => x.FindByEmailAsync(command.Email), Times.Once);
        _jwtServiceMock.Verify(x => x.GenerateJwtToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UserDisabled_ShouldThrowAccountDisabledException()
    {
        // Arrange
        var command = new LoginUserCommand
        {
            Email = _faker.Internet.Email(),
            SupabaseId = _faker.Random.Guid().ToString()
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = command.Email,
            SupabaseId = command.SupabaseId,
            IsEnabled = false // User is disabled
        };

        var validationResult = new ValidationResult();
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);

        // Act & Assert
        await Assert.ThrowsAsync<AccountDisabledException>(() => 
            _handler.Handle(command, CancellationToken.None));

        _validatorMock.Verify(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);
        _userManagerMock.Verify(x => x.FindByEmailAsync(command.Email), Times.Once);
        _jwtServiceMock.Verify(x => x.GenerateJwtToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Handle_FirstTimeLoginWithSupabase_ShouldUpdateSupabaseIdAndLogin()
    {
        // Arrange
        var command = new LoginUserCommand
        {
            Email = _faker.Internet.Email(),
            SupabaseId = _faker.Random.Guid().ToString()
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = command.Email,
            SupabaseId = null, // First time login, no SupabaseId set
            IsEnabled = true,
            RefreshTokens = new List<RefreshToken>()
        };

        var validationResult = new ValidationResult();
        var jwtToken = "jwt-token";
        var refreshToken = "refresh-token";

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);
        _jwtServiceMock.Setup(x => x.GenerateJwtToken(user))
            .ReturnsAsync(jwtToken);
        _jwtServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns(refreshToken);
        _userManagerMock.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().Be(jwtToken);
        user.SupabaseId.Should().Be(command.SupabaseId); // Should be updated

        _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Exactly(2)); // Once for SupabaseId, once for refresh token
    }

    [Fact]
    public async Task Handle_SupabaseIdMismatch_ShouldThrowInvalidCredentialsException()
    {
        // Arrange
        var command = new LoginUserCommand
        {
            Email = _faker.Internet.Email(),
            SupabaseId = _faker.Random.Guid().ToString()
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = command.Email,
            SupabaseId = _faker.Random.Guid().ToString(), // Different SupabaseId
            IsEnabled = true
        };

        var validationResult = new ValidationResult();
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidCredentialsException>(() => 
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Be("Invalid credentials");
        _userManagerMock.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Never);
        _jwtServiceMock.Verify(x => x.GenerateJwtToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UserManagerUpdateFails_ShouldThrowException()
    {
        // Arrange
        var command = new LoginUserCommand
        {
            Email = _faker.Internet.Email(),
            SupabaseId = _faker.Random.Guid().ToString()
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = command.Email,
            SupabaseId = command.SupabaseId,
            IsEnabled = true,
            RefreshTokens = new List<RefreshToken>()
        };

        var validationResult = new ValidationResult();
        var jwtToken = "jwt-token";
        var refreshToken = "refresh-token";

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);
        _jwtServiceMock.Setup(x => x.GenerateJwtToken(user))
            .ReturnsAsync(jwtToken);
        _jwtServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns(refreshToken);
        _userManagerMock.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Update failed" }));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            _handler.Handle(command, CancellationToken.None));

        _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
    }
}
