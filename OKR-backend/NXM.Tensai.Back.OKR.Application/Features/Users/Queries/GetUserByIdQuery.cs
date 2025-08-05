namespace NXM.Tensai.Back.OKR.Application;

public class GetUserByIdQuery : IRequest<UserWithRoleDto>
{
    public Guid Id { get; init; }
}

public class GetUserByIdQueryValidator : AbstractValidator<GetUserByIdQuery>
{
    public GetUserByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty).WithMessage("Id must not be empty.");
    }
}

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserWithRoleDto>
{
    private readonly IUserRepository _userRepository;
    private readonly UserManager<User> _userManager;
    private readonly IValidator<GetUserByIdQuery> _validator;

    public GetUserByIdQueryHandler(IUserRepository userRepository, UserManager<User> userManager, IValidator<GetUserByIdQuery> validator)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _userManager = userManager;
    }

    public async Task<UserWithRoleDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var user = await _userRepository.GetByIdAsync(request.Id);
        if (user == null)
        {
            throw new NotFoundException(nameof(User), request.Id);
        }
        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? string.Empty;

        return user.ToUserWithRoleDto(role);
    }
}
