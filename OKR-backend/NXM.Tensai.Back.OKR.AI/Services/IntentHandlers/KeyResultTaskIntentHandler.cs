using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NXM.Tensai.Back.OKR.AI.Core.AI.Plugins;
using NXM.Tensai.Back.OKR.AI.Models;

namespace NXM.Tensai.Back.OKR.AI.Services.IntentHandlers
{
    /// <summary>
    /// Handler for key result task-related intents
    /// </summary>
    public class KeyResultTaskIntentHandler : BaseIntentHandler
    {
        private readonly KeyResultTaskPlugin _keyResultTaskPlugin;
        private static readonly HashSet<string> _supportedIntents = new()
        {
            "CreateKeyResultTask",
            "UpdateKeyResultTask",
            "DeleteKeyResultTask",
            "GetKeyResultTaskInfo",
            "SearchKeyResultTasks",
            "GetAllKeyResultTasks",
            "GetKeyResultTasksByKeyResult"
        };
        
        public KeyResultTaskIntentHandler(
            KeyResultTaskPlugin keyResultTaskPlugin, 
            KernelService kernelService,
            ILogger<KeyResultTaskIntentHandler> logger) : base(kernelService, logger)
        {
            _keyResultTaskPlugin = keyResultTaskPlugin;
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
                "CreateKeyResultTask" => await HandleCreateKeyResultTaskAsync(conversationId, parameters, userContext),
                "UpdateKeyResultTask" => await HandleUpdateKeyResultTaskAsync(conversationId, parameters, userContext),
                "DeleteKeyResultTask" => await HandleDeleteKeyResultTaskAsync(conversationId, parameters, userContext),
                "GetKeyResultTaskInfo" => await HandleGetKeyResultTaskInfoAsync(conversationId, parameters, userContext),
                "SearchKeyResultTasks" => await HandleSearchKeyResultTasksAsync(conversationId, parameters, userContext),
                "GetAllKeyResultTasks" => await HandleGetAllKeyResultTasksAsync(conversationId, parameters, userContext),
                "GetKeyResultTasksByKeyResult" => await HandleGetKeyResultTasksByKeyResultAsync(conversationId, parameters, userContext),
                _ => CreateErrorResult($"Unsupported key result task intent: {intent}")
            };
        }
        
        private async Task<FunctionExecutionResult> HandleCreateKeyResultTaskAsync(
            string conversationId,
            Dictionary<string, string> parameters,
            UserContext userContext)
        {
            var title = parameters.GetValueOrDefault("title", "");
            if (string.IsNullOrEmpty(title))
            {
                return CreateErrorResult("Task title is required to create a key result task.");
            }

            var keyResultId = parameters.GetValueOrDefault("keyResultId", null);
            var keyResultTitle = parameters.GetValueOrDefault("keyResultTitle", null);

            // Check if we have either keyResultId or keyResultTitle
            if (string.IsNullOrEmpty(keyResultId) && string.IsNullOrEmpty(keyResultTitle))
            {
                // Try to get the key result ID from conversation history
                keyResultId = KernelService.GetMostRecentEntityId(conversationId, "KeyResult");
                Logger.LogInformation("No key result specified, using most recent key result ID from history: {KeyResultId}", keyResultId);

                if (string.IsNullOrEmpty(keyResultId))
                {
                    return CreateErrorResult("Could not determine which key result this task belongs to. Please provide a key result.");
                }
            }

            var taskResult = await _keyResultTaskPlugin.CreateKeyResultTaskAsync(
                title,
                keyResultId,
                keyResultTitle,
                parameters.GetValueOrDefault("description", null),
                parameters.GetValueOrDefault("startDate", null),
                parameters.GetValueOrDefault("endDate", null),
                parameters.GetValueOrDefault("collaboratorId", null),
                parameters.GetValueOrDefault("collaboratorName", null),
                ParseIntParameter(parameters, "progress"),
                parameters.GetValueOrDefault("priority", null));

            return CreateSuccessResult(
                taskResult,
                "KeyResultTask",
                taskResult.KeyResultTaskId,
                "Create",
                taskResult.PromptTemplate);
        }
        
        private async Task<FunctionExecutionResult> HandleUpdateKeyResultTaskAsync(
            string conversationId,
            Dictionary<string, string> parameters,
            UserContext userContext)
        {
            // Get parameters
            var taskId = parameters.GetValueOrDefault("taskId", null);
            var taskTitle = parameters.GetValueOrDefault("title", null);
            
            // If we have no taskId and no title, try to use the most recent task
            if (string.IsNullOrEmpty(taskId) && string.IsNullOrEmpty(taskTitle))
            {
                taskId = KernelService.GetMostRecentEntityId(conversationId, "KeyResultTask");
                Logger.LogInformation("No task specified, using most recent task ID from history: {TaskId}", taskId);
                
                if (string.IsNullOrEmpty(taskId))
                {
                    return CreateErrorResult("Could not find a task to update. Please specify which task you want to update.");
                }
            }

            // Get the new title if provided
            var newTitle = parameters.GetValueOrDefault("newTitle", null);
            
            // Get collaborator ID or name
            var collaboratorId = parameters.GetValueOrDefault("collaboratorId", null);
            var collaboratorName = parameters.GetValueOrDefault("collaboratorName", null);

            // Log the collaborator information
            if (!string.IsNullOrEmpty(collaboratorName))
            {
                Logger.LogInformation("Collaborator name provided: {CollaboratorName}", collaboratorName);
            }

            // Get key result ID or title
            var keyResultId = parameters.GetValueOrDefault("keyResultId", null);
            var keyResultTitle = parameters.GetValueOrDefault("keyResultTitle", null);

            // Let the plugin handle the task update logic
            var updateResult = await _keyResultTaskPlugin.UpdateKeyResultTaskAsync(
                taskId,
                taskTitle,
                newTitle,
                parameters.GetValueOrDefault("description", null),
                keyResultId,
                keyResultTitle,
                parameters.GetValueOrDefault("startDate", null),
                parameters.GetValueOrDefault("endDate", null),
                collaboratorId,
                collaboratorName,
                ParseIntParameter(parameters, "progress"),
                parameters.GetValueOrDefault("priority", null));

            return CreateSuccessResult(
                updateResult,
                "KeyResultTask",
                updateResult.KeyResultTaskId,
                "Update",
                updateResult.PromptTemplate);
        }
        
        private async Task<FunctionExecutionResult> HandleDeleteKeyResultTaskAsync(
            string conversationId,
            Dictionary<string, string> parameters,
            UserContext userContext)
        {
            // Get parameters
            var taskId = parameters.GetValueOrDefault("taskId", null);
            var title = parameters.GetValueOrDefault("title", null);
            
            // If no taskId or title, try to get from recent history
            if (string.IsNullOrEmpty(taskId) && string.IsNullOrEmpty(title))
            {
                taskId = KernelService.GetMostRecentEntityId(conversationId, "KeyResultTask");
                Logger.LogInformation("Retrieved most recent task ID from history for delete operation: {TaskId}", taskId);
                
                if (string.IsNullOrEmpty(taskId))
                {
                    return CreateErrorResult("Could not find the task to delete. Please specify which task you want to delete.");
                }
            }

            // Delete the task - plugin will handle searching by title if needed
            var deleteResult = await _keyResultTaskPlugin.DeleteKeyResultTaskAsync(taskId, title);

            return CreateSuccessResult(
                deleteResult,
                "KeyResultTask",
                deleteResult.KeyResultTaskId,
                "Delete",
                deleteResult.PromptTemplate);
        }
        
        private async Task<FunctionExecutionResult> HandleGetKeyResultTaskInfoAsync(
            string conversationId,
            Dictionary<string, string> parameters,
            UserContext userContext)
        {
            // Get parameters
            var taskId = parameters.GetValueOrDefault("taskId", null);
            var title = parameters.GetValueOrDefault("title", null);
            
            // If no taskId or title, try to get from recent history
            if (string.IsNullOrEmpty(taskId) && string.IsNullOrEmpty(title))
            {
                taskId = KernelService.GetMostRecentEntityId(conversationId, "KeyResultTask");
                Logger.LogInformation("Retrieved most recent task ID from history for details: {TaskId}", taskId);
                
                if (string.IsNullOrEmpty(taskId))
                {
                    return CreateErrorResult("Could not find the task to get details for. Please specify which task you want information about.");
                }
            }
            
            // Get the task details - plugin will handle searching by title if needed
            var taskDetails = await _keyResultTaskPlugin.GetKeyResultTaskDetailsAsync(taskId, title);

            return CreateSuccessResult(
                taskDetails,
                "KeyResultTask",
                taskDetails.KeyResultTaskId,
                "Get Details",
                taskDetails.PromptTemplate);
        }
        
        private async Task<FunctionExecutionResult> HandleSearchKeyResultTasksAsync(
            string conversationId,
            Dictionary<string, string> parameters,
            UserContext userContext)
        {
            // Extract search parameters
            var title = parameters.GetValueOrDefault("title", null);
            var keyResultId = parameters.GetValueOrDefault("keyResultId", null);
            var keyResultTitle = parameters.GetValueOrDefault("keyResultTitle", null);
            var userId = parameters.GetValueOrDefault("userId", null);
            
            // Perform the search
            var searchResult = await _keyResultTaskPlugin.SearchKeyResultTasksAsync(title, keyResultId, keyResultTitle);
            
            // Handle empty search results properly
            return CreateSuccessResult(
                searchResult,
                "KeyResultTask",
                searchResult.KeyResultTasks.Count > 0 ? searchResult.KeyResultTasks[0].KeyResultTaskId : null,
                "Search tasks by criteria",
                searchResult.PromptTemplate);
        }
        
        private async Task<FunctionExecutionResult> HandleGetAllKeyResultTasksAsync(
            string conversationId,
            Dictionary<string, string> parameters,
            UserContext userContext)
        {
            // Get all tasks (with no filtering)
            var tasksResult = await _keyResultTaskPlugin.SearchKeyResultTasksAsync();
            
            return CreateSuccessResult(
                tasksResult,
                "KeyResultTask",
                null,
                "List All Tasks",
                tasksResult.PromptTemplate);
        }
        
        private async Task<FunctionExecutionResult> HandleGetKeyResultTasksByKeyResultAsync(
            string conversationId,
            Dictionary<string, string> parameters,
            UserContext userContext)
        {
            // Get key result ID or title
            var keyResultId = parameters.GetValueOrDefault("keyResultId", null);
            var keyResultTitle = parameters.GetValueOrDefault("keyResultTitle", null);
            
            // If no keyResultId or keyResultTitle, try to get from recent history
            if (string.IsNullOrEmpty(keyResultId) && string.IsNullOrEmpty(keyResultTitle))
            {
                keyResultId = KernelService.GetMostRecentEntityId(conversationId, "KeyResult");
                Logger.LogInformation("Retrieved most recent key result ID from history: {KeyResultId}", keyResultId);
                
                if (string.IsNullOrEmpty(keyResultId))
                {
                    return CreateErrorResult("Could not determine which key result to get tasks for. Please specify a key result.");
                }
            }

            // Get tasks for this key result
            var tasksByKeyResult = await _keyResultTaskPlugin.GetKeyResultTasksByKeyResultIdAsync(keyResultId, keyResultTitle);
            
            return CreateSuccessResult(
                tasksByKeyResult,
                "KeyResult",
                tasksByKeyResult.KeyResultId,
                "List Tasks By Key Result",
                tasksByKeyResult.PromptTemplate);
        }

        /// <summary>
        /// Helper method to safely parse an integer parameter from the parameters dictionary
        /// </summary>
        private int? ParseIntParameter(Dictionary<string, string> parameters, string paramName)
        {
            if (parameters.TryGetValue(paramName, out var value) && !string.IsNullOrEmpty(value))
            {
                if (int.TryParse(value, out var intValue))
                {
                    return intValue;
                }
            }
            return null;
        }
    }

    /// <summary>
    /// Intent models for key result task operations
    /// </summary>
    [Intent("CreateKeyResultTask", "POST", "Create a new task for a key result")]
    public class CreateKeyResultTaskIntent : IIntentModel
    {
        [IntentParameter("The title of the task to create", true, "Market Research")]
        public string Title { get; set; }

        [IntentParameter("The ID of the key result this task belongs to")]
        public string KeyResultId { get; set; }
        
        [IntentParameter("The title of the key result if ID is not provided")]
        public string KeyResultTitle { get; set; }
        
        [IntentParameter("Optional description of the task")]
        public string Description { get; set; }
        
        [IntentParameter("Optional start date in format yyyy-MM-dd")]
        public string StartDate { get; set; }
        
        [IntentParameter("Optional end date in format yyyy-MM-dd")]
        public string EndDate { get; set; }
        
        [IntentParameter("Optional user ID who owns the task")]
        public string UserId { get; set; }
        
        [IntentParameter("Optional collaborator ID who is assigned to work on the task")]
        public string CollaboratorId { get; set; }
        
        [IntentParameter("Optional collaborator name if ID is not provided")]
        public string CollaboratorName { get; set; }
        
        [IntentParameter("Optional initial progress percentage (0-100)")]
        public string Progress { get; set; }
        
        [IntentParameter("Optional priority (Low, Medium, High, Critical)")]
        public string Priority { get; set; }
    }
    
    [Intent("UpdateKeyResultTask", "PUT", "Update an existing key result task")]
    public class UpdateKeyResultTaskIntent : IIntentModel
    {
        [IntentParameter("The ID of the task to update")]
        public string TaskId { get; set; }
        
        [IntentParameter("The current title of the task if ID is not available")]
        public string Title { get; set; }
        
        [IntentParameter("The new title for the task (optional)")]
        public string NewTitle { get; set; }
        
        [IntentParameter("The new description for the task (optional)")]
        public string Description { get; set; }
        
        [IntentParameter("The new key result ID for the task (optional)")]
        public string KeyResultId { get; set; }
        
        [IntentParameter("The new key result title if ID is not available (optional)")]
        public string KeyResultTitle { get; set; }
        
        [IntentParameter("Optional new start date in format yyyy-MM-dd")]
        public string StartDate { get; set; }
        
        [IntentParameter("Optional new end date in format yyyy-MM-dd")]
        public string EndDate { get; set; }
        
        [IntentParameter("Optional new collaborator ID who is assigned to work on the task")]
        public string CollaboratorId { get; set; }
        
        [IntentParameter("Optional new collaborator name if ID is not provided")]
        public string CollaboratorName { get; set; }
        
        [IntentParameter("Optional new progress percentage (0-100)")]
        public string Progress { get; set; }
        
        [IntentParameter("Optional new priority (Low, Medium, High, Critical)")]
        public string Priority { get; set; }
    }
    
    [Intent("DeleteKeyResultTask", "DELETE", "Delete a key result task")]
    public class DeleteKeyResultTaskIntent : IIntentModel
    {
        [IntentParameter("The ID of the task to delete")]
        public string TaskId { get; set; }
        
        [IntentParameter("The title of the task to delete if ID is not available")]
        public string Title { get; set; }
    }
    
    [Intent("GetKeyResultTaskInfo", "GET", "Get details of a specific key result task")]
    public class GetKeyResultTaskInfoIntent : IIntentModel
    {
        [IntentParameter("ID of the task to retrieve")]
        public string TaskId { get; set; }
        
        [IntentParameter("Title of the task if ID is not available")]
        public string Title { get; set; }
    }
    
    [Intent("SearchKeyResultTasks", "GET", "Search for tasks with specific criteria")]
    public class SearchKeyResultTasksIntent : IIntentModel
    {
        [IntentParameter("Optional title to filter tasks by")]
        public string Title { get; set; }
        
        [IntentParameter("Optional key result ID to filter tasks by")]
        public string KeyResultId { get; set; }
        
        [IntentParameter("Optional key result title if ID is not available")]
        public string KeyResultTitle { get; set; }
        
        [IntentParameter("Optional user ID to filter tasks by")]
        public string UserId { get; set; }
    }
    
    [Intent("GetAllKeyResultTasks", "GET", "Get all key result tasks")]
    public class GetAllKeyResultTasksIntent : IIntentModel
    {
        // No parameters needed
    }
    
    [Intent("GetKeyResultTasksByKeyResult", "GET", "Get all tasks for a specific key result")]
    public class GetKeyResultTasksByKeyResultIntent : IIntentModel
    {
        [IntentParameter("The ID of the key result to get tasks for")]
        public string KeyResultId { get; set; }
        
        [IntentParameter("The title of the key result if ID is not available")]
        public string KeyResultTitle { get; set; }
    }
}