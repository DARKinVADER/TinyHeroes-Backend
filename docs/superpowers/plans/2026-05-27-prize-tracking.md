# Prize Tracking & Comments Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a "used" toggle and comment thread to each prize shown in the History tab so parents and co-parents can track real-world prize delivery.

**Architecture:** Two new backend tables (`PrizeClaims`, `PrizeComments`) with a new `PrizeClaimController`. Claims are created lazily on first interaction. The frontend adds a "🎁 Prizes" section to the existing `HistoryComponent` with inline-expandable prize cards.

**Tech Stack:** .NET 8 / EF Core / ASP.NET Identity (backend), Angular 21 standalone components + signals (frontend), Playwright (e2e tests), xUnit + FluentAssertions (backend tests).

---

## File Map

**New backend files:**
- `TinyHeroes.Domain/Entities/PrizeClaim.cs`
- `TinyHeroes.Domain/Entities/PrizeComment.cs`
- `TinyHeroes.Infrastructure/Data/Configurations/PrizeClaimConfiguration.cs`
- `TinyHeroes.Infrastructure/Data/Configurations/PrizeCommentConfiguration.cs`
- `TinyHeroes.Application/DTOs/Prize/PrizeClaimDtos.cs`
- `TinyHeroes.Api/Controllers/PrizeClaimController.cs`
- `TinyHeroes.Tests/Integration/PrizeClaimControllerTests.cs`

**Modified backend files:**
- `TinyHeroes.Infrastructure/Data/AppDbContext.cs` — add `PrizeClaims`, `PrizeComments` DbSets
- `TinyHeroes.Infrastructure/TinyHeroes.Infrastructure.csproj` — no change expected (EF already referenced)

**New frontend files:**
- `frontend/src/app/core/models/prize-claim.model.ts`
- `frontend/src/app/core/services/prize-claim.service.ts`
- `frontend/e2e/prize-tracking.spec.ts`

**Modified frontend files:**
- `frontend/src/app/features/podium/pages/history.component.ts` — add prizes section
- `frontend/public/assets/i18n/en.json` — add PRIZE_CLAIM keys
- `frontend/e2e/helpers/api-mocks.ts` — add `mockPrizeClaimsApi` and `mockSummariesApi`

---

## Task 1: Domain entities

**Files:**
- Create: `backend/TinyHeroes.Domain/Entities/PrizeClaim.cs`
- Create: `backend/TinyHeroes.Domain/Entities/PrizeComment.cs`

- [ ] **Step 1: Create `PrizeClaim.cs`**

```csharp
// backend/TinyHeroes.Domain/Entities/PrizeClaim.cs
namespace TinyHeroes.Domain.Entities;

public class PrizeClaim
{
    public Guid Id { get; set; }
    public Guid FamilyId { get; set; }
    public string Scope { get; set; } = "weekly";
    public Guid? WeekSummaryId { get; set; }
    public Guid? MonthSummaryId { get; set; }
    public int? Rank { get; set; }
    public Guid ChildId { get; set; }
    public string ChildName { get; set; } = string.Empty;
    public string PrizeEmoji { get; set; } = string.Empty;
    public string PrizeLabel { get; set; } = string.Empty;
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Family Family { get; set; } = null!;
    public WeekSummary? WeekSummary { get; set; }
    public MonthSummary? MonthSummary { get; set; }
    public ICollection<PrizeComment> Comments { get; set; } = [];
}
```

- [ ] **Step 2: Create `PrizeComment.cs`**

```csharp
// backend/TinyHeroes.Domain/Entities/PrizeComment.cs
namespace TinyHeroes.Domain.Entities;

public class PrizeComment
{
    public Guid Id { get; set; }
    public Guid PrizeClaimId { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public PrizeClaim PrizeClaim { get; set; } = null!;
}
```

- [ ] **Step 3: Commit**

```bash
cd /Volumes/PersonalProtected/GIT/TinyHeroes
git add backend/TinyHeroes.Domain/Entities/PrizeClaim.cs backend/TinyHeroes.Domain/Entities/PrizeComment.cs
git commit -m "feat: add PrizeClaim and PrizeComment domain entities"
```

---

## Task 2: EF Core configuration and DbContext

**Files:**
- Create: `backend/TinyHeroes.Infrastructure/Data/Configurations/PrizeClaimConfiguration.cs`
- Create: `backend/TinyHeroes.Infrastructure/Data/Configurations/PrizeCommentConfiguration.cs`
- Modify: `backend/TinyHeroes.Infrastructure/Data/AppDbContext.cs`

- [ ] **Step 1: Create `PrizeClaimConfiguration.cs`**

```csharp
// backend/TinyHeroes.Infrastructure/Data/Configurations/PrizeClaimConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TinyHeroes.Domain.Entities;

namespace TinyHeroes.Infrastructure.Data.Configurations;

public class PrizeClaimConfiguration : IEntityTypeConfiguration<PrizeClaim>
{
    public void Configure(EntityTypeBuilder<PrizeClaim> builder)
    {
        builder.HasKey(p => p.Id);
        builder.HasOne(p => p.Family).WithMany().HasForeignKey(p => p.FamilyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(p => p.WeekSummary).WithMany().HasForeignKey(p => p.WeekSummaryId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(p => p.MonthSummary).WithMany().HasForeignKey(p => p.MonthSummaryId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(p => new { p.FamilyId, p.Scope, p.WeekSummaryId, p.MonthSummaryId, p.Rank }).IsUnique();
        builder.Property(p => p.Scope).HasMaxLength(10);
        builder.Property(p => p.ChildName).HasMaxLength(100);
        builder.Property(p => p.PrizeEmoji).HasMaxLength(50);
        builder.Property(p => p.PrizeLabel).HasMaxLength(200);
    }
}
```

- [ ] **Step 2: Create `PrizeCommentConfiguration.cs`**

```csharp
// backend/TinyHeroes.Infrastructure/Data/Configurations/PrizeCommentConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TinyHeroes.Domain.Entities;

namespace TinyHeroes.Infrastructure.Data.Configurations;

public class PrizeCommentConfiguration : IEntityTypeConfiguration<PrizeComment>
{
    public void Configure(EntityTypeBuilder<PrizeComment> builder)
    {
        builder.HasKey(p => p.Id);
        builder.HasOne(p => p.PrizeClaim).WithMany(c => c.Comments).HasForeignKey(p => p.PrizeClaimId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(p => p.Text).HasMaxLength(1000);
    }
}
```

- [ ] **Step 3: Add DbSets to `AppDbContext.cs`**

In `backend/TinyHeroes.Infrastructure/Data/AppDbContext.cs`, add two lines after the existing `PrizeAssignments` DbSet:

```csharp
    public DbSet<PrizeClaim> PrizeClaims => Set<PrizeClaim>();
    public DbSet<PrizeComment> PrizeComments => Set<PrizeComment>();
```

- [ ] **Step 4: Create EF Core migration**

```bash
cd backend
dotnet ef migrations add AddPrizeClaimsAndComments \
  --project TinyHeroes.Infrastructure \
  --startup-project TinyHeroes.Api
```

Expected: a new migration file appears in `TinyHeroes.Infrastructure/Migrations/`.

- [ ] **Step 5: Commit**

```bash
cd /Volumes/PersonalProtected/GIT/TinyHeroes
git add backend/TinyHeroes.Infrastructure/Data/Configurations/PrizeClaimConfiguration.cs
git add backend/TinyHeroes.Infrastructure/Data/Configurations/PrizeCommentConfiguration.cs
git add backend/TinyHeroes.Infrastructure/Data/AppDbContext.cs
git add backend/TinyHeroes.Infrastructure/Migrations/
git commit -m "feat: EF Core config and migration for PrizeClaims and PrizeComments"
```

---

## Task 3: DTOs

**Files:**
- Create: `backend/TinyHeroes.Application/DTOs/Prize/PrizeClaimDtos.cs`

- [ ] **Step 1: Create `PrizeClaimDtos.cs`**

```csharp
// backend/TinyHeroes.Application/DTOs/Prize/PrizeClaimDtos.cs
namespace TinyHeroes.Application.DTOs.Prize;

public record CreatePrizeClaimRequest(
    string Scope,
    Guid? WeekSummaryId,
    Guid? MonthSummaryId,
    int? Rank,
    Guid ChildId,
    string ChildName,
    string PrizeEmoji,
    string PrizeLabel
);

public record UpdateUsedRequest(bool IsUsed);

public record AddCommentRequest(string Text);

public record PrizeCommentDto(Guid Id, string Text, DateTime CreatedAt);

public record PrizeClaimDto(
    Guid Id,
    string Scope,
    Guid? WeekSummaryId,
    Guid? MonthSummaryId,
    int? Rank,
    Guid ChildId,
    string ChildName,
    string PrizeEmoji,
    string PrizeLabel,
    bool IsUsed,
    DateTime? UsedAt,
    DateTime CreatedAt,
    List<PrizeCommentDto> Comments
);
```

- [ ] **Step 2: Commit**

```bash
cd /Volumes/PersonalProtected/GIT/TinyHeroes
git add backend/TinyHeroes.Application/DTOs/Prize/PrizeClaimDtos.cs
git commit -m "feat: add PrizeClaim DTOs"
```

---

## Task 4: Controller (write failing tests first)

**Files:**
- Create: `backend/TinyHeroes.Tests/Integration/PrizeClaimControllerTests.cs`
- Create: `backend/TinyHeroes.Api/Controllers/PrizeClaimController.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// backend/TinyHeroes.Tests/Integration/PrizeClaimControllerTests.cs
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TinyHeroes.Application.DTOs.Auth;
using TinyHeroes.Application.DTOs.Family;
using TinyHeroes.Application.DTOs.Invite;
using TinyHeroes.Application.DTOs.Prize;
using TinyHeroes.Application.DTOs.Summary;
using TinyHeroes.Tests.Integration.Helpers;

namespace TinyHeroes.Tests.Integration;

public class PrizeClaimControllerTests(TestWebApplicationFactory<Program> factory)
    : IClassFixture<TestWebApplicationFactory<Program>>
{
    private async Task<(HttpClient client, Guid weekSummaryId)> SetupWithSummary()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);

        // Add a child
        var childResponse = await client.PostAsJsonAsync("/api/children",
            new { Name = "Alice", Age = 7, Gender = "Girl", AvatarEmoji = "🦸" });
        childResponse.EnsureSuccessStatusCode();
        var child = await childResponse.Content.ReadFromJsonAsync<dynamic>();

        // Get weeks — will generate a summary if any complete weeks exist
        var weeksResponse = await client.GetAsync("/api/summaries/weeks");
        var weeks = await weeksResponse.Content.ReadFromJsonAsync<List<WeekSummaryResponse>>();

        // For testing, create a claim directly without a real summary
        // Use Guid.Empty as a stand-in and verify the unique constraint
        return (client, weeks?.FirstOrDefault()?.Id ?? Guid.NewGuid());
    }

    [Fact]
    public async Task GetByWeekSummary_ReturnsEmptyList_WhenNoClaimsExist()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);
        var summaryId = Guid.NewGuid();

        var response = await client.GetAsync($"/api/prize-claims?weekSummaryId={summaryId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var claims = await response.Content.ReadFromJsonAsync<List<PrizeClaimDto>>();
        claims.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByMonthSummary_ReturnsEmptyList_WhenNoClaimsExist()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);
        var summaryId = Guid.NewGuid();

        var response = await client.GetAsync($"/api/prize-claims?monthSummaryId={summaryId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var claims = await response.Content.ReadFromJsonAsync<List<PrizeClaimDto>>();
        claims.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateClaim_ReturnsClaim_WithEmptyComments()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);
        var summaryId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        var req = new CreatePrizeClaimRequest("weekly", summaryId, null, 1, childId, "Alice", "🍕", "Pizza night");
        var response = await client.PostAsJsonAsync("/api/prize-claims", req);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var claim = await response.Content.ReadFromJsonAsync<PrizeClaimDto>();
        claim.Should().NotBeNull();
        claim!.PrizeLabel.Should().Be("Pizza night");
        claim.IsUsed.Should().BeFalse();
        claim.Comments.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateClaim_Idempotent_ReturnsSameClaimOnDuplicate()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);
        var summaryId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var req = new CreatePrizeClaimRequest("weekly", summaryId, null, 1, childId, "Alice", "🍕", "Pizza night");

        var first = await client.PostAsJsonAsync("/api/prize-claims", req);
        var second = await client.PostAsJsonAsync("/api/prize-claims", req);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var c1 = await first.Content.ReadFromJsonAsync<PrizeClaimDto>();
        var c2 = await second.Content.ReadFromJsonAsync<PrizeClaimDto>();
        c1!.Id.Should().Be(c2!.Id);
    }

    [Fact]
    public async Task MarkUsed_SetsIsUsedAndUsedAt()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);
        var summaryId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var createReq = new CreatePrizeClaimRequest("weekly", summaryId, null, 1, childId, "Alice", "🍕", "Pizza night");
        var createResponse = await client.PostAsJsonAsync("/api/prize-claims", createReq);
        var claim = await createResponse.Content.ReadFromJsonAsync<PrizeClaimDto>();

        var response = await client.PutAsJsonAsync($"/api/prize-claims/{claim!.Id}/used", new UpdateUsedRequest(true));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<PrizeClaimDto>();
        updated!.IsUsed.Should().BeTrue();
        updated.UsedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkUnused_ClearsIsUsedAndUsedAt()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);
        var summaryId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var createReq = new CreatePrizeClaimRequest("weekly", summaryId, null, 1, childId, "Alice", "🍕", "Pizza night");
        var createResponse = await client.PostAsJsonAsync("/api/prize-claims", createReq);
        var claim = await createResponse.Content.ReadFromJsonAsync<PrizeClaimDto>();
        await client.PutAsJsonAsync($"/api/prize-claims/{claim!.Id}/used", new UpdateUsedRequest(true));

        var response = await client.PutAsJsonAsync($"/api/prize-claims/{claim.Id}/used", new UpdateUsedRequest(false));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<PrizeClaimDto>();
        updated!.IsUsed.Should().BeFalse();
        updated.UsedAt.Should().BeNull();
    }

    [Fact]
    public async Task AddComment_AppearsInSubsequentGet()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);
        var summaryId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var createReq = new CreatePrizeClaimRequest("weekly", summaryId, null, 1, childId, "Alice", "🍕", "Pizza night");
        var createResponse = await client.PostAsJsonAsync("/api/prize-claims", createReq);
        var claim = await createResponse.Content.ReadFromJsonAsync<PrizeClaimDto>();

        var commentResponse = await client.PostAsJsonAsync($"/api/prize-claims/{claim!.Id}/comments", new AddCommentRequest("Given out Friday!"));

        commentResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var getResponse = await client.GetAsync($"/api/prize-claims?weekSummaryId={summaryId}");
        var claims = await getResponse.Content.ReadFromJsonAsync<List<PrizeClaimDto>>();
        claims!.Single().Comments.Should().ContainSingle(c => c.Text == "Given out Friday!");
    }

    [Fact]
    public async Task DeleteComment_RemovesItFromGet()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);
        var summaryId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var createReq = new CreatePrizeClaimRequest("weekly", summaryId, null, 1, childId, "Alice", "🍕", "Pizza night");
        var createResponse = await client.PostAsJsonAsync("/api/prize-claims", createReq);
        var claim = await createResponse.Content.ReadFromJsonAsync<PrizeClaimDto>();
        var commentResponse = await client.PostAsJsonAsync($"/api/prize-claims/{claim!.Id}/comments", new AddCommentRequest("Given out Friday!"));
        var comment = await commentResponse.Content.ReadFromJsonAsync<PrizeCommentDto>();

        var deleteResponse = await client.DeleteAsync($"/api/prize-claims/{claim.Id}/comments/{comment!.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var getResponse = await client.GetAsync($"/api/prize-claims?weekSummaryId={summaryId}");
        var claims = await getResponse.Content.ReadFromJsonAsync<List<PrizeClaimDto>>();
        claims!.Single().Comments.Should().BeEmpty();
    }

    [Fact]
    public async Task AllWriteEndpoints_Return401_WhenUnauthenticated()
    {
        var client = factory.CreateClient();
        var id = Guid.NewGuid();

        (await client.PostAsJsonAsync("/api/prize-claims", new { })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.PutAsJsonAsync($"/api/prize-claims/{id}/used", new { })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.PostAsJsonAsync($"/api/prize-claims/{id}/comments", new { })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.DeleteAsync($"/api/prize-claims/{id}/comments/{id}")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CoParent_CanMarkUsedAndComment()
    {
        var adminClient = await TestAuthHelper.RegisterWithFamily(factory);
        var inviteResponse = await adminClient.PostAsJsonAsync("/api/invites", new CreateInviteRequest("coparent@test.com"));
        var invite = await inviteResponse.Content.ReadFromJsonAsync<InviteResponse>();
        var coParentClient = await TestAuthHelper.RegisterOnly(factory);
        await coParentClient.PostAsync($"/api/invites/{invite!.Token}/accept", null);

        var summaryId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var createReq = new CreatePrizeClaimRequest("weekly", summaryId, null, 1, childId, "Alice", "🍕", "Pizza night");

        var createResponse = await coParentClient.PostAsJsonAsync("/api/prize-claims", createReq);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var claim = await createResponse.Content.ReadFromJsonAsync<PrizeClaimDto>();

        var usedResponse = await coParentClient.PutAsJsonAsync($"/api/prize-claims/{claim!.Id}/used", new UpdateUsedRequest(true));
        usedResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var commentResponse = await coParentClient.PostAsJsonAsync($"/api/prize-claims/{claim.Id}/comments", new AddCommentRequest("Co-parent note"));
        commentResponse.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task OtherFamily_CannotAccessClaims()
    {
        var client1 = await TestAuthHelper.RegisterWithFamily(factory, "Family 1");
        var client2 = await TestAuthHelper.RegisterWithFamily(factory, "Family 2");

        var summaryId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var req = new CreatePrizeClaimRequest("weekly", summaryId, null, 1, childId, "Alice", "🍕", "Pizza night");
        var createResponse = await client1.PostAsJsonAsync("/api/prize-claims", req);
        var claim = await createResponse.Content.ReadFromJsonAsync<PrizeClaimDto>();

        var response = await client2.PutAsJsonAsync($"/api/prize-claims/{claim!.Id}/used", new UpdateUsedRequest(true));
        response.StatusCode.Should().Be(HttpStatusCode.Forbid() is var _ ? HttpStatusCode.Forbidden : HttpStatusCode.Forbidden);
    }
}
```

- [ ] **Step 2: Run tests to confirm they fail**

```bash
cd backend
dotnet test --filter "FullyQualifiedName~PrizeClaimControllerTests"
```

Expected: compilation error or "controller not found" failures — confirms tests are wired and failing.

- [ ] **Step 3: Create `PrizeClaimController.cs`**

```csharp
// backend/TinyHeroes.Api/Controllers/PrizeClaimController.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TinyHeroes.Application.DTOs.Prize;
using TinyHeroes.Domain.Entities;
using TinyHeroes.Infrastructure.Data;

namespace TinyHeroes.Api.Controllers;

[ApiController]
[Route("api/prize-claims")]
[Authorize]
public class PrizeClaimController(AppDbContext db) : ControllerBase
{
    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

    private static PrizeClaimDto ToDto(PrizeClaim c) => new(
        c.Id, c.Scope, c.WeekSummaryId, c.MonthSummaryId, c.Rank,
        c.ChildId, c.ChildName, c.PrizeEmoji, c.PrizeLabel,
        c.IsUsed, c.UsedAt, c.CreatedAt,
        c.Comments.OrderBy(x => x.CreatedAt).Select(x => new PrizeCommentDto(x.Id, x.Text, x.CreatedAt)).ToList()
    );

    [HttpGet]
    public async Task<ActionResult<List<PrizeClaimDto>>> List([FromQuery] Guid? weekSummaryId, [FromQuery] Guid? monthSummaryId)
    {
        var userId = GetUserId();
        var membership = await db.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
        if (membership is null) return BadRequest("User does not belong to a family.");

        var query = db.PrizeClaims
            .Include(c => c.Comments)
            .Where(c => c.FamilyId == membership.FamilyId);

        if (weekSummaryId.HasValue)
            query = query.Where(c => c.WeekSummaryId == weekSummaryId);
        else if (monthSummaryId.HasValue)
            query = query.Where(c => c.MonthSummaryId == monthSummaryId);
        else
            return BadRequest("Provide weekSummaryId or monthSummaryId.");

        var claims = await query.ToListAsync();
        return Ok(claims.Select(ToDto).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<PrizeClaimDto>> Create(CreatePrizeClaimRequest req)
    {
        var userId = GetUserId();
        var membership = await db.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
        if (membership is null) return BadRequest("User does not belong to a family.");

        var existing = await db.PrizeClaims
            .Include(c => c.Comments)
            .FirstOrDefaultAsync(c =>
                c.FamilyId == membership.FamilyId &&
                c.Scope == req.Scope &&
                c.WeekSummaryId == req.WeekSummaryId &&
                c.MonthSummaryId == req.MonthSummaryId &&
                c.Rank == req.Rank);

        if (existing is not null)
            return Ok(ToDto(existing));

        var claim = new PrizeClaim
        {
            Id = Guid.NewGuid(),
            FamilyId = membership.FamilyId,
            Scope = req.Scope,
            WeekSummaryId = req.WeekSummaryId,
            MonthSummaryId = req.MonthSummaryId,
            Rank = req.Rank,
            ChildId = req.ChildId,
            ChildName = req.ChildName,
            PrizeEmoji = req.PrizeEmoji,
            PrizeLabel = req.PrizeLabel,
            CreatedAt = DateTime.UtcNow
        };
        db.PrizeClaims.Add(claim);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(List), new { weekSummaryId = claim.WeekSummaryId }, ToDto(claim));
    }

    [HttpPut("{id}/used")]
    public async Task<ActionResult<PrizeClaimDto>> SetUsed(Guid id, UpdateUsedRequest req)
    {
        var userId = GetUserId();
        var membership = await db.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
        if (membership is null) return BadRequest("User does not belong to a family.");

        var claim = await db.PrizeClaims.Include(c => c.Comments).FirstOrDefaultAsync(c => c.Id == id);
        if (claim is null) return NotFound();
        if (claim.FamilyId != membership.FamilyId) return Forbid();

        claim.IsUsed = req.IsUsed;
        claim.UsedAt = req.IsUsed ? DateTime.UtcNow : null;
        await db.SaveChangesAsync();

        return Ok(ToDto(claim));
    }

    [HttpPost("{id}/comments")]
    public async Task<ActionResult<PrizeCommentDto>> AddComment(Guid id, AddCommentRequest req)
    {
        var userId = GetUserId();
        var membership = await db.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
        if (membership is null) return BadRequest("User does not belong to a family.");

        var claim = await db.PrizeClaims.FirstOrDefaultAsync(c => c.Id == id);
        if (claim is null) return NotFound();
        if (claim.FamilyId != membership.FamilyId) return Forbid();

        var comment = new PrizeComment
        {
            Id = Guid.NewGuid(),
            PrizeClaimId = id,
            Text = req.Text,
            CreatedAt = DateTime.UtcNow
        };
        db.PrizeComments.Add(comment);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(List), new { }, new PrizeCommentDto(comment.Id, comment.Text, comment.CreatedAt));
    }

    [HttpDelete("{id}/comments/{commentId}")]
    public async Task<IActionResult> DeleteComment(Guid id, Guid commentId)
    {
        var userId = GetUserId();
        var membership = await db.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
        if (membership is null) return BadRequest("User does not belong to a family.");

        var claim = await db.PrizeClaims.FirstOrDefaultAsync(c => c.Id == id);
        if (claim is null) return NotFound();
        if (claim.FamilyId != membership.FamilyId) return Forbid();

        var comment = await db.PrizeComments.FirstOrDefaultAsync(c => c.Id == commentId && c.PrizeClaimId == id);
        if (comment is null) return NotFound();

        db.PrizeComments.Remove(comment);
        await db.SaveChangesAsync();

        return NoContent();
    }
}
```

- [ ] **Step 4: Run tests — confirm they pass**

```bash
cd backend
dotnet test --filter "FullyQualifiedName~PrizeClaimControllerTests"
```

Expected: all tests pass. If `OtherFamily_CannotAccessClaims` has a compile issue with the status assertion, simplify it:
```csharp
response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
```

- [ ] **Step 5: Run full test suite**

```bash
cd backend
dotnet test
```

Expected: all existing tests still pass.

- [ ] **Step 6: Commit**

```bash
cd /Volumes/PersonalProtected/GIT/TinyHeroes
git add backend/TinyHeroes.Tests/Integration/PrizeClaimControllerTests.cs
git add backend/TinyHeroes.Api/Controllers/PrizeClaimController.cs
git commit -m "feat: add PrizeClaimController with integration tests"
```

---

## Task 5: Frontend model and service

**Files:**
- Create: `frontend/src/app/core/models/prize-claim.model.ts`
- Create: `frontend/src/app/core/services/prize-claim.service.ts`

- [ ] **Step 1: Create `prize-claim.model.ts`**

```typescript
// frontend/src/app/core/models/prize-claim.model.ts
export interface PrizeCommentDto {
  id: string;
  text: string;
  createdAt: string;
}

export interface PrizeClaimDto {
  id: string;
  scope: string;
  weekSummaryId: string | null;
  monthSummaryId: string | null;
  rank: number | null;
  childId: string;
  childName: string;
  prizeEmoji: string;
  prizeLabel: string;
  isUsed: boolean;
  usedAt: string | null;
  createdAt: string;
  comments: PrizeCommentDto[];
}

export interface CreatePrizeClaimRequest {
  scope: string;
  weekSummaryId: string | null;
  monthSummaryId: string | null;
  rank: number | null;
  childId: string;
  childName: string;
  prizeEmoji: string;
  prizeLabel: string;
}
```

- [ ] **Step 2: Create `prize-claim.service.ts`**

```typescript
// frontend/src/app/core/services/prize-claim.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PrizeClaimDto, PrizeCommentDto, CreatePrizeClaimRequest } from '../models/prize-claim.model';

@Injectable({ providedIn: 'root' })
export class PrizeClaimService {
  private base = `${environment.apiUrl}/prize-claims`;

  constructor(private http: HttpClient) {}

  getByWeekSummary(weekSummaryId: string): Observable<PrizeClaimDto[]> {
    return this.http.get<PrizeClaimDto[]>(`${this.base}?weekSummaryId=${weekSummaryId}`);
  }

  getByMonthSummary(monthSummaryId: string): Observable<PrizeClaimDto[]> {
    return this.http.get<PrizeClaimDto[]>(`${this.base}?monthSummaryId=${monthSummaryId}`);
  }

  createClaim(req: CreatePrizeClaimRequest): Observable<PrizeClaimDto> {
    return this.http.post<PrizeClaimDto>(this.base, req);
  }

  setUsed(claimId: string, isUsed: boolean): Observable<PrizeClaimDto> {
    return this.http.put<PrizeClaimDto>(`${this.base}/${claimId}/used`, { isUsed });
  }

  addComment(claimId: string, text: string): Observable<PrizeCommentDto> {
    return this.http.post<PrizeCommentDto>(`${this.base}/${claimId}/comments`, { text });
  }

  deleteComment(claimId: string, commentId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${claimId}/comments/${commentId}`);
  }
}
```

- [ ] **Step 3: Commit**

```bash
cd /Volumes/PersonalProtected/GIT/TinyHeroes
git add frontend/src/app/core/models/prize-claim.model.ts
git add frontend/src/app/core/services/prize-claim.service.ts
git commit -m "feat: add PrizeClaimDto model and PrizeClaimService"
```

---

## Task 6: i18n keys

**Files:**
- Modify: `frontend/public/assets/i18n/en.json`

- [ ] **Step 1: Add PRIZE_CLAIM keys to `en.json`**

Find the `"HISTORY"` section and add a `"PRIZE_CLAIM"` sibling object after it:

```json
"PRIZE_CLAIM": {
  "SECTION_TITLE": "🎁 Prizes",
  "MARK_USED": "Mark used",
  "USED": "Used ✓",
  "PENDING": "Pending",
  "COMMENTS": "Comments",
  "ADD_COMMENT_PLACEHOLDER": "Add a comment…",
  "POST": "Post",
  "DELETE_COMMENT": "Delete"
}
```

- [ ] **Step 2: Commit**

```bash
cd /Volumes/PersonalProtected/GIT/TinyHeroes
git add frontend/public/assets/i18n/en.json
git commit -m "feat: add PRIZE_CLAIM i18n keys"
```

---

## Task 7: HistoryComponent — prizes section

**Files:**
- Modify: `frontend/src/app/features/podium/pages/history.component.ts`

This is the largest change. The component gains:
1. A `claimsMap` signal: `Map<string, PrizeClaimDto[]>` keyed by summaryId
2. An `expandedClaimKey` signal for inline expansion state
3. A `pendingClaims` map for locally-created claims not yet persisted (lazy creation)
4. Helper methods for lazy claim creation, toggle used, add/delete comment

- [ ] **Step 1: Replace `history.component.ts` with the updated version**

```typescript
// frontend/src/app/features/podium/pages/history.component.ts
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { SummaryService } from '../../../core/services/summary.service';
import { ChildService } from '../../../core/services/child.service';
import { PrizeService } from '../../../core/services/prize.service';
import { PrizeClaimService } from '../../../core/services/prize-claim.service';
import { PrizeClaimDto } from '../../../core/models/prize-claim.model';
import { WeekSummaryDto, MonthSummaryDto, RankingEntry } from '../../../core/models/summary.model';
import { PrizeAssignmentDto } from '../../../core/models/prize.model';

@Component({
  selector: 'app-history',
  imports: [TranslateModule, FormsModule],
  template: `
    <div class="max-w-lg mx-auto p-4">
      <!-- Toggle -->
      <div class="flex bg-gray-100 rounded-lg p-1 mb-4">
        <button (click)="activeTab.set('weekly')"
          [class]="activeTab() === 'weekly' ? 'bg-brand-orange text-white shadow-sm' : 'text-brand-muted'"
          class="flex-1 py-2 px-4 rounded-md text-sm font-medium transition-colors">
          {{ 'HISTORY.TAB_WEEKLY' | translate }}
        </button>
        <button (click)="activeTab.set('monthly')"
          [class]="activeTab() === 'monthly' ? 'bg-brand-orange text-white shadow-sm' : 'text-brand-muted'"
          class="flex-1 py-2 px-4 rounded-md text-sm font-medium transition-colors">
          {{ 'HISTORY.TAB_MONTHLY' | translate }}
        </button>
      </div>

      @if (activeTab() === 'weekly') {
        @if (weeks().length === 0) {
          <div class="bg-white rounded-xl border border-brand-border p-8 text-center">
            <p class="text-4xl mb-3">📅</p>
            <p class="text-brand-text font-bold mb-2">{{ 'HISTORY.EMPTY_TITLE' | translate }}</p>
            <p class="text-sm text-brand-muted">{{ 'HISTORY.EMPTY_SUBTITLE' | translate }}</p>
          </div>
        } @else {
          <div class="space-y-3">
            @for (week of weeks(); track week.id) {
              <div class="bg-white rounded-xl border border-brand-border p-4">
                <p class="text-xs text-brand-muted font-medium mb-3">
                  {{ formatWeekDate(week.weekStart) }} – {{ formatWeekDate(week.weekEnd) }}
                </p>
                @if (weekHasDeeds(week.rankings)) {
                  <div class="space-y-2">
                    @for (entry of topPodium(week.rankings); track entry.childId) {
                      <div class="mb-2">
                        <div class="flex items-center gap-2">
                          <span class="text-base w-5">{{ entry.rank === 1 ? '🥇' : entry.rank === 2 ? '🥈' : '🥉' }}</span>
                          <div class="w-7 h-7 rounded-full bg-brand-cream flex items-center justify-center text-sm">
                            {{ getAvatar(entry.childId) }}
                          </div>
                          <span class="flex-1 text-sm text-brand-text">{{ entry.childName }}</span>
                          <span class="text-sm font-bold text-brand-orange">{{ entry.deedCount }}</span>
                          <span class="text-xs text-brand-muted">{{ 'HISTORY.DEEDS' | translate }}</span>
                        </div>
                        @let weekPrize = getWeeklyPrize(entry.rank);
                        @if (weekPrize) {
                          <div class="flex items-center gap-1.5 ml-7 mt-1">
                            <span class="text-xs">🎁</span>
                            <span class="text-xs text-brand-muted">{{ 'HISTORY.PRIZE_WON' | translate }}</span>
                            <span class="text-xs font-semibold text-brand-text">{{ weekPrize }}</span>
                          </div>
                        }
                      </div>
                    }
                  </div>

                  <!-- Prizes section -->
                  @let weekClaims = getClaimsForSummary(week.id);
                  @let weekPrizes = getWeekPrizeEntries(week);
                  @if (weekPrizes.length > 0) {
                    <div class="mt-3 pt-3 border-t border-brand-border">
                      <p class="text-xs font-semibold text-brand-text mb-2">{{ 'PRIZE_CLAIM.SECTION_TITLE' | translate }}</p>
                      <div class="space-y-2">
                        @for (entry of weekPrizes; track entry.rank) {
                          @let claim = findClaim(weekClaims, entry.rank);
                          <div class="rounded-xl border overflow-hidden"
                            [class]="claim?.isUsed ? 'border-green-200 bg-green-50' : 'border-brand-border bg-white'">
                            <!-- Card header -->
                            <div class="flex items-center gap-2 p-3 cursor-pointer"
                              (click)="toggleExpand(week.id + '-' + entry.rank)">
                              <span class="text-xl">{{ entry.assignment.emoji }}</span>
                              <div class="flex-1 min-w-0">
                                <p class="text-sm font-semibold text-brand-text truncate">{{ entry.assignment.label }}</p>
                                <p class="text-xs text-brand-muted">{{ rankEmoji(entry.rank) }} {{ entry.childName }}</p>
                              </div>
                              @if (claim?.isUsed) {
                                <span class="text-xs font-semibold text-green-600 shrink-0">{{ 'PRIZE_CLAIM.USED' | translate }}</span>
                              } @else {
                                <button
                                  class="text-xs px-3 py-1 rounded-full border border-brand-border bg-white text-brand-text shrink-0"
                                  (click)="$event.stopPropagation(); quickMarkUsed(week, entry)"
                                  [disabled]="saving()">
                                  {{ 'PRIZE_CLAIM.MARK_USED' | translate }}
                                </button>
                              }
                              <span class="text-brand-muted text-xs">{{ isExpanded(week.id + '-' + entry.rank) ? '▲' : '▼' }}</span>
                            </div>
                            <!-- Expanded section -->
                            @if (isExpanded(week.id + '-' + entry.rank)) {
                              <div class="border-t border-brand-border p-3 bg-white">
                                <!-- Used toggle -->
                                <label class="flex items-center gap-2 mb-3 cursor-pointer">
                                  <input type="checkbox"
                                    [checked]="claim?.isUsed ?? false"
                                    (change)="toggleUsed(week, entry, $event)"
                                    [disabled]="saving()"
                                    class="accent-green-500 w-4 h-4">
                                  <span class="text-sm font-medium"
                                    [class]="claim?.isUsed ? 'text-green-600' : 'text-brand-muted'">
                                    {{ (claim?.isUsed ? 'PRIZE_CLAIM.USED' : 'PRIZE_CLAIM.PENDING') | translate }}
                                  </span>
                                </label>
                                <!-- Comments -->
                                <p class="text-xs text-brand-muted uppercase mb-2">{{ 'PRIZE_CLAIM.COMMENTS' | translate }}</p>
                                @if (claim && claim.comments.length > 0) {
                                  <div class="space-y-1 mb-2">
                                    @for (c of claim.comments; track c.id) {
                                      <div class="flex items-start gap-2 bg-gray-50 rounded-lg px-3 py-2">
                                        <span class="flex-1 text-sm text-brand-text">{{ c.text }}</span>
                                        <span class="text-xs text-brand-muted shrink-0">{{ formatDate(c.createdAt) }}</span>
                                        <button class="text-xs text-red-400 shrink-0"
                                          (click)="deleteComment(week, entry, c.id)"
                                          [disabled]="saving()">✕</button>
                                      </div>
                                    }
                                  </div>
                                }
                                <!-- Add comment -->
                                <div class="flex gap-2">
                                  <input
                                    type="text"
                                    [(ngModel)]="commentDraft[week.id + '-' + entry.rank]"
                                    placeholder="{{ 'PRIZE_CLAIM.ADD_COMMENT_PLACEHOLDER' | translate }}"
                                    class="flex-1 text-sm border border-brand-border rounded-lg px-3 py-2 outline-none focus:border-brand-orange"
                                    (keydown.enter)="postComment(week, entry)">
                                  <button
                                    class="text-sm px-3 py-2 bg-brand-orange text-white rounded-lg font-medium"
                                    (click)="postComment(week, entry)"
                                    [disabled]="saving() || !commentDraft[week.id + '-' + entry.rank]?.trim()">
                                    {{ 'PRIZE_CLAIM.POST' | translate }}
                                  </button>
                                </div>
                              </div>
                            }
                          </div>
                        }
                      </div>
                    </div>
                  }
                } @else {
                  <p class="text-sm text-brand-muted">{{ 'HISTORY.NO_DEEDS_THIS_WEEK' | translate }}</p>
                }
              </div>
            }
          </div>
        }
      } @else {
        @if (months().length === 0) {
          <div class="bg-white rounded-xl border border-brand-border p-8 text-center">
            <p class="text-4xl mb-3">📅</p>
            <p class="text-brand-text font-bold mb-2">{{ 'HISTORY.EMPTY_TITLE' | translate }}</p>
            <p class="text-sm text-brand-muted">{{ 'HISTORY.EMPTY_SUBTITLE' | translate }}</p>
          </div>
        } @else {
          <div class="space-y-3">
            @for (month of months(); track month.id) {
              <div class="bg-white rounded-xl border border-brand-border p-4">
                <p class="text-xs text-brand-muted font-medium mb-3">
                  {{ formatMonth(month.year, month.month) }}
                </p>
                @if (month.championName) {
                  <div class="flex items-center gap-3">
                    <div class="w-10 h-10 rounded-full bg-brand-cream border-2 border-yellow-400 flex items-center justify-center text-xl">
                      {{ getAvatar(month.championChildId!) }}
                    </div>
                    <div class="flex-1">
                      <p class="text-sm font-bold text-brand-text">{{ month.championName }}</p>
                      <p class="text-xs text-brand-muted">{{ 'HISTORY.CHAMPION' | translate }}</p>
                    </div>
                    <div class="text-right">
                      <p class="text-sm font-bold text-brand-orange">{{ month.totalDeeds }}</p>
                      <p class="text-xs text-brand-muted">{{ 'HISTORY.DEEDS' | translate }}</p>
                    </div>
                  </div>
                  @let mp = monthlyPrize();
                  @if (mp) {
                    <div class="flex items-center gap-1.5 mt-2 pt-2 border-t border-brand-border">
                      <span class="text-sm">🏅</span>
                      <span class="text-xs text-brand-muted">{{ 'HISTORY.PRIZE_WON' | translate }}</span>
                      <span class="text-xs font-semibold text-brand-text">{{ mp.emoji }} {{ mp.label }}</span>
                    </div>

                    <!-- Monthly prize claim -->
                    @let monthClaims = getClaimsForSummary(month.id);
                    @let monthClaim = findClaim(monthClaims, null);
                    <div class="mt-3 pt-3 border-t border-brand-border">
                      <p class="text-xs font-semibold text-brand-text mb-2">{{ 'PRIZE_CLAIM.SECTION_TITLE' | translate }}</p>
                      <div class="rounded-xl border overflow-hidden"
                        [class]="monthClaim?.isUsed ? 'border-green-200 bg-green-50' : 'border-brand-border bg-white'">
                        <div class="flex items-center gap-2 p-3 cursor-pointer"
                          (click)="toggleExpand(month.id + '-monthly')">
                          <span class="text-xl">{{ mp.emoji }}</span>
                          <div class="flex-1 min-w-0">
                            <p class="text-sm font-semibold text-brand-text truncate">{{ mp.label }}</p>
                            <p class="text-xs text-brand-muted">🏅 {{ month.championName }}</p>
                          </div>
                          @if (monthClaim?.isUsed) {
                            <span class="text-xs font-semibold text-green-600 shrink-0">{{ 'PRIZE_CLAIM.USED' | translate }}</span>
                          } @else {
                            <button
                              class="text-xs px-3 py-1 rounded-full border border-brand-border bg-white text-brand-text shrink-0"
                              (click)="$event.stopPropagation(); quickMarkUsedMonthly(month, mp)"
                              [disabled]="saving()">
                              {{ 'PRIZE_CLAIM.MARK_USED' | translate }}
                            </button>
                          }
                          <span class="text-brand-muted text-xs">{{ isExpanded(month.id + '-monthly') ? '▲' : '▼' }}</span>
                        </div>
                        @if (isExpanded(month.id + '-monthly')) {
                          <div class="border-t border-brand-border p-3 bg-white">
                            <label class="flex items-center gap-2 mb-3 cursor-pointer">
                              <input type="checkbox"
                                [checked]="monthClaim?.isUsed ?? false"
                                (change)="toggleUsedMonthly(month, mp, $event)"
                                [disabled]="saving()"
                                class="accent-green-500 w-4 h-4">
                              <span class="text-sm font-medium"
                                [class]="monthClaim?.isUsed ? 'text-green-600' : 'text-brand-muted'">
                                {{ (monthClaim?.isUsed ? 'PRIZE_CLAIM.USED' : 'PRIZE_CLAIM.PENDING') | translate }}
                              </span>
                            </label>
                            <p class="text-xs text-brand-muted uppercase mb-2">{{ 'PRIZE_CLAIM.COMMENTS' | translate }}</p>
                            @if (monthClaim && monthClaim.comments.length > 0) {
                              <div class="space-y-1 mb-2">
                                @for (c of monthClaim.comments; track c.id) {
                                  <div class="flex items-start gap-2 bg-gray-50 rounded-lg px-3 py-2">
                                    <span class="flex-1 text-sm text-brand-text">{{ c.text }}</span>
                                    <span class="text-xs text-brand-muted shrink-0">{{ formatDate(c.createdAt) }}</span>
                                    <button class="text-xs text-red-400 shrink-0"
                                      (click)="deleteCommentMonthly(month, mp, c.id)"
                                      [disabled]="saving()">✕</button>
                                  </div>
                                }
                              </div>
                            }
                            <div class="flex gap-2">
                              <input
                                type="text"
                                [(ngModel)]="commentDraft[month.id + '-monthly']"
                                placeholder="{{ 'PRIZE_CLAIM.ADD_COMMENT_PLACEHOLDER' | translate }}"
                                class="flex-1 text-sm border border-brand-border rounded-lg px-3 py-2 outline-none focus:border-brand-orange"
                                (keydown.enter)="postCommentMonthly(month, mp)">
                              <button
                                class="text-sm px-3 py-2 bg-brand-orange text-white rounded-lg font-medium"
                                (click)="postCommentMonthly(month, mp)"
                                [disabled]="saving() || !commentDraft[month.id + '-monthly']?.trim()">
                                {{ 'PRIZE_CLAIM.POST' | translate }}
                              </button>
                            </div>
                          </div>
                        }
                      </div>
                    </div>
                  }
                } @else {
                  <p class="text-sm text-brand-muted">{{ 'HISTORY.NO_CHAMPION_THIS_MONTH' | translate }}</p>
                }
              </div>
            }
          </div>
        }
      }
    </div>
  `
})
export class HistoryComponent implements OnInit {
  private summaryService = inject(SummaryService);
  private childService = inject(ChildService);
  private prizeService = inject(PrizeService);
  private prizeClaimService = inject(PrizeClaimService);
  private ts = inject(TranslateService);

  activeTab = signal<'weekly' | 'monthly'>('weekly');
  weeks = this.summaryService.weeks;
  months = this.summaryService.months;
  saving = signal(false);

  private claimsMap = signal<Map<string, PrizeClaimDto[]>>(new Map());
  private expandedKey = signal<string | null>(null);
  commentDraft: Record<string, string> = {};

  monthlyPrize = computed(() =>
    this.prizeService.assignments().find(a => a.scope === 'monthly' && a.rank === null) ?? null
  );

  ngOnInit() {
    this.summaryService.loadWeeks();
    this.summaryService.loadMonths();
    if (this.childService.children().length === 0) this.childService.loadChildren();
    if (this.prizeService.assignments().length === 0) this.prizeService.loadAssignments();
  }

  // ── Expansion ────────────────────────────────────────────────
  toggleExpand(key: string) {
    this.expandedKey.set(this.expandedKey() === key ? null : key);
    if (this.expandedKey() === key) this.loadClaimsIfNeeded(key);
  }

  isExpanded(key: string) { return this.expandedKey() === key; }

  private summaryIdFromKey(key: string): string { return key.split('-')[0]; }

  private loadClaimsIfNeeded(key: string) {
    const summaryId = this.summaryIdFromKey(key);
    if (this.claimsMap().has(summaryId)) return;
    const isMonthly = key.endsWith('-monthly');
    const obs = isMonthly
      ? this.prizeClaimService.getByMonthSummary(summaryId)
      : this.prizeClaimService.getByWeekSummary(summaryId);
    obs.pipe(catchError(() => of([]))).subscribe(claims => {
      const m = new Map(this.claimsMap());
      m.set(summaryId, claims);
      this.claimsMap.set(m);
    });
  }

  // ── Data helpers ─────────────────────────────────────────────
  getClaimsForSummary(summaryId: string): PrizeClaimDto[] {
    return this.claimsMap().get(summaryId) ?? [];
  }

  findClaim(claims: PrizeClaimDto[], rank: number | null): PrizeClaimDto | undefined {
    return claims.find(c => c.rank === rank);
  }

  getWeekPrizeEntries(week: WeekSummaryDto): { rank: number; assignment: PrizeAssignmentDto; childName: string }[] {
    return this.topPodium(week.rankings)
      .map(entry => {
        const assignment = this.prizeService.assignments().find(a => a.scope === 'weekly' && a.rank === entry.rank);
        if (!assignment) return null;
        return { rank: entry.rank, assignment, childName: entry.childName };
      })
      .filter((x): x is { rank: number; assignment: PrizeAssignmentDto; childName: string } => x !== null);
  }

  rankEmoji(rank: number): string {
    return rank === 1 ? '🥇' : rank === 2 ? '🥈' : '🥉';
  }

  // ── Lazy claim creation ──────────────────────────────────────
  private getOrCreateClaim(
    summaryId: string,
    scope: string,
    weekSummaryId: string | null,
    monthSummaryId: string | null,
    rank: number | null,
    childId: string,
    childName: string,
    prizeEmoji: string,
    prizeLabel: string
  ): Promise<PrizeClaimDto> {
    return new Promise((resolve, reject) => {
      const existing = this.findClaim(this.getClaimsForSummary(summaryId), rank);
      if (existing) { resolve(existing); return; }
      this.prizeClaimService.createClaim({ scope, weekSummaryId, monthSummaryId, rank, childId, childName, prizeEmoji, prizeLabel })
        .subscribe({
          next: claim => {
            const m = new Map(this.claimsMap());
            const existing = m.get(summaryId) ?? [];
            m.set(summaryId, [...existing, claim]);
            this.claimsMap.set(m);
            resolve(claim);
          },
          error: reject
        });
    });
  }

  private updateClaimInMap(summaryId: string, updated: PrizeClaimDto) {
    const m = new Map(this.claimsMap());
    const list = (m.get(summaryId) ?? []).map(c => c.id === updated.id ? updated : c);
    m.set(summaryId, list);
    this.claimsMap.set(m);
  }

  // ── Weekly interactions ──────────────────────────────────────
  async quickMarkUsed(week: WeekSummaryDto, entry: { rank: number; assignment: PrizeAssignmentDto; childName: string }) {
    const podiumEntry = week.rankings.find(r => r.rank === entry.rank);
    if (!podiumEntry) return;
    this.saving.set(true);
    try {
      const claim = await this.getOrCreateClaim(week.id, 'weekly', week.id, null, entry.rank,
        podiumEntry.childId, podiumEntry.childName, entry.assignment.emoji, entry.assignment.label);
      this.prizeClaimService.setUsed(claim.id, true).subscribe(updated => this.updateClaimInMap(week.id, updated));
    } finally { this.saving.set(false); }
  }

  async toggleUsed(week: WeekSummaryDto, entry: { rank: number; assignment: PrizeAssignmentDto; childName: string }, event: Event) {
    const checked = (event.target as HTMLInputElement).checked;
    const podiumEntry = week.rankings.find(r => r.rank === entry.rank);
    if (!podiumEntry) return;
    this.saving.set(true);
    try {
      const claim = await this.getOrCreateClaim(week.id, 'weekly', week.id, null, entry.rank,
        podiumEntry.childId, podiumEntry.childName, entry.assignment.emoji, entry.assignment.label);
      this.prizeClaimService.setUsed(claim.id, checked).subscribe(updated => this.updateClaimInMap(week.id, updated));
    } finally { this.saving.set(false); }
  }

  async postComment(week: WeekSummaryDto, entry: { rank: number; assignment: PrizeAssignmentDto; childName: string }) {
    const key = week.id + '-' + entry.rank;
    const text = this.commentDraft[key]?.trim();
    if (!text) return;
    const podiumEntry = week.rankings.find(r => r.rank === entry.rank);
    if (!podiumEntry) return;
    this.saving.set(true);
    try {
      const claim = await this.getOrCreateClaim(week.id, 'weekly', week.id, null, entry.rank,
        podiumEntry.childId, podiumEntry.childName, entry.assignment.emoji, entry.assignment.label);
      this.prizeClaimService.addComment(claim.id, text).subscribe(comment => {
        this.updateClaimInMap(week.id, { ...claim, comments: [...claim.comments, comment] });
        this.commentDraft[key] = '';
      });
    } finally { this.saving.set(false); }
  }

  async deleteComment(week: WeekSummaryDto, entry: { rank: number; assignment: PrizeAssignmentDto; childName: string }, commentId: string) {
    const claim = this.findClaim(this.getClaimsForSummary(week.id), entry.rank);
    if (!claim) return;
    this.saving.set(true);
    this.prizeClaimService.deleteComment(claim.id, commentId).subscribe(() => {
      this.updateClaimInMap(week.id, { ...claim, comments: claim.comments.filter(c => c.id !== commentId) });
      this.saving.set(false);
    });
  }

  // ── Monthly interactions ─────────────────────────────────────
  async quickMarkUsedMonthly(month: MonthSummaryDto, prize: PrizeAssignmentDto) {
    if (!month.championChildId || !month.championName) return;
    this.saving.set(true);
    try {
      const claim = await this.getOrCreateClaim(month.id, 'monthly', null, month.id, null,
        month.championChildId, month.championName, prize.emoji, prize.label);
      this.prizeClaimService.setUsed(claim.id, true).subscribe(updated => this.updateClaimInMap(month.id, updated));
    } finally { this.saving.set(false); }
  }

  async toggleUsedMonthly(month: MonthSummaryDto, prize: PrizeAssignmentDto, event: Event) {
    const checked = (event.target as HTMLInputElement).checked;
    if (!month.championChildId || !month.championName) return;
    this.saving.set(true);
    try {
      const claim = await this.getOrCreateClaim(month.id, 'monthly', null, month.id, null,
        month.championChildId, month.championName, prize.emoji, prize.label);
      this.prizeClaimService.setUsed(claim.id, checked).subscribe(updated => this.updateClaimInMap(month.id, updated));
    } finally { this.saving.set(false); }
  }

  async postCommentMonthly(month: MonthSummaryDto, prize: PrizeAssignmentDto) {
    const key = month.id + '-monthly';
    const text = this.commentDraft[key]?.trim();
    if (!text || !month.championChildId || !month.championName) return;
    this.saving.set(true);
    try {
      const claim = await this.getOrCreateClaim(month.id, 'monthly', null, month.id, null,
        month.championChildId, month.championName, prize.emoji, prize.label);
      this.prizeClaimService.addComment(claim.id, text).subscribe(comment => {
        this.updateClaimInMap(month.id, { ...claim, comments: [...claim.comments, comment] });
        this.commentDraft[key] = '';
      });
    } finally { this.saving.set(false); }
  }

  async deleteCommentMonthly(month: MonthSummaryDto, prize: PrizeAssignmentDto, commentId: string) {
    const claim = this.findClaim(this.getClaimsForSummary(month.id), null);
    if (!claim) return;
    this.saving.set(true);
    this.prizeClaimService.deleteComment(claim.id, commentId).subscribe(() => {
      this.updateClaimInMap(month.id, { ...claim, comments: claim.comments.filter(c => c.id !== commentId) });
      this.saving.set(false);
    });
  }

  // ── Existing helpers (unchanged) ─────────────────────────────
  formatWeekDate(dateStr: string): string {
    const d = new Date(dateStr);
    const lang = this.ts.currentLang || 'en';
    return d.toLocaleDateString(lang, { month: 'short', day: 'numeric' });
  }

  formatMonth(year: number, month: number): string {
    const lang = this.ts.currentLang || 'en';
    return new Date(year, month - 1).toLocaleDateString(lang, { month: 'long', year: 'numeric' });
  }

  formatDate(dateStr: string): string {
    const lang = this.ts.currentLang || 'en';
    return new Date(dateStr).toLocaleDateString(lang, { month: 'short', day: 'numeric' });
  }

  weekHasDeeds(rankings: { deedCount: number }[]): boolean {
    return rankings.some(r => r.deedCount > 0);
  }

  topPodium(rankings: RankingEntry[]) {
    return rankings.filter(r => r.deedCount > 0 && r.rank <= 3);
  }

  getAvatar(childId: string): string {
    return this.childService.children().find(c => c.id === childId)?.avatarEmoji ?? '🦸';
  }

  getWeeklyPrize(rank: number): string {
    const a = this.prizeService.assignments().find(a => a.scope === 'weekly' && a.rank === rank);
    return a ? `${a.emoji} ${a.label}` : '';
  }
}
```

- [ ] **Step 2: Build check**

```bash
cd frontend
npx ng build --configuration production 2>&1 | tail -20
```

Expected: build succeeds with no errors.

- [ ] **Step 3: Commit**

```bash
cd /Volumes/PersonalProtected/GIT/TinyHeroes
git add frontend/src/app/features/podium/pages/history.component.ts
git commit -m "feat: add prize tracking section to HistoryComponent"
```

---

## Task 8: E2E tests

**Files:**
- Modify: `frontend/e2e/helpers/api-mocks.ts` — add `mockSummariesApi` and `mockPrizeClaimsApi`
- Create: `frontend/e2e/prize-tracking.spec.ts`

- [ ] **Step 1: Add mock helpers to `api-mocks.ts`**

Add these two functions before the `mockAllApi` function:

```typescript
// Add to frontend/e2e/helpers/api-mocks.ts

const WEEK_SUMMARY_ID = 'week-sum-1';
const MONTH_SUMMARY_ID = 'month-sum-1';
const CLAIM_ID = 'claim-1';
const COMMENT_ID = 'comment-1';

export async function mockSummariesApi(page: Page): Promise<void> {
  await page.route('**/api/summaries/weeks', route =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([{
        id: WEEK_SUMMARY_ID,
        weekStart: '2026-05-12T00:00:00Z',
        weekEnd: '2026-05-18T23:59:59Z',
        rankings: [
          { childId: 'child-1', childName: 'Alice', deedCount: 10, rank: 1 },
          { childId: 'child-2', childName: 'Bob', deedCount: 7, rank: 2 },
        ]
      }])
    })
  );

  await page.route('**/api/summaries/months', route =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([{
        id: MONTH_SUMMARY_ID,
        year: 2026,
        month: 4,
        championName: 'Alice',
        championChildId: 'child-1',
        totalDeeds: 42,
        rankings: [{ childId: 'child-1', childName: 'Alice', deedCount: 42, rank: 1 }]
      }])
    })
  );

  await page.route('**/api/prize-assignments', route =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        { id: 'pa-1', scope: 'weekly', rank: 1, emoji: '🍕', label: 'Pizza night' },
        { id: 'pa-2', scope: 'weekly', rank: 2, emoji: '🍦', label: 'Ice cream' },
        { id: 'pa-m', scope: 'monthly', rank: null, emoji: '🎡', label: 'Amusement park' },
      ])
    })
  );
}

export async function mockPrizeClaimsApi(page: Page, initialClaims: object[] = []): Promise<void> {
  let claims = [...initialClaims];

  await page.route('**/api/prize-claims**', async route => {
    const method = route.request().method();
    const url = route.request().url();

    if (method === 'GET') {
      return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(claims) });
    }

    if (method === 'POST' && !url.includes('/comments')) {
      const claim = {
        id: CLAIM_ID, scope: 'weekly', weekSummaryId: WEEK_SUMMARY_ID, monthSummaryId: null,
        rank: 1, childId: 'child-1', childName: 'Alice', prizeEmoji: '🍕', prizeLabel: 'Pizza night',
        isUsed: false, usedAt: null, createdAt: new Date().toISOString(), comments: []
      };
      claims.push(claim);
      return route.fulfill({ status: 201, contentType: 'application/json', body: JSON.stringify(claim) });
    }

    if (method === 'POST' && url.includes('/comments')) {
      const body = JSON.parse(route.request().postData() ?? '{}');
      const comment = { id: COMMENT_ID, text: body.text, createdAt: new Date().toISOString() };
      const idx = claims.findIndex((c: any) => c.id === CLAIM_ID);
      if (idx >= 0) (claims[idx] as any).comments.push(comment);
      return route.fulfill({ status: 201, contentType: 'application/json', body: JSON.stringify(comment) });
    }

    if (method === 'PUT' && url.includes('/used')) {
      const body = JSON.parse(route.request().postData() ?? '{}');
      const idx = claims.findIndex((c: any) => c.id === CLAIM_ID);
      if (idx >= 0) {
        (claims[idx] as any).isUsed = body.isUsed;
        (claims[idx] as any).usedAt = body.isUsed ? new Date().toISOString() : null;
      }
      return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(claims[idx] ?? {}) });
    }

    if (method === 'DELETE' && url.includes('/comments/')) {
      const idx = claims.findIndex((c: any) => c.id === CLAIM_ID);
      if (idx >= 0) (claims[idx] as any).comments = (claims[idx] as any).comments.filter((c: any) => c.id !== COMMENT_ID);
      return route.fulfill({ status: 204 });
    }

    return route.fulfill({ status: 200, contentType: 'application/json', body: '{}' });
  });
}
```

Also update `mockAllApi` to include summaries:

```typescript
export async function mockAllApi(page: Page): Promise<void> {
  await mockFamilyApi(page);
  await mockChildrenApi(page);
  await mockDeedsApi(page);
  await mockPresetsApi(page);
  await mockSummariesApi(page);
  await mockPrizeClaimsApi(page);
}
```

- [ ] **Step 2: Create `prize-tracking.spec.ts`**

```typescript
// frontend/e2e/prize-tracking.spec.ts
import { test, expect } from '@playwright/test';
import { injectAuth, mockAllApi, mockSummariesApi, mockPrizeClaimsApi } from './helpers';

test.describe('Prize Tracking', () => {
  test.beforeEach(async ({ page }) => {
    await injectAuth(page);
    await mockAllApi(page);
  });

  test('History tab shows Prizes section for a week with prize assignments', async ({ page }) => {
    await page.goto('/podium/history');
    await expect(page.getByText('🎁 Prizes')).toBeVisible();
    await expect(page.getByText('Pizza night')).toBeVisible();
  });

  test('Prizes section shows Pending badge initially', async ({ page }) => {
    await page.goto('/podium/history');
    await expect(page.getByText('Mark used').first()).toBeVisible();
  });

  test('Tapping prize card expands it inline showing toggle and comment input', async ({ page }) => {
    await page.goto('/podium/history');
    // Click the pizza card header area (emoji/label side)
    await page.locator('text=Pizza night').first().click();
    await expect(page.getByPlaceholder('Add a comment…')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Post' })).toBeVisible();
  });

  test('Mark used button marks prize as used without expanding', async ({ page }) => {
    await page.goto('/podium/history');
    const markUsedBtn = page.getByRole('button', { name: 'Mark used' }).first();
    await markUsedBtn.click();
    await expect(page.getByText('Used ✓').first()).toBeVisible();
  });

  test('Toggle inside expanded card marks prize as used', async ({ page }) => {
    await page.goto('/podium/history');
    await page.locator('text=Pizza night').first().click();
    const checkbox = page.locator('input[type="checkbox"]').first();
    await checkbox.check();
    await expect(page.getByText('Used ✓').first()).toBeVisible();
  });

  test('Unchecking toggle clears used status', async ({ page }) => {
    await page.goto('/podium/history');
    // First mark as used
    await page.getByRole('button', { name: 'Mark used' }).first().click();
    // Expand and uncheck
    await page.locator('text=Pizza night').first().click();
    const checkbox = page.locator('input[type="checkbox"]').first();
    await checkbox.uncheck();
    await expect(page.getByText('Mark used').first()).toBeVisible();
  });

  test('Posting a comment adds it to the list with a timestamp', async ({ page }) => {
    await page.goto('/podium/history');
    await page.locator('text=Pizza night').first().click();
    await page.getByPlaceholder('Add a comment…').fill('Given out Friday!');
    await page.getByRole('button', { name: 'Post' }).click();
    await expect(page.getByText('Given out Friday!')).toBeVisible();
  });

  test('Deleting a comment removes it from the list', async ({ page }) => {
    // Start with a claim that already has a comment
    await mockPrizeClaimsApi(page, [{
      id: 'claim-1', scope: 'weekly', weekSummaryId: 'week-sum-1', monthSummaryId: null,
      rank: 1, childId: 'child-1', childName: 'Alice', prizeEmoji: '🍕', prizeLabel: 'Pizza night',
      isUsed: false, usedAt: null, createdAt: new Date().toISOString(),
      comments: [{ id: 'comment-1', text: 'Existing comment', createdAt: new Date().toISOString() }]
    }]);
    await page.goto('/podium/history');
    await page.locator('text=Pizza night').first().click();
    await expect(page.getByText('Existing comment')).toBeVisible();
    await page.getByRole('button', { name: '✕' }).first().click();
    await expect(page.getByText('Existing comment')).not.toBeVisible();
  });

  test('Monthly tab shows prize section for champion month', async ({ page }) => {
    await page.goto('/podium/history');
    await page.getByRole('button', { name: 'Monthly' }).click();
    await expect(page.getByText('Amusement park')).toBeVisible();
    await expect(page.getByText('Mark used').first()).toBeVisible();
  });
});
```

- [ ] **Step 3: Run e2e tests**

```bash
cd frontend
npm run e2e -- --reporter=list 2>&1 | tail -30
```

Expected: all `prize-tracking.spec.ts` tests pass.

- [ ] **Step 4: Commit**

```bash
cd /Volumes/PersonalProtected/GIT/TinyHeroes
git add frontend/e2e/helpers/api-mocks.ts
git add frontend/e2e/prize-tracking.spec.ts
git commit -m "test: add Playwright e2e tests for prize tracking"
```

---

## Task 9: Final verification

- [ ] **Step 1: Run all backend tests**

```bash
cd backend
dotnet test
```

Expected: all tests pass.

- [ ] **Step 2: Run all e2e tests**

```bash
cd frontend
npm run e2e
```

Expected: all tests pass including `prize-tracking.spec.ts`.

- [ ] **Step 3: Start full stack and manually verify**

```bash
cd backend && dotnet run --project TinyHeroes.Api &
cd frontend && npm start
```

Navigate to `/podium/history`:
- Weekly tab: confirm "🎁 Prizes" section appears for weeks with prize assignments and deed counts > 0
- Tap prize card left side — confirm inline expansion with toggle and comment input
- Tap "Mark used" button on right — confirm green badge appears without expanding
- Expand card, check toggle — confirm "Used ✓" shows
- Uncheck toggle — confirm "Pending" / "Mark used" returns
- Type comment, press Post — confirm comment appears with date
- Click ✕ on comment — confirm it disappears
- Monthly tab: confirm champion's prize shows with same controls

- [ ] **Step 4: Final commit**

```bash
cd /Volumes/PersonalProtected/GIT/TinyHeroes
git add -A
git commit -m "feat: prize tracking complete — used toggle, comments, e2e tests"
```
