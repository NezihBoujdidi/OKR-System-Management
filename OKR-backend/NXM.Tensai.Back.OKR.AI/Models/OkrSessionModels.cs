// In Models/OkrSessionModels.cs
namespace NXM.Tensai.Back.OKR.AI.Models
{
    public class OkrSessionCreationRequest
    {
        public string Title { get; set; }
        public string OrganizationId { get; set; }
        public string Description { get; set; }
        public DateTime StartedDate { get; set; }
        public DateTime EndDate { get; set; }
        //public string TeamManagerId { get; set; }
        public List<string> TeamIds { get; set; } = new List<string>();
        public string UserId { get; set; }
        public string Color { get; set; }
        public string Status { get; set; }
    }

    public class OkrSessionCreationResponse
    {
        public string OkrSessionId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartedDate { get; set; }
        public DateTime EndDate { get; set; }
        //public string TeamManagerId { get; set; }
        public List<string> TeamIds { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string Color { get; set; }
        public string Status { get; set; }
        public string PromptTemplate { get; set; }
    }

    public class OkrSessionUpdateRequest
    { 
        public string UserId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartedDate { get; set; }
        public DateTime EndDate { get; set; }
        //public string TeamManagerId { get; set; }
        public string Color { get; set; }
        public string Status { get; set; }
    }

    public class OkrSessionUpdateResponse
    {
        public string OkrSessionId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartedDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string PromptTemplate { get; set; }
    }

    public class OkrSessionDeleteResponse
    {
        public string OkrSessionId { get; set; }
        public string Title { get; set; }
        public DateTime DeletedAt { get; set; }
        public string PromptTemplate { get; set; }
    }

    public class OkrSessionDetailsResponse
    {
        public string OkrSessionId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        //public string TeamManagerId { get; set; }
        //public string TeamManagerName { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Color { get; set; }
        public string Status { get; set; }
        public int Progress { get; set; }
        public string PromptTemplate { get; set; }
    }

    public class OkrSessionSearchResponse
    {
        public string SearchTerm { get; set; }
        public List<OkrSessionDetailsResponse> Sessions { get; set; } = new List<OkrSessionDetailsResponse>();
        public int Count => Sessions.Count;
        public string PromptTemplate { get; set; }
    }

    public class OkrSessionsByTeamResponse
    {
        public string TeamId { get; set; }
        public string TeamName { get; set; }
        public List<OkrSessionDetailsResponse> Sessions { get; set; } = new List<OkrSessionDetailsResponse>();
        public int Count => Sessions.Count;
        public string PromptTemplate { get; set; }
    }

    // --- Added for AI Insights ---
    public class SessionInsightsRequest
    {
        public string SessionId { get; set; }
        public UserContext UserContext { get; set; }
    }

    public class SessionInsightsResponse
    {
        public List<string> Insights { get; set; } = new List<string>();
    }

    public class OkrSuggestionRequest
    {
        public string Prompt { get; set; }
        public List<TeamInfo> AvailableTeams { get; set; }
        public string Context { get; set; }
    }

    public class TeamInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class OkrSuggestionResponse
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public List<string> SuggestedTeams { get; set; }
        public DateTime? SuggestedStartDate { get; set; }
        public DateTime? SuggestedEndDate { get; set; }
        public List<string> IndustryInsights { get; set; }
        public List<string> AlignmentTips { get; set; }
        public List<string> KeyFocusAreas { get; set; }
        public List<string> PotentialKeyResults { get; set; }
    }
}