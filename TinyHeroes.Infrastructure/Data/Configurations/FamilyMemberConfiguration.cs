using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TinyHeroes.Domain.Entities;

namespace TinyHeroes.Infrastructure.Data.Configurations;

public class FamilyMemberConfiguration : IEntityTypeConfiguration<FamilyMember>
{
    public void Configure(EntityTypeBuilder<FamilyMember> builder)
    {
        builder.HasOne(m => m.Family).WithMany(f => f.Members).HasForeignKey(m => m.FamilyId);
        builder.HasOne(m => m.User).WithMany(u => u.FamilyMemberships).HasForeignKey(m => m.UserId);
        builder.HasIndex(m => new { m.FamilyId, m.UserId }).IsUnique();
        builder.HasIndex(m => m.UserId).IsUnique();
    }
}
