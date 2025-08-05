using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NXM.Tensai.Back.OKR.AI.Services
{
    /// <summary>
    /// A custom implementation of IChatCompletionService for DeepSeek API using direct HTTP calls
    /// </summary>
    public class DeepSeekChatCompletionService : IChatCompletionService
    {
        private readonly string _apiKey;
        private readonly string _model;
        private readonly HttpClient _httpClient;
        private readonly bool _disableDeepThink;
        private readonly string _endpoint;
        private readonly ILogger<DeepSeekChatCompletionService> _logger;

        /// <summary>
        /// Implements the required Attributes property from IAIService interface
        /// </summary>
        public IReadOnlyDictionary<string, object?> Attributes => new Dictionary<string, object?>
        {
            { "ModelId", _model },
            { "DeploymentOrModelId", _model },
            { "ApiKey", "[REDACTED]" },
            { "Endpoint", _endpoint }
        };

        public DeepSeekChatCompletionService(string apiKey, string model, string endpoint, bool disableDeepThink, ILogger<DeepSeekChatCompletionService> logger)
        {
            _apiKey = apiKey;
            _model = model;
            _endpoint = endpoint;
            _disableDeepThink = disableDeepThink;
            _logger = logger;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public async Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
            ChatHistory chatHistory, 
            PromptExecutionSettings? executionSettings = null, 
            Kernel? kernel = null, 
            CancellationToken cancellationToken = default)
        {
            // Extract messages from chat history
            var messages = new List<ChatMessage>();
            
            foreach (var message in chatHistory)
            {
                var role = ConvertAuthorRoleToDeepSeekRole(message.Role);
                messages.Add(new ChatMessage
                {
                    Role = role,
                    Content = message.Content
                });
            }

            try
            {
                // Create chat request
                var chatRequest = new ChatRequest
                {
                    Model = _model,
                    Messages = messages,
                    DeepThink = !_disableDeepThink // Disable DeepThink if configured to do so
                };

                // Apply execution settings if provided
                if (executionSettings?.ExtensionData != null)
                {
                    if (executionSettings.ExtensionData.TryGetValue("temperature", out var temp))
                        chatRequest.Temperature = Convert.ToSingle(temp);
                    
                    if (executionSettings.ExtensionData.TryGetValue("max_tokens", out var maxTokens))
                        chatRequest.MaxTokens = Convert.ToInt32(maxTokens);
                    
                    if (executionSettings.FunctionChoiceBehavior != null)
                        chatRequest.ToolChoice = "auto"; // Enable function calling
                }

                // Add tools/functions if they are available (simplified approach)
                if (kernel?.Plugins != null && executionSettings?.FunctionChoiceBehavior != null)
                {
                    var tools = new List<Tool>();
                    
                    // This is a simplified version that doesn't attempt to iterate through plugins
                    // If function calling is required, implement this properly according to the Semantic Kernel version
                    
                    if (tools.Any())
                    {
                        chatRequest.Tools = tools;
                    }
                }

                // Call DeepSeek API
                var requestJson = JsonSerializer.Serialize(chatRequest);
                
                // Log the request being sent (with redacted API key)
                _logger.LogDebug("Sending request to DeepSeek API endpoint {Endpoint} for model {Model}: {Request}", 
                    _endpoint, _model, requestJson);
                
                var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync(_endpoint, content, cancellationToken);
                
                // If the response is not successful, capture the error details
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("DeepSeek API returned error status {StatusCode}: {ErrorContent}", 
                        response.StatusCode, errorContent);
                        
                    throw new HttpRequestException(
                        $"DeepSeek API error {(int)response.StatusCode} ({response.StatusCode}): {errorContent}");
                }
                
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                
                // Log the successful response
                _logger.LogDebug("Received successful response from DeepSeek API: {Response}", 
                    responseJson.Length > 500 ? responseJson.Substring(0, 500) + "..." : responseJson);
                    
                var chatResponse = JsonSerializer.Deserialize<ChatResponse>(responseJson);

                if (chatResponse?.Choices == null || chatResponse.Choices.Count == 0)
                {
                    throw new InvalidOperationException("Empty or null response received from DeepSeek API");
                }

                var result = chatResponse.Choices[0].Message;
                
                // Create a response with the content
                var responseContent = new ChatMessageContent(
                    ConvertDeepSeekRoleToAuthorRole(result.Role),
                    result.Content ?? string.Empty);
                
                // This approach works in all versions of Semantic Kernel
                if (result.ToolCalls != null && result.ToolCalls.Count > 0)
                {
                    // Convert tool calls to a simple JSON string to preserve function calling information
                    var toolCallsJson = JsonSerializer.Serialize(result.ToolCalls);
                    
                    // Create a JSON string with metadata that can be parsed later
                    string metadataString = $"{{\"toolCalls\": {toolCallsJson}}}";
                    
                    // Create new response content with the metadata as a string
                    responseContent = new ChatMessageContent(
                        ConvertDeepSeekRoleToAuthorRole(result.Role),
                        result.Content ?? string.Empty,
                        metadataString);
                }
                
                return new List<ChatMessageContent> { responseContent };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error when calling DeepSeek API: {ErrorMessage}", ex.Message);
                throw new InvalidOperationException($"Error calling DeepSeek API: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON serialization/deserialization error with DeepSeek API: {ErrorMessage}", ex.Message);
                throw new InvalidOperationException($"Error processing DeepSeek API response: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in DeepSeek API call: {ErrorMessage}", ex.Message);
                throw new InvalidOperationException($"Error calling DeepSeek API: {ex.Message}", ex);
            }
        }

        public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
            ChatHistory chatHistory, 
            PromptExecutionSettings? executionSettings = null, 
            Kernel? kernel = null, 
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // DeepSeek supports streaming, but for simplicity, we'll just implement it using the non-streaming API for now
            var response = await GetChatMessageContentsAsync(chatHistory, executionSettings, kernel, cancellationToken);
            
            yield return new StreamingChatMessageContent(
                AuthorRole.Assistant,
                response[0].Content);
        }

        private string ConvertAuthorRoleToDeepSeekRole(AuthorRole role)
        {
            if (role == AuthorRole.System) return "system";
            if (role == AuthorRole.User) return "user";
            if (role == AuthorRole.Assistant) return "assistant";
            if (role == AuthorRole.Tool) return "tool";
            return "user"; // Default to user for unknown roles
        }

        private AuthorRole ConvertDeepSeekRoleToAuthorRole(string role)
        {
            if (role == "system") return AuthorRole.System;
            if (role == "user") return AuthorRole.User;
            if (role == "assistant") return AuthorRole.Assistant;
            if (role == "tool") return AuthorRole.Tool;
            return AuthorRole.User; // Default to user for unknown roles
        }

        // Internal classes for serialization
        private class ChatMessage
        {
            [JsonPropertyName("role")]
            public string Role { get; set; }

            [JsonPropertyName("content")]
            public string Content { get; set; }
        }

        private class ChatRequest
        {
            [JsonPropertyName("model")]
            public string Model { get; set; }

            [JsonPropertyName("messages")]
            public List<ChatMessage> Messages { get; set; }

            [JsonPropertyName("temperature")]
            public float? Temperature { get; set; }

            [JsonPropertyName("max_tokens")]
            public int? MaxTokens { get; set; }

            [JsonPropertyName("deep_think")]
            public bool? DeepThink { get; set; }

            [JsonPropertyName("tools")]
            public List<Tool> Tools { get; set; }

            [JsonPropertyName("tool_choice")]
            public string ToolChoice { get; set; }
        }

        private class Parameters
        {
            [JsonPropertyName("type")]
            public string Type { get; set; }

            [JsonPropertyName("properties")]
            public Dictionary<string, ParameterProperty> Properties { get; set; }

            [JsonPropertyName("required")]
            public List<string> Required { get; set; }
        }

        private class ParameterProperty
        {
            [JsonPropertyName("type")]
            public string Type { get; set; }

            [JsonPropertyName("description")]
            public string Description { get; set; }
        }

        private class Tool
        {
            [JsonPropertyName("type")]
            public string Type { get; set; }

            [JsonPropertyName("function")]
            public FunctionDefinition Function { get; set; }
        }

        private class FunctionDefinition
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("description")]
            public string Description { get; set; }

            [JsonPropertyName("parameters")]
            public Parameters Parameters { get; set; }
        }

        private class ChatResponse
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("object")]
            public string Object { get; set; }

            [JsonPropertyName("created")]
            public long Created { get; set; }

            [JsonPropertyName("choices")]
            public List<Choice> Choices { get; set; }
        }

        private class Choice
        {
            [JsonPropertyName("index")]
            public int Index { get; set; }

            [JsonPropertyName("message")]
            public AssistantMessage Message { get; set; }

            [JsonPropertyName("finish_reason")]
            public string FinishReason { get; set; }
        }

        private class AssistantMessage
        {
            [JsonPropertyName("role")]
            public string Role { get; set; }

            [JsonPropertyName("content")]
            public string Content { get; set; }

            [JsonPropertyName("tool_calls")]
            public List<ToolCallDefinition> ToolCalls { get; set; }
        }

        private class ToolCallDefinition
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("type")]
            public string Type { get; set; }

            [JsonPropertyName("function")]
            public FunctionCallDefinition Function { get; set; }
        }

        private class FunctionCallDefinition
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("arguments")]
            public string Arguments { get; set; }
        }
    }
}