namespace NXM.Tensai.Back.OKR.Application;

public class LoginUserCommand : IRequest<LoginResponse>
{
    public string Email { get; init; } = null!;
    // Password is no longer required since authentication is handled by Supabase
    public string SupabaseId { get; init; } = null!; // Supabase ID is required instead
}

public class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.SupabaseId)
            .NotEmpty().WithMessage("Supabase ID is required.");
    }
}

public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, LoginResponse>
{
    private readonly UserManager<User> _userManager;
    private readonly IJwtService _jwtService;
    private readonly IValidator<LoginUserCommand> _validator;

    public LoginUserCommandHandler(UserManager<User> userManager, IJwtService jwtService, IValidator<LoginUserCommand> validator)
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _validator = validator;
    }

    public async Task<LoginResponse> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // Find user by email and check Supabase ID
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            throw new InvalidCredentialsException("User not found");
        }

        if (!user.IsEnabled)
        {
            throw new AccountDisabledException();
        }

        // Verify if the SupabaseId matches or if it's not set yet, update it
        if (string.IsNullOrEmpty(user.SupabaseId))
        {
            // First time login with Supabase, update the SupabaseId
            user.SupabaseId = request.SupabaseId;
            await _userManager.UpdateAsync(user);
        }
        else if (user.SupabaseId != request.SupabaseId)
        {
            // SupabaseId mismatch - this should not happen
            throw new InvalidCredentialsException("Invalid credentials");
        }

        // Authentication successful - generate token
        var token = await _jwtService.GenerateJwtToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();
        var expires = DateTime.UtcNow.AddDays(7);
        user.RefreshTokens.Add(new RefreshToken { Token = refreshToken, Expires = expires });
        await _userManager.UpdateAsync(user);

        return new LoginResponse { Token = token, RefreshToken = refreshToken, Expires = expires };
    }
}
