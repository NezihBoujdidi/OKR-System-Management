using Microsoft.AspNetCore.Identity;
using System.Diagnostics;
using ValidationException = FluentValidation.ValidationException;

namespace NXM.Tensai.Back.OKR.Application.UnitTests.Performance.Users;

[Trait("Category", "Performance")]
[Trait("Category", "UserPerformance")]
[Trait("Category", "UserPagination")]
public class UserPaginationPerformanceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IValidator<GetUsersByOrganizationIdQuery>> _validatorMock;
    private readonly Mock<IValidator<SearchUserByNameQuery>> _searchValidatorMock;
    private readonly GetUsersByOrganizationIdQueryHandler _getUsersHandler;
    private readonly SearchUserByNameQueryHandler _searchHandler;
    private readonly Faker<User> _userFaker;
    private readonly Guid _organizationId;

    public UserPaginationPerformanceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _userManagerMock = MockUserManager();
        _validatorMock = new Mock<IValidator<GetUsersByOrganizationIdQuery>>();
        _searchValidatorMock = new Mock<IValidator<SearchUserByNameQuery>>();
        
        _getUsersHandler = new GetUsersByOrganizationIdQueryHandler(
            _userRepositoryMock.Object, 
            _userManagerMock.Object, 
            _validatorMock.Object);
            
        _searchHandler = new SearchUserByNameQueryHandler(
            _userRepositoryMock.Object, 
            _searchValidatorMock.Object);        _organizationId = Guid.NewGuid();
        _userFaker = new Faker<User>()
            .RuleFor(u => u.Id, f => Guid.NewGuid())
            .RuleFor(u => u.Email, f => f.Internet.Email())
            .RuleFor(u => u.FirstName, f => f.Name.FirstName())
            .RuleFor(u => u.LastName, f => f.Name.LastName())
            .RuleFor(u => u.UserName, f => f.Internet.UserName())
            .RuleFor(u => u.OrganizationId, _organizationId)
            .RuleFor(u => u.IsEnabled, true)
            .RuleFor(u => u.SupabaseId, f => f.Random.Guid().ToString())
            .RuleFor(u => u.Address, f => f.Address.FullAddress())
            .RuleFor(u => u.Position, f => f.Name.JobTitle())
            .RuleFor(u => u.DateOfBirth, f => f.Date.Past(30, DateTime.Now.AddYears(-18)))
            .RuleFor(u => u.Gender, f => f.PickRandom<Gender>())
            .RuleFor(u => u.ProfilePictureUrl, f => f.Internet.Avatar())
            .RuleFor(u => u.IsNotificationEnabled, f => f.Random.Bool());
    }

    private static Mock<UserManager<User>> MockUserManager()
    {
        var store = new Mock<IUserStore<User>>();
        var mgr = new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
        mgr.Object.UserValidators.Add(new UserValidator<User>());
        mgr.Object.PasswordValidators.Add(new PasswordValidator<User>());
        return mgr;
    }

    [Fact]
    public async Task GetUsersByOrganization_With50Users_ShouldCompleteWithin500ms()
    {
        // Arrange
        var users = _userFaker.Generate(50);
        var expectedRoles = new List<string> { "Collaborator" };

        _validatorMock.Setup(v => v.ValidateAsync(It.IsAny<GetUsersByOrganizationIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _userRepositoryMock.Setup(r => r.GetUsersByOrganizationIdAsync(_organizationId))
            .ReturnsAsync(users);

        foreach (var user in users)
        {
            _userManagerMock.Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(expectedRoles);
        }

        var query = new GetUsersByOrganizationIdQuery { OrganizationId = _organizationId };
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await _getUsersHandler.Handle(query, CancellationToken.None);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500, 
            "GetUsersByOrganization with 50 users should complete within 500ms for good UX");
        result.Should().HaveCount(50);
        result.Should().AllSatisfy(u => u.Role.Should().Be("Collaborator"));
    }

    [Fact]
    public async Task GetUsersByOrganization_WithLargeDataset_ShouldMaintainPerformance()
    {
        // Arrange - Simuler une large base de données
        var users = _userFaker.Generate(20); // Retourner seulement 20 mais similer qu'il y en a 10,000
        var expectedRoles = new List<string> { "Collaborator" };

        _validatorMock.Setup(v => v.ValidateAsync(It.IsAny<GetUsersByOrganizationIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Simuler un délai de requête DB (comme si on avait 10k users)
        _userRepositoryMock.Setup(r => r.GetUsersByOrganizationIdAsync(_organizationId))
            .Returns(async () => 
            {
                await Task.Delay(50); // Simuler latence DB
                return users;
            });

        foreach (var user in users)
        {
            _userManagerMock.Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(expectedRoles);
        }

        var query = new GetUsersByOrganizationIdQuery { OrganizationId = _organizationId };
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await _getUsersHandler.Handle(query, CancellationToken.None);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(300, 
            "Query should maintain performance even with large dataset through proper indexing");
        result.Should().HaveCount(20);
    }

    [Fact]
    public async Task SearchUserByName_WithFiltering_ShouldPerformWithin400ms()
    {        // Arrange
        var matchingUsers = _userFaker.Generate(15)
            .Select(u => { u.FirstName = $"John{u.FirstName}"; return u; })
            .ToList();
        
        var paginatedResult = new PaginatedListResult<UserDto>(
            matchingUsers.Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                SupabaseId = u.SupabaseId,
                Address = u.Address,
                Position = u.Position,
                DateOfBirth = u.DateOfBirth,
                Gender = u.Gender,
                OrganizationId = u.OrganizationId,
                ProfilePictureUrl = u.ProfilePictureUrl,
                IsNotificationEnabled = u.IsNotificationEnabled,                IsEnabled = u.IsEnabled
            }).ToList(),
            15, // total count
            1,  // page index
            1   // total pages
        );
        
        _searchValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<SearchUserByNameQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _userRepositoryMock.Setup(r => r.GetPagedAsync(
            1, 20, It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), 
            It.IsAny<System.Linq.Expressions.Expression<Func<User, object>>[]>()))
            .ReturnsAsync(new PaginatedList<User>(matchingUsers, 15, 1, 20));

        var query = new SearchUserByNameQuery
        { 
            Query = "John", 
            OrganizationId = _organizationId,
            Page = 1,
            PageSize = 20
        };
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await _searchHandler.Handle(query, CancellationToken.None);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(400, 
            "User search with filtering should complete within 400ms");
        result.Items.Should().HaveCount(15);
        result.Items.Should().AllSatisfy(u => u.FirstName.Should().Contain("John"));
    }    [Fact]
    public async Task SearchUserByName_EmptySearchTerm_ShouldReturnAllUsersWithinPerformanceLimit()
    {
        // Arrange
        var allUsers = _userFaker.Generate(30);
        
        var paginatedResult = new PaginatedListResult<UserDto>(
            allUsers.Take(20).Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                SupabaseId = u.SupabaseId,
                Address = u.Address,
                Position = u.Position,
                DateOfBirth = u.DateOfBirth,
                Gender = u.Gender,
                OrganizationId = u.OrganizationId,
                ProfilePictureUrl = u.ProfilePictureUrl,
                IsNotificationEnabled = u.IsNotificationEnabled,                IsEnabled = u.IsEnabled
            }).ToList(),
            30, // total count
            1,  // page index
            2   // total pages
        );
        
        _searchValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<SearchUserByNameQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _userRepositoryMock.Setup(r => r.GetPagedAsync(
            1, 20, It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), 
            It.IsAny<System.Linq.Expressions.Expression<Func<User, object>>[]>()))
            .ReturnsAsync(new PaginatedList<User>(allUsers, 30, 1, 20));

        var query = new SearchUserByNameQuery
        { 
            Query = null, 
            OrganizationId = _organizationId,
            Page = 1,
            PageSize = 20
        };
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await _searchHandler.Handle(query, CancellationToken.None);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(600, 
            "Search without filter should still perform well with proper pagination");
        result.Items.Should().HaveCount(20);
        result.TotalPages.Should().Be(2);
    }
}
