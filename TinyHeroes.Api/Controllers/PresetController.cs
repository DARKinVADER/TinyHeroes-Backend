using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TinyHeroes.Application.DTOs.Preset;
using TinyHeroes.Domain.Entities;
using TinyHeroes.Domain.Enums;
using TinyHeroes.Infrastructure.Data;

namespace TinyHeroes.Api.Controllers;

[ApiController]
[Route("api/presets")]
[Authorize]
public class PresetController(AppDbContext db) : ControllerBase
{
    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

    [HttpGet]
    public async Task<ActionResult<List<PresetResponse>>> List()
    {
        var userId = GetUserId();
        var membership = await db.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
        if (membership is null) return BadRequest("User does not belong to a family.");

        var presets = await db.DeedPresets
            .Where(p => p.FamilyId == null || p.FamilyId == membership.FamilyId)
            .OrderBy(p => p.FamilyId == null ? 0 : 1)
            .ThenBy(p => p.CreatedAt)
            .Select(p => new PresetResponse(p.Id, p.Label, p.ImageValue, p.Enabled, p.FamilyId == null, p.LabelKey))
            .ToListAsync();

        return Ok(presets);
    }

    [HttpPost]
    public async Task<ActionResult<PresetResponse>> Create(CreatePresetRequest req)
    {
        var userId = GetUserId();
        var membership = await db.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
        if (membership is null) return BadRequest("User does not belong to a family.");

        var preset = new DeedPreset
        {
            Id = Guid.NewGuid(),
            FamilyId = membership.FamilyId,
            Label = req.Label,
            ImageValue = req.ImageValue
        };
        db.DeedPresets.Add(preset);
        await db.SaveChangesAsync();

        return Ok(new PresetResponse(preset.Id, preset.Label, preset.ImageValue, preset.Enabled, false, null));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetUserId();
        var membership = await db.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
        if (membership is null) return BadRequest("User does not belong to a family.");
        if (membership.Role != FamilyRole.Admin) return Forbid();

        var preset = await db.DeedPresets.FirstOrDefaultAsync(p => p.Id == id);
        if (preset is null) return NotFound();
        if (preset.FamilyId == null) return Forbid(); // cannot delete system presets
        if (preset.FamilyId != membership.FamilyId) return NotFound();

        db.DeedPresets.Remove(preset);
        await db.SaveChangesAsync();

        return NoContent();
    }
}
