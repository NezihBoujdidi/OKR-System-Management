using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NXM.Tensai.Back.OKR.AI.Core.AI.Plugins;
using NXM.Tensai.Back.OKR.AI.Models;

namespace NXM.Tensai.Back.OKR.AI.Services.IntentHandlers
{
    /// <summary>
    /// Handler for team-related intents
    /// </summary>
    public class TeamIntentHandler : BaseIntentHandler
    {
        private readonly TeamPlugin _teamPlugin;
        private static readonly HashSet<string> _supportedIntents = new()
        {
            "CreateTeam",
            "UpdateTeam",
            "DeleteTeam",
            "SearchTeams",
            "GetTeamsByManagerId",
            "GetTeamsByOrganizationId"
        };
        
        public TeamIntentHandler(
            TeamPlugin teamPlugin, 
            KernelService kernelService,
            ILogger<TeamIntentHandler> logger) : base(kernelService, logger)
        {
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
                "CreateTeam" => await HandleCreateTeamAsync(conversationId, parameters, userContext),
                "UpdateTeam" => await HandleUpdateTeamAsync(conversationId, parameters, userContext),
                "DeleteTeam" => await HandleDeleteTeamAsync(conversationId, parameters, userContext),
                "SearchTeams" => await HandleSearchTeamsAsync(conversationId, parameters, userContext),
                "GetTeamsByManagerId" => await HandleGetTeamsByManagerIdAsync(conversationId, parameters, userContext),
                "GetTeamsByOrganizationId" => await HandleGetTeamsByOrganizationIdAsync(conversationId, parameters, userContext),
                _ => CreateErrorResult($"Unsupported team intent: {intent}")
            };
        }
        
        private async Task<FunctionExecutionResult> HandleCreateTeamAsync(
            string conversationId,
            Dictionary<string, string> parameters,
            UserContext userContext)
        {
            var teamName = parameters.GetValueOrDefault("teamName", "");
            if (string.IsNullOrEmpty(teamName))
            {
                return CreateErrorResult("Team name is required to create a team.");
            }

            var teamResult = await _teamPlugin.CreateTeamAsync(
                teamName,
                parameters.GetValueOrDefault("description", null));

            return CreateSuccessResult(
                teamResult,
                "Team",
                teamResult.TeamId,
                "Create",
                teamResult.PromptTemplate);
        }
        
        private async Task<FunctionExecutionResult> HandleUpdateTeamAsync(
            string conversationId,
            Dictionary<string, string> parameters,
            UserContext userContext)
        {
            // Get parameters
            var teamId = parameters.GetValueOrDefault("teamId", null);
            var teamToUpdateName = parameters.GetValueOrDefault("name", null);
            
            // If we have no teamId and no name, try to use the most recent team
            if (string.IsNullOrEmpty(teamId) && string.IsNullOrEmpty(teamToUpdateName))
            {
                teamId = KernelService.GetMostRecentEntityId(conversationId, "Team");
                Logger.LogInformation("No team specified, using most recent team ID from history: {TeamId}", teamId);
                
                if (string.IsNullOrEmpty(teamId))
                {
                    return CreateErrorResult("Could not find a team to update. Please specify which team you want to update.");
                }
            }

            // Get team manager ID or name
            var teamManagerId = parameters.GetValueOrDefault("teamManagerId", null);
            var teamManagerName = parameters.GetValueOrDefault("teamManagerName", null);

            // Log the manager information
            if (!string.IsNullOrEmpty(teamManagerName))
            {
                Logger.LogInformation("Team manager name provided: {ManagerName}", teamManagerName);
            }

            // Let the function service handle the team update and lookup logic
            var updateResult = await _teamPlugin.UpdateTeamAsync(
                teamId,
                teamToUpdateName, // Pass name for lookup or update
                parameters.GetValueOrDefault("description", null),
                teamManagerId,
                teamManagerName); // Pass team manager name to plugin

            return CreateSuccessResult(
                updateResult,
                "Team",
                updateResult.TeamId,
                "Update",
                updateResult.PromptTemplate);
        }
        
        private async Task<FunctionExecutionResult> HandleDeleteTeamAsync(
            string conversationId,
            Dictionary<string, string> parameters,
            UserContext userContext)
        {
            // Get parameters
            var deleteTeamId = parameters.GetValueOrDefault("teamId", null);
            var deleteTeamName = parameters.GetValueOrDefault("teamName", null);
            
            // If no teamId or name, try to get from recent history
            if (string.IsNullOrEmpty(deleteTeamId) && string.IsNullOrEmpty(deleteTeamName))
            {
                deleteTeamId = KernelService.GetMostRecentEntityId(conversationId, "Team");
                Logger.LogInformation("Retrieved most recent team ID from history for delete operation: {TeamId}", deleteTeamId);
                
                if (string.IsNullOrEmpty(deleteTeamId))
                {
                    return CreateErrorResult("Could not find the team to delete. Please specify which team you want to delete.");
                }
            }

            // Delete the team - function will handle searching by name if needed
            var deleteResult = await _teamPlugin.DeleteTeamAsync(deleteTeamId, deleteTeamName);

            return CreateSuccessResult(
                deleteResult,
                "Team",
                deleteResult.TeamId,
                "Delete",
                deleteResult.PromptTemplate);
        }
        
        private async Task<FunctionExecutionResult> HandleSearchTeamsAsync(
            string conversationId,
            Dictionary<string, string> parameters,
            UserContext userContext)
        {
            // Extract search parameters
            var searchName = parameters.GetValueOrDefault("query", null);
            var searchOrgId = parameters.GetValueOrDefault("organizationId", userContext?.OrganizationId);
            
            // Perform the search
            var searchResult = await _teamPlugin.SearchTeamsAsync(searchName);
            
            // Handle empty search results properly
            return CreateSuccessResult(
                searchResult,
                "Team",
                searchResult.Teams.Count > 0 ? searchResult.Teams[0].TeamId : null,
                "Search teams by query",
                searchResult.PromptTemplate);
        }
        
        private async Task<FunctionExecutionResult> HandleGetTeamsByManagerIdAsync(
            string conversationId,
            Dictionary<string, string> parameters,
            UserContext userContext)
        {
            // Get managerId from parameters
            var managerId = parameters.GetValueOrDefault("managerId", null);
            
            if (string.IsNullOrEmpty(managerId))
            {
                return CreateErrorResult("Manager ID is required to list teams by manager.");
            }
            
            // Get teams by manager
            var managerTeamsResult = await _teamPlugin.GetTeamsByManagerIdAsync(managerId);
            
            return CreateSuccessResult(
                managerTeamsResult,
                "Manager",
                managerId,
                "List Teams By Manager",
                managerTeamsResult.PromptTemplate);
        }
        
        private async Task<FunctionExecutionResult> HandleGetTeamsByOrganizationIdAsync(
            string conversationId,
            Dictionary<string, string> parameters,
            UserContext userContext)
        {
            // Get organizationId from parameters or context
            var orgId = parameters.GetValueOrDefault("organizationId", userContext?.OrganizationId);
            
            if (string.IsNullOrEmpty(orgId))
            {
                return CreateErrorResult("Organization ID is required to list teams by organization.");
            }
            
            // Get teams by organization
            var orgTeamsResult = await _teamPlugin.GetTeamsByOrganizationIdAsync();
            
            return CreateSuccessResult(
                orgTeamsResult,
                "Organization",
                orgId,
                "List Teams By Organization",
                orgTeamsResult.PromptTemplate);
        }
    }
}
