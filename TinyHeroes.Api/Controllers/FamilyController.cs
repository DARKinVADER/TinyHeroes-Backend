using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    [HttpPost]
    public async Task<ActionResult<FamilyResponse>> Create(CreateFamilyRequest req)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

        if (db.FamilyMembers.Any(m => m.UserId == userId))
            return Conflict("User already belongs to a family.");

        var family = new Family { Id = Guid.NewGuid(), Name = req.Name, WeekStartDay = req.WeekStartDay, CreatedByUserId = userId };
        var member = new FamilyMember { Id = Guid.NewGuid(), FamilyId = family.Id, UserId = userId, Role = FamilyRole.Admin };

        db.Families.Add(family);
        db.FamilyMembers.Add(member);
        await db.SaveChangesAsync();

        return Ok(new FamilyResponse(family.Id, family.Name, family.WeekStartDay));
    }
}
