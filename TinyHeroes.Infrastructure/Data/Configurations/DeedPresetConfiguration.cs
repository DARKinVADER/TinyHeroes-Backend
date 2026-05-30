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
        builder.Property(p => p.LabelKey).HasMaxLength(100);
        builder.HasOne(p => p.Family).WithMany().HasForeignKey(p => p.FamilyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(p => p.FamilyId);

        builder.HasData(
            new DeedPreset { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), FamilyId = null, Label = "Did homework", LabelKey = "PRESET.SYSTEM.DID_HOMEWORK", ImageValue = "📚", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new DeedPreset { Id = Guid.Parse("00000000-0000-0000-0000-000000000002"), FamilyId = null, Label = "Helped in kitchen", LabelKey = "PRESET.SYSTEM.HELPED_IN_KITCHEN", ImageValue = "🍳", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new DeedPreset { Id = Guid.Parse("00000000-0000-0000-0000-000000000003"), FamilyId = null, Label = "Cleaned room", LabelKey = "PRESET.SYSTEM.CLEANED_ROOM", ImageValue = "🧹", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new DeedPreset { Id = Guid.Parse("00000000-0000-0000-0000-000000000004"), FamilyId = null, Label = "Helped sibling", LabelKey = "PRESET.SYSTEM.HELPED_SIBLING", ImageValue = "🤝", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new DeedPreset { Id = Guid.Parse("00000000-0000-0000-0000-000000000005"), FamilyId = null, Label = "Behaved all day", LabelKey = "PRESET.SYSTEM.BEHAVED_ALL_DAY", ImageValue = "😊", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new DeedPreset { Id = Guid.Parse("00000000-0000-0000-0000-000000000006"), FamilyId = null, Label = "Made bed", LabelKey = "PRESET.SYSTEM.MADE_BED", ImageValue = "🛏️", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new DeedPreset { Id = Guid.Parse("00000000-0000-0000-0000-000000000007"), FamilyId = null, Label = "Put clothes on", LabelKey = "PRESET.SYSTEM.PUT_CLOTHES_ON", ImageValue = "👕", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new DeedPreset { Id = Guid.Parse("00000000-0000-0000-0000-000000000008"), FamilyId = null, Label = "Put clothes in washing machine", LabelKey = "PRESET.SYSTEM.PUT_CLOTHES_IN_WASHING_MACHINE", ImageValue = "🫧", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new DeedPreset { Id = Guid.Parse("00000000-0000-0000-0000-000000000009"), FamilyId = null, Label = "Put clothes in dryer", LabelKey = "PRESET.SYSTEM.PUT_CLOTHES_IN_DRYER", ImageValue = "🌀", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new DeedPreset { Id = Guid.Parse("00000000-0000-0000-0000-000000000010"), FamilyId = null, Label = "Put plates in dishwasher", LabelKey = "PRESET.SYSTEM.PUT_PLATES_IN_DISHWASHER", ImageValue = "🍽️", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new DeedPreset { Id = Guid.Parse("00000000-0000-0000-0000-000000000011"), FamilyId = null, Label = "Played football", LabelKey = "PRESET.SYSTEM.PLAYED_FOOTBALL", ImageValue = "⚽", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new DeedPreset { Id = Guid.Parse("00000000-0000-0000-0000-000000000012"), FamilyId = null, Label = "Did gymnastics", LabelKey = "PRESET.SYSTEM.DID_GYMNASTICS", ImageValue = "🤸", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new DeedPreset { Id = Guid.Parse("00000000-0000-0000-0000-000000000013"), FamilyId = null, Label = "Went swimming", LabelKey = "PRESET.SYSTEM.WENT_SWIMMING", ImageValue = "🏊", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new DeedPreset { Id = Guid.Parse("00000000-0000-0000-0000-000000000014"), FamilyId = null, Label = "Brushed teeth", LabelKey = "PRESET.SYSTEM.BRUSHED_TEETH", ImageValue = "🦷", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}
