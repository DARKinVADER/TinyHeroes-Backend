using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TinyHeroes.Application.DTOs.Summary;
using TinyHeroes.Application.Interfaces;
using TinyHeroes.Infrastructure.Data;

namespace TinyHeroes.Api.Controllers;

[ApiController]
[Route("api/summaries")]
[Authorize]
public class SummaryController(AppDbContext db, ISummaryService summaryService) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

    [HttpGet("weeks")]
    public async Task<ActionResult<List<WeekSummaryResponse>>> GetWeeks()
    {
        var userId = GetUserId();
        var membership = await db.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
        if (membership is null) return BadRequest("User does not belong to a family.");

        await summaryService.GenerateMissingWeekSummaries(membership.FamilyId);

        var summaries = await db.WeekSummaries
            .Where(ws => ws.FamilyId == membership.FamilyId)
            .OrderByDescending(ws => ws.WeekStart)
            .ToListAsync();

        var response = summaries.Select(ws => new WeekSummaryResponse(
            ws.Id,
            ws.WeekStart,
            ws.WeekEnd,
            JsonSerializer.Deserialize<List<RankingEntry>>(ws.Rankings, JsonOptions) ?? []
        )).ToList();

        return Ok(response);
    }

    [HttpGet("months")]
    public async Task<ActionResult<List<MonthSummaryResponse>>> GetMonths()
    {
        var userId = GetUserId();
        var membership = await db.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
        if (membership is null) return BadRequest("User does not belong to a family.");

        await summaryService.GenerateMissingMonthSummaries(membership.FamilyId);

        var summaries = await db.MonthSummaries
            .Where(ms => ms.FamilyId == membership.FamilyId)
            .OrderByDescending(ms => ms.Year)
            .ThenByDescending(ms => ms.Month)
            .ToListAsync();

        var response = summaries.Select(ms => new MonthSummaryResponse(
            ms.Id,
            ms.Year,
            ms.Month,
            ms.ChampionName,
            ms.ChampionChildId,
            ms.TotalDeeds,
            JsonSerializer.Deserialize<List<RankingEntry>>(ms.Rankings, JsonOptions) ?? []
        )).ToList();

        return Ok(response);
    }

    [HttpGet("current-month")]
    public async Task<ActionResult<CurrentMonthResponse>> GetCurrentMonth()
    {
        var userId = GetUserId();
        var membership = await db.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
        if (membership is null) return BadRequest("User does not belong to a family.");

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var children = await db.Children
            .Where(c => c.FamilyId == membership.FamilyId)
            .Select(c => new { c.Id, c.Name })
            .ToListAsync();

        var deedCounts = await db.GoodDeeds
            .Where(d => d.Child.FamilyId == membership.FamilyId
                && d.CreatedAt >= monthStart)
            .GroupBy(d => d.ChildId)
            .Select(g => new { ChildId = g.Key, Count = g.Count() })
            .ToListAsync();

        var countDict = deedCounts.ToDictionary(x => x.ChildId, x => x.Count);

        var ranked = children
            .Select(c => new { c.Id, c.Name, DeedCount = countDict.GetValueOrDefault(c.Id, 0) })
            .OrderByDescending(x => x.DeedCount)
            .ToList();

        int rank = 0;
        int previousCount = -1;
        var rankings = new List<RankingEntry>();
        for (int i = 0; i < ranked.Count; i++)
        {
            if (ranked[i].DeedCount != previousCount)
            {
                rank = i + 1;
                previousCount = ranked[i].DeedCount;
            }
            rankings.Add(new RankingEntry(ranked[i].Id, ranked[i].Name, ranked[i].DeedCount, rank));
        }

        return Ok(new CurrentMonthResponse(now.Year, now.Month, rankings));
    }
}
