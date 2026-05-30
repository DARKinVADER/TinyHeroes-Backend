using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TinyHeroes.Application.DTOs.Deed;
using TinyHeroes.Application.Interfaces;
using TinyHeroes.Domain.Entities;
using TinyHeroes.Infrastructure.Data;

namespace TinyHeroes.Api.Controllers;

[ApiController]
[Route("api/deeds")]
[Authorize]
public class DeedController(AppDbContext db, IAiImageService aiImageService) : ApiControllerBase
{

    [HttpPost]
    public async Task<ActionResult<DeedResponse>> Create(CreateDeedRequest req)
    {
        var userId = GetUserId();
        var membership = await db.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
        if (membership is null) return BadRequest("User does not belong to a family.");

        var child = await db.Children.FirstOrDefaultAsync(c => c.Id == req.ChildId && c.FamilyId == membership.FamilyId);
        if (child is null) return NotFound("Child not found in your family.");

        var user = await db.Users.FindAsync(userId);
        var deed = new GoodDeed
        {
            Id = Guid.NewGuid(),
            ChildId = req.ChildId,
            AddedByUserId = userId,
            Description = req.Description,
            ImageType = req.ImageType,
            ImageValue = req.ImageValue,
            CreatedAt = DateTime.UtcNow
        };
        db.GoodDeeds.Add(deed);
        await db.SaveChangesAsync();

        return Ok(new DeedResponse(deed.Id, deed.ChildId, deed.Description, deed.ImageType, deed.ImageValue, user!.DisplayName, deed.CreatedAt));
    }

    [HttpGet]
    public async Task<ActionResult<List<DeedResponse>>> List([FromQuery] Guid childId)
    {
        var userId = GetUserId();
        var membership = await db.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
        if (membership is null) return BadRequest("User does not belong to a family.");

        var child = await db.Children.FirstOrDefaultAsync(c => c.Id == childId && c.FamilyId == membership.FamilyId);
        if (child is null) return NotFound("Child not found in your family.");

        var deeds = await db.GoodDeeds
            .Where(d => d.ChildId == childId)
            .OrderByDescending(d => d.CreatedAt)
            .Take(50)
            .Include(d => d.AddedBy)
            .Select(d => new DeedResponse(d.Id, d.ChildId, d.Description, d.ImageType, d.ImageValue, d.AddedBy.DisplayName, d.CreatedAt))
            .ToListAsync();

        return Ok(deeds);
    }

    [HttpGet("stats")]
    public async Task<ActionResult<List<ChildStatsResponse>>> Stats()
    {
        var userId = GetUserId();
        var membership = await db.FamilyMembers.Include(m => m.Family).FirstOrDefaultAsync(m => m.UserId == userId);
        if (membership is null) return BadRequest("User does not belong to a family.");

        var family = membership.Family;
        var today = DateTime.UtcNow.Date;
        var daysSinceStart = ((int)today.DayOfWeek - (int)family.WeekStartDay + 7) % 7;
        var weekStart = today.AddDays(-daysSinceStart);

        var children = await db.Children
            .Where(c => c.FamilyId == family.Id)
            .Select(c => new ChildStatsResponse(
                c.Id,
                c.Deeds.Count(d => d.CreatedAt >= weekStart),
                c.Deeds.Count()
            ))
            .ToListAsync();

        return Ok(children);
    }

    [HttpPost("generate-image")]
    public async Task<ActionResult<GenerateImageResponse>> GenerateImage(GenerateImageRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Prompt))
            return BadRequest("Prompt is required.");

        if (req.Prompt.Length > 500)
            return BadRequest("Prompt must be 500 characters or fewer.");

        try
        {
            var dataUrl = await aiImageService.GenerateDataUrlAsync(req.Prompt, ct);
            return Ok(new GenerateImageResponse(dataUrl));
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(503, ex.Message);
        }
    }
}
