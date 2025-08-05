namespace NXM.Tensai.Back.OKR.Infrastructure;

public class KeyResultConfiguration : IEntityTypeConfiguration<KeyResult>
{
    public void Configure(EntityTypeBuilder<KeyResult> builder)
    {
        builder.HasKey(kr => kr.Id);
        builder.Property(kr => kr.Title).IsRequired().HasMaxLength(100);
        builder.Property(kr => kr.Description).HasMaxLength(500);
        builder.HasOne(kr => kr.Objective)
               .WithMany()
               .HasForeignKey(kr => kr.ObjectiveId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
