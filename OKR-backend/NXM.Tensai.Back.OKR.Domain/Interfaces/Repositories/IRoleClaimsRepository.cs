namespace NXM.Tensai.Back.OKR.Domain;

public interface IRoleClaimsRepository : IRepository<IdentityRoleClaim<Guid>>
{
    Task<bool> HasClaimAsync(Guid roleId, string claimType, string claimValue);
}
