namespace NXM.Tensai.Back.OKR.Infrastructure;

public class RoleClaimsRepository : Repository<IdentityRoleClaim<Guid>>, IRoleClaimsRepository
{
    public RoleClaimsRepository(OKRDbContext context) : base(context)
    {
    }

    public async Task<bool> HasClaimAsync(Guid roleId, string claimType, string claimValue)
    {
        return await _dbSet.AnyAsync(rc => rc.RoleId == roleId && rc.ClaimType == claimType && rc.ClaimValue == claimValue);
    }
}
