# TinyHeroes — Product Design Specification

## Context

Parents want a fun, motivating way to track and reward their children's good behaviour and positive actions. TinyHeroes is a family web app where parents log "good deeds" for each child, compete in weekly and monthly rankings, and assign prizes to winners. The goal is to encourage good behaviour through celebration rather than punishment — making it visible, trackable, and rewarding for the whole family.

---

## Design Language

**Style:** Hybrid of Playful & Colorful + Storybook & Warm  
**Primary color:** `#F97316` (warm orange)  
**Secondary:** `#22C55E` (green), `#A855F7` (purple)  
**Background:** `#FFF8F0` (cream)  
**Text:** `#3D2B1F` (dark warm brown)  
**Cards:** White with `#F0E4D4` borders and warm shadows  
**Typography:** System sans-serif, bold weights, rounded feel  
**Icons:** Emoji-based throughout — accessible for young non-readers  

---

## Platform

Fully responsive web app — works equally well on mobile and desktop browsers. Mobile-first layout with bottom navigation on small screens.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Frontend | Angular 21 (standalone by default, signals) |
| Styling | TailwindCSS (custom warm/playful theme) |
| i18n | @ngx-translate/core |
| Backend | ASP.NET Core 10 Web API |
| ORM | Entity Framework Core 10 + Npgsql |
| Auth | ASP.NET Core Identity + JWT + OAuth2 (Google, Apple, Facebook) |
| Database | PostgreSQL 16 |
| Storage | FluentStorage (provider-agnostic: local disk / AWS S3 / Azure Blob / GCS — swap by config) |
| AI images | IAiImageService abstraction (default: Hugging Face Inference API — FLUX.1-schnell) |
| Containerization | Docker + docker compose |
| Frontend container | nginx |

---

## Architecture Principles

| Principle | Application |
|---|---|
| **Clean Architecture** | 4-layer backend: Domain (entities, enums, repository interfaces, zero deps) → Application (services, DTOs, exceptions, service interfaces) → Infrastructure (EF Core, repos, Identity, FluentStorage, Hugging Face) → Api (thin controllers, middleware, DI wiring). Dependencies point inward. |
| **SOLID** | Single responsibility per service/repo/controller. Open/closed via new services without modifying existing. Liskov-substitutable repos and storage. Interface segregation between storage/AI/auth services. Dependency inversion — Domain defines interfaces, Infrastructure implements. |
| **Smart/Dumb Components** | Frontend pages/ (inject services, manage state) vs components/ (pure @Input/@Output rendering). Feature-scoped services with signal-based state. |
| **Feature-Scoped Architecture** | Angular lazy-loaded feature routes. Each feature owns its pages/, components/, and services/. Shared presentational components in shared/. |

---

## Multi-language Support

- Language auto-detected from browser on first visit
- Changeable at any time in My Profile → Language
- Supported languages: 🇬🇧 English, 🇭🇺 Magyar, 🇩🇪 Deutsch, 🇫🇷 Français, 🇪🇸 Español
- All UI strings must be i18n-ready from day one (no hardcoded copy)

---

## Screen Mockups

All mockups are HTML files viewable in any browser. Persisted to: `docs/superpowers/screens/`

| Screens | File |
|---|---|
| 1–4 Auth & Onboarding | [screens-auth.html](../screens/screens-auth.html) |
| 5–6 Dashboard & Invite | [screens-dashboard.html](../screens/screens-dashboard.html) |
| 7–8 Add Child & Child Profile | [screens-children.html](../screens/screens-children.html) |
| 9 Add Good Deed (final) | [screens-deed-v2.html](../screens/screens-deed-v2.html) |
| 9b Manage Deed Presets | [screens-presets.html](../screens/screens-presets.html) |
| 10 Weekly Podium | [screens-podium.html](../screens/screens-podium.html) |
| 11 Monthly Champion (final) | [screens-monthly-v2.html](../screens/screens-monthly-v2.html) |
| 12 History (final) | [screens-simplified.html](../screens/screens-simplified.html) |
| 13 Prizes Board (final) | [screens-simplified.html](../screens/screens-simplified.html) |
| 13b Prize Editor (final) | [screens-prize-editor-v2.html](../screens/screens-prize-editor-v2.html) |
| 13c My Custom Prizes | [screens-custom-prizes.html](../screens/screens-custom-prizes.html) |
| 14 Family Settings | [screens-settings.html](../screens/screens-settings.html) |
| 15 My Profile (final) | [screens-updates.html](../screens/screens-updates.html) |
| Architecture | [architecture.html](../screens/architecture.html) |

---

## Screens (18 total)

### Auth & Onboarding

**1. Welcome / Landing**  
App name, tagline, three feature cards (Track Deeds, Weekly Podium, Win Prizes), single "Get Started" CTA and "Log in" link.

**2. Sign Up**  
Social login buttons: Google, Apple, Facebook. OR divider then email form (name, email, password). Link to Log In.

**3. Log In**  
Social login buttons: Google, Apple, Facebook. OR divider then email + password. Forgot password link. Link to Sign Up.

**4. Create Family**  
Family name input. Week start day picker (Mon–Sun, tap to select). Info note about weekly/monthly cadence. "Create My Family" CTA.

---

### Family Dashboard

**5. Home Dashboard**  
Top bar: greeting with parent first name + family name + parent avatar initial. Week strip showing Mon–Sun with current day highlighted and past days ticked. "Your heroes" section — one card per child showing: avatar with rank badge (🥇🥈🥉), name, age, deeds today, progress bar, weekly deed count (large), per-child `+` button. Bottom nav: Home · Podium · Prizes · Settings.

**6. Invite Co-Parent**  
Family name and icon header. Current members list (name, email, role, "You" badge). Email invite input + Send button. OR shareable link with Copy button. Note: co-parents can add deeds; only admins manage prizes and settings.

---

### Children

**7. Add Child**  
Avatar picker (grid of 10+ emoji avatars + photo upload option). Name input. Age spinner (− / number / +). Gender selector (Boy / Girl). "Add Hero" CTA.

**8. Child Profile**  
Hero header: large avatar, name, age, gender. Three stat badges: this week / all time / wins. Scrollable deed list grouped by date (Today, Yesterday, etc.). Each deed: image tile + description + "Added by [Parent] · time" + star. "Add Good Deed" button pinned at bottom.

**9. Add Good Deed**  
Child selector (tap to switch child). "Quick pick" grid (4 columns) of preset tiles: system presets (📚 Did homework, 🍳 Helped in kitchen, 🧹 Cleaned room, 🤝 Helped sibling, 😊 Behaved all day, 🛏️ Made bed) and custom presets (purple tint) + `+` tile to add new. "Manage" link opens screen 9b. Description field (auto-filled from preset, editable). Image row: auto-selected icon thumbnail with "Change" tap (opens library tabs: 📚 Library with categorised icon grid, or ✨ AI Generate). Save button.

**9b. Manage Presets (Good Deeds)**  
"Create custom preset" button. List of custom presets (icon + name + edit/delete per row). List of built-in presets with on/off toggles. Accessible from the "Manage" link in screen 9.

---

### Weekly & Monthly

**10. Weekly Podium**  
Purple-to-orange gradient header with confetti stars. Week label. "Results are in!" message. Three-step podium visual: 2nd (silver, medium height) · 1st (gold, tallest) · 3rd (bronze, shortest). Each step: child avatar floating above, name, deed count, medal emoji, rank number. Prize card below: child name + prize text. Full ranking list (all children, medal + avatar + name + deed count). Bottom nav active on Podium.

**11. Monthly Champion**  
Dark starfield gradient (navy-to-indigo) header. Month label. Large trophy, glowing avatar with gold border. Champion name, "Champion of [Month]" subtitle, total deed count pill. Prize card (gold border). Full monthly ranking: all children with monthly deed total + weeks won count. Collapsible "week by week breakdown" row.

**12. History**  
Weekly / Monthly toggle. Grouped by month. Expanded week card: header (week name, dates, Done badge) + three child columns — each column has medal, avatar, name, big deed count number, prize won below in small text. Collapsed weeks show name + deed scores inline. Collapsed month rows show champion name + total deeds + grand prize. Monthly champion rows include grand prize name.

---

### Prizes & Settings

**13. Prizes Board**  
Admin-only. Clean 3-row list (medal icon · rank label · prize text · Edit button) for weekly 1st/2nd/3rd. Monthly grand prize row in purple. Collapsed "Prize suggestions library" hint at the bottom. Tapping Edit opens the Prize Editor (screen 13b).

**13b. Prize Editor**  
Rank label header. Current prize preview (gold/purple card). Custom text input with tappable emoji picker beside it. "Save to my prizes library" checkbox — when checked, the typed prize is added to the custom library for later reuse. "My prizes" row (purple chips, scrollable) with "Manage all →" link to screen 13c. Built-in suggestions in categorised scrollable rows: 🍕 Food & Treats · 🎬 Activities · 🌙 Special Treats · 🎡 Experiences. Save Prize button.

**13c. My Custom Prizes**  
"Add new custom prize" button. Inline add form (emoji picker + text input + Save/Cancel). List of existing custom prizes (emoji tile · name · edit · delete per row). Below: read-only built-in suggestions list for reference. Accessible from Prizes Board and from Prize Editor "Manage all →" link.

**14. Family Settings**  
Family name input. Week start day picker. Co-parents list with remove option and "+ Invite" row. Save Changes button. Danger zone: "Delete Family" with warning text.

**15. My Profile**  
Parent avatar initial + name + role/family subtitle. Account section: name, email, password (each with Edit/Change). Language section: list of supported languages with flag + native name + English name, active one ticked. Notifications section: push notifications toggle (on), weekly email report toggle (off). Sign Out button.

---

## Data Model (logical)

```
User          id, name, email, provider, avatar, language, notifications
Family        id, name, week_start_day, created_by
FamilyMember  family_id, user_id, role (admin | co-parent), relation (dad | mom | grandma | ...)
Child         id, family_id, name, age, gender, avatar_type, avatar_value
GoodDeed      id, child_id, added_by_user_id, description, image_type (library|ai), image_key, created_at
DeedPreset    id, family_id (null=system), label, image_type, image_value, enabled
WeekSummary   id, family_id, week_start, week_end, rankings (JSON), prizes_awarded (JSON)
MonthSummary  id, family_id, month, champion_child_id, total_deeds, prize_awarded
PrizePreset   id, family_id (null=system), label, emoji, scope (weekly|monthly|null), rank (1|2|3|null), enabled
FamilyInvite  id, family_id, email (nullable), token, expires_at, accepted
```

---

## Key Feature Decisions

| Topic | Decision |
|---|---|
| Good deed images | Library (80+ icons in categories) + AI generation, both available |
| Preset deeds | System presets (familyId=null, toggleable via enabled) + family custom presets — no isSystem flag |
| Weekly reset | Deed counts reset per week for competition; accumulate for monthly total |
| Monthly winner | Single winner — child with most deeds across the full month |
| Prizes | Weekly: 1st/2nd/3rd. Monthly: 1st only. Parents set prize text + emoji. |
| Prize library | Unified PrizePreset table: system suggestions (familyId=null) + family library entries + active rank prizes — same pattern as DeedPreset |
| Co-parent role | Can add deeds; cannot manage prizes, settings, or delete family |
| Language | Auto-detect from browser; changeable in profile. 5 languages at launch. |
| Avatars | Emoji grid (10+ options) + photo upload |
| AI image gen | On-demand per deed via IAiImageService; library used by default for speed. Provider swappable by config (Hugging Face default, DALL-E/Stability AI possible). |

---

## Navigation Structure

```
Bottom nav (persistent):
  🏠 Home  →  Dashboard
  🏆 Podium  →  Weekly Podium / Monthly Champion / History
  🎁 Prizes  →  Prizes Board → Prize Editor → My Custom Prizes
  ⚙️ Settings  →  Family Settings / My Profile

Home:
  Child card [+]  →  Add Good Deed
  Child card tap  →  Child Profile → Add Good Deed

Settings:
  Invite Co-Parent  (from Family Settings)
  Manage Presets    (from Add Good Deed)
```

---

## Verification Checklist

- [ ] Auth flow: sign up → create family → dashboard
- [ ] Social login (Google, Apple, Facebook) works
- [ ] Add child with avatar, age, gender
- [ ] Add good deed via preset — image auto-selected
- [ ] Add good deed with custom text + library image
- [ ] Add good deed with AI-generated image
- [ ] Co-parent invite via email and shareable link
- [ ] Weekly podium shows correct ranking and prizes
- [ ] Monthly champion screen shows full ranking
- [ ] History shows deed counts prominently, prizes subtly
- [ ] Prize editor: pick built-in, custom input, save to library
- [ ] My custom prizes: add, edit, delete
- [ ] Preset management: toggle system presets, add/delete custom
- [ ] Language switch: all UI updates immediately
- [ ] Responsive layout: test on 375px mobile and 1280px desktop
