# Role Management — Admin and CoParent Permissions (Issue #14) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Centralise role checks behind a capability-based permission system, add a `PATCH /api/families/mine/members/{userId}/role` endpoint, and add a dedicated admin-only "Manage Members" screen at `settings/members`. The design must allow new roles (Grandparent, ReadOnly, Child) to be added with changes confined to two files — no hunting through controllers or templates.

**Architecture:**
- **Backend:** `FamilyCapability` enum + `RolePermissions` map in Domain; `RequireCapability` / `RequireAdmin` helpers in `ApiControllerBase`. Adding a new role = one map entry in `RolePermissions`, one enum value in `FamilyRole`.
- **Frontend:** `FamilyRole` union type + `ROLE_CAPABILITIES` map in `family.model.ts`; `MemberRolesComponent` derives role options from the map. Adding a new role = one entry in `ROLE_CAPABILITIES`, no template changes.

**Tech Stack:**
- Backend: .NET 10, ASP.NET Core, EF Core (InMemory for tests), xUnit 2.9.3, FluentAssertions 8.10
- Frontend: Angular 21.2, standalone components, signals, `ChangeDetectionStrategy.OnPush`, ngx-translate 17, Tailwind CSS 4.3

---

## Current state (as of branch creation)

- `FamilyRole` is `{ Admin, CoParent }`, stored as `integer` in Postgres (EF default — append-only additions need no migration)
- `JsonStringEnumConverter` is registered globally — role is already serialised as `"Admin"` / `"CoParent"` in every API response
- Permissions inferred by `== FamilyRole.Admin` scattered across 9 call-sites in 6 controllers
- `ApiControllerBase` contains only `GetUserId()`
- Frontend `FamilyMember.role` is typed as `string` — role comparisons (`=== 'Admin'`) are scattered across a service and two component templates
- No `PATCH /api/families/mine/members/{userId}/role` endpoint
- Frontend version: `2.4.3`

---

## Why capability-based, and what "no refactor" means

The key rule: **adding a new role must be confined to the permission table, not scattered logic.**

| Without this design | With this design |
|---|---|
| `if (role != FamilyRole.Admin)` in 9 places | `RequireCapability(db, ManageFamily)` — controllers never name a role |
| `=== 'Admin'` in service + 2 templates | `ROLE_CAPABILITIES[role].canManageFamily` — templates never name a role |
| New role = grep + edit 11 files | New role = 2 file additions (backend map + frontend map) |

The `role === 'Admin'` comparisons in templates are the remaining gap the previous plan version left open. This version eliminates them.

---

## On the `mine` route prefix

```
PATCH /api/families/mine/members/{userId}/role
```

All family-scoped mutations use `mine` — the family is resolved from the JWT, not the URL. Consistent with the existing `DELETE mine/members/{memberId}`. See existing routes in `FamilyController`:

| Method | Route |
|---|---|
| `GET` | `/api/families/mine` |
| `PATCH` | `/api/families/mine` |
| `PATCH` | `/api/families/mine/prize-rules` |
| `DELETE` | `/api/families/mine/members/{memberId}` |
| `DELETE` | `/api/families/mine` |

---

## File Map

| File | Action |
|---|---|
| `backend/TinyHeroes.Domain/Enums/FamilyCapability.cs` | Create |
| `backend/TinyHeroes.Domain/Permissions/RolePermissions.cs` | Create |
| `backend/TinyHeroes.Api/Controllers/ApiControllerBase.cs` | Modify |
| `backend/TinyHeroes.Api/Controllers/ChildController.cs` | Modify |
| `backend/TinyHeroes.Api/Controllers/FamilyController.cs` | Modify |
| `backend/TinyHeroes.Api/Controllers/InviteController.cs` | Modify |
| `backend/TinyHeroes.Api/Controllers/PresetController.cs` | Modify |
| `backend/TinyHeroes.Api/Controllers/PrizePresetController.cs` | Modify |
| `backend/TinyHeroes.Api/Controllers/PrizeAssignmentController.cs` | Modify |
| `backend/TinyHeroes.Application/DTOs/Family/UpdateMemberRoleRequest.cs` | Create |
| `backend/TinyHeroes.Tests/Integration/FamilySettingsControllerTests.cs` | Modify |
| `frontend/src/app/core/models/family.model.ts` | Modify — add `FamilyRole` union type + `ROLE_CAPABILITIES` map |
| `frontend/src/app/core/services/family.service.ts` | Modify — use capability map, add `updateMemberRole()` |
| `frontend/src/app/features/settings/pages/settings.component.ts` | Modify — use capability |
| `frontend/src/app/features/settings/pages/settings.component.spec.ts` | Modify |
| `frontend/src/app/features/settings/pages/family-settings.component.ts` | Modify |
| `frontend/src/app/features/settings/pages/prize-rules.component.ts` | Modify |
| `frontend/src/app/app.routes.ts` | Modify — add `settings/members` |
| `frontend/src/app/features/settings/pages/member-roles.component.ts` | Create |
| `frontend/public/assets/i18n/en.json` | Modify |
| `frontend/public/assets/i18n/{es,fr,de,hu}.json` | Modify |
| `frontend/src/app/features/help/help.component.ts` | Modify |
| `frontend/src/environments/environment.ts` | Modify — `2.4.3` → `2.5.0` |
| `frontend/src/environments/environment.prod.ts` | Modify |
| `CHANGELOG.md` | Modify |

---

## Task 1: Create feature branch

- [ ] **Step 1**

```bash
git checkout master && git pull
git checkout -b feat/14-role-management
```

---

## Task 2: Backend domain — `FamilyCapability` and `RolePermissions`

**Files:**
- Create: `backend/TinyHeroes.Domain/Enums/FamilyCapability.cs`
- Create: `backend/TinyHeroes.Domain/Permissions/RolePermissions.cs`

Not stored in the database — in-memory only. No migration needed.

- [ ] **Step 1: Create `FamilyCapability.cs`**

```csharp
// backend/TinyHeroes.Domain/Enums/FamilyCapability.cs
namespace TinyHeroes.Domain.Enums;

public enum FamilyCapability
{
    ManageFamily,  // settings, members, children, presets, prizes
    AddDeeds,      // record good deeds
    ViewFamily,    // read-only access to family data
}
```

- [ ] **Step 2: Create `RolePermissions.cs`**

```csharp
// backend/TinyHeroes.Domain/Permissions/RolePermissions.cs
using TinyHeroes.Domain.Enums;

namespace TinyHeroes.Domain.Permissions;

public static class RolePermissions
{
    private static readonly Dictionary<FamilyRole, HashSet<FamilyCapability>> Map = new()
    {
        [FamilyRole.Admin]    = [FamilyCapability.ManageFamily, FamilyCapability.AddDeeds, FamilyCapability.ViewFamily],
        [FamilyRole.CoParent] = [FamilyCapability.AddDeeds, FamilyCapability.ViewFamily],
        // To add a new role: add one entry here. No controller changes needed.
    };

    public static bool Can(FamilyRole role, FamilyCapability capability) =>
        Map.TryGetValue(role, out var caps) && caps.Contains(capability);
}
```

- [ ] **Step 3: Verify build**

```bash
cd backend && dotnet build TinyHeroes.Domain
```
Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add backend/TinyHeroes.Domain/Enums/FamilyCapability.cs \
        backend/TinyHeroes.Domain/Permissions/RolePermissions.cs
git commit -m "feat: add FamilyCapability enum and RolePermissions map to Domain"
```

---

## Task 3: Backend — `ApiControllerBase` helpers

**File:** `backend/TinyHeroes.Api/Controllers/ApiControllerBase.cs`

Controllers call `RequireAdmin(db)` — they never name a role. `RequireAdmin` is a convenience alias for `ManageFamily`. When a future role needs a different capability gate (e.g. `AddDeeds`), a controller calls `RequireCapability(db, FamilyCapability.AddDeeds)` directly — still no role name in controller code.

- [ ] **Step 1: Replace the entire file**

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TinyHeroes.Domain.Entities;
using TinyHeroes.Domain.Enums;
using TinyHeroes.Domain.Permissions;
using TinyHeroes.Infrastructure.Data;

namespace TinyHeroes.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

    protected async Task<(FamilyMember? Member, ActionResult? Error)> RequireFamilyMember(AppDbContext db)
    {
        var userId = GetUserId();
        var member = await db.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
        return member is null
            ? (null, BadRequest("User does not belong to a family."))
            : (member, null);
    }

    protected async Task<(FamilyMember? Member, ActionResult? Error)> RequireCapability(
        AppDbContext db, FamilyCapability capability)
    {
        var (member, error) = await RequireFamilyMember(db);
        if (error is not null) return (null, error);
        return RolePermissions.Can(member!.Role, capability)
            ? (member, null)
            : (null, Forbid());
    }

    // Alias — use for any action requiring ManageFamily capability.
    protected Task<(FamilyMember? Member, ActionResult? Error)> RequireAdmin(AppDbContext db) =>
        RequireCapability(db, FamilyCapability.ManageFamily);
}
```

- [ ] **Step 2: Verify build**

```bash
cd backend && dotnet build TinyHeroes.Api
```
Expected: `Build succeeded.`

---

## Task 4: Backend — refactor `ChildController`

**File:** `backend/TinyHeroes.Api/Controllers/ChildController.cs`

- [ ] **Step 1: Replace every member-only lookup block**

```csharp
// Before (5 occurrences — Create, List, Get, Update, UploadAvatar):
var userId = GetUserId();
var membership = await db.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
if (membership is null) return BadRequest("User does not belong to a family.");

// After:
var (membership, memberError) = await RequireFamilyMember(db);
if (memberError is not null) return memberError;
```

- [ ] **Step 2: Replace the admin check on Delete (~line 94)**

```csharp
// Before (member lookup + role check):
var userId = GetUserId();
var membership = await db.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
if (membership is null) return BadRequest("User does not belong to a family.");
if (membership.Role != FamilyRole.Admin) return Forbid();

// After:
var (membership, adminError) = await RequireAdmin(db);
if (adminError is not null) return adminError;
```

- [ ] **Step 3: Run tests**

```bash
cd backend && dotnet test --filter "FullyQualifiedName~ChildControllerTests"
```
Expected: all PASS.

- [ ] **Step 4: Commit**

```bash
git add backend/TinyHeroes.Api/Controllers/ApiControllerBase.cs \
        backend/TinyHeroes.Api/Controllers/ChildController.cs
git commit -m "refactor: add RequireCapability/RequireAdmin to ApiControllerBase, refactor ChildController"
```

---

## Task 5: Backend — refactor remaining 4 controllers

**Files:** `InviteController.cs`, `PresetController.cs`, `PrizePresetController.cs`, `PrizeAssignmentController.cs`

Each has the same 4-line inline pattern. Locations: `InviteController` ~line 24, `PresetController` ~line 60, `PrizePresetController` ~lines 40 + 61, `PrizeAssignmentController` ~line 40.

- [ ] **Step 1: Replace each admin-gated block with `RequireAdmin`**

```csharp
// Before:
var userId = GetUserId();
var membership = await db.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
if (membership is null) return BadRequest("User does not belong to a family.");
if (membership.Role != FamilyRole.Admin) return Forbid();

// After:
var (membership, adminError) = await RequireAdmin(db);
if (adminError is not null) return adminError;
```

For any endpoint with a member-only check (no role gate), use `RequireFamilyMember` instead.

- [ ] **Step 2: Run full test suite**

```bash
cd backend && dotnet test
```
Expected: all PASS.

- [ ] **Step 3: Commit**

```bash
git add backend/TinyHeroes.Api/Controllers/InviteController.cs \
        backend/TinyHeroes.Api/Controllers/PresetController.cs \
        backend/TinyHeroes.Api/Controllers/PrizePresetController.cs \
        backend/TinyHeroes.Api/Controllers/PrizeAssignmentController.cs
git commit -m "refactor: use RequireAdmin helper in InviteController, PresetController, PrizePresetController, PrizeAssignmentController"
```

---

## Task 6: Backend — refactor `FamilyController`, remove `GetAdminFamily()`

**File:** `backend/TinyHeroes.Api/Controllers/FamilyController.cs`

Private `GetAdminFamily()` at ~line 200; called at ~lines 77 + 90. Four further inline checks at ~lines 106, 124, 140, 164.

- [ ] **Step 1: Replace the two `GetAdminFamily()` call-sites**

```csharp
// Before:
var result = await GetAdminFamily();
if (result.Error is not null) return result.Error;
var family = result.Family!;

// After:
var (member, adminError) = await RequireAdmin(db);
if (adminError is not null) return adminError;
var family = await db.Families
    .Include(f => f.Members).ThenInclude(m => m.User)  // adjust includes per endpoint
    .FirstOrDefaultAsync(f => f.Id == member!.FamilyId);
```

- [ ] **Step 2: Replace the 4 inline admin checks** (same pattern as Tasks 4–5)

- [ ] **Step 3: Delete the `GetAdminFamily()` private method**

- [ ] **Step 4: Run full test suite**

```bash
cd backend && dotnet test
```
Expected: all PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/TinyHeroes.Api/Controllers/FamilyController.cs
git commit -m "refactor: remove GetAdminFamily from FamilyController, use base class RequireAdmin"
```

---

## Task 7: Backend — write failing tests for the role endpoint

**File:** `backend/TinyHeroes.Tests/Integration/FamilySettingsControllerTests.cs`

> **`userId` in URL, not pivot `Id`:** `FamilyDetailResponse.Members` already returns `UserId`. Consistent with `DELETE mine/members/{memberId}` which also receives a `UserId` value despite the parameter name.

- [ ] **Step 1: Add private helper + 6 tests**

```csharp
private async Task<(HttpClient adminClient, HttpClient coParentClient, string coParentUserId)> CreateTwoMemberFamily()
{
    var adminClient = await TestAuthHelper.RegisterWithFamily(factory);
    var inviteResponse = await adminClient.PostAsJsonAsync("/api/invites", new CreateInviteRequest(null));
    var invite = await inviteResponse.Content.ReadFromJsonAsync<InviteResponse>(TestWebApplicationFactory<Program>.JsonOptions);

    var coParentClient = await TestAuthHelper.RegisterOnly(factory);
    await coParentClient.PostAsync($"/api/invites/{invite!.Token}/accept", null);

    var familyResponse = await adminClient.GetAsync("/api/families/mine");
    var family = await familyResponse.Content.ReadFromJsonAsync<FamilyDetailResponse>(TestWebApplicationFactory<Program>.JsonOptions);
    var coParentMember = family!.Members.First(m => m.Role == "CoParent");

    return (adminClient, coParentClient, coParentMember.UserId.ToString());
}

[Fact]
public async Task UpdateMemberRole_AdminPromotesCoParent_Returns200WithAdminRole()
{
    var (adminClient, _, coParentUserId) = await CreateTwoMemberFamily();

    var response = await adminClient.PatchAsJsonAsync(
        $"/api/families/mine/members/{coParentUserId}/role",
        new { role = "Admin" });

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var member = await response.Content.ReadFromJsonAsync<FamilyMemberResponse>(TestWebApplicationFactory<Program>.JsonOptions);
    member!.Role.Should().Be("Admin");
}

[Fact]
public async Task UpdateMemberRole_AdminDemotesOtherAdmin_Returns200WithCoParentRole()
{
    var (adminClient, _, coParentUserId) = await CreateTwoMemberFamily();

    await adminClient.PatchAsJsonAsync(
        $"/api/families/mine/members/{coParentUserId}/role",
        new { role = "Admin" });

    var response = await adminClient.PatchAsJsonAsync(
        $"/api/families/mine/members/{coParentUserId}/role",
        new { role = "CoParent" });

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var member = await response.Content.ReadFromJsonAsync<FamilyMemberResponse>(TestWebApplicationFactory<Program>.JsonOptions);
    member!.Role.Should().Be("CoParent");
}

[Fact]
public async Task UpdateMemberRole_LastAdminDemotesSelf_Returns400WithLastAdminError()
{
    var adminClient = await TestAuthHelper.RegisterWithFamily(factory);

    var familyResponse = await adminClient.GetAsync("/api/families/mine");
    var family = await familyResponse.Content.ReadFromJsonAsync<FamilyDetailResponse>(TestWebApplicationFactory<Program>.JsonOptions);
    var selfUserId = family!.Members.Single().UserId.ToString();

    var response = await adminClient.PatchAsJsonAsync(
        $"/api/families/mine/members/{selfUserId}/role",
        new { role = "CoParent" });

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    var body = await response.Content.ReadAsStringAsync();
    body.Should().Contain("last_admin");
}

[Fact]
public async Task UpdateMemberRole_CoParentTriesToChangeRole_Returns403()
{
    var (adminClient, coParentClient, _) = await CreateTwoMemberFamily();

    var familyResponse = await adminClient.GetAsync("/api/families/mine");
    var family = await familyResponse.Content.ReadFromJsonAsync<FamilyDetailResponse>(TestWebApplicationFactory<Program>.JsonOptions);
    var adminUserId = family!.Members.First(m => m.Role == "Admin").UserId.ToString();

    var response = await coParentClient.PatchAsJsonAsync(
        $"/api/families/mine/members/{adminUserId}/role",
        new { role = "CoParent" });

    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
}

[Fact]
public async Task UpdateMemberRole_Unauthenticated_Returns401()
{
    var client = factory.CreateClient();
    var response = await client.PatchAsJsonAsync(
        $"/api/families/mine/members/{Guid.NewGuid()}/role",
        new { role = "Admin" });

    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}

[Fact]
public async Task UpdateMemberRole_TargetUserNotInFamily_Returns404()
{
    var adminClient = await TestAuthHelper.RegisterWithFamily(factory);

    var response = await adminClient.PatchAsJsonAsync(
        $"/api/families/mine/members/{Guid.NewGuid()}/role",
        new { role = "CoParent" });

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
}
```

- [ ] **Step 2: Confirm 6 new tests FAIL, existing tests PASS**

```bash
cd backend && dotnet test --filter "FullyQualifiedName~FamilySettingsControllerTests.UpdateMemberRole"
```

---

## Task 8: Backend — implement the role endpoint

**Files:**
- Create: `backend/TinyHeroes.Application/DTOs/Family/UpdateMemberRoleRequest.cs`
- Modify: `backend/TinyHeroes.Api/Controllers/FamilyController.cs`

**Contract:**
```
PATCH /api/families/mine/members/{userId}/role
Body: { "role": "Admin" | "CoParent" }

200  → FamilyMemberResponse
400  → "last_admin"  (self-demotion, sole admin)
403  → caller lacks ManageFamily capability
404  → target not in caller's family
```

**Last-admin guard:** fires only on self-demotion. Demoting *another* Admin is always allowed — the caller stays Admin, so at least one Admin always remains.

- [ ] **Step 1: Create the DTO**

```csharp
// backend/TinyHeroes.Application/DTOs/Family/UpdateMemberRoleRequest.cs
using TinyHeroes.Domain.Enums;

namespace TinyHeroes.Application.DTOs.Family;

public record UpdateMemberRoleRequest(FamilyRole Role);
```

- [ ] **Step 2: Add endpoint to `FamilyController` after `RemoveMember`**

```csharp
[HttpPatch("mine/members/{userId:guid}/role")]
public async Task<ActionResult<FamilyMemberResponse>> UpdateMemberRole(Guid userId, UpdateMemberRoleRequest req)
{
    var (caller, adminError) = await RequireAdmin(db);
    if (adminError is not null) return adminError;

    var target = await db.FamilyMembers
        .Include(m => m.User)
        .FirstOrDefaultAsync(m => m.UserId == userId && m.FamilyId == caller!.FamilyId);
    if (target is null) return NotFound();

    if (req.Role == FamilyRole.CoParent && target.UserId == caller!.UserId)
    {
        var adminCount = await db.FamilyMembers
            .CountAsync(m => m.FamilyId == caller.FamilyId && m.Role == FamilyRole.Admin);
        if (adminCount <= 1)
            return BadRequest("last_admin");
    }

    target.Role = req.Role;
    await db.SaveChangesAsync();

    return Ok(new FamilyMemberResponse(
        target.UserId,
        target.User.DisplayName,
        target.User.Email!,
        target.Role.ToString()));
}
```

- [ ] **Step 3: Run the 6 new tests**

```bash
cd backend && dotnet test --filter "FullyQualifiedName~FamilySettingsControllerTests.UpdateMemberRole"
```
Expected: all 6 PASS.

- [ ] **Step 4: Run full suite**

```bash
cd backend && dotnet test
```
Expected: all PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/TinyHeroes.Application/DTOs/Family/UpdateMemberRoleRequest.cs \
        backend/TinyHeroes.Api/Controllers/FamilyController.cs \
        backend/TinyHeroes.Tests/Integration/FamilySettingsControllerTests.cs
git commit -m "feat: add PATCH /api/families/mine/members/{userId}/role with last-admin guard"
```

---

## Task 9: Frontend — capability map in `family.model.ts`

**File:** `frontend/src/app/core/models/family.model.ts`

This is the single source of truth for frontend role knowledge. Adding a new role means adding one entry here — no template or service edits.

`FamilyRole` is a union type (not an enum) so TypeScript enforces exhaustiveness. `ROLE_CAPABILITIES` mirrors the backend `RolePermissions` map. `ASSIGNABLE_ROLES` drives the `<select>` options in `MemberRolesComponent` — add a new role here and the dropdown gains a new option automatically.

- [ ] **Step 1: Add types and the capability map**

```typescript
// Add to family.model.ts:

export type FamilyRole = 'Admin' | 'CoParent';
// When adding a new role: extend this union, add an entry to ROLE_CAPABILITIES.

export interface RoleCapabilities {
  canManageFamily: boolean;
  canAddDeeds: boolean;
  canView: boolean;
}

export const ROLE_CAPABILITIES: Record<FamilyRole, RoleCapabilities> = {
  Admin:    { canManageFamily: true,  canAddDeeds: true,  canView: true },
  CoParent: { canManageFamily: false, canAddDeeds: true,  canView: true },
  // Grandparent: { canManageFamily: false, canAddDeeds: true,  canView: true },
  // ReadOnly:    { canManageFamily: false, canAddDeeds: false, canView: true },
};

// Roles that can be assigned via the role management screen.
// Remove a role from this list to hide it from the UI without deleting its capabilities.
export const ASSIGNABLE_ROLES: FamilyRole[] = ['Admin', 'CoParent'];

// Update FamilyMember.role from string → FamilyRole:
export interface FamilyMember {
  userId: string;
  displayName: string;
  email: string;
  role: FamilyRole;
}
```

- [ ] **Step 2: Verify TypeScript compiles**

```bash
cd frontend && npx ng build --configuration production 2>&1 | grep "error TS"
```
Expected: no output. If the compiler flags `role: string` assignments elsewhere, cast them: `role: response.role as FamilyRole`.

---

## Task 10: Frontend — update `FamilyService`

**File:** `frontend/src/app/core/services/family.service.ts`

`isAdmin` is renamed `canManageFamily` and now reads from `ROLE_CAPABILITIES` — no role string comparison in the service.

- [ ] **Step 1: Replace `isAdmin` + add `updateMemberRole()`**

```typescript
import { ROLE_CAPABILITIES } from '../models/family.model';

// Replace isAdmin with:
readonly canManageFamily = computed(() => {
  const family = this._family();
  const userId = this.authService.user()?.userId ?? '';
  const member = family?.members.find(m => m.userId === userId);
  return member ? ROLE_CAPABILITIES[member.role]?.canManageFamily ?? false : false;
});

// Add after removeMember():
updateMemberRole(userId: string, role: FamilyRole) {
  return this.http.patch<FamilyMember>(
    `${environment.apiUrl}/families/mine/members/${userId}/role`,
    { role }
  );
}
```

Add `FamilyRole` and `FamilyMember` to the import from `'../models/family.model'`.

---

## Task 11: Frontend — update consumers of `isAdmin`

Four files reference `isAdmin` directly; rename each to `canManageFamily`.

- [ ] **Step 1: `settings.component.ts` — line 129**

```typescript
// Before:
isAdmin = this.familyService.isAdmin;
// After:
canManageFamily = this.familyService.canManageFamily;
```

The template uses `isAdmin()` — rename to `canManageFamily()` in the template (line 66).

- [ ] **Step 2: `settings.component.spec.ts` — line 26**

```typescript
// Before:
useValue: { isAdmin: isAdminSignal, loadFamily: vi.fn(), family: signal(undefined) },
// After:
useValue: { canManageFamily: isAdminSignal, loadFamily: vi.fn(), family: signal(undefined) },
```

- [ ] **Step 3: `family-settings.component.ts`**

Line 166 — rename `isAdmin` computed to `canManageFamily` and update its 4 usages in the template (lines 57, 82, 93, 104).

Also fix line 78 — `member.role === 'Admin' ? ...` reads a role name in a template. Replace with the capability map:

```html
<!-- Before: -->
<p class="text-xs text-brand-muted">
  {{ member.role === 'Admin' ? ('SETTINGS.ADMIN_BADGE' | translate) : ('SETTINGS.COPARENT_BADGE' | translate) }}
</p>

<!-- After: -->
<p class="text-xs text-brand-muted">{{ 'SETTINGS.ROLE_' + member.role.toUpperCase() | translate }}</p>
```

This resolves e.g. `SETTINGS.ROLE_ADMIN`, `SETTINGS.ROLE_COPARENT` — add a new role's translation key and the label appears automatically.

- [ ] **Step 4: `prize-rules.component.ts` — line 88**

```typescript
// Before:
if (f === null || !this.familyService.isAdmin()) {
// After:
if (f === null || !this.familyService.canManageFamily()) {
```

- [ ] **Step 5: Verify TypeScript compiles**

```bash
cd frontend && npx ng build --configuration production 2>&1 | grep "error TS"
```
Expected: no output.

- [ ] **Step 6: Commit**

```bash
git add frontend/src/app/core/models/family.model.ts \
        frontend/src/app/core/services/family.service.ts \
        frontend/src/app/features/settings/pages/settings.component.ts \
        frontend/src/app/features/settings/pages/settings.component.spec.ts \
        frontend/src/app/features/settings/pages/family-settings.component.ts \
        frontend/src/app/features/settings/pages/prize-rules.component.ts
git commit -m "refactor: introduce FamilyRole type + ROLE_CAPABILITIES map, rename isAdmin → canManageFamily"
```

---

## Task 12: Frontend — add i18n keys

**Files:** all 5 translation JSON files in `frontend/public/assets/i18n/`

- [ ] **Step 1: Add keys to `en.json` inside the `"SETTINGS"` block**

```json
"MANAGE_ROLES": "Manage roles",
"MANAGE_ROLES_TITLE": "Manage Member Roles",
"ROLE_ADMIN": "Admin",
"ROLE_COPARENT": "CoParent",
"ROLE_CHANGE_ERROR_LAST_ADMIN": "You are the only admin — promote another member first"
```

> Adding a new role later only requires a new `SETTINGS.ROLE_<ROLENAME>` key per language — the template already reads it dynamically via `'SETTINGS.ROLE_' + member.role.toUpperCase()`.

- [ ] **Step 2: Add same keys to `es.json`, `fr.json`, `de.json`, `hu.json`** (English placeholders for now)

---

## Task 13: Frontend — create `MemberRolesComponent`

**File:** `frontend/src/app/features/settings/pages/member-roles.component.ts` (new)

Role options are derived from `ASSIGNABLE_ROLES` — the template never names a role. Adding a new role to `ASSIGNABLE_ROLES` in the model automatically adds it to this dropdown.

- [ ] **Step 1: Create the component**

```typescript
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { AuthService } from '../../../core/auth/auth.service';
import { ASSIGNABLE_ROLES, FamilyRole } from '../../../core/models/family.model';
import { FamilyService } from '../../../core/services/family.service';

@Component({
  selector: 'app-member-roles',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslateModule],
  template: `
    <div class="max-w-lg mx-auto p-4">
      <div class="flex items-center gap-2 mb-4">
        <button (click)="router.navigate(['/settings/family'])" class="text-brand-muted text-xl leading-none">←</button>
        <h1 class="text-lg font-bold text-brand-text">{{ 'SETTINGS.MANAGE_ROLES_TITLE' | translate }}</h1>
      </div>

      @if (otherMembers().length === 0) {
        <p class="text-sm text-brand-muted text-center py-8">{{ 'SETTINGS.CO_PARENTS_EMPTY' | translate }}</p>
      } @else {
        <div class="bg-white rounded-xl border border-brand-border divide-y divide-brand-border">
          @for (member of otherMembers(); track member.userId) {
            <div class="flex items-center gap-3 p-3">
              <div class="w-9 h-9 rounded-full bg-gradient-to-br from-brand-orange to-orange-400 flex items-center justify-center text-white text-sm font-bold flex-shrink-0">
                {{ member.displayName[0].toUpperCase() }}
              </div>
              <div class="flex-1 min-w-0">
                <p class="text-sm font-bold text-brand-text truncate">{{ member.displayName }}</p>
                <p class="text-xs text-brand-muted truncate">{{ member.email }}</p>
              </div>
              <select (change)="changeRole(member.userId, $event)"
                class="text-xs border border-brand-border rounded-lg px-2 py-1.5 bg-white text-brand-text flex-shrink-0">
                @for (role of assignableRoles; track role) {
                  <option [value]="role" [selected]="member.role === role">
                    {{ 'SETTINGS.ROLE_' + role.toUpperCase() | translate }}
                  </option>
                }
              </select>
            </div>
          }
        </div>
      }

      @if (errorMessage()) {
        <p class="text-xs text-red-600 mt-3 px-1">{{ errorMessage() }}</p>
      }
    </div>
  `
})
export class MemberRolesComponent implements OnInit {
  protected router = inject(Router);
  private familyService = inject(FamilyService);
  private authService = inject(AuthService);
  private ts = inject(TranslateService);

  readonly assignableRoles = ASSIGNABLE_ROLES;
  errorMessage = signal('');

  private currentUserId = computed(() => this.authService.user()?.userId ?? '');
  otherMembers = computed(() =>
    (this.familyService.family()?.members ?? []).filter(m => m.userId !== this.currentUserId())
  );

  ngOnInit() {
    this.familyService.loadFamily();
  }

  changeRole(userId: string, event: Event) {
    const role = (event.target as HTMLSelectElement).value as FamilyRole;
    this.errorMessage.set('');
    this.familyService.updateMemberRole(userId, role).subscribe({
      next: () => this.familyService.loadFamily(),
      error: (err) => {
        if (err.status === 400 && typeof err.error === 'string' && err.error.includes('last_admin')) {
          this.errorMessage.set(this.ts.instant('SETTINGS.ROLE_CHANGE_ERROR_LAST_ADMIN'));
        }
      }
    });
  }
}
```

- [ ] **Step 2: Register the route in `app.routes.ts`** (after `settings/family`, ~line 40)

```typescript
{ path: 'settings/members', loadComponent: () => import('./features/settings/pages/member-roles.component').then(m => m.MemberRolesComponent) },
```

- [ ] **Step 3: Verify TypeScript compiles**

```bash
cd frontend && npx ng build --configuration production 2>&1 | grep "error TS"
```
Expected: no output.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/app/features/settings/pages/member-roles.component.ts \
        frontend/src/app/app.routes.ts \
        frontend/public/assets/i18n/en.json \
        frontend/public/assets/i18n/es.json \
        frontend/public/assets/i18n/fr.json \
        frontend/public/assets/i18n/de.json \
        frontend/public/assets/i18n/hu.json
git commit -m "feat: add MemberRolesComponent at settings/members for admin role management"
```

---

## Task 14: Frontend — link from `FamilySettingsComponent`

**File:** `frontend/src/app/features/settings/pages/family-settings.component.ts`

- [ ] **Step 1: Add "Manage roles →" link after the `settings/invite` row (~line 89)**

```html
@if (canManageFamily()) {
  <a routerLink="/settings/members"
    class="flex items-center justify-between p-3 border-t border-brand-border">
    <span class="text-sm font-semibold text-brand-orange">{{ 'SETTINGS.MANAGE_ROLES' | translate }}</span>
    <span class="text-brand-muted">→</span>
  </a>
}
```

- [ ] **Step 2: Verify TypeScript compiles**

```bash
cd frontend && npx ng build --configuration production 2>&1 | grep "error TS"
```

- [ ] **Step 3: Commit**

```bash
git add frontend/src/app/features/settings/pages/family-settings.component.ts
git commit -m "feat: add 'Manage roles' link to family settings for admins"
```

---

## Task 15: Docs, CHANGELOG, version bump

- [ ] **Step 1: Update `help.component.ts`** — add: Admins see a "Manage roles" link in family settings. On the screen each member has a role dropdown. Self-demotion as last admin is blocked.

- [ ] **Step 2: Add to `CHANGELOG.md` under `## [Unreleased]`**

```markdown
### Added
- Role management screen at Settings → Family → Manage roles: Admins can change other members' roles. Self-demotion as the last admin is blocked.
- `PATCH /api/families/mine/members/{userId}/role` — change a family member's role.
```

- [ ] **Step 3: Bump frontend version `2.4.3` → `2.5.0`** in both environment files

- [ ] **Step 4: Commit**

```bash
git add frontend/src/app/features/help/help.component.ts \
        CHANGELOG.md \
        frontend/src/environments/environment.ts \
        frontend/src/environments/environment.prod.ts
git commit -m "chore: bump frontend version to 2.5.0, update changelog and help for role management"
```

---

## Task 16: Review, push, PR

- [ ] **Step 1: Full backend test suite**

```bash
cd backend && dotnet test
```

- [ ] **Step 2: Frontend production build**

```bash
cd frontend && npx ng build --configuration production 2>&1 | tail -5
```

- [ ] **Step 3: `/code-review`**

- [ ] **Step 4: `/security-review`**

- [ ] **Step 5: Push**

```bash
git push -u origin feat/14-role-management
```

- [ ] **Step 6: Open PR**

```bash
gh pr create --base master \
  --title "feat: role management — Admin and CoParent permissions" \
  --body "Closes #14

## Summary
- \`FamilyCapability\` + \`RolePermissions\` in Domain — adding a new role = 2 file edits, no controller changes
- \`ApiControllerBase.RequireCapability\` / \`RequireAdmin\` — controllers never name a role directly
- \`FamilyRole\` union type + \`ROLE_CAPABILITIES\` + \`ASSIGNABLE_ROLES\` in \`family.model.ts\` — templates never name a role directly
- \`PATCH /api/families/mine/members/{userId}/role\` with last-admin guard
- New \`settings/members\` screen (\`OnPush\`) — role dropdown derived from \`ASSIGNABLE_ROLES\`
- Renamed \`isAdmin\` → \`canManageFamily\` throughout

## Test plan
- [ ] \`dotnet test\` — all existing + 6 new role-management tests pass
- [ ] Integration env: Settings → Family → 'Manage roles' visible as Admin
- [ ] Invite second user → Manage roles → change to Admin → reloads correctly
- [ ] Demote back to CoParent → succeeds
- [ ] Try to demote yourself as sole Admin → error message, role unchanged
- [ ] Log in as CoParent → no 'Manage roles' link"
```

- [ ] **Step 7: Update project card to In Review**

```bash
gh project item-list 5 --owner DARKinVADER --format json \
  | jq '.items[] | select(.content.number == 14) | .id'

gh project item-edit \
  --project-id PVT_kwHOACNlms4BZWnC \
  --id <PVTI_from_above> \
  --field-id PVTSSF_lAHOACNlms4BZWnCzhUV0xU \
  --single-select-option-id b08d4b47
```

---

## Verification

End-to-end on `https://integration.mytinyheroes.net` (`testuser@demo.com / Password1!`):

1. Settings → Family → "Manage roles →" link visible as Admin
2. Invite a second account; accept as CoParent → appears in the list
3. Change them to Admin → page reloads → dropdown shows Admin
4. Try to demote yourself (sole Admin) → error: *"You are the only admin — promote another member first"*
5. Log in as CoParent → Settings → Family → no "Manage roles" link

---

## Adding a new role in the future

The checklist is **two files** — everything else (controllers, templates, dropdowns, labels) picks it up automatically:

| # | File | What to add |
|---|---|---|
| 1 | `backend/TinyHeroes.Domain/Enums/FamilyRole.cs` | New enum value, e.g. `Grandparent` |
| 2 | `backend/TinyHeroes.Domain/Permissions/RolePermissions.cs` | One map entry: `[FamilyRole.Grandparent] = [AddDeeds, ViewFamily]` |
| 3 | `frontend/src/app/core/models/family.model.ts` | Extend `FamilyRole` union; add entry to `ROLE_CAPABILITIES`; optionally add to `ASSIGNABLE_ROLES` |
| 4 | `frontend/public/assets/i18n/*.json` | Add `SETTINGS.ROLE_GRANDPARENT` key in each language |

No controller changes, no template changes, no service changes. Items 3 and 4 are frontend display concerns — the backend enforces the role correctly as soon as items 1 and 2 are done.

> **EF Core note:** `FamilyRole` is stored as `integer` (EF default, confirmed in migration snapshot). Adding a new enum value at the end of the enum preserves existing integer mappings — no migration needed. If values are inserted out of order or the enum is rearranged, a migration *is* required.
