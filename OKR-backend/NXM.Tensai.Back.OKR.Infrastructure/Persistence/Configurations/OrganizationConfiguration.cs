namespace NXM.Tensai.Back.OKR.Infrastructure;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Name).IsRequired().HasMaxLength(100);
        builder.Property(o => o.Description).HasMaxLength(500);
        builder.Property(o => o.Country).HasMaxLength(100);
        builder.Property(o => o.Industry).HasMaxLength(100);
        builder.Property(o => o.Email).HasMaxLength(100);
        builder.Property(o => o.Phone).HasMaxLength(20);
    }
}
