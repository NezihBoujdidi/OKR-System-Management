using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace NXM.Tensai.Back.OKR.Infrastructure;

public class SupabaseJwtService
{
    private readonly SupabaseJwtSettings _supabaseSettings;
    private readonly ILogger<SupabaseJwtService> _logger;
    private readonly Dictionary<string, string> _tokenSubjectCache = new();

    public SupabaseJwtService(
        IOptions<SupabaseJwtSettings> supabaseSettings,
        ILogger<SupabaseJwtService> logger)
    {
        _supabaseSettings = supabaseSettings.Value;
        _logger = logger;
    }

    public ClaimsPrincipal ValidateToken(string token, out string supabaseUserId)
    {
        supabaseUserId = null;
        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = GetTokenValidationParameters();

        try
        {
            _logger.LogInformation("Attempting to validate Supabase token");
            
            // First, manually decode the token to inspect its contents
            var jwtToken = tokenHandler.ReadJwtToken(token);
            
            _logger.LogInformation("Token issuer: {Issuer}", jwtToken.Issuer);
            _logger.LogInformation("Token audience: {Audience}", jwtToken.Audiences.FirstOrDefault());
            
            // Extract subject ID directly from token
            if (jwtToken.Claims.FirstOrDefault(c => c.Type == "sub") is Claim subClaim)
            {
                supabaseUserId = subClaim.Value;
                _logger.LogInformation("Token contains subject claim: {SubjectId}", supabaseUserId);
                
                // Cache the subject ID for this token
                _tokenSubjectCache[token] = supabaseUserId;
            }
            else
            {
                _logger.LogWarning("Token doesn't contain required subject claim");
            }
            
            // Now validate the token
            var principal = tokenHandler.ValidateToken(token, validationParameters, out var securityToken);
            
            if (!(securityToken is JwtSecurityToken jwtSecurityToken))
            {
                _logger.LogWarning("Token is not a valid JWT security token");
                return null;
            }

            // If we couldn't get the subject from JWT claims, try from validated principal
            if (string.IsNullOrEmpty(supabaseUserId))
            {
                var subFromPrincipal = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                    ?? principal.FindFirst("sub")?.Value;
                
                if (!string.IsNullOrEmpty(subFromPrincipal))
                {
                    supabaseUserId = subFromPrincipal;
                    _tokenSubjectCache[token] = supabaseUserId;
                    _logger.LogInformation("Found subject claim in principal: {SubjectId}", supabaseUserId);
                }
            }

            _logger.LogInformation("Token validation succeeded");
            return principal;
        }
        catch (Exception ex)
        {
            // Log detailed exception for troubleshooting
            _logger.LogError(ex, "Token validation failed: {ErrorMessage}", ex.Message);
            return null;
        }
    }

    // Overload for backward compatibility
    public ClaimsPrincipal ValidateToken(string token)
    {
        return ValidateToken(token, out _);
    }

    public string GetSupabaseUserId(ClaimsPrincipal principal)
    {
        if (principal == null)
        {
            _logger.LogWarning("Principal is null");
            return null;
        }

        // Try different possible claim types for the subject ID
        var subClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        
        if (string.IsNullOrEmpty(subClaim))
        {
            // Try alternate claim types if standard "sub" isn't found
            subClaim = principal.FindFirst("sub")?.Value;
        }
        
        if (string.IsNullOrEmpty(subClaim))
        {
            // Direct access to claims collection as last resort
            var allClaims = principal.Claims.ToList();
            _logger.LogInformation("All available claims: {ClaimCount}", allClaims.Count);
            
            foreach (var claim in allClaims)
            {
                _logger.LogInformation("Claim {ClaimType}: {ClaimValue}", claim.Type, claim.Value);
                
                // Check for any claim that might contain the subject ID
                if (claim.Type.Contains("sub", StringComparison.OrdinalIgnoreCase))
                {
                    subClaim = claim.Value;
                    _logger.LogInformation("Found subject in alternate claim: {ClaimType}", claim.Type);
                    break;
                }
            }
        }
        
        if (string.IsNullOrEmpty(subClaim))
        {
            _logger.LogWarning("Could not find subject claim in token");
            return null;
        }
        
        _logger.LogInformation("Found Supabase user ID in token: {SubjectId}", subClaim);
        return subClaim;
    }

    private TokenValidationParameters GetTokenValidationParameters()
    {
        // For debugging purposes, check if settings are available
        if (string.IsNullOrWhiteSpace(_supabaseSettings.JwtSecret))
        {
            _logger.LogWarning("JWT Secret is empty or not configured");
        }

        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _supabaseSettings.Issuer,
            ValidAudience = _supabaseSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_supabaseSettings.JwtSecret)),
            // Add more flexible clock skew since there might be time differences
            ClockSkew = TimeSpan.FromMinutes(5),
            // Add name mapping for subject claim in case it's using a non-standard name
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
    }
}