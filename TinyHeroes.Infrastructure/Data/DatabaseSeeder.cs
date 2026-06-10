using System.Text.Json;
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

        // ── User ──────────────────────────────────────────────────────────────
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

        // ── Family ────────────────────────────────────────────────────────────
        var familyId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var family = new Family
        {
            Id = familyId,
            Name = "Demo Family",
            WeekStartDay = DayOfWeek.Monday,
            WeeklyMinDeeds = 3,
            MonthlyMinDeeds = 12,
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

        // ── Children ──────────────────────────────────────────────────────────
        var aliceId = Guid.Parse("00000000-0000-0000-0000-000000000010");
        var bobId   = Guid.Parse("00000000-0000-0000-0000-000000000011");
        var claraId = Guid.Parse("00000000-0000-0000-0000-000000000012");
        var danId   = Guid.Parse("00000000-0000-0000-0000-000000000013");

        db.Children.AddRange(
            new Child { Id = aliceId, FamilyId = familyId, Name = "Alice", Age = 5,  Gender = Gender.Girl, AvatarEmoji = "🦸‍♀️", CreatedAt = DateTime.UtcNow },
            new Child { Id = bobId,   FamilyId = familyId, Name = "Bob",   Age = 7,  Gender = Gender.Boy,  AvatarEmoji = "🦸‍♂️", CreatedAt = DateTime.UtcNow },
            new Child { Id = claraId, FamilyId = familyId, Name = "Clara", Age = 9,  Gender = Gender.Girl, AvatarEmoji = "🧝‍♀️", CreatedAt = DateTime.UtcNow },
            new Child { Id = danId,   FamilyId = familyId, Name = "Dan",   Age = 11, Gender = Gender.Boy,  AvatarEmoji = "🧙‍♂️", CreatedAt = DateTime.UtcNow }
        );

        // ── Deeds ─────────────────────────────────────────────────────────────
        // Three past Mondays + the current week start (computed at seed time)
        var week1 = new DateTime(2026, 5, 19, 0, 0, 0, DateTimeKind.Utc); // Mon 19 May
        var week2 = new DateTime(2026, 5, 26, 0, 0, 0, DateTimeKind.Utc); // Mon 26 May
        var week3 = new DateTime(2026, 6,  2, 0, 0, 0, DateTimeKind.Utc); // Mon  2 Jun
        var today = DateTime.UtcNow.Date;
        var daysSinceMonday = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        var currentWeekStart = new DateTime(today.AddDays(-daysSinceMonday).Year,
                                            today.AddDays(-daysSinceMonday).Month,
                                            today.AddDays(-daysSinceMonday).Day,
                                            0, 0, 0, DateTimeKind.Utc);

        void AddDeeds(Guid childId, int count, DateTime weekStart, string desc, string emoji)
        {
            for (var i = 0; i < count; i++)
                db.GoodDeeds.Add(new GoodDeed
                {
                    Id = Guid.NewGuid(),
                    ChildId = childId,
                    AddedByUserId = user.Id,
                    Description = desc,
                    ImageType = "emoji",
                    ImageValue = emoji,
                    CreatedAt = weekStart.AddHours(i + 1)
                });
        }

        // Week 1 (19 May): Clara=7, Alice=5, Bob=3, Dan=2 → ranks 1,2,3,4
        AddDeeds(aliceId, 5, week1, "Helped with dishes",  "🍽️");
        AddDeeds(bobId,   3, week1, "Tidied bedroom",       "🛏️");
        AddDeeds(claraId, 7, week1, "Read a book",          "📚");
        AddDeeds(danId,   2, week1, "Took out the trash",   "🗑️");

        // Week 2 (26 May): Bob=6, Dan=5, Alice=4, Clara=3 → ranks 1,2,3,4
        AddDeeds(aliceId, 4, week2, "Watered plants",       "🌱");
        AddDeeds(bobId,   6, week2, "Helped set the table", "🍴");
        AddDeeds(claraId, 3, week2, "Practiced piano",      "🎹");
        AddDeeds(danId,   5, week2, "Fed the cat",          "🐱");

        // Week 3 (2 Jun): Dan=8, Clara=6, Alice=4, Bob=4 → ranks 1,2,3,3 (Alice & Bob tied)
        AddDeeds(aliceId, 4, week3, "Made the bed",          "🛏️");
        AddDeeds(bobId,   4, week3, "Sorted recycling",      "♻️");
        AddDeeds(claraId, 6, week3, "Helped with groceries", "🛒");
        AddDeeds(danId,   8, week3, "Cleaned the bathroom",  "🚿");

        // Current week (dynamic): in-progress deeds, no summary yet
        AddDeeds(aliceId, 3, currentWeekStart, "Helped with dishes",   "🍽️");
        AddDeeds(bobId,   5, currentWeekStart, "Walked the dog",        "🐕");
        AddDeeds(claraId, 2, currentWeekStart, "Set the table",         "🍴");
        AddDeeds(danId,   6, currentWeekStart, "Vacuumed the living room", "🧹");

        // ── Week summaries ────────────────────────────────────────────────────
        var weekSummary1Id = Guid.Parse("00000000-0000-0000-0000-000000000020");
        var weekSummary2Id = Guid.Parse("00000000-0000-0000-0000-000000000021");
        var weekSummary3Id = Guid.Parse("00000000-0000-0000-0000-000000000022");

        db.WeekSummaries.AddRange(
            new WeekSummary
            {
                Id = weekSummary1Id,
                FamilyId = familyId,
                WeekStart = week1,
                WeekEnd = week1.AddDays(7),
                Rankings = JsonSerializer.Serialize(new[]
                {
                    new { childId = claraId, childName = "Clara", deedCount = 7, rank = 1 },
                    new { childId = aliceId, childName = "Alice", deedCount = 5, rank = 2 },
                    new { childId = bobId,   childName = "Bob",   deedCount = 3, rank = 3 },
                    new { childId = danId,   childName = "Dan",   deedCount = 2, rank = 4 }
                }),
                CreatedAt = week1.AddDays(7)
            },
            new WeekSummary
            {
                Id = weekSummary2Id,
                FamilyId = familyId,
                WeekStart = week2,
                WeekEnd = week2.AddDays(7),
                Rankings = JsonSerializer.Serialize(new[]
                {
                    new { childId = bobId,   childName = "Bob",   deedCount = 6, rank = 1 },
                    new { childId = danId,   childName = "Dan",   deedCount = 5, rank = 2 },
                    new { childId = aliceId, childName = "Alice", deedCount = 4, rank = 3 },
                    new { childId = claraId, childName = "Clara", deedCount = 3, rank = 4 }
                }),
                CreatedAt = week2.AddDays(7)
            },
            new WeekSummary
            {
                Id = weekSummary3Id,
                FamilyId = familyId,
                WeekStart = week3,
                WeekEnd = week3.AddDays(7),
                Rankings = JsonSerializer.Serialize(new[]
                {
                    new { childId = danId,   childName = "Dan",   deedCount = 8, rank = 1 },
                    new { childId = claraId, childName = "Clara", deedCount = 6, rank = 2 },
                    new { childId = aliceId, childName = "Alice", deedCount = 4, rank = 3 },
                    new { childId = bobId,   childName = "Bob",   deedCount = 4, rank = 3 }  // tied — no rank 4
                }),
                CreatedAt = week3.AddDays(7)
            }
        );

        // ── Month summary — May 2026 ───────────────────────────────────────────
        // Weeks 1 & 2 are in May; week3 starts 2 Jun.
        // May totals: Clara=7+3=10, Alice=5+4=9, Bob=3+6=9, Dan=2+5=7
        var monthSummary1Id = Guid.Parse("00000000-0000-0000-0000-000000000030");

        db.MonthSummaries.Add(new MonthSummary
        {
            Id = monthSummary1Id,
            FamilyId = familyId,
            Year = 2026,
            Month = 5,
            ChampionChildId = claraId,
            ChampionName = "Clara",
            TotalDeeds = 35,
            Rankings = JsonSerializer.Serialize(new[]
            {
                new { childId = claraId, childName = "Clara", deedCount = 10, rank = 1 },
                new { childId = aliceId, childName = "Alice", deedCount = 9,  rank = 2 },
                new { childId = bobId,   childName = "Bob",   deedCount = 9,  rank = 2 },
                new { childId = danId,   childName = "Dan",   deedCount = 7,  rank = 4 }
            }),
            CreatedAt = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        // ── Prize assignments (standing prize structure) ───────────────────────
        db.PrizeAssignments.AddRange(
            new PrizeAssignment { Id = Guid.NewGuid(), FamilyId = familyId, Scope = "weekly",  Rank = 1,    Emoji = "🥇", Label = "Gold Medal",              UpdatedAt = DateTime.UtcNow },
            new PrizeAssignment { Id = Guid.NewGuid(), FamilyId = familyId, Scope = "weekly",  Rank = 2,    Emoji = "🥈", Label = "Silver Medal",            UpdatedAt = DateTime.UtcNow },
            new PrizeAssignment { Id = Guid.NewGuid(), FamilyId = familyId, Scope = "weekly",  Rank = 3,    Emoji = "🥉", Label = "Bronze Medal",            UpdatedAt = DateTime.UtcNow },
            new PrizeAssignment { Id = Guid.NewGuid(), FamilyId = familyId, Scope = "monthly", Rank = null, Emoji = "🏆", Label = "Monthly Champion Trophy", UpdatedAt = DateTime.UtcNow }
        );

        // ── Prize claims ──────────────────────────────────────────────────────
        var claimW1R1Id = Guid.Parse("00000000-0000-0000-0000-000000000040");
        var claimW1R2Id = Guid.Parse("00000000-0000-0000-0000-000000000041");
        var claimW1R3Id = Guid.Parse("00000000-0000-0000-0000-000000000042");
        var claimW2R1Id = Guid.Parse("00000000-0000-0000-0000-000000000043");
        var claimMay1Id = Guid.Parse("00000000-0000-0000-0000-000000000050");

        db.PrizeClaims.AddRange(
            // Week 1 — rank 1: Clara (used), rank 2: Alice (used), rank 3: Bob (not used)
            new PrizeClaim
            {
                Id = claimW1R1Id, FamilyId = familyId, Scope = "weekly",
                WeekSummaryId = weekSummary1Id, Rank = 1,
                ChildId = claraId, ChildName = "Clara",
                PrizeEmoji = "🥇", PrizeLabel = "Gold Medal",
                IsUsed = true, UsedAt = week1.AddDays(8),
                CreatedAt = week1.AddDays(7)
            },
            new PrizeClaim
            {
                Id = claimW1R2Id, FamilyId = familyId, Scope = "weekly",
                WeekSummaryId = weekSummary1Id, Rank = 2,
                ChildId = aliceId, ChildName = "Alice",
                PrizeEmoji = "🥈", PrizeLabel = "Silver Medal",
                IsUsed = true, UsedAt = week1.AddDays(9),
                CreatedAt = week1.AddDays(7)
            },
            new PrizeClaim
            {
                Id = claimW1R3Id, FamilyId = familyId, Scope = "weekly",
                WeekSummaryId = weekSummary1Id, Rank = 3,
                ChildId = bobId, ChildName = "Bob",
                PrizeEmoji = "🥉", PrizeLabel = "Bronze Medal",
                IsUsed = false,
                CreatedAt = week1.AddDays(7)
            },
            // Week 2 — rank 1: Bob (not used)
            new PrizeClaim
            {
                Id = claimW2R1Id, FamilyId = familyId, Scope = "weekly",
                WeekSummaryId = weekSummary2Id, Rank = 1,
                ChildId = bobId, ChildName = "Bob",
                PrizeEmoji = "🥇", PrizeLabel = "Gold Medal",
                IsUsed = false,
                CreatedAt = week2.AddDays(7)
            },
            // May monthly — Clara champion (used)
            new PrizeClaim
            {
                Id = claimMay1Id, FamilyId = familyId, Scope = "monthly",
                MonthSummaryId = monthSummary1Id, Rank = null,
                ChildId = claraId, ChildName = "Clara",
                PrizeEmoji = "🏆", PrizeLabel = "Monthly Champion Trophy",
                IsUsed = true, UsedAt = new DateTime(2026, 6, 2, 10, 0, 0, DateTimeKind.Utc),
                CreatedAt = new DateTime(2026, 6, 1, 0, 30, 0, DateTimeKind.Utc)
            }
        );

        // ── Prize comments ────────────────────────────────────────────────────
        db.PrizeComments.AddRange(
            new PrizeComment { Id = Guid.NewGuid(), PrizeClaimId = claimW1R1Id, Text = "Well done Clara, you earned it! 🎉",      CreatedAt = week1.AddDays(7).AddHours(2) },
            new PrizeComment { Id = Guid.NewGuid(), PrizeClaimId = claimW1R1Id, Text = "Can I choose an ice cream? 🍦",            CreatedAt = week1.AddDays(7).AddHours(4) },
            new PrizeComment { Id = Guid.NewGuid(), PrizeClaimId = claimW1R2Id, Text = "Great job Alice! 🌟",                      CreatedAt = week1.AddDays(8).AddHours(1) },
            new PrizeComment { Id = Guid.NewGuid(), PrizeClaimId = claimW2R1Id, Text = "Amazing week Bob! 💪",                     CreatedAt = week2.AddDays(7).AddHours(1) },
            new PrizeComment { Id = Guid.NewGuid(), PrizeClaimId = claimMay1Id, Text = "Clara is the champion of May! 🏆",          CreatedAt = new DateTime(2026, 6, 1,  1, 0, 0, DateTimeKind.Utc) },
            new PrizeComment { Id = Guid.NewGuid(), PrizeClaimId = claimMay1Id, Text = "I want to go to the theme park as my prize!", CreatedAt = new DateTime(2026, 6, 1,  2, 0, 0, DateTimeKind.Utc) },
            new PrizeComment { Id = Guid.NewGuid(), PrizeClaimId = claimMay1Id, Text = "Done! We went on the 2nd 🎢",               CreatedAt = new DateTime(2026, 6, 3,  0, 0, 0, DateTimeKind.Utc) }
        );

        // ── Family-scoped prize presets ───────────────────────────────────────
        db.PrizePresets.AddRange(
            new PrizePreset { Id = Guid.NewGuid(), FamilyId = familyId, Label = "Extra screen time",  Emoji = "📱", Enabled = true,  CreatedAt = DateTime.UtcNow },
            new PrizePreset { Id = Guid.NewGuid(), FamilyId = familyId, Label = "Choose dinner",       Emoji = "🍕", Enabled = true,  CreatedAt = DateTime.UtcNow },
            new PrizePreset { Id = Guid.NewGuid(), FamilyId = familyId, Label = "Bedtime 30 min late", Emoji = "🌙", Enabled = true,  CreatedAt = DateTime.UtcNow },
            new PrizePreset { Id = Guid.NewGuid(), FamilyId = familyId, Label = "Cinema trip",         Emoji = "🎬", Enabled = false, CreatedAt = DateTime.UtcNow }
        );

        await db.SaveChangesAsync();
    }
}
