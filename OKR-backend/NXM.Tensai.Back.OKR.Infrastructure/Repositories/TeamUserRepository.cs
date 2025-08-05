namespace NXM.Tensai.Back.OKR.Infrastructure;

public class TeamUserRepository : Repository<TeamUser>, ITeamUserRepository
{
    public TeamUserRepository(OKRDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Team>> GetTeamsByCollaboratorIdAsync(Guid collaboratorId)
    {
        return await _context.TeamUsers
            .Where(tu => tu.UserId == collaboratorId)
            .Include(tu => tu.Team)
            .Select(tu => tu.Team)
            .ToListAsync();
    }

    public async Task<IEnumerable<User>> GetUsersByTeamIdAsync(Guid teamId)
    {
        return await _context.TeamUsers
            .Where(tu => tu.TeamId == teamId)
            .Include(tu => tu.User)
            .Select(tu => tu.User)
            .ToListAsync();
    }

    public async Task<TeamUser?> GetByTeamAndUserIdAsync(Guid teamId, Guid userId)
    {
        return await _context.TeamUsers
            .FirstOrDefaultAsync(tu => tu.TeamId == teamId && tu.UserId == userId);
    }
    public async Task<List<Team>> GetTeamsByUserIdAsync(Guid userId)
    {
        return await _context.TeamUsers
            .Where(tu => tu.UserId == userId)
            .Select(tu => tu.Team)
            .ToListAsync();
    }
}
