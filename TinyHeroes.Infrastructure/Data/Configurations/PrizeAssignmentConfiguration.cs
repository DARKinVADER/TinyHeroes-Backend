using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TinyHeroes.Domain.Entities;

namespace TinyHeroes.Infrastructure.Data.Configurations;

public class PrizeAssignmentConfiguration : IEntityTypeConfiguration<PrizeAssignment>
{
    public void Configure(EntityTypeBuilder<PrizeAssignment> builder)
    {
        builder.HasKey(p => p.Id);
        builder.HasOne(p => p.Family).WithMany().HasForeignKey(p => p.FamilyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(p => new { p.FamilyId, p.Scope, p.Rank }).IsUnique();
        builder.Property(p => p.Label).HasMaxLength(200);
        builder.Property(p => p.Emoji).HasMaxLength(50);
        builder.Property(p => p.Scope).HasMaxLength(20);
    }
}
