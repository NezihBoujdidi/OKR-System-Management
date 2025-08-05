namespace NXM.Tensai.Back.OKR.Application;

public class GetUsersByOrganizationIdQuery : IRequest<IEnumerable<UserWithRoleDto>>
{
    public Guid OrganizationId { get; init; }
}

public class GetUsersByOrganizationIdQueryValidator : AbstractValidator<GetUsersByOrganizationIdQuery>
{
    public GetUsersByOrganizationIdQueryValidator()
    {
        RuleFor(x => x.OrganizationId)
            .NotEqual(Guid.Empty).WithMessage("Organization ID must not be empty.");
    }
}

public class GetUsersByOrganizationIdQueryHandler : IRequestHandler<GetUsersByOrganizationIdQuery, IEnumerable<UserWithRoleDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly UserManager<User> _userManager;
    private readonly IValidator<GetUsersByOrganizationIdQuery> _validator;

    public GetUsersByOrganizationIdQueryHandler(
        IUserRepository userRepository, 
        UserManager<User> userManager,
        IValidator<GetUsersByOrganizationIdQuery> validator)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<IEnumerable<UserWithRoleDto>> Handle(GetUsersByOrganizationIdQuery request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var users = await _userRepository.GetUsersByOrganizationIdAsync(request.OrganizationId);
        
        if (!users.Any())
        {
            throw new EntityNotFoundException($"No users found for Organization ID {request.OrganizationId}.");
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
