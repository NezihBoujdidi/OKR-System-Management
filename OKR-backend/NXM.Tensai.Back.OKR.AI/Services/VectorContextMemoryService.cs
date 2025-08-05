using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using NXM.Tensai.Back.OKR.AI.Models;
using System.Linq;

#pragma warning disable CS1591, CS0618, CS8602, CS8604
namespace NXM.Tensai.Back.OKR.AI.Services
{
    /// <summary>
    /// Service that provides vector-based context memory using Qdrant
    /// </summary>
    public class VectorContextMemoryService
    {
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        private readonly ISemanticTextMemory _memory;
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        private readonly ILogger<VectorContextMemoryService> _logger;
        private readonly IConfiguration _configuration;
        private const string MemoryCollectionName = "conversation-history";

        public VectorContextMemoryService(IConfiguration configuration, ILogger<VectorContextMemoryService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            // Create the memory store with Qdrant
            var endpoint = _configuration["VectorStore:Qdrant:Endpoint"] ?? "http://localhost:6333";
            
            try
            {
                // Create the embedding generator with direct parameters
#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                var embeddingGenerator = new AzureOpenAITextEmbeddingGenerationService(
                    deploymentName:_configuration["AzureOpenAI:EmbeddingDeployment"] ?? "text-embedding-ada-002",
                    endpoint: _configuration["AzureOpenAI:EmbeddingEndpoint"],
                    apiKey: _configuration["AzureOpenAI:EmbeddingKey"]
                );
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

                // Create the memory store
#pragma warning disable SKEXP0020 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                var vectorStore = new QdrantMemoryStore(endpoint, 1536); // 1536 is the dimension for text-embedding-ada-002
#pragma warning restore SKEXP0020 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

                // Initialize the memory
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                _memory = new SemanticTextMemory(vectorStore, embeddingGenerator);
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

                _logger.LogInformation("Vector context memory service initialized successfully with Qdrant at {Endpoint}", endpoint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize vector context memory service");
                throw;
            }
        }

        /// <summary>
        /// Saves conversation context to the vector store
        /// </summary>
        public async Task SaveConversationContextAsync(string conversationId, string content, string userId = null)
        {
            try
            {
                var id = $"{conversationId}-{DateTime.UtcNow.Ticks}";
                
                // Create metadata as a single string in JSON format
                string metadata = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    { "conversationId", conversationId },
                    { "userId", userId ?? "unknown" },
                    { "content", content } // Also store the content in metadata
                });

                _logger.LogInformation("Saving conversation context for conversation {ConversationId}", conversationId);
                
                await _memory.SaveInformationAsync(
                    collection: MemoryCollectionName,
                    id: id,
                    text: content,
                    description: $"Conversation context for {conversationId}",
                    additionalMetadata: metadata);
                
                _logger.LogInformation("Successfully saved context with ID {Id}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving conversation context for conversation {ConversationId}", conversationId);
            }
        }

        /// <summary>
        /// Retrieves relevant conversation context based on user query
        /// </summary>
        public async Task<string> GetRelevantContextAsync(string userQuery, string conversationId, int limit = 5, double minRelevanceScore = 0.7)
        {
            try
            {
                _logger.LogInformation("Getting relevant context for query in conversation {ConversationId}", conversationId);

                // Search for memories with similar context to the user query
                // Use ToListAsync to handle IAsyncEnumerable correctly
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

                var searchResults = new List<MemoryQueryResult>();
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.


                // Enumerate the async sequence properly
                await foreach (var result in _memory.SearchAsync(
                    collection: MemoryCollectionName,
                    query: userQuery,
                    limit: limit,
                    minRelevanceScore: minRelevanceScore,
                    withEmbeddings: false))
                {
                    searchResults.Add(result);
                }
                _logger.LogInformation("Found {searchResults} results for query: {Query}", searchResults, userQuery);

                // Filter results by conversationId if specified
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

                var filteredResults = new List<MemoryQueryResult>();
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

                foreach (var result in searchResults)
                {
                    // Try to parse the metadata JSON
                    try
                    {
                        var metadataString = result.Metadata.AdditionalMetadata;
                        _logger.LogInformation("metadataString: {MetadataString}", metadataString);
                        var metadataDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(metadataString);
                        _logger.LogInformation("metadataDict: {MetadataDict}", metadataDict);
                        if (metadataDict != null && 
                            metadataDict.TryGetValue("conversationId", out var resultConversationId) && 
                            resultConversationId == conversationId)
                        {
                            filteredResults.Add(result);
                        }
                    }
                    catch
                    {
                        // If we can't parse the metadata, just check if it contains the conversation ID as string
                        _logger.LogWarning("Failed to parse metadata for result: {result}", result);
                        string metadataStr = result.Metadata.ToString();
                        if (metadataStr != null && metadataStr.Contains(conversationId))
                        {
                            filteredResults.Add(result);
                        }
                    }
                }

                // Combine the relevant memories into a single context string
                var contextBuilder = new System.Text.StringBuilder();
                foreach (var result in filteredResults)
                {
                    // Access the text content from the saved metadata if possible, otherwise use key info from metadata
                    try
                    {
                        var metadataString = result.Metadata.AdditionalMetadata;
                        var metadataDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(metadataString);
                        if (metadataDict != null && metadataDict.TryGetValue("content", out var content))
                        {
                            contextBuilder.AppendLine(content);
                        }
                        else
                        {
                            // If we can't get the content directly, use the metadata string
                            contextBuilder.AppendLine(metadataString);
                        }
                    }
                    catch
                    {
                        // Fallback to using the metadata string directly
                        contextBuilder.AppendLine(result.Metadata.ToString());
                    }
                }

                var context = contextBuilder.ToString().Trim();
                
                _logger.LogInformation("Retrieved {Count} relevant context items for conversation {ConversationId}", 
                    filteredResults.Count, conversationId);
                
                return context;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving relevant context for conversation {ConversationId}", conversationId);
                return string.Empty;
            }
        }

        /// <summary>
        /// Enhances a system message with relevant context from the vector store
        /// </summary>
        public string EnhanceSystemMessageWithContext(string systemMessage, string relevantContext)
        {
            if (string.IsNullOrEmpty(relevantContext))
            {
                return systemMessage;
            }

            return $"{systemMessage}\n\nRelevant conversation history:\n{relevantContext}";
        }
    }
}
#pragma warning restore CS1591, CS0618, CS8602, CS8604