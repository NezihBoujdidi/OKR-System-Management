using System.Text;
using System.Text.Json;
using Microsoft.SemanticKernel.ChatCompletion;

namespace NXM.Tensai.Back.OKR.AI.Models
{
    /// <summary>
    /// Represents an enhanced chat message with function execution context
    /// </summary>
    public class EnhancedChatMessage
    {
        /// <summary>
        /// Role of the message sender (system, user, assistant)
        /// </summary>
        public AuthorRole Role { get; set; }

        /// <summary>
        /// The actual message content
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Name of the function that was executed (if applicable)
        /// </summary>
        public string FunctionName { get; set; }

        /// <summary>
        /// JSON serialized output of the function (if applicable)
        /// </summary>
        public string FunctionOutput { get; set; }

        /// <summary>
        /// Entity type this message relates to (e.g., "Team", "OKR", "Task")
        /// </summary>
        public string EntityType { get; set; }

        /// <summary>
        /// ID of the entity this message relates to
        /// </summary>
        public string EntityId { get; set; }

        /// <summary>
        /// Operation performed (e.g., "Create", "Update", "Delete")
        /// </summary>
        public string Operation { get; set; }

        /// <summary>
        /// Additional metadata as key-value pairs
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new();

        /// <summary>
        /// Timestamp of when the message was created
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Create a chat message from a function execution
        /// </summary>
        public static EnhancedChatMessage FromFunctionExecution<T>(
            string functionName, 
            T functionOutput,
            string entityType = null,
            string entityId = null,
            string operation = null)
        {
            return new EnhancedChatMessage
            {
                Role = AuthorRole.Assistant,
                FunctionName = functionName,
                FunctionOutput = JsonSerializer.Serialize(functionOutput),
                EntityType = entityType,
                EntityId = entityId,
                Operation = operation
            };
        }

        /// <summary>
        /// Create a chat message from user input
        /// </summary>
        public static EnhancedChatMessage FromUser(string content)
        {
            return new EnhancedChatMessage
            {
                Role = AuthorRole.User,
                Content = content
            };
        }

        /// <summary>
        /// Create a chat message from system
        /// </summary>
        public static EnhancedChatMessage FromSystem(string content)
        {
            return new EnhancedChatMessage
            {
                Role = AuthorRole.System,
                Content = content
            };
        }

        /// <summary>
        /// Create a chat message from assistant response
        /// </summary>
        public static EnhancedChatMessage FromAssistant(string content)
        {
            return new EnhancedChatMessage
            {
                Role = AuthorRole.Assistant,
                Content = content
            };
        }
    }
}