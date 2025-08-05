using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using NXM.Tensai.Back.OKR.AI.Core.AI;
using NXM.Tensai.Back.OKR.AI.Models;
using NXM.Tensai.Back.OKR.AI.Services;
using NXM.Tensai.Back.OKR.AI.Services.MediatRService;
using NXM.Tensai.Back.OKR.Application;
using NXM.Tensai.Back.OKR.AI.Services.Authorization;
using NXM.Tensai.Back.OKR.Domain;

namespace NXM.Tensai.Back.OKR.AI.Core.AI.Plugins
{
    public class ObjectivePlugin
    {
        private readonly PromptTemplateService _promptTemplateService;
        private readonly IServiceProvider _serviceProvider;
        private readonly OkrSessionPlugin _okrSessionPlugin;
        private readonly TeamPlugin _teamPlugin;
        private readonly ILogger<ObjectivePlugin> _logger;
        private readonly UserContextAccessor _userContextAccessor;
        private const string PluginName = "ObjectiveManagement";
        
        public ObjectivePlugin(
            PromptTemplateService promptTemplateService,
            IServiceProvider serviceProvider,
            OkrSessionPlugin okrSessionPlugin,
            TeamPlugin teamPlugin,
            UserPlugin userPlugin,
            ILogger<ObjectivePlugin> logger,
            IConfiguration configuration,
            UserContextAccessor userContextAccessor)
        {
            _promptTemplateService = promptTemplateService;
            _serviceProvider = serviceProvider;
            _okrSessionPlugin = okrSessionPlugin;
            _teamPlugin = teamPlugin;
            _logger = logger;
            _userContextAccessor = userContextAccessor;
        }

        /// <summary>
        /// Search for objectives with optional filter criteria
        /// </summary>
        [KernelFunction]
        [Description("Search for objectives by title, session, team or user")]
        public async Task<ObjectiveSearchResponse> SearchObjectivesAsync(
            [Description("Optional title to filter objectives by")] string title = null,
            [Description("Optional OKR session ID to filter objectives by")] string okrSessionId = null,
            [Description("Optional OKR session title if OKR session ID is not available")] string okrSessionTitle = null,
            [Description("Optional team ID to filter objectives by")] string teamId = null,
            [Description("Optional team name if team ID is not available")] string teamName = null)
        {
            try
            {
                // Get user context directly from the accessor
                var userContext = _userContextAccessor.CurrentUserContext;

                // Create a scope to resolve the scoped service
                using var scope = _serviceProvider.CreateScope();
                var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
                
                // Check view permission
                _logger.LogDebug("Checking permission {Permission}", Permissions.Objectives_GetAll);
                
                var unauthorizedResponse = await permissionService.CheckPermissionAsync<ObjectiveSearchResponse>(
                    Permissions.Objectives_GetAll,
                    "objective", 
                    "search",
                    (message) => new ObjectiveSearchResponse 
                    { 
                        PromptTemplate = message,
                        SearchTerm = title
                    });
                
                // If unauthorized, return the response
                if (unauthorizedResponse != null)
                {
                    _logger.LogWarning("Permission denied for user {UserId} to search objectives. Message: {Message}", 
                        userContext?.UserId, unauthorizedResponse.PromptTemplate);
                    return unauthorizedResponse;
                }
                
                _logger.LogInformation("Permission check passed for user {UserId} to search objectives", userContext?.UserId);
                
                _logger.LogInformation("Searching for objectives with title: {Title}, sessionId: {SessionId}, teamId: {TeamId}, userId: {UserId}", 
                    title, okrSessionId, teamId, userContext?.UserId);
                
                // If we have an OKR session title but no ID, try to search for the session by title
                if (string.IsNullOrEmpty(okrSessionId) && !string.IsNullOrEmpty(okrSessionTitle))
                {
                    _logger.LogInformation("No OKR session ID provided, attempting to find session by title: {Title}", okrSessionTitle);
                    try
                    {
                        var sessionSearchResults = await _okrSessionPlugin.SearchOkrSessionsAsync(okrSessionTitle);
                        if (sessionSearchResults.Sessions.Count == 1)
                        {
                            okrSessionId = sessionSearchResults.Sessions[0].OkrSessionId;
                            _logger.LogInformation("Found OKR session ID {OkrSessionId} for session titled '{Title}'", okrSessionId, okrSessionTitle);
                        }
                        else if (sessionSearchResults.Sessions.Count > 1)
                        {
                            _logger.LogWarning("Found multiple OKR sessions with title '{Title}'. Using the first one.", okrSessionTitle);
                            okrSessionId = sessionSearchResults.Sessions[0].OkrSessionId;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error searching for OKR session with title '{Title}'", okrSessionTitle);
                    }
                }

                // If we have a team name but no ID, try to search for the team by name
                if (string.IsNullOrEmpty(teamId) && !string.IsNullOrEmpty(teamName))
                {
                    _logger.LogInformation("No team ID provided, attempting to find team by name: {TeamName}", teamName);
                    try
                    {
                        var teamSearchResults = await _teamPlugin.SearchTeamsAsync(teamName);
                        if (teamSearchResults.Teams.Count == 1)
                        {
                            teamId = teamSearchResults.Teams[0].TeamId;
                            _logger.LogInformation("Found team ID {TeamId} for team named '{TeamName}'", teamId, teamName);
                        }
                        else if (teamSearchResults.Teams.Count > 1)
                        {
                            _logger.LogWarning("Found multiple teams with name '{TeamName}'. Using the first one.", teamName);
                            teamId = teamSearchResults.Teams[0].TeamId;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error searching for team with name '{TeamName}'", teamName);
                    }
                }
                    
                // Create a scope to resolve the scoped service
                var objectiveMediatRService = scope.ServiceProvider.GetRequiredService<ObjectiveMediatRService>();
                
                // Use the MediatR service to search for objectives
                var result = await objectiveMediatRService.SearchObjectivesAsync(title, okrSessionId, teamId, userContext?.UserId);
                
                // Generate the objectives list for the prompt template with more detailed information
                var objectivesListBuilder = new StringBuilder();
                for (int i = 0; i < result.Objectives.Count; i++)
                {
                    var objective = result.Objectives[i];
                    objectivesListBuilder.AppendLine($"{i+1}. {objective.Title} (ID: {objective.ObjectiveId})");
                    
                    // Add description if available
                    if (!string.IsNullOrEmpty(objective.Description))
                    {
                        objectivesListBuilder.AppendLine($"   Description: {objective.Description}");
                    }
                    
                    // Add OKR session information
                    if (!string.IsNullOrEmpty(objective.OKRSessionTitle))
                    {
                        objectivesListBuilder.AppendLine($"   OKR Session: {objective.OKRSessionTitle}");
                    }
                    
                    // Add date range
                    objectivesListBuilder.AppendLine($"   Period: {objective.StartedDate:yyyy-MM-dd} to {objective.EndDate:yyyy-MM-dd}");
                    
                    // Add team, status, and progress
                    objectivesListBuilder.AppendLine($"   Team: {objective.ResponsibleTeamName ?? "Not assigned"}");
                    objectivesListBuilder.AppendLine($"   Status: {objective.Status}, Priority: {objective.Priority}, Progress: {objective.Progress}%");
                    
                    // Add a separator between objectives
                    if (i < result.Objectives.Count - 1)
                    {
                        objectivesListBuilder.AppendLine();
                    }
                }
                
                // Generate a prompt template
                Dictionary<string, string> templateValues = new()
                {
                    { "count", result.Count.ToString() },
                    { "searchTerm", !string.IsNullOrEmpty(title) ? title : "(any)" },
                    { "objectivesList", objectivesListBuilder.ToString().Trim() }
                };
                
                // Use different template for empty results
                if (result.Objectives.Count == 0)
                {
                    result.PromptTemplate = _promptTemplateService.GetPrompt("ObjectiveSearchResultsEmpty", templateValues);
                }
                else
                {
                    result.PromptTemplate = _promptTemplateService.GetPrompt("ObjectiveSearchResults", templateValues);
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for objectives with title: {Title}", title);
                throw new ApplicationException($"Failed to search for objectives: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Create a new objective with the provided details
        /// </summary>
        [KernelFunction]
        [Description("Create a new objective with the given title, description, dates, and assignments")]
        public async Task<ObjectiveCreationResponse> CreateObjectiveAsync(
            [Description("The title for the new objective")] string title,
            [Description("OKR session ID that this objective belongs to")] string okrSessionId,
            [Description("OKR session title if the ID is not available")] string okrSessionTitle = null,
            [Description("Start date of the objective (format: yyyy-MM-dd)")] string startDate = null,
            [Description("End date of the objective (format: yyyy-MM-dd)")] string endDate = null,
            [Description("The ID of the responsible team for this objective")] string responsibleTeamId = null,
            [Description("The name of the responsible team if ID is not available")] string responsibleTeamName = null,
            [Description("Optional description of the objective")] string description = null,
            [Description("Optional status for the objective (NotStarted, InProgress, Completed, Cancelled)")] string status = null,
            [Description("Optional priority for the objective (Low, Medium, High, Critical)")] string priority = null,
            [Description("Initial progress percentage (0-100)")] int? progress = null)
        {
            try
            {
                // Get user context directly from the accessor
                var userContext = _userContextAccessor.CurrentUserContext;

                // Create a scope to resolve the scoped service
                using var scope = _serviceProvider.CreateScope();
                var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
                
                // Check create permission
                _logger.LogDebug("Checking permission {Permission}", Permissions.Objectives_Create);
                
                var unauthorizedResponse = await permissionService.CheckPermissionAsync<ObjectiveCreationResponse>(
                    Permissions.Objectives_Create,
                    "objective", 
                    "create",
                    (message) => new ObjectiveCreationResponse 
                    { 
                        PromptTemplate = message,
                        Title = title
                    });
                
                // If unauthorized, return the response
                if (unauthorizedResponse != null)
                {
                    _logger.LogWarning("Permission denied for user {UserId} to create objective. Message: {Message}", 
                        userContext?.UserId, unauthorizedResponse.PromptTemplate);
                    return unauthorizedResponse;
                }
                
                _logger.LogInformation("Permission check passed for user {UserId} to create objective", userContext?.UserId);

                // If we have an OKR session title but no ID, try to search for the session by title
                if (string.IsNullOrEmpty(okrSessionId) && !string.IsNullOrEmpty(okrSessionTitle))
                {
                    _logger.LogInformation("No OKR session ID provided, attempting to find session by title: {Title}", okrSessionTitle);
                    try
                    {
                        var sessionSearchResults = await _okrSessionPlugin.SearchOkrSessionsAsync(okrSessionTitle);
                        if (sessionSearchResults.Sessions.Count == 1)
                        {
                            okrSessionId = sessionSearchResults.Sessions[0].OkrSessionId;
                            _logger.LogInformation("Found OKR session ID {OkrSessionId} for session titled '{Title}'", okrSessionId, okrSessionTitle);
                        }
                        else if (sessionSearchResults.Sessions.Count > 1)
                        {
                            throw new ArgumentException($"Found {sessionSearchResults.Sessions.Count} OKR sessions titled '{okrSessionTitle}'. Please specify which one using the OKR session ID.");
                        }
                        else
                        {
                            throw new ArgumentException($"No OKR session found with title '{okrSessionTitle}'.");
                        }
                    }
                    catch (Exception ex) when (!(ex is ArgumentException))
                    {
                        _logger.LogWarning(ex, "Error searching for OKR session with title '{Title}'", okrSessionTitle);
                        throw new ApplicationException($"Could not find OKR session by title '{okrSessionTitle}': {ex.Message}");
                    }
                }

                if (string.IsNullOrEmpty(okrSessionId))
                {
                    throw new ArgumentException("An OKR session ID is required to create an objective.");
                }

                // If we have a team name but no ID, try to search for the team by name
                if (string.IsNullOrEmpty(responsibleTeamId) && !string.IsNullOrEmpty(responsibleTeamName))
                {
                    _logger.LogInformation("No team ID provided, attempting to find team by name: {TeamName}", responsibleTeamName);
                    try
                    {
                        var teamSearchResults = await _teamPlugin.SearchTeamsAsync(responsibleTeamName);
                        if (teamSearchResults.Teams.Count == 1)
                        {
                            responsibleTeamId = teamSearchResults.Teams[0].TeamId;
                            _logger.LogInformation("Found team ID {TeamId} for team named '{TeamName}'", responsibleTeamId, responsibleTeamName);
                        }
                        else if (teamSearchResults.Teams.Count > 1)
                        {
                            throw new ArgumentException($"Found {teamSearchResults.Teams.Count} teams named '{responsibleTeamName}'. Please specify which one using the team ID.");
                        }
                        else
                        {
                            throw new ArgumentException($"No team found with name '{responsibleTeamName}'.");
                        }
                    }
                    catch (Exception ex) when (!(ex is ArgumentException))
                    {
                        _logger.LogWarning(ex, "Error searching for team with name '{TeamName}'", responsibleTeamName);
                        throw new ApplicationException($"Could not find team by name '{responsibleTeamName}': {ex.Message}");
                    }
                }

                if (string.IsNullOrEmpty(responsibleTeamId))
                {
                    throw new ArgumentException("A responsible team ID is required to create an objective.");
                }

                // Parse dates
                DateTime startDateTime;
                DateTime endDateTime;

                // If start date is not provided, use today's date
                if (string.IsNullOrEmpty(startDate))
                {
                    startDateTime = DateTime.UtcNow.Date;
                }
                else if (!DateTime.TryParse(startDate, out startDateTime))
                {
                    throw new ArgumentException("Start date must be in a valid format (yyyy-MM-dd)");
                }

                // If end date is not provided, try to get OKR session end date
                if (string.IsNullOrEmpty(endDate))
                {
                    try
                    {
                        var session = await _okrSessionPlugin.GetOkrSessionDetailsAsync(okrSessionId);
                        endDateTime = session.EndDate;
                    }
                    catch (Exception)
                    {
                        // Default to 3 months from start date if session lookup fails
                        endDateTime = startDateTime.AddMonths(3);
                    }
                }
                else if (!DateTime.TryParse(endDate, out endDateTime))
                {
                    throw new ArgumentException("End date must be in a valid format (yyyy-MM-dd)");
                }

                // Ensure dates are in UTC format
                startDateTime = EnsureUtc(startDateTime);
                endDateTime = EnsureUtc(endDateTime);

                if (startDateTime > endDateTime)
                {
                    throw new ArgumentException("Start date must be before end date");
                }

                // Create a scope to resolve the scoped service
                var objectiveMediatRService = scope.ServiceProvider.GetRequiredService<ObjectiveMediatRService>();

                // Create a request object for the MediatR service
                var request = new ObjectiveCreationRequest
                {
                    Title = title,
                    Description = description,
                    OKRSessionId = okrSessionId,
                    ResponsibleTeamId = responsibleTeamId,
                    UserId = userContext?.UserId,
                    StartedDate = startDateTime,
                    EndDate = endDateTime,
                    Status = status ?? "NotStarted",
                    Priority = priority ?? "Medium",
                    Progress = progress ?? 0
                };
                
                _logger.LogInformation("Creating objective: {Title} for OKR session: {OkrSessionId}", title, okrSessionId);
                
                // Call the MediatR service to handle the objective creation
                var result = await objectiveMediatRService.CreateObjectiveAsync(request);
                
                _logger.LogInformation("Objective created successfully: {ObjectiveId}, {Title}", result.ObjectiveId, result.Title);

                // Create prompt template values
                Dictionary<string, string> templateValues = new()
                {
                    { "title", result.Title },
                    { "description", result.Description},
                    { "startedDate", result.StartedDate.ToString("yyyy-MM-dd")},
                    { "endDate", result.EndDate.ToString("yyyy-MM-dd")},
                    { "teamName", responsibleTeamName ?? "Not assigned"}
                };

                // Add description if provided
                if (!string.IsNullOrEmpty(description))
                {
                    templateValues["description"] = description;
                    result.PromptTemplate = _promptTemplateService.GetPrompt("ObjectiveCreatedWithDescription", templateValues);
                }
                else
                {
                    result.PromptTemplate = _promptTemplateService.GetPrompt("ObjectiveCreated", templateValues);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating objective '{Title}'", title);
                throw;
            }
        }
        
        /// <summary>
        /// Update an existing objective
        /// </summary>
        [KernelFunction]
        [Description("Update an existing objective's information")]
        public async Task<ObjectiveUpdateResponse> UpdateObjectiveAsync(
            [Description("The ID of the objective to update")] string objectiveId,
            [Description("The title of the objective to update if ID is not available")] string title = null,
            [Description("The new title for the objective (optional)")] string newTitle = null,
            [Description("The new description for the objective (optional)")] string description = null,
            [Description("New start date of the objective (format: yyyy-MM-dd)")] string startDate = null,
            [Description("New end date of the objective (format: yyyy-MM-dd)")] string endDate = null,
            [Description("The ID of the new responsible team (optional)")] string responsibleTeamId = null,
            [Description("The name of the new responsible team (optional)")] string responsibleTeamName = null,
            [Description("New status for the objective (NotStarted, InProgress, Completed, Cancelled)")] string status = null,
            [Description("New priority for the objective (Low, Medium, High, Critical)")] string priority = null,
            [Description("Updated progress percentage (0-100)")] int? progress = null,
            [Description("The ID of the OKR session for this objective (optional)")] string okrSessionId = null)
        {
            try
            {
                // Get user context directly from the accessor
                var userContext = _userContextAccessor.CurrentUserContext;

                // Create a scope to resolve the scoped service
                using var scope = _serviceProvider.CreateScope();
                var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
                
                // Check update permission
                _logger.LogDebug("Checking permission {Permission}", Permissions.Objectives_Update);
                
                var unauthorizedResponse = await permissionService.CheckPermissionAsync<ObjectiveUpdateResponse>(
                    Permissions.Objectives_Update,
                    "objective", 
                    "update",
                    (message) => new ObjectiveUpdateResponse 
                    { 
                        PromptTemplate = message,
                        ObjectiveId = objectiveId
                    });
                
                // If unauthorized, return the response
                if (unauthorizedResponse != null)
                {
                    _logger.LogWarning("Permission denied for user {UserId} to update objective. Message: {Message}", 
                        userContext?.UserId, unauthorizedResponse.PromptTemplate);
                    return unauthorizedResponse;
                }
                
                _logger.LogInformation("Permission check passed for user {UserId} to update objective", userContext?.UserId);
                
                _logger.LogDebug("UpdateObjectiveAsync called with objectiveId: '{ObjectiveId}', title: '{Title}', okrSessionId: '{OkrSessionId}', userId: '{UserId}'", 
                    objectiveId, title, okrSessionId, userContext?.UserId);
                
                var objectiveMediatRService = scope.ServiceProvider.GetRequiredService<ObjectiveMediatRService>();

                // If we have a title but no ID, try to search for the objective by title
                if (string.IsNullOrEmpty(objectiveId) && !string.IsNullOrEmpty(title))
                {
                    _logger.LogInformation("No objective ID provided, attempting to find objective by title: {Title}", title);
                    
                    try
                    {
                        // Search for objectives with this title
                        var searchResults = await objectiveMediatRService.SearchObjectivesAsync(title);
                        
                        // If exactly one match is found, use that objective's ID
                        if (searchResults.Objectives.Count == 1)
                        {
                            objectiveId = searchResults.Objectives[0].ObjectiveId;
                            _logger.LogInformation("Found objective ID {ObjectiveId} for objective titled '{Title}'", objectiveId, title);
                            
                            // Store the current title to check if we actually need to update it
                            var currentTitle = searchResults.Objectives[0].Title;
                            
                            // If the title parameter is being used just to find the objective (not to rename it),
                            // and it matches the current title, don't try to update the title unless newTitle is provided
                            if (title.Equals(currentTitle, StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(newTitle))
                            {
                                newTitle = null; // Clear newTitle to avoid unnecessary update
                                _logger.LogDebug("Title parameter used only for lookup, not for update");
                            }
                        }
                        // If multiple matches are found, we need more specifics
                        else if (searchResults.Objectives.Count > 1)
                        {
                            throw new ArgumentException($"Found {searchResults.Objectives.Count} objectives titled '{title}'. Please specify which one using the objective ID.");
                        }
                        else
                        {
                            throw new ArgumentException($"No objective found with title '{title}'.");
                        }
                    }
                    catch (Exception ex) when (!(ex is ArgumentException))
                    {
                        _logger.LogWarning(ex, "Error searching for objective with title '{Title}'", title);
                        throw new ApplicationException($"Could not find objective by title '{title}': {ex.Message}");
                    }
                }
                
                // At this point we must have an objectiveId
                if (string.IsNullOrEmpty(objectiveId))
                {
                    throw new ArgumentNullException(nameof(objectiveId), "Objective ID is required to update an objective.");
                }

                // Try to get the current objective details first
                ObjectiveDetailsResponse currentObjective;
                try
                {
                    _logger.LogDebug("Retrieving current objective details for objectiveId: {ObjectiveId}", objectiveId);
                    currentObjective = await objectiveMediatRService.GetObjectiveDetailsAsync(objectiveId);
                    _logger.LogDebug("Successfully retrieved objective: {Title} with ID {ObjectiveId}", 
                        currentObjective?.Title, currentObjective?.ObjectiveId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to find objective with ID '{ObjectiveId}'", objectiveId);
                    throw new ApplicationException($"Objective with ID '{objectiveId}' not found.", ex);
                }

                // If okrSessionId is not provided, use the existing one
                if (string.IsNullOrEmpty(okrSessionId))
                {
                    okrSessionId = currentObjective.OKRSessionId;
                    _logger.LogInformation("Using existing OKR session ID: {OkrSessionId}", okrSessionId);
                }

                // If responsibleTeamId is not provided but responsibleTeamName is, try to look up the team
                if (string.IsNullOrEmpty(responsibleTeamId) && !string.IsNullOrEmpty(responsibleTeamName))
                {
                    try
                    {
                        var teamSearchResults = await _teamPlugin.SearchTeamsAsync(responsibleTeamName);
                        if (teamSearchResults.Teams.Count == 1)
                        {
                            responsibleTeamId = teamSearchResults.Teams[0].TeamId;
                            _logger.LogInformation("Found team ID {TeamId} for team named '{TeamName}'", responsibleTeamId, responsibleTeamName);
                        }
                        else if (teamSearchResults.Teams.Count > 1)
                        {
                            _logger.LogWarning("Found multiple teams with name '{TeamName}'. Using the first one.", responsibleTeamName);
                            responsibleTeamId = teamSearchResults.Teams[0].TeamId;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error searching for team with name '{TeamName}'", responsibleTeamName);
                    }
                }

                // Parse dates if provided
                DateTime? startDateTime = null;
                DateTime? endDateTime = null;

                if (!string.IsNullOrEmpty(startDate))
                {
                    if (!DateTime.TryParse(startDate, out var parsedStartDate))
                    {
                        throw new ArgumentException("Start date must be in a valid format (yyyy-MM-dd)");
                    }
                    startDateTime = EnsureUtc(parsedStartDate);
                }

                if (!string.IsNullOrEmpty(endDate))
                {
                    if (!DateTime.TryParse(endDate, out var parsedEndDate))
                    {
                        throw new ArgumentException("End date must be in a valid format (yyyy-MM-dd)");
                    }
                    endDateTime = EnsureUtc(parsedEndDate);
                }

                // Validate that start date is before end date if both are provided
                if (startDateTime.HasValue && endDateTime.HasValue && startDateTime.Value > endDateTime.Value)
                {
                    throw new ArgumentException("Start date must be before end date");
                }

                // Create the update request using current values as defaults
                var request = new ObjectiveUpdateRequest
                {
                    Title = newTitle ?? title ?? currentObjective.Title,
                    Description = description ?? currentObjective.Description,
                    ResponsibleTeamId = responsibleTeamId ?? currentObjective.ResponsibleTeamId,
                    StartedDate = startDateTime ?? currentObjective.StartedDate,
                    EndDate = endDateTime ?? currentObjective.EndDate,
                    Status = status ?? currentObjective.Status,
                    Priority = priority ?? currentObjective.Priority,
                    Progress = progress,
                    OKRSessionId = okrSessionId,
                    UserId = userContext?.UserId ?? currentObjective.UserId
                };

                _logger.LogDebug("Updating objective {ObjectiveId} with values: Title={Title}, Description={Description}, StartDate={StartDate}, EndDate={EndDate}, OkrSessionId={OkrSessionId}", 
                    objectiveId, request.Title, request.Description, request.StartedDate, request.EndDate, request.OKRSessionId);
                
                // Call the MediatR service to update the objective
                var result = await objectiveMediatRService.UpdateObjectiveAsync(objectiveId, request);
                _logger.LogInformation("Objective updated successfully: {ObjectiveId}", result.ObjectiveId);
                
                // Generate a prompt template
                Dictionary<string, string> templateValues = new()
                {
                    { "title", result.Title },
                    { "objectiveId", result.ObjectiveId },
                    { "description", request.Description ?? "No description provided" }
                };

                try
                {
                    if (!string.IsNullOrEmpty(request.Description))
                    {
                        result.PromptTemplate = _promptTemplateService.GetPrompt("ObjectiveUpdatedWithDescription", templateValues);
                    }
                    else
                    {
                        result.PromptTemplate = _promptTemplateService.GetPrompt("ObjectiveUpdated", templateValues);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to find prompt template for objective update. Using generic message instead.");
                    if (!string.IsNullOrEmpty(request.Description))
                    {
                        result.PromptTemplate = $"I've updated the objective '{result.Title}' with the new description: '{request.Description}'.";
                    }
                    else
                    {
                        result.PromptTemplate = $"I've updated the objective '{result.Title}' successfully.";
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateObjectiveAsync");
                throw;
            }
        }

        /// <summary>
        /// Delete an objective
        /// </summary>
        [KernelFunction]
        [Description("Delete an objective from the system")]
        public async Task<ObjectiveDeleteResponse> DeleteObjectiveAsync(
            [Description("The ID of the objective to delete")] string objectiveId,
            [Description("The title of the objective to delete if ID is not available")] string title = null)
        {
            try
            {
                // Get user context directly from the accessor
                var userContext = _userContextAccessor.CurrentUserContext;

                // Create a scope to resolve the scoped service
                using var scope = _serviceProvider.CreateScope();
                var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
                
                // Check delete permission
                _logger.LogDebug("Checking permission {Permission}", Permissions.Objectives_Delete);
                
                var unauthorizedResponse = await permissionService.CheckPermissionAsync<ObjectiveDeleteResponse>(
                    Permissions.Objectives_Delete,
                    "objective", 
                    "delete",
                    (message) => new ObjectiveDeleteResponse 
                    { 
                        PromptTemplate = message,
                        ObjectiveId = objectiveId
                    });
                
                // If unauthorized, return the response
                if (unauthorizedResponse != null)
                {
                    _logger.LogWarning("Permission denied for user {UserId} to delete objective. Message: {Message}", 
                        userContext?.UserId, unauthorizedResponse.PromptTemplate);
                    return unauthorizedResponse;
                }
                
                _logger.LogInformation("Permission check passed for user {UserId} to delete objective", userContext?.UserId);
                
                _logger.LogDebug("DeleteObjectiveAsync called with objectiveId: '{ObjectiveId}', title: '{Title}'", objectiveId, title);
                
                var objectiveMediatRService = scope.ServiceProvider.GetRequiredService<ObjectiveMediatRService>();

                // If we have a title but no ID, try to search for the objective by title
                if (string.IsNullOrEmpty(objectiveId) && !string.IsNullOrEmpty(title))
                {
                    _logger.LogInformation("No objective ID provided, attempting to find objective by title: {Title}", title);
                    
                    try
                    {
                        // Search for objectives with this title
                        var searchResults = await objectiveMediatRService.SearchObjectivesAsync(title);
                        
                        // If exactly one match is found, use that objective's ID
                        if (searchResults.Objectives.Count == 1)
                        {
                            objectiveId = searchResults.Objectives[0].ObjectiveId;
                            _logger.LogInformation("Found objective ID {ObjectiveId} for objective titled '{Title}'", objectiveId, title);
                        }
                        // If multiple matches are found, we need more specifics
                        else if (searchResults.Objectives.Count > 1)
                        {
                            throw new ArgumentException($"Found {searchResults.Objectives.Count} objectives titled '{title}'. Please specify which one using the objective ID.");
                        }
                        else
                        {
                            throw new ArgumentException($"No objective found with title '{title}'.");
                        }
                    }
                    catch (Exception ex) when (!(ex is ArgumentException))
                    {
                        _logger.LogWarning(ex, "Error searching for objective with title '{Title}'", title);
                        throw new ApplicationException($"Could not find objective by title '{title}': {ex.Message}");
                    }
                }
                
                // At this point we must have an objectiveId
                if (string.IsNullOrEmpty(objectiveId))
                {
                    throw new ArgumentNullException(nameof(objectiveId), "Objective ID is required to delete an objective.");
                }

                // Delete the objective and get the response
                var result = await objectiveMediatRService.DeleteObjectiveAsync(objectiveId, userContext?.UserId);
                
                // Generate the prompt template for the response
                Dictionary<string, string> templateValues = new()
                {
                    { "title", result.Title },
                    { "objectiveId", result.ObjectiveId }
                };
                
                result.PromptTemplate = _promptTemplateService.GetPrompt("ObjectiveDeleted", templateValues);
                
                _logger.LogInformation("Objective deleted successfully: {ObjectiveId}, {Title}", result.ObjectiveId, result.Title);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting objective with ID '{ObjectiveId}' or title '{Title}'", objectiveId, title);
                throw;
            }
        }

        /// <summary>
        /// Get details of a specific objective
        /// </summary>
        [KernelFunction]
        [Description("Get details of a specific objective by ID or title")]
        public async Task<ObjectiveDetailsResponse> GetObjectiveDetailsAsync(
            [Description("ID of the objective to retrieve")] string objectiveId,
            [Description("Title of the objective if ID is not available")] string title = null)
        {
            try
            {
                // Get user context directly from the accessor
                var userContext = _userContextAccessor.CurrentUserContext;

                // Create a scope to resolve the scoped service
                using var scope = _serviceProvider.CreateScope();
                var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
                
                // Check view permission
                _logger.LogDebug("Checking permission {Permission}", Permissions.Objectives_GetById);
                
                var unauthorizedResponse = await permissionService.CheckPermissionAsync<ObjectiveDetailsResponse>(
                    Permissions.Objectives_GetById,
                    "objective", 
                    "view",
                    (message) => new ObjectiveDetailsResponse 
                    { 
                        PromptTemplate = message,
                        ObjectiveId = objectiveId
                    });
                
                // If unauthorized, return the response
                if (unauthorizedResponse != null)
                {
                    _logger.LogWarning("Permission denied for user {UserId} to view objective details. Message: {Message}", 
                        userContext?.UserId, unauthorizedResponse.PromptTemplate);
                    return unauthorizedResponse;
                }
                
                _logger.LogInformation("Permission check passed for user {UserId} to view objective details", userContext?.UserId);
                
                _logger.LogInformation("Getting details for objective with ID: {ObjectiveId} or title: {Title}", objectiveId, title);
                
                var objectiveMediatRService = scope.ServiceProvider.GetRequiredService<ObjectiveMediatRService>();

                // If we have a title but no ID, try to search for the objective by title
                if (string.IsNullOrEmpty(objectiveId) && !string.IsNullOrEmpty(title))
                {
                    _logger.LogInformation("No objective ID provided, attempting to find objective by title: {Title}", title);
                    
                    try
                    {
                        // Search for objectives with this title
                        var searchResults = await objectiveMediatRService.SearchObjectivesAsync(title);
                        
                        // If exactly one match is found, use that objective's ID
                        if (searchResults.Objectives.Count == 1)
                        {
                            objectiveId = searchResults.Objectives[0].ObjectiveId;
                            _logger.LogInformation("Found objective ID {ObjectiveId} for objective titled '{Title}'", objectiveId, title);
                        }
                        // If multiple matches are found, we need more specifics
                        else if (searchResults.Objectives.Count > 1)
                        {
                            throw new ArgumentException($"Found {searchResults.Objectives.Count} objectives titled '{title}'. Please specify which one using the objective ID.");
                        }
                        else
                        {
                            throw new ArgumentException($"No objective found with title '{title}'.");
                        }
                    }
                    catch (Exception ex) when (!(ex is ArgumentException))
                    {
                        _logger.LogWarning(ex, "Error searching for objective with title '{Title}'", title);
                        throw new ApplicationException($"Could not find objective by title '{title}': {ex.Message}");
                    }
                }
                
                // At this point we must have an objectiveId
                if (string.IsNullOrEmpty(objectiveId))
                {
                    throw new ArgumentNullException(nameof(objectiveId), "Objective ID is required to get objective details.");
                }
                
                var result = await objectiveMediatRService.GetObjectiveDetailsAsync(objectiveId);
                
                // Generate the prompt template for the response
                Dictionary<string, string> templateValues = new()
                {
                    { "title", result.Title },
                    { "objectiveId", result.ObjectiveId },
                    { "description", result.Description },
                    { "okrSessionTitle", result.OKRSessionTitle ?? "Unknown Session" },
                    { "startDate", result.StartedDate.ToString("yyyy-MM-dd") },
                    { "endDate", result.EndDate.ToString("yyyy-MM-dd") },
                    { "status", result.Status },
                    { "priority", result.Priority },
                    { "progress", result.Progress.ToString() },
                    { "teamName", result.ResponsibleTeamName ?? "Not assigned" }
                };
                
                result.PromptTemplate = _promptTemplateService.GetPrompt("ObjectiveDetails", templateValues);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting objective details for ID: {ObjectiveId} or title: {Title}", objectiveId, title);
                throw new ApplicationException($"Failed to get objective details: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get all objectives by OKR session ID or title
        /// </summary>
        [KernelFunction]
        [Description("Get all objectives for a specific OKR session")]
        public async Task<ObjectivesBySessionResponse> GetObjectivesBySessionIdAsync(
            [Description("The ID of the OKR session to get objectives for")] string okrSessionId,
            [Description("The title of the OKR session if ID is not available")] string okrSessionTitle = null)
        {
            try
            {
                // Get user context directly from the accessor
                var userContext = _userContextAccessor.CurrentUserContext;

                // Create a scope to resolve the scoped service
                using var scope = _serviceProvider.CreateScope();
                var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
                
                // Check view permission
                _logger.LogDebug("Checking permission {Permission}", Permissions.Objectives_GetById);
                
                var unauthorizedResponse = await permissionService.CheckPermissionAsync<ObjectivesBySessionResponse>(
                    Permissions.Objectives_GetById,
                    "objective", 
                    "view by session",
                    (message) => new ObjectivesBySessionResponse 
                    { 
                        PromptTemplate = message,
                        OKRSessionId = okrSessionId
                    });
                
                // If unauthorized, return the response
                if (unauthorizedResponse != null)
                {
                    _logger.LogWarning("Permission denied for user {UserId} to view objectives by session. Message: {Message}", 
                        userContext?.UserId, unauthorizedResponse.PromptTemplate);
                    return unauthorizedResponse;
                }
                
                _logger.LogInformation("Permission check passed for user {UserId} to view objectives by session", userContext?.UserId);
                
                _logger.LogInformation("Getting objectives for OKR session with ID: {OkrSessionId} or title: {OkrSessionTitle}", 
                    okrSessionId, okrSessionTitle);
                
                var objectiveMediatRService = scope.ServiceProvider.GetRequiredService<ObjectiveMediatRService>();

                // If we have a session title but no ID, try to search for the session by title
                if (string.IsNullOrEmpty(okrSessionId) && !string.IsNullOrEmpty(okrSessionTitle))
                {
                    _logger.LogInformation("No OKR session ID provided, attempting to find session by title: {Title}", okrSessionTitle);
                    
                    try
                    {
                        var sessionSearchResults = await _okrSessionPlugin.SearchOkrSessionsAsync(okrSessionTitle);
                        if (sessionSearchResults.Sessions.Count == 1)
                        {
                            okrSessionId = sessionSearchResults.Sessions[0].OkrSessionId;
                            _logger.LogInformation("Found OKR session ID {OkrSessionId} for session titled '{Title}'", 
                                okrSessionId, okrSessionTitle);
                        }
                        else if (sessionSearchResults.Sessions.Count > 1)
                        {
                            throw new ArgumentException($"Found {sessionSearchResults.Sessions.Count} OKR sessions titled '{okrSessionTitle}'. Please specify which one using the OKR session ID.");
                        }
                        else
                        {
                            throw new ArgumentException($"No OKR session found with title '{okrSessionTitle}'.");
                        }
                    }
                    catch (Exception ex) when (!(ex is ArgumentException))
                    {
                        _logger.LogWarning(ex, "Error searching for OKR session with title '{Title}'", okrSessionTitle);
                        throw new ApplicationException($"Could not find OKR session by title '{okrSessionTitle}': {ex.Message}");
                    }
                }
                
                // At this point we must have an okrSessionId
                if (string.IsNullOrEmpty(okrSessionId))
                {
                    throw new ArgumentNullException(nameof(okrSessionId), "OKR Session ID is required to get objectives for a session.");
                }
                
                // Get the objectives for the OKR session
                var result = await objectiveMediatRService.GetObjectivesBySessionIdAsync(okrSessionId);
                
                // Generate the objectives list for the prompt template
                var objectivesListBuilder = new StringBuilder();
                
                // Only build the objectives list if there are objectives
                if (result.Objectives.Count > 0)
                {
                    for (int i = 0; i < result.Objectives.Count; i++)
                    {
                        var objective = result.Objectives[i];
                        objectivesListBuilder.AppendLine($"{i+1}. {objective.Title} (ID: {objective.ObjectiveId})");
                        if (!string.IsNullOrEmpty(objective.Description))
                        {
                            objectivesListBuilder.AppendLine($"   Description: {objective.Description}");
                        }
                        objectivesListBuilder.AppendLine($"   Period: {objective.StartedDate:yyyy-MM-dd} to {objective.EndDate:yyyy-MM-dd}");
                        objectivesListBuilder.AppendLine($"   Team: {objective.ResponsibleTeamName ?? "Not assigned"}");
                        objectivesListBuilder.AppendLine($"   Status: {objective.Status}, Priority: {objective.Priority}, Progress: {objective.Progress}%");
                        
                        // Add a separator line between objectives
                        if (i < result.Objectives.Count - 1)
                        {
                            objectivesListBuilder.AppendLine();
                        }
                    }
                }
                
                // Generate the prompt template
                Dictionary<string, string> templateValues = new()
                {
                    { "okrSessionTitle", result.OKRSessionTitle },
                    { "count", result.Count.ToString() },
                    { "objectivesList", objectivesListBuilder.ToString().Trim() }
                };
                
                // Use different template for empty results or fallback to direct messages if templates are missing
                if (result.Objectives.Count == 0)
                {
                    string fallbackEmptyMessage = $"There are no objectives for the OKR session '{result.OKRSessionTitle}'. Would you like to create one?";
                    try {
                        result.PromptTemplate = SafeGetPrompt("ObjectivesBySessionResultsEmpty", templateValues, fallbackEmptyMessage);
                    } catch (Exception ex) {
                        _logger.LogWarning(ex, "Failed to get prompt template, using direct message");
                        result.PromptTemplate = fallbackEmptyMessage;
                    }
                }
                else
                {
                    string objectivesList = objectivesListBuilder.ToString().Trim();
                    string fallbackMessage = $"I found {result.Objectives.Count} objectives for the OKR session '{result.OKRSessionTitle}':\n\n{objectivesList}";
                    try {
                        result.PromptTemplate = SafeGetPrompt("ObjectivesBySessionResults", templateValues, fallbackMessage);
                    } catch (Exception ex) {
                        _logger.LogWarning(ex, "Failed to get prompt template, using direct message");
                        result.PromptTemplate = fallbackMessage;
                    }
                }
 
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving objectives by OKR session ID: {OkrSessionId} or title: {OkrSessionTitle}", 
                    okrSessionId, okrSessionTitle);
                throw;
            }
        }
        
        /// <summary>
        /// Get all objectives without any filtering
        /// </summary>
        [KernelFunction]
        [Description("Get all objectives in the system")]
        public async Task<ObjectiveSearchResponse> GetAllObjectivesAsync()
        {
            try
            {
                // Get user context directly from the accessor
                var userContext = _userContextAccessor.CurrentUserContext;
                
                _logger.LogInformation("ObjectivePlugin.GetAllObjectivesAsync called - User: {UserId}, Role: {UserRole}", 
                    userContext?.UserId, userContext?.Role);

                // Create a scope to resolve the scoped service
                using var scope = _serviceProvider.CreateScope();
                var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
                
                // Check view permission
                _logger.LogDebug("Checking permission {Permission}", Permissions.Objectives_GetAll);
                
                var unauthorizedResponse = await permissionService.CheckPermissionAsync<ObjectiveSearchResponse>(
                    Permissions.Objectives_GetAll,
                    "objective", 
                    "view all",
                    (message) => new ObjectiveSearchResponse 
                    { 
                        PromptTemplate = message,
                        SearchTerm = "all"
                    });
                
                // If unauthorized, return the response
                if (unauthorizedResponse != null)
                {
                    _logger.LogWarning("Permission denied for user {UserId} to view all objectives. Message: {Message}", 
                        userContext?.UserId, unauthorizedResponse.PromptTemplate);
                    return unauthorizedResponse;
                }
                
                _logger.LogInformation("Permission check passed for user {UserId} to view all objectives", userContext?.UserId);
                
                _logger.LogInformation("Getting all objectives");
                
                var objectiveMediatRService = scope.ServiceProvider.GetRequiredService<ObjectiveMediatRService>();
                
                // Use the search functionality but without any filters to get all objectives
                var result = await objectiveMediatRService.SearchObjectivesAsync(
                    title: null, 
                    okrSessionId: null, 
                    teamId: null, 
                    userId: userContext?.UserId);
                
                // Generate the objectives list for the prompt template with more detailed information
                var objectivesListBuilder = new StringBuilder();
                for (int i = 0; i < result.Objectives.Count; i++)
                {
                    var objective = result.Objectives[i];
                    objectivesListBuilder.AppendLine($"{i+1}. {objective.Title} (ID: {objective.ObjectiveId})");
                    
                    // Add description if available
                    if (!string.IsNullOrEmpty(objective.Description))
                    {
                        objectivesListBuilder.AppendLine($"   Description: {objective.Description}");
                    }
                    
                    // Add OKR session information
                    if (!string.IsNullOrEmpty(objective.OKRSessionTitle))
                    {
                        objectivesListBuilder.AppendLine($"   OKR Session: {objective.OKRSessionTitle}");
                    }
                    
                    // Add date range
                    objectivesListBuilder.AppendLine($"   Period: {objective.StartedDate:yyyy-MM-dd} to {objective.EndDate:yyyy-MM-dd}");
                    
                    // Add team, status, and progress
                    objectivesListBuilder.AppendLine($"   Team: {objective.ResponsibleTeamName ?? "Not assigned"}");
                    objectivesListBuilder.AppendLine($"   Status: {objective.Status}, Priority: {objective.Priority}, Progress: {objective.Progress}%");
                    
                    // Add a separator between objectives
                    if (i < result.Objectives.Count - 1)
                    {
                        objectivesListBuilder.AppendLine();
                    }
                }
                
                // Create a clear message for all objectives listing
                string allObjectivesList = objectivesListBuilder.ToString().Trim();
                string objectivesMessage;
                
                if (result.Objectives.Count == 0)
                {
                    objectivesMessage = "I couldn't find any objectives. Would you like to create a new objective?";
                }
                else 
                {
                    objectivesMessage = $"Here are all {result.Objectives.Count} objectives:\n\n{allObjectivesList}";
                }
                
                // Use our custom message directly
                result.PromptTemplate = objectivesMessage;
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all objectives");
                throw new ApplicationException($"Failed to get all objectives: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Helper method to ensure DateTime values are in UTC format
        /// </summary>
        private DateTime EnsureUtc(DateTime dateTime)
        {
            // If the Kind is already UTC, return it as is
            if (dateTime.Kind == DateTimeKind.Utc)
            {
                return dateTime;
            }
            
            // If the Kind is Local, convert to UTC
            if (dateTime.Kind == DateTimeKind.Local)
            {
                return dateTime.ToUniversalTime();
            }
            
            // If the Kind is Unspecified, specify it as UTC
            // This assumes the time is already in UTC but just not marked as such
            return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
        }
        
        /// <summary>
        /// Helper method to safely get a prompt template with a fallback message
        /// </summary>
        private string SafeGetPrompt(string templateKey, Dictionary<string, string> values, string fallbackMessage)
        {
            try
            {
                return _promptTemplateService.GetPrompt(templateKey, values);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to find prompt template '{TemplateKey}'. Using fallback message.", templateKey);
                return fallbackMessage;
            }
        }
    }
}