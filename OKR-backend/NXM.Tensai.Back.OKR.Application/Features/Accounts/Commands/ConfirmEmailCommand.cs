namespace NXM.Tensai.Back.OKR.Application;

public class ConfirmEmailCommand : IRequest
{
    public string UserId { get; init; } = null!;
    public string Token { get; init; } = null!;
}

public class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token is required.");
    }
}

public class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand>
{
    private readonly UserManager<User> _userManager;
    private readonly IValidator<ConfirmEmailCommand> _validator;

    public ConfirmEmailCommandHandler(UserManager<User> userManager, IValidator<ConfirmEmailCommand> validator)
    {
        _userManager = userManager;
        _validator = validator;
    }

    public async Task Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            throw new UserNotFoundException();
        }

        var result = await _userManager.ConfirmEmailAsync(user, request.Token);
        if (!result.Succeeded)
        {
            throw new EmailConfirmationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}
