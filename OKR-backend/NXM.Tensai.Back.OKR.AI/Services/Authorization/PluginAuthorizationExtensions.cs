using System;
using System.Diagnostics;
using System.Threading.Tasks;
using NXM.Tensai.Back.OKR.AI.Models;
using NXM.Tensai.Back.OKR.Domain;

namespace NXM.Tensai.Back.OKR.AI.Services.Authorization
{
    /// <summary>
    /// Extension methods for authorization in plugins
    /// </summary>
    public static class PluginAuthorizationExtensions
    {
        /// <summary>
        /// Extension method to check permission and return a formatted unauthorized response if needed.
        /// UserContext is automatically retrieved from UserContextAccessor, so it doesn't need to be passed.
        /// </summary>
        public static async Task<T> CheckPermissionAsync<T>(
            this IPermissionService permissionService,
            string permission,
            string resourceType, 
            string action,
            Func<string, T> createUnauthorizedResponse) where T : class
        {
            // Create a stopwatch for timing the permission check
            var stopwatch = Stopwatch.StartNew();
            
            // Use the new overload that doesn't require userContext
            bool hasPermission = await permissionService.HasPermissionAsync(permission);
            stopwatch.Stop();
            
            Debug.WriteLine($"Permission check for {permission} completed in {stopwatch.ElapsedMilliseconds}ms - Result: {hasPermission}");
            
            if (!hasPermission)
            {
                string unauthorizedMessage = permissionService.GetUnauthorizedResponse(permission, resourceType, action);
                return createUnauthorizedResponse(unauthorizedMessage);
            }
            
            return null; // Null means authorized
        }
    }
}
