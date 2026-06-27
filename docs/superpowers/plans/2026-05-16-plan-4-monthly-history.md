# TinyHeroes — Plan 4: Monthly Champion, History & Week Summaries

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add week/month summary persistence, a monthly champion page, and a history page — completing the competition tracking loop so families can view past results.

**Architecture:** WeekSummary and MonthSummary entities store finalized results. A summary endpoint computes and snapshots previous weeks/months on demand. Frontend adds Monthly Champion (Screen 11) and History (Screen 12) pages under the Podium tab with a tab navigation.

**Tech Stack:** ASP.NET Core 10, EF Core 10, Angular 21, Tailwind 4, ngx-translate

---

## Context

Plan 3 (Good Deeds, Presets & Podium) is complete. We have: deed creation, presets, live weekly podium with rankings, dashboard with deed counts. Plan 4 adds persistence of week results, monthly aggregation, and historical views. After this plan, families can look back at any past week/month's results.

**Design spec:** `docs/superpowers/specs/2026-05-16-tinyheroes-design.md` — Screens 11, 12, enhancement to Podium tab navigation.

**Deferred to Plan 5:** Prizes (PrizePreset entity, Prizes Board, Prize Editor). Prize-related fields in summaries stay null/empty until then.

---

## Architecture Decisions

1. **On-demand summary generation.** No background cron. When `GET /api/summaries/weeks` is called, the backend checks if any past weeks since family creation are missing summaries and generates them. Same for months. Simple, testable, no scheduler infrastructure.
2. **WeekSummary.Rankings stored as JSON string.** Contains `[{ childId, childName, deedCount, rank }]`. Denormalized for fast reads — we don't need to join or query individual rankings.
3. **MonthSummary computed from WeekSummaries.** Champion = child with most total deeds across all weeks in that month. If no weeks are finalized for the month, it's not generated.
4. **Podium tab navigation.** The `/podium` route becomes a parent with children: `this-week` (existing live podium), `monthly` (champion), `history`. Tab bar at top.
5. **No prizes in summaries yet.** `PrizesAwarded` field will be a nullable JSON string, set to null until Plan 5 adds prize configuration.

---

## Task Overview (9 Tasks)

| # | Task | Layer |
|---|------|-------|
| 1 | WeekSummary entity + EF config | Backend |
| 2 | MonthSummary entity + EF config | Backend |
| 3 | Summary generation service | Backend |
| 4 | Summary controller + tests | Backend |
| 5 | Podium tab navigation restructure | Frontend |
| 6 | Monthly Champion page (Screen 11) | Frontend |
| 7 | History page (Screen 12) | Frontend |
| 8 | Summary service (frontend) | Frontend |
| 9 | i18n strings (en + hu) + final build verification | Frontend |

---

### Task 1: WeekSummary Entity + EF Configuration

**Files:**
- Create: `backend/TinyHeroes.Domain/Entities/WeekSummary.cs`
- Create: `backend/TinyHeroes.Infrastructure/Data/Configurations/WeekSummaryConfiguration.cs`
- Modify: `backend/TinyHeroes.Infrastructure/Data/AppDbContext.cs` — add `DbSet<WeekSummary>`

```csharp
// Domain/Entities/WeekSummary.cs
namespace TinyHeroes.Domain.Entities;

public class WeekSummary
{
    public Guid Id { get; set; }
    public Guid FamilyId { get; set; }
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public string Rankings { get; set; } = "[]";
    public string? PrizesAwarded { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Family Family { get; set; } = null!;
}
```

**WeekSummaryConfiguration:**
- FK to Family (cascade delete)
- Unique index on (FamilyId, WeekStart)
- Rankings has no max length (JSON blob)

**Commit:** `feat: WeekSummary entity with EF configuration`

---

### Task 2: MonthSummary Entity + EF Configuration

**Files:**
- Create: `backend/TinyHeroes.Domain/Entities/MonthSummary.cs`
- Create: `backend/TinyHeroes.Infrastructure/Data/Configurations/MonthSummaryConfiguration.cs`
- Modify: `backend/TinyHeroes.Infrastructure/Data/AppDbContext.cs` — add `DbSet<MonthSummary>`

```csharp
// Domain/Entities/MonthSummary.cs
namespace TinyHeroes.Domain.Entities;

public class MonthSummary
{
    public Guid Id { get; set; }
    public Guid FamilyId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public Guid? ChampionChildId { get; set; }
    public string? ChampionName { get; set; }
    public int TotalDeeds { get; set; }
    public string Rankings { get; set; } = "[]";
    public string? PrizeAwarded { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Family Family { get; set; } = null!;
    public Child? Champion { get; set; }
}
```

**MonthSummaryConfiguration:**
- FK to Family (cascade delete)
- Optional FK to Child (ChampionChildId, no cascade)
- Unique index on (FamilyId, Year, Month)

**Commit:** `feat: MonthSummary entity with EF configuration`

---

### Task 3: Summary Generation Service

**Files:**
- Create: `backend/TinyHeroes.Application/Services/ISummaryService.cs`
- Create: `backend/TinyHeroes.Infrastructure/Services/SummaryService.cs`
- Modify: `backend/TinyHeroes.Api/Program.cs` — register `ISummaryService`

This is the first service in the Application layer (previous tasks used direct DbContext access). The summary computation logic warrants a service.

```csharp
// Application/Services/ISummaryService.cs
namespace TinyHeroes.Application.Services;

public interface ISummaryService
{
    Task GenerateMissingWeekSummaries(Guid familyId);
    Task GenerateMissingMonthSummaries(Guid familyId);
}
```

**SummaryService logic:**

`GenerateMissingWeekSummaries`:
1. Get family (with WeekStartDay)
2. Find the earliest deed date for this family (or family creation date if no deeds)
3. Iterate week by week from that start to the most recent completed week (not current week)
4. For each week, check if WeekSummary exists for that (familyId, weekStart). If not:
   - Query deed counts per child for that week range
   - Build rankings JSON: `[{ childId, childName, deedCount, rank }]` sorted by deedCount desc
   - Insert WeekSummary

`GenerateMissingMonthSummaries`:
1. Get all completed months (from earliest deed to last completed month, not current month)
2. For each month, check if MonthSummary exists. If not:
   - Sum deed counts across all weeks in that month (query GoodDeeds directly for the month range)
   - Champion = child with highest total
   - Build rankings JSON
   - Insert MonthSummary

**Program.cs registration:**
```csharp
builder.Services.AddScoped<ISummaryService, SummaryService>();
```

**Commit:** `feat: summary generation service`

---

### Task 4: Summary Controller + Tests

**Files:**
- Create: `backend/TinyHeroes.Application/DTOs/Summary/WeekSummaryResponse.cs`
- Create: `backend/TinyHeroes.Application/DTOs/Summary/MonthSummaryResponse.cs`
- Create: `backend/TinyHeroes.Application/DTOs/Summary/RankingEntry.cs`
- Create: `backend/TinyHeroes.Api/Controllers/SummaryController.cs`
- Create: `backend/TinyHeroes.Tests/Integration/SummaryControllerTests.cs`

**DTOs (all in one file `WeekSummaryResponse.cs`):**
```csharp
namespace TinyHeroes.Application.DTOs.Summary;

public record WeekSummaryResponse(Guid Id, DateTime WeekStart, DateTime WeekEnd, List<RankingEntry> Rankings);
public record MonthSummaryResponse(Guid Id, int Year, int Month, string? ChampionName, Guid? ChampionChildId, int TotalDeeds, List<RankingEntry> Rankings);
public record RankingEntry(Guid ChildId, string ChildName, int DeedCount, int Rank);
```

**Endpoints:**
- `GET /api/summaries/weeks` — triggers generation of missing summaries, returns all WeekSummaries for user's family ordered by WeekStart desc
- `GET /api/summaries/months` — triggers generation, returns all MonthSummaries ordered by Year desc, Month desc
- `GET /api/summaries/current-month` — returns live stats for the current (incomplete) month: deed counts per child for the current month (no persistence, computed on the fly)

**Tests (5):**
1. `GetWeeks_WithNoDeeds_ReturnsEmptyList`
2. `GetWeeks_WithPastDeeds_GeneratesSummary` — create deeds with CreatedAt in the past (previous week), call GET, assert summary generated
3. `GetMonths_WithPastDeeds_GeneratesSummary` — create deeds in a past month, call GET, verify champion
4. `GetCurrentMonth_ReturnsLiveStats`
5. `GetWeeks_WithoutFamily_Returns400`

**Important for tests:** To test past-week summaries, you need to insert GoodDeeds with `CreatedAt` set to a past date. Since we use InMemory DB, you can't use the API (it sets CreatedAt = UtcNow). Instead, access the DB directly in the test via the factory's service scope:
```csharp
using var scope = factory.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
// Insert deed with past CreatedAt directly
```

**Commit:** `feat: summary endpoints with integration tests`

---

### Task 5: Podium Tab Navigation Restructure

**Files:**
- Create: `frontend/src/app/features/podium/components/podium-tabs.component.ts`
- Create: `frontend/src/app/features/podium/pages/podium-shell.component.ts`
- Modify: `frontend/src/app/features/podium/pages/podium.component.ts` — becomes child route "this-week"
- Modify: `frontend/src/app/app.routes.ts` — restructure podium routes

**Route restructure:**
```
/podium → PodiumShellComponent (tabs + router-outlet)
  /podium/this-week → PodiumComponent (existing)
  /podium/monthly → MonthlyChampionComponent (Task 6)
  /podium/history → HistoryComponent (Task 7)
```

Default redirect: `/podium` → `/podium/this-week`

**PodiumShellComponent:**
```typescript
template: `
  <app-podium-tabs />
  <router-outlet />
`
```

**PodiumTabsComponent:** Three tab links (This Week, Monthly, History) with active styling.

**Commit:** `feat: podium tab navigation restructure`

---

### Task 6: Monthly Champion Page (Screen 11)

**Files:**
- Create: `frontend/src/app/features/podium/pages/monthly-champion.component.ts`
- Modify: `frontend/src/app/app.routes.ts` — add monthly route (if not done in Task 5)

**UI (Screen 11):**
- Dark gradient header (navy-to-indigo/purple) with stars
- Month label (current or latest completed month)
- Large trophy emoji + champion avatar with gold border
- Champion name, "Champion of [Month]" subtitle
- Total deed count pill
- Full monthly ranking: all children with monthly deed totals
- Empty state if no champion yet

**Data:** Calls summary service `GET /api/summaries/current-month` for live current-month data.

**Commit:** `feat: monthly champion page`

---

### Task 7: History Page (Screen 12)

**Files:**
- Create: `frontend/src/app/features/podium/pages/history.component.ts`
- Modify: `frontend/src/app/app.routes.ts` — add history route (if not done in Task 5)

**UI (Screen 12):**
- Weekly / Monthly toggle at top
- **Weekly view:** List of past weeks, most recent first. Each card shows: week dates, top 3 children with medal + avatar + name + deed count. Expandable/collapsible.
- **Monthly view:** List of past months. Each card: month name, champion avatar + name + total deeds.
- Empty state: "No history yet — complete a week to see results here!"

**Data:** Calls `GET /api/summaries/weeks` and `GET /api/summaries/months`.

**Commit:** `feat: history page with weekly/monthly views`

---

### Task 8: Summary Service (Frontend)

**Files:**
- Create: `frontend/src/app/core/models/summary.model.ts`
- Create: `frontend/src/app/core/services/summary.service.ts`

**Models:**
```typescript
export interface WeekSummaryDto {
  id: string;
  weekStart: string;
  weekEnd: string;
  rankings: RankingEntry[];
}

export interface MonthSummaryDto {
  id: string;
  year: number;
  month: number;
  championName: string | null;
  championChildId: string | null;
  totalDeeds: number;
  rankings: RankingEntry[];
}

export interface RankingEntry {
  childId: string;
  childName: string;
  deedCount: number;
  rank: number;
}

export interface CurrentMonthStats {
  rankings: RankingEntry[];
  month: number;
  year: number;
}
```

**SummaryService:**
```typescript
@Injectable({ providedIn: 'root' })
export class SummaryService {
  private _weeks = signal<WeekSummaryDto[]>([]);
  private _months = signal<MonthSummaryDto[]>([]);
  readonly weeks = this._weeks.asReadonly();
  readonly months = this._months.asReadonly();

  loadWeeks() { /* GET /api/summaries/weeks */ }
  loadMonths() { /* GET /api/summaries/months */ }
  getCurrentMonth() { return this.http.get<CurrentMonthStats>(...); }
}
```

**Note:** This task creates the service. Tasks 6 and 7 consume it. If implementing sequentially, this task should come before 6 and 7. However, the subagent executing Task 6 can create the service inline if needed.

**Commit:** `feat: summary service and models`

---

### Task 9: i18n Strings + Final Build Verification

**Files:**
- Modify: `frontend/public/assets/i18n/en.json` — add MONTHLY, HISTORY sections
- Modify: `frontend/public/assets/i18n/hu.json` — Hungarian translations

**New i18n keys (en):**
```json
"MONTHLY": {
  "TITLE": "Monthly Champion",
  "CHAMPION_OF": "Champion of {{month}}",
  "TOTAL_DEEDS": "{{count}} total deeds",
  "RANKING": "Full ranking",
  "EMPTY_TITLE": "No champion yet!",
  "EMPTY_SUBTITLE": "Complete a full month to crown a champion",
  "WEEKS_WON": "weeks won"
},
"HISTORY": {
  "TITLE": "History",
  "TAB_WEEKLY": "Weekly",
  "TAB_MONTHLY": "Monthly",
  "WEEK_OF": "Week of {{start}} – {{end}}",
  "DEEDS": "deeds",
  "CHAMPION": "Champion",
  "EMPTY_TITLE": "No history yet",
  "EMPTY_SUBTITLE": "Complete a week to see results here!"
},
"PODIUM_TABS": {
  "THIS_WEEK": "This Week",
  "MONTHLY": "Monthly",
  "HISTORY": "History"
}
```

**Hungarian:**
```json
"MONTHLY": {
  "TITLE": "Havi bajnok",
  "CHAMPION_OF": "{{month}} bajnoka",
  "TOTAL_DEEDS": "{{count}} cselekedet összesen",
  "RANKING": "Teljes rangsor",
  "EMPTY_TITLE": "Még nincs bajnok!",
  "EMPTY_SUBTITLE": "Teljesíts egy teljes hónapot a bajnok koronázásához",
  "WEEKS_WON": "hetet nyert"
},
"HISTORY": {
  "TITLE": "Előzmények",
  "TAB_WEEKLY": "Heti",
  "TAB_MONTHLY": "Havi",
  "WEEK_OF": "{{start}} – {{end}} hete",
  "DEEDS": "cselekedet",
  "CHAMPION": "Bajnok",
  "EMPTY_TITLE": "Még nincs előzmény",
  "EMPTY_SUBTITLE": "Teljesíts egy hetet az eredmények megjelenéséhez!"
},
"PODIUM_TABS": {
  "THIS_WEEK": "Ez a hét",
  "MONTHLY": "Havi",
  "HISTORY": "Előzmények"
}
```

**Verification:**
1. `cd backend && dotnet test` — all tests pass (~36 total)
2. `cd frontend && npx ng build --configuration production` — 0 errors

**Commit:** `feat: i18n for Plan 4 screens — Plan 4 complete`

---

## Verification Checklist

- [ ] Backend builds with 0 errors
- [ ] All backend tests pass (~36 total)
- [ ] Frontend builds with 0 errors (prod config)
- [ ] Summary generation creates WeekSummary for past weeks
- [ ] Monthly champion computed from deed totals
- [ ] Podium tabs navigate between This Week / Monthly / History
- [ ] Monthly Champion page shows current month leader
- [ ] History page shows past weeks and months
- [ ] All new strings in i18n files (en + hu)
