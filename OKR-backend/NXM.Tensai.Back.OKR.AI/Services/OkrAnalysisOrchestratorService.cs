using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NXM.Tensai.Back.OKR.AI.Services
{
    public enum OkrAnalysisStep
    {
        Overview,
        AnalyzeRisk,
        AnalyzeOverload,
        RedistributeTasks,
        Complete
    }

    public class OkrAnalysisOrchestratorService
    {
        private readonly AzureOpenAIChatService _azureOpenAIChatService;
        private readonly ILogger<OkrAnalysisOrchestratorService> _logger;

        public OkrAnalysisOrchestratorService(AzureOpenAIChatService azureOpenAIChatService, ILogger<OkrAnalysisOrchestratorService> logger)
        {
            _azureOpenAIChatService = azureOpenAIChatService;
            _logger = logger;
        }        private readonly Dictionary<OkrAnalysisStep, string> _stepInstructions = new()
        {
            { OkrAnalysisStep.Overview, 
                "STEP 1\n" +
                "Gather OKR data and provide a clean status summary.\n\n" +
                "ACTIONS REQUIRED:\n" +
                "- Use GetOngoingOKRTasksAsync() to retrieve all OKR tasks\n" +
                "- Use GetTeamsWithCollaboratorsAsync() to get team information\n" +
                "- Count and categorize items by status only\n\n" +
                "STRICT OUTPUT FORMAT:\n" +
                "# OKR Data Overview\n\n" +
                "## Status Summary\n" +
                "• Total Tasks: [count]\n" +
                "• Not Started: [count] tasks\n" +
                "• In Progress: [count] tasks\n" +
                "• Completed: [count] tasks\n" +
                "• Overdue: [count] tasks\n\n" +
                "## Objectives Summary\n" +
                "[List each objective with task count and basic status only]\n\n" +
                "## Team Summary\n" +
                "[List teams with total assigned tasks]\n\n" +
                "✅ Data collection complete\n\n" +
                "CRITICAL: Do NOT analyze risks, workload, or provide recommendations. Only collect and summarize raw data."
            },            { OkrAnalysisStep.AnalyzeRisk, 
                "STEP 2 - KEY RESULTS RISK IDENTIFICATION ONLY\n" +
                "You are a risk assessment specialist. Data has been collected. Now identify at-risk Key Results using mathematical criteria.\n\n" +
                "KEY RESULTS RISK DETECTION:\n" +
                "• Focus on Key Results (not individual tasks)\n" +
                "• Key Result Progress is calculated as: (Completed Tasks / Total Tasks) * 100\n" +
                "• Example: KR has 4 tasks, 3 completed → Progress = 75%\n\n" +
                "RISK FORMULA FOR KEY RESULTS:\n" +
                "• ElapsedPercent = (Today - StartDate) / (EndDate - StartDate)\n" +
                "• At Risk IF: ElapsedPercent > (KeyResultProgress% / 100) + 0.20\n" +
                "• High Risk IF: ElapsedPercent > (KeyResultProgress% / 100) + 0.35\n\n" +
                "ACTIONS REQUIRED:\n" +
                "• Apply risk formula to each Key Result\n\n" +
                "STRICT OUTPUT FORMAT:\n" +
                "# Key Results Risk Assessment\n\n" +
                "## High Risk Key Results\n" +
                "[For each high-risk KR:]\n" +
                "• KR: [Name] | Objective: [Parent Objective] | Progress: [%] | Elapsed: [%] | Risk Level: High\n" +
                "  Tasks: [X] completed, [Y] in progress, [Z] not started\n\n" +
                "## Medium Risk Key Results\n" +
                "[For each medium-risk KR:]\n" +
                "• KR: [Name] | Objective: [Parent Objective] | Progress: [%] | Elapsed: [%] | Risk Level: Medium\n" +
                "  Tasks: [X] completed, [Y] in progress, [Z] not started\n\n" +
                "## Risk Summary\n" +
                "• Total Key Results at Risk: [count]\n" +
                "• High Risk: [count] Key Results\n" +
                "• Medium Risk: [count] Key Results\n" +
                "• Risk Percentage: [percentage]% of total Key Results\n\n" +
                "✅ Key Results risk assessment complete\n\n" +
                "CRITICAL: Do NOT repeat data overview or analyze individual tasks. Focus only on Key Results-level risk identification using the progress formula."
            },
            { OkrAnalysisStep.AnalyzeOverload, 
                "STEP 3 - WORKLOAD ANALYSIS ONLY\n" +
                "You are a workload distribution analyst. Risk analysis is complete. Now analyze team member workload patterns.\n\n" +
                "WORKLOAD CRITERIA:\n" +
                "• Overloaded: >4 active tasks OR >2 high-priority tasks\n" +
                "• Underutilized: 0 active tasks\n" +
                "• Count only: Not Started + In Progress (exclude Completed)\n" +
                "• Consider task complexity and deadlines\n\n" +
                "STRICT OUTPUT FORMAT:\n" +
                "# Workload Distribution Analysis\n\n" +
                "## Overloaded Members\n" +
                "[For each overloaded person:]\n" +
                "• [Name]: [X] active tasks | Team: [team] | Capacity: [over limit]\n\n" +
                "## Underutilized Members\n" +
                "[For each underutilized person:]\n" +
                "• [Name]: [X] active tasks | Team: [team] | Available capacity: [estimate]\n\n" +
                "## Distribution Metrics\n" +
                "• Average active tasks per person: [number]\n" +
                "• Workload variance: [High/Medium/Low]\n" +
                "• Teams with imbalance: [list if any]\n\n" +
                "✅ Workload analysis complete\n\n" +
                "CRITICAL: Do NOT repeat risk analysis or data overview. Focus only on workload distribution patterns."
            },
            { OkrAnalysisStep.RedistributeTasks, 
                "STEP 4 - REDISTRIBUTION STRATEGY ONLY\n" +
                "You are a task redistribution strategist. Previous analysis identified imbalances. Provide specific redistribution actions proposal.\n\n" +
                "REDISTRIBUTION RULES:\n" +
                "• Move tasks from overloaded to underutilized members\n" +
                "• Prioritize moving 'Not Started' over 'In Progress' tasks\n" +
                "• Consider team boundaries and skill compatibility\n" +
                "• Preserve critical dependencies\n\n" +
                "STRICT OUTPUT FORMAT:\n" +
                "# Task Redistribution Strategy\n\n" +
                "## Immediate Actions (Priority 1)\n" +
                "[For critical redistributions:]\n" +
                "→ Move '[Task Name]' FROM [Current Assignee] TO [Recommended Assignee]\n" +
                "   Reason: [specific justification]\n\n" +
                "## Secondary Actions (Priority 2)\n" +
                "[For important redistributions:]\n" +
                "→ Move '[Task Name]' FROM [Current Assignee] TO [Recommended Assignee]\n" +
                "   Reason: [specific justification]\n\n" +
                "✅ Redistribution strategy complete\n\n" +
                "CRITICAL: Do NOT repeat previous analysis. Focus only on specific, actionable task movement recommendations."
            }
        };public async Task<string> RunAnalysisAsync(string conversationId, string userMessage)
        {
            _logger.LogInformation("Starting OKR Analysis Orchestration for conversation: {ConversationId}", conversationId);
            
            var stepResults = new List<string>();
            var accumulatedContext = "";
              // Base system message - enhanced to match original ChatController style
            string baseSystemMessage = 
                "You are an expert OKR Risk Analyst for the Tensai OKR Management System. Your role is to perform systematic, step-by-step analysis of OKR data to identify risks, workload imbalances, and provide actionable recommendations.\n\n" +
                "CORE PRINCIPLES:\n" +
                "- Be precise and data-driven in your analysis\n" +
                "- Use available functions to execute operations when needed\n" +
                "- Follow the specific task instructions for each analysis step\n" +
                "- Maintain consistency in format and terminology\n" +
                "- Each function returns objects that should inform your analysis\n\n" +
                "CRITICAL REQUIREMENTS:\n" +
                "- Execute ONLY the specific task for the current step\n" +
                "- Use the exact output format provided in the step instructions\n" +
                "- Do NOT repeat information from previous steps\n" +
                "- Do NOT ask questions - provide direct analysis\n" +
                "- Be concise but comprehensive within your assigned scope\n\n";

            foreach (OkrAnalysisStep step in Enum.GetValues(typeof(OkrAnalysisStep)))
            {
                if (step == OkrAnalysisStep.Complete) break;

                _logger.LogInformation("Executing OKR Analysis Step: {Step}", step);

                // Compose focused system message for this specific step
                var stepSystemMessage = baseSystemMessage + _stepInstructions[step];
                
                // For first step, use user message. For subsequent steps, use accumulated context
                string contextForStep = step == OkrAnalysisStep.Overview 
                    ? $"User request: {userMessage}" 
                    : $"Previous analysis completed. Continue with next step.\n\nPrevious context:\n{accumulatedContext}";

                var messages = new List<Models.EnhancedChatMessage>
                {
                    Models.EnhancedChatMessage.FromUser(contextForStep)
                };                try
                {
                    // Call AzureOpenAIChatService for this step
                    var stepResponse = await _azureOpenAIChatService.ExecuteChatWithFunctionsAsync(
                        stepSystemMessage, 
                        messages);

                    _logger.LogInformation("Step {Step} completed successfully. Response length: {Length} characters", 
                        step, stepResponse?.Length ?? 0);
                    
                    // Log the actual response for debugging and monitoring
                    _logger.LogInformation("Step {Step} LLM Response: {Response}", step, stepResponse);

                    // Store result without "Step:" prefix to avoid repetition
                    stepResults.Add(stepResponse);
                      // Accumulate minimal context for next step - avoid data repetition
                    switch (step)
                    {
                        case OkrAnalysisStep.Overview:
                            accumulatedContext = "Overview phase completed: OKR data has been collected and summarized.";
                            break;
                        case OkrAnalysisStep.AnalyzeRisk:
                            accumulatedContext += " Risk analysis completed: At-risk tasks have been identified using mathematical criteria.";
                            break;
                        case OkrAnalysisStep.AnalyzeOverload:
                            accumulatedContext += " Workload analysis completed: Team member workload patterns have been analyzed.";
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing OKR Analysis Step: {Step}", step);
                    stepResults.Add($"Error in {step}: {ex.Message}");
                }
            }            _logger.LogInformation("OKR Analysis Orchestration completed for conversation: {ConversationId}. Total steps: {StepCount}", 
                conversationId, stepResults.Count);

            // Combine results with professional section separators
            var finalResult = string.Join("\n\n" + new string('=', 80) + "\n\n", stepResults);
            
            // Add comprehensive header with timestamp
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
            return $"# Complete OKR Risk Analysis Report\n" +
                   $"**Generated:** {timestamp}\n" +
                   $"{finalResult}\n\n" +
                   $"**End of Analysis Report**";
        }
    }
}
