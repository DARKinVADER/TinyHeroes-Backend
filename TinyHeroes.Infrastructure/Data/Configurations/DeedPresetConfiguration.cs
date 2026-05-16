using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TinyHeroes.Domain.Entities;

namespace TinyHeroes.Infrastructure.Data.Configurations;

public class DeedPresetConfiguration : IEntityTypeConfiguration<DeedPreset>
{
    public void Configure(EntityTypeBuilder<DeedPreset> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Label).HasMaxLength(200);
        builder.Property(p => p.ImageValue).HasMaxLength(50);
        builder.HasOne(p => p.Family).WithMany().HasForeignKey(p => p.FamilyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(p => p.FamilyId);

        builder.HasData(
            new DeedPreset { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), FamilyId = null, Label = "Did homework", ImageValue = "📚", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new DeedPreset { Id = Guid.Parse("00000000-0000-0000-0000-000000000002"), FamilyId = null, Label = "Helped in kitchen", ImageValue = "🍳", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new DeedPreset { Id = Guid.Parse("00000000-0000-0000-0000-000000000003"), FamilyId = null, Label = "Cleaned room", ImageValue = "🧹", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new DeedPreset { Id = Guid.Parse("00000000-0000-0000-0000-000000000004"), FamilyId = null, Label = "Helped sibling", ImageValue = "🤝", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new DeedPreset { Id = Guid.Parse("00000000-0000-0000-0000-000000000005"), FamilyId = null, Label = "Behaved all day", ImageValue = "😊", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new DeedPreset { Id = Guid.Parse("00000000-0000-0000-0000-000000000006"), FamilyId = null, Label = "Made bed", ImageValue = "🛏️", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}
