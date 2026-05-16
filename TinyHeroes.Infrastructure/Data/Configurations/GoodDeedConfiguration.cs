using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TinyHeroes.Domain.Entities;

namespace TinyHeroes.Infrastructure.Data.Configurations;

public class GoodDeedConfiguration : IEntityTypeConfiguration<GoodDeed>
{
    public void Configure(EntityTypeBuilder<GoodDeed> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Description).HasMaxLength(500);
        builder.Property(d => d.ImageType).HasMaxLength(50);
        builder.Property(d => d.ImageValue).HasMaxLength(50);
        builder.HasOne(d => d.Child).WithMany(c => c.Deeds).HasForeignKey(d => d.ChildId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(d => d.AddedBy).WithMany().HasForeignKey(d => d.AddedByUserId).OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(d => d.ChildId);
        builder.HasIndex(d => d.CreatedAt);
    }
}
