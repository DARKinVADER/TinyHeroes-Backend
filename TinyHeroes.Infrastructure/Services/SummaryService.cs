using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TinyHeroes.Application.Helpers;
using TinyHeroes.Application.Interfaces;
using TinyHeroes.Infrastructure.Data;

namespace TinyHeroes.Infrastructure.Services;

public class SummaryService(AppDbContext db) : ISummaryService
{
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

        // Early-exit: if we already have a summary for the most recent completed week, nothing to do.
        var currentWeekStart = AlignToWeekStart(today, family.WeekStartDay);
        var latestStored = existingSet.Count > 0 ? existingSet.Max() : DateTime.MinValue;
        if (latestStored >= currentWeekStart.AddDays(-7))
            return;

        var weekStart = firstWeekStart;
        while (true)
        {
            var weekEnd = weekStart.AddDays(7);
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

                var rankedList = RankingHelper.Rank(children.Select(c =>
                    (c.Id, c.Name, DeedCount: countDict.GetValueOrDefault(c.Id, 0))));

                var summary = new Domain.Entities.WeekSummary
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

        // Early-exit: if we already have the most recent completed month, nothing to do.
        if (existingSet.Contains((lastCompletedYear, lastCompletedMonthNum)))
            return;

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

                var rankedList = RankingHelper.Rank(children.Select(c =>
                    (c.Id, c.Name, DeedCount: countDict.GetValueOrDefault(c.Id, 0))));

                var totalDeeds = deedCounts.Sum(x => x.Count);
                var champion = rankedList.FirstOrDefault(r => r.DeedCount > 0);

                var summary = new Domain.Entities.MonthSummary
                {
                    Id = Guid.NewGuid(),
                    FamilyId = familyId,
                    Year = currentYear,
                    Month = currentMonth,
                    ChampionChildId = champion?.ChildId,
                    ChampionName = champion?.ChildName,
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
