using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NXM.Tensai.Back.OKR.AI.Core.AI.Plugins;
using NXM.Tensai.Back.OKR.AI.Models;

namespace NXM.Tensai.Back.OKR.AI.Services.IntentHandlers
{
    public class OkrSessionIntentHandler : IIntentHandler
    {
        private readonly OkrSessionPlugin _okrSessionPlugin;
        private readonly ILogger<OkrSessionIntentHandler> _logger;

        public OkrSessionIntentHandler(OkrSessionPlugin okrSessionPlugin, ILogger<OkrSessionIntentHandler> logger)
        {
            _okrSessionPlugin = okrSessionPlugin ?? throw new ArgumentNullException(nameof(okrSessionPlugin));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Check if this handler can handle the given intent
        /// </summary>
        public bool CanHandle(string intent)
        {
            return intent switch
            {
                "CreateOkrSession" or
                "UpdateOkrSession" or
                "DeleteOkrSession" or
                "GetOkrSessionInfo" or
                "SearchOkrSessions" or
                "GetOkrSessionsByTeam" or
                "GetAllOkrSessions" => true,
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
                }
                
                // Switch based on the intent name
                switch (intent)
                {
                    case "CreateOkrSession":
                        return await HandleCreateOkrSessionIntent(parameters);
                    
                    case "UpdateOkrSession":
                        return await HandleUpdateOkrSessionIntent(parameters);
                    
                    case "DeleteOkrSession":
                        return await HandleDeleteOkrSessionIntent(parameters);
                    
                    case "GetOkrSessionInfo":
                        return await HandleGetOkrSessionInfoIntent(parameters);
                    
                    case "SearchOkrSessions":
                        return await HandleSearchOkrSessionsIntent(parameters);
                    
                    case "GetOkrSessionsByTeam":
                        return await HandleGetOkrSessionsByTeamIntent(parameters);
                    
                    case "GetAllOkrSessions":
                        return await HandleGetAllOkrSessionsIntent(parameters);
                    
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
        /// Handle the CreateOkrSession intent
        /// </summary>
        private async Task<FunctionExecutionResult> HandleCreateOkrSessionIntent(Dictionary<string, string> parameters)
        {
            try
            {
                _logger.LogInformation("Creating OKR session with parameters: {Parameters}", 
                    string.Join(", ", parameters.Keys));
                
                // Extract parameters with safe defaults
                string title = parameters.GetValueOrDefault("title", "New OKR Session");
                string startDate = parameters.GetValueOrDefault("startDate", DateTime.Now.ToString("yyyy-MM-dd"));
                string endDate = parameters.GetValueOrDefault("endDate", DateTime.Now.AddMonths(3).ToString("yyyy-MM-dd"));
                //string teamManagerId = parameters.GetValueOrDefault("teamManagerId");
                //string teamManagerName = parameters.GetValueOrDefault("teamManagerName");
                string description = parameters.GetValueOrDefault("description");
                string teamIds = parameters.GetValueOrDefault("teamIds");
                string userId = parameters.GetValueOrDefault("userId");
                string color = parameters.GetValueOrDefault("color");
                string status = parameters.GetValueOrDefault("status");
                
                // Call the plugin method
                var result = await _okrSessionPlugin.CreateOkrSessionAsync(
                    title,
                    startDate,
                    endDate,
                    //teamManagerId,
                    //teamManagerName,
                    description,
                    teamIds,
                    color,
                    status
                );
                
                // Return success result
                return new FunctionExecutionResult
                {
                    Success = true,
                    Message = result.PromptTemplate,
                    Result = result,
                    EntityType = "OKRSession",
                    EntityId = result.OkrSessionId,
                    Operation = "Create"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating OKR session");
                return new FunctionExecutionResult
                {
                    Success = false,
                    Message = $"Failed to create OKR session: {ex.Message}"
                };
            }
        }
        
        /// <summary>
        /// Handle the UpdateOkrSession intent
        /// </summary>
        private async Task<FunctionExecutionResult> HandleUpdateOkrSessionIntent(Dictionary<string, string> parameters)
        {
            try
            {
                _logger.LogInformation("Updating OKR session with parameters: {Parameters}",
                    string.Join(", ", parameters.Keys));
                
                // Extract parameters
                string okrSessionId = parameters.GetValueOrDefault("okrSessionId");
                string title = parameters.GetValueOrDefault("title");
                string newTitle = parameters.GetValueOrDefault("newTitle");
                string description = parameters.GetValueOrDefault("description");
                string startDate = parameters.GetValueOrDefault("startDate");
                string endDate = parameters.GetValueOrDefault("endDate");
                //string teamManagerId = parameters.GetValueOrDefault("teamManagerId");
                //string teamManagerName = parameters.GetValueOrDefault("teamManagerName");
                string color = parameters.GetValueOrDefault("color");
                string status = parameters.GetValueOrDefault("status");
                string userId = parameters.GetValueOrDefault("userId");
                
                // Call the plugin method
                var result = await _okrSessionPlugin.UpdateOkrSessionAsync(
                    okrSessionId,
                    title,
                    newTitle,
                    description,
                    startDate,
                    endDate,
                    //teamManagerId,
                    //teamManagerName,
                    color,
                    status
                );
                
                // Return success result
                return new FunctionExecutionResult
                {
                    Success = true,
                    Message = result.PromptTemplate,
                    Result = result,
                    EntityType = "OKRSession",
                    EntityId = result.OkrSessionId,
                    Operation = "Update"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating OKR session");
                return new FunctionExecutionResult
                {
                    Success = false,
                    Message = $"Failed to update OKR session: {ex.Message}"
                };
            }
        }
        
        /// <summary>
        /// Handle the DeleteOkrSession intent
        /// </summary>
        private async Task<FunctionExecutionResult> HandleDeleteOkrSessionIntent(Dictionary<string, string> parameters)
        {
            try
            {
                _logger.LogInformation("Deleting OKR session with parameters: {Parameters}",
                    string.Join(", ", parameters.Keys));
                
                // Extract parameters
                string okrSessionId = parameters.GetValueOrDefault("okrSessionId");
                string title = parameters.GetValueOrDefault("title");
                
                // Call the plugin method
                var result = await _okrSessionPlugin.DeleteOkrSessionAsync(okrSessionId, title);
                
                // Return success result
                return new FunctionExecutionResult
                {
                    Success = true,
                    Message = result.PromptTemplate,
                    Result = result,
                    EntityType = "OKRSession",
                    EntityId = result.OkrSessionId,
                    Operation = "Delete"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting OKR session");
                return new FunctionExecutionResult
                {
                    Success = false,
                    Message = $"Failed to delete OKR session: {ex.Message}"
                };
            }
        }
        
        /// <summary>
        /// Handle the GetOkrSessionInfo intent
        /// </summary>
        private async Task<FunctionExecutionResult> HandleGetOkrSessionInfoIntent(Dictionary<string, string> parameters)
        {
            try
            {
                _logger.LogInformation("Getting OKR session info with parameters: {Parameters}",
                    string.Join(", ", parameters.Keys));
                
                // Extract parameters
                string okrSessionId = parameters.GetValueOrDefault("okrSessionId");
                string title = parameters.GetValueOrDefault("title");
                
                // Call the plugin method
                var result = await _okrSessionPlugin.GetOkrSessionDetailsAsync(okrSessionId, title);
                
                // Return success result
                return new FunctionExecutionResult
                {
                    Success = true,
                    Message = result.PromptTemplate,
                    Result = result,
                    EntityType = "OKRSession",
                    EntityId = result.OkrSessionId,
                    Operation = "View"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OKR session info");
                return new FunctionExecutionResult
                {
                    Success = false,
                    Message = $"Failed to get OKR session information: {ex.Message}"
                };
            }
        }
        
        /// <summary>
        /// Handle the SearchOkrSessions intent
        /// </summary>
        private async Task<FunctionExecutionResult> HandleSearchOkrSessionsIntent(Dictionary<string, string> parameters)
        {
            try
            {
                _logger.LogInformation("Searching OKR sessions with parameters: {Parameters}",
                    string.Join(", ", parameters.Keys));
                
                // Extract parameters
                string title = parameters.GetValueOrDefault("title");
                string userId = parameters.GetValueOrDefault("userId");
                
                // If "list all" request, clear any title filters that might have been incorrectly added
                if (parameters.ContainsKey("listAll") && parameters["listAll"].Equals("true", StringComparison.OrdinalIgnoreCase))
                                {
                    _logger.LogInformation("List all OKR sessions requested - clearing title filter");
                    title = null;
                }
                
                // Call the plugin method
                var result = await _okrSessionPlugin.SearchOkrSessionsAsync(title);
                
                // Return success result
                return new FunctionExecutionResult
                {
                    Success = true,
                    Message = result.PromptTemplate,
                    Result = result,
                    EntityType = "OKRSession",
                    EntityId = null,
                    Operation = "Search"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching OKR sessions");
                return new FunctionExecutionResult
                {
                    Success = false,
                    Message = $"Failed to search OKR sessions: {ex.Message}"
                };
            }
        }
        
        /// <summary>
        /// Handle the GetOkrSessionsByTeam intent
        /// </summary>
        private async Task<FunctionExecutionResult> HandleGetOkrSessionsByTeamIntent(Dictionary<string, string> parameters)
        {
            try
            {
                _logger.LogInformation("Getting OKR sessions by team with parameters: {Parameters}",
                    string.Join(", ", parameters.Keys));
                
                // Extract parameters
                string teamId = parameters.GetValueOrDefault("teamId");
                string teamName = parameters.GetValueOrDefault("teamName");
                
                // Call the plugin method
                var result = await _okrSessionPlugin.GetOkrSessionsByTeamIdAsync(teamId, teamName);
                
                // Return success result
                return new FunctionExecutionResult
                {
                    Success = true,
                    Message = result.PromptTemplate,
                    Result = result,
                    EntityType = "Team",
                    EntityId = result.TeamId,
                    Operation = "ViewOKRSessions"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OKR sessions by team");
                return new FunctionExecutionResult
                {
                    Success = false,
                    Message = $"Failed to get OKR sessions for team: {ex.Message}"
                };
            }
        }
        
        /// <summary>
        /// Handle the GetAllOkrSessions intent
        /// </summary>
        private async Task<FunctionExecutionResult> HandleGetAllOkrSessionsIntent(Dictionary<string, string> parameters)
        {
            try
            {
                _logger.LogInformation("Getting all OKR sessions");
                
                // Extract user ID if available
                string userId = parameters.GetValueOrDefault("userId");
                
                // Call the plugin method with no title filter to get all sessions
                var result = await _okrSessionPlugin.SearchOkrSessionsAsync(title: null);
                
                // Return success result
                return new FunctionExecutionResult
                {
                    Success = true,
                    Message = result.PromptTemplate,
                    Result = result,
                    EntityType = "OKRSession",
                    EntityId = null,
                    Operation = "ListAll"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all OKR sessions");
                return new FunctionExecutionResult
                {
                    Success = false,
                    Message = $"Failed to retrieve all OKR sessions: {ex.Message}"
                };
            }
        }
    }
}