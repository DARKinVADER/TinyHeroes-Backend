# TinyHeroes — User Documentation Page Design

**Date:** 2026-05-31
**Status:** Approved

---

## Overview

A publicly accessible `/help` route in the Angular app that provides end-user documentation for parents. Fully translated into all 5 supported languages (EN, HU, DE, FR, ES) using the existing i18n system. No authentication required.

---

## Architecture

### Route
- Path: `/help`
- Guard: none (public)
- Component: `HelpComponent` — standalone, lazy-loaded
- Location: `frontend/src/app/features/help/help.component.ts`
- Added to `app.routes.ts` alongside other unauthenticated routes (`/login`, `/signup`)

### No backend changes
Entirely frontend. No API calls.

---

## Page Structure

### Three zones

1. **Header bar** (sticky, `position: sticky; top: 0`)
   - Left: TinyHeroes logo + app name, links to `/`
   - Centre: "Help & Guide" page title
   - Right: language picker (`<select>`) — calls `TranslateService.use(lang)` on change, does NOT write to `localStorage` so it doesn't override a logged-in user's saved preference

2. **Sticky sidebar** (desktop ≥768px, `position: sticky; top: 72px`)
   - Lists all section headings as anchor links
   - Active section highlighted via `IntersectionObserver` scroll-spy
   - Hidden on mobile (`hidden md:block`)

3. **Content area**
   - Mobile: "Jump to section" `<select>` anchor dropdown replaces sidebar
   - Numbered-step layout (Option C from design review):
     - Circle number + heading
     - Screenshot indented below the heading
     - Caption below the screenshot

---

## Sections

| # | Section | Key content |
|---|---------|-------------|
| 1 | Getting Started | Sign up, create family (name + week-start day) |
| 2 | Adding Heroes | Add child, choose emoji avatar or upload photo |
| 3 | Logging Good Deeds | Tap hero card, pick preset or custom deed, AI image |
| 4 | Weekly Podium | View rankings, end-of-week awards, 1st/2nd/3rd prizes |
| 5 | Monthly Champion | Last day of month, grand prize, eligibility threshold |
| 6 | Prizes | Built-in suggestions, custom prizes, claiming prizes |
| 7 | Invite a Co-parent | Settings → Invite, share link, co-parent role |
| 8 | Language & Settings | Language picker, profile, family settings |
| 9 | FAQ & Troubleshooting | 5–8 Q&A pairs covering common questions |

---

## Screenshots

- Stored at `public/assets/docs/screenshots/`
- One PNG per key screen: `signup.png`, `create-family.png`, `dashboard.png`, `add-deed.png`, `podium.png`, `monthly.png`, `prizes.png`, `invite.png`, `settings.png`
- Single set of English screenshots shared across all languages (maintaining 5 sets is impractical; screens are mostly visual)
- Placeholder grey boxes used during development; replaced with real screenshots before release

---

## Translations

New `HELP` namespace added to all five `public/assets/i18n/*.json` files.

Key structure:
```
HELP.PAGE_TITLE
HELP.PAGE_SUBTITLE
HELP.NAV_LABEL             ← "Contents" sidebar heading
HELP.JUMP_TO               ← "Jump to section" mobile label
HELP.SECTION_*             ← section headings (9 sections)
HELP.STEP_*                ← step headings
HELP.CAPTION_*             ← screenshot captions (1–2 sentences)
HELP.FAQ_Q_* / FAQ_A_*     ← FAQ question/answer pairs
HELP.SCREENSHOT_NOTE       ← note that screenshots are in English
```

Prose kept short (captions: 1–2 sentences; FAQ answers: 2–4 sentences) to stay within JSON value conventions.

---

## Language Switching

- `HelpComponent` injects `TranslateService`
- Language picker `<select>` in header calls `translate.use(lang)` on `(change)`
- Does NOT persist to `localStorage` — avoids overriding the authenticated user's saved `th_preferred_lang`
- Page re-renders reactively via `translate` pipe on all text

---

## Responsive Behaviour

| Breakpoint | Sidebar | Screenshot width |
|------------|---------|-----------------|
| ≥768px (md) | Sticky left column, 220px | max-width: 320px |
| <768px | Hidden; replaced by `<select>` anchor dropdown | 100% |

---

## Scroll Spy

`IntersectionObserver` watches each section's heading element. When a section enters the viewport, the corresponding sidebar link receives an `active` class. Falls back gracefully (no active highlight) if `IntersectionObserver` is unavailable.

---

## Files Changed

| File | Change |
|------|--------|
| `frontend/src/app/app.routes.ts` | Add `/help` route |
| `frontend/src/app/features/help/help.component.ts` | New component (create) |
| `frontend/public/assets/i18n/en.json` | Add `HELP` namespace |
| `frontend/public/assets/i18n/hu.json` | Add `HELP` namespace |
| `frontend/public/assets/i18n/de.json` | Add `HELP` namespace |
| `frontend/public/assets/i18n/fr.json` | Add `HELP` namespace |
| `frontend/public/assets/i18n/es.json` | Add `HELP` namespace |
| `frontend/public/assets/docs/screenshots/` | Add placeholder screenshots (create dir) |

---

## Out of Scope

- Video tutorials or animated walkthroughs
- A "What's New" changelog section (can be added later)
- Separate screenshot sets per language
- Search within the help page
- Backend or API changes
