using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace NXM.Tensai.Back.OKR.Infrastructure;

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly IRoleClaimsRepository _roleClaimsRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<PermissionHandler> _logger;

    public PermissionHandler(
        UserManager<User> userManager, 
        RoleManager<Role> roleManager, 
        IRoleClaimsRepository roleClaimsRepository,
        IUserRepository userRepository,
        ILogger<PermissionHandler> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _roleClaimsRepository = roleClaimsRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        _logger.LogInformation("Checking permission: {Permission}", requirement.Permission);

        if (context.User == null)
        {
            _logger.LogWarning("Authorization failed: No user in context");
            return;
        }

        // Log all claims for debugging
        foreach (var claim in context.User.Claims)
        {
            _logger.LogDebug("Claim: {Type} = {Value}", claim.Type, claim.Value);
        }

        // Get the user from the context using different possible identifiers
        User user = null;

        // First try to get by SupabaseId if present
        var supabaseIdClaim = context.User.FindFirst("SupabaseId");
        if (supabaseIdClaim != null)
        {
            _logger.LogInformation("Looking up user by Supabase ID: {SupabaseId}", supabaseIdClaim.Value);
            user = await _userRepository.GetUserBySupabaseIdAsync(supabaseIdClaim.Value);
            
            if (user != null)
            {
                _logger.LogInformation("Found user by Supabase ID: {UserId}", user.Id);
            }
        }

        // If not found by SupabaseId, try the standard NameIdentifier (user ID)
        if (user == null)
        {
            var nameIdentifierClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
            if (nameIdentifierClaim != null && Guid.TryParse(nameIdentifierClaim.Value, out var userId))
            {
                _logger.LogInformation("Looking up user by ID: {UserId}", userId);
                user = await _userManager.FindByIdAsync(userId.ToString());
                
                if (user != null)
                {
                    _logger.LogInformation("Found user by ID: {UserId}", user.Id);
                }
            }
        }

        // If still not found, try by email
        if (user == null)
        {
            var emailClaim = context.User.FindFirst(ClaimTypes.Email) ?? context.User.FindFirst("email");
            if (emailClaim != null)
            {
                _logger.LogInformation("Looking up user by email: {Email}", emailClaim.Value);
                user = await _userRepository.GetUserByEmailAsync(emailClaim.Value);
                
                if (user != null)
                {
                    _logger.LogInformation("Found user by email: {UserId}, {Email}", user.Id, emailClaim.Value);
                }
            }
        }

        // If we couldn't find the user, authorization fails
        if (user == null)
        {
            _logger.LogWarning("Authorization failed: User not found in database");
            return;
        }

        // Check if the user has the required permission through their roles
        var roles = await _userManager.GetRolesAsync(user);
        _logger.LogInformation("User has {RoleCount} roles: {Roles}", roles.Count, string.Join(", ", roles));

        foreach (var role in roles)
        {
            var identityRole = await _roleManager.FindByNameAsync(role);
            if (identityRole != null)
            {
                _logger.LogDebug("Checking role {RoleName} for permission {Permission}", role, requirement.Permission);
                var hasClaim = await _roleClaimsRepository.HasClaimAsync(identityRole.Id, "Permission", requirement.Permission);
                
                if (hasClaim)
                {
                    _logger.LogInformation("User {UserId} authorized for {Permission} through role {Role}", user.Id, requirement.Permission, role);
                    context.Succeed(requirement);
                    return;
                }
            }
        }

        _logger.LogWarning("Authorization failed: User {UserId} does not have permission {Permission}", user.Id, requirement.Permission);
    }
}
