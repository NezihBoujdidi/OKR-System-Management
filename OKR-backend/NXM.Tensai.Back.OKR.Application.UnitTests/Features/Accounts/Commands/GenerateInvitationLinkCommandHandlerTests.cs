using Bogus;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Moq;
using NXM.Tensai.Back.OKR.Domain.Entities;
using NXM.Tensai.Back.OKR.Domain.Interfaces;
using NXM.Tensai.Back.OKR.Domain.Interfaces.Repositories;
using Xunit;
using ValidationException = FluentValidation.ValidationException;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace NXM.Tensai.Back.OKR.Application.UnitTests.Features.Accounts.Commands;

public class GenerateInvitationLinkCommandHandlerTests
{
    private readonly Mock<IInvitationLinkRepository> _invitationLinkRepositoryMock;
    private readonly Mock<IEmailSender> _emailSenderMock;
    private readonly Mock<IValidator<GenerateInvitationLinkCommand>> _validatorMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly Mock<RoleManager<Role>> _roleManagerMock;
    private readonly GenerateInvitationLinkCommandHandler _handler;
    private readonly Faker _faker;

    public GenerateInvitationLinkCommandHandlerTests()
    {
        _invitationLinkRepositoryMock = new Mock<IInvitationLinkRepository>();
        _emailSenderMock = new Mock<IEmailSender>();
        _validatorMock = new Mock<IValidator<GenerateInvitationLinkCommand>>();
        _jwtServiceMock = new Mock<IJwtService>();
        _roleManagerMock = new Mock<RoleManager<Role>>(
            Mock.Of<IRoleStore<Role>>(), null, null, null, null);
        _handler = new GenerateInvitationLinkCommandHandler(
            _invitationLinkRepositoryMock.Object,
            _emailSenderMock.Object,
            _validatorMock.Object,
            _jwtServiceMock.Object,
            _roleManagerMock.Object);
        _faker = new Faker();
    }    [Fact]
    public async Task Handle_WithValidRequest_Should_GenerateInvitationLinkSuccessfully()
    {
        // Arrange
        var command = new GenerateInvitationLinkCommand
        {
            Email = _faker.Internet.Email(),
            RoleName = "Admin",
            OrganizationId = Guid.NewGuid(),
            TeamId = Guid.NewGuid()
        };

        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = command.RoleName
        };

        var jwtToken = _faker.Random.AlphaNumeric(128);

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _roleManagerMock.Setup(x => x.FindByNameAsync(command.RoleName))
            .ReturnsAsync(role);

        _jwtServiceMock.Setup(x => x.GenerateJwtToken(
            It.IsAny<User>(), command.RoleName, command.OrganizationId, command.TeamId))
            .ReturnsAsync(jwtToken);

        _invitationLinkRepositoryMock.Setup(x => x.AddAsync(It.IsAny<InvitationLink>()))
            .ReturnsAsync(new InvitationLink());

        _emailSenderMock.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _roleManagerMock.Verify(x => x.FindByNameAsync(command.RoleName), Times.Once);
        _jwtServiceMock.Verify(x => x.GenerateJwtToken(
            It.Is<User>(u => u.Email == command.Email),
            command.RoleName,
            command.OrganizationId,
            command.TeamId), Times.Once);
        _invitationLinkRepositoryMock.Verify(x => x.AddAsync(It.Is<InvitationLink>(il =>
            il.Email == command.Email &&
            il.Role == role &&
            il.Token == jwtToken &&
            il.OrganizationId == command.OrganizationId &&
            il.TeamId == command.TeamId)), Times.Once);
        _emailSenderMock.Verify(x => x.SendEmailAsync(
            command.Email,
            "Invitation to join our platform",
            It.Is<string>(body => body.Contains($"token={jwtToken}"))), Times.Once);
    }    [Fact]
    public async Task Handle_WithValidRequestWithoutTeamId_Should_GenerateInvitationLinkSuccessfully()
    {
        // Arrange
        var command = new GenerateInvitationLinkCommand
        {
            Email = _faker.Internet.Email(),
            RoleName = "User",
            OrganizationId = Guid.NewGuid(),
            TeamId = null
        };

        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = command.RoleName
        };

        var jwtToken = _faker.Random.AlphaNumeric(128);

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _roleManagerMock.Setup(x => x.FindByNameAsync(command.RoleName))
            .ReturnsAsync(role);

        _jwtServiceMock.Setup(x => x.GenerateJwtToken(
            It.IsAny<User>(), command.RoleName, command.OrganizationId, command.TeamId))
            .ReturnsAsync(jwtToken);

        _invitationLinkRepositoryMock.Setup(x => x.AddAsync(It.IsAny<InvitationLink>()))
            .ReturnsAsync(new InvitationLink());

        _emailSenderMock.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _invitationLinkRepositoryMock.Verify(x => x.AddAsync(It.Is<InvitationLink>(il =>
            il.Email == command.Email &&
            il.Role == role &&
            il.Token == jwtToken &&
            il.OrganizationId == command.OrganizationId &&
            il.TeamId == null)), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidValidation_Should_ThrowValidationException()
    {
        // Arrange
        var command = new GenerateInvitationLinkCommand
        {
            Email = "",
            RoleName = "",
            OrganizationId = Guid.Empty,
            TeamId = null
        };

        var validationFailures = new List<ValidationFailure>
        {
            new("Email", "Email is required."),
            new("RoleName", "Role name is required."),
            new("OrganizationId", "Organization ID is required.")
        };

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>();

        _roleManagerMock.Verify(x => x.FindByNameAsync(It.IsAny<string>()), Times.Never);
        _jwtServiceMock.Verify(x => x.GenerateJwtToken(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid?>()), Times.Never);
        _invitationLinkRepositoryMock.Verify(x => x.AddAsync(It.IsAny<InvitationLink>()), Times.Never);
        _emailSenderMock.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithNonExistentRole_Should_ThrowKeyNotFoundException()
    {
        // Arrange
        var command = new GenerateInvitationLinkCommand
        {
            Email = _faker.Internet.Email(),
            RoleName = "NonExistentRole",
            OrganizationId = Guid.NewGuid(),
            TeamId = Guid.NewGuid()
        };

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _roleManagerMock.Setup(x => x.FindByNameAsync(command.RoleName))
            .ReturnsAsync((Role)null);

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Role with name {command.RoleName} not found.");

        _roleManagerMock.Verify(x => x.FindByNameAsync(command.RoleName), Times.Once);
        _jwtServiceMock.Verify(x => x.GenerateJwtToken(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid?>()), Times.Never);
        _invitationLinkRepositoryMock.Verify(x => x.AddAsync(It.IsAny<InvitationLink>()), Times.Never);
        _emailSenderMock.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithJwtServiceFailure_Should_PropagateException()
    {
        // Arrange
        var command = new GenerateInvitationLinkCommand
        {
            Email = _faker.Internet.Email(),
            RoleName = "Admin",
            OrganizationId = Guid.NewGuid(),
            TeamId = Guid.NewGuid()
        };

        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = command.RoleName
        };

        var expectedException = new InvalidOperationException("JWT generation failed");

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _roleManagerMock.Setup(x => x.FindByNameAsync(command.RoleName))
            .ReturnsAsync(role);

        _jwtServiceMock.Setup(x => x.GenerateJwtToken(
            It.IsAny<User>(), command.RoleName, command.OrganizationId, command.TeamId))
            .ThrowsAsync(expectedException);

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("JWT generation failed");

        _roleManagerMock.Verify(x => x.FindByNameAsync(command.RoleName), Times.Once);
        _jwtServiceMock.Verify(x => x.GenerateJwtToken(It.IsAny<User>(), command.RoleName, command.OrganizationId, command.TeamId), Times.Once);
        _invitationLinkRepositoryMock.Verify(x => x.AddAsync(It.IsAny<InvitationLink>()), Times.Never);
        _emailSenderMock.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithRepositoryFailure_Should_PropagateException()
    {
        // Arrange
        var command = new GenerateInvitationLinkCommand
        {
            Email = _faker.Internet.Email(),
            RoleName = "Admin",
            OrganizationId = Guid.NewGuid(),
            TeamId = Guid.NewGuid()
        };

        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = command.RoleName
        };

        var jwtToken = _faker.Random.AlphaNumeric(128);
        var expectedException = new InvalidOperationException("Database save failed");

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _roleManagerMock.Setup(x => x.FindByNameAsync(command.RoleName))
            .ReturnsAsync(role);

        _jwtServiceMock.Setup(x => x.GenerateJwtToken(
            It.IsAny<User>(), command.RoleName, command.OrganizationId, command.TeamId))
            .ReturnsAsync(jwtToken);

        _invitationLinkRepositoryMock.Setup(x => x.AddAsync(It.IsAny<InvitationLink>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database save failed");

        _roleManagerMock.Verify(x => x.FindByNameAsync(command.RoleName), Times.Once);
        _jwtServiceMock.Verify(x => x.GenerateJwtToken(It.IsAny<User>(), command.RoleName, command.OrganizationId, command.TeamId), Times.Once);
        _invitationLinkRepositoryMock.Verify(x => x.AddAsync(It.IsAny<InvitationLink>()), Times.Once);
        _emailSenderMock.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithEmailSenderFailure_Should_PropagateException()
    {
        // Arrange
        var command = new GenerateInvitationLinkCommand
        {
            Email = _faker.Internet.Email(),
            RoleName = "Admin",
            OrganizationId = Guid.NewGuid(),
            TeamId = Guid.NewGuid()
        };

        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = command.RoleName
        };

        var jwtToken = _faker.Random.AlphaNumeric(128);
        var expectedException = new InvalidOperationException("Email service unavailable");

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _roleManagerMock.Setup(x => x.FindByNameAsync(command.RoleName))
            .ReturnsAsync(role);        _jwtServiceMock.Setup(x => x.GenerateJwtToken(
            It.IsAny<User>(), command.RoleName, command.OrganizationId, command.TeamId))
            .ReturnsAsync(jwtToken);

        _invitationLinkRepositoryMock.Setup(x => x.AddAsync(It.IsAny<InvitationLink>()))
            .ReturnsAsync(new InvitationLink());

        _emailSenderMock.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Email service unavailable");

        _roleManagerMock.Verify(x => x.FindByNameAsync(command.RoleName), Times.Once);
        _jwtServiceMock.Verify(x => x.GenerateJwtToken(It.IsAny<User>(), command.RoleName, command.OrganizationId, command.TeamId), Times.Once);
        _invitationLinkRepositoryMock.Verify(x => x.AddAsync(It.IsAny<InvitationLink>()), Times.Once);
        _emailSenderMock.Verify(x => x.SendEmailAsync(command.Email, "Invitation to join our platform", It.IsAny<string>()), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("invalid-email")]
    public async Task Handle_WithInvalidEmail_Should_ThrowValidationException(string invalidEmail)
    {
        // Arrange
        var command = new GenerateInvitationLinkCommand
        {
            Email = invalidEmail,
            RoleName = "Admin",
            OrganizationId = Guid.NewGuid(),
            TeamId = Guid.NewGuid()
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

        _roleManagerMock.Verify(x => x.FindByNameAsync(It.IsAny<string>()), Times.Never);
        _jwtServiceMock.Verify(x => x.GenerateJwtToken(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid?>()), Times.Never);
    }    [Fact]
    public async Task Handle_WithValidRequest_Should_CreateCorrectInvitationLinkUrl()
    {
        // Arrange
        var command = new GenerateInvitationLinkCommand
        {
            Email = _faker.Internet.Email(),
            RoleName = "Admin",
            OrganizationId = Guid.NewGuid(),
            TeamId = Guid.NewGuid()
        };

        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = command.RoleName
        };

        var jwtToken = "test-jwt-token";

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _roleManagerMock.Setup(x => x.FindByNameAsync(command.RoleName))
            .ReturnsAsync(role);

        _jwtServiceMock.Setup(x => x.GenerateJwtToken(
            It.IsAny<User>(), command.RoleName, command.OrganizationId, command.TeamId))
            .ReturnsAsync(jwtToken);

        _invitationLinkRepositoryMock.Setup(x => x.AddAsync(It.IsAny<InvitationLink>()))
            .ReturnsAsync(new InvitationLink());

        _emailSenderMock.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _emailSenderMock.Verify(x => x.SendEmailAsync(
            command.Email,
            "Invitation to join our platform",
            It.Is<string>(body => body.Contains($"http://localhost:4200/signup?token={jwtToken}"))), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidRequest_Should_SetCorrectExpirationDate()
    {
        // Arrange
        var command = new GenerateInvitationLinkCommand
        {
            Email = _faker.Internet.Email(),
            RoleName = "Admin",
            OrganizationId = Guid.NewGuid(),
            TeamId = Guid.NewGuid()
        };

        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = command.RoleName
        };

        var jwtToken = _faker.Random.AlphaNumeric(128);

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _roleManagerMock.Setup(x => x.FindByNameAsync(command.RoleName))
            .ReturnsAsync(role);        _jwtServiceMock.Setup(x => x.GenerateJwtToken(
            It.IsAny<User>(), command.RoleName, command.OrganizationId, command.TeamId))
            .ReturnsAsync(jwtToken);

        _invitationLinkRepositoryMock.Setup(x => x.AddAsync(It.IsAny<InvitationLink>()))
            .ReturnsAsync(new InvitationLink());

        _emailSenderMock.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var beforeExecutionTime = DateTime.UtcNow;

        // Act
        await _handler.Handle(command, CancellationToken.None);

        var afterExecutionTime = DateTime.UtcNow;

        // Assert
        _invitationLinkRepositoryMock.Verify(x => x.AddAsync(It.Is<InvitationLink>(il =>
            il.ExpirationDate >= beforeExecutionTime.AddHours(23) &&
            il.ExpirationDate <= afterExecutionTime.AddHours(25) &&
            il.CreatedAt >= beforeExecutionTime &&
            il.CreatedAt <= afterExecutionTime)), Times.Once);
    }
}
