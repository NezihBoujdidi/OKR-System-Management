namespace NXM.Tensai.Back.OKR.Infrastructure
{
    public class InvitationLinkConfiguration : IEntityTypeConfiguration<InvitationLink>
    {
        public void Configure(EntityTypeBuilder<InvitationLink> builder)
        {
            builder.HasKey(o => o.Id); // Primary key
            builder.Property(o => o.Email).IsRequired().HasMaxLength(255); // Email is required
            builder.Property(o => o.Token).IsRequired();
            builder.Property(o => o.OrganizationId).IsRequired();
            builder.Property(o => o.ExpirationDate).IsRequired(); 
        }
    }
}
