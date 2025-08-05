using System;
using System.Collections.Generic;
using NXM.Tensai.Back.OKR.Domain;

namespace NXM.Tensai.Back.OKR.AI.Models
{
    /// <summary>
    /// Request model for creating a key result task
    /// </summary>
    public class KeyResultTaskCreationRequest
    {
        public string Title { get; set; }
        public string? Description { get; set; }
        public string KeyResultId { get; set; }
        public string UserId { get; set; }
        public DateTime StartedDate { get; set; }
        public DateTime EndDate { get; set; }
        public string CollaboratorId { get; set; }
        public int Progress { get; set; }
        public string? Priority { get; set; }
    }

    /// <summary>
    /// Response model for key result task creation
    /// </summary>
    public class KeyResultTaskCreationResponse
    {
        public string KeyResultTaskId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string KeyResultId { get; set; }
        public string KeyResultTitle { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UserId { get; set; }
        public string CollaboratorId { get; set; }
        public int Progress { get; set; }
        public string PromptTemplate { get; set; }  // Added for AI response generation
    }

    /// <summary>
    /// Request model for updating a key result task
    /// </summary>
    public class KeyResultTaskUpdateRequest
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? KeyResultId { get; set; }
        public string? UserId { get; set; }
        public DateTime? StartedDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? Progress { get; set; }
        public string? Priority { get; set; }
        public bool? IsDeleted { get; set; }
    }

    /// <summary>
    /// Response model for key result task update operations
    /// </summary>
    public class KeyResultTaskUpdateResponse
    {
        public string KeyResultTaskId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string KeyResultId { get; set; }
        public string KeyResultTitle { get; set; }
        public string UserId { get; set; }
        public DateTime StartedDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Progress { get; set; }
        public string Priority { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string PromptTemplate { get; set; }  // Added for AI response generation
    }

    /// <summary>
    /// Response model for key result task delete operations
    /// </summary>
    public class KeyResultTaskDeleteResponse
    {
        /// <summary>
        /// ID of the key result task that was deleted
        /// </summary>
        public string KeyResultTaskId { get; set; }
        
        /// <summary>
        /// Title of the key result task that was deleted
        /// </summary>
        public string Title { get; set; }
        
        /// <summary>
        /// ID of the associated key result
        /// </summary>
        public string KeyResultId { get; set; }
        
        /// <summary>
        /// Title of the associated key result
        /// </summary>
        public string KeyResultTitle { get; set; }
        
        /// <summary>
        /// When the key result task was deleted
        /// </summary>
        public DateTime DeletedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Prompt template for generating responses
        /// </summary>
        public string PromptTemplate { get; set; }
    }

    /// <summary>
    /// Response model for key result task details
    /// </summary>
    public class KeyResultTaskDetailsResponse
    {
        public string KeyResultTaskId { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public string KeyResultId { get; set; }
        public string KeyResultTitle { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string CollaboratorId { get; set; }
        public string CollaboratorName { get; set; }
        public DateTime StartedDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int Progress { get; set; }
        public string Priority { get; set; }
        public bool IsDeleted { get; set; }
        public string PromptTemplate { get; set; }
    }

    /// <summary>
    /// Represents a search response for key result task searches
    /// </summary>
    public class KeyResultTaskSearchResponse
    {
        /// <summary>
        /// List of key result tasks matching the search criteria
        /// </summary>
        public List<KeyResultTaskDetailsResponse> KeyResultTasks { get; set; } = new List<KeyResultTaskDetailsResponse>();
        
        /// <summary>
        /// Total number of key result tasks found
        /// </summary>
        public int Count => KeyResultTasks?.Count ?? 0;
        
        /// <summary>
        /// Search criteria used (title)
        /// </summary>
        public string SearchTerm { get; set; }
        
        /// <summary>
        /// Key Result ID if filtered by key result
        /// </summary>
        public string KeyResultId { get; set; }
        
        /// <summary>
        /// User ID if filtered by user
        /// </summary>
        public string UserId { get; set; }
        
        /// <summary>
        /// Prompt template for generating responses
        /// </summary>
        public string PromptTemplate { get; set; }
    }

    /// <summary>
    /// Represents a response for listing key result tasks by key result
    /// </summary>
    public class KeyResultTasksByKeyResultResponse
    {
        /// <summary>
        /// ID of the key result
        /// </summary>
        public string KeyResultId { get; set; }
        
        /// <summary>
        /// Title of the key result
        /// </summary>
        public string KeyResultTitle { get; set; }
        
        /// <summary>
        /// List of tasks for this key result
        /// </summary>
        public List<KeyResultTaskDetailsResponse> KeyResultTasks { get; set; } = new List<KeyResultTaskDetailsResponse>();
        
        /// <summary>
        /// Total number of tasks found
        /// </summary>
        public int Count => KeyResultTasks?.Count ?? 0;
        
        /// <summary>
        /// Prompt template for generating responses
        /// </summary>
        public string PromptTemplate { get; set; }
    }
}