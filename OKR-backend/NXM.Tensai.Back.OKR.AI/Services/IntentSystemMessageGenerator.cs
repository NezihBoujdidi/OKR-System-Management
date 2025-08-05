using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NXM.Tensai.Back.OKR.AI.Services
{
    public class IntentSystemMessageGenerator
    {
        private readonly KernelService _kernelService;
        private readonly ILogger<IntentSystemMessageGenerator> _logger;
        private readonly Dictionary<string, Type> _intentTypes = new();
        private readonly string _defaultSystemMessage;
        private readonly IConfiguration _configuration;

        public IntentSystemMessageGenerator(
            KernelService kernelService,
            IConfiguration configuration,
            ILogger<IntentSystemMessageGenerator> logger)
        {
            _kernelService = kernelService;
            _logger = logger;
            _configuration = configuration;
            _defaultSystemMessage = configuration["AI:DefaultSystemMessage"] 
                ?? "You are an AI assistant for an OKR Management System called Tensai. Help users manage their teams, objectives, and key results.";
            
            // Find all intent classes in the assembly
            LoadIntentTypes();
        }

        /// <summary>
        /// Loads all classes with the Intent attribute from the assembly
        /// </summary>
        private void LoadIntentTypes()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var intentTypes = assembly.GetTypes()
                    .Where(t => t.GetCustomAttributes(typeof(IntentAttribute), true).Length > 0);
                
                foreach (var type in intentTypes)
                {
                    var attr = type.GetCustomAttribute<IntentAttribute>();
                    if (attr != null)
                    {
                        _intentTypes[attr.Name] = type;
                        _logger.LogInformation("Registered intent: {IntentName}", attr.Name);
                    }
                }
                
                _logger.LogInformation("Loaded {Count} intent types", _intentTypes.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading intent types");
            }
        }
        
        /// <summary>
        /// Determine the best system message based on user context and message content
        /// </summary>
        public async Task<string> GetSystemMessageAsync(string userContext, string userMessage)
        {
            if (string.IsNullOrEmpty(userMessage))
            {
                return _defaultSystemMessage;
            }
            
            // Generate the system message listing all available intents
            var systemMessage = new StringBuilder();
            systemMessage.AppendLine("You are an AI assistant for an OKR Management System called Tensai.");
            systemMessage.AppendLine("You help users manage their teams, objectives, and key results.");
            systemMessage.AppendLine();
            
            if (_intentTypes.Count > 0)
            {
                systemMessage.AppendLine("You can assist with the following capabilities:");
                foreach (var intent in _intentTypes)
                {
                    var attr = intent.Value.GetCustomAttribute<IntentAttribute>();
                    if (attr != null)
                    {
                        systemMessage.AppendLine($"- {attr.Name}: {attr.Description}");
                    }
                }
            }
            
            // Add context about the user if available
            if (!string.IsNullOrEmpty(userContext))
            {
                systemMessage.AppendLine();
                systemMessage.AppendLine("User context:");
                systemMessage.AppendLine(userContext);
            }
            
            return systemMessage.ToString();
        }

        /// <summary>
        /// Generate the intent detection system message for analyzing user prompts
        /// </summary>
        public string GenerateIntentDetectionSystemMessage()
        {
            // Check which LLM provider we're using to adjust the message length
            var provider = _configuration["AI:Provider"]?.ToLower() ?? "azureopenai";
            bool isCohere = provider == "cohere";
            
            // Check if there's a configuration override for message compression
            bool useCompression = false;
            if (_configuration["AI:UseCompressedPrompts"] != null)
            {
                bool.TryParse(_configuration["AI:UseCompressedPrompts"], out useCompression);
            }
            else
            {
                // Default to compression only for Cohere
                useCompression = isCohere;
            }

            // Use compression if configured or if using Cohere
            if (useCompression)
            {
                _logger.LogInformation("Using compressed system message to stay within token limits");
                return GenerateCondensedIntentDetectionSystemMessage();
            }

            // Using the full system message for other providers
            return GenerateFullIntentDetectionSystemMessage();
        }

        /// <summary>
        /// Generate a condensed version of the intent detection system message to stay within Cohere's token limits
        /// </summary>
        private string GenerateCondensedIntentDetectionSystemMessage()
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are an AI assistant that helps identify user intents from natural language.");
            sb.AppendLine("Based on the user's message, determine WHICH OF THE FOLLOWING INTENTS it matches. A single message may contain MULTIPLE INTENTS:");
            sb.AppendLine();
            
            // Define team-related intents with minimal but sufficient descriptions
            sb.AppendLine("CreateTeam: Create a new team. Extract 'teamName' and optional 'description'.");
            sb.AppendLine("UpdateTeam: Update a team. Extract 'name' if team mentioned by name, 'teamId' only if explicitly provided as UUID. For team manager references, extract 'teamManagerName' if name provided or 'teamManagerId' if ID provided.");
            sb.AppendLine("DeleteTeam: Delete a team. Extract 'teamId' if explicitly provided, or 'teamName' if mentioned by name.");
            sb.AppendLine("GetTeamInfo: Get info about a team. Extract 'teamName'.");
            sb.AppendLine("GetTeamsByManagerId: Find teams by manager. Extract 'managerId'.");
            sb.AppendLine("GetTeamsByOrganizationId: List all teams in organization or list me all the teams.");
            sb.AppendLine("SearchTeams: List teams by name criteria. Extract 'query'.");
            
            // User-related intents
            sb.AppendLine("SearchUsers: Search users by name. Extract 'query'.");
            sb.AppendLine("GetTeamManagers: List all team managers.");
            sb.AppendLine("InviteUser: Invite user to organization. Extract 'email', optional 'role' (default 'Collaborator'), 'teamId'/'teamName'.");
            sb.AppendLine("GetUsersByOrganizationId: List all users in organization.");
            sb.AppendLine("EnableUser: Enable a disabled user account. Extract 'userName' if mentioned by name."); //, or 'userId' if provided as UUID
            sb.AppendLine("DisableUser: Disable a user account. Extract 'userName' if mentioned by name."); //, or 'userId' if provided as UUID
            sb.AppendLine("UpdateUser: Update user profile information. Extract 'userName' if the user to update is mentioned by name. Extract any fields to update: 'firstName', 'lastName', 'email', 'position', etc."); //, or 'userId' if mentioned as UUID
            
            // OKR session intents - preserve critical parameter extraction guidance
            sb.AppendLine("CreateOkrSession: Create OKR session. Extract 'title', 'startDate', 'endDate', 'teamManagerId'/'teamManagerName', optional 'description', 'teamIds', 'color', 'status'(only: NotStarted, InProgress, Completed, Overdue).");
            sb.AppendLine("UpdateOkrSession: Update OKR session. Extract ORIGINAL 'title' if mentioned (e.g., 'update OKR session titled Marketing Goals'). DO NOT set okrSessionId parameter unless explicitly given a UUID. Extract update fields like 'description', 'teamManagerId'/'teamManagerName', 'color', 'status'(only: NotStarted, InProgress, Completed, Overdue), 'newTitle' (if renaming), dates, etc. If updating the title, use 'newTitle' parameter.");
            sb.AppendLine("DeleteOkrSession: Delete OKR session. Extract 'okrSessionId' or 'title'.");
            sb.AppendLine("GetOkrSessionInfo: Get session info. Extract 'okrSessionId' or 'title'.");
            sb.AppendLine("SearchOkrSessions: Search sessions. Extract 'title', optional 'userId'.");
            sb.AppendLine("GetAllOkrSessions: List all OKR sessions.");
            sb.AppendLine("GetOkrSessionsByTeam: Get sessions for a team. Extract 'teamId'/'teamName'.");
            
            // Objective intents
            sb.AppendLine("CreateObjective: Create objective. Extract 'title', 'okrSessionId'/'okrSessionTitle', optional dates, 'responsibleTeamId'/'responsibleTeamName', status, etc.");
            sb.AppendLine("UpdateObjective: Update objective. Extract 'title' if mentioned, 'objectiveId' only if UUID. Extract updated fields like 'newTitle', 'description', etc.");
            sb.AppendLine("DeleteObjective: Delete objective. Extract 'objectiveId' or 'title'.");
            sb.AppendLine("GetObjectiveInfo: Get objective info. Extract 'objectiveId'/'title'.");
            sb.AppendLine("SearchObjectives: Search objectives. Extract 'title', 'okrSessionId'/'okrSessionTitle', 'responsibleTeamId'/'responsibleTeamName', optional 'userId'.");
            sb.AppendLine("GetAllObjectives: List all objectives.");
            sb.AppendLine("GetObjectivesBySession: Get objectives for session. Extract 'okrSessionId'/'okrSessionTitle'.");
            
            // Key result intents - preserve critical extraction guidance
            sb.AppendLine("CreateKeyResult: Create key result. Extract 'title', 'objectiveId'/'objectiveTitle', optional dates, status, progress.");
            sb.AppendLine("UpdateKeyResult: Update key result. Extract 'title' if mentioned, 'keyResultId' only if UUID. Extract updated fields.");
            sb.AppendLine("DeleteKeyResult: Delete key result. Extract 'keyResultId'/'title'.");
            sb.AppendLine("GetKeyResultInfo: Get key result info. Extract 'keyResultId'/'title'.");
            sb.AppendLine("SearchKeyResults: Search key results. Extract 'title', 'objectiveId'/'objectiveTitle', optional 'userId'.");
            sb.AppendLine("GetAllKeyResults: List all key results.");
            sb.AppendLine("GetKeyResultsByObjective: Get key results for objective. Extract 'objectiveId'/'objectiveTitle'.");
            
            // Task intents - preserve important collaboration information
            sb.AppendLine("CreateKeyResultTask: Create task. Extract 'title', 'keyResultId'/'keyResultTitle', optional 'description', dates, 'collaboratorId'/'collaboratorName', progress, priority.");
            sb.AppendLine("UpdateKeyResultTask: Update task. Extract 'taskId'/'title'. For collaborator, extract 'collaboratorName' if name provided, 'collaboratorId' if ID provided.");
            sb.AppendLine("DeleteKeyResultTask: Delete task. Extract 'taskId'/'title'.");
            sb.AppendLine("GetKeyResultTaskInfo: Get task info. Extract 'taskId'/'title'.");
            sb.AppendLine("SearchKeyResultTasks: Search tasks. Extract 'title', 'keyResultId'/'keyResultTitle', optional 'userId'.");
            sb.AppendLine("GetAllKeyResultTasks: List all tasks.");
            sb.AppendLine("GetKeyResultTasksByKeyResult: Get tasks for key result. Extract 'keyResultId'/'keyResultTitle'.");
            
            // General intent
            sb.AppendLine("General: General conversation or request not matching above intents.");
            
            // CRITICAL: Entity reference instructions - these are important to preserve
            sb.AppendLine("\nIMPORTANT INSTRUCTIONS:");
            sb.AppendLine("- Use IDs from RECENT CONTEXT section for entity references");
            sb.AppendLine("- For team/session/objective/key result/task updates, extract name/title if mentioned");
            sb.AppendLine("- Only extract IDs (teamId, okrSessionId, etc.) if they are actual UUIDs");
            sb.AppendLine("- If user mentions an entity by name and it appears in RECENT CONTEXT with an ID, use that ID");
            sb.AppendLine("- If no ID found, use name/title parameter for identification");
            
            // JSON format instructions - kept concise but complete
            sb.AppendLine("\nJSON FORMAT: Return ONLY JSON in this format:");
            sb.AppendLine("{\"intents\": [{\"intent\": \"IntentName\", \"parameters\": {\"param1\": \"value1\"}}]}");
            
            return sb.ToString();
        }

        /// <summary>
        /// Generate the full intent detection system message for providers without strict token limits
        /// </summary>
        private string GenerateFullIntentDetectionSystemMessage()
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are an AI assistant that helps identify user intents from natural language.");
            sb.AppendLine("Based on the user's message, determine WHICH OF THE FOLLOWING INTENTS it matches. A single message may contain MULTIPLE INTENTS:");
            sb.AppendLine();
            
            // Full detailed descriptions of all intents as in the original method
            // Define team-related intents with clearer guidance
            sb.AppendLine("CreateTeam: The user wants to create a new team. Extract 'teamName' and optional 'description'.");
            sb.AppendLine("UpdateTeam: The user wants to update a team. IMPORTANT: First extract 'name' if the user mentions a team by name (e.g., 'update team DevOps'). This name will be used to search for the team. Only extract 'teamId' if explicitly provided as a UUID. Extract any other parameters being updated: 'description', 'teamManagerId', etc. If user mentions a team manager by name but not ID, extract 'teamManagerName' parameter instead of 'teamManagerId'.");
            sb.AppendLine("DeleteTeam: The user wants to delete a team. Extract 'teamId' if explicitly provided. If the user mentions a team by name (e.g., 'delete team DevOps'), extract 'teamName' parameter with that team name.");
            sb.AppendLine("GetTeamInfo: The user wants information about a team. Extract 'teamName'. For direct team ID queries, identify the ID as the teamName.");
            sb.AppendLine("GetTeamsByManagerId: The user wants to find teams managed by someone. Extract 'managerId'. Manager IDs are in UUID format ");
            sb.AppendLine("GetTeamsByOrganizationId: The user wants to list all teams in an organization. No need to extract organization ID as the logged in user will already have the organization ID in their user details.");
            sb.AppendLine("SearchTeams: The user wants to list all teams that have a certain criteria in their names. Extract 'query'.");
            
            // Add user-related intents
            sb.AppendLine("SearchUsers: The user wants to search for users by name. Extract 'query' which is the name or part of the name to search for.");
            sb.AppendLine("GetTeamManagers: The user wants to list all team managers in the organization. No need to extract organization ID as the logged in user will already have the organization ID in their user details.");
            sb.AppendLine("InviteUser: The user wants to invite someone to the organization. Extract 'email' which is required. Also extract optional 'role' (default is 'Collaborator') and optional 'teamId' if explicitly provided, else if user mentions team by name, extract 'teamName' parameter with that team name, they want to add the person to a team.");
            sb.AppendLine("GetUsersByOrganizationId: The user wants to list all users in the organization. No parameters needed as the organization ID is automatically provided from user context.");
            sb.AppendLine("EnableUser: The user wants to enable a previously disabled user account. If the user mentions a user by name (e.g., 'enable John Smith's account'), extract 'userName' parameter with that user name."); // Extract 'userId' only if explicitly provided as a UUID
            sb.AppendLine("DisableUser: The user wants to disable a user account. If the user mentions a user by name (e.g., 'disable John Smith's account'), extract 'userName' parameter with that user name."); //Extract 'userId' only if explicitly provided as a UUID
            sb.AppendLine("UpdateUser: The user wants to update a user's profile information. Extract 'userName' if the user mentions someone by name. Extract any fields being updated if they are specified in prompt, else don't extract them as empty: 'firstName', 'lastName', 'email', 'address', 'position', 'profilePictureUrl', 'isNotificationEnabled'.");//, or extract 'userId' only if explicitly provided as user ID as a UUID
            
            // Add OKR session-related intents
            sb.AppendLine("CreateOkrSession: The user wants to create a new OKR session. Extract 'title', 'startDate', 'endDate', 'teamManagerId' or 'teamManagerName', and optional 'description', 'teamIds', 'color', and 'status'.");
            sb.AppendLine("UpdateOkrSession: The user wants to update an OKR session. IMPORTANT: First extract 'title' if the user mentions a session by title (e.g., 'update OKR session titled Marketing Goals'). DO NOT set okrSessionId parameter unless explicitly given a UUID. Extract update fields as mentioned by user: 'description', 'startDate', 'endDate', 'teamManagerId'/'teamManagerName', 'color', 'status', etc. If renaming the session, use 'newTitle' parameter for the new name. All fields are optional including 'color'. Don't ask for fields not mentioned by the user.");
            sb.AppendLine("DeleteOkrSession: The user wants to delete an OKR session. Extract 'okrSessionId' if provided. If the user mentions a session by title (e.g., 'delete Q1 2023 OKR session'), extract 'title' parameter with that session title.");
            sb.AppendLine("GetOkrSessionInfo: The user wants information about an OKR session. Extract 'okrSessionId' if provided, or 'title' if the user mentions a session by title.");
            sb.AppendLine("SearchOkrSessions: The user wants to search for OKR sessions with specific criteria. Extract 'title' and optional 'userId'.");
            sb.AppendLine("GetAllOkrSessions: The user wants to list or view all OKR sessions without any filter. Use this intent when the user explicitly asks to see all OKR sessions (e.g., 'list all OKR sessions', 'show me all OKR sessions', 'view all OKR sessions'). No specific parameters are needed.");
            sb.AppendLine("GetOkrSessionsByTeam: The user wants to find OKR sessions for a specific team. Extract 'teamId' if provided, or 'teamName' if the user mentions a team by name.");
            
            // Add objective-related intents
            sb.AppendLine("CreateObjective: The user wants to create a new objective. Extract 'title', 'okrSessionId' or 'okrSessionTitle', and optional 'startDate', 'endDate', 'responsibleTeamId', 'responsibleTeamName', 'description', 'status', 'priority', and 'progress'.");
            sb.AppendLine("UpdateObjective: The user wants to update an objective. First extract 'title' if the user mentions an objective by title (e.g., 'update Increase Revenue objective'). Only extract 'objectiveId' if explicitly provided as a UUID. Extract any other parameters being updated: 'newTitle', 'description', 'startDate', 'endDate', 'responsibleTeamId', 'responsibleTeamName', 'status', 'priority', 'progress'.");
            sb.AppendLine("DeleteObjective: The user wants to delete an objective. Extract 'objectiveId' if provided. If the user mentions an objective by title (e.g., 'delete Increase Revenue objective'), extract 'title' parameter with that objective title.");
            sb.AppendLine("GetObjectiveInfo: The user wants information about an objective. Extract 'objectiveId' if provided, or 'title' if the user mentions an objective by title.");
            sb.AppendLine("SearchObjectives: The user wants to search for objectives with specific criteria. Extract 'title', 'okrSessionId', 'okrSessionTitle', 'responsibleTeamId', 'responsibleTeamName', and optional 'userId'.");
            sb.AppendLine("GetAllObjectives: The user wants to list or view all objectives without any filter. Use this intent when the user explicitly asks to see all objectives (e.g., 'list all objectives', 'show me all objectives', 'view all objectives'). No specific parameters are needed.");
            sb.AppendLine("GetObjectivesBySession: The user wants to find objectives for a specific OKR session. Extract 'okrSessionId' if provided, or 'okrSessionTitle' if the user mentions an OKR session by title.");
            
            // Add key result-related intents
            sb.AppendLine("CreateKeyResult: The user wants to create a new key result. Extract 'title', 'objectiveId' or 'objectiveTitle', and optional 'startDate', 'endDate', 'description', 'status', and 'progress'.");
            sb.AppendLine("UpdateKeyResult: The user wants to update a key result. First extract 'title' if the user mentions a key result by title (e.g., 'update Increase Conversion Rate key result'). Only extract 'keyResultId' if explicitly provided as a UUID. Extract any other parameters being updated: 'newTitle', 'description', 'startDate', 'endDate', 'status', 'progress'.");
            sb.AppendLine("DeleteKeyResult: The user wants to delete a key result. Extract 'keyResultId' if provided. If the user mentions a key result by title (e.g., 'delete Increase Conversion Rate key result'), extract 'title' parameter with that key result title.");
            sb.AppendLine("GetKeyResultInfo: The user wants information about a key result. Extract 'keyResultId' if provided, or 'title' if the user mentions a key result by title.");
            sb.AppendLine("SearchKeyResults: The user wants to search for key results with specific criteria. Extract 'title', 'objectiveId', 'objectiveTitle', and optional 'userId'.");
            sb.AppendLine("GetAllKeyResults: The user wants to list or view all key results without any filter. Use this intent when the user explicitly asks to see all key results (e.g., 'list all key results', 'show me all key results', 'view all key results'). No specific parameters are needed.");
            sb.AppendLine("GetKeyResultsByObjective: The user wants to find key results for a specific objective. Extract 'objectiveId'/'objectiveTitle'.");
            
            // Add key result task-related intents
            sb.AppendLine("CreateKeyResultTask: The user wants to create a new task for a key result. Extract 'title', 'keyResultId'/'keyResultTitle', and optional 'description', 'startDate', 'endDate', 'userId', 'collaboratorId', 'collaboratorName', 'progress', and 'priority'.");
            sb.AppendLine("UpdateKeyResultTask: The user wants to update a key result task. First extract 'taskId' if explicitly provided as a UUID, or 'title' if the user mentions a task by title (e.g., 'update Research Competitors task'). Extract any other parameters being updated: 'newTitle', 'description', 'startDate', 'endDate', 'keyResultId', 'keyResultTitle', 'collaboratorId', 'collaboratorName', 'progress', 'priority'.");
            sb.AppendLine("DeleteKeyResultTask: The user wants to delete a key result task. Extract 'taskId' if provided. If the user mentions a task by title (e.g., 'delete Research Competitors task'), extract 'title' parameter with that task title.");
            sb.AppendLine("GetKeyResultTaskInfo: The user wants information about a specific key result task. Extract 'taskId' if provided, or 'title' if the user mentions a task by title.");
            sb.AppendLine("SearchKeyResultTasks: The user wants to search for tasks with specific criteria. Extract 'title', 'keyResultId', 'keyResultTitle', and optional 'userId'.");
            sb.AppendLine("GetAllKeyResultTasks: List all tasks.");
            sb.AppendLine("GetKeyResultTasksByKeyResult: Get tasks for key result. Extract 'keyResultId'/'keyResultTitle'.");
            
            // General intent
            sb.AppendLine("General: The user is making a general conversation or request not matching the above intents.");
            
            // Essential formatting instructions
            sb.AppendLine("\nIMPORTANT: Use IDs from RECENT CONTEXT section for entity references.");
            sb.AppendLine("IMPORTANT: Extract appropriate parameters for each intent.");
            
            // JSON format instructions - kept concise but complete
            sb.AppendLine("\nRespond in JSON format:");
            sb.AppendLine("{\"intents\": [{\"intent\": \"IntentName\", \"parameters\": {\"param1\": \"value1\"}}]}");
            
            return sb.ToString();
        }
    }

    /// <summary>
    /// Attribute for marking a class as an intent handler
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class IntentAttribute : Attribute
    {
        public string Name { get; }
        public string Method { get; }
        public string Description { get; }
        
        public IntentAttribute(string name, string method, string description)
        {
            Name = name;
            Method = method;
            Description = description;
        }
    }
    
    /// <summary>
    /// Attribute for marking a property as an intent parameter
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class IntentParameterAttribute : Attribute
    {
        public string Description { get; }
        public bool Required { get; }
        public string Example { get; }
        
        public IntentParameterAttribute(string description, bool required = false, string example = "")
        {
            Description = description;
            Required = required;
            Example = example;
        }
    }
    
    /// <summary>
    /// Interface that all intent model classes must implement
    /// </summary>
    public interface IIntentModel
    {
    }
}