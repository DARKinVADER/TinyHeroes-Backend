# Customization in Settings Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Expose all customization features (deed presets, custom prizes, prize rules) directly from the Settings screen so parents can reach them without navigating through feature flows.

**Architecture:** Add a "Customization" section to the settings menu with three new cards. Move `/manage-presets` and `/prizes/custom` under `/settings/` to align the URL tree with the new entry point. Replace hardcoded back-navigation in both moved components with `Location.back()` so context-aware back works from any caller. Update the two callers inside feature flows (`add-deed`, `prize-editor`) to point at the new routes.

**Tech Stack:** Angular 21 standalone components, `@angular/common Location`, `@ngx-translate/core`, lazy routes in `app.routes.ts`.

---

## File Map

| File | Change |
|---|---|
| `frontend/src/app/app.routes.ts` | Remove `/manage-presets` and `/prizes/custom`; add `/settings/manage-presets` and `/settings/custom-prizes` |
| `frontend/src/app/features/settings/pages/settings.component.ts` | Add Customization section header + 3 cards; move Prize Rules card inside section |
| `frontend/src/app/features/deeds/pages/manage-presets.component.ts` | Replace back `<a routerLink="/dashboard">` with `Location.back()` button |
| `frontend/src/app/features/prizes/pages/custom-prizes.component.ts` | Replace `router.navigate(['/prizes'])` with `Location.back()` |
| `frontend/src/app/features/deeds/pages/add-deed.component.ts` | Update 2× `routerLink="/manage-presets"` → `/settings/manage-presets` |
| `frontend/src/app/features/prizes/pages/prize-editor.component.ts` | Update 2× `router.navigate(['/prizes/custom'])` → `/settings/custom-prizes` |
| `frontend/public/assets/i18n/en.json` | Add `SETTINGS.CUSTOMIZATION_SECTION`, `SETTINGS.DEED_PRESETS`, `SETTINGS.CUSTOM_PRIZES` |
| `frontend/public/assets/i18n/hu.json` | Same three keys, Hungarian values |
| `frontend/public/assets/i18n/de.json` | Same three keys, German values |
| `frontend/public/assets/i18n/fr.json` | Same three keys, French values |
| `frontend/public/assets/i18n/es.json` | Same three keys, Spanish values |

---

## Task 1: Update routes

**Files:**
- Modify: `frontend/src/app/app.routes.ts`

- [ ] **Step 1: Remove old routes and add new ones under settings**

In `app.routes.ts`, inside the authenticated `ShellComponent` children array, make two changes:

1. Remove the two old entries:
```typescript
{ path: 'prizes/custom', loadComponent: () => import('./features/prizes/pages/custom-prizes.component').then(m => m.CustomPrizesComponent) },
// ...
{ path: 'manage-presets', loadComponent: () => import('./features/deeds/pages/manage-presets.component').then(m => m.ManagePresetsComponent) },
```

2. Add the two new routes alongside the other `settings/` children:
```typescript
{ path: 'settings/manage-presets', loadComponent: () => import('./features/deeds/pages/manage-presets.component').then(m => m.ManagePresetsComponent) },
{ path: 'settings/custom-prizes', loadComponent: () => import('./features/prizes/pages/custom-prizes.component').then(m => m.CustomPrizesComponent) },
```

- [ ] **Step 2: Build to verify no import errors**

```bash
cd /Volumes/PersonalProtected/GIT/TinyHeroes/frontend && npx ng build --configuration production 2>&1 | tail -20
```
Expected: build completes with no errors (warnings about bundle size are fine).

- [ ] **Step 3: Commit**

```bash
git add frontend/src/app/app.routes.ts
git commit -m "refactor: move manage-presets and custom-prizes under settings routes"
```

---

## Task 2: Add i18n keys

**Files:**
- Modify: `frontend/public/assets/i18n/en.json`
- Modify: `frontend/public/assets/i18n/hu.json`
- Modify: `frontend/public/assets/i18n/de.json`
- Modify: `frontend/public/assets/i18n/fr.json`
- Modify: `frontend/public/assets/i18n/es.json`

- [ ] **Step 1: Add keys to en.json**

Inside the `"SETTINGS"` object, add after the `"HELP"` key:
```json
"CUSTOMIZATION_SECTION": "Customization",
"DEED_PRESETS": "Deed Presets",
"CUSTOM_PRIZES": "Custom Prizes"
```

- [ ] **Step 2: Add keys to hu.json**

Inside the `"SETTINGS"` object (Hungarian):
```json
"CUSTOMIZATION_SECTION": "Testreszabás",
"DEED_PRESETS": "Cselekedet sablonok",
"CUSTOM_PRIZES": "Egyéni jutalmak"
```

- [ ] **Step 3: Add keys to de.json**

Inside the `"SETTINGS"` object (German):
```json
"CUSTOMIZATION_SECTION": "Anpassung",
"DEED_PRESETS": "Tat-Vorlagen",
"CUSTOM_PRIZES": "Eigene Belohnungen"
```

- [ ] **Step 4: Add keys to fr.json**

Inside the `"SETTINGS"` object (French):
```json
"CUSTOMIZATION_SECTION": "Personnalisation",
"DEED_PRESETS": "Modèles d'actions",
"CUSTOM_PRIZES": "Récompenses personnalisées"
```

- [ ] **Step 5: Add keys to es.json**

Inside the `"SETTINGS"` object (Spanish):
```json
"CUSTOMIZATION_SECTION": "Personalización",
"DEED_PRESETS": "Plantillas de actos",
"CUSTOM_PRIZES": "Premios personalizados"
```

- [ ] **Step 6: Commit**

```bash
git add frontend/public/assets/i18n/
git commit -m "feat: add i18n keys for customization section in settings"
```

---

## Task 3: Update settings.component.ts

**Files:**
- Modify: `frontend/src/app/features/settings/pages/settings.component.ts`

The current template has this flat structure (after the Help card was added):
- Invite co-parent
- My profile
- Family settings
- Prize Rules (admin-only, sits at the same level)
- Help
- Sign out

The new structure groups Prize Rules with the two new cards under a "Customization" section header, inserted between "Family settings" and "Help".

- [ ] **Step 1: Restructure the template**

Replace the entire `<div class="space-y-3">` block and the sign-out `<div class="mt-6">` with:

```html
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

<p class="text-xs font-bold text-brand-muted uppercase tracking-wide mt-6 mb-3 px-1">{{ 'SETTINGS.CUSTOMIZATION_SECTION' | translate }}</p>

<div class="space-y-3">
  <a routerLink="/settings/manage-presets" class="block bg-white rounded-xl border border-brand-border p-4 hover:bg-brand-cream transition-colors">
    <div class="flex items-center justify-between">
      <div class="flex items-center gap-3">
        <span class="text-xl">📋</span>
        <span class="font-medium text-brand-text">{{ 'SETTINGS.DEED_PRESETS' | translate }}</span>
      </div>
      <span class="text-brand-muted">→</span>
    </div>
  </a>
  <a routerLink="/settings/custom-prizes" class="block bg-white rounded-xl border border-brand-border p-4 hover:bg-brand-cream transition-colors">
    <div class="flex items-center justify-between">
      <div class="flex items-center gap-3">
        <span class="text-xl">🎁</span>
        <span class="font-medium text-brand-text">{{ 'SETTINGS.CUSTOM_PRIZES' | translate }}</span>
      </div>
      <span class="text-brand-muted">→</span>
    </div>
  </a>
  @if (isAdmin()) {
    <a routerLink="/settings/prize-rules" class="block bg-white rounded-xl border border-brand-border p-4 hover:bg-brand-cream transition-colors">
      <div class="flex items-center justify-between">
        <div class="flex items-center gap-3">
          <span class="text-xl">🎯</span>
          <div>
            <div class="flex items-center gap-2">
              <span class="font-medium text-brand-text">{{ 'SETTINGS.PRIZE_RULES_TITLE' | translate }}</span>
              <span class="bg-brand-purple text-white text-xs px-1.5 py-0.5 rounded-full font-bold uppercase tracking-wide">{{ 'SETTINGS.ADMIN_BADGE' | translate }}</span>
            </div>
            <p class="text-xs text-brand-muted mt-0.5">{{ 'SETTINGS.PRIZE_RULES_CARD_SUBTITLE' | translate }}</p>
          </div>
        </div>
        <span class="text-brand-muted">→</span>
      </div>
    </a>
  }
</div>

<div class="mt-6 space-y-3">
  <a routerLink="/help" class="block bg-white rounded-xl border border-brand-border p-4 hover:bg-brand-cream transition-colors">
    <div class="flex items-center justify-between">
      <div class="flex items-center gap-3">
        <span class="text-xl">❓</span>
        <span class="font-medium text-brand-text">{{ 'SETTINGS.HELP' | translate }}</span>
      </div>
      <span class="text-brand-muted">→</span>
    </div>
  </a>
</div>
```

Keep the sign-out button and version footer that follow — they are unchanged.

- [ ] **Step 2: Build to verify**

```bash
cd /Volumes/PersonalProtected/GIT/TinyHeroes/frontend && npx ng build --configuration production 2>&1 | tail -20
```
Expected: build completes with no errors.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/app/features/settings/pages/settings.component.ts
git commit -m "feat: add customization section to settings menu"
```

---

## Task 4: Fix back navigation in ManagePresetsComponent

**Files:**
- Modify: `frontend/src/app/features/deeds/pages/manage-presets.component.ts`

Currently has `<a routerLink="/dashboard" class="text-sm text-brand-orange font-medium">{{ 'PRESET.BACK' | translate }}</a>` at line 78. Replace with a button that calls `Location.back()`.

- [ ] **Step 1: Inject Location and replace back link**

Add `Location` to imports and inject it. Replace the `<a routerLink="/dashboard">` anchor with a `<button>`:

```typescript
import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { Location } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { PresetService } from '../../../core/services/preset.service';
```

In the class body, add:
```typescript
private location = inject(Location);
```

In the template, replace:
```html
<a routerLink="/dashboard" class="text-sm text-brand-orange font-medium">{{ 'PRESET.BACK' | translate }}</a>
```
with:
```html
<button (click)="location.back()" class="text-sm text-brand-orange font-medium">{{ 'PRESET.BACK' | translate }}</button>
```

Remove the `RouterLink` import from the `imports` array since it is no longer used in this component.

- [ ] **Step 2: Build to verify**

```bash
cd /Volumes/PersonalProtected/GIT/TinyHeroes/frontend && npx ng build --configuration production 2>&1 | tail -20
```
Expected: build completes with no errors.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/app/features/deeds/pages/manage-presets.component.ts
git commit -m "refactor: use Location.back() in ManagePresetsComponent"
```

---

## Task 5: Fix back navigation in CustomPrizesComponent

**Files:**
- Modify: `frontend/src/app/features/prizes/pages/custom-prizes.component.ts`

Currently the back button calls `router.navigate(['/prizes'])`. Replace with `Location.back()`.

- [ ] **Step 1: Inject Location and replace back navigation**

Add `Location` to imports. In the class body replace `router = inject(Router)` — `Router` is still needed for nothing else, so check first:

```bash
grep -n "router\." /Volumes/PersonalProtected/GIT/TinyHeroes/frontend/src/app/features/prizes/pages/custom-prizes.component.ts
```

The only `router` usage is the back button. So remove the `Router` injection entirely and replace with `Location`:

```typescript
import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { Location } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { PrizeService } from '../../../core/services/prize.service';
```

In the class body, replace `router = inject(Router)` with:
```typescript
private location = inject(Location);
```

In the template, replace:
```html
<button (click)="router.navigate(['/prizes'])" class="text-brand-muted text-lg">←</button>
```
with:
```html
<button (click)="location.back()" class="text-brand-muted text-lg">←</button>
```

- [ ] **Step 2: Build to verify**

```bash
cd /Volumes/PersonalProtected/GIT/TinyHeroes/frontend && npx ng build --configuration production 2>&1 | tail -20
```
Expected: build completes with no errors.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/app/features/prizes/pages/custom-prizes.component.ts
git commit -m "refactor: use Location.back() in CustomPrizesComponent"
```

---

## Task 6: Update links in add-deed.component.ts

**Files:**
- Modify: `frontend/src/app/features/deeds/pages/add-deed.component.ts`

Two `routerLink="/manage-presets"` references at lines 35 and 47.

- [ ] **Step 1: Update both routerLink references**

Change both occurrences from `/manage-presets` to `/settings/manage-presets`:

Line 35:
```html
<a routerLink="/settings/manage-presets" class="text-xs text-brand-orange font-medium">{{ 'DEED.MANAGE_LINK' | translate }}</a>
```

Line 47:
```html
<a routerLink="/settings/manage-presets" class="flex flex-col items-center justify-center gap-1 p-2 rounded-xl bg-gray-50 border border-dashed border-brand-border text-brand-muted">
```

- [ ] **Step 2: Build to verify**

```bash
cd /Volumes/PersonalProtected/GIT/TinyHeroes/frontend && npx ng build --configuration production 2>&1 | tail -20
```
Expected: build completes with no errors.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/app/features/deeds/pages/add-deed.component.ts
git commit -m "fix: update manage-presets links to new settings route"
```

---

## Task 7: Update links in prize-editor.component.ts

**Files:**
- Modify: `frontend/src/app/features/prizes/pages/prize-editor.component.ts`

Two `router.navigate(['/prizes/custom'])` calls at lines 61 and 71.

- [ ] **Step 1: Update both navigate calls**

Line 61:
```html
<button (click)="router.navigate(['/settings/custom-prizes'])" class="text-xs text-purple-600 font-bold">{{ 'PRIZES.MANAGE_ALL' | translate }}</button>
```

Line 71 (the empty-state button — find by the surrounding context `router.navigate(['/prizes/custom'])`):
```html
<button (click)="router.navigate(['/settings/custom-prizes'])"
```

- [ ] **Step 2: Build to verify**

```bash
cd /Volumes/PersonalProtected/GIT/TinyHeroes/frontend && npx ng build --configuration production 2>&1 | tail -20
```
Expected: build completes with no errors.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/app/features/prizes/pages/prize-editor.component.ts
git commit -m "fix: update custom-prizes links to new settings route"
```

---

## Task 8: Manual smoke test

No automated component tests exist for these components. Verify the full flow in the browser.

- [ ] **Step 1: Start dev server**

```bash
cd /Volumes/PersonalProtected/GIT/TinyHeroes/frontend && npm start
```

- [ ] **Step 2: Verify settings screen**

Open http://localhost:4200/settings. Confirm:
- "Invite co-parent", "My profile", "Family settings" appear at the top (no section label above them)
- A "Customization" section label appears below "Family settings"
- "Deed Presets" and "Custom prizes" cards appear under the section label
- "Prize Rules" with Admin badge appears under the section label (if logged in as admin)
- "Help & Guide" appears below the Customization group
- "Sign out" appears at the bottom

- [ ] **Step 3: Verify Deed Presets back navigation from settings**

Click "Deed Presets" → arrives at `/settings/manage-presets`. Click the back link → returns to `/settings`. ✓

- [ ] **Step 4: Verify Deed Presets back navigation from add-deed**

Navigate to a child's profile → tap "Add deed" → click "Manage" or the `+` preset tile → arrives at `/settings/manage-presets`. Click back → returns to the add-deed screen (not settings). ✓

- [ ] **Step 5: Verify Custom Prizes back navigation from settings**

From settings, click "Custom prizes" → arrives at `/settings/custom-prizes`. Click `←` → returns to `/settings`. ✓

- [ ] **Step 6: Verify Custom Prizes back navigation from prize-editor**

Navigate to `/prizes` → click Edit on any slot → in the prize editor, click "Manage all" or the empty-state custom prizes button → arrives at `/settings/custom-prizes`. Click `←` → returns to prize editor. ✓

- [ ] **Step 7: Commit smoke test confirmation**

```bash
git commit --allow-empty -m "chore: manual smoke test passed for customization-in-settings"
```
