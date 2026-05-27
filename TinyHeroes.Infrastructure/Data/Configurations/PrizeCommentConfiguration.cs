// backend/TinyHeroes.Infrastructure/Data/Configurations/PrizeCommentConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TinyHeroes.Domain.Entities;

namespace TinyHeroes.Infrastructure.Data.Configurations;

public class PrizeCommentConfiguration : IEntityTypeConfiguration<PrizeComment>
{
    public void Configure(EntityTypeBuilder<PrizeComment> builder)
    {
        builder.HasKey(p => p.Id);
        builder.HasOne(p => p.PrizeClaim).WithMany(c => c.Comments).HasForeignKey(p => p.PrizeClaimId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(p => p.Text).HasMaxLength(1000);
    }
}
