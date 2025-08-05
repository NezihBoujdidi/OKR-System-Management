using System.ComponentModel;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using NXM.Tensai.Back.OKR.AI.Models;
using NXM.Tensai.Back.OKR.AI.Services.MediatRService;
using NXM.Tensai.Back.OKR.AI.Services.Authorization;
using NXM.Tensai.Back.OKR.Domain;

namespace NXM.Tensai.Back.OKR.AI.Core.AI.Plugins
{
    public class TeamPlugin
    {
        private readonly PromptTemplateService _promptTemplateService;
        private readonly IServiceProvider _serviceProvider;
        private readonly UserPlugin _userPlugin;
        private readonly ILogger<TeamPlugin> _logger;
        private readonly UserContextAccessor _userContextAccessor;

        public TeamPlugin(
            PromptTemplateService promptTemplateService,
            IServiceProvider serviceProvider,
            UserPlugin userPlugin,
            ILogger<TeamPlugin> logger,
            IConfiguration configuration,
            UserContextAccessor userContextAccessor) // Inject UserContextAccessor
        {
            _promptTemplateService = promptTemplateService;
            _serviceProvider = serviceProvider;
            _userPlugin = userPlugin;
            _logger = logger;
            _userContextAccessor = userContextAccessor; // Store reference to UserContextAccessor
        }

        /// <summary>
        /// Create a team with the provided name, description, and organization ID
        /// </summary>
        [KernelFunction]
        [Description("Create a new team with the given name and optional description. The organization ID is automatically provided from user context.")]
        public async Task<TeamCreationResponse> CreateTeamAsync(
            [Description("The name to give to the new team")] string teamName,
            [Description("Optional description of the team purpose")] string description = null)
        {
            try
            {
                // Get user context directly from the accessor instead of creating a new scope
                var userContext = _userContextAccessor.CurrentUserContext;
                
                _logger.LogInformation("TeamPlugin.CreateTeamAsync called - Team: {TeamName}, User: {UserId}, Role: {UserRole}", 
                    teamName, userContext?.UserId, userContext?.Role);

                if (userContext != null)
                {
                    _logger.LogDebug("User Context Details - UserId: {UserId}, Role: {Role}, OrganizationId: {OrganizationId}",
                        userContext.UserId, userContext.Role, userContext.OrganizationId);
                }
                else
                {
                    _logger.LogWarning("User Context is null in TeamPlugin.CreateTeamAsync call");
                }

                // Create a scope to resolve the scoped service
                using var scope = _serviceProvider.CreateScope();
                var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();

                // The UserContextAccessor will be used inside permissionService
                var unauthorizedResponse = await permissionService.CheckPermissionAsync<TeamCreationResponse>(
                    Permissions.Teams_Create,
                    "team", 
                    "create",
                    (message) => new TeamCreationResponse 
                    { 
                        PromptTemplate = message,
                        Name = teamName
                    });
                
                // If unauthorized, return the response
                if (unauthorizedResponse != null)
                {
                    _logger.LogWarning("Permission denied for user {UserId} to create team. Message: {Message}", 
                        userContext?.UserId, unauthorizedResponse.PromptTemplate);
                    return unauthorizedResponse;
                }
                
                _logger.LogInformation("Permission check passed for user {UserId} to create team", userContext?.UserId);
                
                if (string.IsNullOrEmpty(userContext?.OrganizationId))
                {
                    throw new ArgumentNullException(nameof(userContext.OrganizationId), "Organization ID is required to create a team.");
                }

                // Get the TeamMediatRService from the same scope
                var teamMediatRService = scope.ServiceProvider.GetRequiredService<TeamMediatRService>();

                // Create a request object for the MediatR service
                var request = new TeamCreationRequest
                {
                    Name = teamName,
                    Description = description ?? $"Team {teamName}",
                    OrganizationId = userContext.OrganizationId,
                    CurrentUserId = userContext?.UserId // Add the current user ID from the context
                };
                
                // Call the MediatR service to directly handle the team creation
                var result = await teamMediatRService.CreateTeamAsync(request);
                
                _logger.LogInformation("Team created successfully: {TeamId}, {TeamName} in organization {OrganizationId}", 
                    result.TeamId, result.Name, userContext?.OrganizationId);

                // Instead of generating AI response here, just generate and return the prompt template
                Dictionary<string, string> templateValues = new()
                {
                    { "teamName", result.Name },
                    { "organizationId", userContext.OrganizationId }
                };

                result.PromptTemplate = !string.IsNullOrEmpty(description)
                    ? _promptTemplateService.GetPrompt("TeamCreatedWithDescription", templateValues.Concat(new[] 
                        { new KeyValuePair<string, string>("description", description) }).ToDictionary(x => x.Key, x => x.Value))
                    : _promptTemplateService.GetPrompt("TeamCreated", templateValues);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating team '{TeamName}'", 
                    teamName);
                throw;
            }
        }

        /// <summary>
        /// Search for teams with optional name filter
        /// </summary>
        [KernelFunction]
        [Description("Search for teams by name")]
        public async Task<TeamSearchResponse> SearchTeamsAsync(
            [Description("Optional name to filter teams by")] string name = null)
        {
            try
            {
                // Get user context directly from the accessor
                var userContext = _userContextAccessor.CurrentUserContext;

                // Create a scope to resolve the scoped service
                using var scope = _serviceProvider.CreateScope();
                var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
                
                // Check read permission - use the exact permission name from Permissions class
                _logger.LogDebug("Checking permission {Permission}", "permission:teams_get_all");
                
                var unauthorizedResponse = await permissionService.CheckPermissionAsync<TeamSearchResponse>(
                    Permissions.Teams_GetAll, // Use exact permission name
                    "team", 
                    "search",
                    (message) => new TeamSearchResponse 
                    { 
                        PromptTemplate = message,
                        SearchTerm = name
                    });
                
                // If unauthorized, return the response
                if (unauthorizedResponse != null)
                {
                    _logger.LogWarning("Permission denied for user {UserId} to search teams. Message: {Message}", 
                        userContext?.UserId, unauthorizedResponse.PromptTemplate);
                    return unauthorizedResponse;
                }
                
                _logger.LogInformation("Permission check passed for user {UserId} to search teams", userContext?.UserId);
                
                // Create a scope to resolve the scoped service
                var teamMediatRService = scope.ServiceProvider.GetRequiredService<TeamMediatRService>();
                    
                // Use the MediatR service to search for teams
                var result = await teamMediatRService.SearchTeamsAsync(name, userContext.OrganizationId);
                
                // Enhance search results with detailed team information
                var enhancedTeams = new List<TeamDetailsResponse>();
                
                // Fetch detailed information for each team found
                foreach (var team in result.Teams)
                {
                    try
                    {
                        // Get detailed team info by ID
                        var detailedTeam = await teamMediatRService.GetTeamDetailsAsync(team.TeamId);
                        enhancedTeams.Add(detailedTeam);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not fetch detailed information for team {TeamId}. Using basic info.", team.TeamId);
                        // Keep the basic team info if details can't be fetched
                        enhancedTeams.Add(team);
                    }
                }
                
                // Replace the teams in the result with the enhanced ones
                result.Teams = enhancedTeams;
                
                // Generate the teams list for the prompt template with more detailed information
                var teamsListBuilder = new StringBuilder();
                for (int i = 0; i < result.Teams.Count; i++)
                {
                    var team = result.Teams[i];
                    teamsListBuilder.AppendLine($"{i+1}. {team.Name}");
                    
                    // Add description if available
                    if (!string.IsNullOrEmpty(team.Description))
                    {
                        teamsListBuilder.AppendLine($"   Description: {team.Description}");
                    }
                    
                    // Add member count if available
                    if (team.Members != null)
                    {
                        teamsListBuilder.AppendLine($"   Members: {team.Members.Count}");
                    }
                    
                    // Add created date if available
                    if (team.CreatedAt != default)
                    {
                        teamsListBuilder.AppendLine($"   Created: {team.CreatedAt:yyyy-MM-dd}");
                    }
                    
                    // Add a separator line between teams
                    if (i < result.Teams.Count - 1)
                    {
                        teamsListBuilder.AppendLine();
                    }
                }
                
                // Generate a prompt template
                Dictionary<string, string> templateValues = new()
                {
                    { "count", result.Count.ToString() },
                    { "searchTerm", !string.IsNullOrEmpty(name) ? name : "(any)" },
                    { "organizationId", !string.IsNullOrEmpty(userContext.OrganizationId) ? userContext.OrganizationId : "(any)" },
                    { "teamsList", teamsListBuilder.ToString().Trim() }
                };
                
                // Use different template for empty results
                if (result.Teams.Count == 0)
                {
                    result.PromptTemplate = _promptTemplateService.GetPrompt("TeamSearchResultsEmpty", templateValues);
                }
                else
                {
                    result.PromptTemplate = _promptTemplateService.GetPrompt("TeamSearchResults", templateValues);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for teams with name: {Name}", name);
                throw new ApplicationException($"Failed to search for teams: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get team details by ID
        /// </summary>
        [KernelFunction]
        [Description("Get details of a specific team by ID")]
        public async Task<TeamDetailsResponse> GetTeamDetailsAsync(
            [Description("ID of the team to retrieve")] string teamId)
        {
            try
            {
                // Get user context directly from the accessor
                var userContext = _userContextAccessor.CurrentUserContext;

                // Create a scope to resolve the scoped service
                using var scope = _serviceProvider.CreateScope();
                var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
                
                // Check read permission - use the exact permission name from Permissions class
                _logger.LogDebug("Checking permission {Permission}", "permission:teams_view");
                
                var unauthorizedResponse = await permissionService.CheckPermissionAsync<TeamDetailsResponse>(
                    Permissions.Teams_GetById,
                    "team", 
                    "GetDetails",
                    (message) => new TeamDetailsResponse 
                    { 
                        PromptTemplate = message,
                        TeamId = teamId
                    });
                
                // If unauthorized, return the response
                if (unauthorizedResponse != null)
                {
                    _logger.LogWarning("Permission denied for user {UserId} to view team details. Message: {Message}", 
                        userContext?.UserId, unauthorizedResponse.PromptTemplate);
                    return unauthorizedResponse;
                }
                  _logger.LogInformation("Permission check passed for user {UserId} to view team details", userContext?.UserId);
                
                // Create a scope to resolve the scoped service
                var teamMediatRService = scope.ServiceProvider.GetRequiredService<TeamMediatRService>();
                
                // Get the team details from the service
                var result = await teamMediatRService.GetTeamDetailsAsync(teamId);
                
                // Generate the members list for the prompt template
                var membersText = "None";
                if (result.Members != null && result.Members.Count > 0)
                {
                    membersText = string.Join(", ", result.Members.Select(m => $"{m.FirstName} {m.LastName}"));
                }
                
                // Generate a prompt template using the TeamDetails template
                Dictionary<string, string> templateValues = new()
                {
                    { "teamName", result.Name ?? "Unknown" },
                    { "description", result.Description ?? "No description provided" },
                    { "members", membersText }
                };
                
                result.PromptTemplate = _promptTemplateService.GetPrompt("TeamDetails", templateValues);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting team details for ID: {TeamId}", teamId);
                throw new ApplicationException($"Failed to get team details: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Update a team with new name, description, or manager
        /// </summary>
        [KernelFunction]
        [Description("Update an existing team's information")]
        public async Task<TeamUpdateResponse> UpdateTeamAsync(
            [Description("The ID of the team to update")] string teamId,
            [Description("The new name for the team (optional)")] string name = null,
            [Description("The new description for the team (optional)")] string description = null,
            [Description("The ID of the new team manager (optional)")] string teamManagerId = null,
            [Description("The name of the new team manager (optional)")] string teamManagerName = null)
        {
            try
            {
                // Get user context directly from the accessor
                var userContext = _userContextAccessor.CurrentUserContext;

                // Create a scope to resolve the scoped service
                using var scope = _serviceProvider.CreateScope();
                var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
                
                // Check update permission - use the exact permission name from Permissions class
                _logger.LogDebug("Checking permission {Permission}", "permission:teams_edit");
                
                var unauthorizedResponse = await permissionService.CheckPermissionAsync<TeamUpdateResponse>(
                    Permissions.Teams_Update,
                    "team", 
                    "update",
                    (message) => new TeamUpdateResponse 
                    { 
                        PromptTemplate = message,
                        TeamId = teamId
                    });
                
                // If unauthorized, return the response
                if (unauthorizedResponse != null)
                {
                    _logger.LogWarning("Permission denied for user {UserId} to update team. Message: {Message}", 
                        userContext?.UserId, unauthorizedResponse.PromptTemplate);
                    return unauthorizedResponse;
                }
                
                _logger.LogInformation("Permission check passed for user {UserId} to update team", userContext?.UserId);
                
                // If we don't have a team manager ID but have a name, try to find the user by name
                if (string.IsNullOrEmpty(teamManagerId) && !string.IsNullOrEmpty(teamManagerName))
                {
                    _logger.LogInformation("Team manager ID not provided, searching for manager by name: {ManagerName}", teamManagerName);
                    
                    try 
                    {
                        // Search for users with the given name
                        var searchResults = await _userPlugin.SearchUsersByNameAsync(teamManagerName);
                        
                        if (searchResults.Users.Count > 0)
                        {
                            // Take the first matching user - in a real app you might want more precise matching
                            teamManagerId = searchResults.Users[0].UserId;
                            _logger.LogInformation("Found team manager with ID {ManagerId} for name '{ManagerName}'", teamManagerId, teamManagerName);
                        }
                        else
                        {
                            _logger.LogWarning("No users found matching name '{ManagerName}'", teamManagerName);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error searching for team manager by name '{ManagerName}'", teamManagerName);
                        // We'll continue with null teamManagerId
                    }
                }

                // Create a scope to resolve the scoped service
                var teamMediatRService = scope.ServiceProvider.GetRequiredService<TeamMediatRService>();
                
                // If we have a name but no ID, try to search for the team by name
                if (!string.IsNullOrEmpty(name))
                {
                    _logger.LogInformation("No team ID provided, attempting to find team by name: {TeamName}", name);
                    
                    try
                    {
                        // Search for teams with this name
                        var searchResults = await SearchTeamsAsync(name);
                        
                        // If exactly one match is found, use that team's ID
                        if (searchResults.Teams.Count == 1)
                        {
                            teamId = searchResults.Teams[0].TeamId;                            
                            // Store the current name to check if we actually need to update the name
                            var currentName = searchResults.Teams[0].Name;
                            
                            // If the name parameter is being used just to find the team (not to rename it),
                            // and it matches the current name, don't try to update the name
                            if (name.Equals(currentName, StringComparison.OrdinalIgnoreCase))
                            {
                                name = null; // Clear name to avoid unnecessary update
                                _logger.LogDebug("Clearing name parameter as it's the same as current name");
                            }
                        }
                        // If multiple matches are found, we need more specifics
                        else if (searchResults.Teams.Count > 1)
                        {
                            throw new ArgumentException($"Found {searchResults.Teams.Count} teams named '{name}'. Please specify which one using the team ID.");
                        }
                        else
                        {
                            throw new ArgumentException($"No team found with name '{name}'.");
                        }
                    }
                    catch (Exception ex) when (!(ex is ArgumentException))
                    {
                        _logger.LogWarning(ex, "Error searching for team with name '{TeamName}'", name);
                        throw new ApplicationException($"Could not find team by name '{name}': {ex.Message}");
                    }
                }
                
                // At this point we must have a teamId
                if (string.IsNullOrEmpty(teamId))
                {
                    throw new ArgumentNullException(nameof(teamId), "Team ID is required to update a team.");
                }

                // Try to get the current team details first
                TeamDetailsResponse currentTeam;
                try
                {
                    _logger.LogDebug("Retrieving current team details for teamId: {TeamId}", teamId);
                    currentTeam = await teamMediatRService.GetTeamDetailsAsync(teamId);
                    _logger.LogDebug("Successfully retrieved team: {TeamName} with ID {TeamId}", 
                        currentTeam?.Name, currentTeam?.TeamId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to find team with ID '{TeamId}'", teamId);
                    throw new ApplicationException($"Team with ID '{teamId}' not found.", ex);
                }

                // Create the update request using current values as defaults
                var request = new TeamUpdateRequest
                {
                    Name = name ?? currentTeam.Name,
                    Description = description ?? currentTeam.Description,
                    OrganizationId = userContext.OrganizationId,
                    TeamManagerId = teamManagerId ?? currentTeam.TeamManagerId // Preserve existing TeamManagerId if no new one provided
                };

                // Call the MediatR service to update the team
                var result = await teamMediatRService.UpdateTeamAsync(teamId, request);
                _logger.LogInformation("Team updated successfully: {TeamId}", result.TeamId);
                
                // Generate a prompt template
                Dictionary<string, string> templateValues = new()
                {
                    { "teamName", result.Name },
                    { "teamId", result.TeamId }
                };

                if (!string.IsNullOrEmpty(request.Description))
                {
                    templateValues["description"] = request.Description;
                    result.PromptTemplate = _promptTemplateService.GetPrompt("TeamUpdatedWithDescription", templateValues);
                }
                else
                {
                    result.PromptTemplate = _promptTemplateService.GetPrompt("TeamUpdated", templateValues);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateTeamAsync");
                throw;
            }
        }

        /// <summary>
        /// Delete a team from the system
        /// </summary>
        [KernelFunction]
        [Description("Delete a team from the system, one of teamName or teamId is required")]
        public async Task<TeamDeleteResponse> DeleteTeamAsync(
            [Description("The ID of the team to delete")] string teamId,
            [Description("The name of the team to delete")] string teamName = null)
        {
            try
            {
                // Get user context directly from the accessor instead of creating a new scope
                var userContext = _userContextAccessor.CurrentUserContext;

                // Create a scope to resolve the scoped service
                using var scope = _serviceProvider.CreateScope();
                var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();

                // The UserContextAccessor will be used inside permissionService
                var unauthorizedResponse = await permissionService.CheckPermissionAsync<TeamDeleteResponse>(
                    Permissions.Teams_Delete,
                    "team", 
                    "delete",
                    (message) => new TeamDeleteResponse { PromptTemplate = message });
                
                // If unauthorized, return the response
                if (unauthorizedResponse != null)
                {
                    _logger.LogWarning("User {UserId} denied permission to delete team", userContext?.UserId);
                    return unauthorizedResponse;
                }
                
                
                // Use the TeamMediatRService from the same scope
                var teamMediatRService = scope.ServiceProvider.GetRequiredService<TeamMediatRService>();
                
                // If we have a name but no ID, try to search for the team by name
                if (string.IsNullOrEmpty(teamId) && !string.IsNullOrEmpty(teamName))
                {
                    _logger.LogInformation("No team ID provided, attempting to find team by name: {TeamName}", teamName);
                    
                    try
                    {
                        // Search for teams with this name
                        var searchResults = await SearchTeamsAsync(teamName);
                        
                        // If exactly one match is found, use that team's ID
                        if (searchResults.Teams.Count == 1)
                        {
                            teamId = searchResults.Teams[0].TeamId;
                            _logger.LogInformation("Found team ID {TeamId} for team named '{TeamName}'", teamId, teamName);
                        }
                        else if (searchResults.Teams.Count > 1)
                        {
                            throw new ArgumentException($"Found {searchResults.Teams.Count} teams named '{teamName}'. Please specify which one using the team ID.");
                        }
                        else
                        {
                            throw new ArgumentException($"No team found with name '{teamName}'.");
                        }
                    }
                    catch (Exception ex) when (!(ex is ArgumentException))
                    {
                        _logger.LogWarning(ex, "Error searching for team with name '{TeamName}'", teamName);
                        throw new ApplicationException($"Could not find team by name '{teamName}': {ex.Message}");
                    }
                }
                
                // At this point we must have a teamId
                if (string.IsNullOrEmpty(teamId))
                {
                    throw new ArgumentNullException(nameof(teamId), "Team ID is required to delete a team.");
                }

                // Delete the team and get the response
                var result = await teamMediatRService.DeleteTeamAsync(teamId);
                
                // Generate the prompt template for the response
                Dictionary<string, string> templateValues = new()
                {
                    { "teamName", result.Name },
                    { "teamId", result.TeamId }
                };
                
                result.PromptTemplate = _promptTemplateService.GetPrompt("TeamDeleted", templateValues);
                
                _logger.LogInformation("Team deleted successfully: {TeamId}, {TeamName}", result.TeamId, result.Name);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting team with ID '{TeamId}' or name '{TeamName}'", teamId, teamName);
                throw;
            }
        }

        /// <summary>
        /// Get all teams managed by a specific user
        /// </summary>
        [KernelFunction]
        [Description("Get all teams managed by a specific user")]
        public async Task<TeamsByManagerResponse> GetTeamsByManagerIdAsync(
            [Description("The ID of the manager whose teams to retrieve")] string managerId)
        {
            try
            {
                // Get user context directly from the accessor
                var userContext = _userContextAccessor.CurrentUserContext;

                // Create a scope to resolve the scoped service
                using var scope = _serviceProvider.CreateScope();
                var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
                
                // Check read permission - use the exact permission name from Permissions class
                _logger.LogDebug("Checking permission {Permission}", "permission:teams_view");
                
                var unauthorizedResponse = await permissionService.CheckPermissionAsync<TeamsByManagerResponse>(
                    Permissions.Teams_GetByManagerId, // Use exact permission name
                    "team", 
                    "TeamsByManager",
                    (message) => new TeamsByManagerResponse 
                    { 
                        PromptTemplate = message,
                        ManagerId = managerId
                    });
                
                // If unauthorized, return the response
                if (unauthorizedResponse != null)
                {
                    _logger.LogWarning("Permission denied for user {UserId} to view teams by manager. Message: {Message}", 
                        userContext?.UserId, unauthorizedResponse.PromptTemplate);
                    return unauthorizedResponse;
                }
                
                _logger.LogInformation("Permission check passed for user {UserId} to view teams by manager", userContext?.UserId);
                
                // Check if we have the required managerId
                if (string.IsNullOrEmpty(managerId))
                {
                    throw new ArgumentNullException(nameof(managerId), "Manager ID is required to get their teams.");
                }

                // Create a scope to resolve the scoped service
                var teamMediatRService = scope.ServiceProvider.GetRequiredService<TeamMediatRService>();

                // Get the teams for the manager
                var result = await teamMediatRService.GetTeamsByManagerIdAsync(managerId);
                
                // Generate the teams list for the prompt template
                var teamsListBuilder = new StringBuilder();
                
                // Only build the teams list if there are teams
                if (result.Teams.Count > 0)
                {
                    for (int i = 0; i < result.Teams.Count; i++)
                    {
                        var team = result.Teams[i];
                        teamsListBuilder.AppendLine($"{i+1}. {team.Name}");
                        if (!string.IsNullOrEmpty(team.Description))
                        {
                            teamsListBuilder.AppendLine($"   Description: {team.Description}");
                        }
                        teamsListBuilder.AppendLine($"   Members: {team.Members?.Count ?? 0}");
                    }
                }
                
                // Generate the prompt template
                Dictionary<string, string> templateValues = new()
                {
                    { "managerId", managerId },
                    { "count", result.Count.ToString() },
                    { "teamsList", teamsListBuilder.ToString().Trim() }
                };
                
                // Use different template for empty results
                if (result.Teams.Count == 0)
                {
                    result.PromptTemplate = _promptTemplateService.GetPrompt("TeamsByManagerResultsEmpty", templateValues);
                }
                else
                {
                    result.PromptTemplate = _promptTemplateService.GetPrompt("TeamsByManagerResults", templateValues);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving teams by manager ID: {ManagerId}", managerId);
                throw;
            }
        }

        /// <summary>
        /// Get all teams in a specific organization
        /// </summary>
        [KernelFunction]
        [Description("Get all teams in an organization, The organization ID is automatically provided from user context.")]
        public async Task<TeamsByOrganizationResponse> GetTeamsByOrganizationIdAsync()
        {
            try
            {
                // Get user context directly from the accessor
                var userContext = _userContextAccessor.CurrentUserContext;
                
                // Create a scope to resolve the scoped service
                using var scope = _serviceProvider.CreateScope();
                var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
                
                // Check read permission - use the exact permission name from Permissions class
                _logger.LogDebug("Checking permission {Permission}", "permission:teams_view");
                
                var unauthorizedResponse = await permissionService.CheckPermissionAsync<TeamsByOrganizationResponse>(
                    Permissions.Teams_GetByOrganizationId, // Use exact permission name
                    "team", 
                    "TeamsByOrganization",
                    (message) => new TeamsByOrganizationResponse 
                    { 
                        PromptTemplate = message,
                        OrganizationId = userContext.OrganizationId
                    });
                
                // If unauthorized, return the response
                if (unauthorizedResponse != null)
                {
                    _logger.LogWarning("Permission denied for user {UserId} to view teams by organization. Message: {Message}", 
                        userContext?.UserId, unauthorizedResponse.PromptTemplate);
                    return unauthorizedResponse;
                }
                
                _logger.LogInformation("Permission check passed for user {UserId} to view teams by organization", userContext?.UserId);
                
                // Check if we have the required organizationId
                if (string.IsNullOrEmpty(userContext.OrganizationId))
                {
                    throw new ArgumentNullException(nameof(userContext.OrganizationId), "Organization ID is required to get its teams.");
                }

                // Create a scope to resolve the scoped service
                var teamMediatRService = scope.ServiceProvider.GetRequiredService<TeamMediatRService>();

                // Get the teams for the organization
                var result = await teamMediatRService.GetTeamsByOrganizationIdAsync(userContext.OrganizationId);
                
                // Generate the teams list for the prompt template
                var teamsListBuilder = new StringBuilder();
                
                // Only build the teams list if there are teams
                for (int i = 0; i < result.Teams.Count; i++)
                {
                    var team = result.Teams[i];
                    teamsListBuilder.AppendLine($"{i+1}. {team.Name}");
                    if (!string.IsNullOrEmpty(team.Description))
                    {
                        teamsListBuilder.AppendLine($"   Description: {team.Description}");
                    }
                    teamsListBuilder.AppendLine($"   Members: {team.Members?.Count ?? 0}");
                }
               
                // Generate the prompt template
                Dictionary<string, string> templateValues = new()
                {
                    { "organizationId", userContext.OrganizationId },
                    { "count", result.Count.ToString() },
                    { "teamsList", teamsListBuilder.ToString().Trim() }
                };
                
                // Use different template for empty results
                if (result.Teams.Count == 0)
                {
                    result.PromptTemplate = _promptTemplateService.GetPrompt("TeamsByOrgResultsEmpty", templateValues);
                }
                else
                {
                    result.PromptTemplate = _promptTemplateService.GetPrompt("TeamsByOrgResults", templateValues);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving teams by organization ID");
                throw;
            }
        }
    }
}