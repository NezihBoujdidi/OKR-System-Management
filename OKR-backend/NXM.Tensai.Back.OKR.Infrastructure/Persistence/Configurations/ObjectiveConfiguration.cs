namespace NXM.Tensai.Back.OKR.Infrastructure;

public class ObjectiveConfiguration : IEntityTypeConfiguration<Objective>
{
    public void Configure(EntityTypeBuilder<Objective> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Title).IsRequired().HasMaxLength(100);
        builder.Property(o => o.Description).HasMaxLength(500);
        builder.HasOne(o => o.OKRSession)
               .WithMany()
               .HasForeignKey(o => o.OKRSessionId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
