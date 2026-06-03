using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TinyHeroes.Application.DTOs.Child;
using TinyHeroes.Application.Interfaces;
using TinyHeroes.Domain.Entities;
using TinyHeroes.Domain.Enums;
using TinyHeroes.Infrastructure.Data;

namespace TinyHeroes.Api.Controllers;

[ApiController]
[Route("api/children")]
[Authorize]
public class ChildController(AppDbContext db, IFileStorageService fileStorage) : ApiControllerBase
{

    [HttpPost]
    public async Task<ActionResult<ChildResponse>> Create(CreateChildRequest req)
    {
        var userId = GetUserId();
        var membership = await db.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
        if (membership is null) return BadRequest("User does not belong to a family.");

        var child = new Child
        {
            Id = Guid.NewGuid(),
            FamilyId = membership.FamilyId,
            Name = req.Name,
            Age = req.Age,
            Gender = req.Gender,
            AvatarEmoji = req.AvatarEmoji
        };
        db.Children.Add(child);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = child.Id }, new ChildResponse(child.Id, child.Name, child.Age, child.Gender, child.AvatarEmoji, child.AvatarUrl));
    }

    [HttpGet]
    public async Task<ActionResult<List<ChildResponse>>> List()
    {
        var userId = GetUserId();
        var membership = await db.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
        if (membership is null) return BadRequest("User does not belong to a family.");

        var children = await db.Children
            .Where(c => c.FamilyId == membership.FamilyId)
            .Select(c => new ChildResponse(c.Id, c.Name, c.Age, c.Gender, c.AvatarEmoji, c.AvatarUrl))
            .ToListAsync();

        return Ok(children);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ChildResponse>> Get(Guid id)
    {
        var userId = GetUserId();
        var membership = await db.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
        if (membership is null) return BadRequest("User does not belong to a family.");

        var child = await db.Children.FirstOrDefaultAsync(c => c.Id == id && c.FamilyId == membership.FamilyId);
        if (child is null) return NotFound();

        return Ok(new ChildResponse(child.Id, child.Name, child.Age, child.Gender, child.AvatarEmoji, child.AvatarUrl));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ChildResponse>> Update(Guid id, UpdateChildRequest req)
    {
        var userId = GetUserId();
        var membership = await db.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
        if (membership is null) return BadRequest("User does not belong to a family.");

        var child = await db.Children.FirstOrDefaultAsync(c => c.Id == id && c.FamilyId == membership.FamilyId);
        if (child is null) return NotFound();

        child.Name = req.Name;
        child.Age = req.Age;
        child.Gender = req.Gender;
        child.AvatarEmoji = req.AvatarEmoji;
        await db.SaveChangesAsync();

        return Ok(new ChildResponse(child.Id, child.Name, child.Age, child.Gender, child.AvatarEmoji, child.AvatarUrl));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetUserId();
        var membership = await db.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
        if (membership is null) return BadRequest("User does not belong to a family.");
        if (membership.Role != FamilyRole.Admin) return Forbid();

        var child = await db.Children.FirstOrDefaultAsync(c => c.Id == id && c.FamilyId == membership.FamilyId);
        if (child is null) return NotFound();

        db.Children.Remove(child);
        await db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id:guid}/avatar")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<ChildResponse>> UploadAvatar(Guid id, IFormFile file, CancellationToken ct)
    {
        var userId = GetUserId();
        var membership = await db.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
        if (membership is null) return BadRequest("User does not belong to a family.");

        var child = await db.Children.FirstOrDefaultAsync(c => c.Id == id && c.FamilyId == membership.FamilyId);
        if (child is null) return NotFound();

        var ext = Path.GetExtension(file.FileName ?? "").ToLowerInvariant();
        if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp"))
            return BadRequest("Only .jpg, .jpeg, .png, and .webp files are allowed.");

        var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowedMimeTypes.Contains(file.ContentType))
            return BadRequest("Invalid file content type.");

        if (!string.IsNullOrEmpty(child.AvatarUrl))
        {
            var oldFileName = Path.GetFileName(child.AvatarUrl);
            fileStorage.Delete("avatars", oldFileName);
        }

        var fileName = $"{Guid.NewGuid()}{ext}";
        await using var stream = file.OpenReadStream();
        var url = await fileStorage.SaveAsync(stream, "avatars", fileName, ct);

        child.AvatarUrl = url;
        await db.SaveChangesAsync();

        return Ok(new ChildResponse(child.Id, child.Name, child.Age, child.Gender, child.AvatarEmoji, child.AvatarUrl));
    }
}
