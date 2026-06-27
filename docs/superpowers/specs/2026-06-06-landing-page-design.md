# Landing Page — Visual Refresh Design Spec (Issue #8)

## Goal

Replace the minimal auth-gate `WelcomeComponent` with a full-width, benefit-led landing page that communicates TinyHeroes' value to a first-time parent visitor and drives sign-up.

---

## Design Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Visual direction | Option C — 3D Depth | Floating step cards with stacked shadows, perspective-tilted CSS podium; playful and dimensional |
| Hero background | Brand-colour gradient | Uses `--color-brand-orange` → theme-aware automatically |
| Navigation | Wrap in `PublicLayoutComponent` | Consistent with Help, About, Privacy pages; sticky header + footer for free |
| Theme handling | Read `localStorage('th_theme')`, default Sunny | `ThemeService.init()` already handles this at bootstrap — zero extra code |
| Route | Stay at `/` | No route changes needed — restructure in place |

---

## Page Structure

```
┌─────────────────────────────────────────────┐
│  NAV (PublicLayoutComponent header)         │
│  🌟 TinyHeroes  Home Help About  [Log In]   │
├─────────────────────────────────────────────┤
│  HERO                                       │
│  Brand-colour gradient background           │
│  Benefit-led headline (LANDING.HERO_TITLE)  │
│  Subheadline (LANDING.HERO_SUBTITLE)        │
│  [Get Started ✨] CTA → /signup             │
│  3D CSS podium (perspective-tilted, emoji   │
│  avatars on gold/silver/bronze plinths)     │
├─────────────────────────────────────────────┤
│  HOW IT WORKS (LANDING.HOW_TITLE)           │
│  3 floating step cards with stacked shadow  │
│  + orange numbered badge (top-right)        │
│  📝 Track  🏆 Rankings  🎁 Prizes           │
├─────────────────────────────────────────────┤
│  FEATURE HIGHLIGHTS (LANDING.FEATURES_TITLE)│
│  3 layered-shadow cards                     │
│  Weekly Podium │ Monthly Champion │ Prizes  │
├─────────────────────────────────────────────┤
│  SOCIAL PROOF                               │
│  🎉 LANDING.SOCIAL_PROOF                    │
├─────────────────────────────────────────────┤
│  AUTH SECTION                               │
│  [Get Started →] → /signup                  │
│  Already have an account? Log In → /login   │
├─────────────────────────────────────────────┤
│  FOOTER (PublicLayoutComponent footer)      │
│  © TinyHeroes · Privacy · Terms             │
└─────────────────────────────────────────────┘
```

---

## Theming

The template uses only Tailwind theme tokens (`bg-brand-orange`, `text-brand-muted`, `bg-brand-bg`, `bg-brand-cream`, `border-brand-border`). The CSS custom properties are swapped by `data-theme` on `<body>`, so all four themes — Sunny, Ocean, Forest, Candy — work automatically with zero conditional logic in the component.

`ThemeService.init()` runs at app bootstrap (already wired in `app.ts`). It reads `localStorage('th_theme')` and defaults to `'sunny'`. Unauthenticated visitors with no stored preference see Sunny; returning users who previously chose Ocean/Forest/Candy see their theme on the landing page before logging in.

---

## 3D Depth Effects (CSS only)

### Podium
```
perspective: 400px on wrapper
rotateX(22deg) on inner flex container
Gold / Silver / Bronze plinth colours from brand palette
```

### Floating step cards
```
box-shadow: 0 4px 0 0 var(--color-brand-border),   ← stacked layer 1
            0 8px 0 0 var(--color-brand-border/60), ← stacked layer 2
            0 6px 16px rgba(0,0,0,0.08)             ← ambient
```

### Feature cards
```
box-shadow: 0 2px 0 var(--color-brand-border),
            0 4px 0 var(--color-brand-border/70),
            0 8px 20px rgba(0,0,0,0.08)
```

All effects are pure CSS — no JS, no images, no external libraries.

---

## i18n Keys (`LANDING.*`)

Add to all 5 locale files (`en`, `hu`, `de`, `fr`, `es`) under a new `"LANDING"` top-level key:

```json
"LANDING": {
  "HERO_TITLE": "Every good deed deserves a reward",
  "HERO_SUBTITLE": "Track your kids' good deeds, run weekly competitions, and award real prizes.",
  "HOW_TITLE": "How it works",
  "STEP_1_TITLE": "Track good deeds",
  "STEP_1_DESC": "Log any good deed in seconds — preset actions or custom ones.",
  "STEP_2_TITLE": "See the rankings",
  "STEP_2_DESC": "A weekly podium and monthly champion keep kids motivated.",
  "STEP_3_TITLE": "Earn real prizes",
  "STEP_3_DESC": "Parents assign weekly and monthly prizes to the winners.",
  "FEATURES_TITLE": "Everything your family needs",
  "FEATURE_PODIUM_TITLE": "Weekly Podium",
  "FEATURE_PODIUM_DESC": "1st, 2nd, and 3rd place every week — fair and transparent.",
  "FEATURE_CHAMPION_TITLE": "Monthly Champion",
  "FEATURE_CHAMPION_DESC": "The best performer each month earns the grand prize.",
  "FEATURE_PRIZES_TITLE": "Prize Board",
  "FEATURE_PRIZES_DESC": "Manage and track prizes from suggestion to delivery.",
  "SOCIAL_PROOF": "Families are already tracking good deeds with TinyHeroes"
}
```

---

## Files to Modify

| File | Change |
|---|---|
| `frontend/src/app/features/auth/pages/welcome.component.ts` | Replace template; wrap in `PublicLayoutComponent`; add `PublicLayoutComponent` to imports array |
| `frontend/public/assets/i18n/en.json` | Add `LANDING` block |
| `frontend/public/assets/i18n/hu.json` | Add `LANDING` block (Hungarian) |
| `frontend/public/assets/i18n/de.json` | Add `LANDING` block (German) |
| `frontend/public/assets/i18n/fr.json` | Add `LANDING` block (French) |
| `frontend/public/assets/i18n/es.json` | Add `LANDING` block (Spanish) |

**TypeScript class is unchanged** — `InfoService.load()` call and `version` property stay as-is. The `PublicLayoutComponent` footer renders Privacy and Terms links, so the manual footer inside the old template is removed. The version line (`v2.x.x · API v1.x.x`) moves into the bottom of the auth section as a small muted `<p>`, keeping the version info visible without duplicating the footer.

---

## Version Bump

Frontend: `2.5.0` (new feature). Update `environment.ts` and `environment.prod.ts`. Add CHANGELOG entry under `### Added`.

---

## Verification

| Criterion | How to check |
|---|---|
| All six sections visible | Scroll `/` — hero, how-it-works, features, social proof, auth, footer |
| Not constrained to `max-w-sm` | Sections span full viewport width on desktop |
| 3D podium renders | Hero bottom shows tilted podium with three emoji avatars |
| Step cards have depth | Stacked bottom shadow visible on the three how-it-works cards |
| Get Started → `/signup` | Click hero CTA and auth section CTA |
| Log In → `/login` | Click login link in auth section and nav header |
| Theme-aware | Set `localStorage('th_theme', 'ocean')`, reload — all colours shift to blue |
| i18n works | Switch to German via profile settings → all LANDING.* copy updates |
| Mobile | At 390px wide, all columns stack vertically, CTA is thumb-accessible |
