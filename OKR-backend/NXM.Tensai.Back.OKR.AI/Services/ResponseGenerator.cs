using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NXM.Tensai.Back.OKR.AI.Models;

namespace NXM.Tensai.Back.OKR.AI.Services
{
    /// <summary>
    /// Service for generating AI responses based on various inputs and contexts
    /// </summary>
    public class ResponseGenerator
    {
        private readonly KernelService _kernelService;
        private readonly ILogger<ResponseGenerator> _logger;

        public ResponseGenerator(
            KernelService kernelService,
            ILogger<ResponseGenerator> logger)
        {
            _kernelService = kernelService;
            _logger = logger;
        }

        /// <summary>
        /// Generates a consolidated natural response for multiple operations
        /// </summary>
        public async Task<string> GenerateConsolidatedResponse(List<FunctionResultItem> results)
        {
            try
            {
                // Create a summary of the operations performed
                var summarySb = new StringBuilder();
                summarySb.AppendLine("I've performed the following operations:");
                
                foreach (var result in results)
                {
                    // Extract key information from the result
                    string entityName = "unknown";
                    string entityType = result.EntityType ?? "item";
                    
                    // Try to extract entity name from the result data if available
                    if (result.Data != null)
                    {
                        try
                        {
                            // Use reflection to find common name properties
                            foreach (var propName in new[] { "Name", "TeamName", "Title", "ObjectiveName" })
                            {
                                var prop = result.Data.GetType().GetProperty(propName);
                                if (prop != null)
                                {
                                    entityName = prop.GetValue(result.Data)?.ToString() ?? "unknown";
                                    if (!string.IsNullOrEmpty(entityName)) break;
                                }
                            }
                        }
                        catch
                        {
                            // Ignore any errors in extracting the name
                        }
                    }
                    
                    summarySb.AppendLine($"- {result.Operation} {entityType} '{entityName}' (ID: {result.EntityId})");
                }
                
                // Generate a prompt for the AI to create a cohesive response
                string prompt = $@"
I've just performed multiple operations for the user. Here's what happened:

{summarySb.ToString().Trim()}

Please generate a single, cohesive, natural-sounding response that summarizes what was done,
avoiding repetition and using a conversational tone. Focus on the end results rather than listing each step.
Ask if the user needs any further assistance.
";

                // Use the KernelService to get an AI response
                return await _kernelService.ExecuteSinglePromptAsync(
                    "You are an AI assistant helping users manage their OKR system. Be concise and helpful.", 
                    prompt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating consolidated response");
                
                // Fallback to a simple response if we couldn't generate a better one
                return "I've completed all your requested actions successfully. Is there anything else you need help with?";
            }
        }
    }
}
