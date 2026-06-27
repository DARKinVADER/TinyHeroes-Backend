# Family Join Request Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Allow a registered user without a family to enter a short family code, submit a join request, and have an admin approve or reject it from a dashboard banner and modal.

**Architecture:** A new `FamilyJoinRequest` entity tracks user-initiated requests. A permanent `JoinCode` field on `Family` is the entry point. Backend uses a new `JoinRequestController` plus two admin endpoints on `FamilyController`. Frontend adds a `JoinRequestService`, a new pending-state screen, auth routing logic, and admin UI in `FamilySettingsComponent` and `ShellComponent`.

**Tech Stack:** .NET 8 / EF Core / ASP.NET Identity (backend), Angular 21 standalone components / signals / ngx-translate (frontend), Playwright (e2e)

---

## File Map

**Backend — new files:**
- `TinyHeroes.Domain/Enums/JoinRequestStatus.cs`
- `TinyHeroes.Domain/Entities/FamilyJoinRequest.cs`
- `TinyHeroes.Infrastructure/Data/Configurations/FamilyJoinRequestConfiguration.cs`
- `TinyHeroes.Application/DTOs/JoinRequest/JoinRequestDtos.cs`
- `TinyHeroes.Api/Controllers/JoinRequestController.cs`
- `TinyHeroes.Tests/Integration/JoinRequestControllerTests.cs`

**Backend — modified files:**
- `TinyHeroes.Domain/Entities/Family.cs` — add `JoinCode`, `JoinRequests` nav
- `TinyHeroes.Infrastructure/Data/AppDbContext.cs` — add `DbSet<FamilyJoinRequest>`
- `TinyHeroes.Application/DTOs/Family/CreateFamilyRequest.cs` — add `JoinCode` to `FamilyResponse`
- `TinyHeroes.Application/DTOs/Family/FamilyDetailResponse.cs` — add `JoinCode` to `FamilyDetailResponse`
- `TinyHeroes.Api/Controllers/FamilyController.cs` — generate JoinCode on create; expose it in GetMine for admins; add join-request list + resolve endpoints
- `TinyHeroes.Api/TinyHeroes.Api.csproj` — version bump to 1.4.0

**Frontend — new files:**
- `frontend/src/app/core/services/join-request.service.ts`
- `frontend/src/app/core/services/join-request.service.spec.ts`
- `frontend/src/app/features/auth/pages/join-request-pending.component.ts`
- `frontend/src/app/features/auth/pages/join-request-pending.component.spec.ts`

**Frontend — modified files:**
- `frontend/src/app/core/models/family.model.ts` — add `joinCode`, `JoinRequestResponse`
- `frontend/src/app/core/auth/auth.service.ts` — `th_pending_request` key, routing logic, bootstrap check
- `frontend/src/app/core/services/family.service.ts` — `getJoinRequests()`, `resolveJoinRequest()`
- `frontend/src/app/features/auth/pages/create-family.component.ts` — join-with-code toggle
- `frontend/src/app/features/settings/pages/family-settings.component.ts` — JoinCode display, requests badge + modal
- `frontend/src/app/shared/components/shell.component.ts` — admin banner
- `frontend/src/app/app.routes.ts` — new `join-request-pending` route
- `frontend/src/app/features/help/help.component.ts` — new step
- `frontend/src/environments/environment.ts` + `environment.prod.ts` — version 2.3.0
- `frontend/public/assets/i18n/en.json` (+ hu, de, fr, es) — new keys
- `CHANGELOG.md`

---

## Task 1: Domain — JoinRequestStatus enum + FamilyJoinRequest entity

**Files:**
- Create: `backend/TinyHeroes.Domain/Enums/JoinRequestStatus.cs`
- Create: `backend/TinyHeroes.Domain/Entities/FamilyJoinRequest.cs`
- Modify: `backend/TinyHeroes.Domain/Entities/Family.cs`

- [ ] **Step 1: Create JoinRequestStatus enum**

```csharp
// backend/TinyHeroes.Domain/Enums/JoinRequestStatus.cs
namespace TinyHeroes.Domain.Enums;

public enum JoinRequestStatus { Pending, Approved, Rejected }
```

- [ ] **Step 2: Create FamilyJoinRequest entity**

```csharp
// backend/TinyHeroes.Domain/Entities/FamilyJoinRequest.cs
namespace TinyHeroes.Domain.Entities;

public class FamilyJoinRequest
{
    public Guid Id { get; set; }
    public Guid FamilyId { get; set; }
    public Guid RequestedById { get; set; }
    public Enums.JoinRequestStatus Status { get; set; } = Enums.JoinRequestStatus.Pending;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public Guid? ResolvedById { get; set; }
    public Family Family { get; set; } = null!;
    public User RequestedBy { get; set; } = null!;
}
```

- [ ] **Step 3: Add JoinCode and JoinRequests to Family entity**

Replace the `Family` class body in `backend/TinyHeroes.Domain/Entities/Family.cs` — add two properties after `CreatedAt`:

```csharp
public string JoinCode { get; set; } = string.Empty;
// ...existing Members, Children, Invites...
public ICollection<FamilyJoinRequest> JoinRequests { get; set; } = [];
```

Full file after edit:
```csharp
namespace TinyHeroes.Domain.Entities;

public class Family
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DayOfWeek WeekStartDay { get; set; } = DayOfWeek.Monday;
    public int? WeeklyMinDeeds { get; set; }
    public int? MonthlyMinDeeds { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string JoinCode { get; set; } = string.Empty;

    public ICollection<FamilyMember> Members { get; set; } = [];
    public ICollection<Child> Children { get; set; } = [];
    public ICollection<FamilyInvite> Invites { get; set; } = [];
    public ICollection<FamilyJoinRequest> JoinRequests { get; set; } = [];
}
```

- [ ] **Step 4: Verify Domain project builds**

```bash
cd backend && dotnet build TinyHeroes.Domain
```
Expected: Build succeeded, 0 errors.

- [ ] **Step 5: Commit**

```bash
git add backend/TinyHeroes.Domain
git commit -m "feat: add FamilyJoinRequest entity and JoinCode to Family"
```

---

## Task 2: Infrastructure — EF configuration + DbContext + migration

**Files:**
- Create: `backend/TinyHeroes.Infrastructure/Data/Configurations/FamilyJoinRequestConfiguration.cs`
- Modify: `backend/TinyHeroes.Infrastructure/Data/AppDbContext.cs`

- [ ] **Step 1: Create EF configuration**

```csharp
// backend/TinyHeroes.Infrastructure/Data/Configurations/FamilyJoinRequestConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TinyHeroes.Domain.Entities;

namespace TinyHeroes.Infrastructure.Data.Configurations;

public class FamilyJoinRequestConfiguration : IEntityTypeConfiguration<FamilyJoinRequest>
{
    public void Configure(EntityTypeBuilder<FamilyJoinRequest> builder)
    {
        builder.HasOne(r => r.Family)
            .WithMany(f => f.JoinRequests)
            .HasForeignKey(r => r.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.RequestedBy)
            .WithMany()
            .HasForeignKey(r => r.RequestedById)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.RequestedById, r.Status });
        builder.Property(r => r.Status).HasConversion<string>();
    }
}
```

- [ ] **Step 2: Add DbSet to AppDbContext**

In `backend/TinyHeroes.Infrastructure/Data/AppDbContext.cs`, add after the `PrizeComments` line:

```csharp
public DbSet<FamilyJoinRequest> FamilyJoinRequests => Set<FamilyJoinRequest>();
```

- [ ] **Step 3: Add unique index on Family.JoinCode**

Create `backend/TinyHeroes.Infrastructure/Data/Configurations/FamilyConfiguration.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TinyHeroes.Domain.Entities;

namespace TinyHeroes.Infrastructure.Data.Configurations;

public class FamilyConfiguration : IEntityTypeConfiguration<Family>
{
    public void Configure(EntityTypeBuilder<Family> builder)
    {
        builder.HasIndex(f => f.JoinCode).IsUnique();
        builder.Property(f => f.JoinCode).HasMaxLength(8);
    }
}
```

- [ ] **Step 4: Run migration**

```bash
cd backend && dotnet ef migrations add AddFamilyJoinRequest \
  --project TinyHeroes.Infrastructure \
  --startup-project TinyHeroes.Api
```
Expected: `Done. To undo this action, use 'ef migrations remove'`

- [ ] **Step 5: Verify Infrastructure builds**

```bash
cd backend && dotnet build TinyHeroes.Infrastructure
```
Expected: Build succeeded, 0 errors.

- [ ] **Step 6: Commit**

```bash
git add backend/TinyHeroes.Infrastructure
git commit -m "feat: EF config and migration for FamilyJoinRequest"
```

---

## Task 3: Application — DTOs

**Files:**
- Create: `backend/TinyHeroes.Application/DTOs/JoinRequest/JoinRequestDtos.cs`
- Modify: `backend/TinyHeroes.Application/DTOs/Family/CreateFamilyRequest.cs`
- Modify: `backend/TinyHeroes.Application/DTOs/Family/FamilyDetailResponse.cs`

- [ ] **Step 1: Create join request DTOs**

```csharp
// backend/TinyHeroes.Application/DTOs/JoinRequest/JoinRequestDtos.cs
namespace TinyHeroes.Application.DTOs.JoinRequest;

public record SubmitJoinRequestRequest(string JoinCode);
public record ResolveJoinRequestRequest(bool Approve);
public record JoinRequestResponse(
    Guid Id,
    string RequesterDisplayName,
    string RequesterEmail,
    DateTime RequestedAt,
    string Status,
    string FamilyName);
```

- [ ] **Step 2: Add JoinCode to FamilyResponse**

In `backend/TinyHeroes.Application/DTOs/Family/CreateFamilyRequest.cs`, update `FamilyResponse`:

```csharp
namespace TinyHeroes.Application.DTOs.Family;

public record CreateFamilyRequest(string Name, DayOfWeek WeekStartDay);
public record FamilyResponse(Guid Id, string Name, DayOfWeek WeekStartDay, int? WeeklyMinDeeds = null, int? MonthlyMinDeeds = null, string? JoinCode = null);
```

- [ ] **Step 3: Add JoinCode to FamilyDetailResponse**

In `backend/TinyHeroes.Application/DTOs/Family/FamilyDetailResponse.cs`, update `FamilyDetailResponse`:

```csharp
namespace TinyHeroes.Application.DTOs.Family;

public record FamilyDetailResponse(Guid Id, string Name, DayOfWeek WeekStartDay, List<FamilyMemberResponse> Members, int? WeeklyMinDeeds = null, int? MonthlyMinDeeds = null, string? JoinCode = null);
public record FamilyMemberResponse(Guid UserId, string DisplayName, string Email, string Role);
public record UpdateFamilyRequest(string Name, DayOfWeek WeekStartDay);
```

- [ ] **Step 4: Verify Application builds**

```bash
cd backend && dotnet build TinyHeroes.Application
```
Expected: Build succeeded, 0 errors.

- [ ] **Step 5: Commit**

```bash
git add backend/TinyHeroes.Application
git commit -m "feat: DTOs for join request feature"
```

---

## Task 4: Backend tests (write failing) — JoinRequestController

**Files:**
- Create: `backend/TinyHeroes.Tests/Integration/JoinRequestControllerTests.cs`

- [ ] **Step 1: Write the test class**

```csharp
// backend/TinyHeroes.Tests/Integration/JoinRequestControllerTests.cs
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TinyHeroes.Application.DTOs.Family;
using TinyHeroes.Application.DTOs.JoinRequest;
using TinyHeroes.Tests.Integration.Helpers;

namespace TinyHeroes.Tests.Integration;

public class JoinRequestControllerTests(TestWebApplicationFactory<Program> factory)
    : IClassFixture<TestWebApplicationFactory<Program>>
{
    // ── Family creation seeds a JoinCode ────────────────────────────────────

    [Fact]
    public async Task CreateFamily_SetsJoinCode()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);
        var response = await client.GetAsync("/api/families/mine");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var family = await response.Content.ReadFromJsonAsync<FamilyDetailResponse>(TestWebApplicationFactory<Program>.JsonOptions);
        family!.JoinCode.Should().NotBeNullOrEmpty().And.HaveLength(8);
    }

    // ── Submit a join request ────────────────────────────────────────────────

    [Fact]
    public async Task SubmitJoinRequest_WithValidCode_Returns201()
    {
        var adminClient = await TestAuthHelper.RegisterWithFamily(factory);
        var familyRes = await adminClient.GetAsync("/api/families/mine");
        var family = await familyRes.Content.ReadFromJsonAsync<FamilyDetailResponse>(TestWebApplicationFactory<Program>.JsonOptions);

        var requesterClient = await TestAuthHelper.RegisterOnly(factory);
        var response = await requesterClient.PostAsJsonAsync("/api/join-requests", new SubmitJoinRequestRequest(family!.JoinCode!));
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var req = await response.Content.ReadFromJsonAsync<JoinRequestResponse>(TestWebApplicationFactory<Program>.JsonOptions);
        req!.Status.Should().Be("Pending");
        req.FamilyName.Should().Be("Test Family");
    }

    [Fact]
    public async Task SubmitJoinRequest_InvalidCode_Returns404()
    {
        var client = await TestAuthHelper.RegisterOnly(factory);
        var response = await client.PostAsJsonAsync("/api/join-requests", new SubmitJoinRequestRequest("BADCODE1"));
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SubmitJoinRequest_WhenAlreadyInFamily_Returns400()
    {
        var adminClient = await TestAuthHelper.RegisterWithFamily(factory);
        var familyRes = await adminClient.GetAsync("/api/families/mine");
        var family = await familyRes.Content.ReadFromJsonAsync<FamilyDetailResponse>(TestWebApplicationFactory<Program>.JsonOptions);

        var response = await adminClient.PostAsJsonAsync("/api/join-requests", new SubmitJoinRequestRequest(family!.JoinCode!));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SubmitJoinRequest_WhenAlreadyPending_Returns400()
    {
        var adminClient = await TestAuthHelper.RegisterWithFamily(factory);
        var familyRes = await adminClient.GetAsync("/api/families/mine");
        var family = await familyRes.Content.ReadFromJsonAsync<FamilyDetailResponse>(TestWebApplicationFactory<Program>.JsonOptions);

        var requesterClient = await TestAuthHelper.RegisterOnly(factory);
        await requesterClient.PostAsJsonAsync("/api/join-requests", new SubmitJoinRequestRequest(family!.JoinCode!));

        var response = await requesterClient.PostAsJsonAsync("/api/join-requests", new SubmitJoinRequestRequest(family.JoinCode!));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SubmitJoinRequest_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/join-requests", new SubmitJoinRequestRequest("ANYCODE1"));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Get / cancel own request ─────────────────────────────────────────────

    [Fact]
    public async Task GetJoinRequest_WithPending_ReturnsRequest()
    {
        var adminClient = await TestAuthHelper.RegisterWithFamily(factory);
        var familyRes = await adminClient.GetAsync("/api/families/mine");
        var family = await familyRes.Content.ReadFromJsonAsync<FamilyDetailResponse>(TestWebApplicationFactory<Program>.JsonOptions);

        var requesterClient = await TestAuthHelper.RegisterOnly(factory);
        await requesterClient.PostAsJsonAsync("/api/join-requests", new SubmitJoinRequestRequest(family!.JoinCode!));

        var response = await requesterClient.GetAsync("/api/join-requests");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var req = await response.Content.ReadFromJsonAsync<JoinRequestResponse>(TestWebApplicationFactory<Program>.JsonOptions);
        req!.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task GetJoinRequest_WithNone_Returns404()
    {
        var client = await TestAuthHelper.RegisterOnly(factory);
        var response = await client.GetAsync("/api/join-requests");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CancelJoinRequest_DeletesRequest()
    {
        var adminClient = await TestAuthHelper.RegisterWithFamily(factory);
        var familyRes = await adminClient.GetAsync("/api/families/mine");
        var family = await familyRes.Content.ReadFromJsonAsync<FamilyDetailResponse>(TestWebApplicationFactory<Program>.JsonOptions);

        var requesterClient = await TestAuthHelper.RegisterOnly(factory);
        await requesterClient.PostAsJsonAsync("/api/join-requests", new SubmitJoinRequestRequest(family!.JoinCode!));

        var deleteResponse = await requesterClient.DeleteAsync("/api/join-requests");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await requesterClient.GetAsync("/api/join-requests");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Admin: list pending requests ─────────────────────────────────────────

    [Fact]
    public async Task GetFamilyJoinRequests_AsAdmin_ReturnsPendingList()
    {
        var adminClient = await TestAuthHelper.RegisterWithFamily(factory);
        var familyRes = await adminClient.GetAsync("/api/families/mine");
        var family = await familyRes.Content.ReadFromJsonAsync<FamilyDetailResponse>(TestWebApplicationFactory<Program>.JsonOptions);

        var requesterClient = await TestAuthHelper.RegisterOnly(factory);
        await requesterClient.PostAsJsonAsync("/api/join-requests", new SubmitJoinRequestRequest(family!.JoinCode!));

        var response = await adminClient.GetAsync("/api/families/join-requests");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<List<JoinRequestResponse>>(TestWebApplicationFactory<Program>.JsonOptions);
        list.Should().HaveCount(1);
        list![0].Status.Should().Be("Pending");
    }

    [Fact]
    public async Task GetFamilyJoinRequests_AsCoParent_Returns403()
    {
        var adminClient = await TestAuthHelper.RegisterWithFamily(factory);
        var inviteRes = await adminClient.PostAsJsonAsync("/api/invites", new { email = (string?)null });
        var invite = await inviteRes.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(TestWebApplicationFactory<Program>.JsonOptions);
        var token = invite.GetProperty("token").GetString();

        var coParentClient = await TestAuthHelper.RegisterOnly(factory);
        await coParentClient.PostAsync($"/api/invites/{token}/accept", null);

        var response = await coParentClient.GetAsync("/api/families/join-requests");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetFamilyJoinRequests_WithoutFamily_Returns404()
    {
        var client = await TestAuthHelper.RegisterOnly(factory);
        var response = await client.GetAsync("/api/families/join-requests");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Admin: approve ───────────────────────────────────────────────────────

    [Fact]
    public async Task ResolveJoinRequest_Approve_CreatesFamilyMember()
    {
        var adminClient = await TestAuthHelper.RegisterWithFamily(factory);
        var familyRes = await adminClient.GetAsync("/api/families/mine");
        var family = await familyRes.Content.ReadFromJsonAsync<FamilyDetailResponse>(TestWebApplicationFactory<Program>.JsonOptions);

        var requesterClient = await TestAuthHelper.RegisterOnly(factory);
        var submitRes = await requesterClient.PostAsJsonAsync("/api/join-requests", new SubmitJoinRequestRequest(family!.JoinCode!));
        var joinReq = await submitRes.Content.ReadFromJsonAsync<JoinRequestResponse>(TestWebApplicationFactory<Program>.JsonOptions);

        var resolveRes = await adminClient.PostAsJsonAsync(
            $"/api/families/join-requests/{joinReq!.Id}/resolve",
            new ResolveJoinRequestRequest(true));
        resolveRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Requester can now access their family
        var familyCheckRes = await requesterClient.GetAsync("/api/families/mine");
        familyCheckRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var approvedFamily = await familyCheckRes.Content.ReadFromJsonAsync<FamilyDetailResponse>(TestWebApplicationFactory<Program>.JsonOptions);
        approvedFamily!.Members.Should().HaveCount(2);
        approvedFamily.Members.Should().Contain(m => m.Role == "CoParent");
    }

    [Fact]
    public async Task ResolveJoinRequest_Reject_DoesNotCreateFamilyMember()
    {
        var adminClient = await TestAuthHelper.RegisterWithFamily(factory);
        var familyRes = await adminClient.GetAsync("/api/families/mine");
        var family = await familyRes.Content.ReadFromJsonAsync<FamilyDetailResponse>(TestWebApplicationFactory<Program>.JsonOptions);

        var requesterClient = await TestAuthHelper.RegisterOnly(factory);
        var submitRes = await requesterClient.PostAsJsonAsync("/api/join-requests", new SubmitJoinRequestRequest(family!.JoinCode!));
        var joinReq = await submitRes.Content.ReadFromJsonAsync<JoinRequestResponse>(TestWebApplicationFactory<Program>.JsonOptions);

        var resolveRes = await adminClient.PostAsJsonAsync(
            $"/api/families/join-requests/{joinReq!.Id}/resolve",
            new ResolveJoinRequestRequest(false));
        resolveRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var familyCheckRes = await requesterClient.GetAsync("/api/families/mine");
        familyCheckRes.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Requester's join request now has Rejected status
        var reqRes = await requesterClient.GetAsync("/api/join-requests");
        reqRes.StatusCode.Should().Be(HttpStatusCode.NotFound); // pending is gone
    }

    [Fact]
    public async Task ResolveJoinRequest_ApproveTwice_SecondReturns404()
    {
        var adminClient = await TestAuthHelper.RegisterWithFamily(factory);
        var familyRes = await adminClient.GetAsync("/api/families/mine");
        var family = await familyRes.Content.ReadFromJsonAsync<FamilyDetailResponse>(TestWebApplicationFactory<Program>.JsonOptions);

        var requesterClient = await TestAuthHelper.RegisterOnly(factory);
        var submitRes = await requesterClient.PostAsJsonAsync("/api/join-requests", new SubmitJoinRequestRequest(family!.JoinCode!));
        var joinReq = await submitRes.Content.ReadFromJsonAsync<JoinRequestResponse>(TestWebApplicationFactory<Program>.JsonOptions);

        await adminClient.PostAsJsonAsync($"/api/families/join-requests/{joinReq!.Id}/resolve", new ResolveJoinRequestRequest(true));
        var secondRes = await adminClient.PostAsJsonAsync($"/api/families/join-requests/{joinReq.Id}/resolve", new ResolveJoinRequestRequest(true));
        secondRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
```

- [ ] **Step 2: Run tests — all must fail (controllers not yet written)**

```bash
cd backend && dotnet test --filter "FullyQualifiedName~JoinRequestControllerTests"
```
Expected: Build succeeds, tests fail with 404/405 (routes don't exist yet).

- [ ] **Step 3: Commit failing tests**

```bash
git add backend/TinyHeroes.Tests
git commit -m "test: add failing integration tests for join request feature"
```

---

## Task 5: API — JoinRequestController + FamilyController changes

**Files:**
- Create: `backend/TinyHeroes.Api/Controllers/JoinRequestController.cs`
- Modify: `backend/TinyHeroes.Api/Controllers/FamilyController.cs`

- [ ] **Step 1: Create JoinRequestController**

```csharp
// backend/TinyHeroes.Api/Controllers/JoinRequestController.cs
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
```

- [ ] **Step 2: Add admin join-request endpoints to FamilyController**

At the bottom of `FamilyController.cs`, add these two methods before the closing `}`:

```csharp
[HttpGet("join-requests")]
public async Task<ActionResult<List<JoinRequestDtos.JoinRequestResponse>>> GetJoinRequests()
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

    return Ok(requests.Select(r => new JoinRequestDtos.JoinRequestResponse(
        r.Id,
        r.RequestedBy.DisplayName,
        r.RequestedBy.Email!,
        r.RequestedAt,
        r.Status.ToString(),
        r.Family.Name)).ToList());
}

[HttpPost("join-requests/{id:guid}/resolve")]
public async Task<IActionResult> ResolveJoinRequest(Guid id, JoinRequestDtos.ResolveJoinRequestRequest req)
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
```

Also add the using at the top of `FamilyController.cs`:
```csharp
using TinyHeroes.Application.DTOs.JoinRequest;
using TinyHeroes.Domain.Enums;
```

- [ ] **Step 3: Update FamilyController.Create to generate JoinCode**

In the `Create` method of `FamilyController.cs`, update the Family creation line:

```csharp
// Generate a unique 8-char alphanumeric code; retry on collision (unique index enforced by DB)
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
```

- [ ] **Step 4: Update FamilyController.GetMine to expose JoinCode for admins**

Replace the `return Ok(...)` at the end of `GetMine` in `FamilyController.cs`:

```csharp
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
```

- [ ] **Step 5: Run all backend tests**

```bash
cd backend && dotnet test
```
Expected: All tests pass (including the new JoinRequestControllerTests).

- [ ] **Step 6: Commit**

```bash
git add backend/TinyHeroes.Api
git commit -m "feat: JoinRequestController and family admin endpoints"
```

---

## Task 6: Backend version bump

**Files:**
- Modify: `backend/TinyHeroes.Api/TinyHeroes.Api.csproj`

- [ ] **Step 1: Bump version to 1.4.0**

In `TinyHeroes.Api.csproj`, change:
```xml
<Version>1.4.0</Version>
<InformationalVersion>1.4.0</InformationalVersion>
```

- [ ] **Step 2: Build to verify**

```bash
cd backend && dotnet build TinyHeroes.Api
```
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add backend/TinyHeroes.Api/TinyHeroes.Api.csproj
git commit -m "chore: bump api version to 1.4.0"
```

---

## Task 7: Frontend models + JoinRequestService

**Files:**
- Modify: `frontend/src/app/core/models/family.model.ts`
- Create: `frontend/src/app/core/services/join-request.service.ts`
- Create: `frontend/src/app/core/services/join-request.service.spec.ts`

- [ ] **Step 1: Write failing service test**

```typescript
// frontend/src/app/core/services/join-request.service.spec.ts
import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { JoinRequestService } from './join-request.service';
import { JoinRequestResponse } from '../models/family.model';

const mockResponse: JoinRequestResponse = {
  id: 'req-1',
  requesterDisplayName: 'John',
  requesterEmail: 'john@test.com',
  requestedAt: new Date().toISOString(),
  status: 'Pending',
  familyName: 'Test Family'
};

describe('JoinRequestService', () => {
  let service: JoinRequestService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(JoinRequestService);
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('submitRequest posts to /api/join-requests', () => {
    service.submitRequest('HERO4821').subscribe(res => expect(res.status).toBe('Pending'));
    const req = http.expectOne('/api/join-requests');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ joinCode: 'HERO4821' });
    req.flush(mockResponse);
  });

  it('getMyRequest returns null on 404', () => {
    service.getMyRequest().subscribe(res => expect(res).toBeNull());
    const req = http.expectOne('/api/join-requests');
    expect(req.request.method).toBe('GET');
    req.flush('', { status: 404, statusText: 'Not Found' });
  });

  it('getMyRequest returns response on 200', () => {
    service.getMyRequest().subscribe(res => expect(res?.status).toBe('Pending'));
    const req = http.expectOne('/api/join-requests');
    req.flush(mockResponse);
  });

  it('cancelRequest deletes /api/join-requests', () => {
    service.cancelRequest().subscribe();
    const req = http.expectOne('/api/join-requests');
    expect(req.request.method).toBe('DELETE');
    req.flush(null, { status: 204, statusText: 'No Content' });
  });
});
```

- [ ] **Step 2: Run test — must fail**

```bash
cd frontend && npx ng test --include="**/join-request.service.spec.ts" --watch=false
```
Expected: FAIL — `JoinRequestService` not found.

- [ ] **Step 3: Add JoinRequestResponse to family model**

Add to `frontend/src/app/core/models/family.model.ts`:

```typescript
export interface Family {
  id: string;
  name: string;
  weekStartDay: number;
  weeklyMinDeeds: number | null;
  monthlyMinDeeds: number | null;
  members: FamilyMember[];
  joinCode?: string;
}

export interface FamilyMember {
  userId: string;
  displayName: string;
  email: string;
  role: string;
}

export interface SetPrizeRulesRequest {
  weeklyMinDeeds: number | null;
  monthlyMinDeeds: number | null;
}

export interface JoinRequestResponse {
  id: string;
  requesterDisplayName: string;
  requesterEmail: string;
  requestedAt: string;
  status: string;
  familyName: string;
}
```

- [ ] **Step 4: Create JoinRequestService**

```typescript
// frontend/src/app/core/services/join-request.service.ts
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { JoinRequestResponse } from '../models/family.model';

@Injectable({ providedIn: 'root' })
export class JoinRequestService {
  private http = inject(HttpClient);

  submitRequest(joinCode: string): Observable<JoinRequestResponse> {
    return this.http.post<JoinRequestResponse>(`${environment.apiUrl}/join-requests`, { joinCode });
  }

  getMyRequest(): Observable<JoinRequestResponse | null> {
    return this.http.get<JoinRequestResponse>(`${environment.apiUrl}/join-requests`)
      .pipe(catchError(() => of(null)));
  }

  cancelRequest(): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/join-requests`);
  }
}
```

- [ ] **Step 5: Run test — must pass**

```bash
cd frontend && npx ng test --include="**/join-request.service.spec.ts" --watch=false
```
Expected: 4 tests PASS.

- [ ] **Step 6: Extend FamilyService**

Add these two methods to `FamilyService` in `frontend/src/app/core/services/family.service.ts`:

```typescript
getJoinRequests(): Observable<JoinRequestResponse[]> {
  return this.http.get<JoinRequestResponse[]>(`${environment.apiUrl}/families/join-requests`);
}

resolveJoinRequest(id: string, approve: boolean): Observable<void> {
  return this.http.post<void>(`${environment.apiUrl}/families/join-requests/${id}/resolve`, { approve });
}
```

Also add the import at the top:
```typescript
import { Family, SetPrizeRulesRequest, JoinRequestResponse } from '../models/family.model';
```
And add `Observable` to the rxjs import:
```typescript
import { Observable, tap } from 'rxjs';
```

- [ ] **Step 7: Commit**

```bash
git add frontend/src/app/core
git commit -m "feat: JoinRequestService and family model extensions"
```

---

## Task 8: Frontend — AuthService pending state

**Files:**
- Modify: `frontend/src/app/core/auth/auth.service.ts`

- [ ] **Step 1: Update AuthService**

Replace the full content of `frontend/src/app/core/auth/auth.service.ts`:

```typescript
import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, CurrentUser } from '../models/user.model';
import { JoinRequestService } from '../services/join-request.service';

const TOKEN_KEY = 'th_access_token';
const HAS_FAMILY_KEY = 'th_has_family';
const PENDING_REQUEST_KEY = 'th_pending_request';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private joinRequestService = inject(JoinRequestService);
  private _user = signal<CurrentUser | null>(this.loadFromStorage());
  readonly user = this._user.asReadonly();

  constructor(private http: HttpClient, private router: Router) {}

  register(displayName: string, email: string, password: string) {
    return this.http.post<AuthResponse>(`${environment.apiUrl}/auth/register`, { displayName, email, password })
      .pipe(tap(res => this.handleAuthResponse(res)));
  }

  login(email: string, password: string) {
    return this.http.post<AuthResponse>(`${environment.apiUrl}/auth/login`, { email, password })
      .pipe(tap(res => this.handleAuthResponse(res)));
  }

  forgotPassword(email: string) {
    return this.http.post(`${environment.apiUrl}/auth/forgot-password`, { email });
  }

  exchangeCode(code: string) {
    return this.http.post<AuthResponse>(`${environment.apiUrl}/auth/exchange`, { code })
      .pipe(tap(res => this.handleAuthResponse(res)));
  }

  logout() {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(HAS_FAMILY_KEY);
    localStorage.removeItem(PENDING_REQUEST_KEY);
    this._user.set(null);
    this.router.navigate(['/']);
  }

  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  isAuthenticated(): boolean {
    const token = localStorage.getItem(TOKEN_KEY);
    if (!token) return false;
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      if (payload.exp * 1000 < Date.now()) {
        this.logout();
        return false;
      }
      return true;
    } catch {
      return false;
    }
  }

  hasPendingRequest(): boolean {
    return localStorage.getItem(PENDING_REQUEST_KEY) === 'true';
  }

  setPendingRequest(value: boolean) {
    if (value) localStorage.setItem(PENDING_REQUEST_KEY, 'true');
    else localStorage.removeItem(PENDING_REQUEST_KEY);
  }

  // Called from JoinRequestPendingComponent when approved
  markFamilyJoined() {
    localStorage.setItem(HAS_FAMILY_KEY, 'true');
    localStorage.removeItem(PENDING_REQUEST_KEY);
    const user = this._user();
    if (user) this._user.set({ ...user, hasFamily: true });
  }

  // Verify pending request is still valid on bootstrap; navigate away if stale
  verifyPendingRequestOnBootstrap() {
    this.joinRequestService.getMyRequest().subscribe(req => {
      if (req === null) {
        // Request was resolved (approved or rejected) — clear flag
        localStorage.removeItem(PENDING_REQUEST_KEY);
        this.router.navigate(['/create-family']);
      }
    });
  }

  private handleAuthResponse(res: AuthResponse) {
    localStorage.setItem(TOKEN_KEY, res.accessToken);
    localStorage.setItem(HAS_FAMILY_KEY, String(res.hasFamily));
    this._user.set({ userId: res.userId, displayName: res.displayName, email: res.email, hasFamily: res.hasFamily });

    if (res.hasFamily) {
      this.router.navigate(['/dashboard']);
    } else if (this.hasPendingRequest()) {
      this.router.navigate(['/join-request-pending']);
    } else {
      this.router.navigate(['/create-family']);
    }
  }

  private loadFromStorage(): CurrentUser | null {
    const token = localStorage.getItem(TOKEN_KEY);
    if (!token) return null;
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      if (payload.exp * 1000 < Date.now()) {
        localStorage.removeItem(TOKEN_KEY);
        localStorage.removeItem(HAS_FAMILY_KEY);
        localStorage.removeItem(PENDING_REQUEST_KEY);
        return null;
      }
      const hasFamily = localStorage.getItem(HAS_FAMILY_KEY) === 'true';
      return { userId: payload.sub, displayName: payload.displayName, email: payload.email, hasFamily };
    } catch { return null; }
  }
}
```

- [ ] **Step 2: Build to verify**

```bash
cd frontend && npx ng build --configuration development 2>&1 | tail -5
```
Expected: `Build at: ... - Hash: ...` with no errors.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/app/core/auth/auth.service.ts
git commit -m "feat: pending request state in AuthService"
```

---

## Task 9: Frontend — create-family component extension + pending screen

**Files:**
- Modify: `frontend/src/app/features/auth/pages/create-family.component.ts`
- Create: `frontend/src/app/features/auth/pages/join-request-pending.component.ts`
- Create: `frontend/src/app/features/auth/pages/join-request-pending.component.spec.ts`
- Modify: `frontend/src/app/app.routes.ts`

- [ ] **Step 1: Write failing pending component test**

```typescript
// frontend/src/app/features/auth/pages/join-request-pending.component.spec.ts
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { JoinRequestPendingComponent } from './join-request-pending.component';
import { JoinRequestService } from '../../../core/services/join-request.service';
import { AuthService } from '../../../core/auth/auth.service';
import { of } from 'rxjs';

describe('JoinRequestPendingComponent', () => {
  let fixture: ComponentFixture<JoinRequestPendingComponent>;
  let joinRequestService: jasmine.SpyObj<JoinRequestService>;
  let authService: jasmine.SpyObj<AuthService>;

  beforeEach(async () => {
    joinRequestService = jasmine.createSpyObj('JoinRequestService', ['getMyRequest', 'cancelRequest']);
    authService = jasmine.createSpyObj('AuthService', ['markFamilyJoined', 'setPendingRequest']);

    joinRequestService.getMyRequest.and.returnValue(of({
      id: 'req-1', requesterDisplayName: 'John', requesterEmail: 'john@test.com',
      requestedAt: new Date().toISOString(), status: 'Pending', familyName: 'Test Family'
    }));

    await TestBed.configureTestingModule({
      imports: [JoinRequestPendingComponent, TranslateModule.forRoot()],
      providers: [
        provideHttpClient(), provideHttpClientTesting(),
        provideRouter([]),
        { provide: JoinRequestService, useValue: joinRequestService },
        { provide: AuthService, useValue: authService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(JoinRequestPendingComponent);
    fixture.detectChanges();
  });

  it('displays family name from pending request', () => {
    expect(fixture.nativeElement.textContent).toContain('Test Family');
  });

  it('cancel calls cancelRequest and navigates away', fakeAsync(() => {
    joinRequestService.cancelRequest.and.returnValue(of(undefined));
    const cancelBtn = fixture.nativeElement.querySelector('[data-testid="cancel-btn"]');
    cancelBtn.click();
    tick();
    expect(joinRequestService.cancelRequest).toHaveBeenCalled();
    expect(authService.setPendingRequest).toHaveBeenCalledWith(false);
  }));
});
```

- [ ] **Step 2: Run test — must fail**

```bash
cd frontend && npx ng test --include="**/join-request-pending.component.spec.ts" --watch=false
```
Expected: FAIL — component not found.

- [ ] **Step 3: Create JoinRequestPendingComponent**

```typescript
// frontend/src/app/features/auth/pages/join-request-pending.component.ts
import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { interval, Subscription, switchMap } from 'rxjs';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { JoinRequestService } from '../../../core/services/join-request.service';
import { AuthService } from '../../../core/auth/auth.service';
import { FamilyService } from '../../../core/services/family.service';
import { JoinRequestResponse } from '../../../core/models/family.model';

@Component({
  selector: 'app-join-request-pending',
  imports: [TranslatePipe],
  template: `
    <div class="min-h-screen bg-brand-cream flex flex-col items-center justify-center p-6">
      <div class="w-full max-w-sm text-center">
        <div class="text-5xl mb-4">⏳</div>
        <h1 class="text-xl font-black text-brand-text mb-2">{{ 'JOIN.PENDING_TITLE' | translate }}</h1>
        @if (pendingRequest()) {
          <p class="text-brand-muted text-sm mb-6">
            {{ 'JOIN.PENDING_SUBTITLE' | translate : { family: pendingRequest()!.familyName } }}
          </p>
        }
        <button data-testid="cancel-btn" (click)="cancel()" [disabled]="cancelling()"
          class="w-full bg-white border border-brand-border text-brand-text font-bold py-3 rounded-2xl text-sm">
          {{ cancelling() ? '...' : ('JOIN.CANCEL_REQUEST' | translate) }}
        </button>
      </div>
    </div>
  `
})
export class JoinRequestPendingComponent implements OnInit, OnDestroy {
  private joinRequestService = inject(JoinRequestService);
  private authService = inject(AuthService);
  private familyService = inject(FamilyService);
  private router = inject(Router);
  private ts = inject(TranslateService);

  pendingRequest = signal<JoinRequestResponse | null>(null);
  cancelling = signal(false);
  private pollSub?: Subscription;

  ngOnInit() {
    this.joinRequestService.getMyRequest().subscribe(req => this.pendingRequest.set(req));
    // Poll every 30 seconds
    this.pollSub = interval(30000).pipe(
      switchMap(() => this.joinRequestService.getMyRequest())
    ).subscribe(req => {
      if (req === null) {
        // Request gone — either approved (backend created FamilyMember) or rejected
        // Try loading family to distinguish
        this.familyService.loadFamily();
        // Give family service a moment, then check
        setTimeout(() => {
          const family = this.familyService.family();
          if (family) {
            this.authService.markFamilyJoined();
            this.router.navigate(['/dashboard']);
          } else {
            this.authService.setPendingRequest(false);
            this.router.navigate(['/create-family']);
          }
        }, 500);
      }
    });
  }

  ngOnDestroy() {
    this.pollSub?.unsubscribe();
  }

  cancel() {
    this.cancelling.set(true);
    this.joinRequestService.cancelRequest().subscribe({
      next: () => {
        this.authService.setPendingRequest(false);
        this.router.navigate(['/create-family']);
      },
      error: () => this.cancelling.set(false)
    });
  }
}
```

- [ ] **Step 4: Run pending component test — must pass**

```bash
cd frontend && npx ng test --include="**/join-request-pending.component.spec.ts" --watch=false
```
Expected: 2 tests PASS.

- [ ] **Step 5: Extend create-family component with "Join with a Code" option**

Replace the full template and class body of `create-family.component.ts`:

```typescript
// frontend/src/app/features/auth/pages/create-family.component.ts
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { environment } from '../../../../environments/environment';
import { JoinRequestService } from '../../../core/services/join-request.service';
import { AuthService } from '../../../core/auth/auth.service';

type DayOfWeek = 0 | 1 | 2 | 3 | 4 | 5 | 6;

const DAYS: { key: DayOfWeek; labelKey: string }[] = [
  { key: 1, labelKey: 'FAMILY.DAYS.MON' }, { key: 2, labelKey: 'FAMILY.DAYS.TUE' }, { key: 3, labelKey: 'FAMILY.DAYS.WED' },
  { key: 4, labelKey: 'FAMILY.DAYS.THU' }, { key: 5, labelKey: 'FAMILY.DAYS.FRI' }, { key: 6, labelKey: 'FAMILY.DAYS.SAT' }, { key: 0, labelKey: 'FAMILY.DAYS.SUN' }
];

@Component({
  selector: 'app-create-family',
  imports: [ReactiveFormsModule, TranslatePipe],
  template: `
    <div class="min-h-screen bg-brand-cream flex flex-col items-center justify-center p-6">
      <div class="w-full max-w-sm">
        <div class="text-center mb-6">
          <div class="text-5xl mb-3">🏡</div>
          <h1 class="text-xl font-black text-brand-text">{{ 'FAMILY.CREATE_TITLE' | translate }}</h1>
          <p class="text-brand-muted text-sm mt-1">{{ 'FAMILY.CHANGE_LATER' | translate }}</p>
        </div>

        @if (!showJoinForm()) {
          <!-- Create form -->
          <form [formGroup]="form" (ngSubmit)="submit()" class="flex flex-col gap-4">
            <div>
              <label class="text-xs font-bold text-brand-muted uppercase tracking-wide mb-1 block">{{ 'FAMILY.NAME_LABEL' | translate }}</label>
              <input formControlName="name" type="text" class="w-full bg-white border border-brand-border rounded-xl px-3 py-2.5 text-sm text-brand-text" [placeholder]="'FAMILY.NAME_PLACEHOLDER' | translate"/>
            </div>
            <div>
              <label class="text-xs font-bold text-brand-muted uppercase tracking-wide mb-2 block">{{ 'FAMILY.WEEK_STARTS' | translate }}</label>
              <div class="flex gap-2 flex-wrap">
                @for (day of days; track day.key) {
                  <button type="button" (click)="selectDay(day.key)"
                    [class]="selectedDay === day.key
                      ? 'bg-brand-bg border-2 border-brand-orange text-brand-orange font-bold text-sm rounded-xl px-3 py-1.5'
                      : 'bg-white border border-brand-border text-brand-muted font-semibold text-sm rounded-xl px-3 py-1.5'">
                    {{ day.labelKey | translate }}
                  </button>
                }
              </div>
            </div>
            <div class="bg-brand-bg border border-brand-border rounded-xl p-3 text-xs text-brand-muted">
              {{ 'FAMILY.PODIUM_NOTE' | translate }}
            </div>
            @if (error()) { <p class="text-red-500 text-xs text-center">{{ error() }}</p> }
            <button type="submit" [disabled]="form.invalid || loading()"
              class="w-full bg-gradient-to-r from-brand-orange to-orange-400 text-white font-bold py-3 rounded-2xl text-base">
              {{ loading() ? '...' : ('FAMILY.CREATE_CTA' | translate) }}
            </button>
          </form>

          <!-- Divider -->
          <div class="flex items-center gap-3 my-4">
            <div class="flex-1 h-px bg-brand-border"></div>
            <span class="text-xs text-brand-muted">{{ 'FAMILY.OR' | translate }}</span>
            <div class="flex-1 h-px bg-brand-border"></div>
          </div>

          <button (click)="showJoinForm.set(true)"
            class="w-full bg-white border border-brand-border text-brand-text font-bold py-3 rounded-2xl text-sm">
            {{ 'JOIN.JOIN_WITH_CODE' | translate }}
          </button>

        } @else {
          <!-- Join form -->
          <div class="flex flex-col gap-4">
            <div>
              <label class="text-xs font-bold text-brand-muted uppercase tracking-wide mb-1 block">{{ 'JOIN.ENTER_CODE_LABEL' | translate }}</label>
              <input [(ngModel)]="joinCode" type="text" maxlength="8"
                class="w-full bg-white border border-brand-border rounded-xl px-3 py-2.5 text-sm text-brand-text uppercase tracking-widest"
                [placeholder]="'JOIN.CODE_PLACEHOLDER' | translate" />
            </div>
            @if (error()) { <p class="text-red-500 text-xs text-center">{{ error() }}</p> }
            <button (click)="submitJoinRequest()" [disabled]="!joinCode.trim() || loading()"
              class="w-full bg-gradient-to-r from-brand-orange to-orange-400 text-white font-bold py-3 rounded-2xl text-base">
              {{ loading() ? '...' : ('JOIN.SEND_REQUEST' | translate) }}
            </button>
            <button (click)="showJoinForm.set(false); error.set('')"
              class="w-full bg-white border border-brand-border text-brand-muted font-bold py-2.5 rounded-2xl text-sm">
              ← {{ 'SHARED.BACK' | translate }}
            </button>
          </div>
        }
      </div>
    </div>
  `
})
export class CreateFamilyComponent {
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private router = inject(Router);
  private ts = inject(TranslateService);
  private joinRequestService = inject(JoinRequestService);
  private authService = inject(AuthService);

  days = DAYS;
  selectedDay: DayOfWeek = 1;
  loading = signal(false);
  error = signal('');
  showJoinForm = signal(false);
  joinCode = '';

  form = this.fb.nonNullable.group({ name: ['', Validators.required] });

  selectDay(day: DayOfWeek) { this.selectedDay = day; }

  submit() {
    if (this.form.invalid) return;
    this.loading.set(true);
    this.http.post(`${environment.apiUrl}/families`, { name: this.form.value.name, weekStartDay: this.selectedDay })
      .subscribe({
        next: () => this.router.navigate(['/dashboard']),
        error: () => { this.error.set(this.ts.instant('FAMILY.ERROR_CREATE')); this.loading.set(false); }
      });
  }

  submitJoinRequest() {
    if (!this.joinCode.trim()) return;
    this.loading.set(true);
    this.error.set('');
    this.joinRequestService.submitRequest(this.joinCode.trim().toUpperCase()).subscribe({
      next: () => {
        this.authService.setPendingRequest(true);
        this.router.navigate(['/join-request-pending']);
      },
      error: (err) => {
        const status = err?.status;
        this.error.set(status === 404
          ? this.ts.instant('JOIN.INVALID_CODE')
          : this.ts.instant('JOIN.ALREADY_PENDING'));
        this.loading.set(false);
      }
    });
  }
}
```

Also add `FormsModule` to the imports array: `imports: [ReactiveFormsModule, TranslatePipe, FormsModule]`.

- [ ] **Step 6: Add join-request-pending route to app.routes.ts**

After the `create-family` route line, add:
```typescript
{ path: 'join-request-pending', canActivate: [authGuard], loadComponent: () => import('./features/auth/pages/join-request-pending.component').then(m => m.JoinRequestPendingComponent) },
```

- [ ] **Step 7: Build to verify**

```bash
cd frontend && npx ng build --configuration development 2>&1 | tail -5
```
Expected: Build succeeded, 0 errors.

- [ ] **Step 8: Commit**

```bash
git add frontend/src/app/features/auth frontend/src/app/app.routes.ts
git commit -m "feat: create-family join-with-code flow and pending screen"
```

---

## Task 10: Frontend — Admin UI (FamilySettings + Shell banner)

**Files:**
- Modify: `frontend/src/app/features/settings/pages/family-settings.component.ts`
- Modify: `frontend/src/app/shared/components/shell.component.ts`

- [ ] **Step 1: Add JoinCode display + join requests modal to FamilySettingsComponent**

Below the `<!-- Co-parents -->` section in the template, add the following block (insert before the save button):

```html
<!-- Join Code (admin only) -->
@if (isAdmin() && family()?.joinCode) {
  <p class="text-xs font-bold text-brand-orange uppercase tracking-wide mb-2 mt-4">{{ 'JOIN.FAMILY_CODE_LABEL' | translate }}</p>
  <div class="bg-white border border-brand-border rounded-xl p-3 flex items-center justify-between mb-4">
    <span class="font-mono font-bold text-brand-text tracking-widest text-lg">{{ family()!.joinCode }}</span>
    <button (click)="copyJoinCode()" class="text-xs text-brand-orange font-bold">
      {{ joinCodeCopied() ? '✓' : ('JOIN.COPY_CODE' | translate) }}
    </button>
  </div>
}

<!-- Join Requests (admin only) -->
@if (isAdmin() && joinRequestCount() > 0) {
  <button (click)="showRequestsModal.set(true)"
    class="w-full bg-blue-50 border border-blue-200 text-blue-700 rounded-xl py-2.5 text-sm font-bold mb-4">
    👥 {{ 'JOIN.REQUESTS_BADGE' | translate : { count: joinRequestCount() } }}
  </button>
}

<!-- Modal -->
@if (showRequestsModal()) {
  <div class="fixed inset-0 bg-black/40 flex items-end justify-center z-50" (click)="showRequestsModal.set(false)">
    <div class="bg-white rounded-t-2xl w-full max-w-lg p-4 pb-8" (click)="$event.stopPropagation()">
      <h2 class="font-bold text-brand-text mb-3">{{ 'JOIN.MODAL_TITLE' | translate }}</h2>
      @for (req of joinRequests(); track req.id) {
        <div class="flex items-center justify-between py-2 border-b border-brand-border">
          <div>
            <p class="text-sm font-bold text-brand-text">{{ req.requesterDisplayName }}</p>
            <p class="text-xs text-brand-muted">{{ req.requesterEmail }}</p>
          </div>
          <div class="flex gap-2">
            <button (click)="resolve(req.id, true)"
              class="text-xs bg-green-100 text-green-700 font-bold px-3 py-1.5 rounded-xl">{{ 'JOIN.APPROVE' | translate }}</button>
            <button (click)="resolve(req.id, false)"
              class="text-xs bg-red-100 text-red-500 font-bold px-3 py-1.5 rounded-xl">{{ 'JOIN.REJECT' | translate }}</button>
          </div>
        </div>
      }
    </div>
  </div>
}
```

Add these to the class body of `FamilySettingsComponent`:
```typescript
import { JoinRequestResponse } from '../../../core/models/family.model';
// in class:
joinRequests = signal<JoinRequestResponse[]>([]);
joinRequestCount = computed(() => this.joinRequests().length);
showRequestsModal = signal(false);
joinCodeCopied = signal(false);

// in ngOnInit:
if (this.isAdmin()) {
  this.familyService.getJoinRequests().subscribe(reqs => this.joinRequests.set(reqs));
}

resolve(id: string, approve: boolean) {
  this.familyService.resolveJoinRequest(id, approve).subscribe({
    next: () => {
      this.joinRequests.update(reqs => reqs.filter(r => r.id !== id));
      if (this.joinRequests().length === 0) this.showRequestsModal.set(false);
    }
  });
}

copyJoinCode() {
  const code = this.family()?.joinCode;
  if (!code) return;
  navigator.clipboard.writeText(code);
  this.joinCodeCopied.set(true);
  setTimeout(() => this.joinCodeCopied.set(false), 2000);
}
```

Also add `signal`, `computed` to the Angular imports if not already present.

- [ ] **Step 2: Add admin banner to ShellComponent**

Replace `shell.component.ts` with:

```typescript
import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { BottomNavComponent } from './bottom-nav.component';
import { SideNavComponent } from './side-nav.component';
import { UserService } from '../../core/services/user.service';
import { FamilyService } from '../../core/services/family.service';

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, BottomNavComponent, SideNavComponent, TranslatePipe],
  template: `
    <div class="min-h-screen bg-brand-bg flex flex-col">
      @if (pendingRequestCount() > 0) {
        <div class="bg-blue-50 border-b border-blue-200 px-4 py-2 flex items-center justify-between">
          <span class="text-sm text-blue-700 font-medium">
            👥 {{ 'JOIN.BANNER_TEXT' | translate : { count: pendingRequestCount() } }}
          </span>
          <button (click)="router.navigate(['/settings/family'])"
            class="text-xs text-blue-700 font-bold underline">
            {{ 'JOIN.BANNER_REVIEW' | translate }}
          </button>
        </div>
      }
      <div class="flex flex-1">
        <app-side-nav class="hidden md:block" />
        <div class="flex-1 pb-16 md:pb-0 md:ml-16 lg:ml-48 min-w-0">
          <router-outlet />
        </div>
      </div>
    </div>
    <app-bottom-nav />
  `
})
export class ShellComponent implements OnInit {
  private userService = inject(UserService);
  private familyService = inject(FamilyService);
  protected router = inject(Router);

  pendingRequestCount = signal(0);

  ngOnInit() {
    this.userService.loadProfile();
    if (this.familyService.isAdmin()) {
      this.familyService.getJoinRequests().subscribe(reqs => this.pendingRequestCount.set(reqs.length));
    }
  }
}
```

- [ ] **Step 3: Build to verify**

```bash
cd frontend && npx ng build --configuration development 2>&1 | tail -5
```
Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/app/features/settings frontend/src/app/shared
git commit -m "feat: admin join request UI in family settings and shell banner"
```

---

## Task 11: i18n keys

**Files:**
- Modify: `frontend/public/assets/i18n/en.json` (+ hu.json, de.json, fr.json, es.json)

- [ ] **Step 1: Add keys to en.json**

Open `frontend/public/assets/i18n/en.json` and add the following top-level key block (merge into the existing JSON object):

```json
"JOIN": {
  "JOIN_WITH_CODE": "Join a Family",
  "ENTER_CODE_LABEL": "Family Code",
  "CODE_PLACEHOLDER": "HERO4821",
  "SEND_REQUEST": "Send Join Request",
  "INVALID_CODE": "No family found with that code.",
  "ALREADY_PENDING": "You already have a pending join request.",
  "PENDING_TITLE": "Request Pending",
  "PENDING_SUBTITLE": "Waiting for {{family}} admin to approve your request.",
  "CANCEL_REQUEST": "Cancel Request",
  "FAMILY_CODE_LABEL": "Family Join Code",
  "COPY_CODE": "Copy",
  "REQUESTS_BADGE": "{{count}} join request(s) pending",
  "MODAL_TITLE": "Pending Join Requests",
  "APPROVE": "Approve",
  "REJECT": "Reject",
  "BANNER_TEXT": "{{count}} person(s) want to join your family.",
  "BANNER_REVIEW": "Review →"
}
```

Also add to `FAMILY`: `"OR": "or"`
Also add to `SHARED`: `"BACK": "Back"`

- [ ] **Step 2: Mirror keys to other language files**

For `hu.json`, `de.json`, `fr.json`, `es.json` — add the same `JOIN` block with identical English values as placeholders (translators will fill them in later).

- [ ] **Step 3: Commit**

```bash
git add frontend/public/assets/i18n
git commit -m "feat: i18n keys for join request feature"
```

---

## Task 12: Spec doc, CHANGELOG, and version bumps

**Files:**
- Create: `docs/superpowers/specs/2026-06-01-family-join-request-design.md`
- Modify: `CHANGELOG.md`
- Modify: `frontend/src/environments/environment.ts`
- Modify: `frontend/src/environments/environment.prod.ts`

- [ ] **Step 1: Write spec document**

Create `docs/superpowers/specs/2026-06-01-family-join-request-design.md` with the design doc content from the approved plan (see plan context). Keep it concise: data model, API endpoints, frontend flow, security rules.

- [ ] **Step 2: Create GitHub issue**

```bash
gh issue create \
  --title "Family join request flow — user-initiated join with admin approval" \
  --label "enhancement" \
  --body "## Summary
A registered user without a family can enter a short family code on the 'no family' screen, submit a join request, and wait for the admin to approve or reject it.

## Scope
- Permanent \`JoinCode\` field on \`Family\` entity
- New \`FamilyJoinRequest\` entity with Pending/Approved/Rejected status
- New \`JoinRequestController\` (requester-side CRUD)
- Admin endpoints on \`FamilyController\` (list + resolve)
- Frontend: create-family extension, pending screen, admin modal + shell banner

See design spec: \`docs/superpowers/specs/2026-06-01-family-join-request-design.md\`"
```

- [ ] **Step 3: Update CHANGELOG.md**

Add under `## [Unreleased]` (or promote to a new version block if releasing):

```markdown
### Added
- Family join code: each family now has a permanent 8-character code admins can share
- Users without a family can enter a join code to send a request to the admin
- Admin sees a dashboard banner and a modal in Family Settings to approve or reject requests
- Requester sees a locked pending screen with a cancel option while waiting
```

- [ ] **Step 4: Bump frontend version**

In `frontend/src/environments/environment.ts` and `environment.prod.ts`, change:
```typescript
version: '2.3.0',
```

- [ ] **Step 5: Commit**

```bash
git add docs/superpowers/specs/2026-06-01-family-join-request-design.md CHANGELOG.md frontend/src/environments
git commit -m "chore: bump frontend version to 2.3.0 and add join request changelog"
```

---

## Task 13: Help component update

**Files:**
- Modify: `frontend/src/app/features/help/help.component.ts`

- [ ] **Step 1: Add join request step**

Open `frontend/src/app/features/help/help.component.ts` and add a new step in the family onboarding section describing:
- Admins can share a Join Code from Settings → Family
- New users can enter the code on the "no family" screen to send a join request
- Admins approve or reject from the dashboard banner or Settings → Family

- [ ] **Step 2: Commit**

```bash
git add frontend/src/app/features/help/help.component.ts
git commit -m "docs: add join request flow to help guide"
```

---

## Task 14: End-to-end verification

- [ ] **Step 1: Run all backend tests**

```bash
cd backend && dotnet test
```
Expected: All tests PASS.

- [ ] **Step 2: Run all frontend tests**

```bash
cd frontend && npx ng test --watch=false
```
Expected: All tests PASS.

- [ ] **Step 3: Start the stack and manual test**

```bash
docker compose up -d
cd frontend && npm start
```

1. Register a new user → lands on `/create-family`
2. Click "Join a Family" → enter a valid code from an admin account → submit
3. → navigates to `/join-request-pending`, shows family name
4. Log in as Admin in another tab → banner appears at top of dashboard
5. Admin opens Settings → Family → sees join request badge → opens modal → approves
6. Pending screen auto-navigates to `/dashboard` within 30s

- [ ] **Step 4: Security smoke test**

1. In browser DevTools: `localStorage.setItem('th_has_family', 'true')` on a user with only a pending request
2. Reload page — app may show dashboard route but all `/api/families/*` calls return 404
3. The UI should gracefully handle the empty state

- [ ] **Step 5: Run code review and security review**

```
/code-review
/security-review
```
Address all findings before merging.
