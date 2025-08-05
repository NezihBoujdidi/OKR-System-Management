using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace NXM.Tensai.Back.OKR.Infrastructure;

public class SupabaseAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SupabaseAuthenticationMiddleware> _logger;

    public SupabaseAuthenticationMiddleware(
        RequestDelegate next,
        ILogger<SupabaseAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context, 
        SupabaseJwtService supabaseJwtService, 
        IUserRepository userRepository,
        UserManager<User> userManager)
    {
        // Check if there's an Authorization header
        if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            _logger.LogInformation("No Authorization header found in request");
            await _next(context);
            return;
        }

        string authHeaderValue = authHeader.ToString();
        
        // Check if it's a Bearer token
        if (!authHeaderValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Authorization header does not contain a Bearer token");
            await _next(context);
            return;
        }

        // Extract the token
        string token = authHeaderValue.Substring("Bearer ".Length).Trim();
        
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogInformation("Bearer token is empty");
            await _next(context);
            return;
        }

        _logger.LogInformation("Processing Bearer token for authentication");
        
        try
        {
            // Validate the token and extract Supabase user ID in one step
            var principal = supabaseJwtService.ValidateToken(token, out var supabaseUserId);
            if (principal == null)
            {
                _logger.LogWarning("Token validation failed");
                await _next(context);
                return;
            }

            // Check if we successfully extracted the Supabase user ID
            if (string.IsNullOrEmpty(supabaseUserId))
            {
                _logger.LogWarning("Could not extract Supabase user ID from token");
                await _next(context);
                return;
            }

            // Find the user in our database
            _logger.LogInformation("Looking up user with Supabase ID: {SupabaseId}", supabaseUserId);
            var user = await userRepository.GetUserBySupabaseIdAsync(supabaseUserId);
            if (user == null)
            {
                _logger.LogWarning("User with Supabase ID {SupabaseId} not found in database", supabaseUserId);
                // For debugging, try to find user by email if available
                var emailClaim = principal.FindFirst(ClaimTypes.Email) ?? principal.FindFirst("email");
                if (emailClaim != null)
                {
                    _logger.LogInformation("Trying to find user by email: {Email}", emailClaim.Value);
                    var userByEmail = await userRepository.GetUserByEmailAsync(emailClaim.Value);
                    if (userByEmail != null)
                    {
                        _logger.LogWarning("User found by email but Supabase ID doesn't match. Database ID: {DatabaseId}, Email: {Email}", 
                            userByEmail.Id, emailClaim.Value);
                        
                        // If user is found by email but doesn't have Supabase ID, consider updating it
                        if (string.IsNullOrEmpty(userByEmail.SupabaseId))
                        {
                            _logger.LogInformation("Updating user {UserId} with Supabase ID: {SupabaseId}", 
                                userByEmail.Id, supabaseUserId);
                            userByEmail.SupabaseId = supabaseUserId;
                            await userRepository.UpdateAsync(userByEmail);
                            user = userByEmail;
                        }
                    }
                }
                
                if (user == null)
                {
                    await _next(context);
                    return;
                }
            }

            _logger.LogInformation("User found in database: ID={UserId}, Name={UserName}", user.Id, user.UserName);
            
            // Create a new identity with all relevant claims
            var identity = new ClaimsIdentity(principal.Identity);
            
            // Add claims from our user
            identity.AddClaim(new Claim("SupabaseId", supabaseUserId));
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
            
            // Add role claims
            if (!principal.HasClaim(c => c.Type == ClaimTypes.Role))
            {
                // Use UserManager to get roles instead of the repository
                var roles = await userManager.GetRolesAsync(user);
                
                foreach (var role in roles)
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, role));
                    _logger.LogDebug("Added role claim: {Role}", role);
                }
            }
            
            // Replace the current user with our enhanced user
            context.User = new ClaimsPrincipal(identity);
            
            _logger.LogInformation("User successfully authenticated: {UserId}", user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing authentication token: {ErrorMessage}", ex.Message);
        }

        await _next(context);
    }
}