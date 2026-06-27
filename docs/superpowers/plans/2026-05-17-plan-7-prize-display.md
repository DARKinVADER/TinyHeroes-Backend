# TinyHeroes — Plan 7: Prize Display on Podium, Monthly Champion & History

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Display the currently-configured prize assignments on the Weekly Podium, Monthly Champion, and History pages so parents and children can see what prizes are up for grabs or were won.

**Architecture:** Frontend-only. `PrizeService` (from Plan 5) already loads assignments from `GET /api/prize-assignments`. We inject it into the three Podium tab pages and display prize info alongside rankings. No backend changes — prizes shown are the *current* configured prizes (if prizes change later, historical display will reflect the new prizes; that's acceptable for MVP).

**Tech Stack:** Angular 21, Tailwind 4, ngx-translate

---

## Context

Plans 1–6 are complete. All 15 screens are implemented. The prize system (Plan 5) lets admins configure which prizes 1st/2nd/3rd place win each week and the monthly champion wins. However, the Podium, Monthly Champion, and History pages don't yet show this prize information.

**Design reference:**
- `docs/superpowers/screens/screens-updates.html` — History with prizes (Screen 12 updated), shows "Prize won:" badge under each top-3 entry in history cards, and prize+champion info in monthly rows
- Weekly Podium should show a "Prizes this week" section below the full ranking
- Monthly Champion should show the monthly prize card after the champion card

**Existing prize models** (from `frontend/src/app/core/models/prize.model.ts`):
```typescript
interface PrizeAssignmentDto {
  id: string; scope: string; rank: number | null; emoji: string; label: string;
}
```

**PrizeService** (from `frontend/src/app/core/services/prize.service.ts`):
- `assignments` — readonly signal of `PrizeAssignmentDto[]`
- `loadAssignments()` — loads from GET /api/prize-assignments

---

## Task Overview (3 Tasks)

| # | Task | Layer |
|---|------|-------|
| 1 | Prize cards on Weekly Podium (Screen 10) | Frontend |
| 2 | Prize card on Monthly Champion (Screen 11) | Frontend |
| 3 | Prize badges in History (Screen 12) + i18n + build | Frontend |

---

### Task 1: Prize Cards on Weekly Podium

**Files:**
- Modify: `frontend/src/app/features/podium/pages/podium.component.ts`

**What to add:** Below the Full Ranking list (inside the `@else` block that shows when `hasDeeds()`), add a "Prizes this week" section. If no prizes are configured (all assignments empty), show nothing.

**Key logic:**
- Inject `PrizeService` (from `../../../core/services/prize.service`)
- `call prizeService.loadAssignments()` in `ngOnInit`
- `weeklyPrizes = computed(() => ...)` — returns array of weekly prize assignments sorted by rank
- Only show the prizes section if at least one weekly prize is configured

**Add these computed signals** to the component class:
```typescript
private prizeService = inject(PrizeService);

weeklyPrizes = computed(() =>
  this.prizeService.assignments()
    .filter(a => a.scope === 'weekly')
    .sort((a, b) => (a.rank ?? 0) - (b.rank ?? 0))
);

hasWeeklyPrizes = computed(() => this.weeklyPrizes().some(p => p.label));
```

**Add this template block** after the `<!-- Full Ranking List -->` div closing tag (inside the `@else` block):
```html
<!-- Prizes this week -->
@if (hasWeeklyPrizes()) {
  <h2 class="text-sm font-medium text-brand-text mt-6 mb-3">{{ 'PODIUM.PRIZES_THIS_WEEK' | translate }}</h2>
  <div class="bg-white rounded-xl border border-brand-border divide-y divide-brand-border">
    @for (prize of weeklyPrizes(); track prize.rank) {
      <div class="flex items-center gap-3 p-3">
        <span class="text-lg">{{ prize.rank === 1 ? '🥇' : prize.rank === 2 ? '🥈' : '🥉' }}</span>
        <span class="text-xs font-bold text-brand-muted uppercase tracking-wide flex-1">
          {{ prize.rank === 1 ? ('PRIZES.FIRST_PLACE' | translate) : prize.rank === 2 ? ('PRIZES.SECOND_PLACE' | translate) : ('PRIZES.THIRD_PLACE' | translate) }}
        </span>
        <span class="text-sm font-bold text-brand-text">{{ prize.emoji }} {{ prize.label }}</span>
      </div>
    }
  </div>
}
```

**Imports to add:** `PrizeService` import, `TranslateModule` to the component's `imports` array.

**In ngOnInit**, add:
```typescript
this.prizeService.loadAssignments();
```

**Commit:** `feat: show prize cards on weekly podium`

---

### Task 2: Prize Card on Monthly Champion

**Files:**
- Modify: `frontend/src/app/features/podium/pages/monthly-champion.component.ts`

**What to add:** After the champion card div (below the `inline-block` deed count pill), add a monthly prize card. Only show if a monthly prize assignment exists.

**Add these computed signals** to the component class:
```typescript
private prizeService = inject(PrizeService);

monthlyPrize = computed(() =>
  this.prizeService.assignments().find(a => a.scope === 'monthly') ?? null
);
```

**Add this template block** immediately after the champion card's closing `</div>` (before the `<!-- Full Ranking -->` h3):
```html
<!-- Monthly Prize Card -->
@if (monthlyPrize()) {
  <div class="bg-gradient-to-br from-yellow-50 to-amber-50 rounded-xl border-2 border-yellow-300 p-4 mt-4 flex items-center gap-3">
    <span class="text-2xl">🏅</span>
    <div class="flex-1">
      <p class="text-xs font-bold text-yellow-700 uppercase tracking-wide">{{ 'PRIZES.MONTHLY_TITLE' | translate }}</p>
      <p class="text-sm font-bold text-brand-text mt-0.5">{{ monthlyPrize()!.emoji }} {{ monthlyPrize()!.label }}</p>
    </div>
  </div>
}
```

**Imports to add:** `PrizeService` import, `TranslateModule` to the component's `imports` array.

**In ngOnInit**, add:
```typescript
this.prizeService.loadAssignments();
```

**Commit:** `feat: show prize card on monthly champion`

---

### Task 3: Prize Badges in History + i18n + Build Verification

**Files:**
- Modify: `frontend/src/app/features/podium/pages/history.component.ts`
- Modify: `frontend/public/assets/i18n/en.json`
- Modify: `frontend/public/assets/i18n/hu.json`

#### History Component Changes

**Add these signals/computed** to the component class:
```typescript
private prizeService = inject(PrizeService);

getWeeklyPrize(rank: number): string {
  const assignment = this.prizeService.assignments().find(a => a.scope === 'weekly' && a.rank === rank);
  return assignment ? `${assignment.emoji} ${assignment.label}` : '';
}

monthlyPrize = computed(() =>
  this.prizeService.assignments().find(a => a.scope === 'monthly') ?? null
);
```

**In ngOnInit**, add:
```typescript
this.prizeService.loadAssignments();
```

**Imports:** Add `PrizeService` import, `TranslateModule` to `imports` array.

**In the weekly tab template**, update each rank row in `topThree()` to show prize info below the row. Change:
```html
@for (entry of topThree(week.rankings); track entry.childId; let i = $index) {
  <div class="flex items-center gap-2">
    <span class="text-base w-5">{{ i === 0 ? '🥇' : i === 1 ? '🥈' : '🥉' }}</span>
    <div class="w-7 h-7 rounded-full bg-brand-cream flex items-center justify-center text-sm">
      {{ getAvatar(entry.childId) }}
    </div>
    <span class="flex-1 text-sm text-brand-text">{{ entry.childName }}</span>
    <span class="text-sm font-bold text-brand-orange">{{ entry.deedCount }}</span>
    <span class="text-xs text-brand-muted">deeds</span>
  </div>
}
```

To:
```html
@for (entry of topThree(week.rankings); track entry.childId; let i = $index) {
  <div class="mb-2">
    <div class="flex items-center gap-2">
      <span class="text-base w-5">{{ i === 0 ? '🥇' : i === 1 ? '🥈' : '🥉' }}</span>
      <div class="w-7 h-7 rounded-full bg-brand-cream flex items-center justify-center text-sm">
        {{ getAvatar(entry.childId) }}
      </div>
      <span class="flex-1 text-sm text-brand-text">{{ entry.childName }}</span>
      <span class="text-sm font-bold text-brand-orange">{{ entry.deedCount }}</span>
      <span class="text-xs text-brand-muted">deeds</span>
    </div>
    @if (getWeeklyPrize(i + 1)) {
      <div class="flex items-center gap-1.5 ml-7 mt-1">
        <span class="text-xs">🎁</span>
        <span class="text-xs text-brand-muted">{{ 'HISTORY.PRIZE_WON' | translate }}</span>
        <span class="text-xs font-semibold text-brand-text">{{ getWeeklyPrize(i + 1) }}</span>
      </div>
    }
  </div>
}
```

**In the monthly tab template**, update month cards to show the monthly prize beneath the champion. After the champion display `</div>` (the `flex items-center gap-3` outer div), add:
```html
@if (month.championName && monthlyPrize()) {
  <div class="flex items-center gap-1.5 mt-2 pt-2 border-t border-brand-border">
    <span class="text-sm">🏅</span>
    <span class="text-xs text-brand-muted">{{ 'HISTORY.PRIZE_WON' | translate }}</span>
    <span class="text-xs font-semibold text-brand-text">{{ monthlyPrize()!.emoji }} {{ monthlyPrize()!.label }}</span>
  </div>
}
```

#### i18n Changes

**en.json** — Add to `PODIUM` section:
```json
"PRIZES_THIS_WEEK": "Prizes this week"
```

Add to `HISTORY` section:
```json
"PRIZE_WON": "Prize won:"
```

**hu.json** — Add to `PODIUM` section:
```json
"PRIZES_THIS_WEEK": "Heti jutalmak"
```

Add to `HISTORY` section:
```json
"PRIZE_WON": "Nyert jutalom:"
```

#### Verification

1. Run: `cd backend && dotnet test`
   Expected: 54 tests pass (no backend changes in this plan).

2. Run: `cd frontend && npx ng build --configuration production`
   Expected: 0 errors.

**Commit:** `feat: prize badges in history, i18n — Plan 7 complete`

---

## Verification Checklist

- [ ] Frontend builds with 0 errors (prod config)
- [ ] Backend tests: 54 passed
- [ ] Weekly Podium shows "Prizes this week" section with 1st/2nd/3rd place prizes (when configured)
- [ ] Weekly Podium hides prizes section when no prizes are configured
- [ ] Monthly Champion shows monthly prize card beneath the champion info (when configured)
- [ ] History weekly cards show "Prize won: [emoji] [label]" under each of the top 3 entries
- [ ] History monthly cards show monthly prize beneath the champion info
- [ ] Prize display is hidden when prizes aren't configured (no empty prize rows)
- [ ] All new i18n strings in en.json and hu.json
