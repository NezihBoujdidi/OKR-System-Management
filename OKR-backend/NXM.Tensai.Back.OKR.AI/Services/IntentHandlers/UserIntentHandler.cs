using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NXM.Tensai.Back.OKR.AI.Core.AI.Plugins;
using NXM.Tensai.Back.OKR.AI.Models;

namespace NXM.Tensai.Back.OKR.AI.Services.IntentHandlers
{
    /// <summary>
    /// Handler for user-related intents
    /// </summary>
    public class UserIntentHandler : BaseIntentHandler
    {
        private readonly UserPlugin _userPlugin;
        private readonly TeamPlugin _teamPlugin;
        private static readonly HashSet<string> _supportedIntents = new()
        {
            "SearchUsers",
            "GetTeamManagers",
            "InviteUser",
            "GetUsersByOrganizationId",
            "EnableUser",
            "DisableUser",
            "UpdateUser"
        };
        
        public UserIntentHandler(
            UserPlugin userPlugin,
            TeamPlugin teamPlugin,
            KernelService kernelService,
            ILogger<UserIntentHandler> logger) : base(kernelService, logger)
        {
            _userPlugin = userPlugin;
            _teamPlugin = teamPlugin;
        }
        
        public override bool CanHandle(string intent)
        {
            return _supportedIntents.Contains(intent);
        }
        
        public override async Task<FunctionExecutionResult> HandleIntentAsync(
            string conversationId,
            string intent,
            Dictionary<string, string> parameters, 
            UserContext userContext)
        {
            return intent switch
            {
                "SearchUsers" => await HandleSearchUsersAsync(conversationId, parameters, userContext),
                "GetTeamManagers" => await HandleGetTeamManagersAsync(conversationId, parameters, userContext),
                "InviteUser" => await HandleInviteUserAsync(parameters, userContext),
                "GetUsersByOrganizationId" => await HandleGetUsersByOrganizationIdAsync(parameters, userContext),
                "EnableUser" => await HandleEnableUserAsync(parameters, userContext),
                "DisableUser" => await HandleDisableUserAsync(parameters, userContext),
                "UpdateUser" => await HandleUpdateUserAsync(parameters, userContext),
                _ => CreateErrorResult($"Unsupported user intent: {intent}")
            };
        }
        
        private async Task<FunctionExecutionResult> HandleSearchUsersAsync(
            string conversationId,
            Dictionary<string, string> parameters,
            UserContext userContext)
        {
            // Extract search parameters
            var searchQuery = parameters.GetValueOrDefault("query", null);
            var searchOrgId = parameters.GetValueOrDefault("organizationId", userContext?.OrganizationId);
            
            // Perform the search
            var searchResult = await _userPlugin.SearchUsersByNameAsync(searchQuery);
            
            // Return the result
            return CreateSuccessResult(
                searchResult,
                "User",
                null, // No specific entity ID for search
                "Search users by query",
                searchResult.PromptTemplate);
        }
        
        private async Task<FunctionExecutionResult> HandleGetTeamManagersAsync(
            string conversationId,
            Dictionary<string, string> parameters,
            UserContext userContext)
        {
            // Get organizationId from parameters or context
            var orgId = parameters.GetValueOrDefault("organizationId", userContext?.OrganizationId);
            
            if (string.IsNullOrEmpty(orgId))
            {
                return CreateErrorResult("Organization ID is required to list team managers.");
            }
            
            // Get team managers
            var managersResult = await _userPlugin.GetTeamManagersByOrganizationIdAsync();
            
            return CreateSuccessResult(
                managersResult,
                "Organization",
                orgId,
                "List Team Managers By Organization",
                managersResult.PromptTemplate);
        }
        
        private async Task<FunctionExecutionResult> HandleInviteUserAsync(
            Dictionary<string, string> parameters,
            UserContext userContext)
        {
            // Extract parameters
            var email = parameters.GetValueOrDefault("email", null);
            var role = parameters.GetValueOrDefault("role", "Collaborator");
            var orgId = parameters.GetValueOrDefault("organizationId", userContext?.OrganizationId);
            var teamId = parameters.GetValueOrDefault("teamId", null);
            var teamName = parameters.GetValueOrDefault("teamName", null);
            
            if (string.IsNullOrEmpty(email))
            {
                return CreateErrorResult("Email address is required to invite a user.");
            }
            
            if (string.IsNullOrEmpty(orgId))
            {
                return CreateErrorResult("Organization ID is required to invite a user.");
            }
            
            // If we have teamName but no teamId, try to resolve it
            if (!string.IsNullOrEmpty(teamName) && string.IsNullOrEmpty(teamId))
            {
                try
                {
                    var searchResult = await _teamPlugin.SearchTeamsAsync(teamName);
                    if (searchResult.Teams.Count > 0)
                    {
                        // Use the first matching team
                        teamId = searchResult.Teams[0].TeamId;
                        Logger.LogInformation("Resolved team name '{TeamName}' to ID: {TeamId}", teamName, teamId);
                    }
                    else
                    {
                        Logger.LogWarning("Could not find team with name '{TeamName}'", teamName);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error looking up team ID for name '{TeamName}'", teamName);
                }
            }
            
            // Invite the user
            var result = await _userPlugin.InviteUserByEmailAsync(email, role, teamId);
            
            return CreateSuccessResult(
                result,
                "User",
                email, // Using email as "EntityId" since we don't have user ID yet
                "Invite User",
                result.PromptTemplate);
        }
        
        private async Task<FunctionExecutionResult> HandleGetUsersByOrganizationIdAsync(
            Dictionary<string, string> parameters,
            UserContext userContext)
        {
            // Get organizationId from parameters or context
            var orgId = parameters.GetValueOrDefault("organizationId", userContext?.OrganizationId);
            
            if (string.IsNullOrEmpty(orgId))
            {
                return CreateErrorResult("Organization ID is required to list users.");
            }
            
            // Get all users in the organization
            var usersResult = await _userPlugin.GetUsersByOrganizationIdAsync();
            
            return CreateSuccessResult(
                usersResult,
                "Organization",
                orgId,
                "List Users By Organization",
                usersResult.PromptTemplate);
        }
        
        private async Task<FunctionExecutionResult> HandleEnableUserAsync(
            Dictionary<string, string> parameters,
            UserContext userContext)
        {
            // Extract parameters
            var userId = parameters.GetValueOrDefault("userId", null);
            var userName = parameters.GetValueOrDefault("userName", null);
            
            if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(userName))
            {
                return CreateErrorResult("User ID or user name is required to enable a user account.");
            }
            
            // Enable the user
            var result = await _userPlugin.EnableUserAsync(userId, userName);
            
            return CreateSuccessResult(
                result,
                "User",
                userId ?? userName,
                "Enable User",
                result.PromptTemplate);
        }
        
        private async Task<FunctionExecutionResult> HandleDisableUserAsync(
            Dictionary<string, string> parameters,
            UserContext userContext)
        {
            // Extract parameters
            var userId = parameters.GetValueOrDefault("userId", null);
            var userName = parameters.GetValueOrDefault("userName", null);
            
            if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(userName))
            {
                return CreateErrorResult("User ID or user name is required to disable a user account.");
            }
            
            // Disable the user
            var result = await _userPlugin.DisableUserAsync(userId, userName);
            
            return CreateSuccessResult(
                result,
                "User",
                userId ?? userName,
                "Disable User",
                result.PromptTemplate);
        }
        
        private async Task<FunctionExecutionResult> HandleUpdateUserAsync(
            Dictionary<string, string> parameters,
            UserContext userContext)
        {
            // Extract parameters
            var userId = parameters.GetValueOrDefault("userId", null);
            var userName = parameters.GetValueOrDefault("userName", null);
            
            if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(userName))
            {
                return CreateErrorResult("User ID or user name is required to update a user.");
            }
            
            // Extract update fields
            var firstName = parameters.GetValueOrDefault("firstName", null);
            var lastName = parameters.GetValueOrDefault("lastName", null);
            var email = parameters.GetValueOrDefault("email", null);
            var address = parameters.GetValueOrDefault("address", null);
            var position = parameters.GetValueOrDefault("position", null);
            var organizationId = parameters.GetValueOrDefault("organizationId", userContext?.OrganizationId);
            var profilePictureUrl = parameters.GetValueOrDefault("profilePictureUrl", null);
            var gender = parameters.GetValueOrDefault("gender", null);
            
            // Parse boolean parameters if present
            bool? isNotificationEnabled = null;
            if (parameters.TryGetValue("isNotificationEnabled", out var notificationValue))
            {
                if (bool.TryParse(notificationValue, out var parsedValue))
                {
                    isNotificationEnabled = parsedValue;
                }
            }
            
            bool? isEnabled = null;
            if (parameters.TryGetValue("isEnabled", out var enabledValue))
            {
                if (bool.TryParse(enabledValue, out var parsedValue))
                {
                    isEnabled = parsedValue;
                }
            }
            
            // Update the user
            var result = await _userPlugin.UpdateUserAsync(
                userId, userName, firstName, lastName, email, 
                address, position, profilePictureUrl, 
                gender, isNotificationEnabled, isEnabled);
            
            return CreateSuccessResult(
                result,
                "User",
                userId ?? userName,
                "Update User",
                result.PromptTemplate);
        }
    }
}
