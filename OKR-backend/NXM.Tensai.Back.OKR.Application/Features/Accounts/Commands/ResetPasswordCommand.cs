namespace NXM.Tensai.Back.OKR.Application;

public class ResetPasswordCommand : IRequest
{
    public string Email { get; init; } = null!;
    public string Token { get; init; } = null!;
    public string Password { get; init; } = null!;
    public string ConfirmPassword { get; init; } = null!;
}

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Passwords do not match.");
    }
}

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly UserManager<User> _userManager;
    private readonly IValidator<ResetPasswordCommand> _validator;

    public ResetPasswordCommandHandler(UserManager<User> userManager, IValidator<ResetPasswordCommand> validator)
    {
        _userManager = userManager;
        _validator = validator;
    }

    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            throw new UserNotFoundException();
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.Password);
        if (!result.Succeeded)
        {
            throw new PasswordResetException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}
