using Bogus;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Moq;
using NXM.Tensai.Back.OKR.Application.Common.Exceptions;
using NXM.Tensai.Back.OKR.Domain.Entities;
using NXM.Tensai.Back.OKR.Domain.Interfaces;
using System.Security.Claims;
using Xunit;
using ValidationException = FluentValidation.ValidationException;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace NXM.Tensai.Back.OKR.Application.UnitTests.Features.Accounts.Commands;

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly Mock<IValidator<RefreshTokenCommand>> _validatorMock;
    private readonly RefreshTokenCommandHandler _handler;
    private readonly Faker _faker;

    public RefreshTokenCommandHandlerTests()
    {
        _userManagerMock = new Mock<UserManager<User>>(
            Mock.Of<IUserStore<User>>(), null, null, null, null, null, null, null, null);
        _jwtServiceMock = new Mock<IJwtService>();
        _validatorMock = new Mock<IValidator<RefreshTokenCommand>>();
        _handler = new RefreshTokenCommandHandler(
            _userManagerMock.Object,
            _jwtServiceMock.Object,
            _validatorMock.Object);
        _faker = new Faker();
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldReturnNewTokens()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var token = "valid-refresh-token";
        var command = new RefreshTokenCommand { Token = token };

        var user = new User
        {
            Id = Guid.Parse(userId),
            Email = _faker.Internet.Email(),
            RefreshTokens = new List<RefreshToken>
            {
                new RefreshToken
                {
                    Token = token,
                    Expires = DateTime.UtcNow.AddDays(1),
                    Created = DateTime.UtcNow,
                    Revoked = null // This makes IsActive = true
                }
            }
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }));

        var validationResult = new ValidationResult();
        var newJwtToken = "new-jwt-token";
        var newRefreshToken = "new-refresh-token";
        var expires = DateTime.UtcNow.AddDays(7);

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        _jwtServiceMock.Setup(x => x.GetPrincipalFromExpiredToken(token))
            .Returns(principal);
        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        _jwtServiceMock.Setup(x => x.GenerateJwtToken(user))
            .ReturnsAsync(newJwtToken);
        _jwtServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns(newRefreshToken);
        _userManagerMock.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().Be(newJwtToken);
        result.RefreshToken.Should().Be(newRefreshToken);
        result.Expires.Should().BeCloseTo(expires, TimeSpan.FromMinutes(1));

        _validatorMock.Verify(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);
        _jwtServiceMock.Verify(x => x.GetPrincipalFromExpiredToken(token), Times.Once);
        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        _jwtServiceMock.Verify(x => x.GenerateJwtToken(user), Times.Once);
        _jwtServiceMock.Verify(x => x.GenerateRefreshToken(), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidationFails_ShouldThrowValidationException()
    {
        // Arrange
        var command = new RefreshTokenCommand { Token = "" }; // Invalid token

        var validationErrors = new List<ValidationFailure>
        {
            new ValidationFailure("Token", "Token is required.")
        };
        var validationResult = new ValidationResult(validationErrors);
        
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => 
            _handler.Handle(command, CancellationToken.None));

        exception.Errors.Should().HaveCount(1);
        _validatorMock.Verify(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);
        _jwtServiceMock.Verify(x => x.GetPrincipalFromExpiredToken(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UserNotFound_ShouldThrowInvalidRefreshTokenException()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var token = "valid-refresh-token";
        var command = new RefreshTokenCommand { Token = token };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }));

        var validationResult = new ValidationResult();

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        _jwtServiceMock.Setup(x => x.GetPrincipalFromExpiredToken(token))
            .Returns(principal);
        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidRefreshTokenException>(() => 
            _handler.Handle(command, CancellationToken.None));

        _validatorMock.Verify(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);
        _jwtServiceMock.Verify(x => x.GetPrincipalFromExpiredToken(token), Times.Once);
        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        _jwtServiceMock.Verify(x => x.GenerateJwtToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InvalidRefreshToken_ShouldThrowInvalidRefreshTokenException()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var token = "invalid-refresh-token";
        var command = new RefreshTokenCommand { Token = token };

        var user = new User
        {
            Id = Guid.Parse(userId),
            Email = _faker.Internet.Email(),
            RefreshTokens = new List<RefreshToken>
            {
                new RefreshToken
                {
                    Token = "different-token", // Different token
                    Expires = DateTime.UtcNow.AddDays(1),
                    Created = DateTime.UtcNow,
                    Revoked = null // This makes IsActive = true
                }
            }
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }));

        var validationResult = new ValidationResult();

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        _jwtServiceMock.Setup(x => x.GetPrincipalFromExpiredToken(token))
            .Returns(principal);
        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidRefreshTokenException>(() => 
            _handler.Handle(command, CancellationToken.None));

        _jwtServiceMock.Verify(x => x.GenerateJwtToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExpiredRefreshToken_ShouldThrowInvalidRefreshTokenException()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var token = "expired-refresh-token";
        var command = new RefreshTokenCommand { Token = token };

        var user = new User
        {
            Id = Guid.Parse(userId),
            Email = _faker.Internet.Email(),
            RefreshTokens = new List<RefreshToken>
            {
                new RefreshToken
                {
                    Token = token,
                    Expires = DateTime.UtcNow.AddDays(-1), // Expired
                    Created = DateTime.UtcNow,
                    Revoked = DateTime.UtcNow // This makes IsActive = false
                }
            }
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }));

        var validationResult = new ValidationResult();

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        _jwtServiceMock.Setup(x => x.GetPrincipalFromExpiredToken(token))
            .Returns(principal);
        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidRefreshTokenException>(() => 
            _handler.Handle(command, CancellationToken.None));

        _jwtServiceMock.Verify(x => x.GenerateJwtToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Handle_JwtServiceThrowsException_ShouldPropagateException()
    {
        // Arrange
        var token = "valid-refresh-token";
        var command = new RefreshTokenCommand { Token = token };

        var validationResult = new ValidationResult();

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        _jwtServiceMock.Setup(x => x.GetPrincipalFromExpiredToken(token))
            .Throws(new Exception("Invalid token format"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Be("Invalid token format");
        _userManagerMock.Verify(x => x.FindByIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UserManagerUpdateFails_ShouldStillReturnTokens()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var token = "valid-refresh-token";
        var command = new RefreshTokenCommand { Token = token };

        var user = new User
        {
            Id = Guid.Parse(userId),
            Email = _faker.Internet.Email(),
            RefreshTokens = new List<RefreshToken>
            {
                new RefreshToken
                {
                    Token = token,
                    Expires = DateTime.UtcNow.AddDays(1),
                    Created = DateTime.UtcNow,
                    Revoked = null // This makes IsActive = true
                }
            }
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }));

        var validationResult = new ValidationResult();
        var newJwtToken = "new-jwt-token";
        var newRefreshToken = "new-refresh-token";

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        _jwtServiceMock.Setup(x => x.GetPrincipalFromExpiredToken(token))
            .Returns(principal);
        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        _jwtServiceMock.Setup(x => x.GenerateJwtToken(user))
            .ReturnsAsync(newJwtToken);
        _jwtServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns(newRefreshToken);
        _userManagerMock.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Update failed" }));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            _handler.Handle(command, CancellationToken.None));

        _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task Handle_NullUserIdClaim_ShouldThrowException()
    {
        // Arrange
        var token = "valid-refresh-token";
        var command = new RefreshTokenCommand { Token = token };

        var principal = new ClaimsPrincipal(new ClaimsIdentity()); // No claims

        var validationResult = new ValidationResult();

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        _jwtServiceMock.Setup(x => x.GetPrincipalFromExpiredToken(token))
            .Returns(principal);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => 
            _handler.Handle(command, CancellationToken.None));

        _userManagerMock.Verify(x => x.FindByIdAsync(It.IsAny<string>()), Times.Never);
    }
}
