using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TinyHeroes.Domain.Entities;

namespace TinyHeroes.Infrastructure.Data.Configurations;

public class FamilyInviteConfiguration : IEntityTypeConfiguration<FamilyInvite>
{
    public void Configure(EntityTypeBuilder<FamilyInvite> builder)
    {
        builder.HasOne(i => i.Family).WithMany(f => f.Invites).HasForeignKey(i => i.FamilyId);
        builder.HasIndex(i => i.Token).IsUnique();
        builder.HasIndex(i => i.Email);
        builder.Property(i => i.Token).HasMaxLength(64);
        builder.Property(i => i.Email).HasMaxLength(256);
    }
}
