using System;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace NXM.Tensai.Back.OKR.AI.Utilities
{
    /// <summary>
    /// Helper class for JSON-related operations
    /// </summary>
    public static class JsonHelper
    {
        /// <summary>
        /// Sanitizes the JSON response from the AI to ensure it's valid JSON
        /// </summary>
        public static string SanitizeJsonResponse(string jsonResponse)
        {
            try
            {
                // Try to fix common formatting issues in the AI-generated JSON

                // Fix unquoted intent values - match "intent": SomeValue, and replace with "intent": "SomeValue",
                jsonResponse = Regex.Replace(
                    jsonResponse,
                    @"""intent"":\s*([a-zA-Z0-9_]+)([,}])",
                    "\"intent\": \"$1\"$2");

                // Fix missing quotes around parameter keys
                jsonResponse = Regex.Replace(
                    jsonResponse,
                    @"([{,])\s*([a-zA-Z0-9_]+)\s*:",
                    "$1\"$2\":");
                
                // Fix trailing commas in arrays (the issue causing the error)
                jsonResponse = Regex.Replace(
                    jsonResponse,
                    @",(\s*[\]}])",
                    "$1");

                // Remove any non-JSON content before the first {
                int firstBrace = jsonResponse.IndexOf('{');
                if (firstBrace > 0)
                {
                    jsonResponse = jsonResponse.Substring(firstBrace);
                }

                // Remove any content after the last }
                int lastBrace = jsonResponse.LastIndexOf('}');
                if (lastBrace >= 0 && lastBrace < jsonResponse.Length - 1)
                {
                    jsonResponse = jsonResponse.Substring(0, lastBrace + 1);
                }

                return jsonResponse;
            }
            catch (Exception ex)
            {
                // If sanitization fails, wrap the original value in a simple valid JSON structure
                return $"{{\"intents\": [{{\"intent\": \"General\", \"parameters\": {{}}}}]}}";
            }
        }
    }
}
