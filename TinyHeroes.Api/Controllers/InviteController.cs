using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TinyHeroes.Application.DTOs.Invite;
using TinyHeroes.Domain.Entities;
using TinyHeroes.Domain.Enums;
using TinyHeroes.Infrastructure.Data;

namespace TinyHeroes.Api.Controllers;

[ApiController]
[Route("api/invites")]
[Authorize]
public class InviteController(AppDbContext db) : ApiControllerBase
{

    [HttpPost]
    public async Task<ActionResult<InviteResponse>> Create(CreateInviteRequest req)
    {
        var userId = GetUserId();
        var membership = await db.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
        if (membership is null) return BadRequest("User does not belong to a family.");
        if (membership.Role != FamilyRole.Admin) return Forbid();

        var invite = new FamilyInvite
        {
            Id = Guid.NewGuid(),
            FamilyId = membership.FamilyId,
            Email = req.Email,
            Token = Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        db.FamilyInvites.Add(invite);
        await db.SaveChangesAsync();

        return Ok(new InviteResponse(invite.Id, invite.Token, invite.Email, invite.ExpiresAt));
    }

    [HttpPost("{token}/accept")]
    public async Task<IActionResult> Accept(string token)
    {
        var userId = GetUserId();

        var invite = await db.FamilyInvites.FirstOrDefaultAsync(i => i.Token == token && !i.Accepted);
        if (invite is null) return NotFound("Invite not found or already accepted.");

        if (invite.ExpiresAt < DateTime.UtcNow) return BadRequest("Invite has expired.");

        if (await db.FamilyMembers.AnyAsync(m => m.UserId == userId))
            return Conflict("User already belongs to a family.");

        var member = new FamilyMember
        {
            Id = Guid.NewGuid(),
            FamilyId = invite.FamilyId,
            UserId = userId,
            Role = FamilyRole.CoParent
        };

        invite.Accepted = true;
        db.FamilyMembers.Add(member);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict("User already belongs to a family.");
        }

        return Ok();
    }
}
