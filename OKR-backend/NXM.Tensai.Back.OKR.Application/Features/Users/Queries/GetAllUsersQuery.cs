namespace NXM.Tensai.Back.OKR.Application;

public class GetAllUsersQuery : IRequest<List<UserWithRoleDto>>
{
    // No properties needed
}

public class GetAllUsersQueryValidator : AbstractValidator<GetAllUsersQuery>
{
    public GetAllUsersQueryValidator()
    {
        // No rules needed
    }
}

public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, List<UserWithRoleDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IValidator<GetAllUsersQuery> _validator;
    private readonly UserManager<User> _userManager;

    public GetAllUsersQueryHandler(
        IUserRepository userRepository, 
        IValidator<GetAllUsersQuery> validator,
        UserManager<User> userManager)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    }

    public async Task<List<UserWithRoleDto>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var allUsers = await _userRepository.GetAllAsync();

        var userDtos = new List<UserWithRoleDto>();

        foreach (var user in allUsers)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? string.Empty;
            userDtos.Add(user.ToUserWithRoleDto(role));
        }

        return userDtos;
    }
}


