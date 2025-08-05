using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NXM.Tensai.Back.OKR.AI.Core.AI.Plugins;
using NXM.Tensai.Back.OKR.AI.Models;

namespace NXM.Tensai.Back.OKR.AI.Services.IntentHandlers
{
    /// <summary>
    /// Handler for key result-related intents
    /// </summary>
    public class KeyResultIntentHandler : BaseIntentHandler
    {
        private readonly KeyResultPlugin _keyResultPlugin;
        private readonly ObjectivePlugin _objectivePlugin;
        private static readonly HashSet<string> _supportedIntents = new()
        {
            "CreateKeyResult",
            "UpdateKeyResult",
            "DeleteKeyResult",
            "GetKeyResultInfo",
            "SearchKeyResults",
            "GetKeyResultsByObjective",
            "GetAllKeyResults"
        };
        
        public KeyResultIntentHandler(
            KeyResultPlugin keyResultPlugin,
            ObjectivePlugin objectivePlugin,
            KernelService kernelService,
            ILogger<KeyResultIntentHandler> logger) : base(kernelService, logger)
        {
            _keyResultPlugin = keyResultPlugin ?? throw new ArgumentNullException(nameof(keyResultPlugin));
            _objectivePlugin = objectivePlugin ?? throw new ArgumentNullException(nameof(objectivePlugin));
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
                "CreateKeyResult" => await HandleCreateKeyResultAsync(conversationId, parameters, userContext),
                "UpdateKeyResult" => await HandleUpdateKeyResultAsync(conversationId, parameters, userContext),
                "DeleteKeyResult" => await HandleDeleteKeyResultAsync(conversationId, parameters, userContext),
                "GetKeyResultInfo" => await HandleGetKeyResultInfoAsync(conversationId, parameters, userContext),
                "SearchKeyResults" => await HandleSearchKeyResultsAsync(conversationId, parameters, userContext),
                "GetKeyResultsByObjective" => await HandleGetKeyResultsByObjectiveAsync(conversationId, parameters, userContext),
                "GetAllKeyResults" => await HandleGetAllKeyResultsAsync(conversationId, parameters, userContext),
                _ => CreateErrorResult($"Unsupported key result intent: {intent}")
            };
        }
        
        private async Task<FunctionExecutionResult> HandleCreateKeyResultAsync(
            string conversationId,
            Dictionary<string, string> parameters,
            UserContext userContext)
        {
            // Extract required parameters
            var title = parameters.GetValueOrDefault("title", null);
            if (string.IsNullOrEmpty(title))
            {
                return CreateErrorResult("Title is required to create a key result.");
            }
            
            // Extract other parameters
            var objectiveId = parameters.GetValueOrDefault("objectiveId", null);
            var objectiveTitle = parameters.GetValueOrDefault("objectiveTitle", null);
            
            // Need either objectiveId or objectiveTitle
            if (string.IsNullOrEmpty(objectiveId) && string.IsNullOrEmpty(objectiveTitle))
            {
                return CreateErrorResult("An objective ID or title is required to create a key result.");
            }
            
            // Extract optional parameters
            var description = parameters.GetValueOrDefault("description", null);
            var startDate = parameters.GetValueOrDefault("startDate", null);
            var endDate = parameters.GetValueOrDefault("endDate", null);
            var userId = parameters.GetValueOrDefault("userId", userContext?.UserId);
            var status = parameters.GetValueOrDefault("status", null);
            
            // Try to parse progress if provided
            int? progress = null;
            if (parameters.TryGetValue("progress", out var progressStr))
            {
                if (int.TryParse(progressStr, out var progressVal))
                {
                    progress = progressVal;
                }
            }
            
            // Create the key result
            var result = await _keyResultPlugin.CreateKeyResultAsync(
                title,
                objectiveId,
                objectiveTitle,
                startDate,
                endDate,
                description,
                status,
                progress);
                
            return CreateSuccessResult(
                result,
                "KeyResult",
                result.KeyResultId,
                "Create",
                result.PromptTemplate);
        }
        
        private async Task<FunctionExecutionResult> HandleUpdateKeyResultAsync(
            string conversationId,
            Dictionary<string, string> parameters,
            UserContext userContext)
        {
            // Get parameters
            var keyResultId = parameters.GetValueOrDefault("keyResultId", null);
            var title = parameters.GetValueOrDefault("title", null);
            
            // If we have no keyResultId and no title, try to use the most recent key result
            if (string.IsNullOrEmpty(keyResultId) && string.IsNullOrEmpty(title))
            {
                keyResultId = KernelService.GetMostRecentEntityId(conversationId, "KeyResult");
                Logger.LogInformation("No key result specified, using most recent key result ID from history: {KeyResultId}", keyResultId);
                
                if (string.IsNullOrEmpty(keyResultId))
                {
                    return CreateErrorResult("Please provide a key result ID or title to update.");
                }
            }
            
            // Extract other parameters
            var newTitle = parameters.GetValueOrDefault("newTitle", null);
            var description = parameters.GetValueOrDefault("description", null);
            var startDate = parameters.GetValueOrDefault("startDate", null);
            var endDate = parameters.GetValueOrDefault("endDate", null);
            var objectiveId = parameters.GetValueOrDefault("objectiveId", null);
            var objectiveTitle = parameters.GetValueOrDefault("objectiveTitle", null);
            var status = parameters.GetValueOrDefault("status", null);
            var userId = parameters.GetValueOrDefault("userId", userContext?.UserId);
            
            // Try to parse progress if provided
            int? progress = null;
            if (parameters.TryGetValue("progress", out var progressStr))
            {
                if (int.TryParse(progressStr, out var progressVal))
                {
                    progress = progressVal;
                }
            }
            
            // Update the key result
            var result = await _keyResultPlugin.UpdateKeyResultAsync(
                keyResultId,
                title,
                newTitle,
                description,
                startDate,
                endDate,
                objectiveId,
                objectiveTitle,
                status,
                progress);
                
            return CreateSuccessResult(
                result,
                "KeyResult",
                result.KeyResultId,
                "Update",
                result.PromptTemplate);
        }
        
        private async Task<FunctionExecutionResult> HandleDeleteKeyResultAsync(
            string conversationId,
            Dictionary<string, string> parameters,
            UserContext userContext)
        {
            // Get parameters
            var keyResultId = parameters.GetValueOrDefault("keyResultId", null);
            var title = parameters.GetValueOrDefault("title", null);
            
            // If no keyResultId or title, try to get from recent history
            if (string.IsNullOrEmpty(keyResultId) && string.IsNullOrEmpty(title))
            {
                keyResultId = KernelService.GetMostRecentEntityId(conversationId, "KeyResult");
                Logger.LogInformation("Retrieved most recent key result ID from history for delete operation: {KeyResultId}", keyResultId);
                
                if (string.IsNullOrEmpty(keyResultId))
                {
                    return CreateErrorResult("Please provide a key result ID or title to delete.");
                }
            }
            
            // Delete the key result
            var result = await _keyResultPlugin.DeleteKeyResultAsync(keyResultId, title);
            
            return CreateSuccessResult(
                result,
                "KeyResult",
                result.KeyResultId,
                "Delete",
                result.PromptTemplate);
        }
        
        private async Task<FunctionExecutionResult> HandleGetKeyResultInfoAsync(
            string conversationId,
            Dictionary<string, string> parameters,
            UserContext userContext)
        {
            // Get parameters
            var keyResultId = parameters.GetValueOrDefault("keyResultId", null);
            var title = parameters.GetValueOrDefault("title", null);
            
            // If no keyResultId or title, try to get from recent history
            if (string.IsNullOrEmpty(keyResultId) && string.IsNullOrEmpty(title))
            {
                keyResultId = KernelService.GetMostRecentEntityId(conversationId, "KeyResult");
                Logger.LogInformation("Retrieved most recent key result ID from history for view operation: {KeyResultId}", keyResultId);
                
                if (string.IsNullOrEmpty(keyResultId))
                {
                    return CreateErrorResult("Please provide a key result ID or title to view details.");
                }
            }
            
            // Get key result information
            var result = await _keyResultPlugin.GetKeyResultDetailsAsync(keyResultId, title);
            
            return CreateSuccessResult(
                result,
                "KeyResult",
                result.KeyResultId,
                "View",
                result.PromptTemplate);
        }
        
        private async Task<FunctionExecutionResult> HandleSearchKeyResultsAsync(
            string conversationId,
            Dictionary<string, string> parameters,
            UserContext userContext)
        {
            // Extract search parameters
            var title = parameters.GetValueOrDefault("title", null);
            var objectiveId = parameters.GetValueOrDefault("objectiveId", null); 
            var objectiveTitle = parameters.GetValueOrDefault("objectiveTitle", null);
            var userId = parameters.GetValueOrDefault("userId", userContext?.UserId);
            
            // Perform the search
            var result = await _keyResultPlugin.SearchKeyResultsAsync(title, objectiveId, objectiveTitle);
            
            return CreateSuccessResult(
                result,
                "KeyResult",
                null, // No specific entity ID for search
                "Search",
                result.PromptTemplate);
        }
        
        private async Task<FunctionExecutionResult> HandleGetKeyResultsByObjectiveAsync(
            string conversationId,
            Dictionary<string, string> parameters,
            UserContext userContext)
        {
            // Extract parameters
            var objectiveId = parameters.GetValueOrDefault("objectiveId", null);
            var objectiveTitle = parameters.GetValueOrDefault("objectiveTitle", null);
            
            // Need either objectiveId or objectiveTitle
            if (string.IsNullOrEmpty(objectiveId) && string.IsNullOrEmpty(objectiveTitle))
            {
                return CreateErrorResult("An objective ID or title is required to get key results by objective.");
            }
            
            // Get the key results for the objective
            var result = await _keyResultPlugin.GetKeyResultsByObjectiveIdAsync(objectiveId, objectiveTitle);
            
            return CreateSuccessResult(
                result,
                "Objective",
                result.ObjectiveId,
                "ViewKeyResults",
                result.PromptTemplate);
        }
        
        private async Task<FunctionExecutionResult> HandleGetAllKeyResultsAsync(
            string conversationId,
            Dictionary<string, string> parameters,
            UserContext userContext)
        {
            // Extract user ID if available
            string userId = parameters.GetValueOrDefault("userId", userContext?.UserId);
            
            // Call the search method with no title filter to get all key results
            var result = await _keyResultPlugin.SearchKeyResultsAsync(title: null, objectiveId: null, objectiveTitle: null);
            
            return CreateSuccessResult(
                result,
                "KeyResult",
                null,
                "ListAll",
                result.PromptTemplate);
        }
    }
}