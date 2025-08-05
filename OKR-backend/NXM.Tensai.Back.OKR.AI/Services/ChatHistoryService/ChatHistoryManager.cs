using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace NXM.Tensai.Back.OKR.AI.Services.ChatHistoryService
{
    /// <summary>
    /// Service responsible for managing separate chat histories for different conversations
    /// </summary>
    public class ChatHistoryManager
    {
        private readonly ILogger<ChatHistoryManager> _logger;
        private readonly ConcurrentDictionary<string, EnhancedChatHistory> _chatHistories = new();
        
        public ChatHistoryManager(ILogger<ChatHistoryManager> logger)
        {
            _logger = logger;
        }
        
        /// <summary>
        /// Get or create a chat history for a specific conversation ID
        /// </summary>
        /// <param name="conversationId">Unique identifier for the conversation</param>
        /// <returns>The chat history for this conversation</returns>
        public EnhancedChatHistory GetChatHistory(string conversationId)
        {
            if (string.IsNullOrEmpty(conversationId))
            {
                _logger.LogWarning("Empty conversation ID provided, creating temporary chat history");
                return new EnhancedChatHistory();
            }
            
            return _chatHistories.GetOrAdd(conversationId, _ => {
                _logger.LogInformation("Creating new chat history for conversation {ConversationId}", conversationId);
                return new EnhancedChatHistory();
            });
        }
        
        /// <summary>
        /// Reset a conversation's chat history
        /// </summary>
        /// <param name="conversationId">Conversation to reset</param>
        public void ResetChatHistory(string conversationId)
        {
            if (string.IsNullOrEmpty(conversationId))
            {
                _logger.LogWarning("Empty conversation ID provided for reset, ignoring");
                return;
            }
            
            if (_chatHistories.TryGetValue(conversationId, out var history))
            {
                history.Clear();
                _logger.LogInformation("Reset chat history for conversation {ConversationId}", conversationId);
            }
            else
            {
                _logger.LogWarning("Attempted to reset non-existent conversation {ConversationId}", conversationId);
            }
        }
        
        /// <summary>
        /// Remove a conversation's chat history completely
        /// </summary>
        /// <param name="conversationId">Conversation to remove</param>
        public void RemoveChatHistory(string conversationId)
        {
            if (_chatHistories.TryRemove(conversationId, out _))
            {
                _logger.LogInformation("Removed chat history for conversation {ConversationId}", conversationId);
            }
        }
        
        /// <summary>
        /// Get all conversations associated with a specific user ID
        /// </summary>
        /// <param name="userId">The user ID to filter conversations by</param>
        /// <returns>Dictionary of conversation IDs and their chat histories for the specified user</returns>
        public IReadOnlyDictionary<string, EnhancedChatHistory> GetAllConversationsByUserId(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Empty user ID provided for conversation lookup");
                return new Dictionary<string, EnhancedChatHistory>();
            }
            
            _logger.LogDebug("Looking for conversations with userId: {UserId}, current conversation count: {Count}", 
                userId, _chatHistories.Count);
            
            // Filter the chat histories to only include conversations where this user participated
            var userConversations = new Dictionary<string, EnhancedChatHistory>();
            
            foreach (var kvp in _chatHistories)
            {
                bool userFound = false;
                foreach (var msg in kvp.Value.Messages)
                {
                    if (msg.Role == Microsoft.SemanticKernel.ChatCompletion.AuthorRole.User)
                    {
                        if (msg.Metadata.TryGetValue("UserId", out var messageUserId))
                        {
                            // Use case-insensitive string comparison
                            string userIdStr = messageUserId?.ToString();
                            if (!string.IsNullOrEmpty(userIdStr) && 
                                string.Equals(userIdStr, userId, StringComparison.OrdinalIgnoreCase))
                            {
                                userFound = true;
                                _logger.LogDebug("Found conversation {ConversationId} for user {UserId}", kvp.Key, userId);
                                break;
                            }
                        }
                    }
                }
                
                if (userFound)
                {
                    userConversations.Add(kvp.Key, kvp.Value);
                }
            }
            
            _logger.LogInformation("Found {Count} conversations for user {UserId}", userConversations.Count, userId);
            return userConversations;
        }

        /// <summary>
        /// Get all conversations in the system (for diagnostics)
        /// </summary>
        public IReadOnlyDictionary<string, EnhancedChatHistory> GetAllConversations()
        {
            _logger.LogInformation("Retrieving all conversations, current count: {Count}", _chatHistories.Count);
            return _chatHistories;
        }
    }
}