using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TinyHeroes.Domain.Entities;

namespace TinyHeroes.Infrastructure.Data.Configurations;

public class ChildConfiguration : IEntityTypeConfiguration<Child>
{
    public void Configure(EntityTypeBuilder<Child> builder)
    {
        builder.HasOne(c => c.Family).WithMany(f => f.Children).HasForeignKey(c => c.FamilyId);
        builder.Property(c => c.Name).HasMaxLength(100);
        builder.HasIndex(c => c.FamilyId);
    }
}
