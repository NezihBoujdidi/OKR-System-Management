using Bogus;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using NXM.Tensai.Back.OKR.Application.Common.Exceptions;
using NXM.Tensai.Back.OKR.Domain.Entities;
using NXM.Tensai.Back.OKR.Domain.Interfaces.Repositories;
using Xunit;
using ValidationException = FluentValidation.ValidationException;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace NXM.Tensai.Back.OKR.Application.UnitTests.Features.Organizations.Commands;

public class UpdateOrganizationCommandHandlerTests
{
    private readonly Mock<IOrganizationRepository> _organizationRepositoryMock;
    private readonly Mock<IValidator<UpdateOrganizationCommand>> _validatorMock;
    private readonly UpdateOrganizationCommandHandler _handler;
    private readonly Faker _faker;

    public UpdateOrganizationCommandHandlerTests()
    {
        _organizationRepositoryMock = new Mock<IOrganizationRepository>();
        _validatorMock = new Mock<IValidator<UpdateOrganizationCommand>>();
        _handler = new UpdateOrganizationCommandHandler(
            _organizationRepositoryMock.Object,
            _validatorMock.Object);
        _faker = new Faker();
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldUpdateOrganization()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var updateCommand = new UpdateOrganizationCommand
        {
            Name = _faker.Company.CompanyName(),
            Description = _faker.Lorem.Sentence(),
            Country = _faker.Address.Country(),
            Industry = _faker.Commerce.Categories(1).First(),
            Email = _faker.Internet.Email(),
            Phone = _faker.Phone.PhoneNumber(),
            Size = _faker.Random.Int(1, 100000),
            IsActive = true
        };

        var commandWithId = new UpdateOrganizationCommandWithId(organizationId, updateCommand);

        var existingOrganization = new Organization
        {
            Id = organizationId,
            Name = "Old Name",
            Description = "Old Description",
            Country = "Old Country",
            Industry = "Old Industry",
            Email = "old@email.com",
            Phone = "123456789",
            Size = 50,
            IsActive = false,
            CreatedDate = DateTime.UtcNow.AddDays(-30)
        };

        var validationResult = new ValidationResult();
        _validatorMock.Setup(x => x.ValidateAsync(updateCommand, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        _organizationRepositoryMock.Setup(x => x.GetByIdAsync(organizationId))
            .ReturnsAsync(existingOrganization);
        _organizationRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Organization>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(commandWithId, CancellationToken.None);

        // Assert
        _validatorMock.Verify(x => x.ValidateAsync(updateCommand, It.IsAny<CancellationToken>()), Times.Once);
        _organizationRepositoryMock.Verify(x => x.GetByIdAsync(organizationId), Times.Once);
        _organizationRepositoryMock.Verify(x => x.UpdateAsync(It.Is<Organization>(org =>
            org.Id == organizationId &&
            org.Name == updateCommand.Name &&
            org.Description == updateCommand.Description &&
            org.Country == updateCommand.Country &&
            org.Industry == updateCommand.Industry &&
            org.Email == updateCommand.Email &&
            org.Phone == updateCommand.Phone &&
            org.Size == updateCommand.Size &&
            org.IsActive == updateCommand.IsActive &&
            org.ModifiedDate > DateTime.UtcNow.AddMinutes(-1))), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidationFails_ShouldThrowValidationException()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var updateCommand = new UpdateOrganizationCommand
        {
            Name = "", // Invalid name
            Email = "invalid-email" // Invalid email
        };

        var commandWithId = new UpdateOrganizationCommandWithId(organizationId, updateCommand);

        var validationErrors = new List<ValidationFailure>
        {
            new ValidationFailure("Name", "Organization name is required."),
            new ValidationFailure("Email", "Email is not valid.")
        };
        var validationResult = new ValidationResult(validationErrors);
        
        _validatorMock.Setup(x => x.ValidateAsync(updateCommand, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => 
            _handler.Handle(commandWithId, CancellationToken.None));

        exception.Errors.Should().HaveCount(2);
        _validatorMock.Verify(x => x.ValidateAsync(updateCommand, It.IsAny<CancellationToken>()), Times.Once);
        _organizationRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _organizationRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Organization>()), Times.Never);
    }

    [Fact]
    public async Task Handle_OrganizationNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var updateCommand = new UpdateOrganizationCommand
        {
            Name = _faker.Company.CompanyName()
        };

        var commandWithId = new UpdateOrganizationCommandWithId(organizationId, updateCommand);

        var validationResult = new ValidationResult();
        _validatorMock.Setup(x => x.ValidateAsync(updateCommand, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        _organizationRepositoryMock.Setup(x => x.GetByIdAsync(organizationId))
            .ReturnsAsync((Organization?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(() => 
            _handler.Handle(commandWithId, CancellationToken.None));

        exception.Message.Should().Contain("Organization");
        exception.Message.Should().Contain(organizationId.ToString());
        _validatorMock.Verify(x => x.ValidateAsync(updateCommand, It.IsAny<CancellationToken>()), Times.Once);
        _organizationRepositoryMock.Verify(x => x.GetByIdAsync(organizationId), Times.Once);
        _organizationRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Organization>()), Times.Never);
    }

    [Fact]
    public async Task Handle_IsActiveIsNull_ShouldNotUpdateIsActiveProperty()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var updateCommand = new UpdateOrganizationCommand
        {
            Name = _faker.Company.CompanyName(),
            Description = _faker.Lorem.Sentence(),
            IsActive = null // Should not update IsActive
        };

        var commandWithId = new UpdateOrganizationCommandWithId(organizationId, updateCommand);

        var existingOrganization = new Organization
        {
            Id = organizationId,
            Name = "Old Name",
            Description = "Old Description",
            IsActive = true, // Should remain true
            CreatedDate = DateTime.UtcNow.AddDays(-30)
        };

        var validationResult = new ValidationResult();
        _validatorMock.Setup(x => x.ValidateAsync(updateCommand, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        _organizationRepositoryMock.Setup(x => x.GetByIdAsync(organizationId))
            .ReturnsAsync(existingOrganization);
        _organizationRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Organization>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(commandWithId, CancellationToken.None);

        // Assert
        _organizationRepositoryMock.Verify(x => x.UpdateAsync(It.Is<Organization>(org =>
            org.Id == organizationId &&
            org.IsActive == true)), Times.Once); // Should remain true
    }

    [Fact]
    public async Task Handle_PartialUpdate_ShouldUpdateOnlySpecifiedFields()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var updateCommand = new UpdateOrganizationCommand
        {
            Name = _faker.Company.CompanyName(),
            // Only updating name, other fields remain null
        };

        var commandWithId = new UpdateOrganizationCommandWithId(organizationId, updateCommand);

        var existingOrganization = new Organization
        {
            Id = organizationId,
            Name = "Old Name",
            Description = "Existing Description",
            Country = "Existing Country",
            IsActive = true,
            CreatedDate = DateTime.UtcNow.AddDays(-30)
        };

        var validationResult = new ValidationResult();
        _validatorMock.Setup(x => x.ValidateAsync(updateCommand, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        _organizationRepositoryMock.Setup(x => x.GetByIdAsync(organizationId))
            .ReturnsAsync(existingOrganization);
        _organizationRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Organization>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(commandWithId, CancellationToken.None);

        // Assert
        _organizationRepositoryMock.Verify(x => x.UpdateAsync(It.Is<Organization>(org =>
            org.Id == organizationId &&
            org.Name == updateCommand.Name &&
            org.Description == null && // Updated to null from updateCommand
            org.Country == null)), Times.Once); // Updated to null from updateCommand
    }

    [Fact]
    public async Task Handle_RepositoryUpdateThrowsException_ShouldPropagateException()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var updateCommand = new UpdateOrganizationCommand
        {
            Name = _faker.Company.CompanyName()
        };

        var commandWithId = new UpdateOrganizationCommandWithId(organizationId, updateCommand);

        var existingOrganization = new Organization
        {
            Id = organizationId,
            Name = "Old Name",
            CreatedDate = DateTime.UtcNow.AddDays(-30)
        };

        var validationResult = new ValidationResult();
        _validatorMock.Setup(x => x.ValidateAsync(updateCommand, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        _organizationRepositoryMock.Setup(x => x.GetByIdAsync(organizationId))
            .ReturnsAsync(existingOrganization);
        _organizationRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Organization>()))
            .ThrowsAsync(new Exception("Database update error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            _handler.Handle(commandWithId, CancellationToken.None));

        exception.Message.Should().Be("Database update error");
        _organizationRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Organization>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RepositoryGetByIdThrowsException_ShouldPropagateException()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var updateCommand = new UpdateOrganizationCommand
        {
            Name = _faker.Company.CompanyName()
        };

        var commandWithId = new UpdateOrganizationCommandWithId(organizationId, updateCommand);

        var validationResult = new ValidationResult();
        _validatorMock.Setup(x => x.ValidateAsync(updateCommand, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        _organizationRepositoryMock.Setup(x => x.GetByIdAsync(organizationId))
            .ThrowsAsync(new Exception("Database connection error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            _handler.Handle(commandWithId, CancellationToken.None));

        exception.Message.Should().Be("Database connection error");
        _organizationRepositoryMock.Verify(x => x.GetByIdAsync(organizationId), Times.Once);
        _organizationRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Organization>()), Times.Never);
    }
}
