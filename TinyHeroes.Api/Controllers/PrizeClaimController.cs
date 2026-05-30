// backend/TinyHeroes.Api/Controllers/PrizeClaimController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TinyHeroes.Application.DTOs.Prize;
using TinyHeroes.Domain.Entities;
using TinyHeroes.Infrastructure.Data;

namespace TinyHeroes.Api.Controllers;

[ApiController]
[Route("api/prize-claims")]
[Authorize]
public class PrizeClaimController(AppDbContext db) : ApiControllerBase
{

    private static PrizeClaimDto ToDto(PrizeClaim c) => new(
        c.Id, c.Scope, c.WeekSummaryId, c.MonthSummaryId, c.Rank,
        c.ChildId, c.ChildName, c.PrizeEmoji, c.PrizeLabel,
        c.IsUsed, c.UsedAt, c.CreatedAt,
        c.Comments.OrderBy(x => x.CreatedAt).Select(x => new PrizeCommentDto(x.Id, x.Text, x.CreatedAt)).ToList()
    );

    [HttpGet]
    public async Task<ActionResult<List<PrizeClaimDto>>> List([FromQuery] Guid? weekSummaryId, [FromQuery] Guid? monthSummaryId)
    {
        var userId = GetUserId();
        var membership = await db.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
        if (membership is null) return BadRequest("User does not belong to a family.");

        var query = db.PrizeClaims
            .Include(c => c.Comments)
            .Where(c => c.FamilyId == membership.FamilyId);

        if (weekSummaryId.HasValue)
            query = query.Where(c => c.WeekSummaryId == weekSummaryId);
        else if (monthSummaryId.HasValue)
            query = query.Where(c => c.MonthSummaryId == monthSummaryId);
        else
            return BadRequest("Provide weekSummaryId or monthSummaryId.");

        var claims = await query.ToListAsync();
        return Ok(claims.Select(ToDto).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<PrizeClaimDto>> Create(CreatePrizeClaimRequest req)
    {
        var userId = GetUserId();
        var membership = await db.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
        if (membership is null) return BadRequest("User does not belong to a family.");

        var existing = await db.PrizeClaims
            .Include(c => c.Comments)
            .FirstOrDefaultAsync(c =>
                c.FamilyId == membership.FamilyId &&
                c.Scope == req.Scope &&
                c.WeekSummaryId == req.WeekSummaryId &&
                c.MonthSummaryId == req.MonthSummaryId &&
                c.Rank == req.Rank);

        if (existing is not null)
            return Ok(ToDto(existing));

        var claim = new PrizeClaim
        {
            Id = Guid.NewGuid(),
            FamilyId = membership.FamilyId,
            Scope = req.Scope,
            WeekSummaryId = req.WeekSummaryId,
            MonthSummaryId = req.MonthSummaryId,
            Rank = req.Rank,
            ChildId = req.ChildId,
            ChildName = req.ChildName,
            PrizeEmoji = req.PrizeEmoji,
            PrizeLabel = req.PrizeLabel,
            CreatedAt = DateTime.UtcNow
        };
        db.PrizeClaims.Add(claim);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(List), new { weekSummaryId = claim.WeekSummaryId }, ToDto(claim));
    }

    [HttpPut("{id}/used")]
    public async Task<ActionResult<PrizeClaimDto>> SetUsed(Guid id, UpdateUsedRequest req)
    {
        var userId = GetUserId();
        var membership = await db.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
        if (membership is null) return BadRequest("User does not belong to a family.");

        var claim = await db.PrizeClaims.Include(c => c.Comments).FirstOrDefaultAsync(c => c.Id == id);
        if (claim is null) return NotFound();
        if (claim.FamilyId != membership.FamilyId) return Forbid();

        claim.IsUsed = req.IsUsed;
        claim.UsedAt = req.IsUsed ? DateTime.UtcNow : null;
        await db.SaveChangesAsync();

        return Ok(ToDto(claim));
    }

    [HttpPost("{id}/comments")]
    public async Task<ActionResult<PrizeCommentDto>> AddComment(Guid id, AddCommentRequest req)
    {
        var userId = GetUserId();
        var membership = await db.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
        if (membership is null) return BadRequest("User does not belong to a family.");

        var claim = await db.PrizeClaims.FirstOrDefaultAsync(c => c.Id == id);
        if (claim is null) return NotFound();
        if (claim.FamilyId != membership.FamilyId) return Forbid();

        var comment = new PrizeComment
        {
            Id = Guid.NewGuid(),
            PrizeClaimId = id,
            Text = req.Text,
            CreatedAt = DateTime.UtcNow
        };
        db.PrizeComments.Add(comment);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(List), new { }, new PrizeCommentDto(comment.Id, comment.Text, comment.CreatedAt));
    }

    [HttpDelete("{id}/comments/{commentId}")]
    public async Task<IActionResult> DeleteComment(Guid id, Guid commentId)
    {
        var userId = GetUserId();
        var membership = await db.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
        if (membership is null) return BadRequest("User does not belong to a family.");

        var claim = await db.PrizeClaims.FirstOrDefaultAsync(c => c.Id == id);
        if (claim is null) return NotFound();
        if (claim.FamilyId != membership.FamilyId) return Forbid();

        var comment = await db.PrizeComments.FirstOrDefaultAsync(c => c.Id == commentId && c.PrizeClaimId == id);
        if (comment is null) return NotFound();

        db.PrizeComments.Remove(comment);
        await db.SaveChangesAsync();

        return NoContent();
    }
}
