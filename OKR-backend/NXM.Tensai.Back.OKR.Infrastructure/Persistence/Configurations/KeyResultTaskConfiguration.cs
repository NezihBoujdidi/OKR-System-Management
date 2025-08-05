namespace NXM.Tensai.Back.OKR.Infrastructure;

public class KeyResultTaskConfiguration : IEntityTypeConfiguration<KeyResultTask>
{
    public void Configure(EntityTypeBuilder<KeyResultTask> builder)
    {
        builder.HasKey(krt => krt.Id);
        builder.Property(krt => krt.Title).IsRequired().HasMaxLength(100);
        builder.Property(krt => krt.Description).HasMaxLength(500);
        builder.HasOne(krt => krt.KeyResult)
               .WithMany()
               .HasForeignKey(krt => krt.KeyResultId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
