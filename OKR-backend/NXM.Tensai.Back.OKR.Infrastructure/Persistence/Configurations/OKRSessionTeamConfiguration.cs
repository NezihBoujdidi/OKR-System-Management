using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NXM.Tensai.Back.OKR.Domain;

namespace NXM.Tensai.Back.OKR.Infrastructure;

public class OKRSessionTeamConfiguration : IEntityTypeConfiguration<OKRSessionTeam>
{
    public void Configure(EntityTypeBuilder<OKRSessionTeam> builder)
    {
        builder.HasKey(ost => new { ost.OKRSessionId, ost.TeamId });  

        builder.HasOne(ost => ost.OKRSession)
               .WithMany()
               .HasForeignKey(ost => ost.OKRSessionId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ost => ost.Team)
               .WithMany()
               .HasForeignKey(ost => ost.TeamId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

