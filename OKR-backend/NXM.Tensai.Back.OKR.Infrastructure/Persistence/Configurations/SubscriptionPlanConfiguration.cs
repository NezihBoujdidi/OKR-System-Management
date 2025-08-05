using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NXM.Tensai.Back.OKR.Domain;

namespace NXM.Tensai.Back.OKR.Infrastructure.Persistence.Configurations;

public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlanEntity>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlanEntity> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.PlanId)
            .HasMaxLength(50)
            .IsRequired();
        
        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();
        
        builder.Property(x => x.Description)
            .HasMaxLength(500);
        
        builder.Property(x => x.Price)
            .HasPrecision(18, 2)
            .IsRequired();
        
        builder.Property(x => x.Interval)
            .HasMaxLength(20)
            .IsRequired();
        
        builder.Property(x => x.StripeProductId)
            .HasMaxLength(100);
        
        builder.Property(x => x.StripePriceId)
            .HasMaxLength(100);
        
        builder.HasMany(x => x.Features)
            .WithOne(x => x.Plan)
            .HasForeignKey(x => x.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
} 