using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TinyHeroes.Domain.Entities;
using TinyHeroes.Infrastructure.Data;
using TinyHeroes.Infrastructure.Services;

namespace TinyHeroes.Tests.Unit;

public class SummaryServiceTests
{
    // ---------- helpers ----------

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    /// <summary>
    /// Creates a family that started far in the past so its complete weeks / months
    /// are unambiguously in the past relative to the test run.
    /// </summary>
    private static Family CreateFamily(AppDbContext db, DayOfWeek weekStartDay = DayOfWeek.Monday)
    {
        var family = new Family
        {
            Id = Guid.NewGuid(),
            Name = "Test Family",
            WeekStartDay = weekStartDay,
            CreatedByUserId = Guid.NewGuid(),
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            JoinCode = "TEST01"
        };
        db.Families.Add(family);
        db.SaveChanges();
        return family;
    }

    private static Child CreateChild(AppDbContext db, Guid familyId, string name)
    {
        var child = new Child
        {
            Id = Guid.NewGuid(),
            FamilyId = familyId,
            Name = name,
            Age = 8,
            Gender = Domain.Enums.Gender.Boy,
            AvatarEmoji = "🦸"
        };
        db.Children.Add(child);
        db.SaveChanges();
        return child;
    }

    private static void AddDeed(AppDbContext db, Guid childId, DateTime createdAt)
    {
        db.GoodDeeds.Add(new GoodDeed
        {
            Id = Guid.NewGuid(),
            ChildId = childId,
            // EF InMemory does not enforce FK constraints; Guid.Empty is safe here.
            AddedByUserId = Guid.Empty,
            Description = "Good deed",
            CreatedAt = createdAt
        });
        db.SaveChanges();
    }

    private record RankEntry(Guid childId, string childName, int deedCount, int rank);

    private static List<RankEntry> DeserializeRankings(string json) =>
        JsonSerializer.Deserialize<List<RankEntry>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

    // ---------- week summary tests ----------

    [Fact]
    public async Task GenerateMissingWeekSummaries_SingleChild_GetsRankOne()
    {
        await using var db = CreateDb();
        var family = CreateFamily(db);
        var alice = CreateChild(db, family.Id, "Alice");

        // Add a deed in a fully completed week: week of 2024-01-08 (Mon–Sun)
        AddDeed(db, alice.Id, new DateTime(2024, 1, 10, 12, 0, 0, DateTimeKind.Utc));

        var sut = new SummaryService(db);
        await sut.GenerateMissingWeekSummaries(family.Id);

        var summaries = await db.WeekSummaries.Where(ws => ws.FamilyId == family.Id).ToListAsync();
        summaries.Should().NotBeEmpty();

        // Find the summary that covers 2024-01-08
        var target = summaries.First(s => s.WeekStart.Date == new DateTime(2024, 1, 8));
        var rankings = DeserializeRankings(target.Rankings);
        rankings.Should().HaveCount(1);
        rankings[0].rank.Should().Be(1);
        rankings[0].childId.Should().Be(alice.Id);
    }

    [Fact]
    public async Task GenerateMissingWeekSummaries_TwoChildren_MoreDeedsGetsRankOne()
    {
        await using var db = CreateDb();
        var family = CreateFamily(db);
        var alice = CreateChild(db, family.Id, "Alice");
        var bob = CreateChild(db, family.Id, "Bob");

        // Week of 2024-01-08: Alice gets 3 deeds, Bob gets 1
        var weekDay = new DateTime(2024, 1, 10, 12, 0, 0, DateTimeKind.Utc);
        AddDeed(db, alice.Id, weekDay);
        AddDeed(db, alice.Id, weekDay.AddHours(1));
        AddDeed(db, alice.Id, weekDay.AddHours(2));
        AddDeed(db, bob.Id, weekDay.AddHours(3));

        var sut = new SummaryService(db);
        await sut.GenerateMissingWeekSummaries(family.Id);

        var target = await db.WeekSummaries
            .Where(ws => ws.FamilyId == family.Id && ws.WeekStart == new DateTime(2024, 1, 8, 0, 0, 0, DateTimeKind.Utc))
            .FirstAsync();

        var rankings = DeserializeRankings(target.Rankings);
        var aliceEntry = rankings.First(r => r.childId == alice.Id);
        var bobEntry = rankings.First(r => r.childId == bob.Id);

        aliceEntry.rank.Should().Be(1);
        aliceEntry.deedCount.Should().Be(3);
        bobEntry.rank.Should().Be(2);
        bobEntry.deedCount.Should().Be(1);
    }

    [Fact]
    public async Task GenerateMissingWeekSummaries_TiedChildren_GetSameRank()
    {
        // The RankingHelper assigns equal ranks for tied deed counts.
        // It does NOT apply alphabetical tiebreaking — both children receive rank 1.
        await using var db = CreateDb();
        var family = CreateFamily(db);
        var alice = CreateChild(db, family.Id, "Alice");
        var bob = CreateChild(db, family.Id, "Bob");

        var weekDay = new DateTime(2024, 1, 10, 12, 0, 0, DateTimeKind.Utc);
        AddDeed(db, alice.Id, weekDay);
        AddDeed(db, bob.Id, weekDay.AddHours(1));

        var sut = new SummaryService(db);
        await sut.GenerateMissingWeekSummaries(family.Id);

        var target = await db.WeekSummaries
            .Where(ws => ws.FamilyId == family.Id && ws.WeekStart == new DateTime(2024, 1, 8, 0, 0, 0, DateTimeKind.Utc))
            .FirstAsync();

        var rankings = DeserializeRankings(target.Rankings);
        var aliceEntry = rankings.First(r => r.childId == alice.Id);
        var bobEntry = rankings.First(r => r.childId == bob.Id);

        aliceEntry.rank.Should().Be(1, "tied children both receive rank 1");
        bobEntry.rank.Should().Be(1, "tied children both receive rank 1");
        aliceEntry.deedCount.Should().Be(bobEntry.deedCount);
    }

    [Fact]
    public async Task GenerateMissingWeekSummaries_DoesNotGenerateSummaryForCurrentIncompleteWeek()
    {
        await using var db = CreateDb();
        var family = CreateFamily(db);
        var alice = CreateChild(db, family.Id, "Alice");

        // Add a deed dated today (inside the still-in-progress current week)
        AddDeed(db, alice.Id, DateTime.UtcNow);

        var sut = new SummaryService(db);
        await sut.GenerateMissingWeekSummaries(family.Id);

        // The current week end (weekStart + 7 days) is in the future, so no summary should be created
        // for any week whose WeekEnd > today.
        var today = DateTime.UtcNow.Date;
        var summaries = await db.WeekSummaries
            .Where(ws => ws.FamilyId == family.Id && ws.WeekEnd.Date > today)
            .ToListAsync();

        summaries.Should().BeEmpty("the current incomplete week must never produce a summary");
    }

    [Fact]
    public async Task GenerateMissingWeekSummaries_IsIdempotent()
    {
        await using var db = CreateDb();
        var family = CreateFamily(db);
        var alice = CreateChild(db, family.Id, "Alice");

        AddDeed(db, alice.Id, new DateTime(2024, 1, 10, 12, 0, 0, DateTimeKind.Utc));

        var sut = new SummaryService(db);
        await sut.GenerateMissingWeekSummaries(family.Id);

        var countAfterFirst = await db.WeekSummaries.CountAsync(ws => ws.FamilyId == family.Id);

        // Second call must not create duplicates
        await sut.GenerateMissingWeekSummaries(family.Id);

        var countAfterSecond = await db.WeekSummaries.CountAsync(ws => ws.FamilyId == family.Id);
        countAfterSecond.Should().Be(countAfterFirst, "calling the method twice must not insert duplicate summaries");
    }

    // ---------- month summary tests ----------

    [Fact]
    public async Task GenerateMissingMonthSummaries_ChampionIsChildWithMostDeeds()
    {
        await using var db = CreateDb();
        var family = CreateFamily(db);
        var alice = CreateChild(db, family.Id, "Alice");
        var bob = CreateChild(db, family.Id, "Bob");

        // Both deeds are in January 2024 (a completed month)
        var jan = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        AddDeed(db, alice.Id, jan);
        AddDeed(db, alice.Id, jan.AddHours(1));
        AddDeed(db, bob.Id, jan.AddHours(2));

        var sut = new SummaryService(db);
        await sut.GenerateMissingMonthSummaries(family.Id);

        var janSummary = await db.MonthSummaries
            .FirstAsync(ms => ms.FamilyId == family.Id && ms.Year == 2024 && ms.Month == 1);

        janSummary.ChampionChildId.Should().Be(alice.Id, "Alice has the most deeds");
        janSummary.ChampionName.Should().Be("Alice");
        janSummary.TotalDeeds.Should().Be(3);
    }

    [Fact]
    public async Task GenerateMissingMonthSummaries_DoesNotGenerateSummaryForCurrentMonth()
    {
        await using var db = CreateDb();
        var family = CreateFamily(db);
        var alice = CreateChild(db, family.Id, "Alice");

        // Deed dated today — falls inside the current, still-open month
        AddDeed(db, alice.Id, DateTime.UtcNow);

        var sut = new SummaryService(db);
        await sut.GenerateMissingMonthSummaries(family.Id);

        var today = DateTime.UtcNow;
        var currentMonthSummary = await db.MonthSummaries
            .FirstOrDefaultAsync(ms => ms.FamilyId == family.Id
                                       && ms.Year == today.Year
                                       && ms.Month == today.Month);

        currentMonthSummary.Should().BeNull("the current in-progress month must never produce a summary");
    }
}
