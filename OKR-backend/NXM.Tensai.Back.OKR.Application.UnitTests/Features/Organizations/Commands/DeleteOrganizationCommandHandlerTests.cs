using Bogus;
using FluentAssertions;
using Moq;
using NXM.Tensai.Back.OKR.Application.Common.Exceptions;
using NXM.Tensai.Back.OKR.Domain.Entities;
using NXM.Tensai.Back.OKR.Domain.Interfaces.Repositories;
using Xunit;

namespace NXM.Tensai.Back.OKR.Application.UnitTests.Features.Organizations.Commands;

public class DeleteOrganizationCommandHandlerTests
{
    private readonly Mock<IOrganizationRepository> _organizationRepositoryMock;
    private readonly DeleteOrganizationCommandHandler _handler;
    private readonly Faker _faker;

    public DeleteOrganizationCommandHandlerTests()
    {
        _organizationRepositoryMock = new Mock<IOrganizationRepository>();
        _handler = new DeleteOrganizationCommandHandler(_organizationRepositoryMock.Object);
        _faker = new Faker();
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldSoftDeleteOrganization()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var command = new DeleteOrganizationCommand(organizationId);

        var existingOrganization = new Organization
        {
            Id = organizationId,
            Name = _faker.Company.CompanyName(),
            Description = _faker.Lorem.Sentence(),
            IsDeleted = false,
            CreatedDate = DateTime.UtcNow.AddDays(-30),
            ModifiedDate = DateTime.UtcNow.AddDays(-1)
        };

        _organizationRepositoryMock.Setup(x => x.GetByIdAsync(organizationId))
            .ReturnsAsync(existingOrganization);
        _organizationRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Organization>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _organizationRepositoryMock.Verify(x => x.GetByIdAsync(organizationId), Times.Once);
        _organizationRepositoryMock.Verify(x => x.UpdateAsync(It.Is<Organization>(org =>
            org.Id == organizationId &&
            org.IsDeleted == true &&
            org.ModifiedDate > DateTime.UtcNow.AddMinutes(-1))), Times.Once);
    }

    [Fact]
    public async Task Handle_OrganizationNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var command = new DeleteOrganizationCommand(organizationId);

        _organizationRepositoryMock.Setup(x => x.GetByIdAsync(organizationId))
            .ReturnsAsync((Organization?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(() => 
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("Organization");
        exception.Message.Should().Contain(organizationId.ToString());
        _organizationRepositoryMock.Verify(x => x.GetByIdAsync(organizationId), Times.Once);
        _organizationRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Organization>()), Times.Never);
    }

    [Fact]
    public async Task Handle_OrganizationAlreadyDeleted_ShouldStillUpdateModifiedDate()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var command = new DeleteOrganizationCommand(organizationId);

        var existingOrganization = new Organization
        {
            Id = organizationId,
            Name = _faker.Company.CompanyName(),
            IsDeleted = true, // Already deleted
            CreatedDate = DateTime.UtcNow.AddDays(-30),
            ModifiedDate = DateTime.UtcNow.AddDays(-1)
        };

        _organizationRepositoryMock.Setup(x => x.GetByIdAsync(organizationId))
            .ReturnsAsync(existingOrganization);
        _organizationRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Organization>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _organizationRepositoryMock.Verify(x => x.GetByIdAsync(organizationId), Times.Once);
        _organizationRepositoryMock.Verify(x => x.UpdateAsync(It.Is<Organization>(org =>
            org.Id == organizationId &&
            org.IsDeleted == true &&
            org.ModifiedDate > DateTime.UtcNow.AddMinutes(-1))), Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyGuid_ShouldStillTryToFindOrganization()
    {
        // Arrange
        var organizationId = Guid.Empty;
        var command = new DeleteOrganizationCommand(organizationId);

        _organizationRepositoryMock.Setup(x => x.GetByIdAsync(organizationId))
            .ReturnsAsync((Organization?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(() => 
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("Organization");
        _organizationRepositoryMock.Verify(x => x.GetByIdAsync(organizationId), Times.Once);
        _organizationRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Organization>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RepositoryGetByIdThrowsException_ShouldPropagateException()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var command = new DeleteOrganizationCommand(organizationId);

        _organizationRepositoryMock.Setup(x => x.GetByIdAsync(organizationId))
            .ThrowsAsync(new Exception("Database connection error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Be("Database connection error");
        _organizationRepositoryMock.Verify(x => x.GetByIdAsync(organizationId), Times.Once);
        _organizationRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Organization>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RepositoryUpdateThrowsException_ShouldPropagateException()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var command = new DeleteOrganizationCommand(organizationId);

        var existingOrganization = new Organization
        {
            Id = organizationId,
            Name = _faker.Company.CompanyName(),
            IsDeleted = false,
            CreatedDate = DateTime.UtcNow.AddDays(-30)
        };

        _organizationRepositoryMock.Setup(x => x.GetByIdAsync(organizationId))
            .ReturnsAsync(existingOrganization);
        _organizationRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Organization>()))
            .ThrowsAsync(new Exception("Database update error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Be("Database update error");
        _organizationRepositoryMock.Verify(x => x.GetByIdAsync(organizationId), Times.Once);
        _organizationRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Organization>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldPreserveOtherOrganizationProperties()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var command = new DeleteOrganizationCommand(organizationId);

        var existingOrganization = new Organization
        {
            Id = organizationId,
            Name = _faker.Company.CompanyName(),
            Description = _faker.Lorem.Sentence(),
            Country = _faker.Address.Country(),
            Industry = _faker.Commerce.Categories(1).First(),
            Email = _faker.Internet.Email(),
            Phone = _faker.Phone.PhoneNumber(),
            Size = _faker.Random.Int(1, 1000),
            IsActive = true,
            IsDeleted = false,
            CreatedDate = DateTime.UtcNow.AddDays(-30),
            ModifiedDate = DateTime.UtcNow.AddDays(-1)
        };

        var originalName = existingOrganization.Name;
        var originalDescription = existingOrganization.Description;
        var originalCountry = existingOrganization.Country;
        var originalIndustry = existingOrganization.Industry;
        var originalEmail = existingOrganization.Email;
        var originalPhone = existingOrganization.Phone;
        var originalSize = existingOrganization.Size;
        var originalIsActive = existingOrganization.IsActive;
        var originalCreatedDate = existingOrganization.CreatedDate;

        _organizationRepositoryMock.Setup(x => x.GetByIdAsync(organizationId))
            .ReturnsAsync(existingOrganization);
        _organizationRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Organization>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _organizationRepositoryMock.Verify(x => x.UpdateAsync(It.Is<Organization>(org =>
            org.Id == organizationId &&
            org.Name == originalName &&
            org.Description == originalDescription &&
            org.Country == originalCountry &&
            org.Industry == originalIndustry &&
            org.Email == originalEmail &&
            org.Phone == originalPhone &&
            org.Size == originalSize &&
            org.IsActive == originalIsActive &&
            org.CreatedDate == originalCreatedDate &&
            org.IsDeleted == true &&
            org.ModifiedDate > DateTime.UtcNow.AddMinutes(-1))), Times.Once);
    }
}
