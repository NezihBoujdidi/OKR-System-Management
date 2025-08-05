using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NXM.Tensai.Back.OKR.AI.Models
{
    /// <summary>
    /// Result of intent analysis from the LLM
    /// </summary>
    public class PromptAnalysisResult
    {
        /// <summary>
        /// The detected intent name (e.g., "CreateTeam", "GetTeamInfo")
        /// </summary>
        [JsonPropertyName("intent")]
        public string Intent { get; set; }

        /// <summary>
        /// Parameters extracted from the user message
        /// </summary>
        [JsonPropertyName("parameters")]
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Result for multi-intent analysis that can contain multiple intent requests
    /// </summary>
    public class MultiIntentAnalysisResult
    {
        /// <summary>
        /// List of all detected intents and their parameters
        /// </summary>
        [JsonPropertyName("intents")]
        public List<IntentRequest> Intents { get; set; } = new List<IntentRequest>();
    }

    /// <summary>
    /// Single intent request with parameters
    /// </summary>
    public class IntentRequest
    {
        /// <summary>
        /// The detected intent name
        /// </summary>
        [JsonPropertyName("intent")]
        public string Intent { get; set; }

        /// <summary>
        /// Parameters extracted for this intent
        /// </summary>
        [JsonPropertyName("parameters")]
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
    }
}