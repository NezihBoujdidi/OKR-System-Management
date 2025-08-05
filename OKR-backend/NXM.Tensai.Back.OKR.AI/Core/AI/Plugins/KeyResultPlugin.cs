using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using NXM.Tensai.Back.OKR.AI.Core.AI;
using NXM.Tensai.Back.OKR.AI.Models;
using NXM.Tensai.Back.OKR.AI.Services;
using NXM.Tensai.Back.OKR.AI.Services.MediatRService;
using NXM.Tensai.Back.OKR.AI.Services.Authorization;
using NXM.Tensai.Back.OKR.Domain;

namespace NXM.Tensai.Back.OKR.AI.Core.AI.Plugins
{
    public class KeyResultPlugin
    {
        private readonly PromptTemplateService _promptTemplateService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ObjectivePlugin _objectivePlugin;
        private readonly ILogger<KeyResultPlugin> _logger;
        private readonly UserContextAccessor _userContextAccessor;
        private const string PluginName = "KeyResultManagement";

        public KeyResultPlugin(
            PromptTemplateService promptTemplateService,
            IServiceProvider serviceProvider,
            ObjectivePlugin objectivePlugin,
            ILogger<KeyResultPlugin> logger,
            IConfiguration configuration,
            UserContextAccessor userContextAccessor)
        {
            _promptTemplateService = promptTemplateService;
            _serviceProvider = serviceProvider;
            _objectivePlugin = objectivePlugin;
            _logger = logger;
            _userContextAccessor = userContextAccessor;
        }

        /// <summary>
        /// Search for key results with optional filter criteria
        /// </summary>
        [KernelFunction]
        [Description("Search for key results by title, objective, or user")]
        public async Task<KeyResultSearchResponse> SearchKeyResultsAsync(
            [Description("Optional title to filter key results by")] string title = null,
            [Description("Optional objective ID to filter key results by")] string objectiveId = null,
            [Description("Optional objective title if objective ID is not available")] string objectiveTitle = null)
        {
            try
            {
                // Get user context directly from the accessor
                var userContext = _userContextAccessor.CurrentUserContext;

                // Create a scope to resolve the scoped service
                using var scope = _serviceProvider.CreateScope();
                var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
                
                // Check view permission
                _logger.LogDebug("Checking permission {Permission}", Permissions.KeyResults_GetAll);
                
                var unauthorizedResponse = await permissionService.CheckPermissionAsync<KeyResultSearchResponse>(
                    Permissions.KeyResults_GetAll,
                    "key result", 
                    "search",
                    (message) => new KeyResultSearchResponse 
                    { 
                        PromptTemplate = message
                    });
                
                // If unauthorized, return the response
                if (unauthorizedResponse != null)
                {
                    _logger.LogWarning("Permission denied for user {UserId} to search key results. Message: {Message}", 
                        userContext?.UserId, unauthorizedResponse.PromptTemplate);
                    return unauthorizedResponse;
                }
                
                _logger.LogInformation("Permission check passed for user {UserId} to search key results", userContext?.UserId);
                
                
                
                // If we have an objective title but no ID, try to search for the objective by title
                if (string.IsNullOrEmpty(objectiveId) && !string.IsNullOrEmpty(objectiveTitle))
                {
                    _logger.LogInformation("No objective ID provided, attempting to find objective by title: {Title}", objectiveTitle);
                    try
                    {
                        var objectiveSearchResults = await _objectivePlugin.SearchObjectivesAsync(objectiveTitle);
                        if (objectiveSearchResults.Objectives.Count == 1)
                        {
                            objectiveId = objectiveSearchResults.Objectives[0].ObjectiveId;
                            _logger.LogInformation("Found objective ID {ObjectiveId} for objective titled '{Title}'", objectiveId, objectiveTitle);
                        }
                        else if (objectiveSearchResults.Objectives.Count > 1)
                        {
                            _logger.LogWarning("Found multiple objectives with title '{Title}'. Using the first one.", objectiveTitle);
                            objectiveId = objectiveSearchResults.Objectives[0].ObjectiveId;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error searching for objective with title '{Title}'", objectiveTitle);
                    }
                }
                    
                // Create a scope to resolve the scoped service
                var keyResultMediatRService = scope.ServiceProvider.GetRequiredService<KeyResultMediatRService>();
                
                // Use the MediatR service to search for key results
                var result = await keyResultMediatRService.SearchKeyResultsAsync(title, objectiveId, userContext?.UserId);
                
                // Generate the key results list for the prompt template with more detailed information
                var keyResultsListBuilder = new StringBuilder();
                for (int i = 0; i < result.KeyResults.Count; i++)
                {
                    var keyResult = result.KeyResults[i];
                    keyResultsListBuilder.AppendLine($"{i+1}. {keyResult.Title}");
                    
                    // Add description if available
                    if (!string.IsNullOrEmpty(keyResult.Description))
                    {
                        keyResultsListBuilder.AppendLine($"   Description: {keyResult.Description}");
                    }
                    
                    // Add objective information
                    if (!string.IsNullOrEmpty(keyResult.ObjectiveTitle))
                    {
                        keyResultsListBuilder.AppendLine($"   Objective: {keyResult.ObjectiveTitle}");
                    }
                    
                    // Add date range
                    keyResultsListBuilder.AppendLine($"   Period: {keyResult.StartedDate:yyyy-MM-dd} to {keyResult.EndDate:yyyy-MM-dd}");
                    
                    // Add owner and progress
                    keyResultsListBuilder.AppendLine($"   Owner: {keyResult.UserName ?? "Not assigned"}");
                    keyResultsListBuilder.AppendLine($"   Status: {keyResult.Status}, Progress: {keyResult.Progress}%");
                    
                    // Add a separator between key results
                    if (i < result.KeyResults.Count - 1)
                    {
                        keyResultsListBuilder.AppendLine();
                    }
                }
                
                // Generate a prompt template
                Dictionary<string, string> templateValues = new()
                {
                    { "count", result.Count.ToString() },
                    { "searchTerm", !string.IsNullOrEmpty(title) ? title : "(any)" },
                    { "keyResultsList", keyResultsListBuilder.ToString().Trim() }
                };
                
                // Use different template for empty results
                if (result.KeyResults.Count == 0)
                {
                    result.PromptTemplate = _promptTemplateService.GetPrompt("KeyResultSearchResultsEmpty", templateValues);
                }
                else
                {
                    result.PromptTemplate = _promptTemplateService.GetPrompt("KeyResultSearchResults", templateValues);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for key results with title: {Title}", title);
                throw new ApplicationException($"Failed to search for key results: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Create a new key result with the provided details
        /// </summary>
        [KernelFunction]
        [Description("Create a new key result with the given title, description, dates, and assignments")]
        public async Task<KeyResultCreationResponse> CreateKeyResultAsync(
            [Description("The title for the new key result")] string title,
            [Description("Objective ID that this key result belongs to")] string objectiveId,
            [Description("Objective title if ID is not available")] string objectiveTitle = null,
            [Description("Start date of the key result (format: yyyy-MM-dd)")] string startDate = null,
            [Description("End date of the key result (format: yyyy-MM-dd)")] string endDate = null,
            [Description("Optional description of the key result")] string description = null,
            [Description("Optional status for the key result (NotStarted, InProgress, Completed, Cancelled)")] string status = null,
            [Description("Initial progress percentage (0-100)")] int? progress = 0)
        {
            try
            {
                // Get user context directly from the accessor
                var userContext = _userContextAccessor.CurrentUserContext;

                // Create a scope to resolve the scoped service
                using var scope = _serviceProvider.CreateScope();
                var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
                
                // Check create permission
                _logger.LogDebug("Checking permission {Permission}", Permissions.KeyResults_Create);
                
                var unauthorizedResponse = await permissionService.CheckPermissionAsync<KeyResultCreationResponse>(
                    Permissions.KeyResults_Create,
                    "key result", 
                    "create",
                    (message) => new KeyResultCreationResponse 
                    { 
                        PromptTemplate = message,
                        Title = title
                    });
                
                // If unauthorized, return the response
                if (unauthorizedResponse != null)
                {
                    _logger.LogWarning("Permission denied for user {UserId} to create key result. Message: {Message}", 
                        userContext?.UserId, unauthorizedResponse.PromptTemplate);
                    return unauthorizedResponse;
                }
                
                _logger.LogInformation("Permission check passed for user {UserId} to create key result", userContext?.UserId);

                // If we have an objective title but no ID, try to search for the objective by title
                if (string.IsNullOrEmpty(objectiveId) && !string.IsNullOrEmpty(objectiveTitle))
                {
                    _logger.LogInformation("No objective ID provided, attempting to find objective by title: {Title}", objectiveTitle);
                    try
                    {
                        var objectiveSearchResults = await _objectivePlugin.SearchObjectivesAsync(objectiveTitle);
                        if (objectiveSearchResults.Objectives.Count == 1)
                        {
                            objectiveId = objectiveSearchResults.Objectives[0].ObjectiveId;
                            _logger.LogInformation("Found objective ID {ObjectiveId} for objective titled '{Title}'", objectiveId, objectiveTitle);
                        }
                        else if (objectiveSearchResults.Objectives.Count > 1)
                        {
                            throw new ArgumentException($"Found {objectiveSearchResults.Objectives.Count} objectives titled '{objectiveTitle}'. Please specify which one using the objective ID.");
                        }
                        else
                        {
                            throw new ArgumentException($"No objective found with title '{objectiveTitle}'.");
                        }
                    }
                    catch (Exception ex) when (!(ex is ArgumentException))
                    {
                        _logger.LogWarning(ex, "Error searching for objective with title '{Title}'", objectiveTitle);
                        throw new ApplicationException($"Could not find objective by title '{objectiveTitle}': {ex.Message}");
                    }
                }

                if (string.IsNullOrEmpty(objectiveId))
                {
                    throw new ArgumentException("An objective ID is required to create a key result.");
                }

                // Get objective details
                ObjectiveDetailsResponse objectiveDetails;
                try
                {
                    objectiveDetails = await _objectivePlugin.GetObjectiveDetailsAsync(objectiveId);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"Could not find objective with ID '{objectiveId}': {ex.Message}");
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

                // If end date is not provided, use objective end date
                if (string.IsNullOrEmpty(endDate))
                {
                    endDateTime = objectiveDetails.EndDate;
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
                var keyResultMediatRService = scope.ServiceProvider.GetRequiredService<KeyResultMediatRService>();

                // Create a request object for the MediatR service
                var request = new KeyResultCreationRequest
                {
                    Title = title,
                    Description = description,
                    ObjectiveId = objectiveId,
                    UserId = userContext?.UserId,
                    StartedDate = startDateTime,
                    EndDate = endDateTime,
                    Status = status ?? "NotStarted",
                    Progress = progress ?? 0
                };
                
                _logger.LogInformation("Creating key result: {Title} for objective: {ObjectiveId}", title, objectiveId);
                
                // Call the MediatR service to handle the key result creation
                var result = await keyResultMediatRService.CreateKeyResultAsync(request);
                
                _logger.LogInformation("Key result created successfully: {KeyResultId}, {Title}", result.KeyResultId, result.Title);

                // Create prompt template values
                Dictionary<string, string> templateValues = new()
                {
                    { "title", result.Title },
                    { "description", result.Description },
                    { "startedDate", result.StartedDate.ToString("yyyy-MM-dd") },
                    { "endDate", result.EndDate.ToString("yyyy-MM-dd") },
                    { "objectiveTitle", objectiveDetails.Title }
                };

                // Add description if provided
                if (!string.IsNullOrEmpty(description))
                {
                    templateValues["description"] = description;
                    result.PromptTemplate = _promptTemplateService.GetPrompt("KeyResultCreatedWithDescription", templateValues);
                }
                else
                {
                    result.PromptTemplate = _promptTemplateService.GetPrompt("KeyResultCreated", templateValues);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating key result '{Title}'", title);
                throw;
            }
        }
        
        /// <summary>
        /// Update an existing key result
        /// </summary>
        [KernelFunction]
        [Description("Update an existing key result's information")]
        public async Task<KeyResultUpdateResponse> UpdateKeyResultAsync(
            [Description("The ID of the key result to update")] string keyResultId,
            [Description("The title of the key result to update if ID is not available")] string title = null,
            [Description("The new title for the key result (optional)")] string newTitle = null,
            [Description("The new description for the key result (optional)")] string description = null,
            [Description("New start date of the key result (format: yyyy-MM-dd)")] string startDate = null,
            [Description("New end date of the key result (format: yyyy-MM-dd)")] string endDate = null,
            [Description("The ID of the objective for this key result (optional)")] string objectiveId = null,
            [Description("The title of the objective for this key result (optional)")] string objectiveTitle = null,
            [Description("New status for the key result (NotStarted, InProgress, Completed, Cancelled)")] string status = null,
            [Description("Updated progress percentage (0-100)")] int? progress = null)
        {
            try
            {
                // Get user context directly from the accessor
                var userContext = _userContextAccessor.CurrentUserContext;
                
                _logger.LogInformation("KeyResultPlugin.UpdateKeyResultAsync called - KeyResultId: {KeyResultId}, Title: {Title}, User: {UserId}, Role: {UserRole}", 
                    keyResultId, title, userContext?.UserId, userContext?.Role);

                // Create a scope to resolve the scoped service
                using var scope = _serviceProvider.CreateScope();
                var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
                
                // Check update permission
                _logger.LogDebug("Checking permission {Permission}", Permissions.KeyResults_Update);
                
                var unauthorizedResponse = await permissionService.CheckPermissionAsync<KeyResultUpdateResponse>(
                    Permissions.KeyResults_Update,
                    "key result", 
                    "update",
                    (message) => new KeyResultUpdateResponse 
                    { 
                        PromptTemplate = message,
                        KeyResultId = keyResultId
                    });
                
                // If unauthorized, return the response
                if (unauthorizedResponse != null)
                {
                    _logger.LogWarning("Permission denied for user {UserId} to update key result. Message: {Message}", 
                        userContext?.UserId, unauthorizedResponse.PromptTemplate);
                    return unauthorizedResponse;
                }
                
                _logger.LogInformation("Permission check passed for user {UserId} to update key result", userContext?.UserId);
                
                _logger.LogDebug("UpdateKeyResultAsync called with keyResultId: '{KeyResultId}', title: '{Title}'", 
                    keyResultId, title);
                
                var keyResultMediatRService = scope.ServiceProvider.GetRequiredService<KeyResultMediatRService>();
                
                // If we have a title but no ID, try to search for the key result by title
                if (string.IsNullOrEmpty(keyResultId) && !string.IsNullOrEmpty(title))
                {
                    _logger.LogInformation("No key result ID provided, attempting to find key result by title: {Title}", title);
                    
                    try
                    {
                        // Search for key results with this title
                        var searchResults = await SearchKeyResultsAsync(title);
                        
                        // If exactly one match is found, use that key result's ID
                        if (searchResults.KeyResults.Count == 1)
                        {
                            keyResultId = searchResults.KeyResults[0].KeyResultId;
                            _logger.LogInformation("Found key result ID {KeyResultId} for key result titled '{Title}'", keyResultId, title);
                            
                            // Store the current title to check if we actually need to update it
                            var currentTitle = searchResults.KeyResults[0].Title;
                            
                            // If the title parameter is being used just to find the key result (not to rename it),
                            // and it matches the current title, don't try to update the title unless newTitle is provided
                            if (title.Equals(currentTitle, StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(newTitle))
                            {
                                newTitle = null; // Clear newTitle to avoid unnecessary update
                                _logger.LogDebug("Title parameter used only for lookup, not for update");
                            }
                        }
                        // If multiple matches are found, we need more specifics
                        else if (searchResults.KeyResults.Count > 1)
                        {
                            throw new ArgumentException($"Found {searchResults.KeyResults.Count} key results titled '{title}'. Please specify which one using the key result ID.");
                        }
                        else
                        {
                            throw new ArgumentException($"No key result found with title '{title}'.");
                        }
                    }
                    catch (Exception ex) when (!(ex is ArgumentException))
                    {
                        _logger.LogWarning(ex, "Error searching for key result with title '{Title}'", title);
                        throw new ApplicationException($"Could not find key result by title '{title}': {ex.Message}");
                    }
                }
                
                // At this point we must have a keyResultId
                if (string.IsNullOrEmpty(keyResultId))
                {
                    throw new ArgumentNullException(nameof(keyResultId), "Key result ID is required to update a key result.");
                }

                // Try to get the current key result details first
                KeyResultDetailsResponse currentKeyResult;
                try
                {
                    _logger.LogDebug("Retrieving current key result details for keyResultId: {KeyResultId}", keyResultId);
                    currentKeyResult = await keyResultMediatRService.GetKeyResultDetailsAsync(keyResultId);
                    _logger.LogDebug("Successfully retrieved key result: {Title} with ID {KeyResultId}", 
                        currentKeyResult?.Title, currentKeyResult?.KeyResultId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to find key result with ID '{KeyResultId}'", keyResultId);
                    throw new ApplicationException($"Key result with ID '{keyResultId}' not found.", ex);
                }

                // If objectiveId is not provided but objectiveTitle is, try to look up the objective
                if (string.IsNullOrEmpty(objectiveId) && !string.IsNullOrEmpty(objectiveTitle))
                {
                    try
                    {
                        var objectiveSearchResults = await _objectivePlugin.SearchObjectivesAsync(objectiveTitle);
                        if (objectiveSearchResults.Objectives.Count == 1)
                        {
                            objectiveId = objectiveSearchResults.Objectives[0].ObjectiveId;
                            _logger.LogInformation("Found objective ID {ObjectiveId} for objective titled '{Title}'", objectiveId, objectiveTitle);
                        }
                        else if (objectiveSearchResults.Objectives.Count > 1)
                        {
                            _logger.LogWarning("Found multiple objectives with title '{Title}'. Using the first one.", objectiveTitle);
                            objectiveId = objectiveSearchResults.Objectives[0].ObjectiveId;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error searching for objective with title '{Title}'", objectiveTitle);
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
                var request = new KeyResultUpdateRequest
                {
                    Title = newTitle ?? title ?? currentKeyResult.Title,
                    Description = description ?? currentKeyResult.Description,
                    ObjectiveId = objectiveId ?? currentKeyResult.ObjectiveId,
                    StartedDate = startDateTime ?? currentKeyResult.StartedDate,
                    EndDate = endDateTime ?? currentKeyResult.EndDate,
                    Status = status ?? currentKeyResult.Status,
                    Progress = progress ?? currentKeyResult.Progress,
                    UserId = userContext?.UserId ?? currentKeyResult.UserId
                };

                _logger.LogDebug("Updating key result {KeyResultId} with values: Title={Title}, Description={Description}, StartDate={StartDate}, EndDate={EndDate}, ObjectiveId={ObjectiveId}", 
                    keyResultId, request.Title, request.Description, request.StartedDate, request.EndDate, request.ObjectiveId);
                
                // Call the MediatR service to update the key result
                var result = await keyResultMediatRService.UpdateKeyResultAsync(keyResultId, request);
                _logger.LogInformation("Key result updated successfully: {KeyResultId}", result.KeyResultId);
                
                // Generate a prompt template
                Dictionary<string, string> templateValues = new()
                {
                    { "title", result.Title },
                    { "description", request.Description ?? "No description provided" }
                };

                try
                {
                    if (!string.IsNullOrEmpty(request.Description))
                    {
                        result.PromptTemplate = _promptTemplateService.GetPrompt("KeyResultUpdatedWithDescription", templateValues);
                    }
                    else
                    {
                        result.PromptTemplate = _promptTemplateService.GetPrompt("KeyResultUpdated", templateValues);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to find prompt template for key result update. Using generic message instead.");
                    if (!string.IsNullOrEmpty(request.Description))
                    {
                        result.PromptTemplate = $"I've updated the key result '{result.Title}' with the new description: '{request.Description}'.";
                    }
                    else
                    {
                        result.PromptTemplate = $"I've updated the key result '{result.Title}' successfully.";
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateKeyResultAsync");
                throw;
            }
        }

        /// <summary>
        /// Delete a key result
        /// </summary>
        [KernelFunction]
        [Description("Delete a key result from the system")]
        public async Task<KeyResultDeleteResponse> DeleteKeyResultAsync(
            [Description("The ID of the key result to delete")] string keyResultId,
            [Description("The title of the key result to delete if ID is not available")] string title = null)
        {
            try
            {
                // Get user context directly from the accessor
                var userContext = _userContextAccessor.CurrentUserContext;
                
                _logger.LogInformation("KeyResultPlugin.DeleteKeyResultAsync called - KeyResultId: {KeyResultId}, Title: {Title}, User: {UserId}, Role: {UserRole}", 
                    keyResultId, title, userContext?.UserId, userContext?.Role);

                // Create a scope to resolve the scoped service
                using var scope = _serviceProvider.CreateScope();
                var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
                
                // Check delete permission
                _logger.LogDebug("Checking permission {Permission}", Permissions.KeyResults_Delete);
                
                var unauthorizedResponse = await permissionService.CheckPermissionAsync<KeyResultDeleteResponse>(
                    Permissions.KeyResults_Delete,
                    "key result", 
                    "delete",
                    (message) => new KeyResultDeleteResponse 
                    { 
                        PromptTemplate = message,
                        KeyResultId = keyResultId
                    });
                
                // If unauthorized, return the response
                if (unauthorizedResponse != null)
                {
                    _logger.LogWarning("Permission denied for user {UserId} to delete key result. Message: {Message}", 
                        userContext?.UserId, unauthorizedResponse.PromptTemplate);
                    return unauthorizedResponse;
                }
                
                _logger.LogInformation("Permission check passed for user {UserId} to delete key result", userContext?.UserId);
                
                _logger.LogDebug("DeleteKeyResultAsync called with keyResultId: '{KeyResultId}', title: '{Title}'", keyResultId, title);
                
                var keyResultMediatRService = scope.ServiceProvider.GetRequiredService<KeyResultMediatRService>();
                
                // If we have a title but no ID, try to search for the key result by title
                if (string.IsNullOrEmpty(keyResultId) && !string.IsNullOrEmpty(title))
                {
                    _logger.LogInformation("No key result ID provided, attempting to find key result by title: {Title}", title);
                    
                    try
                    {
                        // Search for key results with this title
                        var searchResults = await SearchKeyResultsAsync(title);
                        
                        // If exactly one match is found, use that key result's ID
                        if (searchResults.KeyResults.Count == 1)
                        {
                            keyResultId = searchResults.KeyResults[0].KeyResultId;
                            _logger.LogInformation("Found key result ID {KeyResultId} for key result titled '{Title}'", keyResultId, title);
                        }
                        // If multiple matches are found, we need more specifics
                        else if (searchResults.KeyResults.Count > 1)
                        {
                            throw new ArgumentException($"Found {searchResults.KeyResults.Count} key results titled '{title}'. Please specify which one using the key result ID.");
                        }
                        else
                        {
                            throw new ArgumentException($"No key result found with title '{title}'.");
                        }
                    }
                    catch (Exception ex) when (!(ex is ArgumentException))
                    {
                        _logger.LogWarning(ex, "Error searching for key result with title '{Title}'", title);
                        throw new ApplicationException($"Could not find key result by title '{title}': {ex.Message}");
                    }
                }
                
                // At this point we must have a keyResultId
                if (string.IsNullOrEmpty(keyResultId))
                {
                    throw new ArgumentNullException(nameof(keyResultId), "Key result ID is required to delete a key result.");
                }

                // Delete the key result and get the response
                var result = await keyResultMediatRService.DeleteKeyResultAsync(keyResultId);
                
                // Generate the prompt template for the response
                Dictionary<string, string> templateValues = new()
                {
                    { "title", result.Title },
                    { "keyResultId", result.KeyResultId }
                };
                
                result.PromptTemplate = _promptTemplateService.GetPrompt("KeyResultDeleted", templateValues);
                
                _logger.LogInformation("Key result deleted successfully: {KeyResultId}, {Title}", result.KeyResultId, result.Title);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting key result with ID '{KeyResultId}' or title '{Title}'", keyResultId, title);
                throw;
            }
        }

        /// <summary>
        /// Get details of a specific key result
        /// </summary>
        [KernelFunction]
        [Description("Get details of a specific key result by ID or title")]
        public async Task<KeyResultDetailsResponse> GetKeyResultDetailsAsync(
            [Description("ID of the key result to retrieve")] string keyResultId,
            [Description("Title of the key result if ID is not available")] string title = null)
        {
            try
            {
                // Get user context directly from the accessor
                var userContext = _userContextAccessor.CurrentUserContext;
                
                _logger.LogInformation("KeyResultPlugin.GetKeyResultDetailsAsync called - KeyResultId: {KeyResultId}, Title: {Title}, User: {UserId}, Role: {UserRole}", 
                    keyResultId, title, userContext?.UserId, userContext?.Role);

                // Create a scope to resolve the scoped service
                using var scope = _serviceProvider.CreateScope();
                var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
                
                // Check view permission
                _logger.LogDebug("Checking permission {Permission}", Permissions.KeyResults_GetById);
                
                var unauthorizedResponse = await permissionService.CheckPermissionAsync<KeyResultDetailsResponse>(
                    Permissions.KeyResults_GetById,
                    "key result", 
                    "view",
                    (message) => new KeyResultDetailsResponse 
                    { 
                        PromptTemplate = message,
                        KeyResultId = keyResultId
                    });
                
                // If unauthorized, return the response
                if (unauthorizedResponse != null)
                {
                    _logger.LogWarning("Permission denied for user {UserId} to view key result details. Message: {Message}", 
                        userContext?.UserId, unauthorizedResponse.PromptTemplate);
                    return unauthorizedResponse;
                }
                
                _logger.LogInformation("Permission check passed for user {UserId} to view key result details", userContext?.UserId);
                
                _logger.LogInformation("Getting details for key result with ID: {KeyResultId} or title: {Title}", keyResultId, title);
                
                var keyResultMediatRService = scope.ServiceProvider.GetRequiredService<KeyResultMediatRService>();
                
                // If we have a title but no ID, try to search for the key result by title
                if (string.IsNullOrEmpty(keyResultId) && !string.IsNullOrEmpty(title))
                {
                    _logger.LogInformation("No key result ID provided, attempting to find key result by title: {Title}", title);
                    
                    try
                    {
                        // Search for key results with this title
                        var searchResults = await SearchKeyResultsAsync(title);
                        
                        // If exactly one match is found, use that key result's ID
                        if (searchResults.KeyResults.Count == 1)
                        {
                            keyResultId = searchResults.KeyResults[0].KeyResultId;
                            _logger.LogInformation("Found key result ID {KeyResultId} for key result titled '{Title}'", keyResultId, title);
                        }
                        // If multiple matches are found, we need more specifics
                        else if (searchResults.KeyResults.Count > 1)
                        {
                            throw new ArgumentException($"Found {searchResults.KeyResults.Count} key results titled '{title}'. Please specify which one using the key result ID.");
                        }
                        else
                        {
                            throw new ArgumentException($"No key result found with title '{title}'.");
                        }
                    }
                    catch (Exception ex) when (!(ex is ArgumentException))
                    {
                        _logger.LogWarning(ex, "Error searching for key result with title '{Title}'", title);
                        throw new ApplicationException($"Could not find key result by title '{title}': {ex.Message}");
                    }
                }
                
                // At this point we must have a keyResultId
                if (string.IsNullOrEmpty(keyResultId))
                {
                    throw new ArgumentNullException(nameof(keyResultId), "Key result ID is required to get key result details.");
                }
                
                var result = await keyResultMediatRService.GetKeyResultDetailsAsync(keyResultId);
                
                // Generate the prompt template for the response
                Dictionary<string, string> templateValues = new()
                {
                    { "title", result.Title },
                    { "description", result.Description },
                    { "objectiveTitle", result.ObjectiveTitle ?? "Unknown Objective" },
                    { "startedDate", result.StartedDate.ToString("yyyy-MM-dd") },
                    { "endDate", result.EndDate.ToString("yyyy-MM-dd") },
                    { "status", result.Status },
                    { "progress", result.Progress.ToString() },
                    { "userName", result.UserName ?? "Not assigned" }
                };
                
                result.PromptTemplate = _promptTemplateService.GetPrompt("KeyResultDetails", templateValues);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting key result details for ID: {KeyResultId} or title: {Title}", keyResultId, title);
                throw new ApplicationException($"Failed to get key result details: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get all key results by objective ID or title
        /// </summary>
        [KernelFunction]
        [Description("Get all key results for a specific objective")]
        public async Task<KeyResultsByObjectiveResponse> GetKeyResultsByObjectiveIdAsync(
            [Description("The ID of the objective to get key results for")] string objectiveId,
            [Description("The title of the objective if ID is not available")] string objectiveTitle = null)
        {
            try
            {
                // Get user context directly from the accessor
                var userContext = _userContextAccessor.CurrentUserContext;
                
                _logger.LogInformation("KeyResultPlugin.GetKeyResultsByObjectiveIdAsync called - ObjectiveId: {ObjectiveId}, Title: {ObjectiveTitle}, User: {UserId}, Role: {UserRole}", 
                    objectiveId, objectiveTitle, userContext?.UserId, userContext?.Role);

                // Create a scope to resolve the scoped service
                using var scope = _serviceProvider.CreateScope();
                var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
                
                // Check view permission
                _logger.LogDebug("Checking permission {Permission}", Permissions.KeyResults_GetAll);
                
                var unauthorizedResponse = await permissionService.CheckPermissionAsync<KeyResultsByObjectiveResponse>(
                    Permissions.KeyResults_GetAll,
                    "key result", 
                    "view by objective",
                    (message) => new KeyResultsByObjectiveResponse 
                    { 
                        PromptTemplate = message,
                        ObjectiveId = objectiveId
                    });
                
                // If unauthorized, return the response
                if (unauthorizedResponse != null)
                {
                    _logger.LogWarning("Permission denied for user {UserId} to view key results by objective. Message: {Message}", 
                        userContext?.UserId, unauthorizedResponse.PromptTemplate);
                    return unauthorizedResponse;
                }
                
                _logger.LogInformation("Permission check passed for user {UserId} to view key results by objective", userContext?.UserId);
                
                _logger.LogInformation("Getting key results for objective with ID: {ObjectiveId} or title: {ObjectiveTitle}", 
                    objectiveId, objectiveTitle);
                
                var keyResultMediatRService = scope.ServiceProvider.GetRequiredService<KeyResultMediatRService>();
                
                // If we have an objective title but no ID, try to search for the objective by title
                if (string.IsNullOrEmpty(objectiveId) && !string.IsNullOrEmpty(objectiveTitle))
                {
                    _logger.LogInformation("No objective ID provided, attempting to find objective by title: {Title}", objectiveTitle);
                    
                    try
                    {
                        // Search for objectives with this title
                        var searchResults = await _objectivePlugin.SearchObjectivesAsync(objectiveTitle);
                        
                        // If exactly one match is found, use that objective's ID
                        if (searchResults.Objectives.Count == 1)
                        {
                            objectiveId = searchResults.Objectives[0].ObjectiveId;
                            _logger.LogInformation("Found objective ID {ObjectiveId} for objective titled '{Title}'", 
                                objectiveId, objectiveTitle);
                        }
                        // If multiple matches are found, we need more specifics
                        else if (searchResults.Objectives.Count > 1)
                        {
                            throw new ArgumentException($"Found {searchResults.Objectives.Count} objectives titled '{objectiveTitle}'. Please specify which one using the objective ID.");
                        }
                        else
                        {
                            throw new ArgumentException($"No objective found with title '{objectiveTitle}'.");
                        }
                    }
                    catch (Exception ex) when (!(ex is ArgumentException))
                    {
                        _logger.LogWarning(ex, "Error searching for objective with title '{Title}'", objectiveTitle);
                        throw new ApplicationException($"Could not find objective by title '{objectiveTitle}': {ex.Message}");
                    }
                }
                
                // At this point we must have an objectiveId
                if (string.IsNullOrEmpty(objectiveId))
                {
                    throw new ArgumentNullException(nameof(objectiveId), "Objective ID is required to get key results for an objective.");
                }
                
                // Get the key results for the objective
                var result = await keyResultMediatRService.GetKeyResultsByObjectiveIdAsync(objectiveId);
                
                // Generate the key results list for the prompt template
                var keyResultsListBuilder = new StringBuilder();
                
                // Only build the key results list if there are key results
                if (result.KeyResults.Count > 0)
                {
                    for (int i = 0; i < result.KeyResults.Count; i++)
                    {
                        var keyResult = result.KeyResults[i];
                        keyResultsListBuilder.AppendLine($"{i+1}. {keyResult.Title} (ID: {keyResult.KeyResultId})");
                        if (!string.IsNullOrEmpty(keyResult.Description))
                        {
                            keyResultsListBuilder.AppendLine($"   Description: {keyResult.Description}");
                        }
                        keyResultsListBuilder.AppendLine($"   Period: {keyResult.StartedDate:yyyy-MM-dd} to {keyResult.EndDate:yyyy-MM-dd}");
                        keyResultsListBuilder.AppendLine($"   Owner: {keyResult.UserName ?? "Not assigned"}");
                        keyResultsListBuilder.AppendLine($"   Status: {keyResult.Status}, Progress: {keyResult.Progress}%");
                        
                        // Add a separator line between key results
                        if (i < result.KeyResults.Count - 1)
                        {
                            keyResultsListBuilder.AppendLine();
                        }
                    }
                }
                
                // Generate the prompt template
                Dictionary<string, string> templateValues = new()
                {
                    { "objectiveTitle", result.ObjectiveTitle },
                    { "count", result.Count.ToString() },
                    { "keyResultsList", keyResultsListBuilder.ToString().Trim() }
                };
                
                // Use different template for empty results or fallback to direct messages if templates are missing
                if (result.KeyResults.Count == 0)
                {
                    string fallbackEmptyMessage = $"There are no key results for the objective '{result.ObjectiveTitle}'. Would you like to create one?";
                    try {
                        result.PromptTemplate = SafeGetPrompt("KeyResultsByObjectiveResultsEmpty", templateValues, fallbackEmptyMessage);
                    } catch (Exception ex) {
                        _logger.LogWarning(ex, "Failed to get prompt template, using direct message");
                        result.PromptTemplate = fallbackEmptyMessage;
                    }
                }
                else
                {
                    string keyResultsList = keyResultsListBuilder.ToString().Trim();
                    string fallbackMessage = $"I found {result.KeyResults.Count} key results for the objective '{result.ObjectiveTitle}':\n\n{keyResultsList}";
                    try {
                        result.PromptTemplate = SafeGetPrompt("KeyResultsByObjectiveResults", templateValues, fallbackMessage);
                    } catch (Exception ex) {
                        _logger.LogWarning(ex, "Failed to get prompt template, using direct message");
                        result.PromptTemplate = fallbackMessage;
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving key results by objective ID: {ObjectiveId} or title: {ObjectiveTitle}", 
                    objectiveId, objectiveTitle);
                throw;
            }
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
    }
}