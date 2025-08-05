using Bogus;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NXM.Tensai.Back.OKR.Application.Common.Exceptions;
using NXM.Tensai.Back.OKR.Application;
using NXM.Tensai.Back.OKR.Domain;
using ValidationException = NXM.Tensai.Back.OKR.Application.Common.Exceptions.ValidationException;

namespace NXM.Tensai.Back.OKR.API.UnitTests.Controllers;

public class OrganizationsControllerTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ILogger<OrganizationsController>> _mockLogger;
    private readonly OrganizationsController _controller;
    private readonly Faker<CreateOrganizationCommand> _createOrganizationFaker;
    private readonly Faker<UpdateOrganizationCommand> _updateOrganizationFaker;
    private readonly Faker<SearchOrganizationsQuery> _searchOrganizationsQueryFaker;
    private readonly Faker<OrganizationDto> _organizationDtoFaker;
    private readonly Faker<PaginatedListResult<OrganizationDto>> _paginatedOrganizationResultFaker;

    public OrganizationsControllerTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<OrganizationsController>>();
        _controller = new OrganizationsController(_mockMediator.Object, _mockLogger.Object);

        // Setup fakers for consistent test data generation
        _createOrganizationFaker = new Faker<CreateOrganizationCommand>()
            .RuleFor(c => c.Name, f => f.Company.CompanyName())
            .RuleFor(c => c.Description, f => f.Lorem.Sentence())
            .RuleFor(c => c.Country, f => f.Address.Country())
            .RuleFor(c => c.Industry, f => f.PickRandom("Technology", "Healthcare", "Finance", "Manufacturing"))
            .RuleFor(c => c.Email, f => f.Internet.Email())
            .RuleFor(c => c.Phone, f => f.Phone.PhoneNumber())
            .RuleFor(c => c.Size, f => f.Random.Int(1, 10000))
            .RuleFor(c => c.IsActive, f => f.Random.Bool());

        _updateOrganizationFaker = new Faker<UpdateOrganizationCommand>()
            .RuleFor(c => c.Name, f => f.Company.CompanyName())
            .RuleFor(c => c.Description, f => f.Lorem.Sentence())
            .RuleFor(c => c.Country, f => f.Address.Country())
            .RuleFor(c => c.Industry, f => f.PickRandom("Technology", "Healthcare", "Finance", "Manufacturing"))
            .RuleFor(c => c.Email, f => f.Internet.Email())
            .RuleFor(c => c.Phone, f => f.Phone.PhoneNumber())
            .RuleFor(c => c.Size, f => f.Random.Int(1, 10000))
            .RuleFor(c => c.IsActive, f => f.Random.Bool());

        _searchOrganizationsQueryFaker = new Faker<SearchOrganizationsQuery>()
            .RuleFor(q => q.Name, f => f.Company.CompanyName())
            .RuleFor(q => q.Country, f => f.Address.Country())
            .RuleFor(q => q.Industry, f => f.PickRandom("Technology", "Healthcare", "Finance"))
            .RuleFor(q => q.Page, f => f.Random.Int(1, 5))
            .RuleFor(q => q.PageSize, f => f.Random.Int(5, 20));

        _organizationDtoFaker = new Faker<OrganizationDto>()
            .RuleFor(o => o.Id, f => f.Random.Guid())
            .RuleFor(o => o.EncodedId, f => f.Random.String2(10))
            .RuleFor(o => o.Name, f => f.Company.CompanyName())
            .RuleFor(o => o.Description, f => f.Lorem.Sentence())
            .RuleFor(o => o.Country, f => f.Address.Country())
            .RuleFor(o => o.Industry, f => f.PickRandom("Technology", "Healthcare", "Finance"))
            .RuleFor(o => o.Email, f => f.Internet.Email())
            .RuleFor(o => o.Phone, f => f.Phone.PhoneNumber())
            .RuleFor(o => o.CreatedDate, f => f.Date.Past(1))
            .RuleFor(o => o.ModifiedDate, f => f.Date.Recent())
            .RuleFor(o => o.Size, f => f.Random.Int(1, 10000))
            .RuleFor(o => o.IsActive, f => f.Random.Bool())
            .RuleFor(o => o.SubscriptionPlan, f => f.PickRandom("Basic", "Pro", "Enterprise"));

        _paginatedOrganizationResultFaker = new Faker<PaginatedListResult<OrganizationDto>>()
            .CustomInstantiator(f =>
            {
                var organizations = _organizationDtoFaker.Generate(f.Random.Int(1, 5));
                return new PaginatedListResult<OrganizationDto>(organizations, organizations.Count, 1, 1);
            });
    }

    #region CreateOrganization Tests

    [Fact]
    public async Task CreateOrganization_WithValidCommand_Should_ReturnOkResultWithId()
    {
        // Arrange
        var command = _createOrganizationFaker.Generate();
        var expectedOrganizationId = Guid.NewGuid();

        _mockMediator.Setup(m => m.Send(It.IsAny<CreateOrganizationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedOrganizationId);

        // Act
        var result = await _controller.CreateOrganization(command);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(new { Id = expectedOrganizationId, Message = "Organization created successfully." });

        // Verify mediator was called with the expected command
        _mockMediator.Verify(m => m.Send(It.IsAny<CreateOrganizationCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrganization_WithValidationException_Should_ReturnBadRequestResult()
    {
        // Arrange
        var command = _createOrganizationFaker.Generate();
        var validationErrors = new Dictionary<string, string[]>
        {
            { "Name", new[] { "Organization name is required", "Organization name must be at most 100 characters long" } },
            { "Email", new[] { "Invalid email format" } }
        };
        var validationException = new ValidationException(validationErrors);

        _mockMediator.Setup(m => m.Send(It.IsAny<CreateOrganizationCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(validationException);

        // Act
        var result = await _controller.CreateOrganization(command);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.StatusCode.Should().Be(400);
        badRequestResult.Value.Should().BeEquivalentTo(validationErrors);
    }

    [Fact]
    public async Task CreateOrganization_WithUnexpectedException_Should_ReturnInternalServerError()
    {
        // Arrange
        var command = _createOrganizationFaker.Generate();
        var exception = new Exception("Unexpected database error");

        _mockMediator.Setup(m => m.Send(It.IsAny<CreateOrganizationCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.CreateOrganization(command);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = result as ObjectResult;
        statusCodeResult!.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("An unexpected error occurred.");
    }

    #endregion

    #region UpdateOrganization Tests

    [Fact]
    public async Task UpdateOrganization_WithValidCommand_Should_ReturnOkResult()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var command = _updateOrganizationFaker.Generate();

        _mockMediator.Setup(m => m.Send(It.IsAny<UpdateOrganizationCommandWithId>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateOrganization(organizationId, command);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be("Organization updated successfully.");

        // Verify mediator was called with the correct command
        _mockMediator.Verify(m => m.Send(
            It.Is<UpdateOrganizationCommandWithId>(c => c.Id == organizationId && c.Command == command),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateOrganization_WithValidationException_Should_ReturnBadRequestResult()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var command = _updateOrganizationFaker.Generate();
        var validationErrors = new Dictionary<string, string[]>
        {
            { "Name", new[] { "Organization name is required" } },
            { "Size", new[] { "Size must be greater than 0" } }
        };
        var validationException = new ValidationException(validationErrors);

        _mockMediator.Setup(m => m.Send(It.IsAny<UpdateOrganizationCommandWithId>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(validationException);

        // Act
        var result = await _controller.UpdateOrganization(organizationId, command);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.StatusCode.Should().Be(400);
        badRequestResult.Value.Should().BeEquivalentTo(validationErrors);
    }

    [Fact]
    public async Task UpdateOrganization_WithNotFoundException_Should_ReturnNotFoundResult()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var command = _updateOrganizationFaker.Generate();
        var errorMessage = $"Entity 'Organization' with id '{organizationId}' was not found.";
        var notFoundException = new NotFoundException(errorMessage);

        _mockMediator.Setup(m => m.Send(It.IsAny<UpdateOrganizationCommandWithId>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(notFoundException);

        // Act
        var result = await _controller.UpdateOrganization(organizationId, command);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.Value.Should().Be(errorMessage);
    }

    [Fact]
    public async Task UpdateOrganization_WithUnexpectedException_Should_ReturnInternalServerError()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var command = _updateOrganizationFaker.Generate();
        var exception = new Exception("Unexpected database error");

        _mockMediator.Setup(m => m.Send(It.IsAny<UpdateOrganizationCommandWithId>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.UpdateOrganization(organizationId, command);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = result as ObjectResult;
        statusCodeResult!.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("An unexpected error occurred.");
    }

    #endregion

    #region DeleteOrganization Tests

    [Fact]
    public async Task DeleteOrganization_WithValidId_Should_ReturnOkResult()
    {
        // Arrange
        var organizationId = Guid.NewGuid();

        _mockMediator.Setup(m => m.Send(It.IsAny<DeleteOrganizationCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteOrganization(organizationId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be("Organization deleted successfully.");

        // Verify mediator was called with the correct command
        _mockMediator.Verify(m => m.Send(
            It.Is<DeleteOrganizationCommand>(c => c.Id == organizationId),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteOrganization_WithNotFoundException_Should_ReturnNotFoundResult()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var errorMessage = $"Entity 'Organization' with id '{organizationId}' was not found.";
        var notFoundException = new NotFoundException(errorMessage);

        _mockMediator.Setup(m => m.Send(It.IsAny<DeleteOrganizationCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(notFoundException);

        // Act
        var result = await _controller.DeleteOrganization(organizationId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.Value.Should().Be(errorMessage);
    }

    [Fact]
    public async Task DeleteOrganization_WithUnexpectedException_Should_ReturnInternalServerError()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var exception = new Exception("Unexpected database error");

        _mockMediator.Setup(m => m.Send(It.IsAny<DeleteOrganizationCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.DeleteOrganization(organizationId);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = result as ObjectResult;
        statusCodeResult!.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("An unexpected error occurred.");
    }

    #endregion

    #region GetAllOrganizations Tests

    [Fact]
    public async Task GetAllOrganizations_WithValidQuery_Should_ReturnOkResultWithOrganizations()
    {
        // Arrange
        var query = _searchOrganizationsQueryFaker.Generate();
        var expectedResult = _paginatedOrganizationResultFaker.Generate();

        _mockMediator.Setup(m => m.Send(It.IsAny<SearchOrganizationsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetAllOrganizations(query);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedResult);

        // Verify mediator was called with the correct query
        _mockMediator.Verify(m => m.Send(
            It.IsAny<SearchOrganizationsQuery>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAllOrganizations_WithValidationException_Should_ReturnBadRequestResult()
    {
        // Arrange
        var query = _searchOrganizationsQueryFaker.Generate();
        var validationErrors = new Dictionary<string, string[]>
        {
            { "Page", new[] { "Page must be greater than or equal to 1" } },
            { "PageSize", new[] { "Page size must be greater than or equal to 1" } }
        };
        var validationException = new ValidationException(validationErrors);

        _mockMediator.Setup(m => m.Send(It.IsAny<SearchOrganizationsQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(validationException);

        // Act
        var result = await _controller.GetAllOrganizations(query);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.StatusCode.Should().Be(400);
        badRequestResult.Value.Should().BeEquivalentTo(validationErrors);
    }

    [Fact]
    public async Task GetAllOrganizations_WithUnexpectedException_Should_ReturnInternalServerError()
    {
        // Arrange
        var query = _searchOrganizationsQueryFaker.Generate();
        var exception = new Exception("Unexpected database error");

        _mockMediator.Setup(m => m.Send(It.IsAny<SearchOrganizationsQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.GetAllOrganizations(query);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = result as ObjectResult;
        statusCodeResult!.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("An unexpected error occurred.");
    }

    #endregion

    #region GetOrganizationById Tests

    [Fact]
    public async Task GetOrganizationById_WithValidId_Should_ReturnOkResultWithOrganization()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var expectedOrganization = _organizationDtoFaker.Generate();

        _mockMediator.Setup(m => m.Send(It.IsAny<GetOrganizationByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedOrganization);

        // Act
        var result = await _controller.GetOrganizationById(organizationId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedOrganization);

        // Verify mediator was called with the correct query
        _mockMediator.Verify(m => m.Send(
            It.Is<GetOrganizationByIdQuery>(q => q.Id == organizationId),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetOrganizationById_WithValidationException_Should_ReturnBadRequestResult()
    {
        // Arrange
        var organizationId = Guid.Empty; // Invalid ID
        var validationErrors = new Dictionary<string, string[]>
        {
            { "Id", new[] { "Organization ID must not be empty" } }
        };
        var validationException = new ValidationException(validationErrors);

        _mockMediator.Setup(m => m.Send(It.IsAny<GetOrganizationByIdQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(validationException);

        // Act
        var result = await _controller.GetOrganizationById(organizationId);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.StatusCode.Should().Be(400);
        badRequestResult.Value.Should().BeEquivalentTo(validationErrors);
    }

    [Fact]
    public async Task GetOrganizationById_WithNotFoundException_Should_ReturnNotFoundResult()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var errorMessage = $"Entity 'Organization' with id '{organizationId}' was not found.";
        var notFoundException = new NotFoundException(errorMessage);

        _mockMediator.Setup(m => m.Send(It.IsAny<GetOrganizationByIdQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(notFoundException);

        // Act
        var result = await _controller.GetOrganizationById(organizationId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.Value.Should().Be(errorMessage);
    }

    [Fact]
    public async Task GetOrganizationById_WithUnexpectedException_Should_ReturnInternalServerError()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var exception = new Exception("Unexpected database error");

        _mockMediator.Setup(m => m.Send(It.IsAny<GetOrganizationByIdQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.GetOrganizationById(organizationId);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = result as ObjectResult;
        statusCodeResult!.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("An unexpected error occurred.");
    }

    #endregion
}
