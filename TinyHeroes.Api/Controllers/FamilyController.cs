using System.Security.Claims;
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
public class FamilyController(AppDbContext db) : ControllerBase
{
    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

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
            m.UserId, m.User.DisplayName, m.User.Email!, m.Role
        )).ToList();

        return Ok(new FamilyDetailResponse(family.Id, family.Name, family.WeekStartDay, members));
    }
}
