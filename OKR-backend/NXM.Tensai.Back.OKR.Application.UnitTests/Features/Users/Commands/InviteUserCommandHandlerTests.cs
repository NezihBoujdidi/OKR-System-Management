using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ValidationException = FluentValidation.ValidationException;

namespace NXM.Tensai.Back.OKR.Application.UnitTests.Features.Users.Commands;

public class InviteUserCommandHandlerTests
{
    private readonly Mock<ISupabaseClient> _supabaseClientMock;
    private readonly Mock<IOrganizationRepository> _organizationRepositoryMock;
    private readonly Mock<ITeamRepository> _teamRepositoryMock;
    private readonly Mock<IValidator<InviteUserCommand>> _validatorMock;
    private readonly Mock<ILogger<InviteUserCommandHandler>> _loggerMock;
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<RoleManager<Role>> _roleManagerMock;
    private readonly Mock<ITeamUserRepository> _teamUserRepositoryMock;
    private readonly InviteUserCommandHandler _handler;
    private readonly Faker<InviteUserCommand> _commandFaker;
    private readonly Faker<Organization> _organizationFaker;
    private readonly Faker<Team> _teamFaker;
    private readonly Faker<User> _userFaker;
    private readonly Faker<Role> _roleFaker;

    public InviteUserCommandHandlerTests()
    {
        _supabaseClientMock = new Mock<ISupabaseClient>();
        _organizationRepositoryMock = new Mock<IOrganizationRepository>();
        _teamRepositoryMock = new Mock<ITeamRepository>();
        _validatorMock = new Mock<IValidator<InviteUserCommand>>();
        _loggerMock = new Mock<ILogger<InviteUserCommandHandler>>();
        _userManagerMock = MockUserManager();
        _roleManagerMock = MockRoleManager();
        _teamUserRepositoryMock = new Mock<ITeamUserRepository>();

        _handler = new InviteUserCommandHandler(
            _supabaseClientMock.Object,
            _organizationRepositoryMock.Object,
            _teamRepositoryMock.Object,
            _validatorMock.Object,
            _loggerMock.Object,
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _teamUserRepositoryMock.Object);

        _commandFaker = new Faker<InviteUserCommand>()
            .RuleFor(x => x.Email, f => f.Internet.Email())
            .RuleFor(x => x.Role, f => f.PickRandom<RoleType>())
            .RuleFor(x => x.OrganizationId, f => f.Random.Guid())
            .RuleFor(x => x.TeamId, f => f.Random.Guid());

        _organizationFaker = new Faker<Organization>()
            .RuleFor(x => x.Id, f => f.Random.Guid())
            .RuleFor(x => x.Name, f => f.Company.CompanyName())
            .RuleFor(x => x.Description, f => f.Lorem.Sentence())
            .RuleFor(x => x.IsDeleted, f => false);

        _teamFaker = new Faker<Team>()
            .RuleFor(x => x.Id, f => f.Random.Guid())
            .RuleFor(x => x.Name, f => f.Commerce.Department())
            .RuleFor(x => x.Description, f => f.Lorem.Sentence())
            .RuleFor(x => x.OrganizationId, f => f.Random.Guid())
            .RuleFor(x => x.IsDeleted, f => false);

        _userFaker = new Faker<User>()
            .RuleFor(x => x.Id, f => f.Random.Guid())
            .RuleFor(x => x.Email, f => f.Internet.Email())
            .RuleFor(x => x.UserName, f => f.Internet.UserName())
            .RuleFor(x => x.FirstName, f => f.Name.FirstName())
            .RuleFor(x => x.LastName, f => f.Name.LastName());

        _roleFaker = new Faker<Role>()
            .RuleFor(x => x.Id, f => f.Random.Guid())
            .RuleFor(x => x.Name, f => f.PickRandom<RoleType>().ToString())
            .RuleFor(x => x.NormalizedName, (f, r) => r.Name!.ToUpperInvariant());
    }

    private static Mock<UserManager<User>> MockUserManager()
    {
        var store = new Mock<IUserStore<User>>();
        return new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
    }

    private static Mock<RoleManager<Role>> MockRoleManager()
    {
        var store = new Mock<IRoleStore<Role>>();
        return new Mock<RoleManager<Role>>(store.Object, null, null, null, null);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldInviteUserSuccessfully()
    {
        // Arrange
        var command = _commandFaker.Generate();
        var organization = _organizationFaker.Generate();
        organization.Id = command.OrganizationId;
        var team = _teamFaker.Generate();
        team.Id = command.TeamId!.Value;
        team.OrganizationId = command.OrganizationId;
        var role = _roleFaker.Generate();        role.Name = command.Role.ToString();
        
        var validationResult = new FluentValidation.Results.ValidationResult();
        
        var supabaseResult = SupabaseInviteResult.Successful(Guid.NewGuid().ToString());

        SetupMocks(command, organization, team, role, validationResult, supabaseResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Email.Should().Be(command.Email);
        result.Role.Should().Be(command.Role.ToString());
        result.Message.Should().Be("Invitation sent successfully");
        result.InviteId.Should().NotBeNullOrEmpty();

        VerifyMocks(command, organization, team, role);
    }

    [Fact]
    public async Task Handle_InvalidCommand_ShouldThrowValidationException()
    {
        // Arrange
        var command = _commandFaker.Generate();
        var validationFailures = new List<FluentValidation.Results.ValidationFailure>
        {
            new("Email", "Email is required."),
            new("OrganizationId", "Organization ID is required.")
        };
        var validationResult = new FluentValidation.Results.ValidationResult(validationFailures);
        
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act & Assert
        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .Where(ex => ex.Errors.Count() == 2);
        
        _validatorMock.Verify(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);
        _organizationRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Handle_OrganizationNotFound_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var command = _commandFaker.Generate();
        var validationResult = new FluentValidation.Results.ValidationResult();
        
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        
        _organizationRepositoryMock.Setup(x => x.GetByIdAsync(command.OrganizationId))
            .ReturnsAsync((Organization?)null);

        // Act & Assert
        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<EntityNotFoundException>()
            .WithMessage($"Organization with ID {command.OrganizationId} not found.");
        
        _organizationRepositoryMock.Verify(x => x.GetByIdAsync(command.OrganizationId), Times.Once);
    }

    [Fact]
    public async Task Handle_TeamNotFound_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var command = _commandFaker.Generate();
        var organization = _organizationFaker.Generate();
        organization.Id = command.OrganizationId;
        var validationResult = new FluentValidation.Results.ValidationResult();
        
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        
        _organizationRepositoryMock.Setup(x => x.GetByIdAsync(command.OrganizationId))
            .ReturnsAsync(organization);
        
        _teamRepositoryMock.Setup(x => x.GetByIdAsync(command.TeamId!.Value))
            .ReturnsAsync((Team?)null);

        // Act & Assert
        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<EntityNotFoundException>()
            .WithMessage($"Team with ID {command.TeamId} not found.");
        
        _teamRepositoryMock.Verify(x => x.GetByIdAsync(command.TeamId!.Value), Times.Once);
    }

    [Fact]
    public async Task Handle_TeamDoesNotBelongToOrganization_ShouldThrowValidationException()
    {
        // Arrange
        var command = _commandFaker.Generate();
        var organization = _organizationFaker.Generate();
        organization.Id = command.OrganizationId;
        var team = _teamFaker.Generate();
        team.Id = command.TeamId!.Value;
        team.OrganizationId = Guid.NewGuid(); // Different organization
        
        var validationResult = new FluentValidation.Results.ValidationResult();
        
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        
        _organizationRepositoryMock.Setup(x => x.GetByIdAsync(command.OrganizationId))
            .ReturnsAsync(organization);
        
        _teamRepositoryMock.Setup(x => x.GetByIdAsync(command.TeamId!.Value))
            .ReturnsAsync(team);

        // Act & Assert
        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage($"Team with ID {command.TeamId} does not belong to organization with ID {command.OrganizationId}.");
    }    [Fact]
    public async Task Handle_UserAlreadyExists_ShouldThrowValidationException()
    {
        // Arrange
        var command = _commandFaker.Generate();
        var organization = _organizationFaker.Generate();
        organization.Id = command.OrganizationId;
        var team = _teamFaker.Generate();
        team.Id = command.TeamId!.Value;
        team.OrganizationId = command.OrganizationId;
        var existingUser = _userFaker.Generate();
        existingUser.Email = command.Email;
        
        var validationResult = new FluentValidation.Results.ValidationResult();
        
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        
        _organizationRepositoryMock.Setup(x => x.GetByIdAsync(command.OrganizationId))
            .ReturnsAsync(organization);
            
        _teamRepositoryMock.Setup(x => x.GetByIdAsync(command.TeamId!.Value))
            .ReturnsAsync(team);
        
        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(existingUser);

        // Act & Assert
        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .Where(ex => ex.Errors.Any(e => e.ErrorMessage == "User already invited"));
    }[Fact]
    public async Task Handle_RoleNotFound_ShouldThrowValidationException()
    {
        // Arrange
        var command = _commandFaker.Generate();
        var organization = _organizationFaker.Generate();
        organization.Id = command.OrganizationId;
        var team = _teamFaker.Generate();
        team.Id = command.TeamId!.Value;
        team.OrganizationId = command.OrganizationId;
        
        var validationResult = new FluentValidation.Results.ValidationResult();
        
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        
        _organizationRepositoryMock.Setup(x => x.GetByIdAsync(command.OrganizationId))
            .ReturnsAsync(organization);
            
        _teamRepositoryMock.Setup(x => x.GetByIdAsync(command.TeamId!.Value))
            .ReturnsAsync(team);
        
        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync((User?)null);
        
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        
        _roleManagerMock.Setup(x => x.FindByNameAsync(command.Role.ToString()))
            .ReturnsAsync((Role?)null);

        // Act & Assert
        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage($"Role {command.Role} does not exist.");
    }[Fact]
    public async Task Handle_WithoutTeamId_ShouldNotCreateTeamUser()
    {
        // Arrange
        var baseCommand = _commandFaker.Generate();
        var command = new InviteUserCommand
        {
            Email = baseCommand.Email,
            Role = baseCommand.Role,
            OrganizationId = baseCommand.OrganizationId,
            TeamId = null
        };
        var organization = _organizationFaker.Generate();
        organization.Id = command.OrganizationId;
        var role = _roleFaker.Generate();
        role.Name = command.Role.ToString();
        
        var validationResult = new FluentValidation.Results.ValidationResult();
          var supabaseResult = SupabaseInviteResult.Successful(Guid.NewGuid().ToString());

        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        
        _organizationRepositoryMock.Setup(x => x.GetByIdAsync(command.OrganizationId))
            .ReturnsAsync(organization);
        
        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync((User?)null);
        
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        
        _roleManagerMock.Setup(x => x.FindByNameAsync(command.Role.ToString()))
            .ReturnsAsync(role);
        
        _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), command.Role.ToString()))
            .ReturnsAsync(IdentityResult.Success);
        
        _supabaseClientMock.Setup(x => x.InviteUserByEmailAsync(command.Email, command.Role.ToString(), command.OrganizationId, command.TeamId))
            .ReturnsAsync(supabaseResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        
        _teamUserRepositoryMock.Verify(x => x.AddAsync(It.IsAny<TeamUser>()), Times.Never);
        _teamUserRepositoryMock.Verify(x => x.GetByTeamAndUserIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }    private void SetupMocks(InviteUserCommand command, Organization organization, Team team, Role role, 
        FluentValidation.Results.ValidationResult validationResult, SupabaseInviteResult supabaseResult)
    {
        _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        
        _organizationRepositoryMock.Setup(x => x.GetByIdAsync(command.OrganizationId))
            .ReturnsAsync(organization);
        
        if (command.TeamId.HasValue)
        {
            _teamRepositoryMock.Setup(x => x.GetByIdAsync(command.TeamId.Value))
                .ReturnsAsync(team);
            
            _teamUserRepositoryMock.Setup(x => x.GetByTeamAndUserIdAsync(command.TeamId.Value, It.IsAny<Guid>()))
                .ReturnsAsync((TeamUser?)null);
              _teamUserRepositoryMock.Setup(x => x.AddAsync(It.IsAny<TeamUser>()))
                .ReturnsAsync((TeamUser teamUser) => teamUser);
        }
        
        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync((User?)null);
        
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        
        _roleManagerMock.Setup(x => x.FindByNameAsync(command.Role.ToString()))
            .ReturnsAsync(role);
        
        _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), command.Role.ToString()))
            .ReturnsAsync(IdentityResult.Success);
        
        _supabaseClientMock.Setup(x => x.InviteUserByEmailAsync(command.Email, command.Role.ToString(), command.OrganizationId, command.TeamId))
            .ReturnsAsync(supabaseResult);
    }

    private void VerifyMocks(InviteUserCommand command, Organization organization, Team team, Role role)
    {
        _validatorMock.Verify(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);
        _organizationRepositoryMock.Verify(x => x.GetByIdAsync(command.OrganizationId), Times.Once);
        
        if (command.TeamId.HasValue)
        {
            _teamRepositoryMock.Verify(x => x.GetByIdAsync(command.TeamId.Value), Times.Once);
            _teamUserRepositoryMock.Verify(x => x.AddAsync(It.IsAny<TeamUser>()), Times.Once);
        }
        
        _userManagerMock.Verify(x => x.FindByEmailAsync(command.Email), Times.Once);
        _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Once);
        _roleManagerMock.Verify(x => x.FindByNameAsync(command.Role.ToString()), Times.Once);
        _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<User>(), command.Role.ToString()), Times.Once);
        _supabaseClientMock.Verify(x => x.InviteUserByEmailAsync(command.Email, command.Role.ToString(), command.OrganizationId, command.TeamId), Times.Once);
    }

    [Theory]
    [InlineData("supabaseClient")]
    [InlineData("organizationRepository")]
    [InlineData("teamRepository")]
    [InlineData("validator")]
    [InlineData("logger")]
    [InlineData("userManager")]
    [InlineData("roleManager")]
    [InlineData("teamUserRepository")]
    public void Constructor_WithNullDependencies_ShouldThrowArgumentNullException(string nullParam)
    {
        // Act & Assert
        switch (nullParam)
        {
            case "supabaseClient":
                var act1 = () => new InviteUserCommandHandler(null!, _organizationRepositoryMock.Object, _teamRepositoryMock.Object, _validatorMock.Object, _loggerMock.Object, _userManagerMock.Object, _roleManagerMock.Object, _teamUserRepositoryMock.Object);
                act1.Should().Throw<ArgumentNullException>().WithParameterName("supabaseClient");
                break;
            case "organizationRepository":
                var act2 = () => new InviteUserCommandHandler(_supabaseClientMock.Object, null!, _teamRepositoryMock.Object, _validatorMock.Object, _loggerMock.Object, _userManagerMock.Object, _roleManagerMock.Object, _teamUserRepositoryMock.Object);
                act2.Should().Throw<ArgumentNullException>().WithParameterName("organizationRepository");
                break;
            case "teamRepository":
                var act3 = () => new InviteUserCommandHandler(_supabaseClientMock.Object, _organizationRepositoryMock.Object, null!, _validatorMock.Object, _loggerMock.Object, _userManagerMock.Object, _roleManagerMock.Object, _teamUserRepositoryMock.Object);
                act3.Should().Throw<ArgumentNullException>().WithParameterName("teamRepository");
                break;
            case "validator":
                var act4 = () => new InviteUserCommandHandler(_supabaseClientMock.Object, _organizationRepositoryMock.Object, _teamRepositoryMock.Object, null!, _loggerMock.Object, _userManagerMock.Object, _roleManagerMock.Object, _teamUserRepositoryMock.Object);
                act4.Should().Throw<ArgumentNullException>().WithParameterName("validator");
                break;
            case "logger":
                var act5 = () => new InviteUserCommandHandler(_supabaseClientMock.Object, _organizationRepositoryMock.Object, _teamRepositoryMock.Object, _validatorMock.Object, null!, _userManagerMock.Object, _roleManagerMock.Object, _teamUserRepositoryMock.Object);
                act5.Should().Throw<ArgumentNullException>().WithParameterName("logger");
                break;
            case "userManager":
                var act6 = () => new InviteUserCommandHandler(_supabaseClientMock.Object, _organizationRepositoryMock.Object, _teamRepositoryMock.Object, _validatorMock.Object, _loggerMock.Object, null!, _roleManagerMock.Object, _teamUserRepositoryMock.Object);
                act6.Should().Throw<ArgumentNullException>().WithParameterName("userManager");
                break;
            case "roleManager":
                var act7 = () => new InviteUserCommandHandler(_supabaseClientMock.Object, _organizationRepositoryMock.Object, _teamRepositoryMock.Object, _validatorMock.Object, _loggerMock.Object, _userManagerMock.Object, null!, _teamUserRepositoryMock.Object);
                act7.Should().Throw<ArgumentNullException>().WithParameterName("roleManager");
                break;
            case "teamUserRepository":
                var act8 = () => new InviteUserCommandHandler(_supabaseClientMock.Object, _organizationRepositoryMock.Object, _teamRepositoryMock.Object, _validatorMock.Object, _loggerMock.Object, _userManagerMock.Object, _roleManagerMock.Object, null!);
                act8.Should().Throw<ArgumentNullException>().WithParameterName("teamUserRepository");
                break;
        }
    }
}
