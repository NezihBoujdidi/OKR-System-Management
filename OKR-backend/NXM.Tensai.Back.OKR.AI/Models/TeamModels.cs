using System;
using System.Collections.Generic;

namespace NXM.Tensai.Back.OKR.AI.Models
{
    /// <summary>
    /// Request model for creating a team
    /// </summary>
    public class TeamCreationRequest
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public string OrganizationId { get; set; }
        public string? TeamManagerId { get; set; }
        public string? UserId { get; set; }
        public string? CurrentUserId { get; set; }
    }

    /// <summary>
    /// Response model for team creation
    /// </summary>
    public class TeamCreationResponse
    {
        public string TeamId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string PromptTemplate { get; set; }  // Added for AI response generation
    }

    /// <summary>
    /// Request model for adding a member to a team
    /// </summary>
    public class TeamMemberRequest
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string AddedByUserId { get; set; }
    }

    /// <summary>
    /// Response model for team member operations
    /// </summary>
    public class TeamMemberResponse
    {
        public string MemberId { get; set; }
        public string TeamId { get; set; }
        public string UserId { get; set; }
        public string Email { get; set; }
        public string Position { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Role { get; set; }
        public DateTime AddedAt { get; set; }
    }

    /// <summary>
    /// Response model for team details
    /// </summary>
    public class TeamDetailsResponse
    {
        public string TeamId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? TeamManagerId { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<TeamMemberResponse> Members { get; set; } = new List<TeamMemberResponse>();
        public string PromptTemplate { get; set; } 
    }

    /// <summary>
    /// Request model for updating a team  
    /// </summary>
    public class TeamUpdateRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string OrganizationId { get; set; }
        public string? TeamManagerId { get; set; }
    }

    /// <summary>
    /// Response model for team update operations
    /// </summary>
    public class TeamUpdateResponse
    {
        public string TeamId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string TeamManagerId { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string PromptTemplate { get; set; }  // Added for AI response generation
    }

    /// <summary>
    /// Response model for team delete operations
    /// </summary>
    public class TeamDeleteResponse
    {
        /// <summary>
        /// ID of the team that was deleted
        /// </summary>
        public string TeamId { get; set; }
        
        /// <summary>
        /// Name of the team that was deleted
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// When the team was deleted
        /// </summary>
        public DateTime DeletedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// ID of the user who deleted the team
        /// </summary>
        public string DeletedBy { get; set; }
        
        /// <summary>
        /// Prompt template for generating responses
        /// </summary>
        public string PromptTemplate { get; set; }
    }

    /// <summary>
    /// Represents a search response for team searches
    /// </summary>
    public class TeamSearchResponse
    {
        /// <summary>
        /// List of teams matching the search criteria
        /// </summary>
        public List<TeamDetailsResponse> Teams { get; set; } = new List<TeamDetailsResponse>();
        
        /// <summary>
        /// Total number of teams found
        /// </summary>
        public int Count => Teams?.Count ?? 0;
        
        /// <summary>
        /// Search criteria used
        /// </summary>
        public string SearchTerm { get; set; }
        
        /// <summary>
        /// Organization ID if filtered by organization
        /// </summary>
        public string OrganizationId { get; set; }
        
        /// <summary>
        /// Prompt template for generating responses
        /// </summary>
        public string PromptTemplate { get; set; }
    }

    /// <summary>
    /// Represents a response for listing teams by manager
    /// </summary>
    public class TeamsByManagerResponse
    {
        /// <summary>
        /// ID of the manager
        /// </summary>
        public string ManagerId { get; set; }
        
        /// <summary>
        /// List of teams managed by this manager
        /// </summary>
        public List<TeamDetailsResponse> Teams { get; set; } = new List<TeamDetailsResponse>();
        
        /// <summary>
        /// Total number of teams found
        /// </summary>
        public int Count => Teams?.Count ?? 0;
        
        /// <summary>
        /// Prompt template for generating responses
        /// </summary>
        public string PromptTemplate { get; set; }
    }

    /// <summary>
    /// Represents a response for listing teams by organization
    /// </summary>
    public class TeamsByOrganizationResponse
    {
        /// <summary>
        /// ID of the organization
        /// </summary>
        public string OrganizationId { get; set; }
        
        /// <summary>
        /// List of teams in this organization
        /// </summary>
        public List<TeamDetailsResponse> Teams { get; set; } = new List<TeamDetailsResponse>();
        
        /// <summary>
        /// Total number of teams found
        /// </summary>
        public int Count => Teams?.Count ?? 0;
        
        /// <summary>
        /// Prompt template for generating responses
        /// </summary>
        public string PromptTemplate { get; set; }
    }
}