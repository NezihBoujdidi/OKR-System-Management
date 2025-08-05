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
    /// A custom implementation of IChatCompletionService for Cohere API using direct HTTP calls
    /// </summary>
    public class CohereChatCompletionService : IChatCompletionService
    {
        private readonly string _apiKey;
        private readonly string _model;
        private readonly HttpClient _httpClient;
        private readonly ILogger<CohereChatCompletionService> _logger;
        private const string CohereApiEndpoint = "https://api.cohere.ai/v1/chat";

        /// <summary>
        /// Implements the required Attributes property from IAIService interface
        /// </summary>
        public IReadOnlyDictionary<string, object?> Attributes => new Dictionary<string, object?>
        {
            { "ModelId", _model },
            { "DeploymentOrModelId", _model },
            { "ApiKey", "[REDACTED]" },
            { "Endpoint", CohereApiEndpoint }
        };

        public CohereChatCompletionService(string apiKey, string model, ILogger<CohereChatCompletionService> logger)
        {
            _apiKey = apiKey;
            _model = model;
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
            
            // Handle system message if present (as preamble in Cohere)
            string preamble = string.Empty;
            foreach (var message in chatHistory)
            {
                if (message.Role == AuthorRole.System)
                {
                    preamble = message.Content;
                    continue;
                }
                
                // Convert Semantic Kernel message to Cohere message
                var role = message.Role == AuthorRole.User ? "USER" : "CHATBOT";
                messages.Add(new ChatMessage
                {
                    Role = role,
                    Message = message.Content
                });
            }

            try
            {
                // Create chat request
                var chatRequest = new ChatRequest
                {
                    Message = messages.LastOrDefault(m => m.Role == "USER")?.Message ?? string.Empty,
                    Model = _model,
                    ChatHistory = messages.Count > 1 ? messages.Take(messages.Count - 1).ToList() : null,
                    Preamble = !string.IsNullOrEmpty(preamble) ? preamble : null
                };

                // Apply execution settings if provided
                if (executionSettings?.ExtensionData != null)
                {
                    if (executionSettings.ExtensionData.TryGetValue("temperature", out var temp))
                        chatRequest.Temperature = Convert.ToSingle(temp);
                    
                    if (executionSettings.ExtensionData.TryGetValue("max_tokens", out var maxTokens))
                        chatRequest.MaxTokens = Convert.ToInt32(maxTokens);
                }

                // Log request details
                var requestJson = JsonSerializer.Serialize(chatRequest, new JsonSerializerOptions { WriteIndented = true });
                _logger.LogInformation("Sending request to Cohere API:\n{RequestJson}", requestJson);
                
                var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
                
                try
                {
                    var response = await _httpClient.PostAsync(CohereApiEndpoint, content, cancellationToken);
                    
                    // Log response status code
                    _logger.LogInformation("Cohere API response status: {StatusCode}", response.StatusCode);
                    
                    // Get response content even if it's an error
                    var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError("Cohere API returned error response: {ResponseContent}", responseJson);
                        response.EnsureSuccessStatusCode(); // This will throw with the status code
                    }
                    
                    var chatResponse = JsonSerializer.Deserialize<ChatResponse>(responseJson);

                    // Return the response
                    var responseContent = new ChatMessageContent(
                        AuthorRole.Assistant,
                        chatResponse?.Text ?? string.Empty);
                    
                    return new List<ChatMessageContent> { responseContent };
                }
                catch (HttpRequestException httpEx)
                {
                    _logger.LogError(httpEx, "HTTP error calling Cohere API");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Cohere service: {Message}", ex.Message);
                throw new InvalidOperationException($"Error calling Cohere API: {ex.Message}", ex);
            }
        }

        public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
            ChatHistory chatHistory, 
            PromptExecutionSettings? executionSettings = null, 
            Kernel? kernel = null, 
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // Cohere doesn't provide a direct way to stream responses in the .NET SDK
            // We'll simulate streaming by returning the full response
            var response = await GetChatMessageContentsAsync(chatHistory, executionSettings, kernel, cancellationToken);
            
            yield return new StreamingChatMessageContent(
                AuthorRole.Assistant,
                response[0].Content);
        }

        // Internal classes for serialization
        private class ChatMessage
        {
            [JsonPropertyName("role")]
            public string Role { get; set; }

            [JsonPropertyName("message")]
            public string Message { get; set; }
        }

        private class ChatRequest
        {
            [JsonPropertyName("message")]
            public string Message { get; set; }

            [JsonPropertyName("model")]
            public string Model { get; set; }

            [JsonPropertyName("chat_history")]
            public IEnumerable<ChatMessage> ChatHistory { get; set; }

            [JsonPropertyName("preamble")]
            public string Preamble { get; set; }

            [JsonPropertyName("temperature")]
            public float? Temperature { get; set; }

            [JsonPropertyName("max_tokens")]
            public int? MaxTokens { get; set; }
        }

        private class ChatResponse
        {
            [JsonPropertyName("text")]
            public string Text { get; set; }

            [JsonPropertyName("generation_id")]
            public string GenerationId { get; set; }

            [JsonPropertyName("finish_reason")]
            public string FinishReason { get; set; }
        }
    }
}