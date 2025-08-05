namespace NXM.Tensai.Back.OKR.Application;

public class CreateUserCommand : IRequest<UserDto>
{
    public string Email { get; init; } = null!;
    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;
    public string UserName { get; init; } = null!;
    public string Address { get; init; } = null!;
    public string Position { get; init; } = null!;
    public DateTime DateOfBirth { get; init; }
    public bool IsEnabled { get; init; }
    public Gender Gender { get; init; }
    public string? SupabaseId { get; init; } = null; 
    public string Password { get; init; } = null!;
    public string ConfirmPassword { get; init; } = null!;
    public RoleType Role { get; init; } = RoleType.Collaborator; // Default role is Collaborator
}

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty();
        RuleFor(x => x.LastName).NotEmpty();
        RuleFor(x => x.Address).NotEmpty();
        RuleFor(x => x.DateOfBirth).LessThan(DateTime.Now);
        RuleFor(x => x.Gender).IsInEnum();
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.");
        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Passwords do not match.");
        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Invalid role.");
    }
}

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserDto>
{
    private readonly UserManager<User> _userManager;
    private readonly IUserRepository _userRepository;
    private readonly IValidator<CreateUserCommand> _validator;

    public CreateUserCommandHandler(UserManager<User> userManager, IUserRepository userRepository, IValidator<CreateUserCommand> validator)
    {
        _userManager = userManager;
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var user = request.ToUser();
        user.UserName = user.Email;
        await _userManager.AddPasswordAsync(user, request.Password);
        // Add SupabaseId if provided
        if (!string.IsNullOrEmpty(request.SupabaseId))
        {
            user.SupabaseId = request.SupabaseId;
        }

        

        var userExists = await _userRepository.GetUserByEmailAsync(user.Email!);

        if (userExists is not null)
        {
            throw new UserCreationException($"Username '{user.Email}' is already taken.");
        }

        var createdUser = await _userRepository.AddAsync(user);

        var roleResult = await _userManager.AddToRoleAsync(createdUser, request.Role.ToString());

        if (!roleResult.Succeeded)
        {
            throw new RoleAssignmentException();
        }

        return createdUser.ToDto();
    }
}
