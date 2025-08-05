namespace NXM.Tensai.Back.OKR.Application;

public class GetUserByEmailQuery : IRequest<UserWithRoleDto>
{
    public string Email { get; set; } = null!;
}

public class GetUserByEmailQueryValidator : AbstractValidator<GetUserByEmailQuery>
{
    public GetUserByEmailQueryValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");
    }
}

public class GetUserByEmailQueryHandler : IRequestHandler<GetUserByEmailQuery, UserWithRoleDto>
{
    private readonly UserManager<User> _userManager;
    private readonly IValidator<GetUserByEmailQuery> _validator;

    public GetUserByEmailQueryHandler(
        UserManager<User> userManager,
        IValidator<GetUserByEmailQuery> validator)
    {
        _userManager = userManager;
        _validator = validator;
    }

    public async Task<UserWithRoleDto> Handle(GetUserByEmailQuery request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            throw new EntityNotFoundException($"User with email {request.Email} not found.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? string.Empty;

        return user.ToUserWithRoleDto(role);
    }
}