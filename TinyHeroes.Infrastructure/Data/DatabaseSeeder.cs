using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TinyHeroes.Domain.Entities;
using TinyHeroes.Domain.Enums;

namespace TinyHeroes.Infrastructure.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext db, UserManager<User> userManager)
    {
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();

        var user = new User
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            UserName = "testuser@demo.com",
            NormalizedUserName = "TESTUSER@DEMO.COM",
            Email = "testuser@demo.com",
            NormalizedEmail = "TESTUSER@DEMO.COM",
            EmailConfirmed = true,
            DisplayName = "Demo Parent",
            PreferredLanguage = "en",
            CreatedAt = DateTime.UtcNow
        };
        var result = await userManager.CreateAsync(user, "Password1!");
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"Seeding user failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        var familyId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var family = new Family
        {
            Id = familyId,
            Name = "Demo Family",
            WeekStartDay = DayOfWeek.Monday,
            CreatedByUserId = user.Id,
            JoinCode = "DEMO0001",
            CreatedAt = DateTime.UtcNow
        };
        db.Families.Add(family);

        db.FamilyMembers.Add(new FamilyMember
        {
            Id = Guid.NewGuid(),
            FamilyId = familyId,
            UserId = user.Id,
            Role = FamilyRole.Admin,
            JoinedAt = DateTime.UtcNow
        });

        db.Children.Add(new Child
        {
            Id = Guid.NewGuid(),
            FamilyId = familyId,
            Name = "Alice",
            Age = 5,
            Gender = Gender.Girl,
            AvatarEmoji = "🦸",
            CreatedAt = DateTime.UtcNow
        });
        db.Children.Add(new Child
        {
            Id = Guid.NewGuid(),
            FamilyId = familyId,
            Name = "Bob",
            Age = 7,
            Gender = Gender.Boy,
            AvatarEmoji = "🦸",
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }
}
