using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NXM.Tensai.Back.OKR.AI.Core.AI.Plugins;
using NXM.Tensai.Back.OKR.AI.Models;
using NXM.Tensai.Back.OKR.AI.Services.IntentHandlers;
using NXM.Tensai.Back.OKR.AI.Utilities;

namespace NXM.Tensai.Back.OKR.AI.Services
{
    /// <summary>
    /// Service responsible for intent analysis, processing and execution
    /// </summary>
    public class IntentProcessor
    {
        private readonly KernelService _kernelService;
        private readonly IntentSystemMessageGenerator _intentSystemMessageGenerator;
        private readonly ResponseGenerator _responseGenerator;
        private readonly IEnumerable<IIntentHandler> _intentHandlers;
        private readonly ILogger<IntentProcessor> _logger;

        public IntentProcessor(
            KernelService kernelService,
            IntentSystemMessageGenerator intentSystemMessageGenerator,
            ResponseGenerator responseGenerator,
            IEnumerable<IIntentHandler> intentHandlers,
            ILogger<IntentProcessor> logger)
        {
            _kernelService = kernelService;
            _intentSystemMessageGenerator = intentSystemMessageGenerator;
            _responseGenerator = responseGenerator;
            _intentHandlers = intentHandlers;
            _logger = logger;
        }

        /// <summary>
        /// Analyzes the user's prompt to determine multiple intents and extract parameters
        /// </summary>
        public async Task<List<IntentRequest>> AnalyzePromptAsync(string conversationId, string prompt, string selectedLlmProvider = null)
        {
            // Generate a specialized system message for intent detection
            string systemMessage = _intentSystemMessageGenerator.GenerateIntentDetectionSystemMessage();
            
            // Extract recent entity context from chat history to help with entity references
            var enhancedHistory = _kernelService.GetEnhancedHistory(conversationId);
            var contextBuilder = new StringBuilder();
            
            // Add context about recent entities the user might be referring to
            contextBuilder.AppendLine("\nRECENT CONTEXT:");
            bool hasAddedContext = false;
            
            // Get all entity types in the message history and provide context for each
            foreach (var message in enhancedHistory.Messages)
            {
                if (!string.IsNullOrEmpty(message.EntityType) && !string.IsNullOrEmpty(message.EntityId))
                {
                    // Check if we already added this entity type/id combination
                    if (contextBuilder.ToString().Contains($"ID={message.EntityId}"))
                    {
                        continue;
                    }
                    
                    // Get the most recent entity ID for this type
                    var recentEntityId = enhancedHistory.GetMostRecentEntityId(message.EntityType);
                    if (recentEntityId == message.EntityId) // Only add the most recent one for each type
                    {
                        contextBuilder.AppendLine($"Most recent {message.EntityType}: ID={message.EntityId}");
                        hasAddedContext = true;
                        
                        // Try to extract name from function output if available
                        if (!string.IsNullOrEmpty(message.FunctionOutput))
                        {
                            try
                            {
                                var entityData = JsonSerializer.Deserialize<JsonDocument>(message.FunctionOutput);
                                // Look for common name properties
                                foreach (var propertyName in new[] { "Name", "TeamName", "Title", "ObjectiveName" })
                                {
                                    if (entityData.RootElement.TryGetProperty(propertyName, out var nameElement))
                                    {
                                        var name = nameElement.GetString();
                                        if (!string.IsNullOrEmpty(name))
                                        {
                                            contextBuilder.AppendLine($"{message.EntityType} Name: {name}");
                                            break;
                                        }
                                    }
                                }
                            }
                            catch { /* Ignore parsing errors */ }
                        }
                        
                        // Or use metadata if available
                        if (message.Metadata.TryGetValue("EntityName", out var entityName))
                        {
                            contextBuilder.AppendLine($"{message.EntityType} Name: {entityName}");
                        }
                        
                        // Check if there was an operation performed
                        if (!string.IsNullOrEmpty(message.Operation))
                        {
                            contextBuilder.AppendLine($"Operation: {message.Operation}");
                        }
                    }
                }
            }
            
            // Only add context section if we actually found entity context
            string enhancedSystemMessage;
            if (hasAddedContext)
            {
                enhancedSystemMessage = systemMessage + contextBuilder.ToString();
            }
            else
            {
                enhancedSystemMessage = systemMessage;
            }

            // Use the ExecuteSinglePromptAsync method that doesn't affect the main conversation history
            // Pass the selectedLlmProvider to use the specified LLM
            string analysisResponse = await _kernelService.ExecuteSinglePromptAsync(
                enhancedSystemMessage, prompt, selectedLlmProvider);

            _logger.LogInformation("Intent analysis response for conversation {ConversationId}: {Response}", 
                conversationId, analysisResponse);

            try
            {
                // Sanitize the response to ensure it has proper JSON format
                analysisResponse = JsonHelper.SanitizeJsonResponse(analysisResponse);
                
                // Parse the JSON response from the AI
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var analysis = JsonSerializer.Deserialize<MultiIntentAnalysisResult>(analysisResponse, options);

                if (analysis == null || analysis.Intents == null || !analysis.Intents.Any())
                {
                    _logger.LogWarning("Failed to parse multiple intents for conversation {ConversationId}. Defaulting to general conversation.", 
                        conversationId);
                    return new List<IntentRequest> { new IntentRequest { Intent = "General", Parameters = new Dictionary<string, string>() } };
                }

                return analysis.Intents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing intent analysis response for conversation {ConversationId}: {Message}", 
                    conversationId, ex.Message);
                
                // If the response format is wrong, try to parse it as the old single intent format
                try
                {
                    var singleIntentAnalysis = JsonSerializer.Deserialize<PromptAnalysisResult>(analysisResponse, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (singleIntentAnalysis != null && !string.IsNullOrEmpty(singleIntentAnalysis.Intent))
                    {
                        _logger.LogWarning("Falling back to single intent format for conversation {ConversationId}", 
                            conversationId);
                        return new List<IntentRequest> { 
                            new IntentRequest { 
                                Intent = singleIntentAnalysis.Intent, 
                                Parameters = singleIntentAnalysis.Parameters ?? new Dictionary<string, string>() 
                            } 
                        };
                    }
                }
                catch 
                {
                    // Ignore this fallback attempt
                }
                
                // Default to general conversation
                return new List<IntentRequest> { new IntentRequest { Intent = "General", Parameters = new Dictionary<string, string>() } };
            }
        }

        /// <summary>
        /// Process multiple intents sequentially
        /// </summary>
        public async Task<IntentExecutionResult> ProcessMultipleIntentsAsync(string conversationId, List<IntentRequest> requests, UserContext userContext)
        {
            bool allSuccess = true;
            StringBuilder operationSummaryBuilder = new StringBuilder();
            List<FunctionResultItem> executionResults = new List<FunctionResultItem>();

            // Process each intent sequentially
            foreach (var request in requests)
            {
                _logger.LogInformation("Processing intent for conversation {ConversationId}: {Intent} with {ParameterCount} parameters", 
                    conversationId, request.Intent, request.Parameters.Count);
                
                // Skip General intent as it doesn't require function execution
                if (request.Intent == "General")
                {
                    continue;
                }
                
                var intentResult = await ExecuteFunctionByIntent(conversationId, request.Intent, request.Parameters, userContext);

                if (!intentResult.Success)
                {
                    allSuccess = false;
                    operationSummaryBuilder.AppendLine($"Error processing {request.Intent}: {intentResult.Message}");
                }
                else
                {
                    // For successful results, add to the list 
                    var resultItem = new FunctionResultItem
                    {
                        Intent = request.Intent,
                        Data = intentResult.Result,
                        EntityType = intentResult.EntityType,
                        EntityId = intentResult.EntityId,
                        Operation = intentResult.Operation,
                        Message = intentResult.Message // Store the message from the function execution
                    };
                    
                    executionResults.Add(resultItem);
                    
                    // Record function execution here (only place it should happen)
                    _kernelService.RecordFunctionExecution(
                        conversationId,
                        resultItem.Intent,
                        resultItem.Data,
                        resultItem.EntityType,
                        resultItem.EntityId,
                        resultItem.Operation
                    );
                    
                    // Instead of appending the message, collect what operation was performed
                    if (!string.IsNullOrEmpty(intentResult.Operation) && !string.IsNullOrEmpty(intentResult.EntityType))
                    {
                        operationSummaryBuilder.AppendLine($"- {intentResult.Operation} {intentResult.EntityType} {intentResult.EntityId}");
                    }
                }
            }

            string operationSummary = operationSummaryBuilder.ToString().Trim();
            string finalMessage;

            // If we have multiple successful results, generate a coherent response
            if (executionResults.Count > 1 && allSuccess)
            {
                // Use AI to generate a single coherent response
                finalMessage = await _responseGenerator.GenerateConsolidatedResponse(executionResults);
            }
            // If only one intent was successful, use its message directly from our stored results
            else if (executionResults.Count == 1 && allSuccess)
            {
                finalMessage = executionResults[0].Message;
            }
            // If no message was generated but we have intents, provide a default success message
            else if (string.IsNullOrEmpty(operationSummary) && executionResults.Count > 0)
            {
                finalMessage = "All operations completed successfully.";
            }
            // If errors occurred, use the error messages
            else if (!allSuccess)
            {
                finalMessage = operationSummary;
            }
            // If no intents were processed (only General), return failure
            else if (executionResults.Count == 0 && requests.All(r => r.Intent == "General"))
            {
                allSuccess = false;
                finalMessage = "No specific operations to perform.";
            }
            else
            {
                finalMessage = operationSummary;
            }

            return new IntentExecutionResult
            {
                Success = allSuccess && executionResults.Count > 0,
                Message = finalMessage,
                Results = executionResults
            };
        }

        /// <summary>
        /// Execute a function based on the intent name and parameters
        /// </summary>
        private async Task<FunctionExecutionResult> ExecuteFunctionByIntent(
            string conversationId,
            string intent,
            Dictionary<string, string> parameters,
            UserContext userContext)
        {
            try
            {
                // Find the appropriate handler for this intent
                var handler = _intentHandlers.FirstOrDefault(h => h.CanHandle(intent));
                
                if (handler != null)
                {
                    _logger.LogInformation("Using handler {HandlerType} for intent {Intent}", 
                        handler.GetType().Name, intent);
                    
                    return await handler.HandleIntentAsync(conversationId, intent, parameters, userContext);
                }
                
                // No handler found
                _logger.LogWarning("No handler found for intent: {Intent}", intent);
                return new FunctionExecutionResult
                {
                    Success = false,
                    Message = $"No specific function execution available for {intent}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing function for intent {Intent}", intent);
                return new FunctionExecutionResult
                {
                    Success = false,
                    Message = $"Error executing {intent}: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Helper to get user display name from context
        /// </summary>
        public string GetUserDisplayName(UserContext userContext)
        {
            return !string.IsNullOrEmpty(userContext.UserName)
                ? userContext.UserName
                : $"User-{userContext.UserId?.Substring(0, Math.Min(8, userContext.UserId?.Length ?? 0)) ?? "Anonymous"}";
        }
    }
}
