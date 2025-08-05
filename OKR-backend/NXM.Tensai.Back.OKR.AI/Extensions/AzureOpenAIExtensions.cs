using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.Logging;
using NXM.Tensai.Back.OKR.AI.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NXM.Tensai.Back.OKR.AI.Extensions
{
    /// <summary>
    /// Extension methods for Azure OpenAI integration
    /// </summary>
    public static class AzureOpenAIExtensions
    {
        /// <summary>
        /// Adds document content to a system message with proper handling for large documents
        /// </summary>
        /// <param name="systemMessage">The base system message</param>
        /// <param name="documentContent">The document content to add</param>
        /// <param name="documentProcessingService">The document processing service for token estimation and chunking</param>
        /// <param name="maxTokens">Maximum tokens allowed for document content</param>
        /// <param name="logger">Logger for tracking processing</param>
        /// <returns>System message with document content added</returns>
        public static async Task<string> AddDocumentContentToSystemMessageAsync(
            this string systemMessage,
            string documentContent,
            IDocumentProcessingService documentProcessingService,
            int maxTokens = 4000,
            ILogger logger = null)
        {
            if (string.IsNullOrEmpty(documentContent))
            {
                return systemMessage;
            }

            try
            {
                // Estimate tokens in base system message
                int systemTokens = documentProcessingService.EstimateTokenCount(systemMessage);
                
                // Calculate remaining tokens for document content
                int remainingTokens = maxTokens - systemTokens;
                
                if (remainingTokens <= 0)
                {
                    logger?.LogWarning("System message already exceeds token limit. Cannot add document content.");
                    return systemMessage;
                }
                
                // Prepare document content to fit within remaining tokens
                string optimizedContent = await documentProcessingService.PrepareContentForOpenAIAsync(
                    documentContent, remainingTokens);
                
                // Add document content to system message
                return $"{systemMessage}\n\nDOCUMENT CONTENT:\n\n{optimizedContent}";
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error adding document content to system message");
                
                // Return original message if any error occurs
                return systemMessage;
            }
        }
        
        /// <summary>
        /// Process large documents by chunking and sending multiple requests to OpenAI
        /// </summary>
        /// <param name="chatService">The chat completion service</param>
        /// <param name="baseSystemMessage">Base system message without document content</param>
        /// <param name="documentContent">Full document content to process</param>
        /// <param name="query">User query about the document</param>
        /// <param name="documentProcessingService">Document processing service</param>
        /// <param name="logger">Logger for tracking processing</param>
        /// <returns>Consolidated response from processing all document chunks</returns>
        public static async Task<string> ProcessLargeDocumentAsync(
            this IChatCompletionService chatService,
            string baseSystemMessage,
            string documentContent,
            string query,
            IDocumentProcessingService documentProcessingService,
            ILogger logger = null)
        {
            if (string.IsNullOrEmpty(documentContent))
            {
                logger?.LogWarning("Empty document content provided for processing");
                return "No document content available for analysis.";
            }

            try
            {
                // Estimate total tokens in the document
                int estimatedTokens = documentProcessingService.EstimateTokenCount(documentContent);
                
                // If document is small enough, process it in one request
                if (estimatedTokens <= 4000)
                {
                    logger?.LogInformation("Document fits in a single request ({Tokens} tokens)", estimatedTokens);
                    
                    // Add document content to system message
                    string systemMessage = await baseSystemMessage.AddDocumentContentToSystemMessageAsync(
                        documentContent, documentProcessingService, 6000, logger);
                    
                    // Create chat history
                    var chatHistory = new ChatHistory();
                    chatHistory.AddSystemMessage(systemMessage);
                    chatHistory.AddUserMessage(query);
                    
                    // Get response
                    var response = await chatService.GetChatMessageContentsAsync(chatHistory);
                    return response[0].Content;
                }
                
                // Document is too large, process it in chunks
                logger?.LogInformation("Large document detected ({Tokens} tokens), processing in chunks", estimatedTokens);
                
                // Split document into chunks
                string[] chunks = await documentProcessingService.ChunkDocumentContentAsync(documentContent, 2000);
                
                // Process each chunk and collect responses
                var responses = new List<string>();
                var chunkPrompt = "You are processing part of a larger document. " +
                    "Extract any objectives, key results, or important information from this section. " + 
                    "Be concise and focus only on key points related to goals, metrics, and priorities.";
                
                for (int i = 0; i < chunks.Length; i++)
                {
                    logger?.LogInformation("Processing document chunk {Current}/{Total}", i + 1, chunks.Length);
                    
                    // Create chat history for this chunk
                    var chatHistory = new ChatHistory();
                    chatHistory.AddSystemMessage(chunkPrompt);
                    chatHistory.AddUserMessage($"Document section {i + 1} of {chunks.Length}:\n\n{chunks[i]}");
                    
                    // Get response for this chunk
                    var response = await chatService.GetChatMessageContentsAsync(chatHistory);
                    responses.Add(response[0].Content);
                }
                
                // Process the consolidated information
                var consolidationPrompt = baseSystemMessage + "\n\nI have analyzed a document in sections and extracted key information from each part. Here are my findings from each section:";
                
                // Create chat history for consolidation
                var finalChatHistory = new ChatHistory();
                finalChatHistory.AddSystemMessage(consolidationPrompt);
                
                // Build the consolidated user message
                var consolidatedMessage = new StringBuilder();
                consolidatedMessage.AppendLine(query);
                consolidatedMessage.AppendLine();
                consolidatedMessage.AppendLine("Here are the key points extracted from each section of the document:");
                consolidatedMessage.AppendLine();
                
                for (int i = 0; i < responses.Count; i++)
                {
                    consolidatedMessage.AppendLine($"Section {i + 1}:");
                    consolidatedMessage.AppendLine(responses[i]);
                    consolidatedMessage.AppendLine();
                }
                
                consolidatedMessage.AppendLine("Based on these extracted points, please provide a comprehensive analysis addressing my query.");
                
                finalChatHistory.AddUserMessage(consolidatedMessage.ToString());
                
                // Get final consolidated response
                var finalResponse = await chatService.GetChatMessageContentsAsync(finalChatHistory);
                return finalResponse[0].Content;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error processing large document");
                return $"An error occurred while processing the document: {ex.Message}";
            }
        }
    }
    
    // Add this new extension method to help with OKR workflow state tracking
    /// <summary>
    /// Tracks OKR document processing workflow state and ensures continuation between steps
    /// </summary>
    public static class OkrWorkflowState
    {
        private static readonly Dictionary<string, Dictionary<string, string>> _conversationWorkflowState = new();
        
        /// <summary>
        /// Tracks the current state of an OKR workflow for a specific conversation
        /// </summary>
        public static void TrackWorkflowState(string conversationId, string key, string value)
        {
            if (string.IsNullOrEmpty(conversationId)) return;
            
            // Initialize conversation state if not existing
            if (!_conversationWorkflowState.ContainsKey(conversationId))
            {
                _conversationWorkflowState[conversationId] = new Dictionary<string, string>();
            }
            
            // Track the state value
            _conversationWorkflowState[conversationId][key] = value;
        }
        
        /// <summary>
        /// Gets a workflow state value for a specific conversation
        /// </summary>
        public static string GetWorkflowState(string conversationId, string key, string defaultValue = null)
        {
            if (string.IsNullOrEmpty(conversationId) || 
                !_conversationWorkflowState.ContainsKey(conversationId) ||
                !_conversationWorkflowState[conversationId].ContainsKey(key))
            {
                return defaultValue;
            }
            
            return _conversationWorkflowState[conversationId][key];
        }
        
        /// <summary>
        /// Checks if the OKR response indicates we should transition to the next step and ensures continuity
        /// </summary>
        public static string EnsureWorkflowContinuation(string response, string conversationId)
        {
            // Don't modify if no conversation tracking
            if (string.IsNullOrEmpty(conversationId)) return response;
            
            // Track if we've created an OKR session from the response content
            if (response.Contains("OKR session") && (
                response.Contains("created successfully") || 
                response.Contains("has been created") || 
                response.Contains("successfully created")))
            {
                // Extract the session ID if present in the response using regex
                var sessionIdMatch = System.Text.RegularExpressions.Regex.Match(
                    response, 
                    @"session(?:\s+with)?\s+ID:?\s*['""]?([0-9a-fA-F-]+)['""]?");
                    
                if (sessionIdMatch.Success)
                {
                    string sessionId = sessionIdMatch.Groups[1].Value;
                    TrackWorkflowState(conversationId, "OkrSessionId", sessionId);
                    TrackWorkflowState(conversationId, "CurrentStep", "CreatedSession");
                }
                else
                {
                    TrackWorkflowState(conversationId, "CurrentStep", "CreatedSession");
                }
                
                // If the response doesn't proceed to objectives, add that prompt
                if (!response.Contains("objective") && !response.Contains("Objective") && 
                    !response.Contains("STEP 2") && !response.Contains("Next, let's"))
                {
                    response += "\n\nNow that we've created the OKR session, let's create an objective for it. " +
                        "Based on the document analysis, I suggest the following objective:\n\n" +
                        "[Objective title and description based on the document content]\n\n" +
                        "Would you like me to create this objective for the OKR session? Or would you like to make any adjustments?";
                }
            }
            // Check for objective creation
            else if ((response.Contains("objective") || response.Contains("Objective")) && 
                     response.Contains("created") && 
                     GetWorkflowState(conversationId, "CurrentStep") == "CreatedSession")
            {
                // Extract the objective ID if present
                var objectiveIdMatch = System.Text.RegularExpressions.Regex.Match(
                    response, 
                    @"objective(?:\s+with)?\s+ID:?\s*['""]?([0-9a-fA-F-]+)['""]?");
                    
                if (objectiveIdMatch.Success)
                {
                    string objectiveId = objectiveIdMatch.Groups[1].Value;
                    TrackWorkflowState(conversationId, "ObjectiveId", objectiveId);
                    TrackWorkflowState(conversationId, "CurrentStep", "CreatedObjective");
                }
                else
                {
                    TrackWorkflowState(conversationId, "CurrentStep", "CreatedObjective");
                }
                
                // If the response doesn't proceed to key results, add that prompt
                if (!response.Contains("key result") && !response.Contains("Key Result") && 
                    !response.Contains("STEP 3") && !response.Contains("Next, let's"))
                {
                    response += "\n\nNow that we've created the objective, let's create a key result for it. " +
                        "Based on the document analysis, I suggest the following key result:\n\n" +
                        "[Key result title and description based on the document content]\n\n" +
                        "Would you like me to create this key result for the objective? Or would you like to make any adjustments?";
                }
            }
            // Check for key result creation
            else if ((response.Contains("key result") || response.Contains("Key Result")) && 
                     response.Contains("created") && 
                     GetWorkflowState(conversationId, "CurrentStep") == "CreatedObjective")
            {
                // Extract the key result ID if present
                var keyResultIdMatch = System.Text.RegularExpressions.Regex.Match(
                    response, 
                    @"key result(?:\s+with)?\s+ID:?\s*['""]?([0-9a-fA-F-]+)['""]?");
                    
                if (keyResultIdMatch.Success)
                {
                    string keyResultId = keyResultIdMatch.Groups[1].Value;
                    TrackWorkflowState(conversationId, "KeyResultId", keyResultId);
                    TrackWorkflowState(conversationId, "CurrentStep", "CreatedKeyResult");
                }
                else
                {
                    TrackWorkflowState(conversationId, "CurrentStep", "CreatedKeyResult");
                }
                
                // If the response doesn't proceed to tasks, add that prompt
                if (!response.Contains("task") && !response.Contains("Task") && 
                    !response.Contains("STEP 4") && !response.Contains("Next, let's"))
                {
                    response += "\n\nNow that we've created the key result, let's create a task for it. " +
                        "Based on the document analysis, I suggest the following task:\n\n" +
                        "[Task title and description based on the document content]\n\n" +
                        "Would you like me to create this task for the key result? Or would you like to make any adjustments?";
                }
            }
            
            return response;
        }
        
        /// <summary>
        /// Resets the workflow state for a specific conversation
        /// </summary>
        public static void ResetWorkflowState(string conversationId)
        {
            if (!string.IsNullOrEmpty(conversationId) && _conversationWorkflowState.ContainsKey(conversationId))
            {
                _conversationWorkflowState.Remove(conversationId);
            }
        }
    }
}