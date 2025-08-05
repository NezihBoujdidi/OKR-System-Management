using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NXM.Tensai.Back.OKR.Domain.Entities;
using NXM.Tensai.Back.OKR.Domain;

namespace NXM.Tensai.Back.OKR.Infrastructure;

public class OKRDbContext : IdentityDbContext<User, Role, Guid>
{
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<Team> Teams { get; set; }
    public DbSet<TeamUser> TeamUsers { get; set; }
    public DbSet<OKRSession> OKRSessions { get; set; }
    public DbSet<OKRSessionTeam> OKRSessionTeams { get; set; }
    public DbSet<Objective> Objectives { get; set; }
    public DbSet<KeyResult> KeyResults { get; set; }
    public DbSet<KeyResultTask> KeyResultTasks { get; set; }
    public DbSet<InvitationLink> InvitationLinks { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }

    public OKRDbContext(DbContextOptions<OKRDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(OKRDbContext).Assembly);
    }
}
