# TinyHeroes — Plan 10: Multi-Language Completion

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add German, French, and Spanish translation files and update the language picker to show all 5 supported languages per the design spec.

**Architecture:** The app uses `@ngx-translate/core` with an HTTP loader that fetches `assets/i18n/{lang}.json` on demand. Adding languages requires only creating the JSON file and listing the language in the profile picker. No backend changes needed.

**Tech Stack:** Angular 21, ngx-translate v17

---

## Context

The design spec requires 5 languages: 🇬🇧 English, 🇭🇺 Magyar, 🇩🇪 Deutsch, 🇫🇷 Français, 🇪🇸 Español. Currently only `en.json` and `hu.json` exist. The profile component (`frontend/src/app/features/settings/pages/profile.component.ts`) has a `languages` array with only 2 entries. The translate service is configured in `app.config.ts` with `defaultLanguage: 'en'` and an HTTP loader pointing at `./assets/i18n/`.

---

## Task Overview (2 Tasks)

| # | Task | Layer |
|---|------|-------|
| 1 | Create DE, FR, ES translation JSON files | Frontend |
| 2 | Update language picker + final build verification | Frontend |

---

### Task 1: Create DE, FR, ES Translation Files

**Files:**
- Create: `frontend/public/assets/i18n/de.json`
- Create: `frontend/public/assets/i18n/fr.json`
- Create: `frontend/public/assets/i18n/es.json`

Each file must have the exact same structure and keys as `en.json`, with values translated into the target language. The `APP_NAME` key stays "TinyHeroes" (brand name, not translated).

**Commit:** `feat: add German, French, Spanish translations`

---

### Task 2: Update Language Picker + Final Verification

**Files:**
- Modify: `frontend/src/app/features/settings/pages/profile.component.ts`

Replace the `languages` array (currently 2 entries):
```typescript
languages = [
  { code: 'en', flag: '🇬🇧', name: 'English' },
  { code: 'hu', flag: '🇭🇺', name: 'Magyar' },
];
```

With:
```typescript
languages = [
  { code: 'en', flag: '🇬🇧', name: 'English' },
  { code: 'hu', flag: '🇭🇺', name: 'Magyar' },
  { code: 'de', flag: '🇩🇪', name: 'Deutsch' },
  { code: 'fr', flag: '🇫🇷', name: 'Français' },
  { code: 'es', flag: '🇪🇸', name: 'Español' },
];
```

**Verification:**
1. `cd frontend && npx ng build --configuration production` — 0 errors
2. `cd backend && dotnet test` — 61 tests pass

**Commit:** `feat: 5-language picker in profile — Plan 10 complete`

---

## Verification Checklist

- [ ] `de.json` has all keys matching `en.json` structure
- [ ] `fr.json` has all keys matching `en.json` structure
- [ ] `es.json` has all keys matching `en.json` structure
- [ ] Profile language picker shows 5 languages
- [ ] Frontend builds with 0 errors
- [ ] Backend tests: 61 passed
