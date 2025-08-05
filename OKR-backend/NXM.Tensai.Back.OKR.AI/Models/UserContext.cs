using System;

namespace NXM.Tensai.Back.OKR.AI.Models
{
    /// <summary>
    /// Represents context information about the user making a request
    /// </summary>
    public class UserContext
    {
        /// <summary>
        /// The user's unique identifier
        /// </summary>
        public string UserId { get; set; }
        
        /// <summary>
        /// The user's name or username
        /// </summary>
        public string UserName { get; set; }
        
        /// <summary>
        /// The user's email address
        /// </summary>
        public string Email { get; set; }
        
        /// <summary>
        /// The organization the user belongs to
        /// </summary>
        public string OrganizationId { get; set; }
        
        /// <summary>
        /// The user's role in the organization
        /// </summary>
        public string Role { get; set; }
        
        /// <summary>
        /// The selected LLM provider (e.g., "openai", "cohere")
        /// </summary>
        public string SelectedLLMProvider { get; set; }
    }
}