using EllipticCurve.Utils;

namespace NXM.Tensai.Back.OKR.Infrastructure;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.UserName).IsRequired().HasMaxLength(50);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(100);
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.OrganizationId).IsRequired(false);
    }
}
