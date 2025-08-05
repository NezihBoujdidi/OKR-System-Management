namespace NXM.Tensai.Back.OKR.Application;

public class GetUsersByTeamIdQuery : IRequest<IEnumerable<UserWithRoleDto>>
{
    public Guid TeamId { get; init; }
}

public class GetUsersByTeamIdQueryValidator : AbstractValidator<GetUsersByTeamIdQuery>
{
    public GetUsersByTeamIdQueryValidator()
    {
        RuleFor(x => x.TeamId)
            .NotEqual(Guid.Empty).WithMessage("Team ID must not be empty.");
    }
}

public class GetUsersByTeamIdQueryHandler : IRequestHandler<GetUsersByTeamIdQuery, IEnumerable<UserWithRoleDto>>
{
    private readonly ITeamUserRepository _teamUserRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly UserManager<User> _userManager;
    private readonly IValidator<GetUsersByTeamIdQuery> _validator;

    public GetUsersByTeamIdQueryHandler(
        ITeamUserRepository teamUserRepository,
        ITeamRepository teamRepository,
        UserManager<User> userManager,
        IValidator<GetUsersByTeamIdQuery> validator)
    {
        _teamUserRepository = teamUserRepository ?? throw new ArgumentNullException(nameof(teamUserRepository));
        _teamRepository = teamRepository ?? throw new ArgumentNullException(nameof(teamRepository));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<IEnumerable<UserWithRoleDto>> Handle(GetUsersByTeamIdQuery request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

       // Check if team exists and is not deleted using team repository
        var team = await _teamRepository.GetByIdAsync(request.TeamId);
        if (team == null || team.IsDeleted)
        {
            throw new EntityNotFoundException($"No team found for Team ID {request.TeamId}.");
        }

        var users = await _teamUserRepository.GetUsersByTeamIdAsync(request.TeamId);

        if (!users.Any())
        {
            throw new EntityNotFoundException($"No users found for Team ID {request.TeamId}.");
        }

        var userWithRoleDtos = new List<UserWithRoleDto>();
        
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            string role = roles.FirstOrDefault() ?? string.Empty;
            userWithRoleDtos.Add(user.ToUserWithRoleDto(role));
        }

        return userWithRoleDtos;
    }
}