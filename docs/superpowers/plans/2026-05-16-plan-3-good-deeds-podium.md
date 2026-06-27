# TinyHeroes — Plan 3: Good Deeds, Presets & Weekly Podium

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the core deed-tracking loop — parents can add good deeds for children (with preset quick-picks and emoji images), view deed counts on the dashboard, see deed history on child profiles, and celebrate weekly rankings on the podium page.

**Architecture:** GoodDeed and DeedPreset entities with CRUD controllers. Dashboard enhanced with a stats endpoint returning weekly counts per child. Podium computes live rankings from deed data. Frontend uses signal-based services following established patterns.

**Tech Stack:** ASP.NET Core 10, EF Core 10, Angular 21, Tailwind 4, ngx-translate

---

## Context

Plan 2 (Family Management) is complete. We have: children CRUD, dashboard with child cards (showing 0 deeds), child profile (empty deed list), invite flow, bottom nav shell. Plan 3 adds the core value proposition: logging good deeds, preset management, live deed counts, and the weekly podium ranking.

**Design spec:** `docs/superpowers/specs/2026-05-16-tinyheroes-design.md` — Screens 9, 9b, 10, updates to 5 and 8.

**Deferred to Plan 4+:** AI image generation, monthly champion, history page, prizes.

---

## Architecture Decisions

1. **Images: Emoji-only for Plan 3.** GoodDeed stores `ImageType = "library"` and `ImageValue = "🧹"` (the emoji). AI generation deferred. The "library" is a categorized emoji grid in the frontend — no backend image storage.
2. **DeedPreset seeding:** System presets inserted via EF `HasData()` in configuration. No migration needed (InMemory DB for tests).
3. **Stats endpoint:** `GET /api/children/stats` returns `[{ childId, weeklyCount, totalCount }]` — avoids N+1 by computing counts in a single query. Dashboard and child profile consume this.
4. **Podium: Live computation.** No WeekSummary table yet — the podium page queries deed counts for the current week and ranks them. WeekSummary (snapshotting finalized weeks) is Plan 4.
5. **No separate DeedService in backend.** Controller handles CRUD directly (same pattern as existing controllers). Business logic is minimal — just data access.

---

## Task Overview (11 Tasks)

| # | Task | Layer |
|---|------|-------|
| 1 | GoodDeed entity + EF config | Backend |
| 2 | DeedPreset entity + EF config + system seed data | Backend |
| 3 | GoodDeed controller + tests (create, list by child, stats) | Backend |
| 4 | DeedPreset controller + tests (list, toggle, create custom, delete) | Backend |
| 5 | Deed service + models (frontend) | Frontend |
| 6 | Add Good Deed page (Screen 9) | Frontend |
| 7 | Preset service + Manage Presets page (Screen 9b) | Frontend |
| 8 | Dashboard update — live deed counts + ranking badges | Frontend |
| 9 | Child Profile update — deed list + stats | Frontend |
| 10 | Weekly Podium page (Screen 10) | Frontend |
| 11 | i18n strings (en + hu) + final build verification | Frontend |

---

### Task 1: GoodDeed Entity + EF Configuration

**Files:**
- Create: `backend/TinyHeroes.Domain/Entities/GoodDeed.cs`
- Create: `backend/TinyHeroes.Infrastructure/Data/Configurations/GoodDeedConfiguration.cs`
- Modify: `backend/TinyHeroes.Infrastructure/Data/AppDbContext.cs` — add `DbSet<GoodDeed>`
- Modify: `backend/TinyHeroes.Domain/Entities/Child.cs` — add `ICollection<GoodDeed> Deeds` nav property

```csharp
// Domain/Entities/GoodDeed.cs
namespace TinyHeroes.Domain.Entities;

public class GoodDeed
{
    public Guid Id { get; set; }
    public Guid ChildId { get; set; }
    public Guid AddedByUserId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ImageType { get; set; } = "library";
    public string ImageValue { get; set; } = "⭐";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Child Child { get; set; } = null!;
    public User AddedBy { get; set; } = null!;
}
```

**GoodDeedConfiguration:**
- FK to Child (cascade delete)
- FK to User (no cascade — AddedByUserId)
- Index on ChildId
- Index on CreatedAt
- Description max length 500

**AppDbContext addition:**
```csharp
public DbSet<GoodDeed> GoodDeeds => Set<GoodDeed>();
```

**Child.cs addition:**
```csharp
public ICollection<GoodDeed> Deeds { get; set; } = [];
```

**Commit:** `feat: GoodDeed entity with EF configuration`

---

### Task 2: DeedPreset Entity + EF Configuration + Seed Data

**Files:**
- Create: `backend/TinyHeroes.Domain/Entities/DeedPreset.cs`
- Create: `backend/TinyHeroes.Infrastructure/Data/Configurations/DeedPresetConfiguration.cs`
- Modify: `backend/TinyHeroes.Infrastructure/Data/AppDbContext.cs` — add `DbSet<DeedPreset>`

```csharp
// Domain/Entities/DeedPreset.cs
namespace TinyHeroes.Domain.Entities;

public class DeedPreset
{
    public Guid Id { get; set; }
    public Guid? FamilyId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string ImageValue { get; set; } = "⭐";
    public bool Enabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Family? Family { get; set; }
}
```

**DeedPresetConfiguration:**
- Optional FK to Family (FamilyId nullable — null means system preset)
- Index on FamilyId
- Label max length 200
- Seed 6 system presets via `HasData()`:

```csharp
builder.HasData(
    new DeedPreset { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), FamilyId = null, Label = "Did homework", ImageValue = "📚", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
    new DeedPreset { Id = Guid.Parse("00000000-0000-0000-0000-000000000002"), FamilyId = null, Label = "Helped in kitchen", ImageValue = "🍳", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
    new DeedPreset { Id = Guid.Parse("00000000-0000-0000-0000-000000000003"), FamilyId = null, Label = "Cleaned room", ImageValue = "🧹", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
    new DeedPreset { Id = Guid.Parse("00000000-0000-0000-0000-000000000004"), FamilyId = null, Label = "Helped sibling", ImageValue = "🤝", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
    new DeedPreset { Id = Guid.Parse("00000000-0000-0000-0000-000000000005"), FamilyId = null, Label = "Behaved all day", ImageValue = "😊", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
    new DeedPreset { Id = Guid.Parse("00000000-0000-0000-0000-000000000006"), FamilyId = null, Label = "Made bed", ImageValue = "🛏️", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
);
```

**AppDbContext addition:**
```csharp
public DbSet<DeedPreset> DeedPresets => Set<DeedPreset>();
```

**Commit:** `feat: DeedPreset entity with system seed data`

---

### Task 3: GoodDeed Controller + Tests

**Files:**
- Create: `backend/TinyHeroes.Application/DTOs/Deed/CreateDeedRequest.cs`
- Create: `backend/TinyHeroes.Application/DTOs/Deed/DeedResponse.cs`
- Create: `backend/TinyHeroes.Application/DTOs/Deed/ChildStatsResponse.cs`
- Create: `backend/TinyHeroes.Api/Controllers/DeedController.cs`
- Create: `backend/TinyHeroes.Tests/Integration/DeedControllerTests.cs`

**DTOs (all in one file `CreateDeedRequest.cs`):**
```csharp
namespace TinyHeroes.Application.DTOs.Deed;

public record CreateDeedRequest(Guid ChildId, string Description, string ImageValue);
public record DeedResponse(Guid Id, Guid ChildId, string Description, string ImageType, string ImageValue, string AddedByName, DateTime CreatedAt);
public record ChildStatsResponse(Guid ChildId, int WeeklyCount, int TotalCount);
```

**Endpoints:**
- `POST /api/deeds` — create a good deed for a child in user's family. Validates child belongs to user's family.
- `GET /api/deeds?childId={id}` — list deeds for a child (most recent first, limit 50). Child must belong to user's family.
- `GET /api/deeds/stats` — returns `List<ChildStatsResponse>` for all children in user's family. `WeeklyCount` = deeds created since the family's week start day. `TotalCount` = all deeds ever.

**Weekly count calculation:**
```csharp
// Determine current week start based on family's WeekStartDay
var today = DateTime.UtcNow.Date;
var daysSinceStart = ((int)today.DayOfWeek - (int)family.WeekStartDay + 7) % 7;
var weekStart = today.AddDays(-daysSinceStart);
```

**Tests (7):**
1. `Create_WithValidChild_Returns200` — create deed, assert response has description and imageValue
2. `Create_WithChildNotInFamily_Returns404` — try to add deed for another family's child
3. `Create_WithoutFamily_Returns400` — user with no family tries to create deed
4. `ListByChild_ReturnsDeeds` — create 2 deeds, list, assert count=2 and ordered by CreatedAt desc
5. `ListByChild_WhenChildNotInFamily_Returns404`
6. `Stats_ReturnsCountsForAllChildren` — create children + deeds, verify stats has correct weekly/total counts
7. `Stats_WeeklyCountResetsPerWeek` — create a deed, verify weeklyCount=1 (basic verification)

**Commit:** `feat: good deed endpoints with integration tests`

---

### Task 4: DeedPreset Controller + Tests

**Files:**
- Create: `backend/TinyHeroes.Application/DTOs/Preset/PresetResponse.cs`
- Create: `backend/TinyHeroes.Application/DTOs/Preset/CreatePresetRequest.cs`
- Create: `backend/TinyHeroes.Application/DTOs/Preset/TogglePresetRequest.cs`
- Create: `backend/TinyHeroes.Api/Controllers/PresetController.cs`
- Create: `backend/TinyHeroes.Tests/Integration/PresetControllerTests.cs`

**DTOs (all in one file `PresetResponse.cs`):**
```csharp
namespace TinyHeroes.Application.DTOs.Preset;

public record PresetResponse(Guid Id, string Label, string ImageValue, bool Enabled, bool IsSystem);
public record CreatePresetRequest(string Label, string ImageValue);
public record TogglePresetRequest(bool Enabled);
```

**Endpoints:**
- `GET /api/presets` — returns all presets visible to the user: system presets (FamilyId=null) + family's custom presets (FamilyId=user's family). `IsSystem` = true when FamilyId is null.
- `POST /api/presets` — create a custom preset for user's family. Admin only.
- `PUT /api/presets/{id}/toggle` — toggle `Enabled` flag. Works on system presets (per-family override needed? No — for Plan 3, enabled is global on the preset. Simpler: the toggle only works on the family's own presets. System presets are always shown as enabled). Actually, re-reading spec: "List of built-in presets with on/off toggles". So we need per-family toggling of system presets. **Solution:** When a user toggles a system preset off, we create a family-scoped DeedPreset with the same label but `Enabled=false` and the system preset's Id as a reference. Wait — that's complex. **Simpler approach for Plan 3:** The toggle endpoint sets `Enabled` directly on the preset row. System presets with `Enabled=false` are hidden for ALL families. This is wrong. **Best approach:** Add a `DisabledPresets` table? No, YAGNI. **Final decision:** For Plan 3, system presets are always visible (no per-family toggle). The "Manage" page only allows creating/deleting custom presets. Per-family toggle of system presets deferred to a future plan. The frontend shows all system presets without toggle controls.
- `DELETE /api/presets/{id}` — delete a custom preset. Must belong to user's family. Cannot delete system presets. Admin only.

**Revised endpoints (simplified):**
- `GET /api/presets` — returns system presets + family's custom presets
- `POST /api/presets` — create custom preset (any family member)
- `DELETE /api/presets/{id}` — delete custom preset (Admin only, must be family's preset)

**Tests (5):**
1. `List_ReturnsSystemAndFamilyPresets` — verify system presets appear + any family custom ones
2. `Create_AddsCustomPreset` — create, verify in list with IsSystem=false
3. `Create_WithoutFamily_Returns400`
4. `Delete_CustomPreset_Succeeds` — admin deletes family's custom preset
5. `Delete_SystemPreset_Returns403` — cannot delete system presets

**Commit:** `feat: deed preset endpoints with integration tests`

---

### Task 5: Deed Service + Models (Frontend)

**Files:**
- Create: `frontend/src/app/core/models/deed.model.ts`
- Create: `frontend/src/app/core/services/deed.service.ts`

**Models:**
```typescript
// deed.model.ts
export interface GoodDeed {
  id: string;
  childId: string;
  description: string;
  imageType: string;
  imageValue: string;
  addedByName: string;
  createdAt: string;
}

export interface CreateDeedRequest {
  childId: string;
  description: string;
  imageValue: string;
}

export interface ChildStats {
  childId: string;
  weeklyCount: number;
  totalCount: number;
}

export interface DeedPreset {
  id: string;
  label: string;
  imageValue: string;
  enabled: boolean;
  isSystem: boolean;
}

export interface CreatePresetRequest {
  label: string;
  imageValue: string;
}
```

**DeedService:**
```typescript
// deed.service.ts
import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { GoodDeed, CreateDeedRequest, ChildStats } from '../models/deed.model';

@Injectable({ providedIn: 'root' })
export class DeedService {
  private _stats = signal<ChildStats[]>([]);
  readonly stats = this._stats.asReadonly();

  constructor(private http: HttpClient) {}

  loadStats() {
    return this.http.get<ChildStats[]>(`${environment.apiUrl}/deeds/stats`).subscribe({
      next: (stats) => this._stats.set(stats),
      error: () => this._stats.set([])
    });
  }

  getDeeds(childId: string) {
    return this.http.get<GoodDeed[]>(`${environment.apiUrl}/deeds?childId=${childId}`);
  }

  createDeed(req: CreateDeedRequest) {
    return this.http.post<GoodDeed>(`${environment.apiUrl}/deeds`, req);
  }
}
```

**Commit:** `feat: deed service and models`

---

### Task 6: Add Good Deed Page (Screen 9)

**Files:**
- Create: `frontend/src/app/features/deeds/pages/add-deed.component.ts`
- Create: `frontend/src/app/core/services/preset.service.ts`
- Modify: `frontend/src/app/app.routes.ts` — add `add-deed/:childId` route under shell

**PresetService:**
```typescript
import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { DeedPreset, CreatePresetRequest } from '../models/deed.model';

@Injectable({ providedIn: 'root' })
export class PresetService {
  private _presets = signal<DeedPreset[]>([]);
  readonly presets = this._presets.asReadonly();

  constructor(private http: HttpClient) {}

  loadPresets() {
    return this.http.get<DeedPreset[]>(`${environment.apiUrl}/presets`).subscribe({
      next: (presets) => this._presets.set(presets),
      error: () => this._presets.set([])
    });
  }

  createPreset(req: CreatePresetRequest) {
    return this.http.post<DeedPreset>(`${environment.apiUrl}/presets`, req);
  }

  deletePreset(id: string) {
    return this.http.delete(`${environment.apiUrl}/presets/${id}`);
  }
}
```

**AddDeedComponent UI (Screen 9):**
- Route: `/add-deed/:childId`
- Child selector: shows current child avatar + name at top (from childService)
- "Quick pick" grid: 4 columns of preset tiles. System presets shown normally, custom presets with purple tint. Tapping a preset fills in description + imageValue.
- Description text field (auto-filled from preset, editable)
- Image row: shows selected emoji (large) with "Change" button that opens a categorized emoji picker overlay
- Emoji picker overlay: grid of emojis in categories (🏠 Home, 📚 Learning, 🏃 Activity, 🤝 Social, ⭐ Other)
- "Save" button — calls deedService.createDeed(), navigates back to child profile or dashboard

**Emoji library categories (hardcoded in component):**
```typescript
emojiLibrary = [
  { category: 'Home', emojis: ['🧹', '🛏️', '🍳', '🧺', '🪴', '🗑️', '🧽', '🚿'] },
  { category: 'Learning', emojis: ['📚', '✏️', '🎨', '🎵', '🔬', '📐', '💻', '📖'] },
  { category: 'Activity', emojis: ['🏃', '⚽', '🚴', '🏊', '🧘', '💪', '🎯', '🏆'] },
  { category: 'Social', emojis: ['🤝', '🫂', '💝', '🙏', '👋', '🎁', '😊', '🌟'] },
  { category: 'Other', emojis: ['⭐', '🌈', '🎉', '👏', '💫', '🦸', '🏅', '❤️'] },
];
```

**Commit:** `feat: add good deed page with preset quick-pick`

---

### Task 7: Manage Presets Page (Screen 9b)

**Files:**
- Create: `frontend/src/app/features/deeds/pages/manage-presets.component.ts`
- Modify: `frontend/src/app/app.routes.ts` — add `manage-presets` route under shell

**UI:**
- "Create custom preset" button at top
- Inline form when creating: emoji picker (small grid) + text input + Save/Cancel buttons
- List of custom presets: icon + name + delete button per row
- Separator
- List of system (built-in) presets: icon + name (read-only, no controls)
- Link back: "← Back to Add Deed"

**On delete:** calls presetService.deletePreset(), refreshes list.
**On create:** calls presetService.createPreset(), refreshes list.

**Commit:** `feat: manage presets page`

---

### Task 8: Dashboard Update — Live Deed Counts + Ranking

**Files:**
- Modify: `frontend/src/app/features/dashboard/pages/home.component.ts`

**Changes:**
- Import and inject `DeedService`
- Call `deedService.loadStats()` in `ngOnInit()`
- Create a computed `childrenWithStats()` that merges `childService.children()` with `deedService.stats()` — attaching `weeklyCount` and `totalCount` to each child
- Replace hardcoded `0` in template with actual counts from stats
- Sort children by weeklyCount descending (highest first)
- Show rank badges: 🥇 for 1st, 🥈 for 2nd, 🥉 for 3rd (only when weeklyCount > 0)
- Progress bar: width based on `weeklyCount / maxWeeklyCount * 100`
- Per-child `+` button now routes to `/add-deed/:childId`
- Child card tap (on name/avatar area) routes to `/child/:childId`

**Commit:** `feat: dashboard live deed counts with ranking`

---

### Task 9: Child Profile Update — Deed List + Stats

**Files:**
- Modify: `frontend/src/app/features/dashboard/pages/child-profile.component.ts`

**Changes:**
- Import and inject `DeedService`
- Load deeds for this child: `deedService.getDeeds(childId)` on init, store in a signal
- Load stats: use `deedService.stats()` to get this child's counts
- Replace hardcoded stat "0" values with actual weeklyCount, totalCount (wins stays 0 — computed from WeekSummary in Plan 4)
- Replace empty deed list with actual deeds, grouped by date (Today, Yesterday, Earlier)
- Each deed card: emoji tile + description + "Added by [name] · [relative time]"
- Keep empty state when no deeds exist
- "Add Good Deed" button now routes to `/add-deed/:childId` (no longer disabled)

**Commit:** `feat: child profile with deed list and stats`

---

### Task 10: Weekly Podium Page (Screen 10)

**Files:**
- Modify: `frontend/src/app/features/podium/pages/podium.component.ts`

**UI (Screen 10 from design spec):**
- Purple-to-orange gradient header with decorative star emojis
- Week label: "Week of [date] – [date]"
- Three-step podium visual:
  - Layout: 2nd (silver, medium height) · 1st (gold, tallest) · 3rd (bronze, shortest)
  - Each step: child avatar emoji floating above, name below, deed count, medal emoji
- Full ranking list below podium (all children, medal + avatar + name + weekly deed count)
- If fewer than 3 children: adapt podium (show only available positions)
- If no deeds this week: "No deeds yet this week! Start adding good deeds 🌟" empty state

**Data:** Uses `deedService.stats()` (same data as dashboard), sorts by weeklyCount desc.

**Inject:** DeedService, ChildService. Load data on init.

**Computed signal `rankings`:** Merges children with their weekly stats, sorted by weeklyCount descending.

**Commit:** `feat: weekly podium page with live rankings`

---

### Task 11: i18n Strings + Final Build Verification

**Files:**
- Modify: `frontend/public/assets/i18n/en.json` — add DEED, PRESET, PODIUM sections
- Modify: `frontend/public/assets/i18n/hu.json` — Hungarian translations

**New i18n keys (en):**
```json
"DEED": {
  "ADD_TITLE": "Add Good Deed",
  "QUICK_PICK": "Quick pick",
  "DESCRIPTION_LABEL": "Description",
  "DESCRIPTION_PLACEHOLDER": "What did they do?",
  "IMAGE_LABEL": "Image",
  "CHANGE_IMAGE": "Change",
  "SAVE": "Save Deed",
  "MANAGE_LINK": "Manage",
  "ADDED_BY": "Added by {{name}}",
  "TODAY": "Today",
  "YESTERDAY": "Yesterday",
  "EARLIER": "Earlier"
},
"PRESET": {
  "MANAGE_TITLE": "Manage Presets",
  "CREATE_CUSTOM": "Create custom preset",
  "CUSTOM_PRESETS": "Your custom presets",
  "SYSTEM_PRESETS": "Built-in presets",
  "LABEL_PLACEHOLDER": "Preset name",
  "SAVE": "Save",
  "CANCEL": "Cancel",
  "BACK": "← Back to Add Deed",
  "EMPTY": "No custom presets yet"
},
"PODIUM": {
  "TITLE": "Weekly Podium",
  "WEEK_OF": "Week of {{start}} – {{end}}",
  "RESULTS_IN": "Results are in!",
  "DEEDS": "deeds",
  "RANKING": "Full ranking",
  "EMPTY_TITLE": "No deeds yet this week!",
  "EMPTY_SUBTITLE": "Start adding good deeds 🌟",
  "FIRST": "1st",
  "SECOND": "2nd",
  "THIRD": "3rd"
}
```

**Hungarian translations:**
```json
"DEED": {
  "ADD_TITLE": "Jó cselekedet hozzáadása",
  "QUICK_PICK": "Gyorsválasztó",
  "DESCRIPTION_LABEL": "Leírás",
  "DESCRIPTION_PLACEHOLDER": "Mit csinált?",
  "IMAGE_LABEL": "Kép",
  "CHANGE_IMAGE": "Módosítás",
  "SAVE": "Mentés",
  "MANAGE_LINK": "Kezelés",
  "ADDED_BY": "Hozzáadta: {{name}}",
  "TODAY": "Ma",
  "YESTERDAY": "Tegnap",
  "EARLIER": "Korábban"
},
"PRESET": {
  "MANAGE_TITLE": "Sablonok kezelése",
  "CREATE_CUSTOM": "Egyéni sablon létrehozása",
  "CUSTOM_PRESETS": "Egyéni sablonok",
  "SYSTEM_PRESETS": "Beépített sablonok",
  "LABEL_PLACEHOLDER": "Sablon neve",
  "SAVE": "Mentés",
  "CANCEL": "Mégse",
  "BACK": "← Vissza",
  "EMPTY": "Még nincs egyéni sablon"
},
"PODIUM": {
  "TITLE": "Heti dobogó",
  "WEEK_OF": "{{start}} – {{end}} hete",
  "RESULTS_IN": "Íme az eredmények!",
  "DEEDS": "cselekedet",
  "RANKING": "Teljes rangsor",
  "EMPTY_TITLE": "Még nincs cselekedet ezen a héten!",
  "EMPTY_SUBTITLE": "Kezdj el jó cselekedeteket hozzáadni 🌟",
  "FIRST": "1.",
  "SECOND": "2.",
  "THIRD": "3."
}
```

**Verification:**
1. `cd backend && dotnet test` — all tests pass (19 existing + ~12 new ≈ 31 total)
2. `cd frontend && npx ng build --configuration production` — 0 errors

**Commit:** `feat: i18n for Plan 3 screens — Plan 3 complete`

---

## Verification Checklist

- [ ] Backend builds with 0 errors
- [ ] All backend tests pass (~31 total)
- [ ] Frontend builds with 0 errors (prod config)
- [ ] Add Good Deed: select preset → description auto-fills → save → deed appears
- [ ] Dashboard shows live deed counts and ranking badges
- [ ] Child Profile shows deed list grouped by date
- [ ] Podium shows correct weekly ranking with podium visual
- [ ] Manage Presets: create custom preset → appears in Add Deed quick-pick
- [ ] Delete custom preset works
- [ ] All new strings in i18n files (en + hu)
