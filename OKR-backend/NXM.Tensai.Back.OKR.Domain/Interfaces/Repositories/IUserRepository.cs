namespace NXM.Tensai.Back.OKR.Domain;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetUserByEmailAsync(string email);
    Task<User?> GetUserBySupabaseIdAsync(string supabaseId);
    Task<IEnumerable<User>> GetUsersByOrganizationIdAsync(Guid organizationId);
    Task<IEnumerable<User>> GetTeamManagersByOrganizationIdAsync(Guid organizationId);
    Task<User> GetOrganizationAdminAsync(Guid organizationId);
}
