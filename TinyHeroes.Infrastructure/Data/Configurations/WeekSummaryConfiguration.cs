using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TinyHeroes.Domain.Entities;

namespace TinyHeroes.Infrastructure.Data.Configurations;

public class WeekSummaryConfiguration : IEntityTypeConfiguration<WeekSummary>
{
    public void Configure(EntityTypeBuilder<WeekSummary> builder)
    {
        builder.HasKey(w => w.Id);
        builder.HasOne(w => w.Family).WithMany().HasForeignKey(w => w.FamilyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(w => new { w.FamilyId, w.WeekStart }).IsUnique();
    }
}
