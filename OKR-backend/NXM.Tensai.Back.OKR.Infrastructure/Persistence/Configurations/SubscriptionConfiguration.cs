using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NXM.Tensai.Back.OKR.Domain;
using NXM.Tensai.Back.OKR.Domain.Entities;

namespace NXM.Tensai.Back.OKR.Infrastructure.Persistence.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.HasOne<Organization>()
            .WithOne()
            .HasForeignKey<Subscription>(s => s.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.Property(x => x.StripeCustomerId)
            .HasMaxLength(100)
            .IsRequired();
        
        builder.Property(x => x.StripeSubscriptionId)
            .HasMaxLength(100)
            .IsRequired();
        
        builder.Property(x => x.Status)
            .HasMaxLength(50)
            .IsRequired();
        
        builder.Property(x => x.Currency)
            .HasMaxLength(3)
            .IsRequired();
        
        builder.Property(x => x.LastPaymentIntentId)
            .HasMaxLength(100);
        
        builder.Property(x => x.Amount)
            .HasPrecision(18, 2)
            .IsRequired();
    }
} 