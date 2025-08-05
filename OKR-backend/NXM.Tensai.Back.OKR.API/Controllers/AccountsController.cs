using ValidationException = FluentValidation.ValidationException;

namespace NXM.Tensai.Back.OKR.API;

[Route("api/accounts")]
[ApiController]
public class AccountsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AccountsController> _logger;

    public AccountsController(IMediator mediator, ILogger<AccountsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Registration attempt for email: {Email}", command.Email);

        try
        {
            var userId = await _mediator.Send(command, cancellationToken);
            _logger.LogInformation("Registration successful for email: {Email} with user ID: {UserId}", command.Email, userId);
            return Ok(new { UserId = userId });
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for email: {Email} - {Errors}", command.Email, ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (Application.UserCreationException ex)
        {
            _logger.LogError(ex, "User creation failed for email: {Email}", command.Email);
            return BadRequest(ex.Message);
        }
        catch (RoleAssignmentException ex)
        {
            _logger.LogError(ex, "Role assignment failed for email: {Email}", command.Email);
            return StatusCode(500, ex.Message);
        }
        catch (EmailException ex)
        {
            _logger.LogError(ex, "Email sending failed during registration for email: {Email}", command.Email);
            return StatusCode(500, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during registration attempt for email: {Email}", command.Email);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [HttpGet("validate-invite-key")]
    public async Task<IActionResult> ValidateKey([FromQuery] ValidateKeyQuery query)
    {
        _logger.LogInformation("ValidateKey attempt");

        try
        {
            var result = await _mediator.Send(query);

            if (result.ExpirationDate < DateTime.UtcNow)
            {
                _logger.LogWarning("Key expired for Key: {Key}", query.Key);
                return BadRequest("The invitation link has expired.");
            }

            _logger.LogInformation("ValidateKey successful");
            return Ok(result);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for ValidateKey: {Errors}", ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during ValidateKey attempt");
            return StatusCode(500, "An unexpected error occurred.");
        }
    }


    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginUserCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Login attempt for email: {Email}", command.Email);

        try
        {
            var response = await _mediator.Send(command, cancellationToken);
            _logger.LogInformation("Login successful for email: {Email}", command.Email);
            return Ok(response);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for email: {Email} - {Errors}", command.Email, ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (AccountDisabledException ex)
        {
            _logger.LogWarning(ex, "Login attempt for disabled account: {Email}", command.Email);
            return Unauthorized(ex.Message);
        }
        catch (InvalidCredentialsException ex)
        {
            _logger.LogWarning(ex, "Invalid credentials for email: {Email}", command.Email);
            return Unauthorized(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during login attempt for email: {Email}", command.Email);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [HttpPost("generate-invitation-link")]
    public async Task<IActionResult> GenerateInvitationLink(GenerateInvitationLinkCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Invitation link generation attempt for email: {Email}", command.Email);

        try
        {
            // This should be handled by the command handler
            await _mediator.Send(command, cancellationToken);

            _logger.LogInformation("Invitation link sent successfully for email: {Email}", command.Email);
            return Ok();
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for email: {Email} - {Errors}", command.Email, ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during invitation link generation attempt for email: {Email}", command.Email);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }


    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Forgot password attempt for email: {Email}", command.Email);

        try
        {
            await _mediator.Send(command, cancellationToken);
            _logger.LogInformation("Forgot password email sent for email: {Email}", command.Email);
            return Ok();
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for email: {Email} - {Errors}", command.Email, ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning(ex, "User not found for email: {Email}", command.Email);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during forgot password attempt for email: {Email}", command.Email);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Reset password attempt for email: {Email}", command.Email);

        try
        {
            await _mediator.Send(command, cancellationToken);
            _logger.LogInformation("Password reset successful for email: {Email}", command.Email);
            return Ok();
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for email: {Email} - {Errors}", command.Email, ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning(ex, "User not found for email: {Email}", command.Email);
            return NotFound(ex.Message);
        }
        catch (PasswordResetException ex)
        {
            _logger.LogError(ex, "Password reset failed for email: {Email}", command.Email);
            return StatusCode(500, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during reset password attempt for email: {Email}", command.Email);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Email confirmation attempt for user ID: {UserId}", command.UserId);

        try
        {
            await _mediator.Send(command, cancellationToken);
            _logger.LogInformation("Email confirmation successful for user ID: {UserId}", command.UserId);
            return Ok();
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for user ID: {UserId} - {Errors}", command.UserId, ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning(ex, "User not found for user ID: {UserId}", command.UserId);
            return NotFound(ex.Message);
        }
        catch (EmailConfirmationException ex)
        {
            _logger.LogError(ex, "Email confirmation failed for user ID: {UserId}", command.UserId);
            return StatusCode(500, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during email confirmation attempt for user ID: {UserId}", command.UserId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Refresh token attempt.");

        try
        {
            var response = await _mediator.Send(command, cancellationToken);
            _logger.LogInformation("Refresh token successful.");
            return Ok(response);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for refresh token: {Errors}", ex.Errors);
            return BadRequest(ex.Errors);
        }
        catch (InvalidRefreshTokenException ex)
        {
            _logger.LogWarning(ex, "Invalid refresh token.");
            return Unauthorized(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during refresh token attempt.");
            return StatusCode(500, "An unexpected error occurred.");
        }
    }
}
