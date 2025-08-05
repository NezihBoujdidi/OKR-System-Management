using System;
using System.Collections.Generic;

namespace NXM.Tensai.Back.OKR.AI.Core.AI
{
    public class PromptTemplateService
    {
        private readonly Dictionary<string, string> _templates;

        public PromptTemplateService()
        {
            _templates = new Dictionary<string, string>
            {
                { "CreateTeam", "Create a team named '{teamName}' and provide a short summary of the team's purpose." },
                { "CreateTeamWithDescription", "Create a team named '{teamName}' with the following description: '{description}'. Provide a short summary of how this team will function based on the description." },
                
                // Updated templates removing organization references since the context is already known
                { "TeamCreated", "The team '{teamName}' has been created successfully. Would you like to add members to this team or set up some objectives?" },
                { "TeamCreatedWithDescription", "The team '{teamName}' has been created with the description: '{description}'. Would you like to add team members now?" },
                { "TeamUpdated", "I've updated the team '{teamName}'. Is there anything else you'd like to modify?" },
                { "TeamUpdatedWithDescription", "I've updated the team '{teamName}' with the new description: '{description}'. Anything else you'd like to change?" },
                { "TeamDeleted", "I've successfully deleted the team '{teamName}'. All associated resources have been cleaned up. Is there anything else you'd like to do?" },
                
                // Updated templates for team search and listings to handle zero results case
                { "TeamSearchResults", "I found {count} teams matching your search criteria \"{searchTerm}\".\n\nCan I assist you with anything else?" },
                { "TeamSearchResultsEmpty", "I couldn't find any teams matching your search criteria \"{searchTerm}\". Would you like to create a new team or try a different search?" },
                { "TeamsByManagerResults", "I found {count} teams managed by that specified manager.\n\nCan I assist you with anything else?" },
                { "TeamsByManagerResultsEmpty", "I couldn't find any teams associated with the manager. Would you like to assign a team to this manager?" },
                { "TeamsByOrgResults", "I found {count} teams in this organization.\n\nCan I assist you with anything else?" },
                { "TeamsByOrgResultsEmpty", "I couldn't find any teams in the organization . Would you like to create a new team?" },
                { "TeamDetails", "Here are the details for team '{teamName}':\n\nDescription: {description}\nMembers: {members}\n\nWould you like to add more members or set up objectives for this team?" },
                
                // OKR Session templates
                { "OkrSessionCreated", "I've successfully created the OKR session '{title}' running from {startDate} to {endDate}. The session has been added to the system and is ready for objectives and key results to be added. Would you like me to help you add objectives to this session?" },
                { "OkrSessionCreatedWithDescription", "I've successfully created the OKR session '{title}' with the description: '{description}'. The session runs from {startDate} to {endDate} and is ready for objectives and key results to be added. Would you like me to help you add objectives to this session?" },
                { "OkrSessionUpdated", "I've updated the OKR session '{title}' . Would you like me to help you with anything else related to this session?" },
                { "OkrSessionUpdatedWithDescription", "I've updated the OKR session '{title}' with description: '{description}'. Would you like me to help you with anything else related to this session?" },
                { "OkrSessionDeleted", "I've successfully deleted the OKR session '{title}'. All associated resources have been cleaned up. Is there anything else you'd like me to help you with?" },
                { "OkrSessionDetails", "Here are the details for the OKR session '{title}' :\n\nDescription: {description}\nPeriod: {startDate} to {endDate}\nStatus: {status}\nProgress: {progress}%\n\nWould you like me to help you manage objectives for this session?" },
                { "OkrSessionSearchResults", "I found {count} OKR sessions \nCan I assist you with anything else?" },
                { "OkrSessionSearchResultsEmpty", "I couldn't find any OKR sessions matching your search criteria \"{searchTerm}\". Would you like to create a new OKR session or try a different search?" },
                { "OkrSessionsByTeamResults", "I found {count} OKR sessions for team '{teamName}'. \n\nCan I assist you with anything else?" },
                { "OkrSessionsByTeamResultsEmpty", "I couldn't find any OKR sessions associated with team '{teamName}'. Would you like to create a new OKR session for this team?" },
                
                // Objective templates
                {"ObjectiveCreatedWithDescription", "I've successfully created the Objective '{title}' running from {startedDate} to {endDate} and with responsible team {teamName} and description {description}. The objective has been added to the system. Would you like me to help you add keyresults to this session?"},
                {"ObjectiveCreated", "I've successfully created the Objective '{title}' running from {startedDate} to {endDate} and with responsible team {teamName}. The objective has been added to the system. Would you like me to help you add keyresults to this session?"},    
                {"ObjectiveUpdated", "I've updated the objective '{title}' successfully. What would you like to do with this objective next?"},
                {"ObjectiveUpdatedWithDescription", "I've updated the objective '{title}' with the new description: '{description}'. What would you like to do with this objective next?"},
                {"ObjectiveDeleted", "I've successfully deleted the objective '{title}'. All associated resources have been cleaned up. Is there anything else you'd like to do?"},
                {"ObjectiveDetails", "Here are the details for objective '{title}' :\n\nDescription: {description}\nOKR Session: {okrSessionTitle}\nPeriod: {startDate} to {endDate}\nTeam: {teamName}\nStatus: {status}\nPriority: {priority}\nProgress: {progress}%\n\nWould you like to update this objective or add key results to it?"},
                {"ObjectiveSearchResults", "I found {count} objectives \nCan I assist you with anything else?"},
                {"ObjectiveSearchResultsEmpty", "I couldn't find any objectives matching your search criteria \"{searchTerm}\". Would you like to create a new objective or try a different search?"},
                {"ObjectivesBySessionResults", "I found {count} objectives for the OKR session '{okrSessionTitle}':\n\nCan I assist you with creating more objectives or managing these ones?"},
                {"ObjectivesBySessionResultsEmpty", "There are no objectives for the OKR session '{okrSessionTitle}'. Would you like to create one?"},
                
                
                // KeyResult templates
                {"KeyResultCreated", "I've successfully created the key result '{title}' running from {startedDate} to {endDate} for objective '{objectiveTitle}'. The key result has been added to the system. Would you like me to help you add tasks to this key result?"},
                {"KeyResultCreatedWithDescription", "I've successfully created the key result '{title}' with the description: '{description}'. The key result runs from {startedDate} to {endDate} for objective '{objectiveTitle}' and is ready for tasks to be added. Would you like me to help you add tasks to this key result?"},
                {"KeyResultUpdated", "I've updated the key result '{title}' . What would you like to do with this key result next?"},
                {"KeyResultUpdatedWithDescription", "I've updated the key result '{title}' with the new description: '{description}'. What would you like to do with this key result next?"},
                {"KeyResultDeleted", "I've successfully deleted the key result '{title}'. All associated resources have been cleaned up. Is there anything else you'd like to do?"},
                {"KeyResultDetails", "Here are the details for key result '{title}' :\n\nDescription: {description}\nObjective: {objectiveTitle}\nPeriod: {startedDate} to {endDate}\nOwner: {userName}\nStatus: {status}\nProgress: {progress}%\n\nWould you like to update this key result or add tasks to it?"},
                {"KeyResultSearchResults", "I found {count} key results \nCan I assist you with anything else?"},
                {"KeyResultSearchResultsEmpty", "I couldn't find any key results matching your search criteria \"{searchTerm}\". Would you like to create a new key result or try a different search?"},
                {"KeyResultsByObjectiveResults", "I found {count} key results for the objective '{objectiveTitle}'. \n\nCan I assist you with creating more key results or managing these ones?"},
                {"KeyResultsByObjectiveResultsEmpty", "There are no key results for the objective '{objectiveTitle}'. Would you like to create one?"},
                
                // KeyResultTask templates
                { "KeyResultTaskCreated", "I've successfully created the task '{taskTitle}' for key result '{keyResultTitle}'. Would you like me to add more tasks or update the progress of this one?" },
                { "KeyResultTaskCreatedWithDescription", "I've successfully created the task '{taskTitle}' with the description: '{description}' for key result '{keyResultTitle}'. Would you like me to add more tasks or update the progress of this one?" },
                { "KeyResultTaskUpdated", "I've updated the task '{taskTitle}' for key result '{keyResultTitle}'. What would you like to do next?" },
                { "KeyResultTaskDeleted", "I've successfully deleted the task '{taskTitle}' from key result '{keyResultTitle}'. Is there anything else you'd like to do?" },
                { "KeyResultTaskDetails", "Here are the details for task '{taskTitle}':\n\nKey Result: {keyResultTitle}\nProgress: {progress}%\nDue Date: {endDate}\nPriority: {priority}\n\nWould you like to update this task or view other tasks for this key result?" },
                { "KeyResultTaskDetailsWithDescription", "Here are the details for task '{taskTitle}':\n\nDescription: {description}\nKey Result: {keyResultTitle}\nProgress: {progress}%\nDue Date: {endDate}\nPriority: {priority}\n\nWould you like to update this task or view other tasks for this key result?" },
                { "KeyResultTaskSearchResults", "I found {count} \nCan I assist you with anything else?" },
                { "KeyResultTaskSearchResultsEmpty", "I couldn't find any tasks matching your search criteria \"{searchTerm}\". Would you like to create a new task or try a different search?" },
                { "KeyResultTasksByKeyResultResults", "I found {count} tasks for key result '{keyResultTitle}'. \n\nWould you like me to help you add more tasks or update these ones?" },
                { "KeyResultTasksByKeyResultEmpty", "There are no tasks for key result '{keyResultTitle}'. Would you like to create a task to help track progress on this key result?" },
                
                // user management templates
                { "UserSearchResults", "I found {count} users matching your search criteria \"{searchTerm}\". \n\nCan I assist you with anything else?" },
                { "UserSearchResultsEmpty", "I couldn't find any users matching your search criteria \"{searchTerm}\". Would you like to try a different search?" },
                { "TeamManagersResults", "I found {count} team managers in this organization. \n\nCan I assist you with anything else?" },
                { "TeamManagersResultsEmpty", "I couldn't find any team managers in the organization. Would you like to assign a team manager role to a user?" },
                { "UserInvitedSuccess", "I've sent an invitation to {email} to join the organization with the role of {role}. They'll receive an email with instructions on how to accept the invitation and set up their account." },
                { "UserInvitedSuccessWithTeam", "I've sent an invitation to {email} to join the organization with the role of {role} and added them to the specified team. They'll receive an email with instructions on how to accept the invitation and set up their account." },
                { "UserInvitedFailure", "I wasn't able to send the invitation to {email}. The system returned the following error: {errorMessage}. Please check the email address and try again, or contact support if the issue persists." },
                { "UsersListByOrganizationResults", "I found {count} users in the organization. \n\nCan I assist you with anything else?" },
                { "UsersListByOrganizationEmpty", "I couldn't find any users in the organization. You might want to invite some users to join the organization." },
                { "UserEnabledSuccess", "I've successfully enabled the user account for {firstName} {lastName} ({email}). They can now log in and access the system." },
                { "UserEnabledFailure", "I wasn't able to enable the user account. The system returned: {errorMessage}" },
                { "UserDisabledSuccess", "I've successfully disabled the user account for {firstName} {lastName} ({email}). They will no longer be able to log in to the system." },
                { "UserDisabledFailure", "I wasn't able to disable the user account. The system returned: {errorMessage}" },
                { "UserUpdatedSuccess", "I've updated the user profile for {firstName} {lastName}. The following information was updated: {updatedFields}." },
                { "UserUpdatedFailure", "I wasn't able to update the user profile. The system returned: {errorMessage}" },
                
                // Authorization templates
                { "UnauthorizedAccess", "I'm sorry, but you don't have permission to {action} {resourceType}s. Please contact your administrator if you need this access." },
                { "UnauthorizedAccessWithPermission", "I'm sorry, but you don't have the required permission ({permission}) to {action} {resourceType}s. Please contact your administrator if you need this access." },
                { "ForbiddenAction", "This action is not allowed. You need additional permissions to {action} {resourceType}s." },
            };
        }

        /// <summary>
        /// Gets a prompt template by key and replaces placeholders with provided values
        /// </summary>
        /// <param name="key">The template key</param>
        /// <param name="values">Dictionary of placeholder values to substitute</param>
        /// <returns>The processed prompt with placeholders replaced</returns>
        public string GetPrompt(string key, Dictionary<string, string> values)
        {
            if (!_templates.ContainsKey(key))
                throw new KeyNotFoundException($"Prompt template key '{key}' not found.");

            string template = _templates[key];

            foreach (var pair in values)
            {
                template = template.Replace($"{{{pair.Key}}}", pair.Value);
            }

            return template;
        }

        /// <summary>
        /// Adds a new template or updates an existing one
        /// </summary>
        /// <param name="key">Template key</param>
        /// <param name="template">Template content</param>
        public void AddOrUpdateTemplate(string key, string template)
        {
            _templates[key] = template;
        }

        /// <summary>
        /// Checks if a template exists
        /// </summary>
        /// <param name="key">Template key to check</param>
        /// <returns>True if the template exists</returns>
        public bool HasTemplate(string key)
        {
            return _templates.ContainsKey(key);
        }

        /// <summary>
        /// Gets all template keys
        /// </summary>
        /// <returns>List of all template keys</returns>
        public IEnumerable<string> GetAllTemplateKeys()
        {
            return _templates.Keys;
        }
    }
}