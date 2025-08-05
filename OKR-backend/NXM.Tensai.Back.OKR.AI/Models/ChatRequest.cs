using System;

namespace NXM.Tensai.Back.OKR.AI.Models
{
    /// <summary>
    /// Model for chat requests from the client
    /// </summary>
    public class ChatRequest
    {
        /// <summary>
        /// The message from the user
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Optional context about the user
        /// </summary>
        public UserContext UserContext { get; set; }
        
        /// <summary>
        /// Unique identifier for the conversation
        /// </summary>
        public string ConversationId { get; set; }

        /// <summary>
        /// The selected LLM provider: "cohere", "azureopenai", or "openai"
        /// </summary>
        public string LLMProvider { get; set; }

    }
}