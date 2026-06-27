# TinyHeroes — Plan 2: Family Management, Child Profiles, Co-Parent Invites

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add core family features: managing children, the home dashboard, and co-parent invites.

**Architecture:** Controllers access DbContext directly for simple CRUD. Signal-based Angular services. Shell layout with bottom nav wrapping protected routes.

**Tech Stack:** ASP.NET Core 10, EF Core 10, Angular 21, Tailwind 4, ngx-translate

**Status:** COMPLETE (2026-05-16)

---

## Context

Plan 1 (Foundation & Auth) is complete. We have a working auth flow (register/login/social), family creation, and Docker infrastructure. Plan 2 adds the core family features: managing children, the home dashboard, and co-parent invites. After this plan, parents can add children to their family, view them on the dashboard, invite a co-parent, and navigate between app sections.

**Design spec:** `docs/superpowers/specs/2026-05-16-tinyheroes-design.md` — Screens 5-8.

---

## Architecture Decisions

1. **Avatars: Emoji-only for Plan 2.** No photo upload yet — that arrives in Plan 3 alongside deed images (shared FluentStorage infrastructure).
2. **Deed stats: Zero for now.** Dashboard child cards show 0 deed counts. Real data comes in Plan 3.
3. **Full bottom nav: Yes, with stub routes.** Podium, Prizes, Settings get placeholder components.
4. **No separate service layer for simple CRUD.** Controllers access DbContext directly (same pattern as existing FamilyController). Services added when business logic warrants it.

---

## Task Overview (10 Tasks)

| # | Task | Layer |
|---|------|-------|
| 1 | Child entity + Gender enum + EF config + migration | Backend |
| 2 | FamilyInvite entity + EF config + migration | Backend |
| 3 | Child CRUD controller + integration tests (TDD) | Backend |
| 4 | Family GET endpoint + Invite controller + tests (TDD) | Backend |
| 5 | Test helper refactor (shared auth setup) | Backend |
| 6 | Bottom nav + shell layout + stub routes | Frontend |
| 7 | Family & Child services + Dashboard page | Frontend |
| 8 | Add Child page (avatar picker, age spinner, gender) | Frontend |
| 9 | Child Profile page + Invite Co-Parent page | Frontend |
| 10 | i18n strings (en + hu) + final build verification | Frontend |

---

### Task 1: Child Entity + EF Configuration

**Files:**
- Create: `backend/TinyHeroes.Domain/Entities/Child.cs`
- Create: `backend/TinyHeroes.Domain/Enums/Gender.cs`
- Create: `backend/TinyHeroes.Infrastructure/Data/Configurations/ChildConfiguration.cs`
- Modify: `backend/TinyHeroes.Infrastructure/Data/AppDbContext.cs` — add `DbSet<Child>`
- Modify: `backend/TinyHeroes.Domain/Entities/Family.cs` — add `ICollection<Child> Children` nav property

```csharp
// Domain/Enums/Gender.cs
public enum Gender { Boy, Girl }

// Domain/Entities/Child.cs
public class Child
{
    public Guid Id { get; set; }
    public Guid FamilyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public Gender Gender { get; set; }
    public string AvatarEmoji { get; set; } = "🦸";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Family Family { get; set; } = null!;
}
```

**ChildConfiguration:** FK to Family, index on FamilyId, Name max length 100.

**Commit:** `feat: Child entity with EF configuration`

---

### Task 2: FamilyInvite Entity + EF Configuration

**Files:**
- Create: `backend/TinyHeroes.Domain/Entities/FamilyInvite.cs`
- Create: `backend/TinyHeroes.Infrastructure/Data/Configurations/FamilyInviteConfiguration.cs`
- Modify: `backend/TinyHeroes.Infrastructure/Data/AppDbContext.cs` — add `DbSet<FamilyInvite>`
- Modify: `backend/TinyHeroes.Domain/Entities/Family.cs` — add `ICollection<FamilyInvite> Invites` nav property

```csharp
// Domain/Entities/FamilyInvite.cs
public class FamilyInvite
{
    public Guid Id { get; set; }
    public Guid FamilyId { get; set; }
    public string? Email { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool Accepted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Family Family { get; set; } = null!;
}
```

**Configuration:** FK to Family, unique index on Token, index on Email.

**Commit:** `feat: FamilyInvite entity with EF configuration`

---

### Task 3: Child CRUD Controller + Tests (TDD)

**Files:**
- Create: `backend/TinyHeroes.Application/DTOs/Child/CreateChildRequest.cs`
- Create: `backend/TinyHeroes.Application/DTOs/Child/UpdateChildRequest.cs`
- Create: `backend/TinyHeroes.Application/DTOs/Child/ChildResponse.cs`
- Create: `backend/TinyHeroes.Api/Controllers/ChildController.cs`
- Create: `backend/TinyHeroes.Tests/Integration/ChildControllerTests.cs`

**Endpoints:**
- `POST /api/children` — create child in user's family
- `GET /api/children` — list children in user's family
- `GET /api/children/{id}` — get single child (must belong to user's family)
- `PUT /api/children/{id}` — update child
- `DELETE /api/children/{id}` — delete child (Admin only)

**Authorization:** All endpoints require `[Authorize]`. Extract userId from claims, look up FamilyMember to get familyId. Return 400 if user has no family, 403 if delete by non-admin, 404 if child not in user's family.

**Tests (7):**
1. Create_WithValidData_Returns200
2. Create_WithoutFamily_Returns400
3. List_ReturnsAllFamilyChildren
4. Get_ReturnsChild
5. Update_ModifiesChild
6. Delete_AsAdmin_Succeeds
7. Delete_AsCoParent_Returns403

**Commit:** `feat: child CRUD endpoints with integration tests`

---

### Task 4: Family GET + Invite Controller + Tests (TDD)

**Files:**
- Create: `backend/TinyHeroes.Application/DTOs/Family/FamilyDetailResponse.cs`
- Create: `backend/TinyHeroes.Application/DTOs/Family/FamilyMemberResponse.cs`
- Create: `backend/TinyHeroes.Application/DTOs/Invite/CreateInviteRequest.cs`
- Create: `backend/TinyHeroes.Application/DTOs/Invite/InviteResponse.cs`
- Create: `backend/TinyHeroes.Api/Controllers/InviteController.cs`
- Create: `backend/TinyHeroes.Tests/Integration/InviteControllerTests.cs`
- Modify: `backend/TinyHeroes.Api/Controllers/FamilyController.cs` — add GET endpoint

**New endpoints:**
- `GET /api/families/mine` → FamilyDetailResponse (name, weekStartDay, members list)
- `POST /api/invites` → InviteResponse (Admin only; email=null for shareable link)
- `POST /api/invites/{token}/accept` → joins family as CoParent

**Tests (7):**
1. GetMine_ReturnsFamily_WithMembers
2. GetMine_WhenNoFamily_Returns404
3. CreateInvite_WithEmail_ReturnsToken
4. CreateInvite_AsNonAdmin_Returns403
5. AcceptInvite_JoinsFamily
6. AcceptInvite_WhenExpired_Returns400
7. AcceptInvite_WhenAlreadyInFamily_Returns409

**Commit:** `feat: family detail endpoint + co-parent invite flow with tests`

---

### Task 5: Test Helper Refactor

**Files:**
- Create: `backend/TinyHeroes.Tests/Integration/Helpers/TestAuthHelper.cs`
- Modify: `backend/TinyHeroes.Tests/Integration/ChildControllerTests.cs` — use helper
- Modify: `backend/TinyHeroes.Tests/Integration/InviteControllerTests.cs` — use helper

**Purpose:** Extract repeated register+create-family+get-token pattern into a shared helper class:
```csharp
public static class TestAuthHelper
{
    public static async Task<(HttpClient Client, string Token)> RegisterWithFamily(...)
    public static async Task<(HttpClient Client, string Token)> RegisterOnly(...)
}
```

**Commit:** `refactor: extract shared test auth helper`

---

### Task 6: Bottom Nav + Shell Layout + Stub Routes

**Files:**
- Create: `frontend/src/app/shared/components/bottom-nav.component.ts`
- Create: `frontend/src/app/shared/components/shell.component.ts`
- Create: `frontend/src/app/features/podium/pages/podium.component.ts` (stub)
- Create: `frontend/src/app/features/prizes/pages/prizes.component.ts` (stub)
- Create: `frontend/src/app/features/settings/pages/settings.component.ts` (stub)
- Modify: `frontend/src/app/app.routes.ts` — restructure with shell parent route

**Route structure:**
```
/welcome, /login, /signup, /auth/callback, /create-family  (no shell)
/dashboard, /podium, /prizes, /settings/* (inside shell with bottom nav)
```

**ShellComponent:** `<router-outlet />` + `<app-bottom-nav />`

**BottomNav:** 4 tabs with emoji icons (🏠 🏆 🎁 ⚙️), routerLinkActive for orange highlight.

**Commit:** `feat: bottom navigation shell with stub routes`

---

### Task 7: Family & Child Services + Dashboard Page

**Files:**
- Create: `frontend/src/app/core/services/family.service.ts`
- Create: `frontend/src/app/core/services/child.service.ts`
- Create: `frontend/src/app/core/models/family.model.ts`
- Create: `frontend/src/app/core/models/child.model.ts`
- Modify: `frontend/src/app/features/dashboard/pages/home.component.ts` — full implementation

**Services:** Signal-based (same pattern as AuthService). FamilyService loads family detail; ChildService loads/creates/updates/deletes children.

**Dashboard UI:**
- Top bar: greeting + family name + avatar initial
- Week strip: Mon-Sun, current day highlighted
- "Your heroes" section: child cards (avatar, name, age, deeds=0, progress bar at 0%)
- Per-child `+` button (routes to add-deed — disabled/no-op for now)
- Empty state: "Add your first hero!" with link to /add-child
- FAB or "Add Hero" button in the hero section

**Commit:** `feat: dashboard with family/child services`

---

### Task 8: Add Child Page

**Files:**
- Create: `frontend/src/app/features/dashboard/pages/add-child.component.ts`
- Modify: `frontend/src/app/app.routes.ts` — add `add-child` route under shell

**UI:**
- Avatar picker: grid of 12 emoji avatars (🦸🦹🧙🧚🧜🦄🐉🦁🐯🦊🐼🐨), tap to select (orange border)
- Name input
- Age spinner: `−` button / number / `+` button (range 2-16)
- Gender selector: Boy / Girl toggle buttons
- "Add Hero" CTA button

**On submit:** Call childService.createChild(), navigate back to /dashboard.

**Commit:** `feat: add child page with avatar picker`

---

### Task 9: Child Profile + Invite Co-Parent Pages

**Files:**
- Create: `frontend/src/app/features/dashboard/pages/child-profile.component.ts`
- Create: `frontend/src/app/features/settings/pages/invite.component.ts`
- Modify: `frontend/src/app/app.routes.ts` — add `child/:id` and `settings/invite` routes
- Modify: `frontend/src/app/features/settings/pages/settings.component.ts` — link to invite page

**Child Profile:**
- Hero header: large avatar emoji, name, age, gender badge
- Three stat badges: "This week: 0", "All time: 0", "Wins: 0"
- Empty deed list: "No deeds yet!" message
- "Add Good Deed" button pinned at bottom (routes to future screen, shows toast or no-op)

**Invite Co-Parent:**
- Header: family name + 🏡
- Members list from familyService (name, role badge, "You" badge)
- Email input + "Send Invite" button
- OR separator
- "Copy Link" button (generates shareable invite, copies to clipboard)
- Note about co-parent permissions

**Commit:** `feat: child profile and invite co-parent pages`

---

### Task 10: i18n Strings + Final Verification

**Files:**
- Modify: `frontend/public/assets/i18n/en.json` — add NAV, DASHBOARD, CHILD, INVITE sections
- Modify: `frontend/public/assets/i18n/hu.json` — Hungarian translations

**Verification:**
1. `cd backend && dotnet test` — all tests pass
2. `cd frontend && npx ng build --configuration production` — 0 errors
3. Grep for hardcoded English strings in components — all externalized to i18n

**Commit:** `feat: i18n for Plan 2 screens — Plan 2 complete`

---

## Verification Checklist

- [x] Backend builds with 0 errors
- [x] All backend tests pass (5 existing + 14 new = 19 total)
- [x] Frontend builds with 0 errors (dev + prod configs)
- [x] Dashboard shows children list (or empty state)
- [x] Add Child flow: navigate → fill form → submit → appears on dashboard
- [x] Child Profile shows correct data
- [x] Invite flow: create invite → token generated
- [x] Accept invite: second user joins family as CoParent
- [x] Bottom nav works between all 4 tabs
- [x] All strings in i18n files (en + hu)
