namespace NXM.Tensai.Back.OKR.Application;

/// <summary>
/// Interface for Supabase client operations
/// </summary>
public interface ISupabaseClient
{
    /// <summary>
    /// Invites a user by email with specified role and organization data
    /// </summary>
    /// <param name="email">Email address of the user to invite</param>
    /// <param name="role">Role to assign to the user</param>
    /// <param name="organizationId">Organization ID the user will belong to</param>
    /// <param name="teamId">Optional team ID the user will belong to</param>
    /// <returns>A result object containing status and any relevant information</returns>
    Task<SupabaseInviteResult> InviteUserByEmailAsync(string email, string role, Guid organizationId, Guid? teamId = null);
}

/// <summary>
/// Result object for Supabase invite operations
/// </summary>
public class SupabaseInviteResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public string InviteId { get; set; }
    
    public static SupabaseInviteResult Successful(string inviteId) => 
        new SupabaseInviteResult { 
            Success = true, 
            InviteId = inviteId,
            Message = "Invitation sent successfully" 
        };
    
    public static SupabaseInviteResult Failed(string message) => 
        new SupabaseInviteResult { 
            Success = false, 
            Message = message 
        };
}
