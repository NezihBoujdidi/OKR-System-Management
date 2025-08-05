using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using NXM.Tensai.Back.OKR.AI.Models;
using NXM.Tensai.Back.OKR.AI.Services.ChatHistoryService;
using NXM.Tensai.Back.OKR.AI.Core.AI.Plugins;

namespace NXM.Tensai.Back.OKR.AI.Services
{
    /// <summary>
    /// Service to manage Semantic Kernel operations
    /// </summary>
    public class KernelService
    {
        private readonly ILogger<KernelService> _logger;
        private readonly EnhancedChatHistory _enhancedHistory = new();
        private string _systemMessage = "You are an AI assistant for an OKR Management System. You help users manage their teams, objectives, and key results.";
        private readonly string _openAiSimplifiedSystemMessage = "You are an AI assistant for Tensai OKR Management System. Help users manage their teams, objectives, and key results. Use available functions to execute operations.";
        private readonly IConfiguration _configuration;
        private readonly string _defaultProvider;
        private readonly IServiceProvider _serviceProvider;
        private readonly ChatHistoryManager _chatHistoryManager;
        private readonly ILoggerFactory _loggerFactory;

        // Dictionary to store cached kernels by provider
        private readonly Dictionary<string, Kernel> _kernels = new();
        private readonly Dictionary<string, IChatCompletionService> _chatServices = new();

        public KernelService(
            IConfiguration configuration, 
            ILogger<KernelService> logger,
            IServiceProvider serviceProvider,
            ChatHistoryManager chatHistoryManager,
            ILoggerFactory loggerFactory) // Added logger factory
        {
            _logger = logger;
            _configuration = configuration;
            _serviceProvider = serviceProvider;
            _defaultProvider = configuration["AI:Provider"]?.ToLower() ?? "azureopenai";
            _chatHistoryManager = chatHistoryManager;
            _loggerFactory = loggerFactory;

            // Initialize history with system message
            AddSystemMessage(null, _systemMessage);

            _logger.LogInformation("KernelService initialized with default provider: {Provider}", _defaultProvider);
        }

        /// <summary>
        /// Get or create a kernel for the specified provider
        /// </summary>
        public Kernel GetKernelForProvider(string provider = null)
        {
            provider = provider?.ToLower() ?? _defaultProvider;
            
            // Return cached kernel if we have one
            if (_kernels.TryGetValue(provider, out var existingKernel))
            {
                return existingKernel;
            }
            
            _logger.LogInformation("Creating new kernel for provider: {Provider}", provider);
            
            // Create a new kernel for the provider
            var builder = Kernel.CreateBuilder();
            
            try
            {
                if (provider == "cohere")
                {
                    // Configure Cohere
                    var cohereApiKey = _configuration["Cohere:ApiKey"]
                        ?? throw new ArgumentNullException("Cohere:ApiKey", "API key is required in configuration.");
                    var cohereModel = _configuration["Cohere:Model"] ?? "command";
                    
                    // Create and register the Cohere service
                    var cohereLogger = _loggerFactory.CreateLogger<CohereChatCompletionService>();
                    var cohereService = new CohereChatCompletionService(cohereApiKey, cohereModel, cohereLogger);
                    builder.Services.AddSingleton<IChatCompletionService>(cohereService);
                    
                    _logger.LogInformation("Configured Kernel with Cohere model: {ModelName}", cohereModel);
                }
                else if (provider == "azureopenai")
                {
                    // Configure Azure OpenAI
                    var azureApiKey = _configuration["AzureOpenAI:ApiKey"]
                        ?? throw new ArgumentNullException("AzureOpenAI:ApiKey", "API key is required in configuration.");
                    var endpoint = _configuration["AzureOpenAI:Endpoint"]
                        ?? throw new ArgumentNullException("AzureOpenAI:Endpoint", "Endpoint is required in configuration.");
                    var deploymentName = _configuration["AzureOpenAI:DeploymentName"] ?? "gpt-4o";
                    var modelId = _configuration["AzureOpenAI:ModelId"] ?? "gpt-4o";
                    
                    // Log endpoint for debugging
                    _logger.LogInformation("Using Azure OpenAI endpoint: {Endpoint}", endpoint);

                    // Ensure endpoint doesn't end with a trailing slash
                    endpoint = endpoint.TrimEnd('/');
                    
                    // Get API version from configuration
                    var apiVersion = _configuration["AzureOpenAI:ApiVersion"] ?? "2025-01-01-preview";

                    // Add Azure OpenAI chat completion with API version
                    builder.AddAzureOpenAIChatCompletion(
                        deploymentName: deploymentName,
                        endpoint: endpoint,
                        apiKey: azureApiKey,
                        modelId: modelId,
                        apiVersion: apiVersion
                    );

                    _logger.LogInformation("Configured Kernel with Azure OpenAI model: {ModelName} at deployment {DeploymentName} with API version {ApiVersion}", 
                        modelId, deploymentName, apiVersion);
                }
                else if (provider == "deepseek")
                {
                    // For DeepSeek, we'll use the service already registered in dependency injection
                    var deepSeekService = _serviceProvider.GetRequiredService<IChatCompletionService>();
                    builder.Services.AddSingleton<IChatCompletionService>(deepSeekService);
                    
                    _logger.LogInformation("Configured Kernel with DeepSeek chat completion service");
                }
                
                // Build the kernel
                var kernel = builder.Build();
                
                // Register plugins for function calling if provider is OpenAI or Azure OpenAI
                if (provider == "azureopenai")
                {
                    try
                    {
                        // Use the plugin registration service to register plugins
                        var pluginRegistrationService = _serviceProvider.GetRequiredService<PluginRegistrationService>();
                        pluginRegistrationService.RegisterPlugins(kernel);
                        _logger.LogInformation("Registered plugins for {Provider} function calling", provider);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error registering plugins for {Provider}", provider);
                    }
                }
                
                // Cache the kernel and chat service
                _kernels[provider] = kernel;
                _chatServices[provider] = kernel.GetRequiredService<IChatCompletionService>();
                
                return kernel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating kernel for provider: {Provider}", provider);
                throw;
            }
        }

        /// <summary>
        /// Get the chat completion service for the specified provider
        /// </summary>
        public IChatCompletionService GetChatCompletionService(string provider = null)
        {
            provider = provider?.ToLower() ?? _defaultProvider;
            
            // Get or create the kernel for this provider first
            var kernel = GetKernelForProvider(provider);
            
            // Return cached chat service if we have one
            if (_chatServices.TryGetValue(provider, out var existingChatService))
            {
                return existingChatService;
            }
            
            // Get the chat service from the kernel
            var chatService = kernel.GetRequiredService<IChatCompletionService>();
            _chatServices[provider] = chatService;
            return chatService;
        }

        /// <summary>
        /// Initialize a new conversation with the system message
        /// </summary>
        public void InitializeConversation(string conversationId)
        {
            var history = _chatHistoryManager.GetChatHistory(conversationId);
            if (history.Messages.Count == 0)
            {
                AddSystemMessage(conversationId, _systemMessage);
            }
        }

        /// <summary>
        /// Set the system message for a specific conversation
        /// </summary>
        public void SetSystemMessage(string conversationId, string message)
        {
            var history = _chatHistoryManager.GetChatHistory(conversationId);
            history.Clear();
            AddSystemMessage(conversationId, message);
            _logger.LogInformation("System message set for conversation {ConversationId}: {Message}", 
                conversationId, message);
        }

        /// <summary>
        /// Add a system message to the chat history
        /// </summary>
        private void AddSystemMessage(string conversationId, string message)
        {
            var history = _chatHistoryManager.GetChatHistory(conversationId);
            history.AddMessage(EnhancedChatMessage.FromSystem(message));
        }

        /// <summary>
        /// Reset a specific conversation's chat history
        /// </summary>
        public void ResetChat(string conversationId)
        {
            _chatHistoryManager.ResetChatHistory(conversationId);
            InitializeConversation(conversationId);
            _logger.LogInformation("Chat history reset for conversation {ConversationId}", conversationId);
        }

        /// <summary>
        /// Record a function execution in a conversation's chat history
        /// </summary>
        public void RecordFunctionExecution<T>(
            string conversationId,
            string functionName,
            T result,
            string entityType = null,
            string entityId = null,
            string operation = null)
        {
            var history = _chatHistoryManager.GetChatHistory(conversationId);
            
            // Extract entity name for better context if available
            string entityName = null;
            if (result != null)
            {
                try
                {
                    // Try to get Name property via reflection for better context
                    var nameProperty = result.GetType().GetProperty("Name");
                    entityName = nameProperty?.GetValue(result)?.ToString();
                    
                    _logger.LogDebug("Extracted entity name '{EntityName}' from result", entityName);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not extract name from function result");
                }
            }

            var message = EnhancedChatMessage.FromFunctionExecution(
                functionName,
                result,
                entityType,
                entityId,
                operation);
                
            // Add entity name to metadata if available
            if (!string.IsNullOrEmpty(entityName))
            {
                message.Metadata["EntityName"] = entityName;
            }
            
            history.AddMessage(message);
            
            _logger.LogInformation(
                "Recorded function execution for conversation {ConversationId}: {Function} on {EntityType} {EntityId} ({Operation})",
                conversationId, functionName, entityType, entityId, operation);
        }

        /// <summary>
        /// Get a response from the AI based on the prompt and conversation history
        /// </summary>
        public async Task<string> GetAIResponseAsync(string conversationId, string prompt, string userName = "User", string selectedLlmProvider = null)
        {
            try
            {
                _logger.LogInformation("Getting AI response for conversation {ConversationId}, prompt: {Prompt}, provider: {Provider}", 
                    conversationId, prompt, selectedLlmProvider ?? _defaultProvider);

                var history = _chatHistoryManager.GetChatHistory(conversationId);
                
                // Initialize the conversation if it's new
                if (history.Messages.Count == 0)
                {
                    InitializeConversation(conversationId);
                }

                // Convert enhanced history to Semantic Kernel format
                var skHistory = history.ToSemanticKernelHistory();
                
                // Get the chat completion service for the specified provider
                var chatCompletionService = GetChatCompletionService(selectedLlmProvider);
                
                // Get completion from the service
                var results = await chatCompletionService.GetChatMessageContentsAsync(skHistory);
                var result = results[0]; // Get the first response
                
                // Add AI response to chat history
                var assistantMessage = EnhancedChatMessage.FromAssistant(result.Content);
                assistantMessage.Metadata["AuthorName"] = "AI Assistant";
                assistantMessage.Metadata["Provider"] = selectedLlmProvider ?? _defaultProvider;
                history.AddMessage(assistantMessage);
                
                return result.Content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting AI response for conversation {ConversationId}", conversationId);
                throw;
            }
        }

        /// <summary>
        /// Execute a single prompt without affecting any conversation history
        /// </summary>
        public async Task<string> ExecuteSinglePromptAsync(string systemMessage, string prompt, string selectedLlmProvider = null)
        {
            try
            {
                _logger.LogInformation("Executing single prompt with provider {Provider}: {Prompt}", 
                    selectedLlmProvider ?? _defaultProvider, prompt);
                
                // Get the chat service for the specified provider
                var chatCompletionService = GetChatCompletionService(selectedLlmProvider);
                
                var tempHistory = new ChatHistory();
                tempHistory.AddSystemMessage(systemMessage);
                tempHistory.AddUserMessage(prompt);

                var results = await chatCompletionService.GetChatMessageContentsAsync(tempHistory);
                return results[0].Content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing single prompt");
                throw;
            }
        }
        
        /// <summary>
        /// Get the enhanced chat history for a specific conversation
        /// </summary>
        public EnhancedChatHistory GetEnhancedHistory(string conversationId)
        {
            return _chatHistoryManager.GetChatHistory(conversationId);
        }

        /// <summary>
        /// Get the most recent entity ID of a specific type for a conversation
        /// </summary>
        public string GetMostRecentEntityId(string conversationId, string entityType)
        {
            var history = _chatHistoryManager.GetChatHistory(conversationId);
            return history.GetMostRecentEntityId(entityType);
        }

        /// <summary>
        /// Get the last function result of a specific type for a conversation
        /// </summary>
        public T GetLastFunctionResult<T>(string conversationId, string functionName)
        {
            var history = _chatHistoryManager.GetChatHistory(conversationId);
            return history.GetLastFunctionResult<T>(functionName);
        }
    }
}