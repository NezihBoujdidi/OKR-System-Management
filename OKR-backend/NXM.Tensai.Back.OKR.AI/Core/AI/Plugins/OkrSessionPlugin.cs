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
    public class OkrSessionPlugin
    {
        private readonly PromptTemplateService _promptTemplateService;
        private readonly IServiceProvider _serviceProvider;
        private readonly TeamPlugin _teamPlugin;
        private readonly UserPlugin _userPlugin;
        private readonly ILogger<OkrSessionPlugin> _logger;
        private readonly UserContextAccessor _userContextAccessor;
        private const string PluginName = "OkrSessionManagement";

        public OkrSessionPlugin(
            PromptTemplateService promptTemplateService,
            IServiceProvider serviceProvider,
            TeamPlugin teamPlugin,
            UserPlugin userPlugin,
            ILogger<OkrSessionPlugin> logger,
            IConfiguration configuration,
            UserContextAccessor userContextAccessor)
        {
            _promptTemplateService = promptTemplateService;
            _serviceProvider = serviceProvider;
            _teamPlugin = teamPlugin;
            _userPlugin = userPlugin;
            _logger = logger;
            _userContextAccessor = userContextAccessor;
        }

        /// <summary>
        /// Search for OKR sessions with optional title filter
        /// </summary>
        [KernelFunction]
        [Description("Search for OKR sessions by title")]
        public async Task<OkrSessionSearchResponse> SearchOkrSessionsAsync(
            [Description("Optional title to filter OKR sessions by")] string title = null)
        {
            try
            {
                // Get user context directly from the accessor
                var userContext = _userContextAccessor.CurrentUserContext;

                // Create a scope to resolve the scoped service
                using var scope = _serviceProvider.CreateScope();
                var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
                
                
                var unauthorizedResponse = await permissionService.CheckPermissionAsync<OkrSessionSearchResponse>(
                    Permissions.OKRSessions_GetAll,
                    "OKR session", 
                    "search",
                    (message) => new OkrSessionSearchResponse 
                    { 
                        PromptTemplate = message,
                        SearchTerm = title
                    });
                
                // If unauthorized, return the response
                if (unauthorizedResponse != null)
                {
                    _logger.LogWarning("Permission denied for user {UserId} to search OKR sessions. Message: {Message}", 
                        userContext?.UserId, unauthorizedResponse.PromptTemplate);
                    return unauthorizedResponse;
                }
                
                // Create a scope to resolve the scoped service
                var okrSessionMediatRService = scope.ServiceProvider.GetRequiredService<OkrSessionMediatRService>();
                    
                // Use the MediatR service to search for OKR sessions
                var result = await okrSessionMediatRService.SearchOkrSessionsAsync(title, userContext.UserId);
                
                // Generate the sessions list for the prompt template with more detailed information
                var sessionsListBuilder = new StringBuilder();
                for (int i = 0; i < result.Sessions.Count; i++)
                {
                    var session = result.Sessions[i];
                    sessionsListBuilder.AppendLine($"{i+1}. {session.Title}");
                    
                    // Add description if available
                    if (!string.IsNullOrEmpty(session.Description))
                    {
                        sessionsListBuilder.AppendLine($"   Description: {session.Description}");
                    }
                    
                    // Add date range
                    sessionsListBuilder.AppendLine($"   Period: {session.StartDate:yyyy-MM-dd} to {session.EndDate:yyyy-MM-dd}");
                    
                    // Add status and progress if available
                    sessionsListBuilder.AppendLine($"   Status: {session.Status}, Progress: {session.Progress}%");
                    
                    // Add a separator line between sessions
                    if (i < result.Sessions.Count - 1)
                    {
                        sessionsListBuilder.AppendLine();
                    }
                }
                
                // Generate a prompt template
                Dictionary<string, string> templateValues = new()
                {
                    { "count", result.Count.ToString() },
                    { "searchTerm", !string.IsNullOrEmpty(title) ? title : "(any)" },
                    { "sessionsList", sessionsListBuilder.ToString().Trim() }
                };
                
                // Use different template for empty results
                if (result.Sessions.Count == 0)
                {
                    result.PromptTemplate = _promptTemplateService.GetPrompt("OkrSessionSearchResultsEmpty", templateValues);
                }
                else
                {
                    result.PromptTemplate = _promptTemplateService.GetPrompt("OkrSessionSearchResults", templateValues);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for OKR sessions with title: {Title}", title);
                throw new ApplicationException($"Failed to search for OKR sessions: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Create an OKR session with the provided details
        /// </summary>
        [KernelFunction]
        [Description("Create a new OKR session with the given title, description, dates, and team assignments")]
        public async Task<OkrSessionCreationResponse> CreateOkrSessionAsync(
            [Description("The title for the new OKR session")] string title,
            [Description("Start date of the OKR session (format: yyyy-MM-dd)")] string startDate,
            [Description("End date of the OKR session (format: yyyy-MM-dd)")] string endDate,
            //[Description("The ID of the team manager for this OKR session. If not an ID, it will be treated as a name")] string teamManager,
            //[Description("The name of the team manager if ID is not available (optional, only used as fallback)")] string teamManagerName = null,
            [Description("Optional description of the OKR session")] string description = null,
            [Description("Comma-separated list of team IDs to include in this session")] string teamIds = null,
            [Description("Optional color for the OKR session")] string color = null,
            [Description("Optional status for the OKR session (NotStarted, InProgress , Completed, Overdue)")] string status = null)
        {
            try
            {
                // Get user context directly from the accessor
                var userContext = _userContextAccessor.CurrentUserContext;

                // Create a scope to resolve the scoped service
                using var scope = _serviceProvider.CreateScope();
                var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
                
                var unauthorizedResponse = await permissionService.CheckPermissionAsync<OkrSessionCreationResponse>(
                    Permissions.OKRSessions_Create,
                    "OKR session", 
                    "create",
                    (message) => new OkrSessionCreationResponse 
                    { 
                        PromptTemplate = message,
                        Title = title
                    });
                
                // If unauthorized, return the response
                if (unauthorizedResponse != null)
                {
                    _logger.LogWarning("Permission denied for user {UserId} to create OKR session. Message: {Message}", 
                        userContext?.UserId, unauthorizedResponse.PromptTemplate);
                    return unauthorizedResponse;
                }

                // Parse dates
                DateTime startDateTime;
                DateTime endDateTime;

                if (!DateTime.TryParse(startDate, out startDateTime))
                {
                    throw new ArgumentException("Start date must be in a valid format (yyyy-MM-dd)");
                }

                if (!DateTime.TryParse(endDate, out endDateTime))
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

                //string teamManagerId = null;
                
                // ENHANCED LOGIC FOR TEAM MANAGER IDENTIFICATION:
                // First check if primary teamManager parameter is a valid ID or a name
                /* if (!string.IsNullOrEmpty(teamManager))
                {
                    // Check if teamManager looks like a GUID (team manager ID)
                    if (Guid.TryParse(teamManager, out _))
                    {
                        _logger.LogInformation("Team manager parameter is a valid GUID: {TeamManagerId}", teamManager);
                        teamManagerId = teamManager;
                    }
                    // If not a GUID, treat it as a name and look up the ID
                    else
                    {
                        _logger.LogInformation("Team manager parameter is not a GUID, treating as a name: {TeamManagerName}", teamManager);
                        teamManagerId = await FindUserByNameWithFallbacks(teamManager);
                        
                        if (!string.IsNullOrEmpty(teamManagerId))
                        {
                            _logger.LogInformation("Successfully resolved team manager name '{TeamManagerName}' to ID: {TeamManagerId}", 
                                teamManager, teamManagerId);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to resolve team manager name: {TeamManagerName}", teamManager);
                        }
                    }
                }
                
                // If teamManagerId is still not resolved, try with the explicit teamManagerName if provided
                if (string.IsNullOrEmpty(teamManagerId) && !string.IsNullOrEmpty(teamManagerName))
                {
                    _logger.LogInformation("Team manager ID not resolved from primary parameter, trying with explicit teamManagerName: {TeamManagerName}", 
                        teamManagerName);
                    teamManagerId = await FindUserByNameWithFallbacks(teamManagerName);
                }
                
                // If still no ID, use the current user ID as fallback
                if (string.IsNullOrEmpty(teamManagerId))
                {
                        _logger.LogWarning("No valid team manager found. Using the current user (ID: {UserId}) as team manager.", userContext?.UserId);
                        teamManagerId = userContext?.UserId;
                }
                
                _logger.LogInformation("Final team manager ID for OKR session: {TeamManagerId}", teamManagerId);
 */
                // Parse team IDs if provided
                List<string> teamIdsList = new List<string>();
                if (!string.IsNullOrEmpty(teamIds))
                {
                    teamIdsList = teamIds.Split(',').Select(id => id.Trim()).ToList();
                }

                // Create a scope to resolve the scoped service
                var okrSessionMediatRService = scope.ServiceProvider.GetRequiredService<OkrSessionMediatRService>();

                // Create a request object for the MediatR service
                var request = new OkrSessionCreationRequest
                {
                    Title = title,
                    OrganizationId = userContext?.OrganizationId,
                    Description = description,
                    StartedDate = startDateTime,
                    EndDate = endDateTime,
                    //TeamManagerId = teamManagerId,
                    TeamIds = teamIdsList,
                    UserId = userContext?.UserId,
                    Color = color ?? "#4285F4", // Default to a blue color
                    Status = status ?? "NotStarted" // Default status
                };
                
                /* _logger.LogInformation("Creating OKR session with TeamManagerId: {TeamManagerId}, UserId: {UserId}", 
                    teamManagerId, userContext?.UserId); */
                
                // Call the MediatR service to handle the OKR session creation
                var result = await okrSessionMediatRService.CreateOkrSessionAsync(request);
                
                _logger.LogInformation("OKR session created successfully: {OkrSessionId}, {Title}", result.OkrSessionId, result.Title);

                // Create prompt template values
                Dictionary<string, string> templateValues = new()
                {
                    { "title", result.Title },
                    { "startDate", startDateTime.ToString("yyyy-MM-dd") },
                    { "endDate", endDateTime.ToString("yyyy-MM-dd") }
                };

                // Add description if provided
                if (!string.IsNullOrEmpty(description))
                {
                    templateValues["description"] = description;
                    result.PromptTemplate = _promptTemplateService.GetPrompt("OkrSessionCreatedWithDescription", templateValues);
                }
                else
                {
                    result.PromptTemplate = _promptTemplateService.GetPrompt("OkrSessionCreated", templateValues);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating OKR session '{Title}'", title);
                throw;
            }
        }

        /// <summary>
        /// Update an existing OKR session
        /// </summary>
        [KernelFunction]
        [Description("Update an existing OKR session's information")]
        public async Task<OkrSessionUpdateResponse> UpdateOkrSessionAsync(
            [Description("The ID of the OKR session to update")] string okrSessionId,
            [Description("The title of the OKR session to update if ID is not available")] string title = null,
            [Description("The new title for the OKR session (optional)")] string newTitle = null,
            [Description("The new description for the OKR session (optional)")] string description = null,
            [Description("New start date of the OKR session (format: yyyy-MM-dd)")] string startDate = null,
            [Description("New end date of the OKR session (format: yyyy-MM-dd)")] string endDate = null,
            //[Description("The ID of the new team manager (optional)")] string teamManagerId = null,
            //[Description("The name of the new team manager (optional)")] string teamManagerName = null,
            [Description("New color for the OKR session (optional)")] string color = null,

            [Description("New status for the OKR session (NotStarted, InProgress, Completed, Overdue)")] string status = null)
        {
            try
            {
                // Get user context directly from the accessor
                var userContext = _userContextAccessor.CurrentUserContext;

                // Create a scope to resolve the scoped service
                using var scope = _serviceProvider.CreateScope();
                var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
                
                // Check update permission
                _logger.LogDebug("Checking permission {Permission}", Permissions.OKRSessions_Update);
                
                var unauthorizedResponse = await permissionService.CheckPermissionAsync<OkrSessionUpdateResponse>(
                    Permissions.OKRSessions_Update,
                    "OKR session", 
                    "update",
                    (message) => new OkrSessionUpdateResponse 
                    { 
                        PromptTemplate = message,
                        OkrSessionId = okrSessionId
                    });
                
                // If unauthorized, return the response
                if (unauthorizedResponse != null)
                {
                    _logger.LogWarning("Permission denied for user {UserId} to update OKR session. Message: {Message}", 
                        userContext?.UserId, unauthorizedResponse.PromptTemplate);
                    return unauthorizedResponse;
                }
                
                _logger.LogInformation("Permission check passed for user {UserId} to update OKR session", userContext?.UserId);
                
                _logger.LogDebug("UpdateOkrSessionAsync called with okrSessionId: '{OkrSessionId}', title: '{Title}'", okrSessionId, title);
                
                var okrSessionMediatRService = scope.ServiceProvider.GetRequiredService<OkrSessionMediatRService>();

                // If we have a title but no ID, try to search for the OKR session by title
                if (string.IsNullOrEmpty(okrSessionId) && !string.IsNullOrEmpty(title))
                {
                    _logger.LogInformation("No OKR session ID provided, attempting to find session by title: {Title}", title);
                    
                    try
                    {
                        // Search for OKR sessions with the ORIGINAL title, not newTitle
                        var searchResults = await okrSessionMediatRService.SearchOkrSessionsAsync(title);
                        _logger.LogInformation("Found {Count} OKR sessions matching title '{Title}'", 
                            searchResults.Sessions.Count, title);
                        
                        // If exactly one match is found, use that session's ID
                        if (searchResults.Sessions.Count == 1)
                        {
                            okrSessionId = searchResults.Sessions[0].OkrSessionId;
                            _logger.LogInformation("Found OKR session ID {OkrSessionId} for session titled '{Title}'", okrSessionId, title);
                        }
                        // If multiple matches are found, we need more specifics
                        else if (searchResults.Sessions.Count > 1)
                        {
                            throw new ArgumentException($"Found {searchResults.Sessions.Count} OKR sessions titled '{title}'. Please specify which one using the OKR session ID.");
                        }
                        else
                        {
                            throw new ArgumentException($"No OKR session found with title '{title}'.");
                        }
                    }
                    catch (Exception ex) when (!(ex is ArgumentException))
                    {
                        _logger.LogWarning(ex, "Error searching for OKR session with title '{Title}'", title);
                        throw new ApplicationException($"Could not find OKR session by title '{title}': {ex.Message}");
                    }
                }
                
                // At this point we must have an okrSessionId
                if (string.IsNullOrEmpty(okrSessionId))
                {
                    throw new ArgumentNullException(nameof(okrSessionId), "OKR Session ID is required to update a session.");
                }

                // Try to get the current OKR session details first
                OkrSessionDetailsResponse currentSession;
                try
                {
                    _logger.LogDebug("Retrieving current OKR session details for okrSessionId: {OkrSessionId}", okrSessionId);
                    currentSession = await okrSessionMediatRService.GetOkrSessionDetailsAsync(okrSessionId);
                    _logger.LogDebug("Successfully retrieved OKR session: {Title} with ID {OkrSessionId}", 
                        currentSession?.Title, currentSession?.OkrSessionId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to find OKR session with ID '{OkrSessionId}'", okrSessionId);
                    throw new ApplicationException($"OKR session with ID '{okrSessionId}' not found.", ex);
                }

                // If teamManagerId is not provided but teamManagerName is, try to look up the manager
                /* if (string.IsNullOrEmpty(teamManagerId) && !string.IsNullOrEmpty(teamManagerName))
                {
                    teamManagerId = await FindUserByNameWithFallbacks(teamManagerName);
                } */

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

                // Handle the title correctly:
                // - If newTitle is provided, use that as the new title
                // - Otherwise, if the original title was only used for lookup and not intended as an update, keep the current title
                string finalTitle = newTitle;
                if (string.IsNullOrEmpty(finalTitle))
                {
                    // When no newTitle is provided, if title value matches the current value, 
                    // it means it was used only for lookup, not for update
                    if (string.IsNullOrEmpty(title) || title.Equals(currentSession.Title, StringComparison.OrdinalIgnoreCase))
                    {
                        finalTitle = currentSession.Title; // Keep current title
                    }
                    else
                    {
                        finalTitle = title; // Use title as the new value
                    }
                }
                
                // Validate the Status value - if invalid, keep the current status
                string finalStatus = status;
                if (!string.IsNullOrEmpty(finalStatus))
                {
                    try 
                    {
                        // Check if the status value is valid by attempting to parse it
                        var validStatus = Enum.Parse<Domain.Status>(finalStatus, true);
                        finalStatus = validStatus.ToString(); // Use the properly cased version
                    }
                    catch (ArgumentException ex)
                    {
                        _logger.LogWarning(ex, "Invalid status value '{Status}' provided. Using current status '{CurrentStatus}' instead.", 
                            status, currentSession.Status);
                        finalStatus = currentSession.Status;
                    }
                }

                // Create the update request using current values as defaults
                var request = new OkrSessionUpdateRequest
                {
                    Title = finalTitle,
                    Description = description ?? currentSession.Description,
                    StartedDate = startDateTime ?? currentSession.StartDate,
                    EndDate = endDateTime ?? currentSession.EndDate,
                    //TeamManagerId = teamManagerId ?? currentSession.TeamManagerId,
                    UserId = userContext?.UserId ?? currentSession.CreatedBy,
                    Color = color ?? currentSession.Color,
                    Status = finalStatus ?? currentSession.Status
                };

                _logger.LogDebug("Updating OKR session {OkrSessionId} with values: Title={Title}, Description={Description}, StartDate={StartDate}, EndDate={EndDate}, Color={Color}, Status={Status}", 
                    okrSessionId, request.Title, request.Description, request.StartedDate, request.EndDate, request.Color, request.Status);
                
                // Call the MediatR service to update the OKR session
                var result = await okrSessionMediatRService.UpdateOkrSessionAsync(okrSessionId, request);
                _logger.LogInformation("OKR session updated successfully: {OkrSessionId}, updated from '{OldTitle}' to '{NewTitle}'", 
                    result.OkrSessionId, currentSession.Title, result.Title);
                
                // Generate a prompt template
                Dictionary<string, string> templateValues = new()
                {
                    { "title", result.Title }
                };

                if (!string.IsNullOrEmpty(request.Description))
                {
                    templateValues["description"] = request.Description;
                    result.PromptTemplate = _promptTemplateService.GetPrompt("OkrSessionUpdatedWithDescription", templateValues);
                }
                else
                {
                    result.PromptTemplate = _promptTemplateService.GetPrompt("OkrSessionUpdated", templateValues);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateOkrSessionAsync");
                throw;
            }
        }

        /// <summary>
    /// Delete an OKR session
    /// </summary>
    [KernelFunction]
    [Description("Delete an OKR session from the system")]
    public async Task<OkrSessionDeleteResponse> DeleteOkrSessionAsync(
        [Description("The ID of the OKR session to delete")] string okrSessionId,
        [Description("The title of the OKR session to delete if ID is not available")] string title = null)
    {
        try
        {
                // Get user context directly from the accessor
                var userContext = _userContextAccessor.CurrentUserContext;

                // Create a scope to resolve the scoped service
                using var scope = _serviceProvider.CreateScope();
                var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
                
                // Check delete permission
                _logger.LogDebug("Checking permission {Permission}", Permissions.OKRSessions_Delete);
                
                var unauthorizedResponse = await permissionService.CheckPermissionAsync<OkrSessionDeleteResponse>(
                    Permissions.OKRSessions_Delete,
                    "OKR session", 
                    "delete",
                    (message) => new OkrSessionDeleteResponse 
                    { 
                        PromptTemplate = message,
                        OkrSessionId = okrSessionId
                    });
                
                // If unauthorized, return the response
                if (unauthorizedResponse != null)
                {
                    _logger.LogWarning("Permission denied for user {UserId} to delete OKR session. Message: {Message}", 
                        userContext?.UserId, unauthorizedResponse.PromptTemplate);
                    return unauthorizedResponse;
                }
                
                _logger.LogInformation("Permission check passed for user {UserId} to delete OKR session", userContext?.UserId);
                
                _logger.LogDebug("DeleteOkrSessionAsync called with okrSessionId: '{OkrSessionId}', title: '{Title}'", okrSessionId, title);
                
                var okrSessionMediatRService = scope.ServiceProvider.GetRequiredService<OkrSessionMediatRService>();


                // If we have a title but no ID, try to search for the OKR session by title

            if (string.IsNullOrEmpty(okrSessionId) && !string.IsNullOrEmpty(title))
            {
                _logger.LogInformation("No OKR session ID provided, attempting to find session by title: {Title}", title);

                try
                {
                    var searchResults = await okrSessionMediatRService.SearchOkrSessionsAsync(title);

                    if (searchResults.Sessions.Count == 1)
                    {
                        okrSessionId = searchResults.Sessions[0].OkrSessionId;
                        _logger.LogInformation("Found OKR session ID {OkrSessionId} for session titled '{Title}'", okrSessionId, title);
                    }
                    else if (searchResults.Sessions.Count > 1)
                    {
                        throw new ArgumentException($"Found {searchResults.Sessions.Count} OKR sessions titled '{title}'. Please specify the OKR session ID.");
                    }
                    else
                    {
                        throw new ArgumentException($"No OKR session found with title '{title}'.");
                    }
                }
                catch (Exception ex) when (ex is not ArgumentException)
                {
                    _logger.LogWarning(ex, "Error searching for OKR session with title '{Title}'", title);
                    throw new ApplicationException($"Could not find OKR session by title '{title}': {ex.Message}");
                }
            }

            if (string.IsNullOrEmpty(okrSessionId))
                throw new ArgumentNullException(nameof(okrSessionId), "OKR Session ID is required to delete a session.");

            var sessionDetails = await okrSessionMediatRService.GetOkrSessionDetailsAsync(okrSessionId);
            _logger.LogInformation("Found OKR session to delete: {OkrSessionId}, {Title}", sessionDetails.OkrSessionId, sessionDetails.Title);

            _logger.LogInformation("Attempting to delete OKR session: {OkrSessionId}, {Title}", okrSessionId, sessionDetails.Title);
            var result = await okrSessionMediatRService.DeleteOkrSessionAsync(okrSessionId);


            result.PromptTemplate = _promptTemplateService.GetPrompt("OkrSessionDeleted", new()
            {
                { "title", sessionDetails.Title }
            });      

            _logger.LogInformation("OKR session deleted successfully: {OkrSessionId}, {Title}", result.OkrSessionId, result.Title);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting OKR session with ID '{OkrSessionId}' or title '{Title}'", okrSessionId, title);
            throw;
        }
    }



        /// <summary>
        /// Get details of a specific OKR session
        /// </summary>
        [KernelFunction]
        [Description("Get details of a specific OKR session by ID or title")]
        public async Task<OkrSessionDetailsResponse> GetOkrSessionDetailsAsync(
            [Description("ID of the OKR session to retrieve")] string okrSessionId,
            [Description("Title of the OKR session if ID is not available")] string title = null)
        {
            try
            {
                // Get user context directly from the accessor
                var userContext = _userContextAccessor.CurrentUserContext;

                // Create a scope to resolve the scoped service
                using var scope = _serviceProvider.CreateScope();
                var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
                
                // Check view permission
                _logger.LogDebug("Checking permission {Permission}", Permissions.OKRSessions_GetById);
                
                var unauthorizedResponse = await permissionService.CheckPermissionAsync<OkrSessionDetailsResponse>(
                    Permissions.OKRSessions_GetById,
                    "OKR session", 
                    "view",
                    (message) => new OkrSessionDetailsResponse 
                    { 
                        PromptTemplate = message,
                        OkrSessionId = okrSessionId
                    });
                
                // If unauthorized, return the response
                if (unauthorizedResponse != null)
                {
                    _logger.LogWarning("Permission denied for user {UserId} to view OKR session details. Message: {Message}", 
                        userContext?.UserId, unauthorizedResponse.PromptTemplate);
                    return unauthorizedResponse;
                }
                
                _logger.LogInformation("Permission check passed for user {UserId} to view OKR session details", userContext?.UserId);
                
                _logger.LogInformation("Getting details for OKR session with ID: {OkrSessionId} or title: {Title}", okrSessionId, title);
                
                // Create a scope to resolve the scoped service
                var okrSessionMediatRService = scope.ServiceProvider.GetRequiredService<OkrSessionMediatRService>();
                
                // If we have a title but no ID, try to search for the OKR session by title
                if (string.IsNullOrEmpty(okrSessionId) && !string.IsNullOrEmpty(title))
                {
                    _logger.LogInformation("No OKR session ID provided, attempting to find session by title: {Title}", title);
                    
                    try
                    {
                        // Search for OKR sessions with this title, using MediatR service directly to avoid circular reference
                        var searchResults = await okrSessionMediatRService.SearchOkrSessionsAsync(title);
                        
                        // If exactly one match is found, use that session's ID
                        if (searchResults.Sessions.Count == 1)
                        {
                            okrSessionId = searchResults.Sessions[0].OkrSessionId;
                            _logger.LogInformation("Found OKR session ID {OkrSessionId} for session titled '{Title}'", okrSessionId, title);
                        }
                        // If multiple matches are found, we need more specifics
                        else if (searchResults.Sessions.Count > 1)
                        {
                            throw new ArgumentException($"Found {searchResults.Sessions.Count} OKR sessions titled '{title}'. Please specify which one using the OKR session ID.");
                        }
                        else
                        {
                            throw new ArgumentException($"No OKR session found with title '{title}'.");
                        }
                    }
                    catch (Exception ex) when (!(ex is ArgumentException))
                    {
                        _logger.LogWarning(ex, "Error searching for OKR session with title '{Title}'", title);
                        throw new ApplicationException($"Could not find OKR session by title '{title}': {ex.Message}");
                    }
                }
                
                // At this point we must have an okrSessionId
                if (string.IsNullOrEmpty(okrSessionId))
                {
                    throw new ArgumentNullException(nameof(okrSessionId), "OKR Session ID is required to get session details.");
                }
                
                var result = await okrSessionMediatRService.GetOkrSessionDetailsAsync(okrSessionId);
                
                // Generate the prompt template for the response
                Dictionary<string, string> templateValues = new()
                {
                    { "title", result.Title },
                    { "description", result.Description },
                    { "startDate", result.StartDate.ToString("yyyy-MM-dd") },
                    { "endDate", result.EndDate.ToString("yyyy-MM-dd") },
                    { "status", result.Status },
                    { "progress", result.Progress.ToString() }//,
                    //{ "teamManagerName", result.TeamManagerName ?? "Unknown" }
                };
                
                result.PromptTemplate = _promptTemplateService.GetPrompt("OkrSessionDetails", templateValues);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OKR session details for ID: {OkrSessionId} or title: {Title}", okrSessionId, title);
                throw new ApplicationException($"Failed to get OKR session details: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get all OKR sessions associated with a team
        /// </summary>
        [KernelFunction]
        [Description("Get all OKR sessions associated with a specific team")]
        public async Task<OkrSessionsByTeamResponse> GetOkrSessionsByTeamIdAsync(
            [Description("The ID of the team whose OKR sessions to retrieve")] string teamId,
            [Description("The name of the team if ID is not available")] string teamName = null)
        {
            try
            {
                // Get user context directly from the accessor
                var userContext = _userContextAccessor.CurrentUserContext;

                // Create a scope to resolve the scoped service
                using var scope = _serviceProvider.CreateScope();
                var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
                
                // Check view permission
                _logger.LogDebug("Checking permission {Permission}", Permissions.OKRSessions_GetAll);
                
                var unauthorizedResponse = await permissionService.CheckPermissionAsync<OkrSessionsByTeamResponse>(
                    Permissions.OKRSessions_GetAll,
                    "OKR session", 
                    "view by team",
                    (message) => new OkrSessionsByTeamResponse 
                    { 
                        PromptTemplate = message,
                        TeamId = teamId,
                        TeamName = teamName
                    });
                
                // If unauthorized, return the response
                if (unauthorizedResponse != null)
                {
                    _logger.LogWarning("Permission denied for user {UserId} to view team OKR sessions. Message: {Message}", 
                        userContext?.UserId, unauthorizedResponse.PromptTemplate);
                    return unauthorizedResponse;
                }
                
                _logger.LogInformation("Permission check passed for user {UserId} to view team OKR sessions", userContext?.UserId);
                
                // If we have a team name but no ID, try to search for the team by name
                if (string.IsNullOrEmpty(teamId) && !string.IsNullOrEmpty(teamName))
                {
                    _logger.LogInformation("No team ID provided, attempting to find team by name: {TeamName}", teamName);
                    
                    try
                    {
                        // Search for teams with this name
                        var searchResults = await _teamPlugin.SearchTeamsAsync(teamName);
                        
                        // If exactly one match is found, use that team's ID
                        if (searchResults.Teams.Count == 1)
                        {
                            teamId = searchResults.Teams[0].TeamId;
                            _logger.LogInformation("Found team ID {TeamId} for team named '{TeamName}'", teamId, teamName);
                        }
                        // If multiple matches are found, we need more specifics
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
                    throw new ArgumentNullException(nameof(teamId), "Team ID is required to get OKR sessions for a team.");
                }

                var okrSessionMediatRService = scope.ServiceProvider.GetRequiredService<OkrSessionMediatRService>();
                
                // Get the OKR sessions for the team
                var result = await okrSessionMediatRService.GetOkrSessionsByTeamIdAsync(teamId);
                
                // Generate the sessions list for the prompt template
                var sessionsListBuilder = new StringBuilder();
                
                // Only build the sessions list if there are sessions
                if (result.Sessions.Count > 0)
                {
                    for (int i = 0; i < result.Sessions.Count; i++)
                    {
                        var session = result.Sessions[i];
                        sessionsListBuilder.AppendLine($"{i+1}. {session.Title} (ID: {session.OkrSessionId})");
                        if (!string.IsNullOrEmpty(session.Description))
                        {
                            sessionsListBuilder.AppendLine($"   Description: {session.Description}");
                        }
                        sessionsListBuilder.AppendLine($"   Period: {session.StartDate:yyyy-MM-dd} to {session.EndDate:yyyy-MM-dd}");
                        sessionsListBuilder.AppendLine($"   Status: {session.Status}, Progress: {session.Progress}%");
                        
                        // Add a separator line between sessions
                        if (i < result.Sessions.Count - 1)
                        {
                            sessionsListBuilder.AppendLine();
                        }
                    }
                }
                
                // Generate the prompt template
                Dictionary<string, string> templateValues = new()
                {
                    { "teamName", result.TeamName },
                    { "count", result.Count.ToString() },
                    { "sessionsList", sessionsListBuilder.ToString().Trim() }
                };
                
                // Use different template for empty results
                if (result.Sessions.Count == 0)
                {
                    result.PromptTemplate = _promptTemplateService.GetPrompt("OkrSessionsByTeamResultsEmpty", templateValues);
                }
                else
                {
                    result.PromptTemplate = _promptTemplateService.GetPrompt("OkrSessionsByTeamResults", templateValues);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving OKR sessions by team ID: {TeamId} or name: {TeamName}", teamId, teamName);
                throw;
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
        /// Helper method to find a user by name with multiple fallback strategies
        /// </summary>
        private async Task<string> FindUserByNameWithFallbacks(string userName)
        {
            if (string.IsNullOrEmpty(userName))
            {
                _logger.LogWarning("Empty user name provided");
                return null;
            }

            _logger.LogInformation("Looking for user with name: {UserName}", userName);
            string userId = null;
            
            try
            {
                // Strategy 1: Try exact name match first
                _logger.LogInformation("Attempting exact name match for '{UserName}'", userName);
                var searchResults = await _userPlugin.SearchUsersByNameAsync(userName);
                
                if (searchResults.Users.Count > 0)
                {
                    userId = searchResults.Users[0].UserId;
                    _logger.LogInformation("Found user with ID {UserId} for exact name '{UserName}'", userId, userName);
                    return userId;
                }
                
                // Strategy 2: Try partial name matching
                _logger.LogInformation("Attempting partial name matching for '{UserName}'", userName);
                var nameParts = userName.Split(new[] { ' ', '.', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var part in nameParts)
                {
                    if (part.Length < 2) continue; // Skip very short parts
                    
                    var partialResults = await _userPlugin.SearchUsersByNameAsync(part);
                    if (partialResults.Users.Count > 0)
                    {
                        userId = partialResults.Users[0].UserId;
                        _logger.LogInformation("Found user with ID {UserId} using partial name '{PartialName}'", userId, part);
                        return userId;
                    }
                }
                
                _logger.LogWarning("No users found matching name '{UserName}' after all search attempts", userName);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for user by name '{UserName}'", userName);
                return null;
            }
        }
    }
}