# Landing Page — Visual Refresh Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the minimal `WelcomeComponent` auth gate at `/` with a full-width, benefit-led landing page using a 3D depth visual style that adapts to all four app themes.

**Architecture:** `WelcomeComponent` is rewritten in place — same route, same TypeScript class, new template. The component is wrapped in `PublicLayoutComponent` (already used by Help, About, Privacy) to get the sticky nav header and footer for free. All copy goes through new `LANDING.*` i18n keys added to all five locale files. The 3D podium and card depth effects are pure CSS using `perspective`/`rotateX` and stacked `box-shadow` layers.

**Tech Stack:** Angular 21 (standalone), Tailwind CSS 4, `@ngx-translate/core` (`TranslatePipe`), `RouterLink`, `PublicLayoutComponent`

**Design spec:** `docs/superpowers/specs/2026-06-06-landing-page-design.md`

---

## File Map

| File | Change |
|---|---|
| `frontend/src/app/features/auth/pages/welcome.component.ts` | Replace template; add `PublicLayoutComponent` to imports |
| `frontend/public/assets/i18n/en.json` | Add `LANDING` block after `WELCOME` block |
| `frontend/public/assets/i18n/hu.json` | Add `LANDING` block (Hungarian) |
| `frontend/public/assets/i18n/de.json` | Add `LANDING` block (German) |
| `frontend/public/assets/i18n/fr.json` | Add `LANDING` block (French) |
| `frontend/public/assets/i18n/es.json` | Add `LANDING` block (Spanish) |
| `frontend/src/environments/environment.ts` | Bump `version` to `'2.7.0'` |
| `frontend/src/environments/environment.prod.ts` | Bump `version` to `'2.7.0'` |
| `CHANGELOG.md` | Add entry under `## [Unreleased]` → promote to `## [2.7.0]` |

---

## Pre-flight: Move GitHub project card

- [ ] **Step 1: Move issue #8 card to "In Progress"**

```bash
# Find the item ID for issue #8 in project #5
gh project item-list 5 --owner DARKinVADER --format json | \
  jq '.items[] | select(.content.number == 8) | .id'
```

Then in the GitHub UI (or via `gh project item-edit`): set Status → **In Progress**, add label `in progress`, add comment "Starting implementation".

---

## Task 1: Add `LANDING.*` keys to `en.json`

**Files:**
- Modify: `frontend/public/assets/i18n/en.json`

- [ ] **Step 1: Insert the LANDING block after the `"WELCOME"` block**

Open `frontend/public/assets/i18n/en.json`. After the closing `}` of the `"WELCOME"` key (currently at line ~40), add a comma and insert:

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
},
```

- [ ] **Step 2: Verify JSON is valid**

```bash
cd frontend && node -e "JSON.parse(require('fs').readFileSync('public/assets/i18n/en.json','utf8')); console.log('OK')"
```
Expected output: `OK`

- [ ] **Step 3: Commit**

```bash
git add frontend/public/assets/i18n/en.json
git commit -m "feat: add LANDING i18n keys to en.json"
```

---

## Task 2: Add `LANDING.*` keys to the four non-English locale files

**Files:**
- Modify: `frontend/public/assets/i18n/hu.json`
- Modify: `frontend/public/assets/i18n/de.json`
- Modify: `frontend/public/assets/i18n/fr.json`
- Modify: `frontend/public/assets/i18n/es.json`

In each file, add the `"LANDING"` block after the `"WELCOME"` block (same position as in en.json).

- [ ] **Step 1: Add LANDING block to `hu.json`**

```json
"LANDING": {
  "HERO_TITLE": "Minden jó tett jutalmat érdemel",
  "HERO_SUBTITLE": "Kövesd nyomon gyermekeid jó tetteit, rendezz heti versenyeket, és adj valódi jutalmakat.",
  "HOW_TITLE": "Hogyan működik",
  "STEP_1_TITLE": "Jó tettek nyomon követése",
  "STEP_1_DESC": "Rögzíts bármilyen jó tettet másodpercek alatt — előre beállított vagy egyéni tevékenységekkel.",
  "STEP_2_TITLE": "Ranglista megtekintése",
  "STEP_2_DESC": "A heti dobogó és a havi bajnok motiválva tartja a gyerekeket.",
  "STEP_3_TITLE": "Valódi jutalmak megszerzése",
  "STEP_3_DESC": "A szülők heti és havi jutalmakat adnak a győzteseknek.",
  "FEATURES_TITLE": "Minden, amire a családodnak szüksége van",
  "FEATURE_PODIUM_TITLE": "Heti dobogó",
  "FEATURE_PODIUM_DESC": "1., 2. és 3. hely minden héten — igazságos és átlátható.",
  "FEATURE_CHAMPION_TITLE": "Havi bajnok",
  "FEATURE_CHAMPION_DESC": "A hónap legjobb teljesítője nyeri el a fő díjat.",
  "FEATURE_PRIZES_TITLE": "Jutalom tábla",
  "FEATURE_PRIZES_DESC": "Kezelje és kövesse nyomon a jutalmakat a javaslattól a kézbesítésig.",
  "SOCIAL_PROOF": "A családok már nyomon követik a jó tetteket a TinyHeroes-szal"
},
```

- [ ] **Step 2: Add LANDING block to `de.json`**

```json
"LANDING": {
  "HERO_TITLE": "Jede gute Tat verdient eine Belohnung",
  "HERO_SUBTITLE": "Verfolge die guten Taten deiner Kinder, führe wöchentliche Wettbewerbe durch und vergib echte Preise.",
  "HOW_TITLE": "So funktioniert es",
  "STEP_1_TITLE": "Gute Taten verfolgen",
  "STEP_1_DESC": "Jede gute Tat in Sekunden eintragen — voreingestellte oder eigene Aktionen.",
  "STEP_2_TITLE": "Rankings sehen",
  "STEP_2_DESC": "Ein wöchentliches Podium und monatlicher Champion halten Kinder motiviert.",
  "STEP_3_TITLE": "Echte Preise verdienen",
  "STEP_3_DESC": "Eltern vergeben wöchentliche und monatliche Preise an die Gewinner.",
  "FEATURES_TITLE": "Alles, was deine Familie braucht",
  "FEATURE_PODIUM_TITLE": "Wöchentliches Podium",
  "FEATURE_PODIUM_DESC": "1., 2. und 3. Platz jede Woche — fair und transparent.",
  "FEATURE_CHAMPION_TITLE": "Monatlicher Champion",
  "FEATURE_CHAMPION_DESC": "Der beste Performer des Monats gewinnt den Hauptpreis.",
  "FEATURE_PRIZES_TITLE": "Preistafel",
  "FEATURE_PRIZES_DESC": "Preise von der Vorschlag bis zur Lieferung verwalten und verfolgen.",
  "SOCIAL_PROOF": "Familien verfolgen bereits gute Taten mit TinyHeroes"
},
```

- [ ] **Step 3: Add LANDING block to `fr.json`**

```json
"LANDING": {
  "HERO_TITLE": "Chaque bonne action mérite une récompense",
  "HERO_SUBTITLE": "Suivez les bonnes actions de vos enfants, organisez des compétitions hebdomadaires et attribuez de vraies récompenses.",
  "HOW_TITLE": "Comment ça marche",
  "STEP_1_TITLE": "Suivre les bonnes actions",
  "STEP_1_DESC": "Enregistrez n'importe quelle bonne action en secondes — prédéfinie ou personnalisée.",
  "STEP_2_TITLE": "Voir les classements",
  "STEP_2_DESC": "Un podium hebdomadaire et un champion mensuel gardent les enfants motivés.",
  "STEP_3_TITLE": "Gagner de vraies récompenses",
  "STEP_3_DESC": "Les parents attribuent des récompenses hebdomadaires et mensuelles aux gagnants.",
  "FEATURES_TITLE": "Tout ce dont votre famille a besoin",
  "FEATURE_PODIUM_TITLE": "Podium hebdomadaire",
  "FEATURE_PODIUM_DESC": "1ère, 2ème et 3ème place chaque semaine — équitable et transparent.",
  "FEATURE_CHAMPION_TITLE": "Champion mensuel",
  "FEATURE_CHAMPION_DESC": "Le meilleur performeur du mois remporte le grand prix.",
  "FEATURE_PRIZES_TITLE": "Tableau des récompenses",
  "FEATURE_PRIZES_DESC": "Gérez et suivez les récompenses de la suggestion à la livraison.",
  "SOCIAL_PROOF": "Des familles suivent déjà les bonnes actions avec TinyHeroes"
},
```

- [ ] **Step 4: Add LANDING block to `es.json`**

```json
"LANDING": {
  "HERO_TITLE": "Cada buena acción merece una recompensa",
  "HERO_SUBTITLE": "Registra las buenas acciones de tus hijos, organiza competiciones semanales y otorga premios reales.",
  "HOW_TITLE": "Cómo funciona",
  "STEP_1_TITLE": "Registrar buenas acciones",
  "STEP_1_DESC": "Anota cualquier buena acción en segundos — predefinida o personalizada.",
  "STEP_2_TITLE": "Ver los rankings",
  "STEP_2_DESC": "Un podio semanal y un campeón mensual mantienen a los niños motivados.",
  "STEP_3_TITLE": "Ganar premios reales",
  "STEP_3_DESC": "Los padres asignan premios semanales y mensuales a los ganadores.",
  "FEATURES_TITLE": "Todo lo que tu familia necesita",
  "FEATURE_PODIUM_TITLE": "Podio semanal",
  "FEATURE_PODIUM_DESC": "1º, 2º y 3er lugar cada semana — justo y transparente.",
  "FEATURE_CHAMPION_TITLE": "Campeón mensual",
  "FEATURE_CHAMPION_DESC": "El mejor participante del mes gana el gran premio.",
  "FEATURE_PRIZES_TITLE": "Tablero de premios",
  "FEATURE_PRIZES_DESC": "Gestiona y sigue los premios desde la sugerencia hasta la entrega.",
  "SOCIAL_PROOF": "Las familias ya registran buenas acciones con TinyHeroes"
},
```

- [ ] **Step 5: Validate all four JSON files**

```bash
cd frontend
for f in hu de fr es; do
  node -e "JSON.parse(require('fs').readFileSync('public/assets/i18n/$f.json','utf8')); console.log('$f: OK')"
done
```
Expected: four lines each reading `hu: OK`, `de: OK`, `fr: OK`, `es: OK`.

- [ ] **Step 6: Commit**

```bash
git add frontend/public/assets/i18n/hu.json \
        frontend/public/assets/i18n/de.json \
        frontend/public/assets/i18n/fr.json \
        frontend/public/assets/i18n/es.json
git commit -m "feat: add LANDING i18n keys to hu/de/fr/es"
```

---

## Task 3: Rewrite `WelcomeComponent` template

**Files:**
- Modify: `frontend/src/app/features/auth/pages/welcome.component.ts`

The TypeScript class body (`infoService`, `version`, `ngOnInit`) is **unchanged**. Only the `imports` array and `template` change. `PublicLayoutComponent` is added to `imports` so its `<app-public-layout>` selector can be used as the outer wrapper — this brings the sticky nav header and the Privacy/Terms footer automatically.

- [ ] **Step 1: Replace the entire file with the following**

```typescript
import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { environment } from '../../../../environments/environment';
import { InfoService } from '../../../core/services/info.service';
import { PublicLayoutComponent } from '../../../shared/components/public-layout.component';

@Component({
  selector: 'app-welcome',
  imports: [RouterLink, TranslatePipe, PublicLayoutComponent],
  template: `
    <app-public-layout>

      <!-- HERO -->
      <section class="bg-gradient-to-b from-brand-orange to-orange-400 -mx-4 -mt-10 px-4 pt-16 pb-0 text-center">
        <div class="max-w-2xl mx-auto">
          <h1 class="text-4xl sm:text-5xl font-black text-white leading-tight mb-4">
            {{ 'LANDING.HERO_TITLE' | translate }}
          </h1>
          <p class="text-white/80 text-lg sm:text-xl mb-8">
            {{ 'LANDING.HERO_SUBTITLE' | translate }}
          </p>
          <a routerLink="/signup"
             class="inline-block bg-white text-brand-orange font-black py-4 px-10 rounded-2xl text-lg shadow-lg hover:shadow-xl transition-shadow mb-10">
            {{ 'AUTH.GET_STARTED' | translate }} ✨
          </a>
        </div>

        <!-- 3D CSS podium -->
        <div class="flex justify-center mb-0" style="perspective: 400px">
          <div class="flex items-end gap-1" style="transform: rotateX(22deg); transform-origin: bottom center">
            <div class="flex flex-col items-center justify-end pb-1 rounded-t-lg w-16 h-20 bg-gradient-to-br from-gray-300 to-gray-400">
              <span class="text-2xl">👧</span>
              <span class="text-white text-xs font-black">2nd</span>
            </div>
            <div class="flex flex-col items-center justify-end pb-1 rounded-t-lg w-16 h-28 bg-gradient-to-br from-yellow-400 to-amber-500">
              <span class="text-2xl">👦</span>
              <span class="text-white text-xs font-black">🥇</span>
            </div>
            <div class="flex flex-col items-center justify-end pb-1 rounded-t-lg w-16 h-14 bg-gradient-to-br from-amber-500 to-amber-700">
              <span class="text-2xl">🧒</span>
              <span class="text-white text-xs font-black">3rd</span>
            </div>
          </div>
        </div>
      </section>

      <!-- HOW IT WORKS -->
      <section class="py-16">
        <h2 class="text-2xl font-black text-center text-brand-orange mb-10">
          {{ 'LANDING.HOW_TITLE' | translate }}
        </h2>
        <div class="grid grid-cols-1 sm:grid-cols-3 gap-6">
          <div class="relative bg-white rounded-2xl p-6 text-center"
               style="box-shadow: 0 4px 0 0 var(--color-brand-border), 0 6px 20px rgba(0,0,0,0.08)">
            <span class="absolute -top-3 -right-3 w-7 h-7 bg-brand-orange text-white text-xs font-black rounded-full flex items-center justify-center shadow-md">1</span>
            <div class="text-4xl mb-3">📝</div>
            <h3 class="font-black text-brand-orange mb-2">{{ 'LANDING.STEP_1_TITLE' | translate }}</h3>
            <p class="text-sm text-brand-muted">{{ 'LANDING.STEP_1_DESC' | translate }}</p>
          </div>
          <div class="relative bg-white rounded-2xl p-6 text-center"
               style="box-shadow: 0 4px 0 0 var(--color-brand-border), 0 6px 20px rgba(0,0,0,0.08)">
            <span class="absolute -top-3 -right-3 w-7 h-7 bg-brand-orange text-white text-xs font-black rounded-full flex items-center justify-center shadow-md">2</span>
            <div class="text-4xl mb-3">🏆</div>
            <h3 class="font-black text-brand-orange mb-2">{{ 'LANDING.STEP_2_TITLE' | translate }}</h3>
            <p class="text-sm text-brand-muted">{{ 'LANDING.STEP_2_DESC' | translate }}</p>
          </div>
          <div class="relative bg-white rounded-2xl p-6 text-center"
               style="box-shadow: 0 4px 0 0 var(--color-brand-border), 0 6px 20px rgba(0,0,0,0.08)">
            <span class="absolute -top-3 -right-3 w-7 h-7 bg-brand-orange text-white text-xs font-black rounded-full flex items-center justify-center shadow-md">3</span>
            <div class="text-4xl mb-3">🎁</div>
            <h3 class="font-black text-brand-orange mb-2">{{ 'LANDING.STEP_3_TITLE' | translate }}</h3>
            <p class="text-sm text-brand-muted">{{ 'LANDING.STEP_3_DESC' | translate }}</p>
          </div>
        </div>
      </section>

      <!-- FEATURE HIGHLIGHTS -->
      <section class="py-16 -mx-4 px-4 bg-brand-bg">
        <h2 class="text-2xl font-black text-center text-brand-orange mb-10">
          {{ 'LANDING.FEATURES_TITLE' | translate }}
        </h2>
        <div class="grid grid-cols-1 sm:grid-cols-3 gap-6 max-w-3xl mx-auto">
          <div class="bg-white rounded-2xl p-6"
               style="box-shadow: 0 2px 0 var(--color-brand-border), 0 4px 0 color-mix(in srgb, var(--color-brand-border) 60%, transparent), 0 8px 20px rgba(0,0,0,0.08)">
            <div class="text-3xl mb-3">🏆</div>
            <h3 class="font-black text-brand-orange mb-2">{{ 'LANDING.FEATURE_PODIUM_TITLE' | translate }}</h3>
            <p class="text-sm text-brand-muted">{{ 'LANDING.FEATURE_PODIUM_DESC' | translate }}</p>
          </div>
          <div class="bg-white rounded-2xl p-6"
               style="box-shadow: 0 2px 0 var(--color-brand-border), 0 4px 0 color-mix(in srgb, var(--color-brand-border) 60%, transparent), 0 8px 20px rgba(0,0,0,0.08)">
            <div class="text-3xl mb-3">🌟</div>
            <h3 class="font-black text-brand-orange mb-2">{{ 'LANDING.FEATURE_CHAMPION_TITLE' | translate }}</h3>
            <p class="text-sm text-brand-muted">{{ 'LANDING.FEATURE_CHAMPION_DESC' | translate }}</p>
          </div>
          <div class="bg-white rounded-2xl p-6"
               style="box-shadow: 0 2px 0 var(--color-brand-border), 0 4px 0 color-mix(in srgb, var(--color-brand-border) 60%, transparent), 0 8px 20px rgba(0,0,0,0.08)">
            <div class="text-3xl mb-3">🎁</div>
            <h3 class="font-black text-brand-orange mb-2">{{ 'LANDING.FEATURE_PRIZES_TITLE' | translate }}</h3>
            <p class="text-sm text-brand-muted">{{ 'LANDING.FEATURE_PRIZES_DESC' | translate }}</p>
          </div>
        </div>
      </section>

      <!-- SOCIAL PROOF -->
      <section class="py-10 text-center">
        <p class="text-brand-muted font-semibold text-lg">
          🎉 {{ 'LANDING.SOCIAL_PROOF' | translate }}
        </p>
      </section>

      <!-- AUTH -->
      <section class="py-16 -mx-4 px-4 bg-brand-bg text-center">
        <div class="max-w-xs mx-auto flex flex-col gap-3">
          <a routerLink="/signup"
             class="block w-full text-center bg-gradient-to-r from-brand-orange to-orange-400 text-white font-black py-4 rounded-2xl text-base shadow-md hover:shadow-lg transition-shadow">
            {{ 'AUTH.GET_STARTED' | translate }} →
          </a>
          <a routerLink="/login" class="block text-center text-brand-orange font-semibold text-sm">
            {{ 'AUTH.ALREADY_ACCOUNT' | translate }} {{ 'AUTH.LOG_IN' | translate }}
          </a>
          <p class="text-xs text-brand-muted/50 mt-2">
            v{{ version }}
            @if (infoService.apiInfo(); as api) {
              · API v{{ api.version }}
            }
          </p>
        </div>
      </section>

    </app-public-layout>
  `
})
export class WelcomeComponent implements OnInit {
  protected infoService = inject(InfoService);
  readonly version = environment.version;

  ngOnInit() {
    this.infoService.load();
  }
}
```

**Note on full-bleed sections:** `PublicLayoutComponent`'s `<main>` constrains content to `max-w-5xl px-4 py-10`. The hero and feature sections use `-mx-4 -mt-10` Tailwind negative margins to break out of that container and span full width. This is intentional — the `py-16` on each section restores vertical rhythm.

- [ ] **Step 2: Start the dev server and visually verify at `http://localhost:4200`**

```bash
cd frontend && npm start
```

Check all of the following while logged out (open an incognito window or clear `th_access_token` from localStorage):
- Sticky nav header visible at top: `🌟 TinyHeroes` logo + navigation links + "Log In" button on right
- Hero section: orange gradient background, white headline, white CTA button, 3D tilted podium underneath
- "How it works": 3 white floating cards with stacked bottom shadow, orange numbered badge top-right
- Feature highlights: 3 layered-shadow cards on a slightly darker background strip
- Social proof: single centred line
- Auth section: orange gradient "Get Started →" button + "Log In" link below it
- Version number (`v2.7.0`) visible under auth buttons
- Footer from `PublicLayoutComponent`: `© TinyHeroes · Privacy · Terms`
- **No** `max-w-sm` constraint — hero and feature sections span full viewport width

- [ ] **Step 3: Test theme switching**

In the browser DevTools console at `http://localhost:4200`:
```javascript
localStorage.setItem('th_theme', 'ocean'); location.reload();
```
Expected: hero turns blue, step card shadows use blue border colour, font switches to Nunito. Repeat with `'forest'` (green) and `'candy'` (pink) to verify all themes adapt.

- [ ] **Step 4: Test mobile layout (resize to 390px wide)**

Verify:
- Three-column grids (how-it-works, features) collapse to single column
- "Get Started →" CTA in auth section is visible without scrolling past halfway
- No horizontal overflow

- [ ] **Step 5: Test i18n**

```javascript
localStorage.setItem('th_preferred_lang', 'de'); location.reload();
```
Expected: all `LANDING.*` copy renders in German. Switch back to `'en'` to confirm.

- [ ] **Step 6: Test navigation links**

- Click "Get Started ✨" in hero → lands on `/signup`
- Click "Get Started →" in auth section → lands on `/signup`
- Click "Log In" link → lands on `/login`
- Click "Log In" button in nav header → lands on `/login`
- Click "Privacy" in footer → lands on `/privacy`
- Click "Terms" in footer → lands on `/terms`

- [ ] **Step 7: Commit**

```bash
git add frontend/src/app/features/auth/pages/welcome.component.ts
git commit -m "feat: redesign WelcomeComponent as full-width 3D landing page"
```

---

## Task 4: Bump version and update CHANGELOG

**Files:**
- Modify: `frontend/src/environments/environment.ts`
- Modify: `frontend/src/environments/environment.prod.ts`
- Modify: `CHANGELOG.md`

- [ ] **Step 1: Bump version in `environment.ts`**

Change line 4 from:
```typescript
version: '2.6.0',
```
to:
```typescript
version: '2.7.0',
```

- [ ] **Step 2: Bump version in `environment.prod.ts`**

Same change — `'2.6.0'` → `'2.7.0'`.

- [ ] **Step 3: Add CHANGELOG entry**

At the top of `CHANGELOG.md`, add (or insert into an existing `## [Unreleased]` block):

```markdown
## [2.7.0] - 2026-06-06

### Added
- Landing page with benefit-led hero headline, a perspective-tilted podium illustration, "How it works" 3-step section, feature highlight cards, social proof, and prominent sign-up / log-in calls to action.
- The landing page adapts to the selected theme (Sunny, Ocean, Forest, Candy) automatically.
```

- [ ] **Step 4: Commit**

```bash
git add frontend/src/environments/environment.ts \
        frontend/src/environments/environment.prod.ts \
        CHANGELOG.md
git commit -m "chore: bump frontend version to 2.7.0"
```

---

## Task 5: Code review and security review

- [ ] **Step 1: Run code review**

```
/code-review
```
Address all findings before continuing.

- [ ] **Step 2: Run security review**

```
/security-review
```
Address all findings. Key areas to check: `routerLink` values are hardcoded strings (not user input), `TranslatePipe` output is text-interpolated (XSS-safe in Angular), no user data is accepted on this page.

---

## Task 6: Push, smoke test on integration, open PR

- [ ] **Step 1: Create feature branch (if not already on one)**

```bash
git checkout -b feat/8-landing-page-refresh
# If you've been committing to master, first create the branch from the current HEAD
```

- [ ] **Step 2: Push**

```bash
git push -u origin feat/8-landing-page-refresh
```
This triggers `deploy-integration.yml` automatically.

- [ ] **Step 3: Smoke test on integration**

Open `https://integration.mytinyheroes.net` (not logged in — you should land on `/`).

Verify each acceptance criterion from the issue:
- [ ] Hero section with benefit-led headline and primary CTA visible
- [ ] "How it works" 3-step section visible when scrolling
- [ ] Feature highlights section (3 cards) visible
- [ ] Social proof line visible
- [ ] Sign-up and Log-in CTAs are prominent
- [ ] Footer includes Privacy and Terms links
- [ ] "Get Started" → navigates to `/signup`
- [ ] "Log In" → navigates to `/login`
- [ ] Page is **not** constrained to a narrow column on desktop
- [ ] On mobile: sections stack vertically, CTA is thumb-accessible

- [ ] **Step 4: Open PR**

```bash
gh pr create --base master \
  --title "feat: landing page visual refresh and benefit-led copy (#8)" \
  --body "Closes #8"
```

- [ ] **Step 5: Move project card to "In Review"**

In GitHub Projects UI (or via `gh project item-edit`): set Status → **In Review** for issue #8.

---

## Verification Summary

| Acceptance criterion | Verified by |
|---|---|
| Hero section with benefit-led headline and CTA | Visual check at `/` |
| "How it works" 3-step section | Scroll down — 3 floating cards visible |
| Feature highlights (3 cards) | Section below steps |
| Social proof placeholder | Single line `🎉 Families are already…` |
| Sign-up and Log-in CTAs prominent | Hero CTA + auth section |
| Footer with Privacy + Terms | `PublicLayoutComponent` footer |
| All copy i18n'd via `LANDING.*` | Language switch test (Task 3, Step 5) |
| Theme-aware | Theme switch test (Task 3, Step 3) |
| No `max-w-sm` constraint | Sections span full width on desktop |
