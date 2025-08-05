using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using NXM.Tensai.Back.OKR.AI.Core.AI;
using NXM.Tensai.Back.OKR.AI.Models;
using NXM.Tensai.Back.OKR.Domain;

namespace NXM.Tensai.Back.OKR.AI.Services.Authorization
{
    /// <summary>
    /// Implementation of the permission service for Semantic Kernel plugins
    /// </summary>
    public class PermissionService : IPermissionService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleClaimsRepository _roleClaimsRepository;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly UserContextAccessor _userContextAccessor;
        private readonly PromptTemplateService _promptTemplateService;
        private readonly ILogger<PermissionService> _logger;

        public PermissionService(
            IUserRepository userRepository,
            IRoleClaimsRepository roleClaimsRepository,
            UserManager<User> userManager,
            RoleManager<Role> roleManager,
            UserContextAccessor userContextAccessor,
            PromptTemplateService promptTemplateService,
            ILogger<PermissionService> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _roleClaimsRepository = roleClaimsRepository ?? throw new ArgumentNullException(nameof(roleClaimsRepository));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _userContextAccessor = userContextAccessor ?? throw new ArgumentNullException(nameof(userContextAccessor));
            _promptTemplateService = promptTemplateService ?? throw new ArgumentNullException(nameof(promptTemplateService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        }

        /// <inheritdoc />
        public async Task<bool> HasPermissionAsync(UserContext userContext, string permission)
        {
            try
            {
                _logger.LogInformation("===== PERMISSION CHECK STARTED =====");
                _logger.LogInformation("Checking permission: {Permission}", permission);
                
                // Always use the context from the accessor
                var contextFromAccessor = _userContextAccessor.CurrentUserContext;
                
                if (contextFromAccessor == null)
                {
                    _logger.LogCritical("CRITICAL: UserContext is NULL in the UserContextAccessor");
                    return false;
                }
                
                _logger.LogInformation("UserContext details from accessor: UserId={UserId}, Role={Role}, OrganizationId={OrganizationId}", 
                    contextFromAccessor.UserId, contextFromAccessor.Role, contextFromAccessor.OrganizationId);

                // Quick short-circuit if no user ID provided (but allow context with just a role)
                if (string.IsNullOrEmpty(contextFromAccessor.UserId) && string.IsNullOrEmpty(contextFromAccessor.Role))
                {
                    _logger.LogWarning("No user ID or role provided in user context for permission check");
                    return false;
                }

                // Admin override - only SuperAdmin role has all permissions automatically
                if (!string.IsNullOrEmpty(contextFromAccessor.Role))
                {
                    _logger.LogInformation("Checking role-based permission override for role: {Role}", contextFromAccessor.Role);
                    
                    // Only SuperAdmin gets full permissions automatically
                    var adminRoles = new[] { "SuperAdmin" };
                    
                    if (adminRoles.Any(r => contextFromAccessor.Role.Equals(r, StringComparison.OrdinalIgnoreCase)))
                    {
                        _logger.LogInformation("User {UserId} has SuperAdmin role, granting permission {Permission}", 
                            contextFromAccessor.UserId, permission);
                        return true;
                    }
                    
                    _logger.LogDebug("Role {Role} is not SuperAdmin - checking specific permissions", contextFromAccessor.Role);
                }

                // Try to parse as Guid, if fails, might be a string ID
                if (!Guid.TryParse(contextFromAccessor.UserId, out var userId))
                {
                    // Either use a lookup service or deny access
                    _logger.LogWarning("User ID {UserId} is not in expected GUID format", contextFromAccessor.UserId);
                    return false;
                }

                // Get user from repository
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found", contextFromAccessor.UserId);
                    return false;
                }

                // Get the roles for the user
                var roleNames = await _userManager.GetRolesAsync(user);
                _logger.LogInformation("User {UserId} has roles: {roleNames}", contextFromAccessor.UserId, string.Join(", ", roleNames));
                
                foreach (var roleName in roleNames)
                {
                    // Get the role by name first
                    var role = await _roleManager.FindByNameAsync(roleName);
                    _logger.LogInformation("Role found for user {UserId}: {role}", 
                                contextFromAccessor.UserId, role);
                    if (role != null)
                    {
                        // Check for AccessAll permission first
                        if (await _roleClaimsRepository.HasClaimAsync(role.Id, "Permission", Permissions.AccessAll))
                        {
                            _logger.LogInformation("User {UserId} has AccessAll permission via role {RoleName}", 
                                contextFromAccessor.UserId, roleName);
                            return true;
                        }

                        // Add detailed logging before specific permission check
                        _logger.LogInformation("Checking specific permission {Permission} for role {RoleId} ({RoleName})", 
                            permission, role.Id, roleName);

                        // Check for the specific permission
                        if (await _roleClaimsRepository.HasClaimAsync(role.Id, "Permission", permission))
                        {
                            _logger.LogInformation("User {UserId} has permission {Permission} via role {RoleName}", 
                                contextFromAccessor.UserId, permission, roleName);
                            return true;
                        }
                        else
                        {
                            _logger.LogWarning("Role {RoleName} ({RoleId}) does NOT have permission {Permission}", 
                                roleName, role.Id, permission);
                        }
                    }
                }

                _logger.LogWarning("User {UserId} does not have permission {Permission}", 
                    contextFromAccessor.UserId, permission);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permission {Permission} for user {UserId}", 
                    permission, _userContextAccessor.CurrentUserContext?.UserId);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> HasPermissionAsync(string permission)
        {
            return await HasPermissionAsync(null, permission);
        }

        /// <inheritdoc />
        public string GetUnauthorizedResponse(string permission, string resourceType, string action)
        {
            try
            {
                // Create the template values
                var templateValues = new Dictionary<string, string>
                {
                    { "resourceType", resourceType },
                    { "action", action },
                    { "permission", permission }
                };

                // Try to get the template
                return _promptTemplateService.GetPrompt("UnauthorizedAccess", templateValues);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unauthorized response template for {Permission}", permission);
                // Fallback message
                return $"I'm sorry, but you don't have permission to {action} {resourceType}s. Please contact your administrator if you need this access.";
            }
        }
    }
}
