using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TinyHeroes.Domain.Entities;

namespace TinyHeroes.Infrastructure.Data.Configurations;

public class MonthSummaryConfiguration : IEntityTypeConfiguration<MonthSummary>
{
    public void Configure(EntityTypeBuilder<MonthSummary> builder)
    {
        builder.HasKey(m => m.Id);
        builder.HasOne(m => m.Family).WithMany().HasForeignKey(m => m.FamilyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(m => m.Champion).WithMany().HasForeignKey(m => m.ChampionChildId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(m => new { m.FamilyId, m.Year, m.Month }).IsUnique();
    }
}
