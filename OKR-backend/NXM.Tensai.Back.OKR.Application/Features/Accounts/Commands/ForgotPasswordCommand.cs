namespace NXM.Tensai.Back.OKR.Application;

public class ForgotPasswordCommand : IRequest
{
    public string Email { get; init; } = null!;
}

public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");
    }
}

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand>
{
    private readonly UserManager<User> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly IValidator<ForgotPasswordCommand> _validator;

    public ForgotPasswordCommandHandler(UserManager<User> userManager, IEmailSender emailSender, IValidator<ForgotPasswordCommand> validator)
    {
        _userManager = userManager;
        _emailSender = emailSender;
        _validator = validator;
    }

    public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
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

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = Uri.EscapeDataString(token);
        var resetLink = $"http://localhost:4200/login/reset-password?userId={user.Id}&token={encodedToken}";
        var emailBody = $@"
            <p>Hello,</p>
            <p>You requested a password reset. Please click the link below to reset your password:</p>
            <p><a href='{resetLink}'>Reset your password</a></p>
            <p>If you did not request a password reset, please ignore this email.</p>
            <p>Best regards,<br>OKR Support Team.</p>
";
        await _emailSender.SendEmailAsync(user.Email, "Reset your password", emailBody);
    }
}
