namespace NXM.Tensai.Back.OKR.Infrastructure;

public class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).HasMaxLength(100);
        builder.HasOne(t => t.Organization)
               .WithMany()
               .HasForeignKey(t => t.OrganizationId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
