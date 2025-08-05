using System;

namespace NXM.Tensai.Back.OKR.AI.Models
{
    /// <summary>
    /// Request to set a system message for a specific conversation
    /// </summary>
    public class SystemMessageWithConversationRequest
    {
        /// <summary>
        /// The system message to set
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// Unique identifier for the conversation
        /// </summary>
        public string ConversationId { get; set; }
    }
}