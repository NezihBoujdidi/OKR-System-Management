using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using NXM.Tensai.Back.OKR.AI.Models;
using NXM.Tensai.Back.OKR.Application;
using NXM.Tensai.Back.OKR.Domain; // Add this for RoleType

namespace NXM.Tensai.Back.OKR.AI.Services.MediatRService
{
    public class UserMediatRService
    {
        private readonly IMediator _mediator;
        private readonly ILogger<UserMediatRService> _logger;

        public UserMediatRService(IMediator mediator, ILogger<UserMediatRService> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Search for users by name
        /// </summary>
        public async Task<UserSearchResponse> SearchUsersByNameAsync(string query = null, string organizationId = null)
        {
            try
            {
                _logger.LogInformation("Searching for users with name: {Name}, organization: {OrganizationId}", 
                    query, organizationId);
                
                var searchQuery = new SearchUserByNameQuery
                {
                    Query = query,
                    OrganizationId = !string.IsNullOrEmpty(organizationId) ? Guid.Parse(organizationId) : null,
                    Page = 1,
                    PageSize = 20
                };
                
                var result = await _mediator.Send(searchQuery);
                
                // Map the results to the expected response format
                var response = new UserSearchResponse
                {
                    SearchTerm = query,
                    OrganizationId = organizationId,
                    Users = new List<UserDetailsResponse>()
                };
                
                foreach (var user in result.Items)
                {
                    response.Users.Add(new UserDetailsResponse
                    {
                        UserId = user.Id.ToString(),
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Email = user.Email,
                        Position = user.Position,
                        ProfilePictureUrl = user.ProfilePictureUrl
                    });
                }
                
                _logger.LogInformation("Found {Count} users matching criteria", response.Users.Count);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users: {ErrorMessage}", ex.Message);
                throw new ApplicationException("Failed to search users", ex);
            }
        }

        /// <summary>
        /// Get team managers by organization ID
        /// </summary>
        public async Task<TeamManagersResponse> GetTeamManagersByOrganizationIdAsync(string organizationId)
        {
            try
            {
                _logger.LogInformation("Getting team managers for organization with ID: {OrganizationId}", organizationId);
                
                // Parse the organizationId to Guid
                var id = Guid.Parse(organizationId);
                
                // Create and send the query
                var query = new GetTeamManagersByOrganizationIdQuery { OrganizationId = id };
                var managers = await _mediator.Send(query);
                
                // Map the results to the expected response format
                var result = new TeamManagersResponse
                {
                    OrganizationId = organizationId,
                    Managers = new List<UserDetailsResponse>()
                };
                
                foreach (var manager in managers)
                {
                    result.Managers.Add(new UserDetailsResponse
                    {
                        UserId = manager.Id.ToString(),
                        FirstName = manager.FirstName,
                        LastName = manager.LastName,
                        Email = manager.Email,
                        Position = manager.Position,
                        ProfilePictureUrl = manager.ProfilePictureUrl
                    });
                }
                
                _logger.LogInformation("Found {Count} team managers for organization {OrganizationId}", result.Managers.Count, organizationId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting team managers by organization ID: {ErrorMessage}", ex.Message);
                throw new ApplicationException($"Failed to get team managers for organization: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Invite a user by email
        /// </summary>
        public async Task<UserInviteResponse> InviteUserByEmailAsync(UserInviteRequest request)
        {
            try
            {
                _logger.LogInformation("Inviting user with email: {Email}, role: {Role}, to organization: {OrganizationId}", 
                    request.Email, request.Role, request.OrganizationId);
                
                // Create the command with properties that can be initialized
                var command = new InviteUserCommand
                {
                    Email = request.Email,
                    Role = Enum.Parse<RoleType>(request.Role, true),
                    OrganizationId = Guid.Parse(request.OrganizationId)
                };
                
                // Add team ID if provided - using a separate variable since TeamId is init-only
                Guid? teamId = null;
                if (!string.IsNullOrEmpty(request.TeamId))
                {
                    teamId = Guid.Parse(request.TeamId);
                }
                
                // Create a new command with all properties including teamId
                var finalCommand = new InviteUserCommand
                {
                    Email = request.Email,
                    Role = Enum.Parse<RoleType>(request.Role, true),
                    OrganizationId = Guid.Parse(request.OrganizationId),
                    TeamId = teamId
                };
                
                var result = await _mediator.Send(finalCommand);
                
                return new UserInviteResponse
                {
                    Success = result.Success,
                    Message = result.Message,
                    Email = result.Email,
                    Role = result.Role,
                    OrganizationId = request.OrganizationId,
                    TeamId = request.TeamId,
                    InviteId = result.InviteId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inviting user: {ErrorMessage}", ex.Message);
                return new UserInviteResponse 
                {
                    Success = false,
                    Message = $"Failed to invite user: {ex.Message}",
                    Email = request.Email,
                    Role = request.Role,
                    OrganizationId = request.OrganizationId,
                    TeamId = request.TeamId
                };
            }
        }

        /// <summary>
        /// Get all users belonging to a specific organization
        /// </summary>
        public async Task<UsersListResponse> GetUsersByOrganizationIdAsync(string organizationId)
        {
            try
            {
                _logger.LogInformation("Getting users for organization with ID: {OrganizationId}", organizationId);
                
                if (string.IsNullOrEmpty(organizationId))
                {
                    throw new ArgumentNullException(nameof(organizationId), "Organization ID is required");
                }
                
                // Parse the organizationId to Guid
                var id = Guid.Parse(organizationId);
                
                // Create and send the query
                var query = new GetUsersByOrganizationIdQuery { OrganizationId = id };
                var users = await _mediator.Send(query);
                
                // Map the results to the expected response format
                var result = new UsersListResponse
                {
                    OrganizationId = organizationId,
                    Users = new List<UserDetailsResponse>()
                };
                
                foreach (var user in users)
                {
                    result.Users.Add(new UserDetailsResponse
                    {
                        UserId = user.Id.ToString(),
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Email = user.Email,
                        Position = user.Position,
                        ProfilePictureUrl = user.ProfilePictureUrl,
                        IsEnabled = user.IsEnabled,
                        Role = user.Role
                    });
                }
                
                _logger.LogInformation("Found {Count} users for organization {OrganizationId}", result.Users.Count, organizationId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users by organization ID: {OrganizationId}", organizationId);
                throw new ApplicationException($"Failed to get users for organization: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Enable a user account
        /// </summary>
        public async Task<UserActionResponse> EnableUserAsync(string userId)
        {
            try
            {
                _logger.LogInformation("Enabling user with ID: {UserId}", userId);
                
                if (string.IsNullOrEmpty(userId))
                {
                    throw new ArgumentNullException(nameof(userId), "User ID is required");
                }
                
                // Parse the userId to Guid
                var id = Guid.Parse(userId);
                
                // Create and send the command
                var command = new EnableUserByIdCommand(id);
                var user = await _mediator.Send(command);
                
                // Create the response
                var response = new UserActionResponse
                {
                    UserId = user.Id.ToString(),
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    IsEnabled = true,
                    Action = "enabled",
                    Success = true,
                    Message = $"User {user.FirstName} {user.LastName} has been enabled successfully."
                };
                
                _logger.LogInformation("User {UserId} ({UserName}) enabled successfully", userId, $"{user.FirstName} {user.LastName}");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enabling user with ID: {UserId}", userId);
                return new UserActionResponse
                {
                    UserId = userId,
                    Action = "enable",
                    Success = false,
                    Message = $"Failed to enable user: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Disable a user account
        /// </summary>
        public async Task<UserActionResponse> DisableUserAsync(string userId)
        {
            try
            {
                _logger.LogInformation("Disabling user with ID: {UserId}", userId);
                
                if (string.IsNullOrEmpty(userId))
                {
                    throw new ArgumentNullException(nameof(userId), "User ID is required");
                }
                
                // Parse the userId to Guid
                var id = Guid.Parse(userId);
                
                // Create and send the command
                var command = new DisableUserByIdCommand(id);
                var user = await _mediator.Send(command);
                
                // Create the response
                var response = new UserActionResponse
                {
                    UserId = user.Id.ToString(),
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    IsEnabled = false,
                    Action = "disabled",
                    Success = true,
                    Message = $"User {user.FirstName} {user.LastName} has been disabled successfully."
                };
                
                _logger.LogInformation("User {UserId} ({UserName}) disabled successfully", userId, $"{user.FirstName} {user.LastName}");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling user with ID: {UserId}", userId);
                return new UserActionResponse
                {
                    UserId = userId,
                    Action = "disable",
                    Success = false,
                    Message = $"Failed to disable user: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        public async Task<UserDetailsResponse> GetUserByIdAsync(string userId)
        {
            try
            {
                _logger.LogInformation("Getting user with ID: {UserId}", userId);
                
                if (string.IsNullOrEmpty(userId))
                {
                    throw new ArgumentNullException(nameof(userId), "User ID is required");
                }
                
                // Parse the userId to Guid
                var id = Guid.Parse(userId);
                
                // Create and send the query
                var query = new GetUserByIdQuery { Id = id };
                var user = await _mediator.Send(query);
                
                if (user == null)
                {
                    throw new ApplicationException($"User with ID '{userId}' not found.");
                }
                
                // Add comprehensive null checks to all properties
                // Map the result to the expected response format
                var response = new UserDetailsResponse
                {
                    UserId = user.Id.ToString(),
                    FirstName = user.FirstName ?? string.Empty,
                    LastName = user.LastName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    Position = user.Position ?? string.Empty,
                    ProfilePictureUrl = user.ProfilePictureUrl ?? string.Empty,
                    Address = user.Address ?? string.Empty,
                    IsEnabled = user.IsEnabled
                };
                
                // Handle Role property with explicit null check
                if (user.Role != null)
                {
                    response.Role = user.Role.ToString();
                }
                else
                {
                    response.Role = "User"; // Default value
                }
                
                // Handle Gender property with explicit null check
                if (user.Gender != null)
                {
                    response.Gender = user.Gender.ToString();
                }
                else
                {
                    response.Gender = "NotSpecified"; // Default value
                }
                
                // Handle OrganizationId with explicit null check
                response.OrganizationId = user.OrganizationId?.ToString();
                
                // Handle IsNotificationEnabled with default if null
                response.IsNotificationEnabled = user.IsNotificationEnabled;
                
                _logger.LogInformation("Successfully retrieved user {UserId}", userId);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID: {UserId}", userId);
                throw new ApplicationException($"Failed to get user: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Update a user's profile information
        /// </summary>
        public async Task<UserUpdateResponse> UpdateUserAsync(string userId, UserUpdateRequest request)
        {
            try
            {
                _logger.LogInformation("Updating user with ID: {UserId}", userId);
                
                if (string.IsNullOrEmpty(userId))
                {
                    throw new ArgumentNullException(nameof(userId), "User ID is required");
                }
                
                // Parse the userId to Guid
                var id = Guid.Parse(userId);
                
                // Create the update command with the provided fields
                var command = new UpdateUserCommand
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    Address = request.Address,
                    Position = request.Position,
                    DateOfBirth = request.DateOfBirth ?? DateTime.UtcNow.AddYears(-30), // Default value if not provided
                    ProfilePictureUrl = request.ProfilePictureUrl,
                    IsNotificationEnabled = request.IsNotificationEnabled ?? true,
                    IsEnabled = request.IsEnabled ?? true,
                    Gender = ParseGender(request.Gender), // Parse gender as provided by the plugin
                    OrganizationId = !string.IsNullOrEmpty(request.OrganizationId) ? 
                        Guid.Parse(request.OrganizationId) : null
                };
                
                // Wrap with the WithId command
                var updateCommand = new UpdateUserCommandWithId(id, command);
                
                // Send the command
                var updatedUser = await _mediator.Send(updateCommand);
                
                // Create the response
                var response = new UserUpdateResponse
                {
                    UserId = updatedUser.Id.ToString(),
                    FirstName = updatedUser.FirstName,
                    LastName = updatedUser.LastName,
                    Email = updatedUser.Email,
                    Address = updatedUser.Address,
                    Position = updatedUser.Position,
                    DateOfBirth = updatedUser.DateOfBirth,
                    ProfilePictureUrl = updatedUser.ProfilePictureUrl,
                    IsEnabled = updatedUser.IsEnabled,
                    Gender = updatedUser.Gender.ToString(),
                    OrganizationId = updatedUser.OrganizationId?.ToString(),
                    IsNotificationEnabled = updatedUser.IsNotificationEnabled,
                    UpdatedAt = DateTime.UtcNow,
                    Success = true,
                    Message = $"User {updatedUser.FirstName} {updatedUser.LastName} has been updated successfully."
                };
                
                _logger.LogInformation("User {UserId} ({UserName}) updated successfully", userId, $"{updatedUser.FirstName} {updatedUser.LastName}");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user with ID: {UserId}", userId);
                return new UserUpdateResponse
                {
                    UserId = userId,
                    Success = false,
                    Message = $"Failed to update user: {ex.Message}"
                };
            }
        }
        
        /// <summary>
        /// Helper method to parse the gender string
        /// </summary>
        private Gender ParseGender(string genderString)
        {
            if (string.IsNullOrEmpty(genderString))
            {
                return Gender.Male; // Default to Male if not specified
            }
            
            if (Enum.TryParse<Gender>(genderString, true, out var gender))
            {
                return gender;
            }
            
            _logger.LogWarning("Invalid gender value provided: {Gender}. Using default Male value.", genderString);
            return Gender.Male;
        }
    }
}
