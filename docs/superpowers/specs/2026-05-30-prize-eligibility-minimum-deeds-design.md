# Prize Eligibility — Minimum Deeds Design

**Date:** 2026-05-30  
**Status:** Approved

---

## UI Mockups

HTML mockups are in [`docs/superpowers/specs/assets/`](assets/). Open in a browser.

| Screen | File | Description |
|---|---|---|
| 1–3 | [full-design.html](assets/full-design.html) | Settings hub card → Prize Rules page → Weekly podium (approved flow) |
| 4 | [monthly-champion.html](assets/monthly-champion.html) | Monthly champion with eligibility treatment |
| 5 | [responsive-layouts.html](assets/responsive-layouts.html) | Prize Rules page + podium at mobile / tablet / desktop |
| 6 | [empty-state.html](assets/empty-state.html) | Motivational empty state when nobody meets the minimum |

---

## Context

Parents want a way to raise the bar before prizes are awarded. Without a minimum, every child who logs even a single deed is technically eligible for a weekly or monthly prize regardless of effort. This feature lets admins set a per-period deed floor; children who don't reach it are visually excluded from prizes while still appearing on the podium so their progress is visible and they're motivated to improve.

---

## Decisions Summary

| Question | Decision |
|---|---|
| Where is the setting? | New "Prize Rules" card in the Settings hub (admin-only) |
| UI control | +/− spinners; 0 = disabled (everyone qualifies) |
| Ineligible treatment on podium | Dimmed / greyscale + red banner listing who needs how many more deeds |
| Prize rank behaviour | Ranks compress — eligible child wins their prize; ineligible slots show "Unclaimed" |
| Empty state (nobody qualifies) | Motivational: sleepy emoji + "No prizes this week/month" + per-child progress ("needs X more deeds") |
| Monthly prize | Single winner only (champion); same eligibility rules apply |

---

## Data Model Changes

### Backend — `Family` entity

Add two nullable integer columns to `Family`:

```csharp
public int? WeeklyMinDeeds { get; set; }   // null or 0 = no minimum
public int? MonthlyMinDeeds { get; set; }  // null or 0 = no minimum
```

Add an EF Core migration: `AddPrizeMinDeeds`.

### DTOs

- `FamilyResponse` — add `weeklyMinDeeds: number | null` and `monthlyMinDeeds: number | null`
- New request DTO `SetPrizeRulesRequest { WeeklyMinDeeds: int?, MonthlyMinDeeds: int? }` — validation: values must be ≥ 0

### API endpoint

`PATCH /api/families/mine/prize-rules` (admin only)  
Accepts `SetPrizeRulesRequest`, updates the two fields, returns updated `FamilyResponse`.

---

## Eligibility Logic

A helper method (alongside `RankingHelper`) determines eligibility per period:

```
IsEligible(child, minDeeds) =>
  minDeeds == null || minDeeds == 0 || child.deedCount >= minDeeds
```

This is applied:
- In the **weekly podium** using `WeeklyMinDeeds` against `ChildStatsResponse.WeeklyCount`
- In the **monthly champion** using `MonthlyMinDeeds` against the monthly deed count
- Both are computed client-side from data already returned by existing endpoints — no new API calls needed

---

## Frontend Changes

### 1. Settings hub (`settings.component.ts`)

Add a new navigation card:

```
🎯 Prize Rules   [ADMIN badge]
   Minimum deeds to qualify   ›
```

Card is only shown when the current user is an admin (`familyMember.role === 'Admin'`). Route: `/settings/prize-rules`.

### 2. New component: `PrizeRulesComponent` (`/settings/pages/prize-rules.component.ts`)

- Loads family data via `FamilyService` (already has `family` signal)
- Displays two rows in a card: "🏅 Weekly prize" and "🏆 Monthly prize"
- Each row has a label, subtitle, and `+`/`−` spinner bound to a local signal
- Minimum value: 0. No maximum enforced in UI.
- "Save changes" button calls `PATCH /api/families/mine/prize-rules`
- Responsive: on mobile the rows stack vertically; on tablet/desktop they sit side-by-side in a 2-column grid
- Route added to `settings.routes.ts`: `{ path: 'prize-rules', component: PrizeRulesComponent, canActivate: [authGuard] }`

### 3. Weekly podium (`podium.component.ts`)

Inject `FamilyService` and call `familyService.loadFamily()` in `ngOnInit` (if not already loaded). The `family()` signal is then available for the eligibility check.

New computed signal:

```ts
eligibleRankings = computed(() =>
  this.rankings().map(r => ({
    ...r,
    eligible: this.isEligible(r.deedCount, this.family()?.weeklyMinDeeds)
  }))
)
```

**Podium visual:**
- Ineligible child: `opacity-35`, dashed avatar border, greyscale medal (`filter: grayscale(1)`)
- Eligible child: unchanged (full colour)

**Red banner** (shown only when `weeklyMinDeeds > 0` AND at least one child is ineligible):

> ⚠️ **Bob** and **Charlie** need at least **5 deeds** to qualify for a prize this week.

**Prize slots:**
- Eligible rank-1/2/3 child → show prize as today
- Ineligible rank position → replace prize row content with "🔒 Unclaimed" at `opacity-40`

**Empty state** (all children ineligible OR no deeds at all):

> 😴 No prizes this week  
> All children need at least **5 deeds**. Keep going!  
> _(per-child list: "Alice — 3 deeds · needs 2 more")_

### 4. Monthly champion (`monthly-champion.component.ts`)

Inject `FamilyService` (same as weekly podium above). Same pattern:

- `champion()` computed already finds rank-1 — add eligibility check against `monthlyMinDeeds`
- If champion is ineligible: show empty state instead of champion card
- Red banner when champion's deed count is shown alongside ineligible children in the ranking list
- Monthly prize slot shows "🔒 Unclaimed" if no eligible champion

**Empty state:**

> 🏆 No champion yet  
> Children need at least **20 deeds** to win the monthly prize. The month isn't over yet — keep earning!

### 5. `FamilyService`

- Update `FamilyResponse` model to include `weeklyMinDeeds` and `monthlyMinDeeds`
- Add `updatePrizeRules(req: SetPrizeRulesRequest): Observable<Family>` method calling the new PATCH endpoint

### 6. i18n

Add translation keys to all locale files (`en`, `hu`, `de`, `fr`, `es`):

```
settings.prizeRules.title
settings.prizeRules.subtitle
settings.prizeRules.weeklyLabel
settings.prizeRules.weeklySubtitle
settings.prizeRules.monthlyLabel
settings.prizeRules.monthlySubtitle
settings.prizeRules.save
podium.ineligibleBanner        (parameterised: names + minDeeds)
podium.noQualifiers.weekly     (parameterised: minDeeds)
podium.noQualifiers.monthly    (parameterised: minDeeds)
podium.needsMoreDeeds          (parameterised: count)
podium.unclaimed
```

---

## Responsive Behaviour

| Screen | Prize Rules page | Podium |
|---|---|---|
| Mobile (< 640px) | Rows stack vertically | Single column, banner full width |
| Tablet (640–1024px) | 2-column grid side by side | Podium + prize list side by side |
| Desktop (> 1024px) | 2-column grid, max-width 680px | Same as tablet |

No layout changes needed for the Settings hub card itself — it uses the same card list pattern already responsive.

---

## Files to Create / Modify

**New:**
- `backend/TinyHeroes.Infrastructure/Migrations/<timestamp>_AddPrizeMinDeeds.cs`
- `frontend/src/app/features/settings/pages/prize-rules.component.ts`

**Modified:**
- `backend/TinyHeroes.Domain/Entities/Family.cs` — add two fields
- `backend/TinyHeroes.Application/DTOs/Family/FamilyDtos.cs` — add fields to response + new request DTO
- `backend/TinyHeroes.Api/Controllers/FamilyController.cs` — add PATCH endpoint
- `frontend/src/app/core/models/family.model.ts` — add two fields
- `frontend/src/app/core/services/family.service.ts` — add `updatePrizeRules()`
- `frontend/src/app/features/settings/pages/settings.component.ts` — add Prize Rules card
- `frontend/src/app/features/settings/settings.routes.ts` — add route
- `frontend/src/app/features/podium/pages/podium.component.ts` — eligibility logic + dimming + banner + empty state
- `frontend/src/app/features/podium/pages/monthly-champion.component.ts` — same
- `frontend/public/assets/i18n/en.json` (and hu, de, fr, es) — new keys

---

## Verification

1. Set weekly minimum to 5 via Settings → Prize Rules → save
2. Add 7 deeds for Alice, 3 for Bob → weekly podium: Alice full colour, Bob dimmed, red banner, 🥈 slot shows "Unclaimed"
3. Set minimum to 0 → all children eligible, banner disappears
4. Add no deeds for any child (or all below minimum) → motivational empty state with per-child progress shown
5. Set monthly minimum to 20 → monthly champion screen reflects same logic
6. Non-admin user: "Prize Rules" card is not visible in Settings hub
7. Run `dotnet test` — existing integration tests should pass (no behaviour change when `WeeklyMinDeeds` is null)
