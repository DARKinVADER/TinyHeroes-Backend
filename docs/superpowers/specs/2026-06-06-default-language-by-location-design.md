# Design Spec: Default Language Based on Location (Issue #60)

**Date:** 2026-06-06
**Status:** Approved
**Issue:** https://github.com/DARKinVADER/TinyHeroes/issues/60

---

## Problem

The app currently always starts in English for every visitor. Users whose browser is configured for Hungarian, German, French, or Spanish must manually navigate to Profile Settings and change the language — even though the app already has full translations for all five languages.

---

## Solution

Detect the visitor's browser locale (`navigator.language`) on first visit and apply the matching language automatically. Add a language-selector pill to every navigation surface so users can override it at any time.

---

## Detection Logic

On app startup (`APP_INITIALIZER` in `app.config.ts`):

1. Check `localStorage` for `th_preferred_lang` (an explicit user choice).
2. If none, read `navigator.language` (e.g. `"hu-HU"`), extract the two-letter prefix (`"hu"`), and check against the supported set: `['en', 'hu', 'de', 'fr', 'es']`.
3. If the detected locale is in the set, use it. Otherwise fall back to `'en'`.

The detected language is **not** written to localStorage — only explicit user choices are persisted. This ensures the browser locale continues to take effect for new sessions until the user deliberately overrides it.

---

## Language Selector Component

A new standalone `LanguageSelectorComponent` is added to `frontend/src/app/shared/components/`.

**Pill UI:** `🇬🇧 EN ▼` — flag emoji + uppercase two-letter code + chevron.

**Dropdown:** Opens on click, lists all 5 languages as `flag + native name` (e.g. `🇭🇺 Magyar`). Active language shows an orange `✓`. Closes on outside click.

**`position` input:** `'down'` (default) or `'up'`. Controls whether the dropdown opens below or above the pill. The bottom-nav uses `position="up"` to prevent the dropdown from opening off-screen.

**Language switching:**
- Sets `th_preferred_lang` in localStorage.
- Calls `translate.use(code)`.
- If the user is logged in (i.e. `userService.profile()` is non-null), also calls `userService.updateProfile({ preferredLanguage: code })` so the choice is persisted to the backend.

**`onLangChange` sync:** Subscribes to `translate.onLangChange` so the pill reflects changes triggered by other sources (e.g. when `user.service.ts` applies the profile language after login).

---

## Placement

| Surface | Location |
|---|---|
| Public nav | Between the About link and the Log In link |
| Authenticated side-nav | Bottom of the nav column, pinned with `mt-auto` |
| Mobile bottom-nav | Fifth slot at the right end, `position="up"` |

The existing language picker in Profile Settings remains — it is the canonical preference control for logged-in users. The nav pills provide a quick-switch shortcut.

---

## Supported Languages

| Code | Flag | Native name |
|---|---|---|
| `en` | 🇬🇧 | English |
| `hu` | 🇭🇺 | Magyar |
| `de` | 🇩🇪 | Deutsch |
| `fr` | 🇫🇷 | Français |
| `es` | 🇪🇸 | Español |

---

## Out of Scope

- IP-based geolocation (decided against — `navigator.language` is sufficient and requires no external API).
- Adding new language translations (existing 5 languages only).
- Changing the language display in the dropdown to show English names (`Magyar` not `Hungarian`) — native names chosen for international friendliness.

---

## Version

Frontend: `2.7.0` → `2.8.0` (MINOR bump — new feature).
