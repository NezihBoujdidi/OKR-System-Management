namespace NXM.Tensai.Back.OKR.Infrastructure;

public class OKRSessionConfiguration : IEntityTypeConfiguration<OKRSession>
{
    public void Configure(EntityTypeBuilder<OKRSession> builder)
    {
        builder.HasKey(o => o.Id);
        
        builder.Property(o => o.Title)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(o => o.Description)
            .HasMaxLength(500);
            
        builder.Property(o => o.Color)
            .HasMaxLength(50);


// builder.HasMany(o => o.OKRSessionTeams)
//             .WithOne(ost => ost.OKRSession)
//             .HasForeignKey(ost => ost.OKRSessionId)
//             .OnDelete(DeleteBehavior.Cascade);

// builder.HasMany(o => o.OKRSessionTeams)
//             .WithOne(ost => ost.OKRSession)
//             .HasForeignKey(ost => ost.OKRSessionId)
//             .OnDelete(DeleteBehavior.Cascade);

// builder.HasMany(o => o.OKRSessionTeams)
//             .WithOne(ost => ost.OKRSession)
//             .HasForeignKey(ost => ost.OKRSessionId)
//             .OnDelete(DeleteBehavior.Cascade);

        // builder.HasMany(o => o.OKRSessionTeams)
        //     .WithOne(ost => ost.OKRSession)
        //     .HasForeignKey(ost => ost.OKRSessionId)
        //     .OnDelete(DeleteBehavior.Cascade);

        // Indexing
        builder.HasIndex(o => o.UserId);
        builder.HasIndex(o => o.IsActive);
    }
}
