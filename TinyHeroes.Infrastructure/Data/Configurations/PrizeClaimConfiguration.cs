// backend/TinyHeroes.Infrastructure/Data/Configurations/PrizeClaimConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TinyHeroes.Domain.Entities;

namespace TinyHeroes.Infrastructure.Data.Configurations;

public class PrizeClaimConfiguration : IEntityTypeConfiguration<PrizeClaim>
{
    public void Configure(EntityTypeBuilder<PrizeClaim> builder)
    {
        builder.HasKey(p => p.Id);
        builder.HasOne(p => p.Family).WithMany().HasForeignKey(p => p.FamilyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(p => p.WeekSummary).WithMany().HasForeignKey(p => p.WeekSummaryId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(p => p.MonthSummary).WithMany().HasForeignKey(p => p.MonthSummaryId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(p => new { p.FamilyId, p.Scope, p.WeekSummaryId, p.MonthSummaryId, p.Rank }).IsUnique();
        builder.Property(p => p.Scope).HasMaxLength(10);
        builder.Property(p => p.ChildName).HasMaxLength(100);
        builder.Property(p => p.PrizeEmoji).HasMaxLength(50);
        builder.Property(p => p.PrizeLabel).HasMaxLength(200);
    }
}
