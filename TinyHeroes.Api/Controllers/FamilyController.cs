using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TinyHeroes.Application.DTOs.Family;
using TinyHeroes.Application.DTOs.JoinRequest;
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

        string joinCode;
        do { joinCode = Guid.NewGuid().ToString("N")[..8].ToUpper(); }
        while (await db.Families.AnyAsync(f => f.JoinCode == joinCode));

        var family = new Family
        {
            Id = Guid.NewGuid(),
            Name = req.Name,
            WeekStartDay = req.WeekStartDay,
            CreatedByUserId = userId,
            JoinCode = joinCode
        };
        var member = new FamilyMember { Id = Guid.NewGuid(), FamilyId = family.Id, UserId = userId, Role = FamilyRole.Admin };

        db.Families.Add(family);
        db.FamilyMembers.Add(member);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMine), new FamilyResponse(family.Id, family.Name, family.WeekStartDay, family.WeeklyMinDeeds, family.MonthlyMinDeeds, family.JoinCode));
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

        var membership = family.Members.FirstOrDefault(m => m.UserId == userId);
        var isAdmin = membership?.Role == FamilyRole.Admin;

        return Ok(new FamilyDetailResponse(
            family.Id,
            family.Name,
            family.WeekStartDay,
            members,
            family.WeeklyMinDeeds,
            family.MonthlyMinDeeds,
            isAdmin ? family.JoinCode : null));
    }

    [HttpPatch("mine")]
    public async Task<ActionResult<FamilyResponse>> UpdateMine(UpdateFamilyRequest req)
    {
        var result = await GetAdminFamily();
        if (result.Error is not null) return result.Error;

        result.Family!.Name = req.Name;
        result.Family.WeekStartDay = req.WeekStartDay;
        await db.SaveChangesAsync();

        return Ok(new FamilyResponse(result.Family.Id, result.Family.Name, result.Family.WeekStartDay, result.Family.WeeklyMinDeeds, result.Family.MonthlyMinDeeds, result.Family.JoinCode));
    }

    [HttpPatch("mine/prize-rules")]
    public async Task<ActionResult<FamilyResponse>> UpdatePrizeRules(SetPrizeRulesRequest req)
    {
        var result = await GetAdminFamily();
        if (result.Error is not null) return result.Error;

        result.Family!.WeeklyMinDeeds = req.WeeklyMinDeeds;
        result.Family.MonthlyMinDeeds = req.MonthlyMinDeeds;
        await db.SaveChangesAsync();

        return Ok(new FamilyResponse(result.Family.Id, result.Family.Name, result.Family.WeekStartDay, result.Family.WeeklyMinDeeds, result.Family.MonthlyMinDeeds, result.Family.JoinCode));
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

    [HttpGet("join-requests")]
    public async Task<ActionResult<List<JoinRequestResponse>>> GetJoinRequests()
    {
        var userId = GetUserId();
        var member = await db.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
        if (member is null) return NotFound("User does not belong to a family.");
        if (member.Role != FamilyRole.Admin) return Forbid();

        var requests = await db.FamilyJoinRequests
            .Include(r => r.RequestedBy)
            .Include(r => r.Family)
            .Where(r => r.FamilyId == member.FamilyId && r.Status == JoinRequestStatus.Pending)
            .OrderBy(r => r.RequestedAt)
            .ToListAsync();

        return Ok(requests.Select(r => new JoinRequestResponse(
            r.Id,
            r.RequestedBy.DisplayName,
            r.RequestedBy.Email!,
            r.RequestedAt,
            r.Status.ToString(),
            r.Family.Name)).ToList());
    }

    [HttpPost("join-requests/{id:guid}/resolve")]
    public async Task<IActionResult> ResolveJoinRequest(Guid id, ResolveJoinRequestRequest req)
    {
        var adminId = GetUserId();
        var adminMember = await db.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == adminId);
        if (adminMember is null) return NotFound("User does not belong to a family.");
        if (adminMember.Role != FamilyRole.Admin) return Forbid();

        var joinRequest = await db.FamilyJoinRequests
            .FirstOrDefaultAsync(r => r.Id == id && r.FamilyId == adminMember.FamilyId && r.Status == JoinRequestStatus.Pending);
        if (joinRequest is null) return NotFound();

        joinRequest.Status = req.Approve ? JoinRequestStatus.Approved : JoinRequestStatus.Rejected;
        joinRequest.ResolvedAt = DateTime.UtcNow;
        joinRequest.ResolvedById = adminId;

        if (req.Approve)
        {
            // Race condition guard: re-check requester has no family membership
            if (await db.FamilyMembers.AnyAsync(m => m.UserId == joinRequest.RequestedById))
            {
                joinRequest.Status = JoinRequestStatus.Rejected;
                await db.SaveChangesAsync();
                return Conflict("User already belongs to a family.");
            }

            var newMember = new FamilyMember
            {
                Id = Guid.NewGuid(),
                FamilyId = joinRequest.FamilyId,
                UserId = joinRequest.RequestedById,
                Role = FamilyRole.CoParent
            };
            db.FamilyMembers.Add(newMember);
        }

        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException) { return Conflict("User already belongs to a family."); }

        return Ok();
    }

    private async Task<(Family? Family, ActionResult? Error)> GetAdminFamily()
    {
        var userId = GetUserId();
        var member = await db.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
        if (member is null) return (null, BadRequest("User does not belong to a family."));
        if (member.Role != FamilyRole.Admin) return (null, Forbid());
        var family = await db.Families.FindAsync(member.FamilyId);
        if (family is null) return (null, NotFound());
        return (family, null);
    }
}
