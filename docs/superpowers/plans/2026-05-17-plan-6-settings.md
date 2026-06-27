# TinyHeroes — Plan 6: Settings & Profile

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add Family Settings (Screen 14) and My Profile (Screen 15) pages, replacing the "coming soon" stubs in the Settings hub. Parents can edit family name/week start day, manage co-parents, and update their own display name, language preference, and notification toggles.

**Architecture:** Two new backend controllers (extend FamilyController + new UserController) with PATCH/DELETE endpoints. Three new Angular pages (family-settings, profile) plus update to the existing Settings hub. Signal-based UserService mirrors the existing FamilyService pattern. Language switching delegates to TranslateService.

**Tech Stack:** ASP.NET Core 10, EF Core 10, ASP.NET Identity, Angular 21, Tailwind 4, ngx-translate

---

## Context

Plan 5 (Prizes Board, Prize Editor & Custom Prizes) is complete. We have 45 backend tests passing and all frontend pages for prizes. Plan 6 adds the Settings & Profile screens so parents can manage their family and user preferences.

**Design spec:** `docs/superpowers/screens/screens-settings.html` — Screens 14 and 15.

**Scope simplifications (YAGNI):**
- Email editing requires email-confirmation flow → deferred. Show email as read-only with "Edit" that shows a toast "Coming soon".
- Password change requires current-password verification → deferred. "Change" shows toast "Coming soon".
- Language picker: only `en` and `hu` are implemented (translation files exist). Show 🇬🇧 English and 🇭🇺 Magyar only.

**Important codebase notes:**
- `FamilyRole` is an enum (`Admin = 0`, `CoParent = 1`). Currently serializes as integer. Task 1 fixes FamilyMemberResponse.Role to return a string via `.ToString()` so the frontend sees `"Admin"` / `"CoParent"`.
- `User` entity has: `DisplayName`, `Email`, `PreferredLanguage` (default "en"), `PushNotificationsEnabled` (default true), `WeeklyEmailEnabled` (default false).
- Primary constructor DI pattern: `public class MyController(AppDbContext db, UserManager<User> um) : ControllerBase`
- `GetUserId()` helper (already in every controller): `private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);`
- Admin check via FamilyMember: `await db.FamilyMembers.AnyAsync(m => m.UserId == userId && m.FamilyId == family.Id && m.Role == FamilyRole.Admin)`

---

## Task Overview (6 Tasks)

| # | Task | Layer |
|---|------|-------|
| 1 | Family Settings backend (PATCH family, DELETE member, DELETE family) + tests | Backend |
| 2 | User Profile backend (GET + PATCH /api/users/me) + tests | Backend |
| 3 | Frontend models + services (UserService + extend FamilyService) | Frontend |
| 4 | Family Settings page (Screen 14) | Frontend |
| 5 | My Profile page (Screen 15) + Settings hub update | Frontend |
| 6 | i18n strings (en + hu) + final build verification | Frontend |

---

### Task 1: Family Settings Backend

**Files:**
- Modify: `backend/TinyHeroes.Application/DTOs/Family/FamilyDetailResponse.cs` — change Role field to string
- Modify: `backend/TinyHeroes.Api/Controllers/FamilyController.cs` — add PATCH, DELETE member, DELETE family
- Create: `backend/TinyHeroes.Tests/Integration/FamilySettingsControllerTests.cs`

**Step 1: Fix FamilyMemberResponse.Role to string**

Change the FamilyMemberResponse record in `FamilyDetailResponse.cs`:
```csharp
// Change from:
public record FamilyMemberResponse(Guid UserId, string DisplayName, string Email, FamilyRole Role);
// Change to:
public record FamilyMemberResponse(Guid UserId, string DisplayName, string Email, string Role);
```

In `FamilyController.GetMine()`, update the projection:
```csharp
var members = family.Members.Select(m => new FamilyMemberResponse(
    m.UserId, m.User.DisplayName, m.User.Email!, m.Role.ToString()
)).ToList();
```

**Step 2: Add DTOs for update**

Add to `backend/TinyHeroes.Application/DTOs/Family/FamilyDetailResponse.cs`:
```csharp
public record UpdateFamilyRequest(string Name, DayOfWeek WeekStartDay);
```

**Step 3: Add endpoints to FamilyController**

Add these three endpoints inside `FamilyController`:

```csharp
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

[HttpDelete("mine/members/{userId:guid}")]
public async Task<IActionResult> RemoveMember(Guid userId)
{
    var currentUserId = GetUserId();
    var currentMember = await db.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == currentUserId);
    if (currentMember is null) return BadRequest("User does not belong to a family.");
    if (currentMember.Role != FamilyRole.Admin) return Forbid();
    if (userId == currentUserId) return BadRequest("Cannot remove yourself from the family.");

    var targetMember = await db.FamilyMembers.FirstOrDefaultAsync(m =>
        m.UserId == userId && m.FamilyId == currentMember.FamilyId);
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
```

**Step 4: Write tests**

Create `backend/TinyHeroes.Tests/Integration/FamilySettingsControllerTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TinyHeroes.Application.DTOs.Family;
using TinyHeroes.Application.DTOs.Invite;
using TinyHeroes.Tests.Integration.Helpers;

namespace TinyHeroes.Tests.Integration;

public class FamilySettingsControllerTests(TestWebApplicationFactory<Program> factory)
    : IClassFixture<TestWebApplicationFactory<Program>>
{
    [Fact]
    public async Task UpdateFamily_AsAdmin_Succeeds()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);

        var response = await client.PatchAsJsonAsync("/api/families/mine",
            new UpdateFamilyRequest("Updated Name", DayOfWeek.Sunday));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<FamilyResponse>();
        result!.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task UpdateFamily_AsCoParent_Returns403()
    {
        var adminClient = await TestAuthHelper.RegisterWithFamily(factory);
        var inviteResponse = await adminClient.PostAsJsonAsync("/api/invites", new CreateInviteRequest("coparent@test.com"));
        var invite = await inviteResponse.Content.ReadFromJsonAsync<InviteResponse>();

        var coParentClient = await TestAuthHelper.RegisterOnly(factory);
        await coParentClient.PostAsync($"/api/invites/{invite!.Token}/accept", null);

        var response = await coParentClient.PatchAsJsonAsync("/api/families/mine",
            new UpdateFamilyRequest("Hacked Name", DayOfWeek.Monday));
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RemoveMember_AsAdmin_Succeeds()
    {
        var adminClient = await TestAuthHelper.RegisterWithFamily(factory);
        var inviteResponse = await adminClient.PostAsJsonAsync("/api/invites", new CreateInviteRequest("coparent2@test.com"));
        var invite = await inviteResponse.Content.ReadFromJsonAsync<InviteResponse>();

        var coParentClient = await TestAuthHelper.RegisterOnly(factory, "coparent2@test.com");
        var acceptResponse = await coParentClient.PostAsync($"/api/invites/{invite!.Token}/accept", null);
        acceptResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var acceptData = await acceptResponse.Content.ReadFromJsonAsync<dynamic>();

        // Get co-parent userId from family members list
        var familyResponse = await adminClient.GetAsync("/api/families/mine");
        var family = await familyResponse.Content.ReadFromJsonAsync<FamilyDetailResponse>();
        var coParentMember = family!.Members.First(m => m.Role == "CoParent");

        var removeResponse = await adminClient.DeleteAsync($"/api/families/mine/members/{coParentMember.UserId}");
        removeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterRemove = await adminClient.GetAsync("/api/families/mine");
        var afterFamily = await afterRemove.Content.ReadFromJsonAsync<FamilyDetailResponse>();
        afterFamily!.Members.Should().NotContain(m => m.UserId == coParentMember.UserId);
    }

    [Fact]
    public async Task RemoveMember_Self_Returns400()
    {
        var adminClient = await TestAuthHelper.RegisterWithFamily(factory);
        var family = await (await adminClient.GetAsync("/api/families/mine")).Content.ReadFromJsonAsync<FamilyDetailResponse>();
        var adminMember = family!.Members.First(m => m.Role == "Admin");

        var response = await adminClient.DeleteAsync($"/api/families/mine/members/{adminMember.UserId}");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteFamily_AsAdmin_Succeeds()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);

        var response = await client.DeleteAsync("/api/families/mine");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Family no longer exists
        var getResponse = await client.GetAsync("/api/families/mine");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteFamily_AsCoParent_Returns403()
    {
        var adminClient = await TestAuthHelper.RegisterWithFamily(factory);
        var inviteResponse = await adminClient.PostAsJsonAsync("/api/invites", new CreateInviteRequest("coparent3@test.com"));
        var invite = await inviteResponse.Content.ReadFromJsonAsync<InviteResponse>();

        var coParentClient = await TestAuthHelper.RegisterOnly(factory);
        await coParentClient.PostAsync($"/api/invites/{invite!.Token}/accept", null);

        var response = await coParentClient.DeleteAsync("/api/families/mine");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
```

**Step 5: Verify tests pass**

Run: `cd backend && dotnet test`
Expected: All tests pass (45 existing + 6 new = 51 total).

**Commit:** `feat: family settings endpoints (update, remove member, delete family) with tests`

---

### Task 2: User Profile Backend

**Files:**
- Create: `backend/TinyHeroes.Application/DTOs/User/UserDtos.cs`
- Create: `backend/TinyHeroes.Api/Controllers/UserController.cs`
- Create: `backend/TinyHeroes.Tests/Integration/UserControllerTests.cs`

**Step 1: Create DTOs**

Create `backend/TinyHeroes.Application/DTOs/User/UserDtos.cs`:
```csharp
namespace TinyHeroes.Application.DTOs.User;

public record UserProfileResponse(
    string UserId,
    string DisplayName,
    string Email,
    string PreferredLanguage,
    bool PushNotificationsEnabled,
    bool WeeklyEmailEnabled);

public record UpdateUserProfileRequest(
    string? DisplayName,
    string? PreferredLanguage,
    bool? PushNotificationsEnabled,
    bool? WeeklyEmailEnabled);
```

**Step 2: Create UserController**

Create `backend/TinyHeroes.Api/Controllers/UserController.cs`:
```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TinyHeroes.Application.DTOs.User;
using TinyHeroes.Domain.Entities;

namespace TinyHeroes.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UserController(UserManager<User> userManager) : ControllerBase
{
    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

    [HttpGet("me")]
    public async Task<ActionResult<UserProfileResponse>> GetMe()
    {
        var user = await userManager.FindByIdAsync(GetUserId().ToString());
        if (user is null) return NotFound();

        return Ok(new UserProfileResponse(
            user.Id.ToString(),
            user.DisplayName,
            user.Email!,
            user.PreferredLanguage,
            user.PushNotificationsEnabled,
            user.WeeklyEmailEnabled));
    }

    [HttpPatch("me")]
    public async Task<ActionResult<UserProfileResponse>> UpdateMe(UpdateUserProfileRequest req)
    {
        var user = await userManager.FindByIdAsync(GetUserId().ToString());
        if (user is null) return NotFound();

        if (req.DisplayName is not null) user.DisplayName = req.DisplayName;
        if (req.PreferredLanguage is not null) user.PreferredLanguage = req.PreferredLanguage;
        if (req.PushNotificationsEnabled.HasValue) user.PushNotificationsEnabled = req.PushNotificationsEnabled.Value;
        if (req.WeeklyEmailEnabled.HasValue) user.WeeklyEmailEnabled = req.WeeklyEmailEnabled.Value;

        await userManager.UpdateAsync(user);

        return Ok(new UserProfileResponse(
            user.Id.ToString(),
            user.DisplayName,
            user.Email!,
            user.PreferredLanguage,
            user.PushNotificationsEnabled,
            user.WeeklyEmailEnabled));
    }
}
```

**Step 3: Write tests**

Create `backend/TinyHeroes.Tests/Integration/UserControllerTests.cs`:
```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TinyHeroes.Application.DTOs.User;
using TinyHeroes.Tests.Integration.Helpers;

namespace TinyHeroes.Tests.Integration;

public class UserControllerTests(TestWebApplicationFactory<Program> factory)
    : IClassFixture<TestWebApplicationFactory<Program>>
{
    [Fact]
    public async Task GetProfile_ReturnsCurrentUser()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);

        var response = await client.GetAsync("/api/users/me");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        profile.Should().NotBeNull();
        profile!.DisplayName.Should().NotBeNullOrEmpty();
        profile.Email.Should().NotBeNullOrEmpty();
        profile.PreferredLanguage.Should().Be("en");
    }

    [Fact]
    public async Task UpdateProfile_ChangesDisplayName()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);

        var response = await client.PatchAsJsonAsync("/api/users/me",
            new UpdateUserProfileRequest("New Name", null, null, null));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        profile!.DisplayName.Should().Be("New Name");
    }

    [Fact]
    public async Task UpdateProfile_ChangesPreferences()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);

        var response = await client.PatchAsJsonAsync("/api/users/me",
            new UpdateUserProfileRequest(null, "hu", false, true));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        profile!.PreferredLanguage.Should().Be("hu");
        profile.PushNotificationsEnabled.Should().BeFalse();
        profile.WeeklyEmailEnabled.Should().BeTrue();
    }
}
```

**Step 4: Verify tests pass**

Run: `cd backend && dotnet test`
Expected: All tests pass (51 existing + 3 new = 54 total).

**Commit:** `feat: user profile endpoints (get, patch) with tests`

---

### Task 3: Frontend Models + Services

**Files:**
- Create: `frontend/src/app/core/models/user-profile.model.ts`
- Create: `frontend/src/app/core/services/user.service.ts`
- Modify: `frontend/src/app/core/services/family.service.ts`
- Modify: `frontend/src/app/core/models/family.model.ts`

**Step 1: Create user-profile model**

Create `frontend/src/app/core/models/user-profile.model.ts`:
```typescript
export interface UserProfile {
  userId: string;
  displayName: string;
  email: string;
  preferredLanguage: string;
  pushNotificationsEnabled: boolean;
  weeklyEmailEnabled: boolean;
}

export interface UpdateProfileRequest {
  displayName?: string;
  preferredLanguage?: string;
  pushNotificationsEnabled?: boolean;
  weeklyEmailEnabled?: boolean;
}
```

**Step 2: Create UserService**

Create `frontend/src/app/core/services/user.service.ts`:
```typescript
import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { UserProfile, UpdateProfileRequest } from '../models/user-profile.model';

@Injectable({ providedIn: 'root' })
export class UserService {
  private http = inject(HttpClient);
  private _profile = signal<UserProfile | null>(null);
  readonly profile = this._profile.asReadonly();

  loadProfile() {
    this.http.get<UserProfile>(`${environment.apiUrl}/users/me`).subscribe({
      next: (p) => this._profile.set(p),
      error: () => this._profile.set(null)
    });
  }

  updateProfile(req: UpdateProfileRequest) {
    return this.http.patch<UserProfile>(`${environment.apiUrl}/users/me`, req)
      .pipe(tap(p => this._profile.set(p)));
  }
}
```

**Step 3: Extend FamilyService**

Update `frontend/src/app/core/services/family.service.ts`:
```typescript
import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Family } from '../models/family.model';

@Injectable({ providedIn: 'root' })
export class FamilyService {
  private http = inject(HttpClient);
  private _family = signal<Family | null>(null);
  readonly family = this._family.asReadonly();

  loadFamily() {
    this.http.get<Family>(`${environment.apiUrl}/families/mine`).subscribe({
      next: (family) => this._family.set(family),
      error: () => this._family.set(null)
    });
  }

  updateFamily(name: string, weekStartDay: number) {
    return this.http.patch<Family>(`${environment.apiUrl}/families/mine`, { name, weekStartDay })
      .pipe(tap(f => this._family.set(f)));
  }

  removeMember(userId: string) {
    return this.http.delete(`${environment.apiUrl}/families/mine/members/${userId}`);
  }

  deleteFamily() {
    return this.http.delete(`${environment.apiUrl}/families/mine`);
  }
}
```

**Step 4: Update FamilyMember.role type**

In `frontend/src/app/core/models/family.model.ts`, ensure role is typed correctly (it now returns `"Admin"` or `"CoParent"` as a string from the updated backend):
```typescript
export interface Family {
  id: string;
  name: string;
  weekStartDay: number;
  members: FamilyMember[];
}

export interface FamilyMember {
  userId: string;
  displayName: string;
  email: string;
  role: string; // "Admin" or "CoParent"
}
```

**Commit:** `feat: user service, profile models, extended family service`

---

### Task 4: Family Settings Page (Screen 14)

**Files:**
- Create: `frontend/src/app/features/settings/pages/family-settings.component.ts`
- Modify: `frontend/src/app/app.routes.ts` — add `/settings/family` route

**Step 1: Add route**

In `frontend/src/app/app.routes.ts`, inside the shell children array (after `settings/invite`), add:
```typescript
{ path: 'settings/family', loadComponent: () => import('./features/settings/pages/family-settings.component').then(m => m.FamilySettingsComponent) },
```

**Step 2: Create Family Settings component**

Create `frontend/src/app/features/settings/pages/family-settings.component.ts`:
```typescript
import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { FamilyService } from '../../../core/services/family.service';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-family-settings',
  imports: [FormsModule, RouterLink, TranslateModule],
  template: `
    <div class="max-w-lg mx-auto p-4">
      <!-- Header -->
      <div class="flex items-center gap-2 mb-4">
        <button (click)="router.navigate(['/settings'])" class="text-brand-muted text-xl">←</button>
        <h1 class="text-lg font-bold text-brand-text">{{ 'SETTINGS.FAMILY_TITLE' | translate }}</h1>
      </div>

      @if (family()) {
        <!-- Family name -->
        <p class="text-xs font-bold text-brand-orange uppercase tracking-wide mb-1.5">{{ 'SETTINGS.FAMILY_NAME' | translate }}</p>
        <input [(ngModel)]="familyName" class="w-full bg-white border border-brand-border rounded-xl px-3 py-2.5 text-sm text-brand-text mb-4 focus:outline-none focus:ring-2 focus:ring-brand-orange" />

        <!-- Week starts on -->
        <p class="text-xs font-bold text-brand-orange uppercase tracking-wide mb-2">{{ 'SETTINGS.WEEK_STARTS' | translate }}</p>
        <div class="flex gap-2 flex-wrap mb-4">
          @for (day of days; track day.value) {
            <button (click)="selectedWeekStart = day.value"
              [class]="selectedWeekStart === day.value
                ? 'bg-brand-cream border-2 border-brand-orange text-brand-orange font-bold rounded-xl px-3 py-1.5 text-xs'
                : 'bg-white border border-brand-border text-brand-muted font-semibold rounded-xl px-3 py-1.5 text-xs hover:bg-brand-cream'">
              {{ day.label }}
            </button>
          }
        </div>

        <!-- Co-parents -->
        <p class="text-xs font-bold text-brand-orange uppercase tracking-wide mb-2">{{ 'SETTINGS.CO_PARENTS' | translate }}</p>
        <div class="bg-white rounded-xl border border-brand-border divide-y divide-brand-border mb-4">
          @for (member of family()!.members; track member.userId) {
            <div class="flex items-center gap-3 p-3">
              <div class="w-9 h-9 rounded-full bg-gradient-to-br from-brand-orange to-orange-400 flex items-center justify-center text-white text-sm font-bold flex-shrink-0">
                {{ member.displayName[0].toUpperCase() }}
              </div>
              <div class="flex-1">
                <p class="text-sm font-bold text-brand-text">{{ member.displayName }}</p>
                <p class="text-xs text-brand-muted">{{ member.role === 'Admin' ? ('SETTINGS.ADMIN_BADGE' | translate) : ('SETTINGS.COPARENT_BADGE' | translate) }}</p>
              </div>
              @if (member.userId === currentUserId()) {
                <span class="text-xs bg-green-100 text-green-700 px-2 py-0.5 rounded-full font-bold">{{ 'SETTINGS.YOU_BADGE' | translate }}</span>
              } @else if (isAdmin()) {
                <button (click)="removeMember(member.userId)" class="text-xs text-red-500 font-semibold hover:text-red-700">{{ 'SETTINGS.REMOVE' | translate }}</button>
              }
            </div>
          }
          <a routerLink="/settings/invite" class="flex items-center justify-center p-3 cursor-pointer">
            <span class="text-sm font-bold text-brand-orange">{{ 'SETTINGS.INVITE_ANOTHER' | translate }}</span>
          </a>
        </div>

        <!-- Save button -->
        <button (click)="save()" [disabled]="saving()" class="w-full bg-gradient-to-r from-brand-orange to-orange-400 text-white rounded-2xl py-3 text-sm font-bold mb-4 disabled:opacity-50">
          {{ saving() ? '...' : ('SETTINGS.SAVE_CHANGES' | translate) }}
        </button>

        <!-- Danger zone -->
        <div class="bg-red-50 rounded-xl border border-red-200 p-3">
          <p class="text-xs font-bold text-red-600 uppercase tracking-wide mb-2">⚠️ {{ 'SETTINGS.DANGER_ZONE' | translate }}</p>
          <p class="text-xs text-red-900 mb-3">{{ 'SETTINGS.DELETE_FAMILY_WARNING' | translate }}</p>
          <button (click)="deleteFamily()" class="w-full bg-white border border-red-400 text-red-500 rounded-xl py-2.5 text-xs font-bold hover:bg-red-50">
            {{ 'SETTINGS.DELETE_FAMILY' | translate }}
          </button>
        </div>
      } @else {
        <p class="text-sm text-brand-muted text-center py-8">Loading...</p>
      }
    </div>
  `
})
export class FamilySettingsComponent implements OnInit {
  protected router = inject(Router);
  private familyService = inject(FamilyService);
  private authService = inject(AuthService);

  family = this.familyService.family;
  currentUserId = computed(() => this.authService.user()?.userId ?? '');
  isAdmin = computed(() => {
    const family = this.family();
    const userId = this.currentUserId();
    return family?.members.some(m => m.userId === userId && m.role === 'Admin') ?? false;
  });

  familyName = '';
  selectedWeekStart = 1; // Monday
  saving = signal(false);

  days = [
    { value: 1, label: 'Mon' }, { value: 2, label: 'Tue' }, { value: 3, label: 'Wed' },
    { value: 4, label: 'Thu' }, { value: 5, label: 'Fri' }, { value: 6, label: 'Sat' },
    { value: 0, label: 'Sun' },
  ];

  ngOnInit() {
    this.familyService.loadFamily();
    const f = this.family();
    if (f) {
      this.familyName = f.name;
      this.selectedWeekStart = f.weekStartDay;
    }
    // also set after load
    this.familyService.family;
  }

  ngOnChanges() {
    const f = this.family();
    if (f && !this.familyName) {
      this.familyName = f.name;
      this.selectedWeekStart = f.weekStartDay;
    }
  }

  save() {
    this.saving.set(true);
    this.familyService.updateFamily(this.familyName, this.selectedWeekStart).subscribe({
      next: () => {
        this.saving.set(false);
        this.router.navigate(['/settings']);
      },
      error: () => this.saving.set(false)
    });
  }

  removeMember(userId: string) {
    if (!confirm('Remove this co-parent from the family?')) return;
    this.familyService.removeMember(userId).subscribe({
      next: () => this.familyService.loadFamily()
    });
  }

  deleteFamily() {
    if (!confirm('Delete the entire family? This cannot be undone.')) return;
    this.familyService.deleteFamily().subscribe({
      next: () => {
        localStorage.removeItem('th_access_token');
        this.router.navigate(['/']);
      }
    });
  }
}
```

**Note on form pre-fill:** Because signal reads happen before loadFamily() completes, the component must initialize `familyName` and `selectedWeekStart` from an `effect()` or after-load callback. The cleanest approach: in `ngOnInit`, after calling `loadFamily()`, subscribe to the result and set form fields. Rewrite the FamilySettingsComponent's `ngOnInit` to use the observable returned by loadFamily if possible, or use Angular `effect()`:

Actually the simplest fix: use an `effect()` in the constructor to reactively sync the form fields when the family signal changes:

```typescript
constructor() {
  effect(() => {
    const f = this.family();
    if (f && !this.familyName) {
      this.familyName = f.name;
      this.selectedWeekStart = f.weekStartDay;
    }
  });
}
```

Remove `ngOnChanges()` and replace the `ngOnInit` form-initialization code with the above `effect()` approach. Keep `ngOnInit` for just calling `loadFamily()`.

**Commit:** `feat: family settings page`

---

### Task 5: My Profile Page + Settings Hub Update

**Files:**
- Create: `frontend/src/app/features/settings/pages/profile.component.ts`
- Modify: `frontend/src/app/features/settings/pages/settings.component.ts`
- Modify: `frontend/src/app/app.routes.ts` — add `/settings/profile` route

**Step 1: Add route**

In `frontend/src/app/app.routes.ts`, inside the shell children array (after `settings/family`), add:
```typescript
{ path: 'settings/profile', loadComponent: () => import('./features/settings/pages/profile.component').then(m => m.ProfileComponent) },
```

**Step 2: Create Profile component**

Create `frontend/src/app/features/settings/pages/profile.component.ts`:
```typescript
import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { UserService } from '../../../core/services/user.service';
import { AuthService } from '../../../core/auth/auth.service';
import { FamilyService } from '../../../core/services/family.service';

@Component({
  selector: 'app-profile',
  imports: [FormsModule, TranslateModule],
  template: `
    <div class="max-w-lg mx-auto p-4">
      <!-- Header -->
      <div class="flex items-center gap-2 mb-4">
        <button (click)="router.navigate(['/settings'])" class="text-brand-muted text-xl">←</button>
        <h1 class="text-lg font-bold text-brand-text">{{ 'SETTINGS.PROFILE_TITLE' | translate }}</h1>
      </div>

      @if (profile()) {
        <!-- Avatar -->
        <div class="text-center mb-5">
          <div class="w-16 h-16 rounded-full bg-gradient-to-br from-brand-orange to-orange-400 inline-flex items-center justify-center text-white text-2xl font-bold border-4 border-orange-200 mb-2">
            {{ profile()!.displayName[0].toUpperCase() }}
          </div>
          <p class="text-base font-bold text-brand-text">{{ profile()!.displayName }}</p>
          <p class="text-xs text-brand-muted">{{ familyRole() }} · {{ familyName() }}</p>
        </div>

        <!-- Account section -->
        <p class="text-xs font-bold text-brand-muted uppercase tracking-wide mb-2">{{ 'SETTINGS.ACCOUNT' | translate }}</p>
        <div class="bg-white rounded-xl border border-brand-border divide-y divide-brand-border mb-4">
          <!-- Full name row -->
          <div>
            <div class="flex items-center gap-3 p-3">
              <span class="text-base">👤</span>
              <div class="flex-1">
                <p class="text-xs text-brand-muted">{{ 'SETTINGS.FULL_NAME' | translate }}</p>
                @if (!editingName()) {
                  <p class="text-sm font-bold text-brand-text">{{ profile()!.displayName }}</p>
                } @else {
                  <input [(ngModel)]="nameInput" class="w-full border border-brand-border rounded-lg px-2 py-1 text-sm text-brand-text mt-0.5 focus:outline-none focus:ring-1 focus:ring-brand-orange" />
                }
              </div>
              @if (!editingName()) {
                <button (click)="editingName.set(true)" class="text-xs text-brand-orange font-semibold">{{ 'SHARED.EDIT' | translate }}</button>
              } @else {
                <div class="flex gap-2">
                  <button (click)="saveName()" class="text-xs text-brand-orange font-bold">{{ 'PRIZES.SAVE' | translate }}</button>
                  <button (click)="editingName.set(false)" class="text-xs text-brand-muted">{{ 'PRIZES.CANCEL' | translate }}</button>
                </div>
              }
            </div>
          </div>
          <!-- Email row (read-only) -->
          <div class="flex items-center gap-3 p-3">
            <span class="text-base">📧</span>
            <div class="flex-1">
              <p class="text-xs text-brand-muted">{{ 'SETTINGS.EMAIL' | translate }}</p>
              <p class="text-sm font-bold text-brand-text">{{ profile()!.email }}</p>
            </div>
            <button (click)="showComingSoon()" class="text-xs text-brand-orange font-semibold opacity-40">{{ 'SHARED.EDIT' | translate }}</button>
          </div>
          <!-- Password row -->
          <div class="flex items-center gap-3 p-3">
            <span class="text-base">🔒</span>
            <div class="flex-1">
              <p class="text-xs text-brand-muted">{{ 'SETTINGS.PASSWORD' | translate }}</p>
              <p class="text-sm font-bold text-brand-text">••••••••</p>
            </div>
            <button (click)="showComingSoon()" class="text-xs text-brand-orange font-semibold opacity-40">{{ 'SETTINGS.CHANGE_PASSWORD' | translate }}</button>
          </div>
        </div>

        <!-- Preferences section -->
        <p class="text-xs font-bold text-brand-muted uppercase tracking-wide mb-2">{{ 'SETTINGS.PREFERENCES' | translate }}</p>
        <div class="bg-white rounded-xl border border-brand-border divide-y divide-brand-border mb-4">
          <!-- Language row -->
          <div>
            <div class="flex items-center gap-3 p-3 cursor-pointer" (click)="showLangPicker.set(!showLangPicker())">
              <span class="text-base">🌍</span>
              <div class="flex-1">
                <p class="text-xs text-brand-muted">{{ 'SETTINGS.LANGUAGE' | translate }}</p>
                <p class="text-sm font-bold text-brand-text">{{ currentLangLabel() }}</p>
              </div>
              <span class="text-brand-muted text-xs">{{ showLangPicker() ? '▲' : 'Change' }}</span>
            </div>
            @if (showLangPicker()) {
              <div class="border-t border-brand-border">
                <div class="px-3 py-2 bg-brand-cream text-xs font-bold text-brand-muted">{{ 'SETTINGS.SELECT_LANGUAGE' | translate }}</div>
                @for (lang of languages; track lang.code) {
                  <div (click)="selectLanguage(lang.code)"
                    class="flex items-center justify-between px-3 py-2.5 border-b border-brand-border last:border-0 cursor-pointer hover:bg-brand-cream">
                    <span class="text-sm text-brand-text">{{ lang.flag }} {{ lang.name }}</span>
                    @if (profile()!.preferredLanguage === lang.code) {
                      <span class="text-brand-orange text-sm">✓</span>
                    }
                  </div>
                }
              </div>
            }
          </div>
          <!-- Push notifications toggle -->
          <div class="flex items-center gap-3 p-3">
            <span class="text-base">🔔</span>
            <div class="flex-1">
              <p class="text-sm font-bold text-brand-text">{{ 'SETTINGS.PUSH_NOTIFICATIONS' | translate }}</p>
              <p class="text-xs text-brand-muted">{{ 'SETTINGS.PUSH_NOTIFICATIONS_DESC' | translate }}</p>
            </div>
            <button (click)="togglePush()" class="flex-shrink-0">
              <div [class]="profile()!.pushNotificationsEnabled
                ? 'w-10 h-6 bg-brand-orange rounded-full relative transition-colors'
                : 'w-10 h-6 bg-gray-300 rounded-full relative transition-colors'">
                <div [class]="profile()!.pushNotificationsEnabled
                  ? 'w-4 h-4 bg-white rounded-full absolute right-1 top-1 transition-transform'
                  : 'w-4 h-4 bg-white rounded-full absolute left-1 top-1 transition-transform'"></div>
              </div>
            </button>
          </div>
          <!-- Weekly email toggle -->
          <div class="flex items-center gap-3 p-3">
            <span class="text-base">📊</span>
            <div class="flex-1">
              <p class="text-sm font-bold text-brand-text">{{ 'SETTINGS.WEEKLY_EMAIL' | translate }}</p>
              <p class="text-xs text-brand-muted">{{ 'SETTINGS.WEEKLY_EMAIL_DESC' | translate }}</p>
            </div>
            <button (click)="toggleWeeklyEmail()" class="flex-shrink-0">
              <div [class]="profile()!.weeklyEmailEnabled
                ? 'w-10 h-6 bg-brand-orange rounded-full relative transition-colors'
                : 'w-10 h-6 bg-gray-300 rounded-full relative transition-colors'">
                <div [class]="profile()!.weeklyEmailEnabled
                  ? 'w-4 h-4 bg-white rounded-full absolute right-1 top-1 transition-transform'
                  : 'w-4 h-4 bg-white rounded-full absolute left-1 top-1 transition-transform'"></div>
              </div>
            </button>
          </div>
        </div>

        <!-- Sign out -->
        <button (click)="signOut()" class="w-full bg-white border border-brand-border text-red-500 rounded-2xl py-3 text-sm font-bold">
          {{ 'SETTINGS.SIGN_OUT' | translate }}
        </button>
      } @else {
        <p class="text-sm text-brand-muted text-center py-8">Loading...</p>
      }
    </div>
  `
})
export class ProfileComponent implements OnInit {
  protected router = inject(Router);
  private userService = inject(UserService);
  private authService = inject(AuthService);
  private familyService = inject(FamilyService);
  private translateService = inject(TranslateService);

  profile = this.userService.profile;
  editingName = signal(false);
  nameInput = '';
  showLangPicker = signal(false);

  languages = [
    { code: 'en', flag: '🇬🇧', name: 'English' },
    { code: 'hu', flag: '🇭🇺', name: 'Magyar' },
  ];

  currentLangLabel = computed(() => {
    const lang = this.profile()?.preferredLanguage ?? 'en';
    return this.languages.find(l => l.code === lang)?.name ?? 'English';
  });

  familyName = computed(() => this.familyService.family()?.name ?? '');
  familyRole = computed(() => {
    const userId = this.authService.user()?.userId;
    const member = this.familyService.family()?.members.find(m => m.userId === userId);
    return member?.role ?? '';
  });

  ngOnInit() {
    this.userService.loadProfile();
    this.familyService.loadFamily();
  }

  saveName() {
    if (!this.nameInput.trim()) return;
    this.userService.updateProfile({ displayName: this.nameInput.trim() }).subscribe({
      next: () => this.editingName.set(false)
    });
  }

  selectLanguage(code: string) {
    this.translateService.use(code);
    this.userService.updateProfile({ preferredLanguage: code }).subscribe();
    this.showLangPicker.set(false);
  }

  togglePush() {
    const current = this.profile()?.pushNotificationsEnabled ?? true;
    this.userService.updateProfile({ pushNotificationsEnabled: !current }).subscribe();
  }

  toggleWeeklyEmail() {
    const current = this.profile()?.weeklyEmailEnabled ?? false;
    this.userService.updateProfile({ weeklyEmailEnabled: !current }).subscribe();
  }

  signOut() {
    this.authService.logout();
  }

  showComingSoon() {
    alert('Coming soon!');
  }
}
```

**Step 3: Update Settings hub component**

Replace the content of `frontend/src/app/features/settings/pages/settings.component.ts`:
```typescript
import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-settings',
  imports: [RouterLink, TranslateModule],
  template: `
    <div class="p-4 max-w-lg mx-auto">
      <h1 class="text-2xl font-bold text-brand-text mb-6">{{ 'SETTINGS.TITLE' | translate }}</h1>
      <div class="space-y-3">
        <a routerLink="/settings/invite" class="block bg-white rounded-xl border border-brand-border p-4 hover:bg-brand-cream transition-colors">
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-3">
              <span class="text-xl">👥</span>
              <span class="font-medium text-brand-text">{{ 'SETTINGS.INVITE_COPARENT' | translate }}</span>
            </div>
            <span class="text-brand-muted">→</span>
          </div>
        </a>
        <a routerLink="/settings/profile" class="block bg-white rounded-xl border border-brand-border p-4 hover:bg-brand-cream transition-colors">
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-3">
              <span class="text-xl">👤</span>
              <span class="font-medium text-brand-text">{{ 'SETTINGS.MY_PROFILE' | translate }}</span>
            </div>
            <span class="text-brand-muted">→</span>
          </div>
        </a>
        <a routerLink="/settings/family" class="block bg-white rounded-xl border border-brand-border p-4 hover:bg-brand-cream transition-colors">
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-3">
              <span class="text-xl">🏠</span>
              <span class="font-medium text-brand-text">{{ 'SETTINGS.FAMILY_SETTINGS' | translate }}</span>
            </div>
            <span class="text-brand-muted">→</span>
          </div>
        </a>
      </div>
    </div>
  `
})
export class SettingsComponent {}
```

**Commit:** `feat: profile and family settings pages, settings hub links`

---

### Task 6: i18n Strings + Final Build Verification

**Files:**
- Modify: `frontend/public/assets/i18n/en.json` — expand SETTINGS section, add SHARED section
- Modify: `frontend/public/assets/i18n/hu.json` — Hungarian translations

**Step 1: Update en.json**

Replace the existing `"SETTINGS"` section and add a `"SHARED"` section:

```json
"SHARED": {
  "EDIT": "Edit"
},
"SETTINGS": {
  "TITLE": "Settings",
  "INVITE_COPARENT": "Invite Co-Parent",
  "MY_PROFILE": "My Profile",
  "FAMILY_SETTINGS": "Family Settings",
  "PROFILE_TITLE": "My Profile",
  "ACCOUNT": "Account",
  "PREFERENCES": "Preferences",
  "FULL_NAME": "Full name",
  "EMAIL": "Email",
  "PASSWORD": "Password",
  "CHANGE_PASSWORD": "Change",
  "LANGUAGE": "Language",
  "PUSH_NOTIFICATIONS": "Push notifications",
  "PUSH_NOTIFICATIONS_DESC": "Weekly summary & reminders",
  "WEEKLY_EMAIL": "Weekly email report",
  "WEEKLY_EMAIL_DESC": "Sent every Sunday",
  "SIGN_OUT": "Sign Out",
  "SELECT_LANGUAGE": "Select language",
  "FAMILY_TITLE": "Family Settings",
  "FAMILY_NAME": "Family name",
  "WEEK_STARTS": "Week starts on",
  "CO_PARENTS": "Co-parents",
  "REMOVE": "Remove",
  "INVITE_ANOTHER": "+ Invite another co-parent",
  "SAVE_CHANGES": "Save Changes",
  "DANGER_ZONE": "Danger zone",
  "DELETE_FAMILY_WARNING": "Deleting the family removes all children, deeds, and history permanently.",
  "DELETE_FAMILY": "Delete Family",
  "YOU_BADGE": "You",
  "ADMIN_BADGE": "Admin",
  "COPARENT_BADGE": "Co-parent"
}
```

**Step 2: Update hu.json**

Replace the existing `"SETTINGS"` section and add `"SHARED"`:
```json
"SHARED": {
  "EDIT": "Szerkesztés"
},
"SETTINGS": {
  "TITLE": "Beállítások",
  "INVITE_COPARENT": "Szülőtárs meghívása",
  "MY_PROFILE": "Profilom",
  "FAMILY_SETTINGS": "Család beállítások",
  "PROFILE_TITLE": "Profilom",
  "ACCOUNT": "Fiók",
  "PREFERENCES": "Preferenciák",
  "FULL_NAME": "Teljes név",
  "EMAIL": "E-mail",
  "PASSWORD": "Jelszó",
  "CHANGE_PASSWORD": "Módosítás",
  "LANGUAGE": "Nyelv",
  "PUSH_NOTIFICATIONS": "Push értesítések",
  "PUSH_NOTIFICATIONS_DESC": "Heti összefoglaló és emlékeztetők",
  "WEEKLY_EMAIL": "Heti e-mail riport",
  "WEEKLY_EMAIL_DESC": "Minden vasárnap elküldve",
  "SIGN_OUT": "Kijelentkezés",
  "SELECT_LANGUAGE": "Válassz nyelvet",
  "FAMILY_TITLE": "Család beállítások",
  "FAMILY_NAME": "Család neve",
  "WEEK_STARTS": "A hét kezdőnapja",
  "CO_PARENTS": "Szülőtársak",
  "REMOVE": "Eltávolítás",
  "INVITE_ANOTHER": "+ Másik szülőtárs meghívása",
  "SAVE_CHANGES": "Változtatások mentése",
  "DANGER_ZONE": "Veszélyes zóna",
  "DELETE_FAMILY_WARNING": "A család törlése az összes gyermeket, cselekedetet és előzményt véglegesen eltávolítja.",
  "DELETE_FAMILY": "Család törlése",
  "YOU_BADGE": "Te",
  "ADMIN_BADGE": "Admin",
  "COPARENT_BADGE": "Szülőtárs"
}
```

**Step 3: Verify backend tests**

Run: `cd backend && dotnet test`
Expected: All tests pass (54 total).

**Step 4: Verify frontend production build**

Run: `cd frontend && npx ng build --configuration production`
Expected: 0 errors, 0 warnings about missing translations.

**Commit:** `feat: i18n for Plan 6 screens — Plan 6 complete`

---

## Verification Checklist

- [ ] Backend builds with 0 errors
- [ ] All backend tests pass (54 total: 45 from Plans 1-5 + 6 family-settings + 3 user-profile)
- [ ] Frontend builds with 0 errors (prod config)
- [ ] FamilyMemberResponse.Role returns "Admin" or "CoParent" (string, not integer)
- [ ] PATCH /api/families/mine updates name and week start day (Admin only)
- [ ] DELETE /api/families/mine/members/{userId} removes co-parent (Admin only, cannot remove self)
- [ ] DELETE /api/families/mine deletes family (Admin only)
- [ ] GET /api/users/me returns profile with all preference fields
- [ ] PATCH /api/users/me updates displayName, language, notification flags
- [ ] Settings hub links to My Profile and Family Settings (no more "coming soon")
- [ ] Family Settings page: edit name, pick week start day, see members, remove co-parent
- [ ] Family Settings page: Save Changes calls PATCH, navigates back to /settings
- [ ] Family Settings page: Delete Family calls DELETE, redirects to /
- [ ] My Profile page: shows name/email/role/family
- [ ] My Profile page: inline name edit via PATCH
- [ ] My Profile page: language picker (en/hu), changes app language immediately via TranslateService
- [ ] My Profile page: push notifications and weekly email toggles persist via PATCH
- [ ] My Profile page: Sign Out clears token and navigates to /
- [ ] All new strings in i18n files (en + hu)
