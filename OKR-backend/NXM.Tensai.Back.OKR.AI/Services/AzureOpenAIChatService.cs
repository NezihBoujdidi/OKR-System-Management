using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using NXM.Tensai.Back.OKR.AI.Core.AI.Plugins;
using NXM.Tensai.Back.OKR.AI.Models;
using NXM.Tensai.Back.OKR.AI.Extensions;
using NXM.Tensai.Back.OKR.AI.Services.Interfaces;
// using NXM.Tensai.Back.OKR.Infrastructure;
using NXM.Tensai.Back.OKR.Domain.Entities;
using NXM.Tensai.Back.OKR.Domain.Interfaces.Repositories;
using NXM.Tensai.Back.OKR.Domain;

namespace NXM.Tensai.Back.OKR.AI.Services
{
    /// <summary>
    /// Service providing Azure OpenAI chat completions with automatic function execution
    /// </summary>
    public class AzureOpenAIChatService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AzureOpenAIChatService> _logger;
        private readonly PluginRegistrationService _pluginRegistrationService;
        private readonly TeamPlugin _teamPlugin;
        private readonly UserPlugin _userPlugin;
        private readonly IDocumentProcessingService _documentProcessingService;
        private readonly IObjectiveRepository _objectiveRepository;
        private readonly IKeyResultRepository _keyResultRepository;
        private readonly IKeyResultTaskRepository _keyResultTaskRepository;
        
        // Cache the kernel for performance
        private Kernel _kernel;

        public AzureOpenAIChatService(
            IConfiguration configuration,
            PluginRegistrationService pluginRegistrationService,
            TeamPlugin teamPlugin,
            UserPlugin userPlugin,
            IDocumentProcessingService documentProcessingService,
            IObjectiveRepository objectiveRepository,
            IKeyResultRepository keyResultRepository,
            IKeyResultTaskRepository keyResultTaskRepository,
            ILogger<AzureOpenAIChatService> logger)
        {
            _configuration = configuration;
            _pluginRegistrationService = pluginRegistrationService;
            _teamPlugin = teamPlugin;
            _userPlugin = userPlugin;
            _documentProcessingService = documentProcessingService;
            _objectiveRepository = objectiveRepository;
            _keyResultRepository = keyResultRepository;
            _keyResultTaskRepository = keyResultTaskRepository;
            _logger = logger;
        }

        /// <summary>
        /// Get or create a kernel with plugins registered, with enhanced diagnostics
        /// </summary>
        private Kernel GetOrCreateKernel()
        {
            if (_kernel != null)
            {
                return _kernel;
            }

            try
            {
                _logger.LogInformation("Creating new Azure OpenAI kernel with plugins");
                
                // Get configuration values with validation
                var apiKey = _configuration["AzureOpenAI:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogError("AzureOpenAI:ApiKey is missing in configuration");
                    throw new InvalidOperationException("AzureOpenAI:ApiKey not found in configuration");
                }
                
                var endpoint = _configuration["AzureOpenAI:Endpoint"];
                if (string.IsNullOrEmpty(endpoint))
                {
                    _logger.LogError("AzureOpenAI:Endpoint is missing in configuration");
                    throw new InvalidOperationException("AzureOpenAI:Endpoint not found in configuration");
                }
                
                var deploymentName = _configuration["AzureOpenAI:DeploymentName"] ?? "gpt-4o";
                var modelId = _configuration["AzureOpenAI:ModelId"] ?? "gpt-4o";
                var apiVersion = _configuration["AzureOpenAI:ApiVersion"] ?? "2025-01-01-preview";
                
                // Ensure endpoint doesn't end with trailing slash
                endpoint = endpoint.TrimEnd('/');
                
                // Add diagnostic logging
                _logger.LogInformation("Azure OpenAI Configuration:");
                _logger.LogInformation(" - Endpoint: {Endpoint}", endpoint);
                _logger.LogInformation(" - Deployment Name: {DeploymentName}", deploymentName);
                _logger.LogInformation(" - Model ID: {ModelId}", modelId);
                _logger.LogInformation(" - API Version: {ApiVersion}", apiVersion);
                
                // Test basic connectivity before creating kernel
                TestEndpointConnectivity(endpoint).GetAwaiter().GetResult();
                
                // Create kernel builder
                var kernelBuilder = Kernel.CreateBuilder();
                
                // Add Azure OpenAI Chat Completion
                kernelBuilder.AddAzureOpenAIChatCompletion(
                    deploymentName: deploymentName,
                    endpoint: endpoint,
                    apiKey: apiKey,
                    modelId: modelId,
                    apiVersion: apiVersion);
                
                // Build the kernel
                _kernel = kernelBuilder.Build();
                
                // Register plugins (functions) into the kernel
                _pluginRegistrationService.RegisterPlugins(_kernel);
                
                _logger.LogInformation("Azure OpenAI kernel created successfully with deployment {DeploymentName}, model {ModelId}", 
                    deploymentName, modelId);
                
                return _kernel;
            }
            catch (Exception ex)
            {
                // Enhanced error logging with exception details
                _logger.LogError(ex, "Error creating Azure OpenAI kernel: {ErrorType}: {ErrorMessage}", 
                    ex.GetType().Name, ex.Message);
                
                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner exception: {InnerErrorType}: {InnerErrorMessage}", 
                        ex.InnerException.GetType().Name, ex.InnerException.Message);
                    
                    if (ex.InnerException.InnerException != null)
                    {
                        _logger.LogError("Nested inner exception: {NestedErrorType}: {NestedErrorMessage}", 
                            ex.InnerException.InnerException.GetType().Name, 
                            ex.InnerException.InnerException.Message);
                    }
                }
                
                throw;
            }
        }

        /// <summary>
        /// Tests basic connectivity to the endpoint before attempting to create a Kernel
        /// </summary>
        private async Task TestEndpointConnectivity(string endpoint)
        {
            try
            {
                _logger.LogInformation("Testing basic connectivity to Azure OpenAI endpoint: {Endpoint}", endpoint);
                
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);
                
                // Just try to connect to the base domain without making an actual API call
                var uri = new Uri(endpoint);
                var baseUrl = $"{uri.Scheme}://{uri.Host}";
                
                _logger.LogDebug("Sending HTTP HEAD request to: {BaseUrl}", baseUrl);
                var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, baseUrl));
                
                _logger.LogInformation("Connection test status: {StatusCode}", response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to Azure OpenAI endpoint: {ErrorMessage}", ex.Message);
                
                if (ex is HttpRequestException httpEx)
                {
                    _logger.LogError("HTTP request error: {ErrorMessage}", httpEx.Message);
                    if (httpEx.InnerException != null && httpEx.InnerException is System.Net.Sockets.SocketException socketEx)
                    {
                        _logger.LogError("Socket error: {ErrorCode} - {ErrorMessage}", socketEx.ErrorCode, socketEx.Message);
                        _logger.LogError("Socket error details: NativeErrorCode={NativeErrorCode}", socketEx.NativeErrorCode);
                    }
                }
                
                throw new InvalidOperationException($"Cannot connect to Azure OpenAI endpoint: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Run a diagnostic check on the Azure OpenAI configuration
        /// </summary>
        public async Task<Dictionary<string, string>> RunDiagnosticsAsync()
        {
            var results = new Dictionary<string, string>();
            
            try
            {
                // Check configuration values
                var apiKey = _configuration["AzureOpenAI:ApiKey"];
                var endpoint = _configuration["AzureOpenAI:Endpoint"];
                var deploymentName = _configuration["AzureOpenAI:DeploymentName"];
                var modelId = _configuration["AzureOpenAI:ModelId"];
                var apiVersion = _configuration["AzureOpenAI:ApiVersion"];
                
                results["ApiKey"] = !string.IsNullOrEmpty(apiKey) ? "Configured (redacted)" : "Missing";
                results["Endpoint"] = endpoint ?? "Missing";
                results["DeploymentName"] = deploymentName ?? "Missing";
                results["ModelId"] = modelId ?? "Missing";
                results["ApiVersion"] = apiVersion ?? "Missing";
                
                // Check TCP connectivity to the endpoint
                if (!string.IsNullOrEmpty(endpoint))
                {
                    try
                    {
                        var uri = new Uri(endpoint);
                        using var tcpClient = new System.Net.Sockets.TcpClient();
                        await tcpClient.ConnectAsync(uri.Host, uri.Port);
                        results["TcpConnectivity"] = "Success";
                    }
                    catch (Exception ex)
                    {
                        results["TcpConnectivity"] = $"Failed: {ex.Message}";
                    }
                }
                else
                {
                    results["TcpConnectivity"] = "Skipped (no endpoint)";
                }
                
                // Try to create a kernel
                try
                {
                    _kernel = null; // Force creation of a new kernel
                    var kernel = GetOrCreateKernel();
                    results["KernelCreation"] = "Success";
                    
                    // Try a simple completion
                    try
                    {
                        var chatHistory = new ChatHistory();
                        chatHistory.AddSystemMessage("You are a helpful assistant.");
                        chatHistory.AddUserMessage("Say hello");
                        
                        var settings = new PromptExecutionSettings { 
                            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() 
                        };
                        
                        if (settings.ExtensionData == null)
                        {
                            settings.ExtensionData = new Dictionary<string, object>();
                        }
                        settings.ExtensionData["api-version"] = apiVersion ?? "2025-01-01-preview";
                        
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                        var chatService = kernel.GetRequiredService<IChatCompletionService>();
                        var response = await chatService.GetChatMessageContentsAsync(
                            chatHistory, settings, kernel, cts.Token);
                        
                        results["TestCompletion"] = $"Success: {response[0].Content.Substring(0, 
                            Math.Min(response[0].Content.Length, 50))}...";
                    }
                    catch (Exception ex)
                    {
                        results["TestCompletion"] = $"Failed: {ex.Message}";
                        if (ex.InnerException != null)
                            results["TestCompletionInnerError"] = ex.InnerException.Message;
                    }
                }
                catch (Exception ex)
                {
                    results["KernelCreation"] = $"Failed: {ex.Message}";
                    if (ex.InnerException != null)
                        results["KernelCreationInnerError"] = ex.InnerException.Message;
                }
                
                // Check DNS resolution
                if (!string.IsNullOrEmpty(endpoint))
                {
                    try
                    {
                        var uri = new Uri(endpoint);
                        var hostEntry = await System.Net.Dns.GetHostEntryAsync(uri.Host);
                        var ips = hostEntry.AddressList;
                        results["DnsResolution"] = $"Success: Resolved to {ips.Length} IP addresses";
                        if (ips.Length > 0)
                        {
                            results["DnsFirstIp"] = ips[0].ToString();
                        }
                    }
                    catch (Exception ex)
                    {
                        results["DnsResolution"] = $"Failed: {ex.Message}";
                    }
                }
                
                // Check if HTTP connectivity works
                if (!string.IsNullOrEmpty(endpoint))
                {
                    try
                    {
                        using var httpClient = new HttpClient();
                        httpClient.Timeout = TimeSpan.FromSeconds(10);
                        var uri = new Uri(endpoint);
                        var baseUrl = $"{uri.Scheme}://{uri.Host}";
                        var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, baseUrl));
                        results["HttpConnectivity"] = $"Success: Status {response.StatusCode}";
                    }
                    catch (Exception ex)
                    {
                        results["HttpConnectivity"] = $"Failed: {ex.Message}";
                        if (ex.InnerException != null)
                            results["HttpConnectivityInnerError"] = ex.InnerException.Message;
                    }
                }
                
                results["DiagnosticsResult"] = "Completed";
            }
            catch (Exception ex)
            {
                results["DiagnosticsError"] = $"{ex.GetType().Name}: {ex.Message}";
                _logger.LogError(ex, "Error running Azure OpenAI diagnostics");
            }
            
            return results;
        }

        /// <summary>
        /// Execute a chat completion with automatic function calling
        /// </summary>
        public async Task<string> ExecuteChatWithFunctionsAsync(
            string systemMessage,
            List<EnhancedChatMessage> messages,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Get or create the kernel
                var kernel = GetOrCreateKernel();
                
                // Get the chat completion service
                var chatService = kernel.GetRequiredService<IChatCompletionService>();
                
                // Create a chat history
                var chatHistory = new ChatHistory();
                
                // Add system message
                if (!string.IsNullOrEmpty(systemMessage))
                {
                    chatHistory.AddSystemMessage(systemMessage);
                }
                
                // Add organization context to first user message if not already in system message
                if (messages.Any() && !systemMessage.Contains("organization ID"))
                {
                    var firstUserMessage = messages.FirstOrDefault(m => m.Role == AuthorRole.User);
                    if (firstUserMessage != null && firstUserMessage.Metadata.TryGetValue("OrganizationId", out var orgId))
                    {
                        _logger.LogDebug("Adding organization context to first user message: {OrgId}", orgId);
                        // We don't modify the chat history here, just ensure context is passed to the LLM
                    }
                }
                if (messages.Any() && !systemMessage.Contains("user ID"))
                {
                    var firstUserMessage = messages.FirstOrDefault(m => m.Role == AuthorRole.User);
                    if (firstUserMessage != null && firstUserMessage.Metadata.TryGetValue("UserId", out var userId))
                    {
                        _logger.LogDebug("Adding user context to first user message: {UserId}", userId);
                        // We don't modify the chat history here, just ensure context is passed to the LLM
                    }
                }
                // Add the most recent messages from history (limit to avoid token limits)
                foreach (var message in messages.TakeLast(10))
                {
                    if (message.Role == AuthorRole.User)
                    {
                        chatHistory.AddUserMessage(message.Content);
                    }
                    else if (message.Role == AuthorRole.Assistant && !string.IsNullOrEmpty(message.Content))
                    {
                        chatHistory.AddAssistantMessage(message.Content);
                    }
                    // Skip function messages since they don't map directly
                }

                // Configure for automatic function invocation using FunctionChoiceBehavior.Auto()
                PromptExecutionSettings settings = new() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() };
                
                // Add API version for Azure OpenAI if needed
                if (settings.ExtensionData == null)
                {
                    settings.ExtensionData = new Dictionary<string, object>();
                }
                
                settings.ExtensionData["api-version"] = _configuration["AzureOpenAI:ApiVersion"] ?? "2025-01-01-preview";
                
                // Get response with automatic function calling
                _logger.LogInformation("Sending chat with {MessageCount} messages to Azure OpenAI", chatHistory.Count);
                
                // Debug log the actual user message for troubleshooting
                var lastUserMessage = chatHistory.LastOrDefault(m => m.Role == AuthorRole.User)?.Content;
                if (!string.IsNullOrEmpty(lastUserMessage))
                {
                    _logger.LogDebug("Last user message: {Message}", lastUserMessage);
                }
                
                // Set a timeout for the request
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);
                
                try
                {
                    var response = await chatService.GetChatMessageContentsAsync(
                        chatHistory, settings, kernel, linkedCts.Token);
                    
                    _logger.LogInformation("Azure OpenAI response received successfully");
                    return response[0].Content;
                }
                catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
                {
                    _logger.LogWarning("Azure OpenAI request timed out after 60 seconds");
                    return "I'm sorry, but the request to Azure OpenAI timed out. Please try again with a simpler request.";
                }
            }
            catch (Exception ex)
            {
                var errorBuilder = new StringBuilder();
                errorBuilder.AppendLine($"Error executing chat with functions: {ex.GetType().Name}: {ex.Message}");
                
                // Log nested exceptions for better diagnostics
                Exception innerEx = ex.InnerException;
                int level = 1;
                while (innerEx != null)
                {
                    string indent = new string(' ', level * 2);
                    errorBuilder.AppendLine($"{indent}Inner exception level {level}: {innerEx.GetType().Name}: {innerEx.Message}");
                    
                    // Special handling for socket exceptions
                    if (innerEx is System.Net.Sockets.SocketException socketEx)
                    {
                        errorBuilder.AppendLine($"{indent}Socket error code: {socketEx.SocketErrorCode}, native error: {socketEx.NativeErrorCode}");
                    }
                    
                    // Special handling for HTTP exceptions
                    if (innerEx is HttpRequestException httpEx)
                    {
                        errorBuilder.AppendLine($"{indent}HTTP status code: {httpEx.StatusCode}");
                    }
                    
                    innerEx = innerEx.InnerException;
                    level++;
                }
                
                _logger.LogError("{ErrorDetails}", errorBuilder.ToString());
                return $"I encountered an error while processing your request: {ex.Message}. Please check logs for more information.";
            }
        }
        
        /// <summary>
        /// Execute a streaming chat completion with automatic function calling
        /// </summary>
        public async IAsyncEnumerable<string> ExecuteStreamingChatWithFunctionsAsync(
            string systemMessage,
            List<EnhancedChatMessage> messages,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // For simplicity, use the non-streaming version for now
            // This eliminates the CS1626 error by avoiding yield in try/catch blocks
            string response = await ExecuteChatWithFunctionsAsync(systemMessage, messages, cancellationToken);
            
            // Return the single response as if it were streaming
            yield return response;
        }

       /// <summary>
        /// Execute a multi-step operation using function calling with Auto behavior
        /// </summary>
        public async Task<string> ExecuteMultiStepPlanAsync(
            string userMessage,
            string systemMessage = null,
            UserContext userContext = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Get or create the kernel
                var kernel = GetOrCreateKernel();
                
                // Get the chat completion service
                var chatService = kernel.GetRequiredService<IChatCompletionService>();
                // Create a chat history with system message
                var chatHistory = new ChatHistory();
                chatHistory.AddSystemMessage(systemMessage);
                chatHistory.AddUserMessage(userMessage);
                
                // Set a timeout for the request
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(120)); // Longer timeout for multi-step planning
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);
                
                // Execute the plan using Auto function calling behavior
                _logger.LogInformation("Executing multi-step plan for request: {Message}", userMessage);
                
                // Configure for automatic function invocation
                var settings = new PromptExecutionSettings { 
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() 
                };
                
                // Add API version for Azure OpenAI if needed
                if (settings.ExtensionData == null)
                {
                    settings.ExtensionData = new Dictionary<string, object>();
                }
                
                settings.ExtensionData["api-version"] = _configuration["AzureOpenAI:ApiVersion"] ?? "2025-01-01-preview";
                
                // Add user context information to the execution settings if available
                if (userContext != null)
                {
                    settings.ExtensionData["userContext"] = new Dictionary<string, string>
                    {
                        ["organizationId"] = userContext.OrganizationId ?? "",
                        ["userId"] = userContext.UserId ?? "",
                        ["userName"] = userContext.UserName ?? "",
                        ["email"] = userContext.Email ?? "",
                        ["role"] = userContext.Role ?? ""
                    };
                    
                    _logger.LogDebug("Added user context to execution settings: UserId={UserId}, OrganizationId={OrganizationId}",
                        userContext.UserId, userContext.OrganizationId);
                }
                
                try
                {
                    // Get the chat message content which will automatically call functions as needed
                    var chatMessageContent = await chatService.GetChatMessageContentAsync(
                        chatHistory, 
                        settings, 
                        kernel, 
                        linkedCts.Token);
                    
                    _logger.LogInformation("Multi-step plan execution completed successfully, chatMassageContent is : {ChatMessageContent}", chatMessageContent);
                    
                    // Return the final answer
                    return chatMessageContent.Content;
                }
                catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
                {
                    _logger.LogWarning("Multi-step request timed out after 120 seconds");
                    return "I'm sorry, but the multi-step request timed out. Please try again with a simpler request.";
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Multi-step plan execution was canceled by the caller");
                return "The request was canceled. Please try again.";
            }
            catch (Exception ex)
            {
                var errorBuilder = new StringBuilder();
                errorBuilder.AppendLine($"Error executing multi-step plan: {ex.GetType().Name}: {ex.Message}");
                
                // Log nested exceptions for better diagnostics
                Exception innerEx = ex.InnerException;
                int level = 1;
                while (innerEx != null)
                {
                    errorBuilder.AppendLine($"  Inner exception level {level}: {innerEx.GetType().Name}: {innerEx.Message}");
                    innerEx = innerEx.InnerException;
                    level++;
                }
                
                _logger.LogError("{ErrorDetails}", errorBuilder.ToString());
                return $"I encountered an error while processing your multi-step request: {ex.Message}";
            }
        }

        /// <summary>
        /// Process a document with Azure OpenAI to generate OKR suggestions
        /// </summary>
        /// <param name="systemMessage">Base system message without document content</param>
        /// <param name="documentContent">The extracted document content</param>
        /// <param name="userQuery">The user's query about the document</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>AI response with OKR suggestions based on document content</returns>
        public async Task<string> ProcessDocumentWithOpenAIAsync(
            string systemMessage,
            string documentContent,
            string userQuery,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(documentContent))
            {
                _logger.LogWarning("Empty document content provided for processing");
                return "No document content was available for analysis. Please check the document and try again.";
            }

            try
            {
                // Get or create the kernel
                var kernel = GetOrCreateKernel();
                
                // Get the chat completion service
                var chatService = kernel.GetRequiredService<IChatCompletionService>();
                
                // Estimate document tokens
                int estimatedTokens = _documentProcessingService.EstimateTokenCount(documentContent);
                _logger.LogInformation("Processing document with Azure OpenAI. Estimated tokens: {TokenCount}", estimatedTokens);
                
                // For small documents, use standard processing
                if (estimatedTokens <= 4000)
                {
                    _logger.LogInformation("Document fits within standard token limits. Processing directly.");
                    
                    // Enhance the system message with additional workflow guidance to ensure continuation
                    string enhancedSystemMessage = systemMessage + "\n\n" +
                        "CRITICAL WORKFLOW CONTINUATION INSTRUCTIONS:\n" +
                        "1. After successfully creating an OKR session with function calling, IMMEDIATELY proceed to STEP 2.\n" +
                        "2. Begin STEP 2 by saying 'Now that we've created the OKR session, let's create an objective for it.'\n" +
                        "3. Then propose a specific objective based on the document content.\n" +
                        "4. Remember to maintain the step-by-step approach throughout the entire workflow.\n" +
                        "5. After the user confirms each entity creation and the function is called, explicitly transition to the next step.\n" +
                        "6. NEVER end the conversation after just creating the OKR session - always continue to the next step.\n\n" +
                        "WORKFLOW STATE TRACKING:\n" +
                        "- After OKR session creation, store the session ID and use it when creating the objective.\n" +
                        "- After objective creation, store the objective ID and use it when creating the key result.\n" +
                        "- After key result creation, store the key result ID and use it when creating the task.\n";
                    
                    // Prepare system message with document content
                    string fullSystemMessage = await enhancedSystemMessage.AddDocumentContentToSystemMessageAsync(
                        documentContent, _documentProcessingService, 6000, _logger);
                    
                    // Create chat history
                    var chatHistory = new ChatHistory();
                    chatHistory.AddSystemMessage(fullSystemMessage);
                    
                    // Default user query if none provided
                    string defaultQuery = "Please analyze this document and extract potential objectives and key results. " +
                        "Organize them into a structured format that would be useful for an OKR planning session.";
                    
                    chatHistory.AddUserMessage(userQuery ?? defaultQuery);
                    
                    // Configure settings with explicit continuation enforcement
                    PromptExecutionSettings settings = new()
                    {
                        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                    };
                    
                    // Add API version for Azure OpenAI if needed
                    if (settings.ExtensionData == null)
                    {
                        settings.ExtensionData = new Dictionary<string, object>();
                    }
                    
                    settings.ExtensionData["api-version"] = _configuration["AzureOpenAI:ApiVersion"] ?? "2025-01-01-preview";
                    
                    // Add workflow continuation flag to ensure the model continues after OKR session creation
                    settings.ExtensionData["continue_workflow"] = true;
                    
                    // Set a timeout for the request
                    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(120)); // 2 minutes for document processing
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);
                    
                    try
                    {
                        _logger.LogInformation("Sending document analysis request to Azure OpenAI");
                        var response = await chatService.GetChatMessageContentsAsync(
                            chatHistory, settings, kernel, linkedCts.Token);
                        
                        _logger.LogInformation("Azure OpenAI document analysis response received");
                        
                        // Verify if the response indicates OKR session creation but doesn't mention objectives
                        string content = response[0].Content;
                        if (content.Contains("session created") || content.Contains("created the OKR session") || 
                            content.Contains("successfully created") || content.Contains("has been created"))
                        {
                            if (!content.Contains("objective") && !content.Contains("Objective") && 
                                !content.Contains("STEP 2") && !content.Contains("Next, let's"))
                            {
                                // Add workflow continuation prompt to the response
                                content += "\n\nNow that we've created the OKR session, let's create an objective for it. " +
                                    "Based on the document analysis, I suggest the following objective:\n\n" +
                                    "[Objective title and description will be derived from the document]\n\n" +
                                    "Would you like me to create this objective for the OKR session? Or would you like to make any adjustments?";
                                
                                _logger.LogInformation("Added workflow continuation prompt for objective creation");
                            }
                        }
                        
                        return content;
                    }
                    catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
                    {
                        _logger.LogWarning("Document processing request timed out after 120 seconds");
                        return "The document analysis took too long to complete. Please try with a smaller document or a more specific query.";
                    }
                }
                else
                {
                    // For large documents, use chunked processing
                    _logger.LogInformation("Large document detected ({Tokens} tokens). Using chunked processing.", estimatedTokens);
                    
                    // Add workflow continuation instructions to system message first
                    string enhancedSystemMessage = systemMessage + "\n\n" +
                        "CRITICAL WORKFLOW CONTINUATION INSTRUCTIONS:\n" +
                        "1. After successfully creating an OKR session with function calling, IMMEDIATELY proceed to STEP 2.\n" +
                        "2. Begin STEP 2 by saying 'Now that we've created the OKR session, let's create an objective for it.'\n" +
                        "3. Then propose a specific objective based on the document content.\n" +
                        "4. Remember to maintain the step-by-step approach throughout the entire workflow.\n" +
                        "5. After the user confirms each entity creation and the function is called, explicitly transition to the next step.\n" +
                        "6. NEVER end the conversation after just creating the OKR session - always continue to the next step.";
                    
                    // Process using chunked extension method
                    string response = await chatService.ProcessLargeDocumentAsync(
                        enhancedSystemMessage,
                        documentContent,
                        userQuery ?? "Analyze this document and extract potential objectives and key results",
                        _documentProcessingService,
                        _logger);
                        
                    // Check if the response stops after OKR session creation
                    if (response.Contains("session created") || response.Contains("created the OKR session") || 
                        response.Contains("successfully created") || response.Contains("has been created"))
                    {
                        if (!response.Contains("objective") && !response.Contains("Objective") && 
                            !response.Contains("STEP 2") && !response.Contains("Next, let's"))
                        {
                            // Add workflow continuation prompt to the response
                            response += "\n\nNow that we've created the OKR session, let's create an objective for it. " +
                                "Based on the document analysis, I suggest the following objective:\n\n" +
                                "[Objective title and description will be derived from the document]\n\n" +
                                "Would you like me to create this objective for the OKR session? Or would you like to make any adjustments?";
                            
                            _logger.LogInformation("Added workflow continuation prompt for objective creation to chunked document response");
                        }
                    }
                    
                    return response;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing document with Azure OpenAI");
                return $"An error occurred while analyzing the document: {ex.Message}";
            }
        }

        /// <summary>
        /// Generate AI-powered insights for a given OKR session
        /// </summary>
        public async Task<List<string>> GetSessionInsightsAsync(string sessionId, UserContext userContext, CancellationToken cancellationToken = default)
        {
            if (!Guid.TryParse(sessionId, out var sessionGuid))
                throw new ArgumentException("Invalid sessionId");

            // 1. Fetch objectives for the session
            var objectives = (await _objectiveRepository.GetBySessionIdAsync(sessionGuid)).ToList();

            // 2. Fetch key results for these objectives
            var objectiveIds = objectives.Select(o => o.Id).ToList();
            var keyResults = new List<KeyResult>();
            foreach (var objId in objectiveIds)
            {
                var krs = await _keyResultRepository.GetByObjectiveAsync(objId);
                keyResults.AddRange(krs);
            }

            // 3. Fetch tasks for these key results
            var keyResultIds = keyResults.Select(kr => kr.Id).ToList();
            var tasks = new List<KeyResultTask>();
            foreach (var krId in keyResultIds)
            {
                var tks = await _keyResultTaskRepository.GetByKeyResultAsync(krId);
                tasks.AddRange(tks);
            }

            // 4. Format the data for the LLM
            var sessionData = new
            {
                Objectives = objectives.Select(o => new { o.Id, o.Title, o.Description, o.Status, o.Progress }),
                KeyResults = keyResults.Select(kr => new { kr.Id, kr.Title, kr.Description, kr.Status, kr.Progress, kr.ObjectiveId }),
                Tasks = tasks.Select(t => new { t.Id, t.Title, t.Description, t.Status, t.Progress, t.KeyResultId })
            };

            string prompt = $"You are an OKR (Objectives and Key Results) assistant. Analyze the following session data and provide a concise list of actionable AI insights (as bullet points) for alignment, risks, and improvements.\nSession Data:\n{System.Text.Json.JsonSerializer.Serialize(sessionData, new System.Text.Json.JsonSerializerOptions { WriteIndented = true })}\n\nRespond with an array of strings, each string being a single insight.";

            // 5. Use the LLM to get insights
            var kernel = GetOrCreateKernel();
            var chatService = kernel.GetRequiredService<IChatCompletionService>();
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(prompt);
            var settings = new PromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() };
            if (settings.ExtensionData == null)
                settings.ExtensionData = new Dictionary<string, object>();
            settings.ExtensionData["api-version"] = _configuration["AzureOpenAI:ApiVersion"] ?? "2025-01-01-preview";

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);
            var response = await chatService.GetChatMessageContentsAsync(chatHistory, settings, kernel, linkedCts.Token);
            var content = response[0].Content;

            // Try to parse the response as a JSON array of strings

            var insights = System.Text.Json.JsonSerializer.Deserialize<List<string>>(content);
            // if (insights != null && insights.Count > 0)
            return insights;
        
        }

        /// <summary>
        /// Generate dynamic OKR session suggestions using AI
        /// </summary>
        public async Task<OkrSuggestionResponse> GetOkrSessionSuggestionsAsync(OkrSuggestionRequest request, CancellationToken cancellationToken = default)
        {   
            // Build a system prompt for the LLM
            var systemPrompt = $@"
You are an OKR (Objectives and Key Results) assistant. Based on the following user prompt and available teams, suggest a new OKR session.

User Prompt: {request.Prompt}
Available Teams (ID and Name):
{string.Join("\n", request.AvailableTeams?.Select(t => $"- {t.Id}: {t.Name}") ?? new List<string>())}

IMPORTANT: When suggesting teams, ONLY use the team IDs from the provided list above in the 'suggestedTeams' array. Do NOT invent or use team names. The array must contain only valid IDs as strings. If no team is relevant, return an empty array.

Respond with a JSON object with the following properties:
- title: string (session title)
- description: string (session description)
- suggestedTeams: array of team IDs (from available teams, if relevant)
- suggestedStartDate: ISO date string (suggested start date)
- suggestedEndDate: ISO date string (suggested end date)
- industryInsights: array of strings (optional, industry trends or best practices)
- alignmentTips: array of strings (optional, tips for alignment)
- keyFocusAreas: array of strings (optional, focus areas)
- potentialKeyResults: array of strings (optional, sample key results)
";

            var kernel = GetOrCreateKernel();
            var chatService = kernel.GetRequiredService<IChatCompletionService>();
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(systemPrompt);

            var userMessage = request.Prompt;
            chatHistory.AddUserMessage(userMessage);

            var settings = new PromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() };
            if (settings.ExtensionData == null)
                settings.ExtensionData = new Dictionary<string, object>();
            settings.ExtensionData["api-version"] = _configuration["AzureOpenAI:ApiVersion"] ?? "2025-01-01-preview";

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);
            var response = await chatService.GetChatMessageContentsAsync(chatHistory, settings, kernel, linkedCts.Token);
            var content = response[0].Content;

            // Parse the response as JSON
            OkrSuggestionResponse suggestion = null;
            try
            {
                // Try to extract JSON from a code block if present
                string json = content;
                var codeBlockStart = content.IndexOf("```json");
                var codeBlockEnd = content.IndexOf("```", codeBlockStart + 1);

                if (codeBlockStart != -1 && codeBlockEnd != -1)
                {
                    // Extract the JSON between the code block markers
                    json = content.Substring(codeBlockStart + 7, codeBlockEnd - (codeBlockStart + 7)).Trim();
                }

                suggestion = System.Text.Json.JsonSerializer.Deserialize<OkrSuggestionResponse>(json, new System.Text.Json.JsonSerializerOptions {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse OKR suggestion AI response: {Content}", content);
                throw new InvalidOperationException("Failed to parse OKR suggestion AI response.");
            }

            return suggestion;
        }
    }
}