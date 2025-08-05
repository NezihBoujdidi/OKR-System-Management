using System;

namespace NXM.Tensai.Back.OKR.AI.Models
{
    /// <summary>
    /// Model for setting a custom system message
    /// </summary>
    public class SystemMessageRequest
    {
        /// <summary>
        /// The system message to set
        /// </summary>
        public string Message { get; set; }
    }
}