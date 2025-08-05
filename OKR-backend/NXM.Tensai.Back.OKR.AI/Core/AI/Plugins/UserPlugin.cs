using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using NXM.Tensai.Back.OKR.AI.Core.AI;
using NXM.Tensai.Back.OKR.AI.Models;
using NXM.Tensai.Back.OKR.AI.Services;
using NXM.Tensai.Back.OKR.AI.Services.MediatRService;
using NXM.Tensai.Back.OKR.Application;
using NXM.Tensai.Back.OKR.AI.Services.Authorization;
using NXM.Tensai.Back.OKR.Domain;

namespace NXM.Tensai.Back.OKR.AI.Core.AI.Plugins
{
    public class UserPlugin
    {
        private readonly PromptTemplateService _promptTemplateService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<UserPlugin> _logger;
        private readonly UserContextAccessor _userContextAccessor;

        public UserPlugin(
            PromptTemplateService promptTemplateService,
            IServiceProvider serviceProvider,
            ILogger<UserPlugin> logger,
            IConfiguration configuration,
            UserContextAccessor userContextAccessor)
        {
            _promptTemplateService = promptTemplateService;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _userContextAccessor = userContextAccessor;
        }

        /// <summary>
        /// Search for users by name
        /// </summary>
        [KernelFunction]
        [Description("Search for users by name")]
        public async Task<UserSearchResponse> SearchUsersByNameAsync(
            [Description("Name to search for in users' first or last name")] string query = null)
        {
            try
            {
                // Get user context directly from the accessor
                var userContext = _userContextAccessor.CurrentUserContext;

                // Create a scope to resolve the scoped service
                using var scope = _serviceProvider.CreateScope();
                var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
                
                var unauthorizedResponse = await permissionService.CheckPermissionAsync<UserSearchResponse>(
                    Permissions.Users_GetAll,
                    "user", 
                    "search",
                    (message) => new UserSearchResponse 
                    { 
                        PromptTemplate = message,
                        SearchTerm = query
                    });
                
                // If unauthorized, return the response
                if (unauthorizedResponse != null)
                {
                    _logger.LogWarning("Permission denied for user {UserId} to search users. Message: {Message}", 
                        userContext?.UserId, unauthorizedResponse.PromptTemplate);
                    return unauthorizedResponse;
                }
                
                // Use the MediatR service to search for users
                var userMediatRService = scope.ServiceProvider.GetRequiredService<UserMediatRService>();
                var result = await userMediatRService.SearchUsersByNameAsync(query, userContext.OrganizationId);
                
                // Generate the users list for the prompt template
                var usersListBuilder = new StringBuilder();
                
                for (int i = 0; i < result.Users.Count; i++)
                {
                    var user = result.Users[i];
                    usersListBuilder.AppendLine($"{i+1}. {user.FullName} ({user.Position})");
                    usersListBuilder.AppendLine($"   Email: {user.Email}");
                    usersListBuilder.AppendLine($"   Status: {(user.IsEnabled ? "Enabled" : "Disabled")}");
                    usersListBuilder.AppendLine($"   Date Of Birth: {(string.IsNullOrEmpty(user.DateOfBirth) ? "N/A" : user.DateOfBirth)}");

                    
                    // Add a separator line between users
                    if (i < result.Users.Count - 1)
                    {
                        usersListBuilder.AppendLine();
                    }
                }
                
                // Generate a prompt template
                Dictionary<string, string> templateValues = new()
                {
                    { "count", result.Count.ToString() },
                    { "searchTerm", !string.IsNullOrEmpty(query) ? query : "(any)" },
                    { "usersList", usersListBuilder.ToString().Trim() }
                };
                
                // Use different template for empty results
                if (result.Users.Count == 0)
                {
                    result.PromptTemplate = _promptTemplateService.GetPrompt("UserSearchResultsEmpty", templateValues);
                }
                else
                {
                    result.PromptTemplate = _promptTemplateService.GetPrompt("UserSearchResults", templateValues);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for users with name: {Query}", query);
                throw;
            }
        }

        /// <summary>
        /// Get all team managers in an organization
        /// </summary>
        [KernelFunction]
        [Description("Get all team managers in an organization")]
        public async Task<TeamManagersResponse> GetTeamManagersByOrganizationIdAsync()
        {
            try
            {
                // Get user context directly from the accessor
                var userContext = _userContextAccessor.CurrentUserContext;

                // Create a scope to resolve the scoped service
                using var scope = _serviceProvider.CreateScope();
                var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
                
                var unauthorizedResponse = await permissionService.CheckPermissionAsync<TeamManagersResponse>(
                    Permissions.Users_GetTeamManagersByOrganizationId,
                    "team managers", 
                    "view",
                    (message) => new TeamManagersResponse 
                    { 
                        PromptTemplate = message,
                        OrganizationId = userContext.OrganizationId
                    });
                
                // If unauthorized, return the response
                if (unauthorizedResponse != null)
                {
                    _logger.LogWarning("Permission denied for user {UserId} to view team managers. Message: {Message}", 
                        userContext?.UserId, unauthorizedResponse.PromptTemplate);
                    return unauthorizedResponse;
                }

                // Check for required organizationId
                if (string.IsNullOrEmpty(userContext.OrganizationId))
                {
                    throw new ArgumentNullException(nameof(userContext.OrganizationId), "Organization ID is required to get team managers.");
                }

                // Create a scope to resolve the scoped service
                var userMediatRService = scope.ServiceProvider.GetRequiredService<UserMediatRService>();

                // Get team managers for the organization
                var result = await userMediatRService.GetTeamManagersByOrganizationIdAsync(userContext.OrganizationId);
                
                // Generate the managers list for the prompt template
                var managersListBuilder = new StringBuilder();
                
                for (int i = 0; i < result.Managers.Count; i++)
                {
                    var manager = result.Managers[i];
                    managersListBuilder.AppendLine($"{i+1}. {manager.FullName} ({manager.Position})");
                    managersListBuilder.AppendLine($"   Email: {manager.Email}");
                    
                    // Add a separator line between managers
                    if (i < result.Managers.Count - 1)
                    {
                        managersListBuilder.AppendLine();
                    }
                }
               

                // Generate the prompt template
                Dictionary<string, string> templateValues = new()
                {
                    { "count", result.Count.ToString() },
                    { "managersList", managersListBuilder.ToString().Trim() }
                };
                
                // Use different template for empty results
                if (result.Managers.Count == 0)
                {
                    result.PromptTemplate = _promptTemplateService.GetPrompt("TeamManagersResultsEmpty", templateValues);
                }
                else
                {
                    result.PromptTemplate = _promptTemplateService.GetPrompt("TeamManagersResults", templateValues);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving team managers by organization ID");
                throw;
            }
        }

        /// <summary>
        /// Invite a user by email
        /// </summary>
        [KernelFunction]
        [Description("Invite a user by email to join the organization")]
        public async Task<UserInviteResponse> InviteUserByEmailAsync(
            [Description("Email address of the user to invite")] string email,
            [Description("Role to assign to the user (e.g., Collaborator, TeamManager)")] string role = "Collaborator",
            [Description("Optional ID of the team to add the user to")] string teamId = null)
        {
            try
            {
                // Get user context directly from the accessor
                var userContext = _userContextAccessor.CurrentUserContext;

                // Create a scope to resolve the scoped service
                using var scope = _serviceProvider.CreateScope();
                var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
                
                // Check create permission
                _logger.LogDebug("Checking permission {Permission}", Permissions.Users_Invite);
                
                var unauthorizedResponse = await permissionService.CheckPermissionAsync<UserInviteResponse>(
                    Permissions.Users_Invite,
                    "user", 
                    "invite",
                    (message) => new UserInviteResponse 
                    { 
                        PromptTemplate = message,
                        Email = email,
                        Role = role,
                        OrganizationId = userContext.OrganizationId,
                        TeamId = teamId,
                        Success = false,
                        Message = "Unauthorized"
                    });
                
                // If unauthorized, return the response
                if (unauthorizedResponse != null)
                {
                    _logger.LogWarning("Permission denied for user {UserId} to invite users. Message: {Message}", 
                        userContext?.UserId, unauthorizedResponse.PromptTemplate);
                    return unauthorizedResponse;
                }
                
                // Check for required organizationId
                if (string.IsNullOrEmpty(userContext.OrganizationId))
                {
                    throw new ArgumentNullException(nameof(userContext.OrganizationId), "Organization ID is required to invite a user.");
                }

                // Create a scope to resolve the scoped service
                var userMediatRService = scope.ServiceProvider.GetRequiredService<UserMediatRService>();

                // Create the request
                var request = new UserInviteRequest
                {
                    Email = email,
                    Role = role,
                    OrganizationId = userContext.OrganizationId,
                    TeamId = teamId
                };
                
                // Send the invitation
                var result = await userMediatRService.InviteUserByEmailAsync(request);
                
                // Generate prompt template based on success or failure
                Dictionary<string, string> templateValues = new()
                {
                    { "email", email },
                    { "role", role },
                };
                
                if (!string.IsNullOrEmpty(teamId))
                {
                    templateValues["teamId"] = teamId;
                }
                
                if (result.Success)
                {
                    result.PromptTemplate = _promptTemplateService.GetPrompt("UserInvitedSuccess", templateValues);
                }
                else
                {
                    templateValues["errorMessage"] = result.Message;
                    result.PromptTemplate = _promptTemplateService.GetPrompt("UserInvitedFailure", templateValues);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inviting user {Email}", email);
                
                var errorResponse = new UserInviteResponse
                {
                    Success = false,
                    Email = email,
                    Role = role,
                    TeamId = teamId,
                    Message = $"Error: {ex.Message}"
                };
                
                // Add error template
                Dictionary<string, string> errorTemplateValues = new()
                {
                    { "email", email },
                    { "role", role },
                    { "errorMessage", ex.Message }
                };
                
                errorResponse.PromptTemplate = _promptTemplateService.GetPrompt("UserInvitedFailure", errorTemplateValues);
                
                return errorResponse;
            }
        }

        /// <summary>
        /// Get all users in an organization
        /// </summary>
        [KernelFunction]
        [Description("Get all users belonging to a specific organization")]
        public async Task<UsersListResponse> GetUsersByOrganizationIdAsync()
        {
            try
            {
                // Get user context directly from the accessor
                var userContext = _userContextAccessor.CurrentUserContext;

                // Create a scope to resolve the scoped service
                using var scope = _serviceProvider.CreateScope();
                var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
                
                
                var unauthorizedResponse = await permissionService.CheckPermissionAsync<UsersListResponse>(
                    Permissions.Users_GetByOrganizationId,
                    "user", 
                    "view all",
                    (message) => new UsersListResponse 
                    { 
                        PromptTemplate = message,
                        OrganizationId = userContext.OrganizationId
                    });
                
                // If unauthorized, return the response
                if (unauthorizedResponse != null)
                {
                    _logger.LogWarning("Permission denied for user {UserId} to view all users. Message: {Message}", 
                        userContext?.UserId, unauthorizedResponse.PromptTemplate);
                    return unauthorizedResponse;
                }
            
                // Check for required organizationId
                if (string.IsNullOrEmpty(userContext.OrganizationId))
                {
                    throw new ArgumentNullException(nameof(userContext.OrganizationId), "Organization ID is required to get users.");
                }

                // Create a scope to resolve the scoped service
                var userMediatRService = scope.ServiceProvider.GetRequiredService<UserMediatRService>();

                // Get users for the organization
                var result = await userMediatRService.GetUsersByOrganizationIdAsync(userContext.OrganizationId);
                
                // Generate the users list for the prompt template
                var usersListBuilder = new StringBuilder();
                
                for (int i = 0; i < result.Users.Count; i++)
                {
                    var user = result.Users[i];
                    usersListBuilder.AppendLine($"{i+1}. {user.FullName} ({user.Position})");
                    usersListBuilder.AppendLine($"   Email: {user.Email}");
                    usersListBuilder.AppendLine($"   Status: {(user.IsEnabled ? "Enabled" : "Disabled")}"); 
                    usersListBuilder.AppendLine($"   Role: {user.Role}");
                    
                    // Add a separator line between users
                    if (i < result.Users.Count - 1)
                    {
                        usersListBuilder.AppendLine();
                    }
                }
               

                // Generate the prompt template
                Dictionary<string, string> templateValues = new()
                {
                    { "count", result.Count.ToString() },
                    { "usersList", usersListBuilder.ToString().Trim() }
                };
                
                // Use different template for empty results
                if (result.Users.Count == 0)
                {
                    result.PromptTemplate = "I couldn't find any users in the organization. You might want to invite some users to join the organization.";
                }
                else
                {
                    result.PromptTemplate = $"I found {result.Users.Count} users in the organization:\n\n{usersListBuilder}\n\nCan I assist you with anything else?";
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users by organization ID");
                throw;
            }
        }
        
        /// <summary>
        /// Enable a user account
        /// </summary>
        [KernelFunction]
        [Description("Enable a user account that has been disabled")]
        public async Task<UserActionResponse> EnableUserAsync(
            [Description("(optional) The ID of the user to enable")] string userId = null,
            [Description("The name of the user to enable if ID is not available")] string userName = null)
        {
            try
            {
                // Get user context directly from the accessor
                var userContext = _userContextAccessor.CurrentUserContext;

                // Create a scope to resolve the scoped service
                using var scope = _serviceProvider.CreateScope();
                var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
                
                var unauthorizedResponse = await permissionService.CheckPermissionAsync<UserActionResponse>(
                    Permissions.Users_Update,
                    "user account", 
                    "enable",
                    (message) => new UserActionResponse 
                    { 
                        PromptTemplate = message,
                        UserId = userId,
                        Action = "enable",
                        Success = false,
                        Message = "Unauthorized"
                    });
                
                // If unauthorized, return the response
                if (unauthorizedResponse != null)
                {
                    _logger.LogWarning("Permission denied for user {UserId} to enable user accounts. Message: {Message}", 
                        userContext?.UserId, unauthorizedResponse.PromptTemplate);
                    return unauthorizedResponse;
                }
                
                // If userId is not provided but userName is, try to search for the user by name
                if (string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(userName))
                {
                    _logger.LogInformation("No user ID provided, attempting to find user by name: {UserName}", userName);
                    
                    try
                    {
                        // Search for users with this name
                        var searchResults = await SearchUsersByNameAsync(userName);
                        
                        // If exactly one match is found, use that user's ID
                        if (searchResults.Users.Count == 1)
                        {
                            userId = searchResults.Users[0].UserId;
                            _logger.LogInformation("Found user ID {UserId} for user named '{UserName}'", userId, userName);
                        }
                        // If multiple matches are found, we need more specifics
                        else if (searchResults.Users.Count > 1)
                        {
                            throw new ArgumentException($"Found {searchResults.Users.Count} users named '{userName}'. Please specify which one using the user ID.");
                        }
                        else
                        {
                            throw new ArgumentException($"No user found with name '{userName}'.");
                        }
                    }
                    catch (Exception ex) when (!(ex is ArgumentException))
                    {
                        _logger.LogWarning(ex, "Error searching for user with name '{UserName}'", userName);
                        throw new ApplicationException($"Could not find user by name '{userName}': {ex.Message}");
                    }
                }
                
                // At this point we must have a userId
                if (string.IsNullOrEmpty(userId))
                {
                    throw new ArgumentNullException(nameof(userId), "User ID is required to enable a user.");
                }

                // Create a scope to resolve the scoped service
                var userMediatRService = scope.ServiceProvider.GetRequiredService<UserMediatRService>();

                // Enable the user
                var result = await userMediatRService.EnableUserAsync(userId);
                
                // Generate prompt template based on success or failure
                if (result.Success)
                {
                    result.PromptTemplate = $"I've successfully enabled the user account for {result.FirstName} {result.LastName} ({result.Email}). They can now log in and access the system.";
                }
                else
                {
                    result.PromptTemplate = $"I wasn't able to enable the user account. The system returned: {result.Message}";
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enabling user with ID: {UserId} or name: {UserName}", userId, userName);
                
                var errorResponse = new UserActionResponse
                {
                    UserId = userId,
                    Action = "enable",
                    Success = false,
                    Message = $"Error: {ex.Message}",
                    PromptTemplate = $"There was a problem enabling the user account: {ex.Message}. Please check if the user information is correct and try again."
                };
                
                return errorResponse;
            }
        }
        
        /// <summary>
        /// Disable a user account
        /// </summary>
        [KernelFunction]
        [Description("Disable a user account to prevent them from accessing the system")]
        public async Task<UserActionResponse> DisableUserAsync(
            [Description("(optional) The ID of the user to disable")] string userId = null,
            [Description("The name of the user to disable if ID is not available")] string userName = null)
        {
            try
            {
                // Get user context directly from the accessor
                var userContext = _userContextAccessor.CurrentUserContext;

                // Create a scope to resolve the scoped service
                using var scope = _serviceProvider.CreateScope();
                var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
                
                var unauthorizedResponse = await permissionService.CheckPermissionAsync<UserActionResponse>(
                    Permissions.Users_Update,
                    "user account", 
                    "disable",
                    (message) => new UserActionResponse 
                    { 
                        PromptTemplate = message,
                        UserId = userId,
                        Action = "disable",
                        Success = false,
                        Message = "Unauthorized"
                    });
                
                // If unauthorized, return the response
                if (unauthorizedResponse != null)
                {
                    _logger.LogWarning("Permission denied for user {UserId} to disable user accounts. Message: {Message}", 
                        userContext?.UserId, unauthorizedResponse.PromptTemplate);
                    return unauthorizedResponse;
                }
                
                // If userId is not provided but userName is, try to search for the user by name
                if (string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(userName))
                {
                    _logger.LogInformation("No user ID provided, attempting to find user by name: {UserName}", userName);
                    
                    try
                    {
                        // Search for users with this name
                        var searchResults = await SearchUsersByNameAsync(userName);
                        
                        // If exactly one match is found, use that user's ID
                        if (searchResults.Users.Count == 1)
                        {
                            userId = searchResults.Users[0].UserId;
                            _logger.LogInformation("Found user ID {UserId} for user named '{UserName}'", userId, userName);
                        }
                        // If multiple matches are found, we need more specifics
                        else if (searchResults.Users.Count > 1)
                        {
                            throw new ArgumentException($"Found {searchResults.Users.Count} users named '{userName}'. Please specify which one using the user ID.");
                        }
                        else
                        {
                            throw new ArgumentException($"No user found with name '{userName}'.");
                        }
                    }
                    catch (Exception ex) when (!(ex is ArgumentException))
                    {
                        _logger.LogWarning(ex, "Error searching for user with name '{UserName}'", userName);
                        throw new ApplicationException($"Could not find user by name '{userName}': {ex.Message}");
                    }
                }
                
                // At this point we must have a userId
                if (string.IsNullOrEmpty(userId))
                {
                    throw new ArgumentNullException(nameof(userId), "User ID is required to disable a user.");
                }

                // Create a scope to resolve the scoped service
                var userMediatRService = scope.ServiceProvider.GetRequiredService<UserMediatRService>();

                // Disable the user
                var result = await userMediatRService.DisableUserAsync(userId);
                
                // Generate prompt template based on success or failure
                if (result.Success)
                {
                    result.PromptTemplate = $"I've successfully disabled the user account for {result.FirstName} {result.LastName} ({result.Email}). They will no longer be able to log in to the system.";
                }
                else
                {
                    result.PromptTemplate = $"I wasn't able to disable the user account. The system returned: {result.Message}";
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling user with ID: {UserId} or name: {UserName}", userId, userName);
                
                var errorResponse = new UserActionResponse
                {
                    UserId = userId,
                    Action = "disable",
                    Success = false,
                    Message = $"Error: {ex.Message}",
                    PromptTemplate = $"There was a problem disabling the user account: {ex.Message}. Please check if the user information is correct and try again."
                };
                
                return errorResponse;
            }
        }
        
        /// <summary>
        /// Update a user's profile information
        /// </summary>
        [KernelFunction]
        [Description("Update a user's profile information")]
        public async Task<UserUpdateResponse> UpdateUserAsync(
            [Description("(optional) The ID of the user to update, if not provided , don't ask for it")] string userId = null,
            [Description("The name of the user to update if ID is not available")] string userName = null,
            [Description("New first name for the user")] string firstName = null,
            [Description("New last name for the user")] string lastName = null,
            [Description("New email address for the user")] string email = null,
            [Description("New address for the user")] string address = null,
            [Description("New position/job title for the user")] string position = null,
            [Description("New profile picture URL for the user")] string profilePictureUrl = null,
            [Description("Gender of the user (Male, Female, NotSpecified)")] string gender = null,
            [Description("Whether the user should receive notifications (true/false)")] bool? isNotificationEnabled = null,
            [Description("Whether the user account is enabled (true/false)")] bool? isEnabled = null)
        {
            try
            {
                // Get user context directly from the accessor
                var userContext = _userContextAccessor.CurrentUserContext;

                // Create a scope to resolve the scoped service
                using var scope = _serviceProvider.CreateScope();
                var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
                
                var unauthorizedResponse = await permissionService.CheckPermissionAsync<UserUpdateResponse>(
                    Permissions.Users_Update,
                    "user profile", 
                    "update",
                    (message) => new UserUpdateResponse 
                    { 
                        PromptTemplate = message,
                        UserId = userId,
                        Success = false,
                        Message = "Unauthorized"
                    });
                
                // If unauthorized, return the response
                if (unauthorizedResponse != null)
                {
                    _logger.LogWarning("Permission denied for user {UserId} to update user profiles. Message: {Message}", 
                        userContext?.UserId, unauthorizedResponse.PromptTemplate);
                    return unauthorizedResponse;
                }
                
                // If userId is not provided but userName is, try to search for the user by name
                if (string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(userName))
                {
                    _logger.LogInformation("No user ID provided, attempting to find user by name: {UserName}", userName);
                    
                    try
                    {
                        // Search for users with this name
                        var searchResults = await SearchUsersByNameAsync(userName);
                        
                        // If exactly one match is found, use that user's ID
                        if (searchResults.Users.Count == 1)
                        {
                            userId = searchResults.Users[0].UserId;
                            _logger.LogInformation("Found user ID {UserId} for user named '{UserName}'", userId, userName);
                        }
                        // If multiple matches are found, we need more specifics
                        else if (searchResults.Users.Count > 1)
                        {
                            throw new ArgumentException($"Found {searchResults.Users.Count} users named '{userName}'. Please specify which one using the user ID.");
                        }
                        else
                        {
                            throw new ArgumentException($"No user found with name '{userName}'.");
                        }
                    }
                    catch (Exception ex) when (!(ex is ArgumentException))
                    {
                        _logger.LogWarning(ex, "Error searching for user with name '{UserName}'", userName);
                        throw new ApplicationException($"Could not find user by name '{userName}': {ex.Message}");
                    }
                }
                
                // At this point we must have a userId
                if (string.IsNullOrEmpty(userId))
                {
                    throw new ArgumentNullException(nameof(userId), "User ID is required to update a user.");
                }

                // Create a scope to resolve the scoped service
                var userMediatRService = scope.ServiceProvider.GetRequiredService<UserMediatRService>();
                
                // First get current user data to preserve existing values for fields not being updated
                _logger.LogInformation("Retrieving current user details for userId: {UserId}", userId);
                
                // Call userMediatRService.GetUserByIdAsync directly instead of using helper method
                var currentUser = await userMediatRService.GetUserByIdAsync(userId);
                
                if (currentUser == null)
                {
                    throw new ArgumentException($"User with ID '{userId}' not found.");
                }
                
                _logger.LogInformation("Successfully retrieved current user data for {UserId}", userId);
                
                // Create the update request preserving existing values for null fields
                var request = new UserUpdateRequest
                {
                    // Only update fields that were explicitly provided (non-null)
                    FirstName = firstName ?? currentUser.FirstName,
                    LastName = lastName ?? currentUser.LastName,
                    Email = email ?? currentUser.Email,
                    Address = address ?? currentUser.Address,
                    Position = position ?? currentUser.Position,
                    OrganizationId = currentUser.OrganizationId,
                    ProfilePictureUrl = profilePictureUrl ?? currentUser.ProfilePictureUrl,
                    IsNotificationEnabled = isNotificationEnabled ?? currentUser.IsNotificationEnabled,
                    IsEnabled = isEnabled ?? currentUser.IsEnabled,
                    // IMPORTANT: Always keep the current gender value regardless of what was passed in
                    Gender = currentUser.Gender
                };
                
                // Log fields that are being updated
                var updatedFields = new List<string>();
                if (firstName != null) updatedFields.Add("first name");
                if (lastName != null) updatedFields.Add("last name");
                if (email != null) updatedFields.Add("email");
                if (address != null) updatedFields.Add("address");
                if (position != null) updatedFields.Add("position");
                if (profilePictureUrl != null) updatedFields.Add("profile picture");
                if (isNotificationEnabled.HasValue) updatedFields.Add("notification settings");
                if (isEnabled.HasValue) updatedFields.Add("account status");
                
                _logger.LogInformation("Updating the following fields for user {UserId}: {Fields}", 
                    userId, string.Join(", ", updatedFields));

                // Update the user
                var result = await userMediatRService.UpdateUserAsync(userId, request);
                
                // Generate prompt template based on success or failure
                if (result.Success)
                {
                    string updatedFieldsText = string.Join(", ", updatedFields);
                    result.PromptTemplate = $"I've updated the user profile for {result.FirstName} {result.LastName}. The following information was updated: {updatedFieldsText}.";
                }
                else
                {
                    result.PromptTemplate = $"I wasn't able to update the user profile. The system returned: {result.Message}";
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user with ID: {UserId} or name: {UserName}", userId, userName);
                
                var errorResponse = new UserUpdateResponse
                {
                    UserId = userId,
                    Success = false,
                    Message = $"Error: {ex.Message}",
                    PromptTemplate = $"There was a problem updating the user profile: {ex.Message}. Please check the provided information and try again."
                };
                
                return errorResponse;
            }
        }
    }
}
