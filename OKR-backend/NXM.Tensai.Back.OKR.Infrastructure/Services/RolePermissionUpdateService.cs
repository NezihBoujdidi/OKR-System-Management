using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace NXM.Tensai.Back.OKR.Infrastructure;

public class RolePermissionUpdateService
{
    private readonly RoleManager<Role> _roleManager;
    private readonly ILogger<RolePermissionUpdateService> _logger;

    public RolePermissionUpdateService(
        RoleManager<Role> roleManager,
        ILogger<RolePermissionUpdateService> logger)
    {
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task UpdateRolePermissions(string roleName)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null)
        {
            _logger.LogWarning("Role {RoleName} not found", roleName);
            return;
        }

        // Get existing claims
        var existingClaims = await _roleManager.GetClaimsAsync(role);
        
        // Remove all existing permission claims
        foreach (var claim in existingClaims.Where(c => c.Type == "Permission"))
        {
            await _roleManager.RemoveClaimAsync(role, claim);
        }

        // Get fresh permissions for the role
        var roleType = Enum.Parse<RoleType>(roleName);
        var permissions = RoleSeeder.GetPermissionsForRole(roleType);

        // Add new permission claims
        foreach (var permission in permissions)
        {
            await _roleManager.AddClaimAsync(role, new Claim("Permission", permission));
        }

        _logger.LogInformation("Successfully updated permissions for role {RoleName}", roleName);
    }
}