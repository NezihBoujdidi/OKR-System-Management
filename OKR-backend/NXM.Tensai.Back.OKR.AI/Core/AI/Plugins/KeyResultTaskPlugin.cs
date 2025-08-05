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
using NXM.Tensai.Back.OKR.Application;
using NXM.Tensai.Back.OKR.Domain;
using NXM.Tensai.Back.OKR.AI.Services.Authorization;

namespace NXM.Tensai.Back.OKR.AI.Core.AI.Plugins
{
    public class KeyResultTaskPlugin
    {
        private readonly PromptTemplateService _promptTemplateService;
        private readonly IServiceProvider _serviceProvider;
        private readonly KeyResultPlugin _keyResultPlugin;
        private readonly UserPlugin _userPlugin;
        private readonly ILogger<KeyResultTaskPlugin> _logger;
        private readonly UserContextAccessor _userContextAccessor;
        private const string PluginName = "KeyResultTaskManagement";

        public KeyResultTaskPlugin(
            PromptTemplateService promptTemplateService,
            IServiceProvider serviceProvider,
            KeyResultPlugin keyResultPlugin,
            UserPlugin userPlugin,
            ILogger<KeyResultTaskPlugin> logger,
            IConfiguration configuration,
            UserContextAccessor userContextAccessor)
        {
            _promptTemplateService = promptTemplateService;
            _serviceProvider = serviceProvider;
            _keyResultPlugin = keyResultPlugin;
            _userPlugin = userPlugin;
            _logger = logger;
            _userContextAccessor = userContextAccessor;
        }

        /// <summary>
        /// Create a task for a key result
        /// </summary>
        [KernelFunction]
        [Description("Create a new task for a specific key result")]
        public async Task<KeyResultTaskCreationResponse> CreateKeyResultTaskAsync(
            [Description("The title of the task to create")] string title,
            [Description("The ID of the key result this task belongs to")] string keyResultId,
            [Description("The title of the key result if keyResultId not provided")] string keyResultTitle = null,
            [Description("Optional description of the task")] string description = null,
            [Description("Optional start date in format yyyy-MM-dd")] string startDate = null,
            [Description("Optional end date in format yyyy-MM-dd")] string endDate = null,
            [Description("Optional collaborator ID who is assigned to work on the task")] string collaboratorId = null,
            [Description("Optional collaborator name if ID is not provided")] string collaboratorName = null,
            [Description("Optional initial progress percentage (0-100)")] int? progress = null,
            [Description("Optional priority (Low, Medium, High, Critical)")] string priority = null)
        {
            try
            {
                // Get user context directly from the accessor
                var userContext = _userContextAccessor.CurrentUserContext;
                
                _logger.LogInformation("KeyResultTaskPlugin.CreateKeyResultTaskAsync called - Title: {Title}, User: {UserId}, Role: {UserRole}", 
                    title, userContext?.UserId, userContext?.Role);

                // Create a scope to resolve the scoped service
                using var scope = _serviceProvider.CreateScope();
                var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
                
                // Check create permission
                _logger.LogDebug("Checking permission {Permission}", Permissions.KeyResultTasks_Create);
                
                var unauthorizedResponse = await permissionService.CheckPermissionAsync<KeyResultTaskCreationResponse>(
                    Permissions.KeyResultTasks_Create,
                    "task", 
                    "create",
                    (message) => new KeyResultTaskCreationResponse 
                    { 
                        PromptTemplate = message,
                        Title = title,
                        KeyResultId = keyResultId
                    });
                
                // If unauthorized, return the response
                if (unauthorizedResponse != null)
                {
                    _logger.LogWarning("Permission denied for user {UserId} to create task. Message: {Message}", 
                        userContext?.UserId, unauthorizedResponse.PromptTemplate);
                    return unauthorizedResponse;
                }
                
                _logger.LogInformation("Permission check passed for user {UserId} to create task", userContext?.UserId);
                
                _logger.LogDebug("CreateKeyResultTaskAsync called with title: '{Title}', keyResultId: '{KeyResultId}'", 
                    title, keyResultId);

                // If we have a key result title but no ID, try to search for the key result by title
                if (string.IsNullOrEmpty(keyResultId) && !string.IsNullOrEmpty(keyResultTitle))
                {
                    _logger.LogInformation("Key result ID not provided, searching for key result by title: {Title}", keyResultTitle);
                    
                    try
                    {
                        // Search for key results with this title
                        var searchResults = await _keyResultPlugin.SearchKeyResultsAsync(keyResultTitle);
                        
                        if (searchResults.KeyResults.Count > 0)
                        {
                            // Take the first matching key result
                            keyResultId = searchResults.KeyResults[0].KeyResultId;
                            _logger.LogInformation("Found key result with ID {KeyResultId} for title '{Title}'", 
                                keyResultId, keyResultTitle);
                        }
                        else
                        {
                            throw new ArgumentException($"No key result found with title '{keyResultTitle}'.");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error searching for key result with title '{Title}'", keyResultTitle);
                        throw new ApplicationException($"Could not find key result by title '{keyResultTitle}': {ex.Message}");
                    }
                }
                
                if (string.IsNullOrEmpty(keyResultId))
                {
                    throw new ArgumentNullException(nameof(keyResultId), "Key result ID is required to create a task.");
                }

                // If we have a collaborator name but no ID, try to find the user by name
                if (string.IsNullOrEmpty(collaboratorId) && !string.IsNullOrEmpty(collaboratorName))
                {
                    _logger.LogInformation("Collaborator ID not provided, searching for user by name: {Name}", collaboratorName);
                    
                    try
                    {
                        // Search for users with the given name
                        var searchResults = await _userPlugin.SearchUsersByNameAsync(collaboratorName);
                        
                        if (searchResults.Users.Count > 0)
                        {
                            // Take the first matching user
                            collaboratorId = searchResults.Users[0].UserId;
                            _logger.LogInformation("Found collaborator with ID {CollaboratorId} for name '{Name}'", 
                                collaboratorId, collaboratorName);
                        }
                        else
                        {
                            _logger.LogWarning("No users found matching name '{Name}'", collaboratorName);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error searching for collaborator by name '{Name}'", collaboratorName);
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

                if (startDateTime.HasValue && endDateTime.HasValue && startDateTime > endDateTime)
                {
                    throw new ArgumentException("Start date must be before end date");
                }

                var keyResultTaskMediatRService = scope.ServiceProvider.GetRequiredService<KeyResultTaskMediatRService>();

                // Get key result details to validate it exists and to get default dates if needed
                var keyResultDetails = await _keyResultPlugin.GetKeyResultDetailsAsync(keyResultId);
                
                // Set default dates based on the key result if not provided
                if (!startDateTime.HasValue)
                {
                    startDateTime = keyResultDetails.StartedDate;
                }
                
                if (!endDateTime.HasValue)
                {
                    endDateTime = keyResultDetails.EndDate;
                }
                
                // Set default user ID from the current user context
                string userId = userContext?.UserId;
                
                // Set default collaborator ID to the user ID if not provided
                if (string.IsNullOrEmpty(collaboratorId))
                {
                    collaboratorId = userId;
                }

                // Create a request object for the MediatR service
                var request = new KeyResultTaskCreationRequest
                {
                    Title = title,
                    Description = description,
                    KeyResultId = keyResultId,
                    UserId = userId,
                    StartedDate = startDateTime.Value,
                    EndDate = endDateTime.Value,
                    CollaboratorId = collaboratorId,
                    Progress = progress ?? 0,
                    Priority = priority
                };

                // Call the MediatR service to create the key result task
                var result = await keyResultTaskMediatRService.CreateKeyResultTaskAsync(request);
                
                _logger.LogInformation("Key result task created successfully with ID: {TaskId} for key result {KeyResultId}", 
                    result.KeyResultTaskId, keyResultId);

                // Generate prompt template
                Dictionary<string, string> templateValues = new()
                {
                    { "taskTitle", result.Title },
                    { "keyResultTitle", result.KeyResultTitle }
                };

                if (!string.IsNullOrEmpty(description))
                {
                    templateValues["description"] = description;
                    result.PromptTemplate = _promptTemplateService.GetPrompt("KeyResultTaskCreatedWithDescription", templateValues);
                }
                else
                {
                    result.PromptTemplate = _promptTemplateService.GetPrompt("KeyResultTaskCreated", templateValues);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating key result task '{Title}' for key result ID '{KeyResultId}'", 
                    title, keyResultId);
                throw;
            }
        }

        /// <summary>
        /// Update a key result task with new information
        /// </summary>
        [KernelFunction]
        [Description("Update an existing key result task's information")]
        public async Task<KeyResultTaskUpdateResponse> UpdateKeyResultTaskAsync(
            [Description("The ID of the task to update")] string taskId,
            [Description("The current title of the task if ID is not available")] string title = null,
            [Description("The new title for the task (optional)")] string newTitle = null,
            [Description("The new description for the task (optional)")] string description = null,
            [Description("The new key result ID for the task (optional)")] string keyResultId = null,
            [Description("The new key result title if ID is not available (optional)")] string keyResultTitle = null,
            [Description("Optional new start date in format yyyy-MM-dd")] string startDate = null,
            [Description("Optional new end date in format yyyy-MM-dd")] string endDate = null,
            [Description("Optional new collaborator ID who is assigned to work on the task")] string collaboratorId = null,
            [Description("Optional new collaborator name if ID is not provided")] string collaboratorName = null,
            [Description("Optional new progress percentage (0-100)")] int? progress = null,
            [Description("Optional new priority (Low, Medium, High, Critical)")] string priority = null)
        {
            try
            {
                // Get user context directly from the accessor
                var userContext = _userContextAccessor.CurrentUserContext;
                
                _logger.LogInformation("KeyResultTaskPlugin.UpdateKeyResultTaskAsync called - TaskId: {TaskId}, Title: {Title}, User: {UserId}, Role: {UserRole}", 
                    taskId, title, userContext?.UserId, userContext?.Role);

                // Create a scope to resolve the scoped service
                using var scope = _serviceProvider.CreateScope();
                var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
                
                // Check update permission
                _logger.LogDebug("Checking permission {Permission}", Permissions.KeyResultTasks_Update);
                
                var unauthorizedResponse = await permissionService.CheckPermissionAsync<KeyResultTaskUpdateResponse>(
                    Permissions.KeyResultTasks_Update,
                    "task", 
                    "update",
                    (message) => new KeyResultTaskUpdateResponse 
                    { 
                        PromptTemplate = message,
                        KeyResultTaskId = taskId
                    });
                
                // If unauthorized, return the response
                if (unauthorizedResponse != null)
                {
                    _logger.LogWarning("Permission denied for user {UserId} to update task. Message: {Message}", 
                        userContext?.UserId, unauthorizedResponse.PromptTemplate);
                    return unauthorizedResponse;
                }
                
                _logger.LogInformation("Permission check passed for user {UserId} to update task", userContext?.UserId);
                
                _logger.LogDebug("UpdateKeyResultTaskAsync called with taskId: '{TaskId}', title: '{Title}', newTitle: '{NewTitle}'", 
                    taskId, title, newTitle);
                
                // If we have a title but no ID, try to search for the task by title and other criteria
                if (string.IsNullOrEmpty(taskId) && !string.IsNullOrEmpty(title))
                {
                    _logger.LogInformation("Task ID not provided, searching for task by title: {Title}", title);
                    
                    try
                    {
                        // Try to find key result ID from title if provided
                        string searchKeyResultId = null;
                        if (!string.IsNullOrEmpty(keyResultTitle) && string.IsNullOrEmpty(keyResultId))
                        {
                            var keyResultSearchResults = await _keyResultPlugin.SearchKeyResultsAsync(keyResultTitle);
                            if (keyResultSearchResults.KeyResults.Count > 0)
                            {
                                searchKeyResultId = keyResultSearchResults.KeyResults[0].KeyResultId;
                            }
                        }
                        else if (!string.IsNullOrEmpty(keyResultId))
                        {
                            searchKeyResultId = keyResultId;
                        }
                        
                        // Search for tasks with this title
                        var searchResults = await SearchKeyResultTasksAsync(title, searchKeyResultId);
                        
                        if (searchResults.KeyResultTasks.Count == 1)
                        {
                            // We found exactly one matching task
                            taskId = searchResults.KeyResultTasks[0].KeyResultTaskId;
                            _logger.LogInformation("Found task ID {TaskId} for title '{Title}'", taskId, title);
                        }
                        else if (searchResults.KeyResultTasks.Count > 1)
                        {
                            throw new ArgumentException($"Found {searchResults.KeyResultTasks.Count} tasks with title '{title}'. Please specify which one using the task ID.");
                        }
                        else
                        {
                            throw new ArgumentException($"No task found with title '{title}'.");
                        }
                    }
                    catch (Exception ex) when (!(ex is ArgumentException))
                    {
                        _logger.LogWarning(ex, "Error searching for task with title '{Title}'", title);
                        throw new ApplicationException($"Could not find task by title '{title}': {ex.Message}");
                    }
                }
                
                // At this point we must have a taskId
                if (string.IsNullOrEmpty(taskId))
                {
                    throw new ArgumentNullException(nameof(taskId), "Task ID is required to update a task.");
                }

                // If we have a key result title but no ID, try to search for the key result by title
                if (string.IsNullOrEmpty(keyResultId) && !string.IsNullOrEmpty(keyResultTitle))
                {
                    _logger.LogInformation("Key result ID not provided, searching for key result by title: {Title}", keyResultTitle);
                    
                    try
                    {
                        var searchResults = await _keyResultPlugin.SearchKeyResultsAsync(keyResultTitle);
                        if (searchResults.KeyResults.Count > 0)
                        {
                            keyResultId = searchResults.KeyResults[0].KeyResultId;
                            _logger.LogInformation("Found key result with ID {KeyResultId} for title '{Title}'", keyResultId, keyResultTitle);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error searching for key result with title '{Title}'", keyResultTitle);
                    }
                }

                // If we have a collaborator name but no ID, try to find the user by name
                if (string.IsNullOrEmpty(collaboratorId) && !string.IsNullOrEmpty(collaboratorName))
                {
                    _logger.LogInformation("Collaborator ID not provided, searching for user by name: {Name}", collaboratorName);
                    
                    try
                    {
                        var searchResults = await _userPlugin.SearchUsersByNameAsync(collaboratorName);
                        if (searchResults.Users.Count > 0)
                        {
                            collaboratorId = searchResults.Users[0].UserId;
                            _logger.LogInformation("Found collaborator with ID {CollaboratorId} for name '{Name}'", collaboratorId, collaboratorName);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error searching for collaborator by name '{Name}'", collaboratorName);
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

                var keyResultTaskMediatRService = scope.ServiceProvider.GetRequiredService<KeyResultTaskMediatRService>();
                
                // Get the current task to populate what's missing
                var currentTask = await keyResultTaskMediatRService.GetKeyResultTaskDetailsAsync(taskId);
                
                // Create a request object for the MediatR service
                var request = new KeyResultTaskUpdateRequest
                {
                    Title = newTitle ?? title, // Use new title if provided, otherwise use title if provided, otherwise null
                    Description = description,
                    KeyResultId = keyResultId,
                    StartedDate = startDateTime,
                    EndDate = endDateTime,
                    Progress = progress,
                    Priority = priority,
                    UserId = userContext?.UserId // Add the current user ID from context
                };

                // Call the MediatR service to update the task
                var result = await keyResultTaskMediatRService.UpdateKeyResultTaskAsync(taskId, request);
                _logger.LogInformation("Key result task updated successfully: {TaskId}", result.KeyResultTaskId);
                
                // Generate prompt template
                Dictionary<string, string> templateValues = new()
                {
                    { "taskTitle", result.Title },
                    { "keyResultTitle", result.KeyResultTitle }
                };
                
                result.PromptTemplate = _promptTemplateService.GetPrompt("KeyResultTaskUpdated", templateValues);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating key result task");
                throw;
            }
        }

        /// <summary>
        /// Delete a key result task from the system
        /// </summary>
        [KernelFunction]
        [Description("Delete a key result task from the system")]
        public async Task<KeyResultTaskDeleteResponse> DeleteKeyResultTaskAsync(
            [Description("The ID of the task to delete")] string taskId,
            [Description("The title of the task to delete if ID is not available")] string title = null)
        {
            try
            {
                // Get user context directly from the accessor
                var userContext = _userContextAccessor.CurrentUserContext;
                
                _logger.LogInformation("KeyResultTaskPlugin.DeleteKeyResultTaskAsync called - TaskId: {TaskId}, Title: {Title}, User: {UserId}, Role: {UserRole}", 
                    taskId, title, userContext?.UserId, userContext?.Role);

                // Create a scope to resolve the scoped service
                using var scope = _serviceProvider.CreateScope();
                var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
                
                // Check delete permission
                _logger.LogDebug("Checking permission {Permission}", Permissions.KeyResultTasks_Delete);
                
                var unauthorizedResponse = await permissionService.CheckPermissionAsync<KeyResultTaskDeleteResponse>(
                    Permissions.KeyResultTasks_Delete,
                    "task", 
                    "delete",
                    (message) => new KeyResultTaskDeleteResponse 
                    { 
                        PromptTemplate = message,
                        KeyResultTaskId = taskId
                    });
                
                // If unauthorized, return the response
                if (unauthorizedResponse != null)
                {
                    _logger.LogWarning("Permission denied for user {UserId} to delete task. Message: {Message}", 
                        userContext?.UserId, unauthorizedResponse.PromptTemplate);
                    return unauthorizedResponse;
                }
                
                _logger.LogInformation("Permission check passed for user {UserId} to delete task", userContext?.UserId);
                
                _logger.LogDebug("DeleteKeyResultTaskAsync called with taskId: '{TaskId}', title: '{Title}'", taskId, title);
                
                // If we have a title but no ID, try to search for the task by title
                if (string.IsNullOrEmpty(taskId) && !string.IsNullOrEmpty(title))
                {
                    _logger.LogInformation("Task ID not provided, attempting to find task by title: {Title}", title);
                    
                    try
                    {
                        // Search for tasks with this title
                        var searchResults = await SearchKeyResultTasksAsync(title);
                        
                        if (searchResults.KeyResultTasks.Count == 1)
                        {
                            taskId = searchResults.KeyResultTasks[0].KeyResultTaskId;
                            _logger.LogInformation("Found task ID {TaskId} for task titled '{Title}'", taskId, title);
                        }
                        else if (searchResults.KeyResultTasks.Count > 1)
                        {
                            throw new ArgumentException($"Found {searchResults.KeyResultTasks.Count} tasks titled '{title}'. Please specify which one using the task ID.");
                        }
                        else
                        {
                            throw new ArgumentException($"No task found with title '{title}'.");
                        }
                    }
                    catch (Exception ex) when (!(ex is ArgumentException))
                    {
                        _logger.LogWarning(ex, "Error searching for task with title '{Title}'", title);
                        throw new ApplicationException($"Could not find task by title '{title}': {ex.Message}");
                    }
                }
                
                // At this point we must have a taskId
                if (string.IsNullOrEmpty(taskId))
                {
                    throw new ArgumentNullException(nameof(taskId), "Task ID is required to delete a task.");
                }

                var keyResultTaskMediatRService = scope.ServiceProvider.GetRequiredService<KeyResultTaskMediatRService>();
                
                // Delete the task and get the response
                var result = await keyResultTaskMediatRService.DeleteKeyResultTaskAsync(taskId);
                
                // Generate the prompt template for the response
                Dictionary<string, string> templateValues = new()
                {
                    { "taskTitle", result.Title },
                    { "keyResultTitle", result.KeyResultTitle }
                };
                
                result.PromptTemplate = _promptTemplateService.GetPrompt("KeyResultTaskDeleted", templateValues);
                
                _logger.LogInformation("Key result task deleted successfully: {TaskId}, {Title}", result.KeyResultTaskId, result.Title);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting key result task with ID '{TaskId}' or title '{Title}'", taskId, title);
                throw;
            }
        }

        /// <summary>
        /// Get details of a specific key result task
        /// </summary>
        [KernelFunction]
        [Description("Get details of a specific key result task by ID")]
        public async Task<KeyResultTaskDetailsResponse> GetKeyResultTaskDetailsAsync(
            [Description("ID of the task to retrieve")] string taskId,
            [Description("Title of the task if ID is not available")] string title = null)
        {
            try
            {
                // Get user context directly from the accessor
                var userContext = _userContextAccessor.CurrentUserContext;
                
                _logger.LogInformation("KeyResultTaskPlugin.GetKeyResultTaskDetailsAsync called - TaskId: {TaskId}, Title: {Title}, User: {UserId}, Role: {UserRole}", 
                    taskId, title, userContext?.UserId, userContext?.Role);

                // Create a scope to resolve the scoped service
                using var scope = _serviceProvider.CreateScope();
                var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
                
                // Check view permission
                _logger.LogDebug("Checking permission {Permission}", Permissions.KeyResultTasks_GetById);
                
                var unauthorizedResponse = await permissionService.CheckPermissionAsync<KeyResultTaskDetailsResponse>(
                    Permissions.KeyResultTasks_GetById,
                    "task", 
                    "view",
                    (message) => new KeyResultTaskDetailsResponse 
                    { 
                        PromptTemplate = message,
                        KeyResultTaskId = taskId
                    });
                
                // If unauthorized, return the response
                if (unauthorizedResponse != null)
                {
                    _logger.LogWarning("Permission denied for user {UserId} to view task details. Message: {Message}", 
                        userContext?.UserId, unauthorizedResponse.PromptTemplate);
                    return unauthorizedResponse;
                }
                
                _logger.LogInformation("Permission check passed for user {UserId} to view task details", userContext?.UserId);
                
                _logger.LogInformation("Getting details for task with ID: {TaskId} or title: {Title}", taskId, title);
                
                var keyResultTaskMediatRService = scope.ServiceProvider.GetRequiredService<KeyResultTaskMediatRService>();
                
                // If we have a title but no ID, try to search for the task by title
                if (string.IsNullOrEmpty(taskId) && !string.IsNullOrEmpty(title))
                {
                    var searchResults = await SearchKeyResultTasksAsync(title);
                    
                    if (searchResults.KeyResultTasks.Count == 1)
                    {
                        // We found exactly one matching task, use its ID
                        taskId = searchResults.KeyResultTasks[0].KeyResultTaskId;
                        _logger.LogInformation("Found task ID {TaskId} for title '{Title}'", taskId, title);
                    }
                    else if (searchResults.KeyResultTasks.Count > 1)
                    {
                        throw new ArgumentException($"Found {searchResults.KeyResultTasks.Count} tasks with title '{title}'. Please specify which one using the task ID.");
                    }
                    else
                    {
                        throw new ArgumentException($"No task found with title '{title}'.");
                    }
                }
                
                // At this point we must have a taskId
                if (string.IsNullOrEmpty(taskId))
                {
                    throw new ArgumentException("Either task ID or title must be provided.");
                }
                
                var result = await keyResultTaskMediatRService.GetKeyResultTaskDetailsAsync(taskId);
                
                // Generate prompt template for the response
                Dictionary<string, string> templateValues = new()
                {
                    { "taskTitle", result.Title },
                    { "keyResultTitle", result.KeyResultTitle },
                    { "progress", result.Progress.ToString() },
                    { "endDate", result.EndDate.ToString("yyyy-MM-dd") }
                };
                
                // Add description if available
                if (!string.IsNullOrEmpty(result.Description))
                {
                    templateValues["description"] = result.Description;
                    result.PromptTemplate = _promptTemplateService.GetPrompt("KeyResultTaskDetailsWithDescription", templateValues);
                }
                else
                {
                    result.PromptTemplate = _promptTemplateService.GetPrompt("KeyResultTaskDetails", templateValues);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting key result task details for ID: {TaskId} or title: {Title}", taskId, title);
                throw new ApplicationException($"Failed to get key result task details: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Search for key result tasks with optional filter criteria
        /// </summary>
        [KernelFunction]
        [Description("Search for key result tasks by title, key result, or user")]
        public async Task<KeyResultTaskSearchResponse> SearchKeyResultTasksAsync(
            [Description("Optional title to filter tasks by")] string title = null,
            [Description("Optional key result ID to filter tasks by")] string keyResultId = null,
            [Description("Optional key result title if ID is not available")] string keyResultTitle = null)
        {
            try
            {
                // Get user context directly from the accessor
                var userContext = _userContextAccessor.CurrentUserContext;
                
                _logger.LogInformation("KeyResultTaskPlugin.SearchKeyResultTasksAsync called - Title: {Title}, User: {UserId}, Role: {UserRole}", 
                    title, userContext?.UserId, userContext?.Role);

                // Create a scope to resolve the scoped service
                using var scope = _serviceProvider.CreateScope();
                var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
                
                // Check view permission
                _logger.LogDebug("Checking permission {Permission}", Permissions.KeyResultTasks_GetAll);
                
                var unauthorizedResponse = await permissionService.CheckPermissionAsync<KeyResultTaskSearchResponse>(
                    Permissions.KeyResultTasks_GetAll,
                    "task", 
                    "search",
                    (message) => new KeyResultTaskSearchResponse 
                    { 
                        PromptTemplate = message,
                        SearchTerm = title
                    });
                
                // If unauthorized, return the response
                if (unauthorizedResponse != null)
                {
                    _logger.LogWarning("Permission denied for user {UserId} to search tasks. Message: {Message}", 
                        userContext?.UserId, unauthorizedResponse.PromptTemplate);
                    return unauthorizedResponse;
                }
                
                _logger.LogInformation("Permission check passed for user {UserId} to search tasks", userContext?.UserId);
                
                _logger.LogInformation("Searching for key result tasks with title: {Title}, keyResultId: {KeyResultId}, userId: {UserId}", 
                    title, keyResultId, userContext?.UserId);
                
                // If we have a key result title but no ID, try to search for the key result by title
                if (string.IsNullOrEmpty(keyResultId) && !string.IsNullOrEmpty(keyResultTitle))
                {
                    _logger.LogInformation("Key result ID not provided, searching for key result by title: {Title}", keyResultTitle);
                    
                    try
                    {
                        var keyResultSearchResults = await _keyResultPlugin.SearchKeyResultsAsync(keyResultTitle);
                        if (keyResultSearchResults.KeyResults.Count > 0)
                        {
                            keyResultId = keyResultSearchResults.KeyResults[0].KeyResultId;
                            _logger.LogInformation("Found key result with ID {KeyResultId} for title '{Title}'", keyResultId, keyResultTitle);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error searching for key result with title '{Title}'", keyResultTitle);
                    }
                }
                    
                var keyResultTaskMediatRService = scope.ServiceProvider.GetRequiredService<KeyResultTaskMediatRService>();
                
                // Use the MediatR service to search for tasks
                var result = await keyResultTaskMediatRService.SearchKeyResultTasksAsync(title, keyResultId, userContext?.UserId);
                
                // Generate the tasks list for the prompt template with more detailed information
                var tasksListBuilder = new StringBuilder();
                for (int i = 0; i < result.KeyResultTasks.Count; i++)
                {
                    var task = result.KeyResultTasks[i];
                    tasksListBuilder.AppendLine($"{i+1}. {task.Title} (ID: {task.KeyResultTaskId})");
                    tasksListBuilder.AppendLine($"   Key Result: {task.KeyResultTitle}");
                    tasksListBuilder.AppendLine($"   Progress: {task.Progress}%");
                    tasksListBuilder.AppendLine($"   Due: {task.EndDate:yyyy-MM-dd}");
                    tasksListBuilder.AppendLine($"   Priority: {task.Priority}");
                    
                    // Add a separator line between tasks (except after the last one)
                    if (i < result.KeyResultTasks.Count - 1)
                    {
                        tasksListBuilder.AppendLine();
                    }
                }
                
                // Generate prompt template
                Dictionary<string, string> templateValues = new()
                {
                    { "count", result.Count.ToString() },
                    { "searchTerm", !string.IsNullOrEmpty(title) ? title : "(any)" },
                    { "keyResultId", !string.IsNullOrEmpty(keyResultId) ? keyResultId : "(any)" },
                    { "tasksList", tasksListBuilder.ToString().Trim() }
                };
                
                // Use different template for empty results
                if (result.KeyResultTasks.Count == 0)
                {
                    result.PromptTemplate = _promptTemplateService.GetPrompt("KeyResultTaskSearchResultsEmpty", templateValues);
                }
                else
                {
                    result.PromptTemplate = _promptTemplateService.GetPrompt("KeyResultTaskSearchResults", templateValues);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for key result tasks: {ErrorMessage}", ex.Message);
                throw new ApplicationException($"Failed to search for key result tasks: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get all tasks for a specific key result
        /// </summary>
        [KernelFunction]
        [Description("Get all tasks for a specific key result")]
        public async Task<KeyResultTasksByKeyResultResponse> GetKeyResultTasksByKeyResultIdAsync(
            [Description("The ID of the key result to get tasks for")] string keyResultId,
            [Description("The title of the key result if ID is not available")] string keyResultTitle = null)
        {
            try
            {
                // Get user context directly from the accessor
                var userContext = _userContextAccessor.CurrentUserContext;
                
                _logger.LogInformation("KeyResultTaskPlugin.GetKeyResultTasksByKeyResultIdAsync called - KeyResultId: {KeyResultId}, Title: {KeyResultTitle}, User: {UserId}, Role: {UserRole}", 
                    keyResultId, keyResultTitle, userContext?.UserId, userContext?.Role);

                // Create a scope to resolve the scoped service
                using var scope = _serviceProvider.CreateScope();
                var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
                
                // Check view permission
                _logger.LogDebug("Checking permission {Permission}", Permissions.KeyResultTasks_GetAll);
                
                var unauthorizedResponse = await permissionService.CheckPermissionAsync<KeyResultTasksByKeyResultResponse>(
                    Permissions.KeyResultTasks_GetAll,
                    "task", 
                    "view by key result",
                    (message) => new KeyResultTasksByKeyResultResponse 
                    { 
                        PromptTemplate = message,
                        KeyResultId = keyResultId
                    });
                
                // If unauthorized, return the response
                if (unauthorizedResponse != null)
                {
                    _logger.LogWarning("Permission denied for user {UserId} to view tasks by key result. Message: {Message}", 
                        userContext?.UserId, unauthorizedResponse.PromptTemplate);
                    return unauthorizedResponse;
                }
                
                _logger.LogInformation("Permission check passed for user {UserId} to view tasks by key result", userContext?.UserId);
                
                _logger.LogInformation("Getting tasks for key result with ID: {KeyResultId} or title: {KeyResultTitle}", 
                    keyResultId, keyResultTitle);
                
                var keyResultTaskMediatRService = scope.ServiceProvider.GetRequiredService<KeyResultTaskMediatRService>();
                
                // If we have a key result title but no ID, try to find the key result by title
                if (string.IsNullOrEmpty(keyResultId) && !string.IsNullOrEmpty(keyResultTitle))
                {
                    var searchResults = await _keyResultPlugin.SearchKeyResultsAsync(keyResultTitle);
                    
                    if (searchResults.KeyResults.Count > 0)
                    {
                        keyResultId = searchResults.KeyResults[0].KeyResultId;
                        _logger.LogInformation("Found key result with ID {KeyResultId} for title '{Title}'", keyResultId, keyResultTitle);
                    }
                    else
                    {
                        throw new ArgumentException($"No key result found with title '{keyResultTitle}'.");
                    }
                }
                
                // At this point we must have a keyResultId
                if (string.IsNullOrEmpty(keyResultId))
                {
                    throw new ArgumentException("Either key result ID or title must be provided.");
                }
                
                var result = await keyResultTaskMediatRService.GetKeyResultTasksByKeyResultIdAsync(keyResultId);
                
                // Generate prompt template based on the result
                // The MediatR service already sets a default prompt template, but you may want to customize further here
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tasks by key result ID: {KeyResultId}", keyResultId);
                throw new ApplicationException($"Failed to get tasks for key result: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Helper method to ensure DateTime values are in UTC format
        /// </summary>
        private DateTime EnsureUtc(DateTime dateTime)
        {
            return dateTime.Kind == DateTimeKind.Unspecified 
                ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc) 
                : dateTime.ToUniversalTime();
        }
    }
}