using Microsoft.AspNetCore.Identity;
using System.Diagnostics;
using ValidationException = FluentValidation.ValidationException;

namespace NXM.Tensai.Back.OKR.Application.UnitTests.Performance.Users;

[Trait("Category", "Performance")]
[Trait("Category", "UserPerformance")]
[Trait("Category", "UserConcurrency")]
public class UserConcurrencyTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IValidator<GetUsersByOrganizationIdQuery>> _getUsersValidatorMock;
    private readonly Mock<IValidator<CreateUserCommand>> _createUserValidatorMock;
    private readonly GetUsersByOrganizationIdQueryHandler _getUsersHandler;
    private readonly CreateUserCommandHandler _createUserHandler;
    private readonly Faker<User> _userFaker;
    private readonly Faker<CreateUserCommand> _commandFaker;
    private readonly Guid _organizationId;

    public UserConcurrencyTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _userManagerMock = MockUserManager();
        _getUsersValidatorMock = new Mock<IValidator<GetUsersByOrganizationIdQuery>>();
        _createUserValidatorMock = new Mock<IValidator<CreateUserCommand>>();
        
        _getUsersHandler = new GetUsersByOrganizationIdQueryHandler(
            _userRepositoryMock.Object, 
            _userManagerMock.Object, 
            _getUsersValidatorMock.Object);

        _createUserHandler = new CreateUserCommandHandler(
            _userManagerMock.Object,
            _userRepositoryMock.Object,
            _createUserValidatorMock.Object);        _organizationId = Guid.NewGuid();
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

        _commandFaker = new Faker<CreateUserCommand>()
            .RuleFor(x => x.Email, f => f.Internet.Email())
            .RuleFor(x => x.FirstName, f => f.Name.FirstName())
            .RuleFor(x => x.LastName, f => f.Name.LastName())
            .RuleFor(x => x.UserName, f => f.Internet.UserName())
            .RuleFor(x => x.Address, f => f.Address.FullAddress())
            .RuleFor(x => x.Position, f => f.Name.JobTitle())
            .RuleFor(x => x.DateOfBirth, f => f.Date.Past(30, DateTime.Now.AddYears(-18)))
            .RuleFor(x => x.Gender, f => f.PickRandom<Gender>())
            .RuleFor(x => x.IsEnabled, f => f.Random.Bool())
            .RuleFor(x => x.Password, f => f.Internet.Password())
            .RuleFor(x => x.ConfirmPassword, (f, u) => u.Password);
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
    public async Task GetUsersByOrganization_50ConcurrentRequests_ShouldMaintainPerformance()
    {
        // Arrange
        var users = _userFaker.Generate(30);
        var concurrentRequests = 50;

        _getUsersValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<GetUsersByOrganizationIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _userRepositoryMock.Setup(r => r.GetUsersByOrganizationIdAsync(_organizationId))
            .Returns(async () =>
            {
                await Task.Delay(50); // Simuler latence DB
                return users;
            });

        foreach (var user in users)
        {
            _userManagerMock.Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Collaborator" });
        }

        var query = new GetUsersByOrganizationIdQuery { OrganizationId = _organizationId };
        var stopwatch = Stopwatch.StartNew();

        // Act - Exécuter 50 requêtes concurrentes
        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(_ => _getUsersHandler.Handle(query, CancellationToken.None))
            .ToArray();

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(800, 
            "50 concurrent requests should complete within 800ms");
        
        results.Should().HaveCount(concurrentRequests);
        results.Should().AllSatisfy(result => result.Should().HaveCount(30));
    }

    [Fact]
    public async Task CreateUser_100ConcurrentCreations_ShouldHandleLoadEfficiently()
    {
        // Arrange
        var concurrentCreations = 100;
        var commands = _commandFaker.Generate(concurrentCreations);

        _createUserValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<CreateUserCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _userManagerMock.Setup(um => um.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User)null); // Pas d'utilisateur existant

        _userManagerMock.Setup(um => um.CreateAsync(It.IsAny<User>()))
            .Returns(async (User user) =>
            {
                await Task.Delay(10); // Simuler création en DB
                return IdentityResult.Success;
            });

        _userRepositoryMock.Setup(r => r.AddAsync(It.IsAny<User>()))
            .Returns(async (User user) =>
            {
                await Task.Delay(5); // Simuler insertion
                return user;
            });

        var stopwatch = Stopwatch.StartNew();

        // Act - Créer 100 utilisateurs en parallèle
        var tasks = commands.Select(command => _createUserHandler.Handle(command, CancellationToken.None));
        var userIds = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(3000, 
            "100 concurrent user creations should complete within 3 seconds");
          userIds.Should().HaveCount(concurrentCreations);
        userIds.Should().AllSatisfy(id => id.Should().NotBe(Guid.Empty));
    }

    [Fact]
    public async Task MixedUserOperations_ReadWriteConcurrency_ShouldMaintainConsistency()
    {
        // Arrange - Test de lecture/écriture concurrente
        var existingUsers = _userFaker.Generate(20);
        var newUserCommands = _commandFaker.Generate(10);
        var readOperations = 30;

        // Setup pour les lectures
        _getUsersValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<GetUsersByOrganizationIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _userRepositoryMock.Setup(r => r.GetUsersByOrganizationIdAsync(_organizationId))
            .ReturnsAsync(existingUsers);

        foreach (var user in existingUsers)
        {
            _userManagerMock.Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Collaborator" });
        }

        // Setup pour les écritures
        _createUserValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<CreateUserCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _userManagerMock.Setup(um => um.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User)null);

        _userManagerMock.Setup(um => um.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);

        _userRepositoryMock.Setup(r => r.AddAsync(It.IsAny<User>()))
            .ReturnsAsync((User user) => user);

        var query = new GetUsersByOrganizationIdQuery { OrganizationId = _organizationId };
        var stopwatch = Stopwatch.StartNew();

        // Act - Mélanger opérations de lecture et d'écriture
        var readTasks = Enumerable.Range(0, readOperations)
            .Select(_ => _getUsersHandler.Handle(query, CancellationToken.None));

        var writeTasks = newUserCommands
            .Select(command => _createUserHandler.Handle(command, CancellationToken.None));

        var allTasks = readTasks.Concat(writeTasks.Cast<Task>()).ToArray();
        await Task.WhenAll(allTasks);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000, 
            "Mixed read/write operations should complete efficiently");
    }

    [Fact]
    public async Task UserQuery_UnderMemoryPressure_ShouldMaintainPerformance()
    {
        // Arrange - Simuler pression mémoire avec beaucoup d'utilisateurs
        var largeUserSet = _userFaker.Generate(500);
        var batchSize = 50;
        var numberOfBatches = 10;

        _getUsersValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<GetUsersByOrganizationIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _userRepositoryMock.Setup(r => r.GetUsersByOrganizationIdAsync(_organizationId))
            .Returns(async () =>
            {
                await Task.Delay(100); // Simuler requête lourde
                return largeUserSet;
            });

        foreach (var user in largeUserSet)
        {
            _userManagerMock.Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Collaborator" });
        }

        var query = new GetUsersByOrganizationIdQuery { OrganizationId = _organizationId };
        var allExecutionTimes = new List<long>();

        // Act - Exécuter plusieurs batches pour simuler charge mémoire
        for (int batch = 0; batch < numberOfBatches; batch++)
        {
            var batchStopwatch = Stopwatch.StartNew();
            
            var batchTasks = Enumerable.Range(0, batchSize)
                .Select(_ => _getUsersHandler.Handle(query, CancellationToken.None));
            
            await Task.WhenAll(batchTasks);
            batchStopwatch.Stop();
            
            allExecutionTimes.Add(batchStopwatch.ElapsedMilliseconds);
        }

        // Assert
        var averageTime = allExecutionTimes.Average();
        var maxTime = allExecutionTimes.Max();
        
        averageTime.Should().BeLessThan(1200, "Average batch time should remain reasonable");
        maxTime.Should().BeLessThan(2000, "No batch should take excessively long");
        
        // Vérifier qu'il n'y a pas de dégradation significative
        var firstBatchTime = allExecutionTimes.First();
        var lastBatchTime = allExecutionTimes.Last();
        var degradationRatio = (double)lastBatchTime / firstBatchTime;
        
        degradationRatio.Should().BeLessThan(2.0, 
            "Performance should not degrade significantly under memory pressure");
    }
}
