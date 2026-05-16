using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TinyHeroes.Application.Interfaces;
using TinyHeroes.Domain.Entities;
using TinyHeroes.Infrastructure.Data;

namespace TinyHeroes.Infrastructure.Services;

public class SummaryService(AppDbContext db) : ISummaryService
{
    private record RankingEntry(Guid ChildId, string ChildName, int DeedCount, int Rank);

    public async Task GenerateMissingWeekSummaries(Guid familyId)
    {
        var family = await db.Families.AsNoTracking().FirstAsync(f => f.Id == familyId);

        var earliestDeed = await db.GoodDeeds
            .Where(d => d.Child.FamilyId == familyId)
            .OrderBy(d => d.CreatedAt)
            .Select(d => (DateTime?)d.CreatedAt)
            .FirstOrDefaultAsync();

        var startDate = earliestDeed ?? family.CreatedAt;

        var today = DateTime.UtcNow.Date;

        var firstWeekStart = AlignToWeekStart(startDate, family.WeekStartDay);

        var children = await db.Children
            .Where(c => c.FamilyId == familyId)
            .Select(c => new { c.Id, c.Name })
            .ToListAsync();

        var existingSummaries = await db.WeekSummaries
            .Where(ws => ws.FamilyId == familyId)
            .Select(ws => ws.WeekStart)
            .ToListAsync();

        var existingSet = new HashSet<DateTime>(existingSummaries.Select(d => d.Date));

        var weekStart = firstWeekStart;
        while (true)
        {
            var weekEnd = weekStart.AddDays(7);

            // Only summarize completed weeks (today >= weekEnd)
            if (weekEnd > today)
                break;

            if (!existingSet.Contains(weekStart.Date))
            {
                var deedCounts = await db.GoodDeeds
                    .Where(d => d.Child.FamilyId == familyId
                        && d.CreatedAt >= weekStart
                        && d.CreatedAt < weekEnd)
                    .GroupBy(d => d.ChildId)
                    .Select(g => new { ChildId = g.Key, Count = g.Count() })
                    .ToListAsync();

                var countDict = deedCounts.ToDictionary(x => x.ChildId, x => x.Count);

                var rankings = children
                    .Select(c => new { c.Id, c.Name, DeedCount = countDict.GetValueOrDefault(c.Id, 0) })
                    .OrderByDescending(x => x.DeedCount)
                    .ToList();

                int rank = 0;
                int previousCount = -1;
                var rankedList = new List<RankingEntry>();
                for (int i = 0; i < rankings.Count; i++)
                {
                    if (rankings[i].DeedCount != previousCount)
                    {
                        rank = i + 1;
                        previousCount = rankings[i].DeedCount;
                    }
                    rankedList.Add(new RankingEntry(rankings[i].Id, rankings[i].Name, rankings[i].DeedCount, rank));
                }

                var summary = new WeekSummary
                {
                    Id = Guid.NewGuid(),
                    FamilyId = familyId,
                    WeekStart = weekStart,
                    WeekEnd = weekEnd,
                    Rankings = JsonSerializer.Serialize(rankedList.Select(r => new
                    {
                        childId = r.ChildId,
                        childName = r.ChildName,
                        deedCount = r.DeedCount,
                        rank = r.Rank
                    })),
                    CreatedAt = DateTime.UtcNow
                };

                db.WeekSummaries.Add(summary);
            }

            weekStart = weekEnd;
        }

        await db.SaveChangesAsync();
    }

    public async Task GenerateMissingMonthSummaries(Guid familyId)
    {
        var family = await db.Families.AsNoTracking().FirstAsync(f => f.Id == familyId);

        var earliestDeed = await db.GoodDeeds
            .Where(d => d.Child.FamilyId == familyId)
            .OrderBy(d => d.CreatedAt)
            .Select(d => (DateTime?)d.CreatedAt)
            .FirstOrDefaultAsync();

        var startDate = earliestDeed ?? family.CreatedAt;

        var today = DateTime.UtcNow;

        // Most recent completed month: the month before the current one
        var lastCompletedMonth = new DateTime(today.Year, today.Month, 1).AddDays(-1);
        var lastCompletedYear = lastCompletedMonth.Year;
        var lastCompletedMonthNum = lastCompletedMonth.Month;

        var children = await db.Children
            .Where(c => c.FamilyId == familyId)
            .Select(c => new { c.Id, c.Name })
            .ToListAsync();

        var existingSummaries = await db.MonthSummaries
            .Where(ms => ms.FamilyId == familyId)
            .Select(ms => new { ms.Year, ms.Month })
            .ToListAsync();

        var existingSet = new HashSet<(int Year, int Month)>(
            existingSummaries.Select(s => (s.Year, s.Month)));

        var currentYear = startDate.Year;
        var currentMonth = startDate.Month;

        while (currentYear < lastCompletedYear
            || (currentYear == lastCompletedYear && currentMonth <= lastCompletedMonthNum))
        {
            if (!existingSet.Contains((currentYear, currentMonth)))
            {
                var monthStart = new DateTime(currentYear, currentMonth, 1, 0, 0, 0, DateTimeKind.Utc);
                var nextMonthStart = monthStart.AddMonths(1);

                var deedCounts = await db.GoodDeeds
                    .Where(d => d.Child.FamilyId == familyId
                        && d.CreatedAt >= monthStart
                        && d.CreatedAt < nextMonthStart)
                    .GroupBy(d => d.ChildId)
                    .Select(g => new { ChildId = g.Key, Count = g.Count() })
                    .ToListAsync();

                var countDict = deedCounts.ToDictionary(x => x.ChildId, x => x.Count);

                var rankings = children
                    .Select(c => new { c.Id, c.Name, DeedCount = countDict.GetValueOrDefault(c.Id, 0) })
                    .OrderByDescending(x => x.DeedCount)
                    .ToList();

                int rank = 0;
                int previousCount = -1;
                var rankedList = new List<RankingEntry>();
                for (int i = 0; i < rankings.Count; i++)
                {
                    if (rankings[i].DeedCount != previousCount)
                    {
                        rank = i + 1;
                        previousCount = rankings[i].DeedCount;
                    }
                    rankedList.Add(new RankingEntry(rankings[i].Id, rankings[i].Name, rankings[i].DeedCount, rank));
                }

                var totalDeeds = deedCounts.Sum(x => x.Count);
                var champion = rankings.FirstOrDefault(r => r.DeedCount > 0);

                var summary = new MonthSummary
                {
                    Id = Guid.NewGuid(),
                    FamilyId = familyId,
                    Year = currentYear,
                    Month = currentMonth,
                    ChampionChildId = champion?.Id,
                    ChampionName = champion?.Name,
                    TotalDeeds = totalDeeds,
                    Rankings = JsonSerializer.Serialize(rankedList.Select(r => new
                    {
                        childId = r.ChildId,
                        childName = r.ChildName,
                        deedCount = r.DeedCount,
                        rank = r.Rank
                    })),
                    CreatedAt = DateTime.UtcNow
                };

                db.MonthSummaries.Add(summary);
            }

            // Advance to next month
            currentMonth++;
            if (currentMonth > 12)
            {
                currentMonth = 1;
                currentYear++;
            }
        }

        await db.SaveChangesAsync();
    }

    private static DateTime AlignToWeekStart(DateTime date, DayOfWeek weekStartDay)
    {
        var daysSinceStart = ((int)date.DayOfWeek - (int)weekStartDay + 7) % 7;
        return date.AddDays(-daysSinceStart).Date;
    }
}
