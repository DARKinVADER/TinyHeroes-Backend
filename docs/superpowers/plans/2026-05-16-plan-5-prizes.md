# TinyHeroes — Plan 5: Prizes Board, Prize Editor & Custom Prizes

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add the complete prize system: PrizePreset entity, Prizes Board (Screen 13), Prize Editor (Screen 13b), My Custom Prizes (Screen 13c), and active prize assignment per rank/scope.

**Architecture:** PrizePreset entity (same pattern as DeedPreset — familyId=null for system suggestions, familyId set for custom). A PrizeAssignment entity tracks which prizes are currently active for each rank/scope in a family. Frontend adds 3 new pages under the Prizes tab with route navigation.

**Tech Stack:** ASP.NET Core 10, EF Core 10, Angular 21, Tailwind 4, ngx-translate

---

## Context

Plan 4 (Monthly Champion, History & Week Summaries) is complete. We have: deed tracking, presets, live weekly podium, monthly champion, history views, and summary persistence. Plan 5 adds the prize system: parents configure which prizes children win for 1st/2nd/3rd weekly and monthly champion. After this plan, families can set up prizes, pick from built-in suggestions or create custom ones, and see prize info on the podium/history pages.

**Design spec:** `docs/superpowers/specs/2026-05-16-tinyheroes-design.md` — Screens 13, 13b, 13c.

**Deferred to Plan 6:** Settings page (Family Settings, My Profile, language switching).

---

## Architecture Decisions

1. **PrizePreset entity** — same pattern as DeedPreset. `FamilyId=null` means system suggestion (seeded via HasData). `FamilyId` set means family's custom prize. Fields: Id, FamilyId, Label, Emoji, Enabled, CreatedAt.
2. **PrizeAssignment entity** — tracks which prize is active for each slot. Fields: Id, FamilyId, Scope (weekly/monthly), Rank (1/2/3 for weekly, null for monthly), Emoji, Label. One row per active prize slot. When a user edits a prize, the existing row is upserted.
3. **Admin-only for prize management.** Only Admin role can create/edit/delete prizes. CoParent can view the prizes board but cannot edit.
4. **No automatic prize distribution.** Prizes are displayed on the Podium/History UI but not "awarded" in a transactional sense yet. The PrizesAwarded/PrizeAwarded fields in WeekSummary/MonthSummary remain null — connecting them is a future enhancement.
5. **Prize Editor is a separate page** (not a modal), navigated from the Prizes Board via route params specifying scope+rank.

---

## Task Overview (9 Tasks)

| # | Task | Layer |
|---|------|-------|
| 1 | PrizePreset entity + EF config + seed data | Backend |
| 2 | PrizeAssignment entity + EF config | Backend |
| 3 | PrizePreset CRUD controller + tests | Backend |
| 4 | PrizeAssignment controller + tests | Backend |
| 5 | Prize service + models (frontend) | Frontend |
| 6 | Prizes Board page (Screen 13) | Frontend |
| 7 | Prize Editor page (Screen 13b) | Frontend |
| 8 | My Custom Prizes page (Screen 13c) | Frontend |
| 9 | i18n strings (en + hu) + final build verification | Frontend |

---

### Task 1: PrizePreset Entity + EF Configuration + Seed Data

**Files:**
- Create: `backend/TinyHeroes.Domain/Entities/PrizePreset.cs`
- Create: `backend/TinyHeroes.Infrastructure/Data/Configurations/PrizePresetConfiguration.cs`
- Modify: `backend/TinyHeroes.Infrastructure/Data/AppDbContext.cs` — add `DbSet<PrizePreset>`

```csharp
// Domain/Entities/PrizePreset.cs
namespace TinyHeroes.Domain.Entities;

public class PrizePreset
{
    public Guid Id { get; set; }
    public Guid? FamilyId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Emoji { get; set; } = "🎁";
    public bool Enabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Family? Family { get; set; }
}
```

**PrizePresetConfiguration:**
- FK to Family (cascade delete), nullable
- Index on FamilyId
- Label max length 200, Emoji max length 50
- HasData with system suggestions (FamilyId=null):

```csharp
builder.HasData(
    new PrizePreset { Id = Guid.Parse("10000000-0000-0000-0000-000000000001"), FamilyId = null, Label = "Pizza night", Emoji = "🍕", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
    new PrizePreset { Id = Guid.Parse("10000000-0000-0000-0000-000000000002"), FamilyId = null, Label = "Ice cream", Emoji = "🍦", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
    new PrizePreset { Id = Guid.Parse("10000000-0000-0000-0000-000000000003"), FamilyId = null, Label = "Cupcake", Emoji = "🧁", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
    new PrizePreset { Id = Guid.Parse("10000000-0000-0000-0000-000000000004"), FamilyId = null, Label = "Movie night", Emoji = "🍿", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
    new PrizePreset { Id = Guid.Parse("10000000-0000-0000-0000-000000000005"), FamilyId = null, Label = "Extra screen time", Emoji = "🎮", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
    new PrizePreset { Id = Guid.Parse("10000000-0000-0000-0000-000000000006"), FamilyId = null, Label = "Game night", Emoji = "🎲", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
    new PrizePreset { Id = Guid.Parse("10000000-0000-0000-0000-000000000007"), FamilyId = null, Label = "Late bedtime", Emoji = "🌙", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
    new PrizePreset { Id = Guid.Parse("10000000-0000-0000-0000-000000000008"), FamilyId = null, Label = "Bubble bath", Emoji = "🛁", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
    new PrizePreset { Id = Guid.Parse("10000000-0000-0000-0000-000000000009"), FamilyId = null, Label = "Story choice", Emoji = "📖", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
    new PrizePreset { Id = Guid.Parse("10000000-0000-0000-0000-00000000000a"), FamilyId = null, Label = "Skip a chore", Emoji = "🧹", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
    new PrizePreset { Id = Guid.Parse("10000000-0000-0000-0000-00000000000b"), FamilyId = null, Label = "Amusement park", Emoji = "🎡", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
    new PrizePreset { Id = Guid.Parse("10000000-0000-0000-0000-00000000000c"), FamilyId = null, Label = "Cake slice", Emoji = "🍰", Enabled = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
);
```

**Commit:** `feat: PrizePreset entity with EF configuration and seed data`

---

### Task 2: PrizeAssignment Entity + EF Configuration

**Files:**
- Create: `backend/TinyHeroes.Domain/Entities/PrizeAssignment.cs`
- Create: `backend/TinyHeroes.Infrastructure/Data/Configurations/PrizeAssignmentConfiguration.cs`
- Modify: `backend/TinyHeroes.Infrastructure/Data/AppDbContext.cs` — add `DbSet<PrizeAssignment>`

```csharp
// Domain/Entities/PrizeAssignment.cs
namespace TinyHeroes.Domain.Entities;

public class PrizeAssignment
{
    public Guid Id { get; set; }
    public Guid FamilyId { get; set; }
    public string Scope { get; set; } = "weekly"; // "weekly" or "monthly"
    public int? Rank { get; set; } // 1, 2, 3 for weekly; null for monthly
    public string Emoji { get; set; } = "🎁";
    public string Label { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Family Family { get; set; } = null!;
}
```

**PrizeAssignmentConfiguration:**
- FK to Family (cascade delete)
- Unique index on (FamilyId, Scope, Rank)
- Label max length 200, Emoji max length 50, Scope max length 20

**Commit:** `feat: PrizeAssignment entity with EF configuration`

---

### Task 3: PrizePreset CRUD Controller + Tests

**Files:**
- Create: `backend/TinyHeroes.Application/DTOs/Prize/PrizePresetDtos.cs`
- Create: `backend/TinyHeroes.Api/Controllers/PrizePresetController.cs`
- Create: `backend/TinyHeroes.Tests/Integration/PrizePresetControllerTests.cs`

**DTOs:**
```csharp
namespace TinyHeroes.Application.DTOs.Prize;

public record CreatePrizePresetRequest(string Label, string Emoji);
public record PrizePresetResponse(Guid Id, string Label, string Emoji, bool IsSystem);
```

**Endpoints:**
- `GET /api/prize-presets` — returns system presets (FamilyId=null) + family's custom presets. Map IsSystem = (FamilyId == null).
- `POST /api/prize-presets` — create custom prize preset for user's family (Admin only)
- `DELETE /api/prize-presets/{id}` — delete a custom preset (Admin only, cannot delete system presets)

**Tests (5):**
1. `List_ReturnsSystemAndCustomPresets`
2. `Create_AsAdmin_Succeeds`
3. `Create_WithoutFamily_Returns400`
4. `Delete_AsAdmin_Succeeds`
5. `Delete_SystemPreset_Returns403`

**Commit:** `feat: prize preset CRUD endpoints with tests`

---

### Task 4: PrizeAssignment Controller + Tests

**Files:**
- Create: `backend/TinyHeroes.Application/DTOs/Prize/PrizeAssignmentDtos.cs`
- Create: `backend/TinyHeroes.Api/Controllers/PrizeAssignmentController.cs`
- Create: `backend/TinyHeroes.Tests/Integration/PrizeAssignmentControllerTests.cs`

**DTOs:**
```csharp
namespace TinyHeroes.Application.DTOs.Prize;

public record SetPrizeRequest(string Scope, int? Rank, string Emoji, string Label);
public record PrizeAssignmentResponse(Guid Id, string Scope, int? Rank, string Emoji, string Label);
```

**Endpoints:**
- `GET /api/prize-assignments` — returns all active prize assignments for user's family (4 slots max: weekly 1st/2nd/3rd + monthly)
- `PUT /api/prize-assignments` — upsert a prize assignment (Admin only). Match on (FamilyId, Scope, Rank). Creates if not exists, updates if exists.

**Tests (4):**
1. `List_ReturnsAssignments`
2. `Set_AsAdmin_CreatesAssignment`
3. `Set_AsAdmin_UpdatesExisting`
4. `Set_AsCoParent_Returns403`

**Commit:** `feat: prize assignment endpoints with tests`

---

### Task 5: Prize Service + Models (Frontend)

**Files:**
- Create: `frontend/src/app/core/models/prize.model.ts`
- Create: `frontend/src/app/core/services/prize.service.ts`

**Models:**
```typescript
export interface PrizePresetDto {
  id: string;
  label: string;
  emoji: string;
  isSystem: boolean;
}

export interface PrizeAssignmentDto {
  id: string;
  scope: string;
  rank: number | null;
  emoji: string;
  label: string;
}

export interface SetPrizeRequest {
  scope: string;
  rank: number | null;
  emoji: string;
  label: string;
}

export interface CreatePrizePresetRequest {
  label: string;
  emoji: string;
}
```

**PrizeService:**
```typescript
@Injectable({ providedIn: 'root' })
export class PrizeService {
  private _presets = signal<PrizePresetDto[]>([]);
  private _assignments = signal<PrizeAssignmentDto[]>([]);
  readonly presets = this._presets.asReadonly();
  readonly assignments = this._assignments.asReadonly();

  loadPresets() { /* GET /api/prize-presets */ }
  loadAssignments() { /* GET /api/prize-assignments */ }
  createPreset(req: CreatePrizePresetRequest) { return this.http.post<PrizePresetDto>(...); }
  deletePreset(id: string) { return this.http.delete(...); }
  setPrize(req: SetPrizeRequest) { return this.http.put<PrizeAssignmentDto>(...); }
}
```

**Commit:** `feat: prize service and models`

---

### Task 6: Prizes Board Page (Screen 13)

**Files:**
- Modify: `frontend/src/app/features/prizes/pages/prizes.component.ts` — replace stub with full implementation
- Modify: `frontend/src/app/app.routes.ts` — add child routes for prize-editor and custom-prizes

**UI (Screen 13):**
- Header: "Prizes Board" + "Admin only 🔒" badge
- **Weekly prizes section:** 3 rows (1st/2nd/3rd). Each row: medal emoji, rank label, prize text (emoji + label from assignments), "Edit" button → navigates to `/prizes/edit?scope=weekly&rank=1`
- **Monthly grand prize section:** Purple gradient card with 🏅, "Monthly champion only" label, prize text, "Edit" button → `/prizes/edit?scope=monthly`
- **Prize suggestions library:** Collapsible hint card at bottom
- Empty state for unset prizes: "Not set yet — tap Edit"

**Route changes:** The prizes route becomes a parent (like podium):
```
/prizes → PrizesComponent (board)
/prizes/edit → PrizeEditorComponent (Task 7)
/prizes/custom → CustomPrizesComponent (Task 8)
```

**Data:** Loads `prizeService.loadAssignments()` on init. Uses `computed()` to get assignment per slot.

**Commit:** `feat: prizes board page`

---

### Task 7: Prize Editor Page (Screen 13b)

**Files:**
- Create: `frontend/src/app/features/prizes/pages/prize-editor.component.ts`

**UI (Screen 13b):**
- Header: "Edit Prize" + scope/rank subtitle (e.g., "🥇 Weekly · 1st place")
- **Current prize preview:** Gold/purple gradient card showing current active prize (if set)
- **Write your own section:** Emoji picker button + text input. "Save to my prizes library" checkbox.
- **My prizes row:** Horizontal scrollable chips of family's custom presets (purple-tinted). "+ New" chip. "Manage all →" link → navigates to `/prizes/custom`
- **Built-in suggestions:** Categorized rows:
  - 🍕 Food & Treats: Pizza night, Ice cream, Cupcake, Cake slice
  - 🎬 Activities: Movie night, Extra screen time, Game night
  - 🌙 Special treats: Late bedtime, Bubble bath, Story choice, Skip a chore
  - 🎡 Experiences: Amusement park
- Tapping any suggestion/custom prize fills the input
- **Save Prize button:** Calls `prizeService.setPrize()`, optionally `prizeService.createPreset()` if save-to-library is checked, then navigates back

**Data:** Reads query params `scope` and `rank`. Loads presets and assignments.

**Commit:** `feat: prize editor page`

---

### Task 8: My Custom Prizes Page (Screen 13c)

**Files:**
- Create: `frontend/src/app/features/prizes/pages/custom-prizes.component.ts`

**UI (Screen 13c):**
- Header: "My Custom Prizes" + back button
- **"+ Add new custom prize" button** — reveals inline form
- **Inline form:** Emoji picker + text input + Save/Cancel buttons
- **Custom prizes list:** Each row: emoji tile, label, edit (✏️) and delete (🗑️) buttons
- **Built-in suggestions section (read-only):** Shows system presets with "Built-in" badge, greyed out

**Data:** Uses `prizeService.presets()` signal. Creates/deletes via service methods.

**Commit:** `feat: custom prizes page`

---

### Task 9: i18n Strings + Final Build Verification

**Files:**
- Modify: `frontend/public/assets/i18n/en.json` — add PRIZES section
- Modify: `frontend/public/assets/i18n/hu.json` — Hungarian translations

**New i18n keys (en):**
```json
"PRIZES": {
  "BOARD_TITLE": "Prizes Board",
  "ADMIN_ONLY": "Admin only",
  "WEEKLY_TITLE": "Weekly prizes",
  "MONTHLY_TITLE": "Monthly grand prize",
  "MONTHLY_SUBTITLE": "Monthly champion only",
  "FIRST_PLACE": "1st place",
  "SECOND_PLACE": "2nd place",
  "THIRD_PLACE": "3rd place",
  "EDIT": "Edit",
  "NOT_SET": "Not set yet — tap Edit",
  "SUGGESTIONS_TITLE": "Prize suggestions library",
  "SUGGESTIONS_HINT": "Tap Edit on any prize to pick from ideas",
  "EDITOR_TITLE": "Edit Prize",
  "CURRENT_PRIZE": "Current prize",
  "WRITE_OWN": "Write your own",
  "SAVE_TO_LIBRARY": "Save to my prizes library",
  "SAVE_TO_LIBRARY_HINT": "Reuse it anytime in future prize editing",
  "MY_PRIZES": "My prizes",
  "MANAGE_ALL": "Manage all →",
  "BUILTIN_SUGGESTIONS": "Built-in suggestions",
  "FOOD_TREATS": "Food & Treats",
  "ACTIVITIES": "Activities",
  "SPECIAL_TREATS": "Special treats",
  "EXPERIENCES": "Experiences",
  "SAVE_PRIZE": "Save Prize",
  "CUSTOM_TITLE": "My Custom Prizes",
  "ADD_NEW": "+ Add new custom prize",
  "NEW_PRIZE": "New prize",
  "PRIZE_PLACEHOLDER": "e.g. Trampoline park visit",
  "SAVE": "Save",
  "CANCEL": "Cancel",
  "YOUR_PRIZES": "Your prizes",
  "BUILTIN_NOTE": "These are always available in the Prize Editor — you can't remove them.",
  "BUILTIN_BADGE": "Built-in",
  "EMPTY": "No custom prizes yet"
}
```

**Hungarian:**
```json
"PRIZES": {
  "BOARD_TITLE": "Jutalmak tábla",
  "ADMIN_ONLY": "Csak admin",
  "WEEKLY_TITLE": "Heti jutalmak",
  "MONTHLY_TITLE": "Havi fődíj",
  "MONTHLY_SUBTITLE": "Csak a havi bajnoknak",
  "FIRST_PLACE": "1. hely",
  "SECOND_PLACE": "2. hely",
  "THIRD_PLACE": "3. hely",
  "EDIT": "Szerkesztés",
  "NOT_SET": "Nincs beállítva — koppints a Szerkesztésre",
  "SUGGESTIONS_TITLE": "Jutalom ötletek könyvtára",
  "SUGGESTIONS_HINT": "Koppints a Szerkesztésre bármely jutalomnál az ötletekhez",
  "EDITOR_TITLE": "Jutalom szerkesztése",
  "CURRENT_PRIZE": "Jelenlegi jutalom",
  "WRITE_OWN": "Írj sajátot",
  "SAVE_TO_LIBRARY": "Mentés a jutalom könyvtáramba",
  "SAVE_TO_LIBRARY_HINT": "Bármikor újra felhasználhatod később",
  "MY_PRIZES": "Saját jutalmak",
  "MANAGE_ALL": "Összes kezelése →",
  "BUILTIN_SUGGESTIONS": "Beépített ötletek",
  "FOOD_TREATS": "Étel és nasik",
  "ACTIVITIES": "Tevékenységek",
  "SPECIAL_TREATS": "Különleges kedvezmények",
  "EXPERIENCES": "Élmények",
  "SAVE_PRIZE": "Jutalom mentése",
  "CUSTOM_TITLE": "Saját jutalmak",
  "ADD_NEW": "+ Új egyéni jutalom",
  "NEW_PRIZE": "Új jutalom",
  "PRIZE_PLACEHOLDER": "pl. Trambulin park látogatás",
  "SAVE": "Mentés",
  "CANCEL": "Mégse",
  "YOUR_PRIZES": "Jutalmaid",
  "BUILTIN_NOTE": "Ezek mindig elérhetők a Jutalom szerkesztőben — nem törölhetők.",
  "BUILTIN_BADGE": "Beépített",
  "EMPTY": "Még nincs egyéni jutalom"
}
```

**Verification:**
1. `cd backend && dotnet test` — all tests pass (~45 total)
2. `cd frontend && npx ng build --configuration production` — 0 errors

**Commit:** `feat: i18n for Plan 5 screens — Plan 5 complete`

---

## Verification Checklist

- [ ] Backend builds with 0 errors
- [ ] All backend tests pass (~45 total)
- [ ] Frontend builds with 0 errors (prod config)
- [ ] PrizePreset CRUD works (list, create, delete)
- [ ] PrizeAssignment upsert works (set prize for rank/scope)
- [ ] Prizes Board shows current prize assignments per slot
- [ ] Prize Editor: pick from suggestions, type custom, save
- [ ] Save-to-library checkbox creates a custom preset
- [ ] My Custom Prizes: add, delete custom prizes
- [ ] Admin-only enforcement on create/edit/delete
- [ ] All new strings in i18n files (en + hu)
