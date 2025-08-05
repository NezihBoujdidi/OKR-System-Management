namespace NXM.Tensai.Back.OKR.Infrastructure;

public class UserRepository : Repository<User>, IUserRepository
{
    private readonly UserManager<User> _userManager;

    public UserRepository(OKRDbContext context, UserManager<User> userManager) : base(context)
    {
        _userManager = userManager;
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _dbSet.SingleOrDefaultAsync(u => u.Email == email);
    }
    
    public async Task<User?> GetUserBySupabaseIdAsync(string supabaseId)
    {
        return await _dbSet.SingleOrDefaultAsync(u => u.SupabaseId == supabaseId);
    }

    public async Task<IEnumerable<User>> GetUsersByOrganizationIdAsync(Guid organizationId)
    {
        var users = await _dbSet.Where(u => u.OrganizationId == organizationId)
                           .ToListAsync();
        
        return users;
    }

    public async Task<IEnumerable<User>> GetTeamManagersByOrganizationIdAsync(Guid organizationId)
    {
        // Get all users from the organization
        var users = await _dbSet.Where(u => u.OrganizationId == organizationId)
                               .ToListAsync();
        
        // Filter users who have the TeamManager role
        var teamManagers = new List<User>();
        foreach (var user in users)
        {
            if (await _userManager.IsInRoleAsync(user, "TeamManager"))
            {
                teamManagers.Add(user);
            }
        }
        
        return teamManagers;
    }

    public async Task<User> GetOrganizationAdminAsync(Guid organizationId)
    {
        // Get all users from the organization
        var users = await _dbSet.Where(u => u.OrganizationId == organizationId)
                               .ToListAsync();

        // Filter users who have the Admin role
        var admins = new List<User>();
        foreach (var user in users)
        {
            if (await _userManager.IsInRoleAsync(user, "OrganizationAdmin"))
            {
                admins.Add(user);
            }
        }

        return admins[0];
    }
}
