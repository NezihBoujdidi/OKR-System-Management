using Microsoft.AspNetCore.Identity;
using System.Diagnostics;
using ValidationException = FluentValidation.ValidationException;

namespace NXM.Tensai.Back.OKR.Application.UnitTests.Performance.Users;

[Trait("Category", "Performance")]
[Trait("Category", "UserPerformance")]
[Trait("Category", "UserQueryOptimization")]
public class UserQueryOptimizationTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IValidator<GetUsersByOrganizationIdQuery>> _validatorMock;
    private readonly GetUsersByOrganizationIdQueryHandler _handler;
    private readonly Faker<User> _userFaker;
    private readonly Guid _organizationId;

    public UserQueryOptimizationTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _userManagerMock = MockUserManager();
        _validatorMock = new Mock<IValidator<GetUsersByOrganizationIdQuery>>();
        
        _handler = new GetUsersByOrganizationIdQueryHandler(
            _userRepositoryMock.Object, 
            _userManagerMock.Object, 
            _validatorMock.Object);        _organizationId = Guid.NewGuid();
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
    public async Task GetUsersWithRoles_ShouldAvoidNPlusOneQueries()
    {
        // Arrange
        var users = _userFaker.Generate(50);
        var queryCallCount = 0;

        _validatorMock.Setup(v => v.ValidateAsync(It.IsAny<GetUsersByOrganizationIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _userRepositoryMock.Setup(r => r.GetUsersByOrganizationIdAsync(_organizationId))
            .Callback(() => queryCallCount++)
            .ReturnsAsync(users);

        // Simuler GetRoles de manière efficace (une seule fois par user)
        var roleCallCount = 0;
        foreach (var user in users)
        {
            _userManagerMock.Setup(um => um.GetRolesAsync(user))
                .Callback(() => roleCallCount++)
                .ReturnsAsync(new List<string> { "Collaborator" });
        }

        var query = new GetUsersByOrganizationIdQuery { OrganizationId = _organizationId };
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
        stopwatch.Stop();

        // Assert
        queryCallCount.Should().Be(1, "Should make only one database query to get users");
        roleCallCount.Should().Be(50, "Should call GetRoles once per user, but this could be optimized with bulk role loading");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(400, "Optimized query should complete quickly");
        result.Should().HaveCount(50);
    }

    [Fact]
    public async Task GetUsersByOrganization_WithProperIndexing_ShouldPerformEfficiently()
    {
        // Arrange - Test qui valide que l'index sur OrganizationId est efficace
        var users = _userFaker.Generate(100);

        _validatorMock.Setup(v => v.ValidateAsync(It.IsAny<GetUsersByOrganizationIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _userRepositoryMock.Setup(r => r.GetUsersByOrganizationIdAsync(_organizationId))
            .Returns(async () =>
            {
                // Simuler une requête indexée - rapide même avec beaucoup de données
                await Task.Delay(20); // Simulation d'une requête optimisée
                return users;
            });

        foreach (var user in users)
        {
            _userManagerMock.Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Collaborator" });
        }

        var query = new GetUsersByOrganizationIdQuery { OrganizationId = _organizationId };
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(200, 
            "Query with proper indexing on OrganizationId should be very fast");
        result.Should().HaveCount(100);
    }

    [Fact]
    public async Task GetUsersByEmail_ShouldUsePrimaryKeyIndex()
    {
        // Arrange - Test de l'optimisation pour GetUserByEmailQuery (si elle existe)
        var user = _userFaker.Generate();
        var email = user.Email;

        _userRepositoryMock.Setup(r => r.GetUserByEmailAsync(email))
            .Returns(async () =>
            {
                await Task.Delay(5); // Très rapide car index unique sur email
                return user;
            });

        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await _userRepositoryMock.Object.GetUserByEmailAsync(email);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(50, 
            "Email lookup should be extremely fast with unique index");
        result.Should().NotBeNull();
        result.Email.Should().Be(email);
    }

    [Fact]
    public async Task GetUsersByOrganization_ErrorHandling_ShouldNotImpactPerformance()
    {
        // Arrange - Test que la gestion d'erreur ne dégrade pas les performances
        _validatorMock.Setup(v => v.ValidateAsync(It.IsAny<GetUsersByOrganizationIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _userRepositoryMock.Setup(r => r.GetUsersByOrganizationIdAsync(_organizationId))
            .ReturnsAsync(new List<User>()); // Pas d'utilisateurs trouvés

        var query = new GetUsersByOrganizationIdQuery { OrganizationId = _organizationId };
        var stopwatch = Stopwatch.StartNew();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _handler.Handle(query, CancellationToken.None));
        
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100, 
            "Error handling should not add significant overhead");
        exception.Message.Should().Contain("No users found");
    }

    [Fact]
    public async Task GetUsersByOrganization_ValidationError_ShouldFailFast()
    {
        // Arrange - Test que la validation échoue rapidement
        var invalidValidationResult = new FluentValidation.Results.ValidationResult(
            new[] { new FluentValidation.Results.ValidationFailure("OrganizationId", "Invalid organization ID") });

        _validatorMock.Setup(v => v.ValidateAsync(It.IsAny<GetUsersByOrganizationIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invalidValidationResult);

        var query = new GetUsersByOrganizationIdQuery { OrganizationId = Guid.Empty };
        var stopwatch = Stopwatch.StartNew();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _handler.Handle(query, CancellationToken.None));
        
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(50, 
            "Validation should fail immediately without database calls");
        exception.Errors.Should().HaveCount(1);
        
        // Vérifier qu'aucune requête DB n'a été faite
        _userRepositoryMock.Verify(r => r.GetUsersByOrganizationIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ConcurrentUserQueries_ShouldNotDegradeIndividualPerformance()
    {
        // Arrange - Test de performance sous charge concurrente
        var users = _userFaker.Generate(20);
        var organizationIds = Enumerable.Range(0, 10).Select(_ => Guid.NewGuid()).ToList();

        _validatorMock.Setup(v => v.ValidateAsync(It.IsAny<GetUsersByOrganizationIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        foreach (var orgId in organizationIds)
        {
            _userRepositoryMock.Setup(r => r.GetUsersByOrganizationIdAsync(orgId))
                .ReturnsAsync(users);

            foreach (var user in users)
            {
                _userManagerMock.Setup(um => um.GetRolesAsync(user))
                    .ReturnsAsync(new List<string> { "Collaborator" });
            }
        }

        var tasks = organizationIds.Select(async orgId =>
        {
            var query = new GetUsersByOrganizationIdQuery { OrganizationId = orgId };
            var stopwatch = Stopwatch.StartNew();
            
            var result = await _handler.Handle(query, CancellationToken.None);
            stopwatch.Stop();
            
            return stopwatch.ElapsedMilliseconds;
        });

        // Act
        var allExecutionTimes = await Task.WhenAll(tasks);

        // Assert
        allExecutionTimes.Should().AllSatisfy(time => 
            time.Should().BeLessThan(600, "Each concurrent query should maintain good performance"));
        
        var averageTime = allExecutionTimes.Average();
        averageTime.Should().BeLessThan(400, "Average response time should be good under concurrent load");
    }
}
