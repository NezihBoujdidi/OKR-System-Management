using System.Threading.Tasks;
using NXM.Tensai.Back.OKR.AI.Models;

namespace NXM.Tensai.Back.OKR.AI.Services.Authorization
{
    /// <summary>
    /// Service interface for handling permissions in AI plugins
    /// </summary>
    public interface IPermissionService
    {
        /// <summary>
        /// Checks if the user has the required permission.
        /// User context is retrieved from UserContextAccessor internally.
        /// The userContext parameter is kept for backward compatibility but is ignored.
        /// </summary>
        /// <param name="userContext">DEPRECATED - user context is retrieved from UserContextAccessor</param>
        /// <param name="permission">The permission to check</param>
        /// <returns>True if the user has the permission, false otherwise</returns>
        Task<bool> HasPermissionAsync(UserContext userContext, string permission);
        
        /// <summary>
        /// Checks if the user has the required permission.
        /// User context is retrieved from UserContextAccessor internally.
        /// </summary>
        /// <param name="permission">The permission to check</param>
        /// <returns>True if the user has the permission, false otherwise</returns>
        Task<bool> HasPermissionAsync(string permission);
        
        /// <summary>
        /// Gets an unauthorized response with appropriate message from the prompt template
        /// </summary>
        /// <param name="permission">The permission that was checked</param>
        /// <param name="resourceType">The type of resource (e.g., "team", "user", etc.)</param>
        /// <param name="action">The action that was attempted (e.g., "create", "update", etc.)</param>
        /// <returns>A message to display to the user</returns>
        string GetUnauthorizedResponse(string permission, string resourceType, string action);
    }
}
