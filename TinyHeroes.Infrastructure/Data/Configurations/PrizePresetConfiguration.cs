using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TinyHeroes.Domain.Entities;

namespace TinyHeroes.Infrastructure.Data.Configurations;

public class PrizePresetConfiguration : IEntityTypeConfiguration<PrizePreset>
{
    public void Configure(EntityTypeBuilder<PrizePreset> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Label).HasMaxLength(200);
        builder.Property(p => p.Emoji).HasMaxLength(50);
        builder.HasOne(p => p.Family).WithMany().HasForeignKey(p => p.FamilyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(p => p.FamilyId);

        builder.HasData(
            new PrizePreset { Id = Guid.Parse("10000000-0000-0000-0000-000000000001"), FamilyId = null, Label = "Pizza night", Emoji = "🍕", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PrizePreset { Id = Guid.Parse("10000000-0000-0000-0000-000000000002"), FamilyId = null, Label = "Ice cream", Emoji = "🍦", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PrizePreset { Id = Guid.Parse("10000000-0000-0000-0000-000000000003"), FamilyId = null, Label = "Cupcake", Emoji = "🧁", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PrizePreset { Id = Guid.Parse("10000000-0000-0000-0000-000000000004"), FamilyId = null, Label = "Movie night", Emoji = "🍿", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PrizePreset { Id = Guid.Parse("10000000-0000-0000-0000-000000000005"), FamilyId = null, Label = "Extra screen time", Emoji = "🎮", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PrizePreset { Id = Guid.Parse("10000000-0000-0000-0000-000000000006"), FamilyId = null, Label = "Game night", Emoji = "🎲", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PrizePreset { Id = Guid.Parse("10000000-0000-0000-0000-000000000007"), FamilyId = null, Label = "Late bedtime", Emoji = "🌙", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PrizePreset { Id = Guid.Parse("10000000-0000-0000-0000-000000000008"), FamilyId = null, Label = "Bubble bath", Emoji = "🛁", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PrizePreset { Id = Guid.Parse("10000000-0000-0000-0000-000000000009"), FamilyId = null, Label = "Story choice", Emoji = "📖", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PrizePreset { Id = Guid.Parse("10000000-0000-0000-0000-00000000000a"), FamilyId = null, Label = "Skip a chore", Emoji = "🧹", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PrizePreset { Id = Guid.Parse("10000000-0000-0000-0000-00000000000b"), FamilyId = null, Label = "Amusement park", Emoji = "🎡", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PrizePreset { Id = Guid.Parse("10000000-0000-0000-0000-00000000000c"), FamilyId = null, Label = "Cake slice", Emoji = "🍰", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}
