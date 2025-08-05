using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NXM.Tensai.Back.OKR.AI.Core.AI.Plugins;
using NXM.Tensai.Back.OKR.AI.Models;

namespace NXM.Tensai.Back.OKR.AI.Services.IntentHandlers
{
    public class ObjectiveIntentHandler : IIntentHandler
    {
        private readonly ObjectivePlugin _objectivePlugin;
        private readonly ILogger<ObjectiveIntentHandler> _logger;

        public ObjectiveIntentHandler(ObjectivePlugin objectivePlugin, ILogger<ObjectiveIntentHandler> logger)
        {
            _objectivePlugin = objectivePlugin ?? throw new ArgumentNullException(nameof(objectivePlugin));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Check if this handler can handle the given intent
        /// </summary>
        public bool CanHandle(string intent)
        {
            return intent switch
            {
                "CreateObjective" or
                "UpdateObjective" or
                "DeleteObjective" or
                "GetObjectiveInfo" or
                "SearchObjectives" or
                "GetObjectivesBySession" or
                "GetAllObjectives" => true,
                _ => false
            };
        }

        /// <summary>
        /// Handle the specified intent with the given parameters
        /// </summary>
        public async Task<FunctionExecutionResult> HandleIntentAsync(
            string conversationId,
            string intent,
            Dictionary<string, string> parameters,
            UserContext userContext)
        {
            try
            {
                _logger.LogInformation("Handling {Intent} intent with {ParameterCount} parameters", 
                    intent, parameters.Count);
                
                // Add the user ID to parameters if available
                if (!parameters.ContainsKey("userId") && !string.IsNullOrEmpty(userContext?.UserId))
                {
                    parameters["userId"] = userContext.UserId;
                    _logger.LogInformation("Added userId from context: {UserId}", userContext.UserId);
                }
                
                // Switch based on the intent name
                switch (intent)
                {
                    case "CreateObjective":
                        return await HandleCreateObjectiveIntent(parameters);
                    
                    case "UpdateObjective":
                        return await HandleUpdateObjectiveIntent(parameters);
                    
                    case "DeleteObjective":
                        return await HandleDeleteObjectiveIntent(parameters);
                    
                    case "GetObjectiveInfo":
                        return await HandleGetObjectiveInfoIntent(parameters);
                    
                    case "SearchObjectives":
                        return await HandleSearchObjectivesIntent(parameters);
                    
                    case "GetObjectivesBySession":
                        return await HandleGetObjectivesBySessionIntent(parameters);
                    
                    case "GetAllObjectives":
                        return await HandleGetAllObjectivesIntent(parameters);
                    
                    default:
                        _logger.LogWarning("Unknown intent: {Intent}", intent);
                        return new FunctionExecutionResult
                        {
                            Success = false,
                            Message = $"I don't know how to handle the '{intent}' intent."
                        };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling intent: {Intent}", intent);
                return new FunctionExecutionResult
                {
                    Success = false,
                    Message = $"An error occurred while processing your request: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Handle the CreateObjective intent
        /// </summary>
        private async Task<FunctionExecutionResult> HandleCreateObjectiveIntent(Dictionary<string, string> parameters)
        {
            try
            {
                _logger.LogInformation("Creating objective with parameters: {Parameters}", 
                    string.Join(", ", parameters.Keys));
                
                // Extract parameters with safe defaults
                string title = parameters.GetValueOrDefault("title", "New Objective");
                string okrSessionId = parameters.GetValueOrDefault("okrSessionId");
                string okrSessionTitle = parameters.GetValueOrDefault("okrSessionTitle");
                string startDate = parameters.GetValueOrDefault("startDate");
                string endDate = parameters.GetValueOrDefault("endDate");
                string responsibleTeamId = parameters.GetValueOrDefault("responsibleTeamId");
                string responsibleTeamName = parameters.GetValueOrDefault("responsibleTeamName");
                string description = parameters.GetValueOrDefault("description");
                string userId = parameters.GetValueOrDefault("userId");
                string status = parameters.GetValueOrDefault("status");
                string priority = parameters.GetValueOrDefault("priority");
                
                // Try to parse progress if provided
                int? progress = null;
                if(parameters.TryGetValue("progress", out var progressStr))
                {
                    if(int.TryParse(progressStr, out var progressVal))
                    {
                        progress = progressVal;
                    }
                }
                
                // Call the plugin method
                var result = await _objectivePlugin.CreateObjectiveAsync(
                    title,
                    okrSessionId,
                    okrSessionTitle,
                    startDate,
                    endDate,
                    responsibleTeamId,
                    responsibleTeamName,
                    description,
                    status,
                    priority,
                    progress
                );
                
                // Return success result
                return new FunctionExecutionResult
                {
                    Success = true,
                    Message = result.PromptTemplate,
                    Result = result,
                    EntityType = "Objective",
                    EntityId = result.ObjectiveId,
                    Operation = "Create"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating objective");
                return new FunctionExecutionResult
                {
                    Success = false,
                    Message = $"Failed to create objective: {ex.Message}"
                };
            }
        }
        
        /// <summary>
        /// Handle the UpdateObjective intent
        /// </summary>
        private async Task<FunctionExecutionResult> HandleUpdateObjectiveIntent(Dictionary<string, string> parameters)
        {
            try
            {
                _logger.LogInformation("Updating objective with parameters: {Parameters}",
                    string.Join(", ", parameters.Keys));
                
                // Extract parameters
                string objectiveId = parameters.GetValueOrDefault("objectiveId");
                string title = parameters.GetValueOrDefault("title");
                string newTitle = parameters.GetValueOrDefault("newTitle");
                string description = parameters.GetValueOrDefault("description");
                string startDate = parameters.GetValueOrDefault("startDate");
                string endDate = parameters.GetValueOrDefault("endDate");
                string responsibleTeamId = parameters.GetValueOrDefault("responsibleTeamId");
                string responsibleTeamName = parameters.GetValueOrDefault("responsibleTeamName");
                string status = parameters.GetValueOrDefault("status");
                string priority = parameters.GetValueOrDefault("priority");
                string okrSessionId = parameters.GetValueOrDefault("okrSessionId");
                string userId = parameters.GetValueOrDefault("userId");
                
                // Log extracted parameters for debugging
                _logger.LogDebug("Extracted objectiveId={ObjectiveId}, title={Title}, description={Description}, responsibleTeamId={ResponsibleTeamId}, responsibleTeamName={ResponsibleTeamName}",
                    objectiveId, title, description, responsibleTeamId, responsibleTeamName);
                
                // Try to parse progress if provided
                int? progress = null;
                if(parameters.TryGetValue("progress", out var progressStr))
                {
                    if(int.TryParse(progressStr, out var progressVal))
                    {
                        progress = progressVal;
                    }
                }
                
                // Don't pass responsibleTeamName directly - let the ObjectivePlugin handle the name-to-ID conversion
                // as it has logic to search for teams by name
                
                // Call the plugin method
                var result = await _objectivePlugin.UpdateObjectiveAsync(
                    objectiveId,
                    title,
                    newTitle,
                    description,
                    startDate,
                    endDate,
                    responsibleTeamId,
                    responsibleTeamName,
                    status,
                    priority,
                    progress,
                    okrSessionId
                );
                
                // Return success result
                return new FunctionExecutionResult
                {
                    Success = true,
                    Message = result.PromptTemplate,
                    Result = result,
                    EntityType = "Objective",
                    EntityId = result.ObjectiveId,
                    Operation = "Update"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating objective");
                return new FunctionExecutionResult
                {
                    Success = false,
                    Message = $"Failed to update objective: {ex.Message}"
                };
            }
        }
        
        /// <summary>
        /// Handle the DeleteObjective intent
        /// </summary>
        private async Task<FunctionExecutionResult> HandleDeleteObjectiveIntent(Dictionary<string, string> parameters)
        {
            try
            {
                _logger.LogInformation("Deleting objective with parameters: {Parameters}",
                    string.Join(", ", parameters.Keys));
                
                // Extract parameters
                string objectiveId = parameters.GetValueOrDefault("objectiveId");
                string title = parameters.GetValueOrDefault("title");
                string userId = parameters.GetValueOrDefault("userId");
                
                // Call the plugin method
                var result = await _objectivePlugin.DeleteObjectiveAsync(objectiveId, title);
                
                // Return success result
                return new FunctionExecutionResult
                {
                    Success = true,
                    Message = result.PromptTemplate,
                    Result = result,
                    EntityType = "Objective",
                    EntityId = result.ObjectiveId,
                    Operation = "Delete"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting objective");
                return new FunctionExecutionResult
                {
                    Success = false,
                    Message = $"Failed to delete objective: {ex.Message}"
                };
            }
        }
        
        /// <summary>
        /// Handle the GetObjectiveInfo intent
        /// </summary>
        private async Task<FunctionExecutionResult> HandleGetObjectiveInfoIntent(Dictionary<string, string> parameters)
        {
            try
            {
                _logger.LogInformation("Getting objective info with parameters: {Parameters}",
                    string.Join(", ", parameters.Keys));
                
                // Extract parameters
                string objectiveId = parameters.GetValueOrDefault("objectiveId");
                string title = parameters.GetValueOrDefault("title");
                
                // Call the plugin method
                var result = await _objectivePlugin.GetObjectiveDetailsAsync(objectiveId, title);
                
                // Return success result
                return new FunctionExecutionResult
                {
                    Success = true,
                    Message = result.PromptTemplate,
                    Result = result,
                    EntityType = "Objective",
                    EntityId = result.ObjectiveId,
                    Operation = "View"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting objective info");
                return new FunctionExecutionResult
                {
                    Success = false,
                    Message = $"Failed to get objective information: {ex.Message}"
                };
            }
        }
        
        /// <summary>
        /// Handle the SearchObjectives intent
        /// </summary>
        private async Task<FunctionExecutionResult> HandleSearchObjectivesIntent(Dictionary<string, string> parameters)
        {
            try
            {
                _logger.LogInformation("Searching objectives with parameters: {Parameters}",
                    string.Join(", ", parameters.Keys));
                
                // Extract parameters
                string title = parameters.GetValueOrDefault("title");
                string okrSessionId = parameters.GetValueOrDefault("okrSessionId");
                string okrSessionTitle = parameters.GetValueOrDefault("okrSessionTitle");
                string teamId = parameters.GetValueOrDefault("teamId");
                string teamName = parameters.GetValueOrDefault("teamName");
                string userId = parameters.GetValueOrDefault("userId");
                
                // If "list all" request, clear any title filters that might have been incorrectly added
                if (parameters.ContainsKey("listAll") && parameters["listAll"].Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("List all objectives requested - clearing title filter");
                    title = null;
                }
                
                // Call the plugin method
                var result = await _objectivePlugin.SearchObjectivesAsync(title, okrSessionId, okrSessionTitle, teamId, teamName);
                
                // Return success result
                return new FunctionExecutionResult
                {
                    Success = true,
                    Message = result.PromptTemplate,
                    Result = result,
                    EntityType = "Objective",
                    EntityId = null,
                    Operation = "Search"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching objectives");
                return new FunctionExecutionResult
                {
                    Success = false,
                    Message = $"Failed to search objectives: {ex.Message}"
                };
            }
        }
        
        /// <summary>
        /// Handle the GetObjectivesBySession intent
        /// </summary>
        private async Task<FunctionExecutionResult> HandleGetObjectivesBySessionIntent(Dictionary<string, string> parameters)
        {
            try
            {
                _logger.LogInformation("Getting objectives by session with parameters: {Parameters}",
                    string.Join(", ", parameters.Keys));
                
                // Extract parameters
                string okrSessionId = parameters.GetValueOrDefault("okrSessionId");
                string okrSessionTitle = parameters.GetValueOrDefault("okrSessionTitle");
                
                // Call the plugin method
                var result = await _objectivePlugin.GetObjectivesBySessionIdAsync(okrSessionId, okrSessionTitle);
                
                // Return success result
                return new FunctionExecutionResult
                {
                    Success = true,
                    Message = result.PromptTemplate,
                    Result = result,
                    EntityType = "OKRSession",
                    EntityId = okrSessionId,
                    Operation = "ViewObjectives"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting objectives by session");
                return new FunctionExecutionResult
                {
                    Success = false,
                    Message = $"Failed to get objectives for session: {ex.Message}"
                };
            }
        }
        
        /// <summary>
        /// Handle the GetAllObjectives intent
        /// </summary>
        private async Task<FunctionExecutionResult> HandleGetAllObjectivesIntent(Dictionary<string, string> parameters)
        {
            try
            {
                _logger.LogInformation("Getting all objectives");
                
                // Extract user ID if available
                string userId = parameters.GetValueOrDefault("userId");
                
                // Call the plugin method with no title filter to get all objectives
                var result = await _objectivePlugin.SearchObjectivesAsync(title: null, okrSessionId: null, okrSessionTitle: null, teamId: null, teamName: null);
                
                // Return success result
                return new FunctionExecutionResult
                {
                    Success = true,
                    Message = result.PromptTemplate,
                    Result = result,
                    EntityType = "Objective",
                    EntityId = null,
                    Operation = "ListAll"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all objectives");
                return new FunctionExecutionResult
                {
                    Success = false,
                    Message = $"Failed to retrieve all objectives: {ex.Message}"
                };
            }
        }
    }
}