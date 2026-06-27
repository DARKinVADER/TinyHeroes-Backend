# Theme Selection — Design Spec

**Issue:** #57  
**Date:** 2026-06-06  
**Status:** Approved

---

## Summary

Users can choose from four visual themes. Each theme overrides the app's color palette and font. The preference is stored per user (like `preferredLanguage`) and restored on every app open. Selecting a theme applies instantly with no save button.

---

## Themes

| Key | Name | Personality | Primary | Background | Accent | Font |
|---|---|---|---|---|---|---|
| `sunny` | 🌞 Sunny | Warm, default | `#F97316` | `#FFF3E8` | `#22C55E` | System sans-serif (unchanged) |
| `ocean` | 🌊 Ocean | Cool, calm | `#0EA5E9` | `#E0F7FA` | `#14B8A6` | Nunito (Google Fonts) |
| `forest` | 🌿 Forest | Natural, earthy | `#16A34A` | `#F0FDF4` | `#D97706` | Quicksand (Google Fonts) |
| `candy` | 🍭 Candy | Playful, vivid | `#EC4899` | `#FDF2F8` | `#8B5CF6` | Baloo 2 (Google Fonts) |

`sunny` is the default and is always present. Its `@theme` block in `styles.css` is the baseline — it is not changed.

---

## Architecture

### CSS — `frontend/src/styles.css`

The existing `@theme` block remains as the Sunny default. Three additional `[data-theme]` override blocks are appended. Each block reassigns the full set of `--color-brand-*` variables and `--font-sans`.

```css
[data-theme="ocean"] {
  --color-brand-orange: #0EA5E9;
  --color-brand-green:  #14B8A6;
  --color-brand-purple: #818CF8;
  --color-brand-cream:  #BAE6FD;
  --color-brand-bg:     #E0F7FA;
  --color-brand-border: #BAE6FD;
  --color-brand-text:   #0C4A6E;
  --color-brand-muted:  #0369A1;
  --font-sans: 'Nunito', sans-serif;
}

[data-theme="forest"] {
  --color-brand-orange: #16A34A;
  --color-brand-green:  #D97706;
  --color-brand-purple: #7C3AED;
  --color-brand-cream:  #DCFCE7;
  --color-brand-bg:     #F0FDF4;
  --color-brand-border: #BBF7D0;
  --color-brand-text:   #14532D;
  --color-brand-muted:  #166534;
  --font-sans: 'Quicksand', sans-serif;
}

[data-theme="candy"] {
  --color-brand-orange: #EC4899;
  --color-brand-green:  #8B5CF6;
  --color-brand-purple: #06B6D4;
  --color-brand-cream:  #FCE7F3;
  --color-brand-bg:     #FDF2F8;
  --color-brand-border: #FBCFE8;
  --color-brand-text:   #831843;
  --color-brand-muted:  #9D174D;
  --font-sans: 'Baloo 2', sans-serif;
}
```

Because Tailwind v4 emits each `@theme` token as a CSS custom property, every existing utility (`bg-brand-bg`, `text-brand-text`, etc.) resolves through these variables automatically. **No template changes are required.**

Google Fonts `<link>` tags for Nunito, Quicksand, and Baloo 2 are added to `frontend/src/index.html`.

### ThemeService — `frontend/src/app/core/services/theme.service.ts`

New singleton service. Responsibilities:
- Expose a readonly `current` signal (string).
- `init()` — reads `localStorage('th_theme')`, defaults to `'sunny'`, sets `document.body.setAttribute('data-theme', value)`. Called once from `AppComponent.ngOnInit()`.
- `apply(theme: string)` — updates the signal, writes localStorage, sets the body attribute. Called from the profile picker; the caller additionally persists to the backend via `UserService.updateProfile`.

The `themes` array is the single source of truth for valid theme keys and display metadata (name, emoji, swatches).

### Backend — `User` entity

New field on `TinyHeroes.Domain.Entities.User`:

```csharp
public string PreferredTheme { get; set; } = "sunny";
```

Exposed via the existing `UserProfile` DTO and accepted via the existing `UpdateProfileRequest` DTO. No new endpoint. One EF Core migration required.

`UserService.loadProfile()` already syncs `preferredLanguage` by calling `this.translate.use()` in its success handler. `PreferredTheme` follows the same pattern: `UserService` injects `ThemeService` and calls `themeService.apply(p.preferredTheme)` in the same handler. This ensures the backend theme is applied whenever `loadProfile()` is called — on any device, without callers needing to know about theming.

---

## UI

The theme picker lives in the **Profile page → Preferences section**, as a new row between Language and Push Notifications. It follows the identical expand/collapse pattern of the existing Language picker.

**Collapsed state:** shows 🎨 icon, "Theme" label, current theme's accent dot + name, chevron.

**Expanded state:** inline list of all four themes. Each row shows three color swatches (primary · background · accent), emoji + name, and a ✓ checkmark on the active theme.

Tapping a theme row:
1. Calls `themeService.apply(theme)` → instant live preview.
2. Calls `userService.updateProfile({ preferredTheme: theme })` in the background.
3. Collapses the picker.

No save button, no navigation — same interaction model as language selection.

The Settings index page gains a Theme entry in the Customization section linking to `/settings/profile`, with a 🎨 icon. No dedicated `/settings/theme` route is needed.

---

## Data flow

```
App opens
  ├── AppComponent.ngOnInit()
  │     └── themeService.init()           ← reads localStorage, sets data-theme instantly
  └── UserService.loadProfile() (called by ProfileComponent or any consumer)
        └── on success: themeService.apply(profile.preferredTheme)  ← syncs from backend

User taps a theme in Profile picker
  └── themeService.apply(theme)           ← instant visual change
        └── userService.updateProfile({ preferredTheme: theme })    ← persists in background
```

---

## Testing

- `ThemeService` unit test: `init()` reads localStorage and sets `data-theme`; `apply()` updates signal, localStorage, and `data-theme`.
- `ProfileComponent` test: selecting a theme calls `themeService.apply` and `userService.updateProfile` with correct args.
- Backend: `UpdateProfile` handler test for `PreferredTheme` field round-trip.

---

## Out of scope

- Per-family theme (Admin-set default) — deferred, not needed for v1.
- Border-radius personality per theme — deferred.
- Custom user-defined colors — deferred.
- Theme preview before applying — the live apply is the preview.
