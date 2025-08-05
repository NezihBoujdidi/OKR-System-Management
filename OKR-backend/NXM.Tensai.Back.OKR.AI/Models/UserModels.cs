using System;
using System.Collections.Generic;

namespace NXM.Tensai.Back.OKR.AI.Models
{
    // Existing models

    /// <summary>
    /// Response model for searching users
    /// </summary>
    public class UserSearchResponse
    {
        public string SearchTerm { get; set; }
        public string OrganizationId { get; set; }
        public List<UserDetailsResponse> Users { get; set; } = new List<UserDetailsResponse>();
        public int Count => Users?.Count ?? 0;
        public string PromptTemplate { get; set; }
    }

    /// <summary>
    /// Response model for listing team managers
    /// </summary>
    public class TeamManagersResponse
    {
        public string OrganizationId { get; set; }
        public List<UserDetailsResponse> Managers { get; set; } = new List<UserDetailsResponse>();
        public int Count => Managers?.Count ?? 0;
        public string PromptTemplate { get; set; }
    }

    /// <summary>
    /// Request model for inviting a user
    /// </summary>
    public class UserInviteRequest
    {
        public string Email { get; set; }
        public string Role { get; set; }
        public string OrganizationId { get; set; }
        public string TeamId { get; set; }
    }

    /// <summary>
    /// Response model for user invitation
    /// </summary>
    public class UserInviteResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string OrganizationId { get; set; }
        public string TeamId { get; set; }
        public string InviteId { get; set; }
        public string PromptTemplate { get; set; }
    }

    /// <summary>
    /// Response model for user details
    /// </summary>
    public class UserDetailsResponse
    {
        public string UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        public string Email { get; set; }
        public string Position { get; set; }
        public string ProfilePictureUrl { get; set; }
        public bool IsEnabled { get; set; } = true;
        public string DateOfBirth { get; set; }
        public string Role { get; set; }
        public string Address { get; set; }
        public string Gender { get; set; }
        public string OrganizationId { get; set; }
        public bool IsNotificationEnabled { get; set; }
    }

    // New models for the additional methods

    /// <summary>
    /// Response model for listing users by organization
    /// </summary>
    public class UsersListResponse
    {
        public string OrganizationId { get; set; }
        public List<UserDetailsResponse> Users { get; set; } = new List<UserDetailsResponse>();
        public int Count => Users?.Count ?? 0;
        public string PromptTemplate { get; set; }
    }

    /// <summary>
    /// Response model for user enable/disable actions
    /// </summary>
    public class UserActionResponse
    {
        public string UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public bool IsEnabled { get; set; }
        public string Action { get; set; } // "enabled" or "disabled"
        public bool Success { get; set; }
        public string Message { get; set; }
        public string PromptTemplate { get; set; }
    }

    /// <summary>
    /// Request model for updating a user
    /// </summary>
    public class UserUpdateRequest
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string Position { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string ProfilePictureUrl { get; set; }
        public bool? IsNotificationEnabled { get; set; }
        public bool? IsEnabled { get; set; }
        public string Gender { get; set; }
        public string OrganizationId { get; set; }
    }

    /// <summary>
    /// Response model for updating a user
    /// </summary>
    public class UserUpdateResponse
    {
        public string UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string Position { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string ProfilePictureUrl { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsNotificationEnabled { get; set; }
        public string Gender { get; set; }
        public string OrganizationId { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
        public string PromptTemplate { get; set; }
    }
}
