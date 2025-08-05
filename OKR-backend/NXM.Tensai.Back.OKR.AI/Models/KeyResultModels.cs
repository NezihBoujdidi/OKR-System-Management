// In Models/KeyResultModels.cs
namespace NXM.Tensai.Back.OKR.AI.Models
{
    /// <summary>
    /// Request model for creating a new key result
    /// </summary>
    public class KeyResultCreationRequest
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartedDate { get; set; }
        public DateTime EndDate { get; set; }
        public string ObjectiveId { get; set; }
        public string UserId { get; set; }
        public string Status { get; set; }
        public int Progress { get; set; }
    }

    /// <summary>
    /// Response model for key result creation
    /// </summary>
    public class KeyResultCreationResponse
    {
        public string KeyResultId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartedDate { get; set; }
        public DateTime EndDate { get; set; }
        public string ObjectiveId { get; set; }
        public string UserId { get; set; }
        public string Status { get; set; }
        public int Progress { get; set; }
        public DateTime CreatedAt { get; set; }
        public string PromptTemplate { get; set; }
    }

    /// <summary>
    /// Request model for updating a key result
    /// </summary>
    public class KeyResultUpdateRequest
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartedDate { get; set; }
        public DateTime EndDate { get; set; }
        public string ObjectiveId { get; set; }
        public string UserId { get; set; }
        public string Status { get; set; }
        public int Progress { get; set; }
    }

    /// <summary>
    /// Response model for key result update
    /// </summary>
    public class KeyResultUpdateResponse
    {
        public string KeyResultId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartedDate { get; set; }
        public DateTime EndDate { get; set; }
        public string ObjectiveId { get; set; }
        public string ObjectiveTitle { get; set; }
        public string UserId { get; set; }
        public string Status { get; set; }
        public int Progress { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string PromptTemplate { get; set; }
    }

    /// <summary>
    /// Response model for key result deletion
    /// </summary>
    public class KeyResultDeleteResponse
    {
        public string KeyResultId { get; set; }
        public string Title { get; set; }
        public DateTime DeletedAt { get; set; }
        public string PromptTemplate { get; set; }
    }

    /// <summary>
    /// Response model for key result details
    /// </summary>
    public class KeyResultDetailsResponse
    {
        public string KeyResultId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartedDate { get; set; }
        public DateTime EndDate { get; set; }
        public string ObjectiveId { get; set; }
        public string ObjectiveTitle { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Status { get; set; }
        public int Progress { get; set; }
        public string PromptTemplate { get; set; }
    }

    /// <summary>
    /// Response model for key result searches
    /// </summary>
    public class KeyResultSearchResponse
    {
        public int Count { get; set; }
        public List<KeyResultDetailsResponse> KeyResults { get; set; } = new List<KeyResultDetailsResponse>();
        public string PromptTemplate { get; set; }
    }

    /// <summary>
    /// Response model for listing key results by objective
    /// </summary>
    public class KeyResultsByObjectiveResponse
    {
        public int Count { get; set; }
        public string ObjectiveId { get; set; }
        public string ObjectiveTitle { get; set; }
        public List<KeyResultDetailsResponse> KeyResults { get; set; } = new List<KeyResultDetailsResponse>();
        public string PromptTemplate { get; set; }
    }
}