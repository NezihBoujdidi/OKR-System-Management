using System;

namespace NXM.Tensai.Back.OKR.AI.Models
{
    /// <summary>
    /// Base request model with conversation ID
    /// </summary>
    public class ConversationRequest
    {
        /// <summary>
        /// Unique identifier for the conversation
        /// </summary>
        public string ConversationId { get; set; }
    }
}