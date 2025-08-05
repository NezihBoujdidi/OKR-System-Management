// This handler is currently unused and contains compilation errors.
// Since we're handling authentication through the SupabaseAuthenticationMiddleware,
// this file can be safely removed or kept as a reference for future needs.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace NXM.Tensai.Back.OKR.Infrastructure;

public class SupabaseAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IUserRepository _userRepository;
    private readonly SupabaseJwtService _supabaseJwtService;
    private readonly ILogger<SupabaseAuthenticationHandler> _logger;

    public SupabaseAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory loggerFactory,
        UrlEncoder encoder,
        IUserRepository userRepository,
        SupabaseJwtService supabaseJwtService)
        : base(options, loggerFactory, encoder)
    {
        _userRepository = userRepository;
        _supabaseJwtService = supabaseJwtService;
        _logger = loggerFactory.CreateLogger<SupabaseAuthenticationHandler>();
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check if the Authorization header is present
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return AuthenticateResult.NoResult();
        }

        string authorizationHeader = Request.Headers["Authorization"];
        if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        string token = authorizationHeader.Substring("Bearer ".Length).Trim();
        if (string.IsNullOrEmpty(token))
        {
            return AuthenticateResult.NoResult();
        }

        try
        {
            // Validate the token and get Supabase user ID
            var principal = _supabaseJwtService.ValidateToken(token, out var supabaseUserId);
            if (principal == null)
            {
                _logger.LogWarning("Token validation failed");
                return AuthenticateResult.Fail("Invalid token");
            }

            // Check if we successfully extracted the Supabase user ID
            if (string.IsNullOrEmpty(supabaseUserId))
            {
                _logger.LogWarning("Could not extract Supabase user ID from token");
                return AuthenticateResult.Fail("Invalid token format");
            }

            // Find the user in our database by Supabase ID
            var user = await _userRepository.GetUserBySupabaseIdAsync(supabaseUserId);
            if (user == null)
            {
                return AuthenticateResult.Fail("User not found");
            }

            // Create a new principal with claims from our system
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim("SupabaseId", user.SupabaseId)
            };

            var identity = new ClaimsIdentity(claims, JwtBearerDefaults.AuthenticationScheme);
            var authenticatedPrincipal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(authenticatedPrincipal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Exception occurred while processing Supabase JWT token");
            return AuthenticateResult.Fail("Authentication failed");
        }
    }
}