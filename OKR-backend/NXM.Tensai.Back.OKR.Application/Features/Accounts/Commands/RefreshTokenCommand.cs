using System.Security.Claims;

namespace NXM.Tensai.Back.OKR.Application;

public class RefreshTokenCommand : IRequest<RefreshTokenResponse>
{
    public string Token { get; init; } = null!;
}

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty().WithMessage("Token is required.");
    }
}

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    private readonly UserManager<User> _userManager;
    private readonly IJwtService _jwtService;
    private readonly IValidator<RefreshTokenCommand> _validator;

    public RefreshTokenCommandHandler(UserManager<User> userManager, IJwtService jwtService, IValidator<RefreshTokenCommand> validator)
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _validator = validator;
    }

    public async Task<RefreshTokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var principal = _jwtService.GetPrincipalFromExpiredToken(request.Token);
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null || !user.RefreshTokens.Any(t => t.Token == request.Token && t.IsActive))
        {
            throw new InvalidRefreshTokenException();
        }

        var newJwtToken = await _jwtService.GenerateJwtToken(user);
        var newRefreshToken = _jwtService.GenerateRefreshToken();
        var expires = DateTime.UtcNow.AddDays(7);

        user.RefreshTokens.Add(new RefreshToken { Token = newRefreshToken, Expires = expires });
        await _userManager.UpdateAsync(user);

        return new RefreshTokenResponse { Token = newJwtToken, RefreshToken = newRefreshToken, Expires = expires };
    }
}
