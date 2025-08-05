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

public class CreateOrganizationCommandHandlerTests
{
    private readonly Mock<IOrganizationRepository> _organizationRepositoryMock;
    private readonly Mock<IValidator<CreateOrganizationCommand>> _validatorMock;
    private readonly CreateOrganizationCommandHandler _handler;
    private readonly Faker _faker;

    public CreateOrganizationCommandHandlerTests()
    {
        _organizationRepositoryMock = new Mock<IOrganizationRepository>();
        _validatorMock = new Mock<IValidator<CreateOrganizationCommand>>();
        _handler = new CreateOrganizationCommandHandler(
            _organizationRepositoryMock.Object,
            _validatorMock.Object);
        _faker = new Faker();
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateOrganizationAndReturnId()
    {
        // Arrange
        var command = new CreateOrganizationCommand
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

        var validationResult = new ValidationResult();
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        _organizationRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Organization>()))
            .ReturnsAsync((Organization org) => org);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBe(Guid.Empty);
        _validatorMock.Verify(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);
        _organizationRepositoryMock.Verify(x => x.AddAsync(It.Is<Organization>(org =>
            org.Name == command.Name &&
            org.Description == command.Description &&
            org.Country == command.Country &&
            org.Industry == command.Industry &&
            org.Email == command.Email &&
            org.Phone == command.Phone &&
            org.Size == command.Size &&
            org.IsActive == command.IsActive &&
            org.Id != Guid.Empty)), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidationFails_ShouldThrowValidationException()
    {
        // Arrange
        var command = new CreateOrganizationCommand
        {
            Name = "", // Invalid name
            Email = "invalid-email" // Invalid email
        };

        var validationErrors = new List<ValidationFailure>
        {
            new ValidationFailure("Name", "Organization name is required."),
            new ValidationFailure("Email", "Email is not valid.")
        };
        var validationResult = new ValidationResult(validationErrors);
        
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => 
            _handler.Handle(command, CancellationToken.None));

        exception.Errors.Should().HaveCount(2);
        _validatorMock.Verify(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);
        _organizationRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Organization>()), Times.Never);
    }

    [Fact]
    public async Task Handle_MinimalValidCommand_ShouldCreateOrganization()
    {
        // Arrange
        var command = new CreateOrganizationCommand
        {
            Name = _faker.Company.CompanyName(),
            IsActive = false
        };

        var validationResult = new ValidationResult();
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        _organizationRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Organization>()))
            .ReturnsAsync((Organization org) => org);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBe(Guid.Empty);
        _validatorMock.Verify(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);
        _organizationRepositoryMock.Verify(x => x.AddAsync(It.Is<Organization>(org =>
            org.Name == command.Name &&
            org.Description == null &&
            org.Country == null &&
            org.Industry == null &&
            org.Email == null &&
            org.Phone == null &&
            org.Size == null &&
            org.IsActive == false)), Times.Once);
    }

    [Fact]
    public async Task Handle_RepositoryThrowsException_ShouldPropagateException()
    {
        // Arrange
        var command = new CreateOrganizationCommand
        {
            Name = _faker.Company.CompanyName(),
            IsActive = true
        };

        var validationResult = new ValidationResult();
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        _organizationRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Organization>()))
            .ThrowsAsync(new Exception("Database connection error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Be("Database connection error");
        _validatorMock.Verify(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);
        _organizationRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Organization>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CommandWithMaximumLength_ShouldCreateOrganization()
    {
        // Arrange
        var command = new CreateOrganizationCommand
        {
            Name = new string('A', 100), // Max length
            Description = new string('B', 500), // Max length
            Country = new string('C', 100), // Max length
            Industry = new string('D', 100), // Max length
            Email = "test@" + new string('e', 94) + ".com", // Max length email
            Phone = new string('1', 20), // Max length
            Size = 100000, // Max size
            IsActive = true
        };

        var validationResult = new ValidationResult();
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        _organizationRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Organization>()))
            .ReturnsAsync((Organization org) => org);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBe(Guid.Empty);
        _organizationRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Organization>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidatorThrowsException_ShouldPropagateException()
    {
        // Arrange
        var command = new CreateOrganizationCommand
        {
            Name = _faker.Company.CompanyName()
        };

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Validator error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Be("Validator error");
        _validatorMock.Verify(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);
        _organizationRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Organization>()), Times.Never);
    }
}
