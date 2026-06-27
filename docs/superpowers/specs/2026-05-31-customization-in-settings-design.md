# Customization in Settings — Design

**Date:** 2026-05-31
**Status:** Approved

## Problem

Customization features — deed presets, custom prizes, prize assignments, and prize rules — are scattered across unrelated routes. Parents currently stumble on them via feature flows (the "manage" link inside `add-deed`, the prize assignment board's suggestions hint, the editor's "save to library" toggle). There is no single place to go to configure the app.

## Goal

Every customization capability reachable from the Settings screen, with no regressions on the existing prize and deed flows.

## What changes

### 1. Settings screen — Customization section

`settings.component.ts` gains a "Customization" section header between "Family settings" and "Sign out", containing three new navigation cards plus the existing Prize Rules card relocated here:

| Card | Route | Visibility |
|---|---|---|
| 📋 Deed presets | `/settings/manage-presets` | All parents |
| 🎁 Custom prizes | `/settings/custom-prizes` | All parents |
| 🎯 Prize rules | `/settings/prize-rules` | Admin only (badge shown) |

Prize Rules keeps its existing subtitle and admin badge. The Prizes tab in the bottom nav (`/prizes`) is unaffected.

### 2. Route moves

Two routes are renamed to sit under `/settings/`:

| Old route | New route | Component |
|---|---|---|
| `/manage-presets` | `/settings/manage-presets` | `ManagePresetsComponent` |
| `/prizes/custom` | `/settings/custom-prizes` | `CustomPrizesComponent` |

`app.routes.ts` is updated accordingly. Both old routes are removed — no redirects needed since no external links exist.

### 3. Back navigation — `Location.back()`

Both moved components currently hardcode their back destination:

- `ManagePresetsComponent`: `<a routerLink="/dashboard">` back link at bottom of template
- `CustomPrizesComponent`: `router.navigate(['/prizes'])` in the back button handler

Both are changed to call `Location.back()` (Angular `@angular/common`). This makes back navigation context-aware: arriving from settings returns to settings; arriving from `add-deed` returns to `add-deed`.

`PrizeRulesComponent` already navigates to `/settings` on back/save — no change needed.

### 4. Link updates in `add-deed` and `prize-editor`

`add-deed.component.ts` has two `routerLink="/manage-presets"` references (the "Manage" label link and the `+` tile). Both are updated to `/settings/manage-presets`.

`prize-editor.component.ts` has two `router.navigate(['/prizes/custom'])` calls (a "Manage all" button and an empty-state button). Both are updated to `/settings/custom-prizes`.

### 5. i18n

Three new translation keys are added to all five locale files (`en`, `hu`, `de`, `fr`, `es`):

| Key | English value |
|---|---|
| `SETTINGS.CUSTOMIZATION_SECTION` | `Customization` |
| `SETTINGS.DEED_PRESETS` | `Deed presets` |
| `SETTINGS.CUSTOM_PRIZES` | `Custom prizes` |

## What does NOT change

- `/prizes`, `/prizes/edit`, `/prizes/editor` routes and `PrizesComponent` — untouched
- Bottom nav — untouched
- `ManagePresetsComponent` and `CustomPrizesComponent` logic — only back navigation changes
- Prize Rules component logic — unchanged
- Admin-only guard on Prize Rules card in settings — unchanged

## Tests

- No new backend tests required (no API changes)
- Frontend: update existing route paths in any component tests that reference `/manage-presets` or `/prizes/custom`
- Verify `Location.back()` in component tests by providing a `SpyLocation` and asserting `back()` is called on the button click
