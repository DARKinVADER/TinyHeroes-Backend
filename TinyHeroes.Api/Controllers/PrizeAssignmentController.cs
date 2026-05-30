using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TinyHeroes.Application.DTOs.Prize;
using TinyHeroes.Domain.Entities;
using TinyHeroes.Domain.Enums;
using TinyHeroes.Infrastructure.Data;

namespace TinyHeroes.Api.Controllers;

[ApiController]
[Route("api/prize-assignments")]
[Authorize]
public class PrizeAssignmentController(AppDbContext db) : ApiControllerBase
{

    [HttpGet]
    public async Task<ActionResult<List<PrizeAssignmentResponse>>> List()
    {
        var userId = GetUserId();
        var membership = await db.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
        if (membership is null) return BadRequest("User does not belong to a family.");

        var assignments = await db.PrizeAssignments
            .Where(a => a.FamilyId == membership.FamilyId)
            .OrderBy(a => a.Scope)
            .ThenBy(a => a.Rank)
            .Select(a => new PrizeAssignmentResponse(a.Id, a.Scope, a.Rank, a.Emoji, a.Label))
            .ToListAsync();

        return Ok(assignments);
    }

    [HttpPut]
    public async Task<ActionResult<PrizeAssignmentResponse>> Set(SetPrizeRequest req)
    {
        var userId = GetUserId();
        var membership = await db.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
        if (membership is null) return BadRequest("User does not belong to a family.");
        if (membership.Role != FamilyRole.Admin) return Forbid();

        var existing = await db.PrizeAssignments
            .FirstOrDefaultAsync(a => a.FamilyId == membership.FamilyId && a.Scope == req.Scope && a.Rank == req.Rank);

        if (existing is not null)
        {
            existing.Emoji = req.Emoji;
            existing.Label = req.Label;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            existing = new PrizeAssignment
            {
                Id = Guid.NewGuid(),
                FamilyId = membership.FamilyId,
                Scope = req.Scope,
                Rank = req.Rank,
                Emoji = req.Emoji,
                Label = req.Label,
                UpdatedAt = DateTime.UtcNow
            };
            db.PrizeAssignments.Add(existing);
        }

        await db.SaveChangesAsync();

        return Ok(new PrizeAssignmentResponse(existing.Id, existing.Scope, existing.Rank, existing.Emoji, existing.Label));
    }
}
