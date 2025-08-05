using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using NXM.Tensai.Back.OKR.AI.Models;
using NXM.Tensai.Back.OKR.AI.Services;
using NXM.Tensai.Back.OKR.AI.Services.Authorization;
using NXM.Tensai.Back.OKR.AI.Services.ChatHistoryService;
using Microsoft.AspNetCore.Http;
using MediatR;
using NXM.Tensai.Back.OKR.Application.Features.Documents.Commands.UploadDocument;
using NXM.Tensai.Back.OKR.Application.Features.Documents.Interfaces;
using NXM.Tensai.Back.OKR.Domain.Interfaces.Repositories;
using System.Security.Claims;
using NXM.Tensai.Back.OKR.Application.Common.Exceptions;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Pdf.Canvas.Parser;
using System.Text;
using NXM.Tensai.Back.OKR.AI.Services.Interfaces;

namespace NXM.Tensai.Back.OKR.AI.Controllers
{
    [ApiController]
    [Route("api/ai")]
    public class ChatController : ControllerBase
    {
        private readonly KernelService _kernelService;
        private readonly IntentProcessor _intentProcessor;
        private readonly ChatHistoryManager _chatHistoryManager;
        private readonly AzureOpenAIChatService _azureOpenAIChatService;
        private readonly VectorContextMemoryService _vectorMemoryService;
        private readonly UserContextAccessor _userContextAccessor;
        private readonly ILogger<ChatController> _logger;
        private readonly IMediator _mediator;
        private readonly IDocumentStorageService _documentStorageService;
        private readonly IDocumentRepository _documentRepository;        private readonly IDocumentProcessingService _documentProcessingService;
        private readonly PdfGeneratorService _pdfGeneratorService;
        private readonly OkrAnalysisOrchestratorService _okrAnalysisOrchestrator;

        public ChatController(
            KernelService kernelService,
            IntentProcessor intentProcessor,
            ChatHistoryManager chatHistoryManager,
            AzureOpenAIChatService azureOpenAIChatService,
            VectorContextMemoryService vectorMemoryService,
            UserContextAccessor userContextAccessor,
            ILogger<ChatController> logger,
            IMediator mediator,
            IDocumentStorageService documentStorageService,            IDocumentRepository documentRepository,
            IDocumentProcessingService documentProcessingService,
            PdfGeneratorService pdfGeneratorService,
            OkrAnalysisOrchestratorService okrAnalysisOrchestrator)
        {
            _kernelService = kernelService;
            _intentProcessor = intentProcessor;
            _chatHistoryManager = chatHistoryManager;
            _azureOpenAIChatService = azureOpenAIChatService;
            _vectorMemoryService = vectorMemoryService;
            _userContextAccessor = userContextAccessor;
            _logger = logger;
            _mediator = mediator;
            _documentStorageService = documentStorageService;            _documentRepository = documentRepository;
            _documentProcessingService = documentProcessingService;
            _pdfGeneratorService = pdfGeneratorService;
            _okrAnalysisOrchestrator = okrAnalysisOrchestrator;
        }

        /// <summary>
        /// Endpoint to handle chat requests and generate AI responses
        /// </summary>
        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null || string.IsNullOrEmpty(request.Message))
            {
                return BadRequest("Message is required");
            }

            // Ensure we have a conversation ID
            if (string.IsNullOrEmpty(request.ConversationId))
            {
                return BadRequest("ConversationId is required to maintain separate chat histories");
            }

            try
            {
                _logger.LogInformation("Chat request received for conversation {ConversationId}: {Message}", 
                    request.ConversationId, request.Message);

                // Create or get the user context and set it in the accessor
                var userContext = request.UserContext ?? new UserContext();
                
                // IMPORTANT: Set the user context in the accessor
                _userContextAccessor.CurrentUserContext = userContext;
                string userName = _intentProcessor.GetUserDisplayName(userContext);
                
                // Add the user message to chat history
                var userMessage = EnhancedChatMessage.FromUser(request.Message);
                userMessage.Metadata["AuthorName"] = userName;
                // Always store the user ID, even if empty, for consistency in retrieval
                userMessage.Metadata["UserId"] = userContext?.UserId ?? string.Empty;
                _kernelService.GetEnhancedHistory(request.ConversationId).AddMessage(userMessage);
                
                // Get the selected LLM provider from the request
                string selectedLlmProvider = request.LLMProvider?.ToLower();
                
                // If using Azure OpenAI with the new dedicated service
                if (selectedLlmProvider == "azureopenai")
                {
                    _logger.LogInformation("Using dedicated Azure OpenAI service for the request");
                    
                    // Retrieve relevant context from vector store for this conversation
                    var relevantContext = await _vectorMemoryService.GetRelevantContextAsync(
                        request.Message,
                        request.ConversationId);
                         
                    // --- Add OKR Risk Analysis context ---
                    bool isOKRTasksAnalysis = request.Message?.Trim().ToLower().Contains("analyze okrs risks") == true
                        || request.Message?.Trim().ToLower().Contains("okr risk analysis") == true
                        || request.Message?.Trim().ToLower().Contains("/analyze_okrs_risks") == true;

                     string systemMessage = string.Empty;
                    if (!isOKRTasksAnalysis)
                    {
                    // Enhanced system message with role-based context
                        systemMessage = 
                        "You are an AI assistant for an OKR Management System. Help users manage their teams, objectives, and key results. Use available functions to execute operations.\n\n" +
                        "You may need to execute multiple actions when you receive a user prompt if they need it, you can do so by breaking down " +
                        "complex requests into manageable steps and executing them in the correct order. " +
                        "You can perform multiple operations in sequence when needed, such as creating a team and then adding members to it." +
                        "Each function returns objects and you should return that full object ,in Json as it is, as your response, you should wrap it in the a json string like this ```json ...etc```\n\n" +                        
                        "The system has the following user roles with different permissions:\n\n" +
                        "1. SuperAdmin: Has full access to all features and can manage everything in the system.\n" +
                        "2. OrganizationAdmin: Can manage everything within their organization, including teams (create, update , delete, getAll,..), users (update, Getall, invite), OKR sessions (create, update , delete, getAll), objectives (create, update , delete, getAll), and key results (create, update , delete, getAll).\n" +
                        "3. TeamManager: Can manage their assigned teams (Update, GetAll) ,OKR Sessions (update and GetAll), objectives (create, update , delete, getAll) , and key results (create, update , delete, getAll) for their teams.\n" +
                        "4. Collaborator: Limited access - can view users, teams, OKR sessions, objectives they are part of, vien key results and update their assigned tasks or create new tasks.\n\n" +
                        
                        $"The current user has the role '{userContext?.Role ?? "Unknown"}'. " +
                        "Adjust your responses according to their role and permissions. If they request something they don't have permission for, " +
                        "politely explain the limitation or recommend they contact their administrator.\n\n" +
                        
                        // Add the delete confirmation instruction
                        "IMPORTANT: If the user asks to perform a destructive action (like delete, remove, erase, etc.), " +
                        "ask for confirmation first by responding with a clear confirmation message (e.g., 'Are you sure you want to delete X?'). " +
                        "Do not call any function that performs deletion until the user explicitly confirms.\n" +
                        "Once the user confirms (e.g., 'yes', 'confirm', 'go ahead'), you may proceed with the previously intended delete operation.\n\n";
                    // Enhance the system message with relevant context from vector memory
                    systemMessage = _vectorMemoryService.EnhanceSystemMessageWithContext(systemMessage, relevantContext);
                    }
                    // Check if there's document context in the relevant context
                    bool hasDocumentContext = relevantContext != null && 
                                            (relevantContext.Contains("Document uploaded") || 
                                            relevantContext.Contains("Document analysis"));

                    if (hasDocumentContext)
                    {
                        // Add special instructions for document-related conversations
                        systemMessage += "\n\nYou have previously analyzed a document for this user. " +
                            "Refer to that analysis when answering questions about OKRs or goals. " +
                            "If the user confirms they want to proceed with your OKR suggestions from the document, " +
                            "use function calling to create appropriate OKR entities (session, objectives, key results, tasks). " +
                            "Follow the user's guidance on any changes they want to make to your proposals.\n\n" +
                            "Do not create entities without explicit user confirmation.";                    }

                    // Declare response variable that will be used across different branches
                    string response;
                    byte[]? pdfBytes = null; // Initialize as null, will be set only for OKR analysis

                    if (isOKRTasksAnalysis)
                    {
                        // Use the orchestrator for OKR risk analysis
                        _logger.LogInformation("Using OKR Analysis Orchestrator for risk analysis");
                        response = await _okrAnalysisOrchestrator.RunAnalysisAsync(request.ConversationId, request.Message);
                        
                        // Generate PDF from orchestrator response
                        pdfBytes = _pdfGeneratorService.GeneratePdfFromText("OKR Risk Analysis", response);
                        
                        // Save conversation context to vector memory
                        await _vectorMemoryService.SaveConversationContextAsync(
                            request.ConversationId,
                            $"User: {request.Message}\nAI: {response}",
                            userContext?.UserId);
                          // Add the response to the chat history
                        var orchestratorAssistantMessage = EnhancedChatMessage.FromAssistant(response);
                        orchestratorAssistantMessage.Metadata["AuthorName"] = "AI Assistant (OKR Analysis Orchestrator)";
                        orchestratorAssistantMessage.Metadata["Provider"] = "azureopenai";
                        _kernelService.GetEnhancedHistory(request.ConversationId).AddMessage(orchestratorAssistantMessage);
                        
                        return Ok(new
                        {
                            response = response,
                            intents = new[] { "OKRRiskAnalysis" },
                            parameters = new Dictionary<string, string>(),
                            functionResults = (object)null,
                            chatHistory = _kernelService.GetEnhancedHistory(request.ConversationId).Messages,
                            usingFunctionCalling = true,
                            provider = "azureopenai",
                            organizationContext = !string.IsNullOrEmpty(userContext?.OrganizationId)
                                                ? userContext.OrganizationId
                                                : null,
                            pdf = Convert.ToBase64String(pdfBytes)
                        });
                    }
                      // Check for complex multi-operation request patterns
                    bool isMultiStepRequest = true;
                    
                    if (isMultiStepRequest)
                    {
                        _logger.LogInformation("Detected multi-step request, using multi-action FunctionCalling");
                        response = await _azureOpenAIChatService.ExecuteMultiStepPlanAsync(
                            request.Message, 
                            systemMessage, 
                            userContext, // Pass the user context explicitly
                            cancellationToken);

                        // Add workflow state continuation to ensure we proceed through the OKR generation workflow
                        if (!string.IsNullOrEmpty(request.ConversationId) && hasDocumentContext)
                        {
                            // Apply workflow state tracking and continuation for document-related conversations
                            response = Extensions.OkrWorkflowState.EnsureWorkflowContinuation(response, request.ConversationId);
                            _logger.LogInformation("Applied workflow continuation for OKR generation workflow in conversation {ConversationId}, current step: {CurrentStep}", 
                                request.ConversationId, 
                                Extensions.OkrWorkflowState.GetWorkflowState(request.ConversationId, "CurrentStep", "Unknown"));
                        }
                    }
                    else
                    {
                        // Get the chat history
                        var chatHistory = _kernelService.GetEnhancedHistory(request.ConversationId).Messages.ToList();
                        
                        // Get response from the dedicated Azure OpenAI service using regular method
                        response = await _azureOpenAIChatService.ExecuteChatWithFunctionsAsync(systemMessage, chatHistory);
                    }
                    
                    // Save conversation context to vector memory
                    await _vectorMemoryService.SaveConversationContextAsync(
                        request.ConversationId,
                        $"User: {request.Message}\nAI: {response}",
                        userContext?.UserId);
                    
                    // Add the response to the chat history
                    
                    // var assistantMessage = EnhancedChatMessage.FromAssistant(response);
                    // assistantMessage.Metadata["AuthorName"] = "AI Assistant (Azure OpenAI)";
                    // assistantMessage.Metadata["Provider"] = "azureopenai";
                    // _kernelService.GetEnhancedHistory(request.ConversationId).AddMessage(assistantMessage);

                    // Default to an empty JsonElement (this is a struct, so it's safe)
                    JsonElement jsonObject = default;
                    string responseToReturn = response;
                    _logger.LogInformation("Response before parsing: {Response}", response);
                    // Only try to parse as JSON if the response contains a JSON code block
                    if (response.Contains("```json"))
                    {
                        try
                        {
                            // Clean the formatting
                            string cleaned = response.Replace("```json", "").Replace("```", "").Trim();

                            // Deserialize to JsonElement
                            jsonObject = JsonSerializer.Deserialize<JsonElement>(cleaned);
                            Console.WriteLine("Formatted JSON: {0}", jsonObject);
                            
                            // Extract the prompt template if it exists
                            if (jsonObject.TryGetProperty("PromptTemplate", out JsonElement promptElement))
                            {
                                responseToReturn = promptElement.GetString();
                            }
                        }
                        catch (JsonException ex)
                        {
                            // Log the error but continue with the original response
                            _logger.LogWarning(ex, "Failed to parse response as JSON: {Response}", response);
                        }
                    }
                    var assistantMessage = EnhancedChatMessage.FromAssistant(responseToReturn);
                    assistantMessage.Metadata["AuthorName"] = "AI Assistant (Azure OpenAI)";
                    assistantMessage.Metadata["Provider"] = "azureopenai";
                    assistantMessage.FunctionOutput = jsonObject.ValueKind != JsonValueKind.Undefined ? JsonSerializer.Serialize(jsonObject) : null;
                    _kernelService.GetEnhancedHistory(request.ConversationId).AddMessage(assistantMessage);

                    return Ok(new
                    {
                        response = responseToReturn,
                        intents = new[] { isMultiStepRequest ? "MultiStepPlan" : "AzureOpenAIFunction" },
                        parameters = new Dictionary<string, string>(),
                        functionResults = jsonObject.ValueKind != JsonValueKind.Undefined ? (object)jsonObject : null,
                        chatHistory = _kernelService.GetEnhancedHistory(request.ConversationId).Messages,                        usingFunctionCalling = true,
                        provider = "azureopenai",
                        organizationContext = !string.IsNullOrEmpty(userContext?.OrganizationId)
                                            ? userContext.OrganizationId
                                            : null,
                        pdf = pdfBytes != null ? Convert.ToBase64String(pdfBytes) : null
                    });
                }
                
                // Handle DeepSeek provider
                else if (selectedLlmProvider == "deepseek")
                {
                    _logger.LogInformation("Using DeepSeek service for the request");
                    
                    // Retrieve relevant context from vector store for this conversation
                    var relevantContext = await _vectorMemoryService.GetRelevantContextAsync(
                        request.Message,
                        request.ConversationId);
                    
                    // Create system message for DeepSeek
                    string systemMessage = "You are an AI assistant for an OKR Management System called Tensai. Help users manage their teams, objectives, and key results.";
                    
                    // Add organization context to system message if available
                    if (!string.IsNullOrEmpty(userContext?.OrganizationId))
                    {
                        systemMessage += $"\n\nThe user's organization ID is: {userContext.OrganizationId}.";
                    }
                    
                    // Enhance with relevant context
                    systemMessage = _vectorMemoryService.EnhanceSystemMessageWithContext(systemMessage, relevantContext);
                    
                    // Get chat history and get response
                    var history = _kernelService.GetEnhancedHistory(request.ConversationId);
                    string response = await _kernelService.ExecuteSinglePromptAsync(
                        systemMessage, 
                        request.Message, 
                        request.LLMProvider); // Always use the provider from the request
                    
                    // Save conversation context to vector memory
                    await _vectorMemoryService.SaveConversationContextAsync(
                        request.ConversationId,
                        $"User: {request.Message}\nAI: {response}",
                        userContext?.UserId);
                    
                    // Add the response to the chat history
                    var assistantMessage = EnhancedChatMessage.FromAssistant(response);
                    assistantMessage.Metadata["AuthorName"] = "AI Assistant (DeepSeek)";
                    assistantMessage.Metadata["Provider"] = "deepseek";
                    history.AddMessage(assistantMessage);
                    
                    return Ok(new
                    {
                        response = response,
                        intents = new[] { "GeneralConversation" },
                        parameters = new Dictionary<string, string>(),
                        functionResults = (object)null,
                        chatHistory = history.Messages,
                        usingFunctionCalling = false,
                        provider = "deepseek",
                        organizationContext = !string.IsNullOrEmpty(userContext?.OrganizationId) 
                                            ? userContext.OrganizationId 
                                            : null
                    });
                }
                
                // Original approach for Cohere or when no provider specified

                // First analyze the user's message to detect multiple intents
                var intentRequests = await _intentProcessor.AnalyzePromptAsync(request.ConversationId, request.Message, selectedLlmProvider);
                _logger.LogInformation("Detected {Count} intents for conversation {ConversationId}", 
                    intentRequests.Count, request.ConversationId);

                // Add organization context if available
                foreach (var intentRequest in intentRequests)
                {
                    if (!string.IsNullOrEmpty(userContext.OrganizationId) && 
                        !intentRequest.Parameters.ContainsKey("organizationId"))
                    {
                        intentRequest.Parameters["organizationId"] = userContext.OrganizationId;
                    }
                }

                // Process all detected intents
                var functionResult = await _intentProcessor.ProcessMultipleIntentsAsync(
                    request.ConversationId, intentRequests, userContext);

                // If we have function results, return them directly
                if (functionResult.Success)
                {
                    // Create a merged parameters dictionary that keeps only the last value for each parameter key
                    var mergedParameters = new Dictionary<string, string>();
                    foreach (var intentRequest in intentRequests)
                    {
                        foreach (var param in intentRequest.Parameters)
                        {
                            mergedParameters[param.Key] = param.Value;
                        }
                    }

                    // Return the combined message from all intents
                    return Ok(new
                    {
                        response = functionResult.Message,
                        intents = intentRequests.Select(i => i.Intent).ToList(),
                        parameters = mergedParameters,
                        functionResults = functionResult.Results.Select(r => r.Data).ToList(),
                        chatHistory = _kernelService.GetEnhancedHistory(request.ConversationId).Messages,
                        usingFunctionCalling = false,
                        conversationId = request.ConversationId,
                        provider = selectedLlmProvider
                    });
                }

                // For non-function or failed executions, get AI response
                var aiResponse = await _kernelService.GetAIResponseAsync(
                    request.ConversationId,
                    functionResult.Message ?? request.Message, 
                    userName,
                    selectedLlmProvider); // Pass the provider from the request

                var mergedParams = new Dictionary<string, string>();
                foreach (var intentRequest in intentRequests)
                {
                    foreach (var param in intentRequest.Parameters)
                    {
                        mergedParams[param.Key] = param.Value;
                    }
                }

                return Ok(new
                {
                    response = aiResponse,
                    intents = intentRequests.Select(i => i.Intent).ToList(),
                    parameters = mergedParams,
                    functionResults = (object)null,
                    chatHistory = _kernelService.GetEnhancedHistory(request.ConversationId).Messages,
                    conversationId = request.ConversationId,
                    usingFunctionCalling = false,
                    provider = selectedLlmProvider
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chat request for conversation {ConversationId}", 
                    request.ConversationId);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Reset the chat history for a specific conversation
        /// </summary>
        [HttpPost("reset")]
        public IActionResult ResetChat([FromBody] ConversationRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.ConversationId))
            {
                return BadRequest("ConversationId is required");
            }

            try
            {
                _kernelService.ResetChat(request.ConversationId);
                 _logger.LogInformation("Resetting chat history for conversation {ConversationId}", 
                    request.ConversationId);
                return Ok(new { 
                    message = "Chat history reset successfully", 
                    conversationId = request.ConversationId 
                });
            }
            catch (Exception ex)
            {
                 _logger.LogInformation("Error resetting chat history for conversation {ConversationId}", 
                    request.ConversationId);
                _logger.LogError(ex, "Error resetting chat history for conversation {ConversationId}", 
                    request.ConversationId);
                return StatusCode(500, "An error occurred while resetting chat history.");
            }
        }

        /// <summary>
        /// Reset all chat histories across all conversations
        /// </summary>
        [HttpPost("reset-all")]
        public IActionResult ResetAllChats()
        {
            try
            {
                _logger.LogInformation("Attempting to reset all chat histories");
                
                // Get all conversations first
                var allConversations = _chatHistoryManager.GetAllConversations();
                int conversationCount = allConversations.Count;
                
                foreach (var conversation in allConversations)
                {
                    _kernelService.ResetChat(conversation.Key);
                }
                
                _logger.LogInformation("Successfully reset {Count} chat histories", conversationCount);
                
                return Ok(new { 
                    message = $"All chat histories reset successfully. Reset {conversationCount} conversation(s).", 
                    conversationCount = conversationCount 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting all chat histories");
                return StatusCode(500, "An error occurred while resetting all chat histories.");
            }
        }

        /// <summary>
        /// Get the current chat history for a specific conversation
        /// </summary>
        [HttpGet("history/{conversationId}")]
        public IActionResult GetChatHistory(string conversationId)
        {
            if (string.IsNullOrEmpty(conversationId))
            {
                return BadRequest("ConversationId is required");
            }

            try
            {
                var history = _kernelService.GetEnhancedHistory(conversationId);
                return Ok(new { 
                    messages = history.Messages, 
                    count = history.Messages.Count,
                    conversationId = conversationId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving chat history for conversation {ConversationId}", 
                    conversationId);
                return StatusCode(500, "An error occurred while retrieving chat history.");
            }
        }

        /// <summary>
        /// Set a custom system message for a specific conversation
        /// </summary>
        [HttpPost("system-message")]
        public IActionResult SetSystemMessage([FromBody] SystemMessageWithConversationRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Message))
            {
                return BadRequest("System message is required");
            }

            if (string.IsNullOrEmpty(request.ConversationId))
            {
                return BadRequest("ConversationId is required");
            }

            try
            {
                _kernelService.SetSystemMessage(request.ConversationId, request.Message);
                return Ok(new { 
                    message = "System message updated successfully",
                    conversationId = request.ConversationId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting system message for conversation {ConversationId}", 
                    request.ConversationId);
                return StatusCode(500, "An error occurred while updating the system message.");
            }
        }

        /// <summary>
        /// Get all conversations for a specific user
        /// </summary>
        [HttpGet("conversations/user/{userId}")]
        public IActionResult GetUserConversations(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("UserId is required");
            }

            try
            {
                _logger.LogInformation("Retrieving conversations for user ID: {UserId}", userId);
                var conversations = _chatHistoryManager.GetAllConversationsByUserId(userId);
                _logger.LogInformation("Found {Count} conversations for user ID: {UserId}", conversations.Count, userId);
                
                // First get the timestamps for sorting
                var conversationsWithTimestamps = conversations.Select(c => new
                {
                    ConversationPair = c,
                    Timestamp = GetConversationTimestamp(c.Value)
                }).ToList();
                
                // Format conversation data for client with full message history and parsed function outputs
                var result = conversationsWithTimestamps.OrderByDescending(c => c.Timestamp)
                    .Select(c => new
                    {
                        id = c.ConversationPair.Key,
                        title = GetConversationTitle(c.ConversationPair.Key, c.ConversationPair.Value),
                        timestamp = c.Timestamp,
                        messageCount = c.ConversationPair.Value.Messages.Count,
                        lastMessage = c.ConversationPair.Value.Messages
                            .OrderByDescending(m => m.Timestamp)
                            .FirstOrDefault()?.Content,
                        messages = c.ConversationPair.Value.GetMessagesWithParsedFunctionOutputs()
                            .OrderBy(m => ((dynamic)m).timestamp)
                            .ToList(),
                        userId = userId
                    }).ToList();
                
                _logger.LogDebug("Returning {Count} formatted conversations for user ID: {UserId}", 
                    result.Count, userId);
                
                return Ok(new
                {
                    conversations = result,
                    count = result.Count,
                    userId = userId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving conversations for user {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving user conversations.");
            }
        }
        
        /// <summary>
        /// Helper method to get a title for a conversation
        /// </summary>
        private string GetConversationTitle(string conversationId, EnhancedChatHistory history)
        {
            // Try to generate a title from the first user message
            var firstUserMessage = history.Messages
                .FirstOrDefault(m => m.Role == Microsoft.SemanticKernel.ChatCompletion.AuthorRole.User)?.Content;
            
            if (!string.IsNullOrEmpty(firstUserMessage))
            {
                // Truncate and clean up the message for use as a title
                var title = firstUserMessage.Trim()
                    .Replace("\n", " ")
                    .Replace("\r", "");
                
                // If too long, truncate it
                if (title.Length > 50)
                {
                    title = title.Substring(0, 47) + "...";
                }
                
                return title;
            }
            
            // Fallback to a generic title with the conversation ID
            return $"Conversation {conversationId.Substring(0, Math.Min(conversationId.Length, 8))}";
        }
        
        /// <summary>
        /// Helper method to get the timestamp for a conversation
        /// </summary>
        private DateTime GetConversationTimestamp(EnhancedChatHistory history)
        {
            // Use the latest message timestamp, or default to now if no messages
            return history.Messages
                .OrderByDescending(m => m.Timestamp)
                .FirstOrDefault()?.Timestamp ?? DateTime.UtcNow;
        }

        /// <summary>
        /// Get diagnostic information about conversations and how they're linked to users
        /// </summary>
        [HttpGet("conversations/diagnostics")]
        public IActionResult GetConversationDiagnostics()
        {
            try
            {
                var conversations = _chatHistoryManager.GetAllConversations();
                
                var diagnosticInfo = conversations.Select(c => new
                {
                    conversationId = c.Key,
                    messageCount = c.Value.Messages.Count,
                    userMessageCount = c.Value.Messages.Count(m => m.Role == AuthorRole.User),
                    userIds = c.Value.Messages
                        .Where(m => m.Role == AuthorRole.User && m.Metadata.ContainsKey("UserId"))
                        .Select(m => m.Metadata["UserId"])
                        .Distinct()
                        .ToList(),
                    firstMessageTimestamp = c.Value.Messages.OrderBy(m => m.Timestamp).FirstOrDefault()?.Timestamp,
                    lastMessageTimestamp = c.Value.Messages.OrderByDescending(m => m.Timestamp).FirstOrDefault()?.Timestamp
                }).ToList();
                
                return Ok(new { conversations = diagnosticInfo, count = diagnosticInfo.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving conversation diagnostics");
                return StatusCode(500, "An error occurred while retrieving conversation diagnostics.");
            }
        }

        /// <summary>
        /// Uploads a document to be used in chat conversations, with optional immediate message processing
        /// </summary>
        [HttpPost("documents/upload")]
        [RequestSizeLimit(10 * 1024 * 1024)] // 10MB limit
        public async Task<IActionResult> UploadDocument(
            IFormFile file, 
            [FromQuery] string conversationId,
            [FromQuery] string userId,
            [FromQuery] string message = null )
        {
            // Get the current user context
            var userGuid = Guid.Parse(userId); // Default test user ID
            

            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("Upload document attempt with empty file");
                return BadRequest("No file was uploaded");
            }

            _logger.LogInformation("Upload document attempt for file: {FileName}, size: {FileSize}, conversation: {ConversationId}", 
                file.FileName, file.Length, conversationId);

            try
            {
                var command = new UploadDocumentCommand
                {
                    File = file,
                    UserId = userGuid,
                    OKRSessionId = null // No session ID for chat documents
                };

                var result = await _mediator.Send(command);
                
                // Extract and log a sample of text from the PDF
                if (file.ContentType == "application/pdf" || _documentProcessingService.IsSupportedFileType(file.ContentType))
                {
                    try
                    {
                        // First get the full Document entity which contains the StoragePath
                        var document = await _documentRepository.GetByIdAsync(result.Id);
                        if (document != null)
                        {
                            // Get the document from storage
                            var documentStream = await _documentStorageService.RetrieveDocumentAsync(document.StoragePath);
                            
                            // Use the document processing service for text extraction
                            var extractedText = await _documentProcessingService.ExtractTextFromPdfAsync(documentStream);
                            
                            // Prepare content for OpenAI (optimize for token usage)
                            var optimizedText = await _documentProcessingService.PrepareContentForOpenAIAsync(extractedText);
                            int estimatedTokens = _documentProcessingService.EstimateTokenCount(optimizedText);
                            
                            // Enhanced logging for debugging
                            _logger.LogInformation("Successfully extracted and optimized text from {FileName}: {TextLength} chars, ~{TokenCount} tokens", 
                            document.FileName, optimizedText.Length, estimatedTokens);
                            
                            // Create system message for document analysis
                            string systemMessage = 
                                "You are an AI assistant specializing in OKR (Objectives and Key Results) development. " +
                                "Your task is to analyze a document and extract potential objectives and key results based on its content. " +
                                "Follow this exact step-by-step workflow, waiting for user confirmation at EACH step:\n\n" +
                                
                                "STEP 1: Analyze the document and propose an OKR session with a title, description, start date, and end date.\n" +
                                "        Present this to the user and ASK: 'Would you like me to create this OKR session? Or would you like to make any adjustments first?'\n" +
                                "        Wait for the user to CONFIRM or REQUEST CHANGES.\n" +
                                "        If changes are requested, adjust the proposal and ask again.\n" +
                                "        Only after explicit confirmation, use function calling to create the OKR session.\n\n" +
                                
                                "STEP 2: Propose ONE objective for the session with a title, description, and relevant metrics.\n" +
                                "        Present this to the user and ASK: 'Would you like me to create this objective for the OKR session? Or would you like to make any adjustments?'\n" +
                                "        Wait for the user to CONFIRM or REQUEST CHANGES.\n" +
                                "        Only after explicit confirmation, use function calling to create the objective.\n\n" +
                                
                                "STEP 3: Propose ONE key result for the objective with a title, description, and measurable target.\n" +
                                "        Present this to the user and ASK: 'Would you like me to create this key result for the objective? Or would you like to make any adjustments?'\n" +
                                "        Wait for the user to CONFIRM or REQUEST CHANGES.\n" +
                                "        Only after explicit confirmation, use function calling to create the key result.\n\n" +
                                
                                "STEP 4: Propose ONE task for the key result with a title, description, and target completion date.\n" +
                                "        Present this to the user and ASK: 'Would you like me to create this task for the key result? Or would you like to make any adjustments?'\n" +
                                "        Wait for the user to CONFIRM or REQUEST CHANGES.\n" +
                                "        Only after explicit confirmation, use function calling to create the key result task.\n\n" +
                                
                                "IMPORTANT RULES:\n" +
                                "- Do NOT create any entities until the user explicitly confirms each step.\n" +
                                "- Create ONLY ONE entity at each level (one session, one objective, one key result, one task).\n" +
                                "- Do NOT skip steps or create multiple entities at once.\n" +
                                "- If the user asks to make changes to a proposal, modify it according to their feedback and present it again for confirmation.\n" +
                                "- After creating an entity, show a clear confirmation and propose the next one in the sequence.\n\n" +
                                
                                "Example of good analysis: If the document mentions 'Increase customer satisfaction by 20% in Q1', " +   
                                "you should identify 'Increase customer satisfaction' as an objective and '20% increase in satisfaction ratings' as a key result.\n\n" +
                                
                                $"Document metadata: {document.FileName}, uploaded on {document.UploadDate:yyyy-MM-dd}";
                            
                            // Process document with Azure OpenAI
                            // string defaultQuery = "Please analyze this document and suggest potential OKRs that could be implemented based on its content.";
                            string analysisResponse = await _azureOpenAIChatService.ProcessDocumentWithOpenAIAsync(
                                systemMessage,
                                extractedText,
                                message);
                            
                            // Add this code to track the initial workflow state and ensure continuation
                            if (!string.IsNullOrEmpty(conversationId))
                            {
                                // Initialize workflow state for this conversation
                                Extensions.OkrWorkflowState.ResetWorkflowState(conversationId);
                                Extensions.OkrWorkflowState.TrackWorkflowState(conversationId, "DocumentId", document.Id.ToString());
                                Extensions.OkrWorkflowState.TrackWorkflowState(conversationId, "CurrentStep", "DocumentProcessed");
                                
                                // Apply workflow continuation to ensure we follow the complete process
                                analysisResponse = Extensions.OkrWorkflowState.EnsureWorkflowContinuation(analysisResponse, conversationId);
                            }
                            
                            // Save document content and analysis to vector memory for future reference
                            if (!string.IsNullOrEmpty(conversationId))
                            {
                                _logger.LogInformation("Saving document context to vector memory with conversationId: '{ConversationId}'", conversationId);
                                
                                await _vectorMemoryService.SaveConversationContextAsync(
                                    conversationId,
                                    $"Document uploaded: {document.FileName}\nDocument analysis: {analysisResponse}",
                                    userId);
                                
                                // Add document metadata to the conversation history
                                var docSystemMessage = EnhancedChatMessage.FromSystem(
                                    $"Document uploaded: {document.FileName}. Analysis available for conversation.");
                                docSystemMessage.Metadata["DocumentId"] = document.Id.ToString();
                                docSystemMessage.Metadata["DocumentFileName"] = document.FileName;
                                _kernelService.GetEnhancedHistory(conversationId).AddMessage(docSystemMessage);
                                
                                // Add document analysis as an assistant message
                                var assistantMessage = EnhancedChatMessage.FromAssistant(analysisResponse);
                                assistantMessage.Metadata["DocumentId"] = document.Id.ToString();
                                assistantMessage.Metadata["DocumentFileName"] = document.FileName;
                                assistantMessage.Metadata["Provider"] = "azureopenai";
                                _kernelService.GetEnhancedHistory(conversationId).AddMessage(assistantMessage);
                                
                                _logger.LogInformation("Document context added to conversation history and vector memory: {ConversationId}, {DocumentId}", 
                                    conversationId, document.Id);
                            }
                            
                            // If no message to process, return just the document info with extracted text
                            return Ok(new
                            {
                                document = result,
                                response = analysisResponse,
                                message = message                             
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to extract text from PDF for logging purposes");
                        // Continue with normal flow - don't fail the upload just because text extraction failed
                    }
                }
                
                _logger.LogInformation("Document uploaded successfully: {DocumentId}", result.Id);
                return Ok(result);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation failed for document upload: {FileName}", file.FileName);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document: {FileName}", file.FileName);
                return StatusCode(500, "An error occurred while uploading the document");
            }
        }

        /// <summary>
        /// Endpoint to get AI-powered insights for a session
        /// </summary>
        [HttpPost("session-insights")]
        public async Task<IActionResult> GetSessionInsights([FromBody] SessionInsightsRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null || string.IsNullOrEmpty(request.SessionId))
                return BadRequest("SessionId is required.");

            try
            {
                var userContext = request.UserContext ?? new UserContext();
                _userContextAccessor.CurrentUserContext = userContext;

                var insights = await _azureOpenAIChatService.GetSessionInsightsAsync(request.SessionId, userContext, cancellationToken);
                _logger.LogInformation("Generated insights for session {SessionId}: {Insights}", request.SessionId, insights);
                return Ok(new SessionInsightsResponse { Insights = insights });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating session insights for session {SessionId}", request.SessionId);
                return StatusCode(500, "Failed to generate session insights.");
            }
        }

        /// <summary>
        /// Generate dynamic OKR session suggestions using AI
        /// </summary>
        [HttpPost("okr-suggestions")]
        public async Task<IActionResult> GetOkrSessionSuggestions([FromBody] OkrSuggestionRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Prompt))
                return BadRequest("Prompt is required.");

            try
            {
                // Call the AI service to get suggestions
                var suggestions = await _azureOpenAIChatService.GetOkrSessionSuggestionsAsync(request, cancellationToken);
                return Ok(suggestions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate OKR session suggestions");
                return StatusCode(500, "Failed to generate OKR session suggestions");
            }
        }
    }
}