# Theme Selection Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let each user pick from four visual themes (Sunny, Ocean, Forest, Candy) that swap the app's color palette and font; preference is stored per user and restored on every app open.

**Architecture:** CSS variable overrides on `[data-theme]` body attribute exploit Tailwind v4's token-as-custom-property design — zero template changes needed. A new `ThemeService` owns the signal + localStorage + body attribute; `UserService` syncs the backend value in its existing `loadProfile()` success handler (same pattern as `preferredLanguage`). Backend adds one field (`PreferredTheme`) to `User` and threads it through the existing `PATCH /api/users/me` endpoint.

**Tech Stack:** Angular 19 (signals, standalone), Tailwind v4, ASP.NET Core Identity (`UserManager<User>`), EF Core (SQLite in dev), xUnit integration tests, Vitest + Angular TestBed for frontend.

---

## File Map

| File | Change |
|---|---|
| `backend/TinyHeroes.Domain/Entities/User.cs` | Add `PreferredTheme` property |
| `backend/TinyHeroes.Application/DTOs/User/UserDtos.cs` | Add `PreferredTheme` to both records |
| `backend/TinyHeroes.Api/Controllers/UserController.cs` | Apply `PreferredTheme` in `UpdateMe` and return it in `GetMe` |
| `backend/TinyHeroes.Infrastructure/Migrations/` | New EF migration (generated, not hand-written) |
| `backend/TinyHeroes.Tests/Integration/UserControllerTests.cs` | Add `PreferredTheme` test |
| `frontend/src/index.html` | Add Google Fonts `<link>` tags |
| `frontend/src/styles.css` | Add three `[data-theme]` override blocks |
| `frontend/src/app/core/services/theme.service.ts` | **Create** — signal, `init()`, `apply()` |
| `frontend/src/app/core/services/theme.service.spec.ts` | **Create** — unit tests |
| `frontend/src/app/core/models/user-profile.model.ts` | Add `preferredTheme` to both interfaces |
| `frontend/src/app/core/services/user.service.ts` | Inject `ThemeService`, call `themeService.apply()` in `loadProfile()` success |
| `frontend/src/app/app.ts` | Call `themeService.init()` in `ngOnInit` |
| `frontend/src/app/features/settings/pages/profile.component.ts` | Add theme picker row to Preferences section |
| `frontend/public/assets/i18n/en.json` | Add `SETTINGS.THEME`, `SETTINGS.SELECT_THEME` keys |
| `frontend/public/assets/i18n/hu.json` | Same keys (Hungarian) |
| `frontend/public/assets/i18n/de.json` | Same keys (German) |
| `frontend/public/assets/i18n/fr.json` | Same keys (French) |
| `frontend/public/assets/i18n/es.json` | Same keys (Spanish) |
| `CHANGELOG.md` | Add Unreleased entry |
| `frontend/src/app/features/help/help.component.ts` | Update if Settings section exists |

---

## Task 1: Backend — Add `PreferredTheme` to domain + DTOs

**Files:**
- Modify: `backend/TinyHeroes.Domain/Entities/User.cs`
- Modify: `backend/TinyHeroes.Application/DTOs/User/UserDtos.cs`
- Modify: `backend/TinyHeroes.Api/Controllers/UserController.cs`

- [ ] **Step 1: Add property to User entity**

In `backend/TinyHeroes.Domain/Entities/User.cs`, add after `PreferredLanguage`:

```csharp
public string PreferredTheme { get; set; } = "sunny";
```

- [ ] **Step 2: Add to DTOs**

Replace the contents of `backend/TinyHeroes.Application/DTOs/User/UserDtos.cs` with:

```csharp
namespace TinyHeroes.Application.DTOs.User;

public record UserProfileResponse(
    string UserId,
    string DisplayName,
    string Email,
    string PreferredLanguage,
    bool PushNotificationsEnabled,
    bool WeeklyEmailEnabled,
    string PreferredTheme);

public record UpdateUserProfileRequest(
    string? DisplayName,
    string? PreferredLanguage,
    bool? PushNotificationsEnabled,
    bool? WeeklyEmailEnabled,
    string? PreferredTheme);
```

- [ ] **Step 3: Apply in controller**

In `backend/TinyHeroes.Api/Controllers/UserController.cs`, add after the `WeeklyEmailEnabled` line in `UpdateMe`:

```csharp
if (req.PreferredTheme is not null) user.PreferredTheme = req.PreferredTheme;
```

Update both `new UserProfileResponse(...)` calls (in `GetMe` and `UpdateMe`) to include `user.PreferredTheme` as the last argument:

```csharp
return Ok(new UserProfileResponse(
    user.Id.ToString(),
    user.DisplayName,
    user.Email!,
    user.PreferredLanguage,
    user.PushNotificationsEnabled,
    user.WeeklyEmailEnabled,
    user.PreferredTheme));
```

- [ ] **Step 4: Verify build compiles**

```bash
cd backend && dotnet build
```

Expected: `Build succeeded. 0 Warning(s). 0 Error(s).`

- [ ] **Step 5: Commit**

```bash
git add backend/TinyHeroes.Domain/Entities/User.cs \
        backend/TinyHeroes.Application/DTOs/User/UserDtos.cs \
        backend/TinyHeroes.Api/Controllers/UserController.cs
git commit -m "feat: add PreferredTheme field to user domain and DTOs"
```

---

## Task 2: Backend — EF Core migration

**Files:**
- Create: `backend/TinyHeroes.Infrastructure/Migrations/<timestamp>_AddUserPreferredTheme.cs` (generated)

- [ ] **Step 1: Generate migration**

```bash
cd backend && dotnet ef migrations add AddUserPreferredTheme \
  --project TinyHeroes.Infrastructure \
  --startup-project TinyHeroes.Api
```

Expected output ends with: `Done. To undo this action, use 'ef migrations remove'`

- [ ] **Step 2: Verify migration content**

Open the generated migration file (path will be printed by the previous command). Confirm it contains:

```csharp
migrationBuilder.AddColumn<string>(
    name: "PreferredTheme",
    table: "AspNetUsers",
    ...
    defaultValue: "sunny");
```

If `defaultValue` is missing, add it manually to ensure existing rows default to `"sunny"`.

- [ ] **Step 3: Apply migration and verify tests pass**

```bash
cd backend && dotnet test
```

Expected: all existing tests pass (the migration is applied to the test database automatically via `TestWebApplicationFactory`).

- [ ] **Step 4: Commit**

```bash
git add backend/TinyHeroes.Infrastructure/Migrations/
git commit -m "feat: migrate AddUserPreferredTheme"
```

---

## Task 3: Backend — Integration test for `PreferredTheme`

**Files:**
- Modify: `backend/TinyHeroes.Tests/Integration/UserControllerTests.cs`

- [ ] **Step 1: Write the failing test**

Add to `UserControllerTests.cs` (inside the class, after existing tests):

```csharp
[Fact]
public async Task UpdateProfile_ChangesPreferredTheme()
{
    var client = await TestAuthHelper.RegisterWithFamily(factory);

    var response = await client.PatchAsJsonAsync("/api/users/me",
        new UpdateUserProfileRequest(null, null, null, null, "ocean"));
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    var profile = await response.Content.ReadFromJsonAsync<UserProfileResponse>(TestWebApplicationFactory<Program>.JsonOptions);
    profile!.PreferredTheme.Should().Be("ocean");
}

[Fact]
public async Task GetProfile_ReturnsDefaultTheme()
{
    var client = await TestAuthHelper.RegisterWithFamily(factory);

    var response = await client.GetAsync("/api/users/me");
    var profile = await response.Content.ReadFromJsonAsync<UserProfileResponse>(TestWebApplicationFactory<Program>.JsonOptions);
    profile!.PreferredTheme.Should().Be("sunny");
}
```

- [ ] **Step 2: Run the new tests**

```bash
cd backend && dotnet test --filter "FullyQualifiedName~UserControllerTests"
```

Expected: both new tests pass (the domain + migration work is already done).

- [ ] **Step 3: Commit**

```bash
git add backend/TinyHeroes.Tests/Integration/UserControllerTests.cs
git commit -m "test: verify PreferredTheme round-trip via PATCH /api/users/me"
```

---

## Task 4: Frontend — Google Fonts + CSS theme overrides

**Files:**
- Modify: `frontend/src/index.html`
- Modify: `frontend/src/styles.css`

- [ ] **Step 1: Add Google Fonts to index.html**

In `frontend/src/index.html`, add inside `<head>` before `</head>`:

```html
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Baloo+2:wght@400;600;700&family=Nunito:wght@400;600;700&family=Quicksand:wght@400;600;700&display=swap" rel="stylesheet">
```

- [ ] **Step 2: Add theme override blocks to styles.css**

Append to the end of `frontend/src/styles.css`:

```css
[data-theme="ocean"] {
  --color-brand-orange: #0EA5E9;
  --color-brand-green:  #14B8A6;
  --color-brand-purple: #818CF8;
  --color-brand-cream:  #BAE6FD;
  --color-brand-bg:     #E0F7FA;
  --color-brand-border: #BAE6FD;
  --color-brand-text:   #0C4A6E;
  --color-brand-muted:  #0369A1;
  --font-sans: 'Nunito', sans-serif;
}

[data-theme="forest"] {
  --color-brand-orange: #16A34A;
  --color-brand-green:  #D97706;
  --color-brand-purple: #7C3AED;
  --color-brand-cream:  #DCFCE7;
  --color-brand-bg:     #F0FDF4;
  --color-brand-border: #BBF7D0;
  --color-brand-text:   #14532D;
  --color-brand-muted:  #166534;
  --font-sans: 'Quicksand', sans-serif;
}

[data-theme="candy"] {
  --color-brand-orange: #EC4899;
  --color-brand-green:  #8B5CF6;
  --color-brand-purple: #06B6D4;
  --color-brand-cream:  #FCE7F3;
  --color-brand-bg:     #FDF2F8;
  --color-brand-border: #FBCFE8;
  --color-brand-text:   #831843;
  --color-brand-muted:  #9D174D;
  --font-sans: 'Baloo 2', sans-serif;
}
```

Also update the `body` rule to use the CSS variable so it tracks theme changes:

```css
body { background-color: var(--color-brand-bg); }
```

- [ ] **Step 3: Commit**

```bash
git add frontend/src/index.html frontend/src/styles.css
git commit -m "feat: add theme CSS variable overrides and Google Fonts"
```

---

## Task 5: Frontend — `ThemeService`

**Files:**
- Create: `frontend/src/app/core/services/theme.service.ts`
- Create: `frontend/src/app/core/services/theme.service.spec.ts`

- [ ] **Step 1: Write the failing tests**

Create `frontend/src/app/core/services/theme.service.spec.ts`:

```typescript
import { TestBed } from '@angular/core/testing';
import { ThemeService } from './theme.service';

describe('ThemeService', () => {
  let service: ThemeService;

  beforeEach(() => {
    localStorage.clear();
    document.body.removeAttribute('data-theme');
    TestBed.configureTestingModule({});
    service = TestBed.inject(ThemeService);
  });

  it('init() defaults to sunny when localStorage is empty', () => {
    service.init();
    expect(document.body.getAttribute('data-theme')).toBe('sunny');
    expect(service.current()).toBe('sunny');
  });

  it('init() restores theme from localStorage', () => {
    localStorage.setItem('th_theme', 'ocean');
    service.init();
    expect(document.body.getAttribute('data-theme')).toBe('ocean');
    expect(service.current()).toBe('ocean');
  });

  it('apply() updates signal, localStorage, and data-theme attribute', () => {
    service.apply('forest');
    expect(service.current()).toBe('forest');
    expect(localStorage.getItem('th_theme')).toBe('forest');
    expect(document.body.getAttribute('data-theme')).toBe('forest');
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
cd frontend && npx ng test --watch=false --include="**/theme.service.spec.ts"
```

Expected: FAIL — `ThemeService` does not exist yet.

- [ ] **Step 3: Implement ThemeService**

Create `frontend/src/app/core/services/theme.service.ts`:

```typescript
import { Injectable, signal } from '@angular/core';

export const THEMES = [
  { key: 'sunny',  name: '🌞 Sunny',  swatches: ['#F97316', '#FFF3E8', '#22C55E'] },
  { key: 'ocean',  name: '🌊 Ocean',  swatches: ['#0EA5E9', '#E0F7FA', '#14B8A6'] },
  { key: 'forest', name: '🌿 Forest', swatches: ['#16A34A', '#F0FDF4', '#D97706'] },
  { key: 'candy',  name: '🍭 Candy',  swatches: ['#EC4899', '#FDF2F8', '#8B5CF6'] },
] as const;

@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly current = signal<string>('sunny');

  init(): void {
    const stored = localStorage.getItem('th_theme') ?? 'sunny';
    this.current.set(stored);
    document.body.setAttribute('data-theme', stored);
  }

  apply(theme: string): void {
    this.current.set(theme);
    localStorage.setItem('th_theme', theme);
    document.body.setAttribute('data-theme', theme);
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
cd frontend && npx ng test --watch=false --include="**/theme.service.spec.ts"
```

Expected: 3 tests pass.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/app/core/services/theme.service.ts \
        frontend/src/app/core/services/theme.service.spec.ts
git commit -m "feat: add ThemeService with signal, localStorage, and data-theme sync"
```

---

## Task 6: Frontend — Wire ThemeService into app bootstrap and UserService

**Files:**
- Modify: `frontend/src/app/app.ts`
- Modify: `frontend/src/app/core/models/user-profile.model.ts`
- Modify: `frontend/src/app/core/services/user.service.ts`

- [ ] **Step 1: Add `preferredTheme` to the frontend models**

In `frontend/src/app/core/models/user-profile.model.ts`, add `preferredTheme: string` to `UserProfile` and `preferredTheme?: string` to `UpdateProfileRequest`:

```typescript
export interface UserProfile {
  userId: string;
  displayName: string;
  email: string;
  preferredLanguage: string;
  pushNotificationsEnabled: boolean;
  weeklyEmailEnabled: boolean;
  preferredTheme: string;
}

export interface UpdateProfileRequest {
  displayName?: string;
  preferredLanguage?: string;
  pushNotificationsEnabled?: boolean;
  weeklyEmailEnabled?: boolean;
  preferredTheme?: string;
}
```

- [ ] **Step 2: Wire ThemeService into UserService**

In `frontend/src/app/core/services/user.service.ts`, inject `ThemeService` and call `themeService.apply()` in the `loadProfile` success handler, mirroring the existing language sync pattern:

```typescript
import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs';
import { TranslateService } from '@ngx-translate/core';
import { environment } from '../../../environments/environment';
import { UserProfile, UpdateProfileRequest } from '../models/user-profile.model';
import { ThemeService } from './theme.service';

@Injectable({ providedIn: 'root' })
export class UserService {
  private http = inject(HttpClient);
  private translate = inject(TranslateService);
  private themeService = inject(ThemeService);
  private _profile = signal<UserProfile | null>(null);
  readonly profile = this._profile.asReadonly();

  loadProfile() {
    this.http.get<UserProfile>(`${environment.apiUrl}/users/me`).subscribe({
      next: (p) => {
        this._profile.set(p);
        if (p.preferredLanguage) {
          localStorage.setItem('th_preferred_lang', p.preferredLanguage);
          this.translate.use(p.preferredLanguage);
        }
        if (p.preferredTheme) {
          this.themeService.apply(p.preferredTheme);
        }
      },
      error: () => this._profile.set(null)
    });
  }

  updateProfile(req: UpdateProfileRequest) {
    return this.http.patch<UserProfile>(`${environment.apiUrl}/users/me`, req)
      .pipe(tap(p => this._profile.set(p)));
  }
}
```

- [ ] **Step 3: Call `themeService.init()` in AppComponent**

In `frontend/src/app/app.ts`, inject `ThemeService` and call `init()` in `ngOnInit` so the stored theme is applied before first render:

```typescript
import { Component, OnInit, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Title } from '@angular/platform-browser';
import { TranslateService } from '@ngx-translate/core';
import { RouterProgressComponent } from './shared/components/router-progress.component';
import { ThemeService } from './core/services/theme.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterProgressComponent],
  template: `<app-router-progress /><router-outlet />`
})
export class AppComponent implements OnInit {
  private title = inject(Title);
  private translate = inject(TranslateService);
  private themeService = inject(ThemeService);

  ngOnInit() {
    this.translate.get('APP_NAME').subscribe(name => this.title.setTitle(name));
    this.themeService.init();
  }
}
```

> **Note:** Read `app.ts` before editing — add only what's missing, preserve any existing imports/template content.

- [ ] **Step 4: Run the full frontend test suite**

```bash
cd frontend && npx ng test --watch=false
```

Expected: all existing tests pass.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/app/core/models/user-profile.model.ts \
        frontend/src/app/core/services/user.service.ts \
        frontend/src/app/app.ts
git commit -m "feat: wire ThemeService into AppComponent bootstrap and UserService profile sync"
```

---

## Task 7: Frontend — Theme picker in Profile component

**Files:**
- Modify: `frontend/src/app/features/settings/pages/profile.component.ts`

- [ ] **Step 1: Add theme picker to ProfileComponent**

In `profile.component.ts`, make the following changes:

**Add import:**
```typescript
import { ThemeService, THEMES } from '../../../core/services/theme.service';
```

**Add injected service and signal in the class body (after existing injections):**
```typescript
private themeService = inject(ThemeService);
protected themes = THEMES;
protected showThemePicker = signal(false);
protected currentThemeName = computed(() => {
  const key = this.themeService.current();
  return THEMES.find(t => t.key === key)?.name ?? '🌞 Sunny';
});
```

**Add method:**
```typescript
selectTheme(key: string) {
  this.themeService.apply(key);
  this.userService.updateProfile({ preferredTheme: key }).subscribe();
  this.showThemePicker.set(false);
}
```

**Add theme picker row in the template, inside the Preferences `<div>` block, after the Language rows and before the Push notifications row.** The language picker block ends with `}` closing `@if (showLangPicker())`. Insert this block after it:

```html
<!-- Theme row -->
<div>
  <div class="flex items-center gap-3 p-3 cursor-pointer" (click)="showThemePicker.set(!showThemePicker())">
    <span class="text-base">🎨</span>
    <div class="flex-1">
      <p class="text-xs text-brand-muted">{{ 'SETTINGS.THEME' | translate }}</p>
      <div class="flex items-center gap-1.5 mt-0.5">
        <div class="w-3 h-3 rounded-sm" [style.background]="themes[0].swatches[0]"
          [style]="'background:' + (themes | themeSwatch:themeService.current())"></div>
        <p class="text-sm font-bold text-brand-text">{{ currentThemeName() }}</p>
      </div>
    </div>
    <span class="text-brand-muted text-xs flex-shrink-0">{{ showThemePicker() ? '▲' : ('SHARED.CHANGE' | translate) }}</span>
  </div>
  @if (showThemePicker()) {
    <div class="border-t border-brand-border">
      <div class="px-3 py-2 bg-brand-cream text-xs font-bold text-brand-muted">{{ 'SETTINGS.SELECT_THEME' | translate }}</div>
      @for (theme of themes; track theme.key) {
        <div (click)="selectTheme(theme.key)"
          class="flex items-center gap-3 px-3 py-2.5 border-b border-brand-border last:border-0 cursor-pointer hover:bg-brand-cream">
          <div class="flex gap-1">
            @for (swatch of theme.swatches; track swatch) {
              <div class="w-4 h-4 rounded" [style.background]="swatch"></div>
            }
          </div>
          <span class="text-sm text-brand-text flex-1">{{ theme.name }}</span>
          @if (themeService.current() === theme.key) {
            <span class="text-brand-orange text-sm">✓</span>
          }
        </div>
      }
    </div>
  }
</div>
```

> **Note:** The `themeSwatch` pipe reference in the accent dot is optional complexity — simplify to just showing the first swatch of the active theme using a `computed()` signal instead:

Replace the accent dot in the collapsed row with:
```typescript
protected currentThemeSwatches = computed(() => {
  const key = this.themeService.current();
  return THEMES.find(t => t.key === key)?.swatches ?? THEMES[0].swatches;
});
```

And in the template use:
```html
<div class="w-3 h-3 rounded-sm" [style.background]="currentThemeSwatches()[0]"></div>
```

- [ ] **Step 2: Run frontend tests**

```bash
cd frontend && npx ng test --watch=false
```

Expected: all tests pass.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/app/features/settings/pages/profile.component.ts
git commit -m "feat: add theme picker to Profile preferences section"
```

---

## Task 8: Frontend — i18n translation keys

**Files:**
- Modify: `frontend/public/assets/i18n/en.json`
- Modify: `frontend/public/assets/i18n/hu.json`
- Modify: `frontend/public/assets/i18n/de.json`
- Modify: `frontend/public/assets/i18n/fr.json`
- Modify: `frontend/public/assets/i18n/es.json`

- [ ] **Step 1: Add keys to all locale files**

In each file, add under the `SETTINGS` object (after `SELECT_LANGUAGE`):

**en.json:**
```json
"THEME": "Theme",
"SELECT_THEME": "Select theme"
```

**hu.json:**
```json
"THEME": "Téma",
"SELECT_THEME": "Téma kiválasztása"
```

**de.json:**
```json
"THEME": "Design",
"SELECT_THEME": "Design auswählen"
```

**fr.json:**
```json
"THEME": "Thème",
"SELECT_THEME": "Choisir un thème"
```

**es.json:**
```json
"THEME": "Tema",
"SELECT_THEME": "Seleccionar tema"
```

- [ ] **Step 2: Commit**

```bash
git add frontend/public/assets/i18n/
git commit -m "feat: add theme picker i18n keys for all 5 locales"
```

---

## Task 9: Docs, changelog, and version bump

**Files:**
- Modify: `CHANGELOG.md`
- Modify: `frontend/src/environments/environment.ts`
- Modify: `frontend/src/environments/environment.prod.ts`
- Modify: `frontend/src/app/features/help/help.component.ts`

- [ ] **Step 1: Update CHANGELOG.md**

Under `## [Unreleased]`, add:

```markdown
### Added
- Theme selection: choose from four visual themes (Sunny, Ocean, Forest, Candy) in Profile → Preferences. Each theme changes the app's colour palette and font. Your choice is saved to your account and restored on every device.
```

- [ ] **Step 2: Bump frontend version**

Read the current version from `frontend/src/environments/environment.ts`. If the current version is `x.y.z`, bump the minor version to `x.(y+1).0`.

Update `frontend/src/environments/environment.ts` — change the `version` field.
Update `frontend/src/environments/environment.prod.ts` — change the `version` field to the same value.

Promote the `## [Unreleased]` CHANGELOG block to `## [x.y+1.0] - 2026-06-06`.

- [ ] **Step 3: Update help component if needed**

Read `frontend/src/app/features/help/help.component.ts`. If it has a section describing Settings or Profile, add a step describing the theme picker. If no such section exists, skip this step.

- [ ] **Step 4: Commit**

```bash
git add CHANGELOG.md \
        frontend/src/environments/environment.ts \
        frontend/src/environments/environment.prod.ts \
        frontend/src/app/features/help/help.component.ts
git commit -m "chore: bump frontend version to x.y+1.0 — theme selection"
```

---

## Task 10: Smoke test and final verification

- [ ] **Step 1: Run full backend tests**

```bash
cd backend && dotnet test
```

Expected: all tests pass.

- [ ] **Step 2: Run full frontend tests**

```bash
cd frontend && npx ng test --watch=false
```

Expected: all tests pass.

- [ ] **Step 3: Start the full stack and smoke test manually**

```bash
docker compose up -d --build
```

Then open `http://localhost:4200`, log in as `testuser@demo.com / Password1!`, and verify:

1. Navigate to Settings → My Profile
2. Scroll to Preferences section — a Theme row appears showing 🌞 Sunny
3. Tap the Theme row — it expands showing all 4 themes with color swatches
4. Select Ocean — the entire app immediately turns blue/teal, font changes to Nunito
5. Reload the page — Ocean theme is still active (localStorage restore)
6. Select Forest — app turns green
7. Select Candy — app turns pink/purple
8. Select Sunny — returns to default
9. Log out, log back in — theme from backend is restored (even after clearing localStorage)

- [ ] **Step 4: Final commit if any fixes were needed, then push**

```bash
git push -u origin feat/57-theme-selection
```

---

## Self-Review Notes

- All `UserProfileResponse` constructor calls in the controller are updated in Task 1 — both `GetMe` and `UpdateMe`.
- `THEMES` constant is defined in `theme.service.ts` and imported by `profile.component.ts` — no duplication.
- `currentThemeSwatches` computed replaces the pipe approach — no custom pipe needed.
- The `body { background-color }` rule is updated to `var(--color-brand-bg)` in Task 4 so the body background tracks the theme; without this the page background stays warm-cream regardless of theme.
- Backend default value `"sunny"` on the migration ensures existing users get the default theme without null handling anywhere in the frontend.
