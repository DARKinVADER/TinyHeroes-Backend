using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TinyHeroes.Application.DTOs.Family;
using TinyHeroes.Domain.Entities;
using TinyHeroes.Domain.Enums;
using TinyHeroes.Infrastructure.Data;

namespace TinyHeroes.Api.Controllers;

[ApiController]
[Route("api/families")]
[Authorize]
public class FamilyController(AppDbContext db) : ApiControllerBase
{

    [HttpPost]
    public async Task<ActionResult<FamilyResponse>> Create(CreateFamilyRequest req)
    {
        var userId = GetUserId();

        if (await db.FamilyMembers.AnyAsync(m => m.UserId == userId))
            return Conflict("User already belongs to a family.");

        var family = new Family { Id = Guid.NewGuid(), Name = req.Name, WeekStartDay = req.WeekStartDay, CreatedByUserId = userId };
        var member = new FamilyMember { Id = Guid.NewGuid(), FamilyId = family.Id, UserId = userId, Role = FamilyRole.Admin };

        db.Families.Add(family);
        db.FamilyMembers.Add(member);
        await db.SaveChangesAsync();

        return Ok(new FamilyResponse(family.Id, family.Name, family.WeekStartDay));
    }

    [HttpGet("mine")]
    public async Task<ActionResult<FamilyDetailResponse>> GetMine()
    {
        var userId = GetUserId();
        var family = await db.Families
            .Include(f => f.Members).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(f => f.Members.Any(m => m.UserId == userId));

        if (family is null) return NotFound("User does not belong to a family.");

        var members = family.Members.Select(m => new FamilyMemberResponse(
            m.UserId, m.User.DisplayName, m.User.Email!, m.Role.ToString()
        )).ToList();

        return Ok(new FamilyDetailResponse(family.Id, family.Name, family.WeekStartDay, members));
    }

    [HttpPatch("mine")]
    public async Task<ActionResult<FamilyResponse>> UpdateMine(UpdateFamilyRequest req)
    {
        var userId = GetUserId();
        var member = await db.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
        if (member is null) return BadRequest("User does not belong to a family.");
        if (member.Role != FamilyRole.Admin) return Forbid();

        var family = await db.Families.FindAsync(member.FamilyId);
        if (family is null) return NotFound();

        family.Name = req.Name;
        family.WeekStartDay = req.WeekStartDay;
        await db.SaveChangesAsync();

        return Ok(new FamilyResponse(family.Id, family.Name, family.WeekStartDay));
    }

    [HttpDelete("mine/members/{memberId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid memberId)
    {
        var currentUserId = GetUserId();
        var currentMember = await db.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == currentUserId);
        if (currentMember is null) return BadRequest("User does not belong to a family.");
        if (currentMember.Role != FamilyRole.Admin) return Forbid();
        if (memberId == currentUserId) return BadRequest("Cannot remove yourself from the family.");

        var targetMember = await db.FamilyMembers.FirstOrDefaultAsync(m =>
            m.UserId == memberId && m.FamilyId == currentMember.FamilyId);
        if (targetMember is null) return NotFound("Member not found in this family.");

        db.FamilyMembers.Remove(targetMember);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("mine")]
    public async Task<IActionResult> DeleteFamily()
    {
        var userId = GetUserId();
        var member = await db.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
        if (member is null) return BadRequest("User does not belong to a family.");
        if (member.Role != FamilyRole.Admin) return Forbid();

        var family = await db.Families.FindAsync(member.FamilyId);
        if (family is null) return NotFound();

        db.Families.Remove(family);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
