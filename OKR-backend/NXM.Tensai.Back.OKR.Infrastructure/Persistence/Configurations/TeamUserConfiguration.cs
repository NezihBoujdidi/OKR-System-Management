namespace NXM.Tensai.Back.OKR.Infrastructure;

public class TeamUserConfiguration : IEntityTypeConfiguration<TeamUser>
{
    public void Configure(EntityTypeBuilder<TeamUser> builder)
    {
        builder.HasKey(tu => new { tu.TeamId, tu.UserId });
        builder.HasOne(tu => tu.Team)
               .WithMany()
               .HasForeignKey(tu => tu.TeamId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(tu => tu.User)
               .WithMany()
               .HasForeignKey(tu => tu.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
