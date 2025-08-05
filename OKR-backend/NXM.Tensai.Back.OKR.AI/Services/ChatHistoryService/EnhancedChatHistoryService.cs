using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.SemanticKernel.ChatCompletion;
using NXM.Tensai.Back.OKR.AI.Models;

namespace NXM.Tensai.Back.OKR.AI.Services.ChatHistoryService
{
    /// <summary>
    /// Service to manage enhanced chat history with context tracking
    /// </summary>
    public class EnhancedChatHistory
    {
        private readonly List<EnhancedChatMessage> _messages = new();
        private readonly Dictionary<string, List<EntityReference>> _entityReferences = new();

        public IReadOnlyList<EnhancedChatMessage> Messages => _messages.AsReadOnly();

        private class EntityReference
        {
            public string EntityId { get; set; }
            public string EntityType { get; set; }
            public DateTime Timestamp { get; set; }
            public string Operation { get; set; }
            public int MessageIndex { get; set; }
            public string Name { get; set; }
        }

        /// <summary>
        /// Add a message to the chat history
        /// </summary>
        public void AddMessage(EnhancedChatMessage message)
        {
            // Skip AI responses that contain code blocks
            if (message.Role == AuthorRole.Assistant && 
                message.Content?.Contains("```") == true)
            {
                return;
            }

            int messageIndex = _messages.Count;
            _messages.Add(message);

            // Track entity references if available
            if (!string.IsNullOrEmpty(message.EntityType) && !string.IsNullOrEmpty(message.EntityId))
            {
                if (!_entityReferences.ContainsKey(message.EntityType))
                {
                    _entityReferences[message.EntityType] = new List<EntityReference>();
                }

                // Extract name from function output if available
                string name = null;
                if (!string.IsNullOrEmpty(message.FunctionOutput))
                {
                    try
                    {
                        var outputObj = JsonSerializer.Deserialize<JsonDocument>(message.FunctionOutput);
                        
                        // Try common property names for entity name
                        foreach (var propertyName in new[] { "Name", "TeamName", "Title", "ObjectiveName" })
                        {
                            if (outputObj.RootElement.TryGetProperty(propertyName, out var nameElement))
                            {
                                name = nameElement.GetString();
                                if (!string.IsNullOrEmpty(name)) break;
                            }
                        }
                    }
                    catch { /* Ignore parsing errors */ }
                }

                // If we couldn't extract name from output, check metadata
                if (string.IsNullOrEmpty(name) && message.Metadata.TryGetValue("EntityName", out var entityName))
                {
                    name = entityName;
                }

                _entityReferences[message.EntityType].Add(new EntityReference
                {
                    EntityId = message.EntityId,
                    EntityType = message.EntityType,
                    Timestamp = message.Timestamp,
                    Operation = message.Operation,
                    MessageIndex = messageIndex,
                    Name = name
                });
            }
        }

        /// <summary>
        /// Get the most recent entity ID of a specific type
        /// </summary>
        public string GetMostRecentEntityId(string entityType)
        {
            if (_entityReferences.TryGetValue(entityType, out var references))
            {
                var mostRecent = references.OrderByDescending(r => r.Timestamp).FirstOrDefault();
                if (mostRecent != null)
                {
                    return mostRecent.EntityId;
                }
            }
            return null;
        }

        /// <summary>
        /// Get the entity ID based on contextual reference
        /// </summary>
        private string GetContextualEntityId(string entityType, string userMessage)
        {
            if (!_entityReferences.TryGetValue(entityType, out var references) || !references.Any())
                return null;

            var orderedRefs = references.OrderBy(r => r.Timestamp).ToList();
            var messageNormalized = userMessage.ToLowerInvariant();

            // Handle various temporal references
            if (messageNormalized.Contains("initial") || 
                messageNormalized.Contains("first") || 
                messageNormalized.Contains("original"))
            {
                return orderedRefs.FirstOrDefault()?.EntityId;
            }
            
            if (messageNormalized.Contains("last") || 
                messageNormalized.Contains("latest") ||
                messageNormalized.Contains("current"))
            {
                return orderedRefs.LastOrDefault()?.EntityId;
            }
            
            if (messageNormalized.Contains("previous") || 
                messageNormalized.Contains("before"))
            {
                var lastRef = orderedRefs.LastOrDefault();
                var index = orderedRefs.IndexOf(lastRef);
                return index > 0 ? orderedRefs[index - 1].EntityId : null;
            }

            // Try to match by name if mentioned
            foreach (var reference in orderedRefs.OrderByDescending(r => r.Timestamp))
            {
                if (!string.IsNullOrEmpty(reference.Name) && 
                    messageNormalized.Contains(reference.Name.ToLowerInvariant()))
                {
                    return reference.EntityId;
                }
            }

            // Default to most recent unless specifically asking about an older item
            if (!messageNormalized.Contains("old"))
            {
                return orderedRefs.LastOrDefault()?.EntityId;
            }

            return null;
        }

        /// <summary>
        /// Convert history to chat messages compatible with Semantic Kernel
        /// </summary>
        public ChatHistory ToSemanticKernelHistory()
        {
            var history = new ChatHistory();
            var lastFunctionResult = new Dictionary<string, EnhancedChatMessage>();

            foreach (var message in _messages.OrderBy(m => m.Timestamp))
            {
                var content = new StringBuilder();
                
                // Add the main message content if it exists
                if (!string.IsNullOrEmpty(message.Content))
                {
                    content.AppendLine(message.Content);
                }

                // For function executions, enhance the context
                if (!string.IsNullOrEmpty(message.FunctionName))
                {
                    lastFunctionResult[message.FunctionName] = message;

                    // Add function execution context to the history
                    if (message.Role == AuthorRole.Assistant)
                    {
                        var functionMessage = new StringBuilder();
                        // Add context about the entity and operation
                        if (!string.IsNullOrEmpty(message.EntityType) && !string.IsNullOrEmpty(message.EntityId))
                        {
                            functionMessage.AppendLine($"Context: {message.Operation} operation on {message.EntityType} (ID: {message.EntityId})");
                        }

                        if (!string.IsNullOrEmpty(message.FunctionOutput))
                        {
                            functionMessage.AppendLine($"Result: {message.FunctionOutput}");
                        }
                        history.AddMessage(message.Role, functionMessage.ToString().Trim());
                    }
                    continue;
                }

                // For user messages, try to add relevant context about referenced entities
                if (message.Role == AuthorRole.User)
                {
                    foreach (var entityType in _entityReferences.Keys)
                    {
                        var contextualId = GetContextualEntityId(entityType, message.Content);
                        if (!string.IsNullOrEmpty(contextualId))
                        {
                            content.AppendLine($"\nContext: Referenced {entityType} ID: {contextualId}");
                        }
                    }
                }
                // Add metadata as context (except AuthorName)
                if (message.Metadata.Any())
                {
                    foreach (var meta in message.Metadata.Where(m => m.Key != "AuthorName"))
                    {
                        content.AppendLine($"{meta.Key}: {meta.Value}");
                    }
                }

                var finalContent = content.ToString().Trim();
                if (!string.IsNullOrEmpty(finalContent))
                {
                    history.AddMessage(message.Role, finalContent);
                }
            }

            return history;
        }

        /// <summary>
        /// Clear all messages from history
        /// </summary>
        public void Clear()
        {
            _messages.Clear();
            _entityReferences.Clear();
        }

        /// <summary>
        /// Get all messages related to a specific entity
        /// </summary>
        public IEnumerable<EnhancedChatMessage> GetEntityHistory(string entityType, string entityId)
        {
            return _messages.Where(m => 
                m.EntityType == entityType && 
                m.EntityId == entityId)
                .OrderBy(m => m.Timestamp);
        }

        /// <summary>
        /// Get the most recent function execution result of a specific type
        /// </summary>
        public T GetLastFunctionResult<T>(string functionName)
        {
            var lastMessage = _messages
                .Where(m => m.FunctionName == functionName && !string.IsNullOrEmpty(m.FunctionOutput))
                .OrderByDescending(m => m.Timestamp)
                .FirstOrDefault();

            if (lastMessage != null)
            {
                try
                {
                    return JsonSerializer.Deserialize<T>(lastMessage.FunctionOutput);
                }
                catch
                {
                    return default;
                }
            }

            return default;
        }

        /// <summary>
        /// Get messages with function outputs parsed as JSON objects for API responses
        /// </summary>
        /// <returns>List of messages with parsed function outputs</returns>
        public IEnumerable<object> GetMessagesWithParsedFunctionOutputs()
        {
            // Create a new list to avoid modifying the original messages
            var result = new List<object>();
            
            foreach (var message in _messages)
            {
                // Parse function output if present
                object parsedFunctionOutput = message.FunctionOutput;
                if (!string.IsNullOrEmpty(message.FunctionOutput))
                {
                    try
                    {
                        // Parse JSON string to object that can be serialized
                        parsedFunctionOutput = JsonSerializer.Deserialize<object>(message.FunctionOutput);
                    }
                    catch
                    {
                        // Keep as string if parsing fails
                    }
                }
                
                // Create an anonymous object with all the properties
                // Format the role to match frontend expectations: { label: "roleName" }
                var processedMessage = new 
                {
                    role = new { label = message.Role.ToString().ToLowerInvariant() },
                    content = message.Content,
                    functionName = message.FunctionName,
                    functionOutput = parsedFunctionOutput,
                    entityType = message.EntityType,
                    entityId = message.EntityId,
                    operation = message.Operation,
                    metadata = message.Metadata,
                    timestamp = message.Timestamp
                };
                
                result.Add(processedMessage);
            }
            
            return result;
        }
    }
}
