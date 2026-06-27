# Prize Claim Status and Comments on Load — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Pre-fetch prize claim data (status + comments) for all visible history summaries on page load, so `isUsed` and `comments` are shown immediately without requiring the user to expand a card.

**Architecture:** Add an Angular `effect()` in `HistoryComponent` that reacts to the `weeks` and `months` signals. When summaries populate (after the HTTP response), it eagerly fetches claims for every summary that has at least one prize-eligible podium entry. `loadClaimsIfNeeded()` already guards against duplicate fetches via the `claimsMap`, so calling it multiple times is safe. No backend changes required.

**Tech Stack:** Angular 18 signals + `effect()`, `@angular/core/rxjs-interop`, Vitest unit tests.

---

## Root Cause Recap

`ngOnInit()` calls `loadWeeks()` / `loadMonths()` which are fire-and-forget subscriptions. The `weeks` and `months` signals are empty until the HTTP response arrives. `loadClaimsIfNeeded()` is only called from `toggleExpand()`, so claims are never fetched on page load. Result: all prize cards show "Pending" with no comments on first render.

## File Structure

**Modified files:**
- `frontend/src/app/features/podium/pages/history.component.ts` — add `effect()` to eagerly load claims when summaries arrive; extract a `prizeEligibleSummaryIds()` helper

---

### Task 1: Add eager claim pre-fetch via `effect()`

**Files:**
- Modify: `frontend/src/app/features/podium/pages/history.component.ts`

- [ ] **Step 1: Write failing tests**

The component does not yet have a spec file. Create `frontend/src/app/features/podium/pages/history.component.spec.ts`:

```typescript
import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { signal } from '@angular/core';
import { of, Subject } from 'rxjs';
import { vi } from 'vitest';
import { provideRouter } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { HistoryComponent } from './history.component';
import { SummaryService } from '../../../core/services/summary.service';
import { ChildService } from '../../../core/services/child.service';
import { PrizeService } from '../../../core/services/prize.service';
import { PrizeClaimService } from '../../../core/services/prize-claim.service';

const WEEK = {
  id: 'w1',
  weekStart: '2026-06-02',
  weekEnd: '2026-06-08',
  rankings: [
    { childId: 'c1', childName: 'Alice', rank: 1, deedCount: 5 },
  ],
};

const MONTH = {
  id: 'm1',
  year: 2026,
  month: 6,
  championChildId: 'c1',
  championName: 'Alice',
  totalDeeds: 20,
};

const WEEKLY_PRIZE = { id: 'p1', scope: 'weekly', rank: 1, emoji: '🎁', label: 'Ice cream' };
const MONTHLY_PRIZE = { id: 'p2', scope: 'monthly', rank: null, emoji: '🏅', label: 'Movie night' };

describe('HistoryComponent — eager claim loading', () => {
  let getByWeekSpy: ReturnType<typeof vi.fn>;
  let getByMonthSpy: ReturnType<typeof vi.fn>;
  let weeksSignal: ReturnType<typeof signal<typeof WEEK[]>>;
  let monthsSignal: ReturnType<typeof signal<typeof MONTH[]>>;
  let assignmentsSignal: ReturnType<typeof signal<typeof WEEKLY_PRIZE[]>>;

  beforeEach(async () => {
    weeksSignal = signal<typeof WEEK[]>([]);
    monthsSignal = signal<typeof MONTH[]>([]);
    assignmentsSignal = signal<typeof WEEKLY_PRIZE[]>([]);
    getByWeekSpy = vi.fn().mockReturnValue(of([]));
    getByMonthSpy = vi.fn().mockReturnValue(of([]));

    await TestBed.configureTestingModule({
      imports: [HistoryComponent],
      providers: [
        provideRouter([]),
        provideTranslateService({ defaultLanguage: 'en' }),
        {
          provide: SummaryService,
          useValue: {
            weeks: weeksSignal,
            months: monthsSignal,
            loadWeeks: vi.fn(),
            loadMonths: vi.fn(),
          },
        },
        {
          provide: ChildService,
          useValue: { children: signal([]), loadChildren: vi.fn() },
        },
        {
          provide: PrizeService,
          useValue: {
            assignments: assignmentsSignal,
            loadAssignments: vi.fn(),
          },
        },
        {
          provide: PrizeClaimService,
          useValue: {
            getByWeekSummary: getByWeekSpy,
            getByMonthSummary: getByMonthSpy,
            createClaim: vi.fn().mockReturnValue(new Subject()),
            setUsed: vi.fn().mockReturnValue(new Subject()),
            addComment: vi.fn().mockReturnValue(new Subject()),
            deleteComment: vi.fn().mockReturnValue(new Subject()),
          },
        },
      ],
    }).compileComponents();
  });

  afterEach(() => vi.restoreAllMocks());

  it('fetches week claims eagerly when weeks signal populates with prize-eligible entries', fakeAsync(() => {
    const fixture = TestBed.createComponent(HistoryComponent);
    fixture.detectChanges();
    assignmentsSignal.set([WEEKLY_PRIZE as any]);
    weeksSignal.set([WEEK as any]);
    fixture.detectChanges();
    tick(); // flush effects
    fixture.detectChanges();
    expect(getByWeekSpy).toHaveBeenCalledWith('w1');
  }));

  it('does NOT fetch week claims for summaries with no prize-eligible entries (no deeds or no prize assignment)', fakeAsync(() => {
    const noDeeds = { ...WEEK, rankings: [{ childId: 'c1', childName: 'Alice', rank: 1, deedCount: 0 }] };
    const fixture = TestBed.createComponent(HistoryComponent);
    fixture.detectChanges();
    assignmentsSignal.set([WEEKLY_PRIZE as any]);
    weeksSignal.set([noDeeds as any]);
    fixture.detectChanges();
    tick();
    fixture.detectChanges();
    expect(getByWeekSpy).not.toHaveBeenCalled();
  }));

  it('fetches month claims eagerly when months signal populates with a champion and prize', fakeAsync(() => {
    const fixture = TestBed.createComponent(HistoryComponent);
    fixture.detectChanges();
    assignmentsSignal.set([MONTHLY_PRIZE as any]);
    monthsSignal.set([MONTH as any]);
    fixture.detectChanges();
    tick();
    fixture.detectChanges();
    expect(getByMonthSpy).toHaveBeenCalledWith('m1');
  }));

  it('does NOT fetch month claims when month has no champion', fakeAsync(() => {
    const noChampion = { ...MONTH, championChildId: null, championName: null };
    const fixture = TestBed.createComponent(HistoryComponent);
    fixture.detectChanges();
    assignmentsSignal.set([MONTHLY_PRIZE as any]);
    monthsSignal.set([noChampion as any]);
    fixture.detectChanges();
    tick();
    fixture.detectChanges();
    expect(getByMonthSpy).not.toHaveBeenCalled();
  }));

  it('does NOT re-fetch claims for a summary already in the cache', fakeAsync(() => {
    const fixture = TestBed.createComponent(HistoryComponent);
    fixture.detectChanges();
    assignmentsSignal.set([WEEKLY_PRIZE as any]);
    weeksSignal.set([WEEK as any]);
    fixture.detectChanges();
    tick();
    fixture.detectChanges();
    expect(getByWeekSpy).toHaveBeenCalledTimes(1);
    // update signal again (e.g. tab switch) — should not re-fetch
    weeksSignal.set([...weeksSignal()]);
    fixture.detectChanges();
    tick();
    fixture.detectChanges();
    expect(getByWeekSpy).toHaveBeenCalledTimes(1);
  }));
});
```

- [ ] **Step 2: Run tests to confirm they fail**

```bash
cd /Volumes/PersonalProtected/GIT/TinyHeroes/frontend && npx ng test --watch=false --include="src/app/features/podium/pages/history.component.spec.ts" 2>&1 | tail -20
```

Expected: FAIL — `getByWeekSpy` not called (eager loading not implemented yet).

- [ ] **Step 3: Implement the eager loading**

In `frontend/src/app/features/podium/pages/history.component.ts`, make the following changes:

**3a: Add `effect` to the import from `@angular/core`**

Change the existing import:
```typescript
import { Component, OnInit, computed, inject, signal } from '@angular/core';
```
To:
```typescript
import { Component, OnInit, computed, effect, inject, signal } from '@angular/core';
```

**3b: Add the `effect()` calls inside the constructor**

Add a `constructor()` to `HistoryComponent` (it doesn't have one yet). Place it right after the `monthlyPrize` computed property (after line ~300):

```typescript
constructor() {
  // Eagerly load claims for all prize-eligible weekly summaries
  effect(() => {
    const weeks = this.weeks();
    const assignments = this.prizeService.assignments();
    for (const week of weeks) {
      const hasPrize = this.topPodium(week.rankings).some(
        entry => assignments.some(a => a.scope === 'weekly' && a.rank === entry.rank)
      );
      if (hasPrize) {
        this.loadClaimsIfNeeded(week.id + '-1'); // key format: summaryId-rank (any rank works — loadClaimsIfNeeded only uses summaryId)
      }
    }
  });

  // Eagerly load claims for all monthly summaries with a champion and prize
  effect(() => {
    const months = this.months();
    const assignments = this.prizeService.assignments();
    const hasMonthlyPrize = assignments.some(a => a.scope === 'monthly' && a.rank === null);
    if (!hasMonthlyPrize) return;
    for (const month of months) {
      if (month.championChildId) {
        this.loadClaimsIfNeeded(month.id + '-monthly');
      }
    }
  });
}
```

**Key detail:** `loadClaimsIfNeeded(key)` uses `this.summaryIdFromKey(key)` which does `key.split('-')[0]`. So passing `week.id + '-1'` correctly extracts `week.id` as the summaryId. For months, `month.id + '-monthly'` ends with `-monthly` which triggers the `isMonthly` branch in `loadClaimsIfNeeded`.

Also change `loadClaimsIfNeeded` from `private` to `protected` or package-private (remove the `private` modifier) so the spec can call `onDocumentClick`-style — actually it doesn't need to be, since the effect calls it. Keep it `private`.

- [ ] **Step 4: Run tests to confirm they pass**

```bash
cd /Volumes/PersonalProtected/GIT/TinyHeroes/frontend && npx ng test --watch=false --include="src/app/features/podium/pages/history.component.spec.ts" 2>&1 | tail -20
```

Expected: all 5 tests PASS.

- [ ] **Step 5: Run full test suite to check for regressions**

```bash
cd /Volumes/PersonalProtected/GIT/TinyHeroes/frontend && npx ng test --watch=false 2>&1 | tail -10
```

Expected: all tests PASS.

- [ ] **Step 6: Commit**

```bash
cd /Volumes/PersonalProtected/GIT/TinyHeroes && git add frontend/src/app/features/podium/pages/history.component.ts frontend/src/app/features/podium/pages/history.component.spec.ts && git commit -m "fix: eagerly load prize claim status and comments on history page load"
```

---

### Task 2: Version bump and CHANGELOG

**Files:**
- Modify: `frontend/src/environments/environment.ts`
- Modify: `frontend/src/environments/environment.prod.ts`
- Modify: `frontend/src/environments/environment.integration.ts`
- Modify: `CHANGELOG.md`

- [ ] **Step 1: Bump version from 3.1.3 → 3.1.4 in all three environment files**

`frontend/src/environments/environment.ts`: `version: '3.1.4'`
`frontend/src/environments/environment.prod.ts`: `version: '3.1.4'`
`frontend/src/environments/environment.integration.ts`: `version: '3.1.4'`

- [ ] **Step 2: Add CHANGELOG entry**

Add above `## [3.1.3]` in `CHANGELOG.md`:

```markdown
## [3.1.4] - 2026-06-09

### Fixed
- Prize claim status (Used / Pending) and comments now load immediately when opening the History page, without requiring the user to expand a card first.

```

- [ ] **Step 3: Commit**

```bash
cd /Volumes/PersonalProtected/GIT/TinyHeroes && git add frontend/src/environments/environment.ts frontend/src/environments/environment.prod.ts frontend/src/environments/environment.integration.ts CHANGELOG.md && git commit -m "chore: bump frontend version to 3.1.4"
```
