using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TinyHeroes.Application.DTOs.JoinRequest;
using TinyHeroes.Domain.Entities;
using TinyHeroes.Domain.Enums;
using TinyHeroes.Infrastructure.Data;

namespace TinyHeroes.Api.Controllers;

[ApiController]
[Route("api/join-requests")]
[Authorize]
public class JoinRequestController(AppDbContext db) : ApiControllerBase
{
    [HttpPost]
    public async Task<ActionResult<JoinRequestResponse>> Submit(SubmitJoinRequestRequest req)
    {
        var userId = GetUserId();

        if (await db.FamilyMembers.AnyAsync(m => m.UserId == userId))
            return BadRequest("User already belongs to a family.");

        if (await db.FamilyJoinRequests.AnyAsync(r => r.RequestedById == userId && r.Status == JoinRequestStatus.Pending))
            return BadRequest("User already has a pending join request.");

        var family = await db.Families
            .Include(f => f.JoinRequests)
            .FirstOrDefaultAsync(f => f.JoinCode == req.JoinCode);
        if (family is null) return NotFound("No family found with that code.");

        var joinRequest = new FamilyJoinRequest
        {
            Id = Guid.NewGuid(),
            FamilyId = family.Id,
            RequestedById = userId
        };

        db.FamilyJoinRequests.Add(joinRequest);
        await db.SaveChangesAsync();

        var requester = await db.Users.FindAsync(userId);
        return CreatedAtAction(nameof(GetMine), new JoinRequestResponse(
            joinRequest.Id,
            requester!.DisplayName,
            requester.Email!,
            joinRequest.RequestedAt,
            joinRequest.Status.ToString(),
            family.Name));
    }

    [HttpGet]
    public async Task<ActionResult<JoinRequestResponse>> GetMine()
    {
        var userId = GetUserId();
        var joinRequest = await db.FamilyJoinRequests
            .Include(r => r.Family)
            .Include(r => r.RequestedBy)
            .FirstOrDefaultAsync(r => r.RequestedById == userId && r.Status == JoinRequestStatus.Pending);

        if (joinRequest is null) return NotFound();

        return Ok(new JoinRequestResponse(
            joinRequest.Id,
            joinRequest.RequestedBy.DisplayName,
            joinRequest.RequestedBy.Email!,
            joinRequest.RequestedAt,
            joinRequest.Status.ToString(),
            joinRequest.Family.Name));
    }

    [HttpDelete]
    public async Task<IActionResult> Cancel()
    {
        var userId = GetUserId();
        var joinRequest = await db.FamilyJoinRequests
            .FirstOrDefaultAsync(r => r.RequestedById == userId && r.Status == JoinRequestStatus.Pending);

        if (joinRequest is null) return NotFound();

        db.FamilyJoinRequests.Remove(joinRequest);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
