using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TinyHeroes.Application.DTOs.Prize;
using TinyHeroes.Domain.Entities;
using TinyHeroes.Domain.Enums;
using TinyHeroes.Infrastructure.Data;

namespace TinyHeroes.Api.Controllers;

[ApiController]
[Route("api/prize-presets")]
[Authorize]
public class PrizePresetController(AppDbContext db) : ControllerBase
{
    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

    [HttpGet]
    public async Task<ActionResult<List<PrizePresetResponse>>> List()
    {
        var userId = GetUserId();
        var membership = await db.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
        if (membership is null) return BadRequest("User does not belong to a family.");

        var presets = await db.PrizePresets
            .Where(p => p.FamilyId == null || p.FamilyId == membership.FamilyId)
            .OrderBy(p => p.FamilyId == null ? 0 : 1)
            .ThenByDescending(p => p.CreatedAt)
            .Select(p => new PrizePresetResponse(p.Id, p.Label, p.Emoji, p.FamilyId == null))
            .ToListAsync();

        return Ok(presets);
    }

    [HttpPost]
    public async Task<ActionResult<PrizePresetResponse>> Create(CreatePrizePresetRequest req)
    {
        var userId = GetUserId();
        var membership = await db.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
        if (membership is null) return BadRequest("User does not belong to a family.");
        if (membership.Role != FamilyRole.Admin) return Forbid();

        var preset = new PrizePreset
        {
            Id = Guid.NewGuid(),
            FamilyId = membership.FamilyId,
            Label = req.Label,
            Emoji = req.Emoji
        };
        db.PrizePresets.Add(preset);
        await db.SaveChangesAsync();

        return Ok(new PrizePresetResponse(preset.Id, preset.Label, preset.Emoji, false));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetUserId();
        var membership = await db.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
        if (membership is null) return BadRequest("User does not belong to a family.");
        if (membership.Role != FamilyRole.Admin) return Forbid();

        var preset = await db.PrizePresets.FirstOrDefaultAsync(p => p.Id == id);
        if (preset is null) return NotFound();
        if (preset.FamilyId == null) return Forbid(); // cannot delete system presets
        if (preset.FamilyId != membership.FamilyId) return NotFound();

        db.PrizePresets.Remove(preset);
        await db.SaveChangesAsync();

        return Ok();
    }
}
