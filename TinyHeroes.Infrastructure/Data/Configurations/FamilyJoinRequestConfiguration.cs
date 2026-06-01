using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TinyHeroes.Domain.Entities;

namespace TinyHeroes.Infrastructure.Data.Configurations;

public class FamilyJoinRequestConfiguration : IEntityTypeConfiguration<FamilyJoinRequest>
{
    public void Configure(EntityTypeBuilder<FamilyJoinRequest> builder)
    {
        builder.HasOne(r => r.Family)
            .WithMany(f => f.JoinRequests)
            .HasForeignKey(r => r.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.RequestedBy)
            .WithMany()
            .HasForeignKey(r => r.RequestedById)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.RequestedById, r.Status });
        builder.Property(r => r.Status).HasConversion<string>();
    }
}
