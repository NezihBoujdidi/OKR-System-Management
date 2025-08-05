using System;
using System.Collections.Generic;

namespace NXM.Tensai.Back.OKR.AI.Models
{
    /// <summary>
    /// Request model for creating an objective
    /// </summary>
    public class ObjectiveCreationRequest
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string OKRSessionId { get; set; }
        public string ResponsibleTeamId { get; set; }
        public string UserId { get; set; }
        public DateTime StartedDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; }
        public string Priority { get; set; }
        public bool IsDeleted { get; set; } = false;
        public int Progress  { get; set; }
    }

    /// <summary>
    /// Response model for objective creation
    /// </summary>
    public class ObjectiveCreationResponse
    {
        public string ObjectiveId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string OKRSessionId { get; set; }
        public string ResponsibleTeamId { get; set; }
        public string UserId { get; set; }
        public DateTime StartedDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Status { get; set; }
        public string Priority { get; set; }
        public int Progress { get; set; }
        public string PromptTemplate { get; set; }  // Added for AI response generation
    }

    /// <summary>
    /// Request model for updating an objective
    /// </summary>
    public class ObjectiveUpdateRequest
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string ResponsibleTeamId { get; set; }
        public DateTime StartedDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; }
        public string Priority { get; set; }
        public int? Progress { get; set; }
        public string OKRSessionId { get; set; }
        public string UserId { get; set; }
    }

    /// <summary>
    /// Response model for objective update operations
    /// </summary>
    public class ObjectiveUpdateResponse
    {
        public string ObjectiveId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string OKRSessionId { get; set; }
        public string ResponsibleTeamId { get; set; }
        public DateTime StartedDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string Status { get; set; }
        public string Priority { get; set; }
        public int Progress { get; set; }
        public string PromptTemplate { get; set; }  // Added for AI response generation
    }

    /// <summary>
    /// Response model for objective delete operations
    /// </summary>
    public class ObjectiveDeleteResponse
    {
        /// <summary>
        /// ID of the objective that was deleted
        /// </summary>
        public string ObjectiveId { get; set; }
        
        /// <summary>
        /// Title of the objective that was deleted
        /// </summary>
        public string Title { get; set; }
        
        /// <summary>
        /// When the objective was deleted
        /// </summary>
        public DateTime DeletedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// ID of the user who deleted the objective
        /// </summary>
        public string DeletedBy { get; set; }
        
        /// <summary>
        /// Prompt template for generating responses
        /// </summary>
        public string PromptTemplate { get; set; }
    }

    /// <summary>
    /// Response model for objective details
    /// </summary>
    public class ObjectiveDetailsResponse
    {
        public string ObjectiveId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string OKRSessionId { get; set; }
        public string OKRSessionTitle { get; set; }
        public string ResponsibleTeamId { get; set; }
        public string ResponsibleTeamName { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public DateTime StartedDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string Status { get; set; }
        public string Priority { get; set; }
        public int Progress { get; set; }
        public string PromptTemplate { get; set; }  // Added for AI response generation
    }

    /// <summary>
    /// Represents a search response for objective searches
    /// </summary>
    public class ObjectiveSearchResponse
    {
        /// <summary>
        /// List of objectives matching the search criteria
        /// </summary>
        public List<ObjectiveDetailsResponse> Objectives { get; set; } = new List<ObjectiveDetailsResponse>();
        
        /// <summary>
        /// Total number of objectives found
        /// </summary>
        public int Count => Objectives?.Count ?? 0;
        
        /// <summary>
        /// Search criteria used
        /// </summary>
        public string SearchTerm { get; set; }
        
        /// <summary>
        /// Prompt template for generating responses
        /// </summary>
        public string PromptTemplate { get; set; }
    }

    /// <summary>
    /// Represents a response for listing objectives by OKR session
    /// </summary>
    public class ObjectivesBySessionResponse
    {
        /// <summary>
        /// ID of the OKR session
        /// </summary>
        public string OKRSessionId { get; set; }
        
        /// <summary>
        /// Title of the OKR session
        /// </summary>
        public string OKRSessionTitle { get; set; }
        
        /// <summary>
        /// List of objectives in this OKR session
        /// </summary>
        public List<ObjectiveDetailsResponse> Objectives { get; set; } = new List<ObjectiveDetailsResponse>();
        
        /// <summary>
        /// Total number of objectives found
        /// </summary>
        public int Count => Objectives?.Count ?? 0;
        
        /// <summary>
        /// Prompt template for generating responses
        /// </summary>
        public string PromptTemplate { get; set; }
    }
}