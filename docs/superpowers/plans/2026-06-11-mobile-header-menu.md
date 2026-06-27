# Mobile Header Menu Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a hamburger-menu toggle to the public site header so mobile users can access the nav links (Home, Help, Blog, About) that are currently hidden on small screens.

**Architecture:** Convert `PublicLayoutComponent` from a purely stateless component to one that manages a `menuOpen` signal. A hamburger button (visible only on `sm:hidden`) toggles the signal; a dropdown panel renders the nav links when open. A `HostListener` on `document:click` closes the menu when the user taps outside — same pattern used by `LanguageSelectorComponent`. No new files needed.

**Tech Stack:** Angular 17+ signals, Tailwind CSS, `@ngx-translate/core`, `RouterLink` / `RouterLinkActive`

---

## File Map

| File | Action |
|---|---|
| `frontend/src/app/shared/components/public-layout.component.ts` | Modify — add signal, hamburger button, mobile dropdown panel |
| `frontend/src/app/shared/components/public-layout.component.spec.ts` | Modify — add tests for hamburger visibility and menu toggle behaviour |
| `frontend/public/assets/i18n/en.json` | Modify — add `LEGAL.OPEN_MENU` key |
| `frontend/public/assets/i18n/hu.json` | Modify — add `LEGAL.OPEN_MENU` key |
| `frontend/public/assets/i18n/de.json` | Modify — add `LEGAL.OPEN_MENU` key |
| `frontend/public/assets/i18n/fr.json` | Modify — add `LEGAL.OPEN_MENU` key |
| `frontend/public/assets/i18n/es.json` | Modify — add `LEGAL.OPEN_MENU` key |
| `frontend/src/app/features/help/help.component.ts` | No change (already uses `PublicLayoutComponent` via selector only) |
| `CHANGELOG.md` | Modify — add entry under `## [Unreleased]` |
| `frontend/src/environments/environment.ts` | Modify — bump version to 3.2.0 |
| `frontend/src/environments/environment.prod.ts` | Modify — bump version to 3.2.0 |

---

## Task 1: Add i18n key for the hamburger button aria-label

**Files:**
- Modify: `frontend/public/assets/i18n/en.json`
- Modify: `frontend/public/assets/i18n/hu.json`
- Modify: `frontend/public/assets/i18n/de.json`
- Modify: `frontend/public/assets/i18n/fr.json`
- Modify: `frontend/public/assets/i18n/es.json`

- [ ] **Step 1: Add the key to en.json**

In `frontend/public/assets/i18n/en.json`, inside the `LEGAL` object, add after `"PUBLIC_NAV_BLOG": "Blog"`:
```json
"OPEN_MENU": "Open navigation menu",
"CLOSE_MENU": "Close navigation menu",
```

- [ ] **Step 2: Add the key to hu.json**

In `frontend/public/assets/i18n/hu.json`, inside `LEGAL`, add after `"PUBLIC_NAV_BLOG": "Blog"`:
```json
"OPEN_MENU": "Navigációs menü megnyitása",
"CLOSE_MENU": "Navigációs menü bezárása",
```

- [ ] **Step 3: Add the key to de.json**

In `frontend/public/assets/i18n/de.json`, inside `LEGAL`, add after `"PUBLIC_NAV_BLOG": "Blog"`:
```json
"OPEN_MENU": "Navigationsmenü öffnen",
"CLOSE_MENU": "Navigationsmenü schließen",
```

- [ ] **Step 4: Add the key to fr.json**

In `frontend/public/assets/i18n/fr.json`, inside `LEGAL`, add after `"PUBLIC_NAV_BLOG": "Blog"`:
```json
"OPEN_MENU": "Ouvrir le menu de navigation",
"CLOSE_MENU": "Fermer le menu de navigation",
```

- [ ] **Step 5: Add the key to es.json**

In `frontend/public/assets/i18n/es.json`, inside `LEGAL`, add after `"PUBLIC_NAV_BLOG": "Blog"`:
```json
"OPEN_MENU": "Abrir el menú de navegación",
"CLOSE_MENU": "Cerrar el menú de navegación",
```

- [ ] **Step 6: Commit**

```bash
git add frontend/public/assets/i18n/
git commit -m "feat: add i18n keys for mobile menu open/close"
```

---

## Task 2: Write failing tests for the mobile menu

**Files:**
- Modify: `frontend/src/app/shared/components/public-layout.component.spec.ts`

The `PublicLayoutComponent` spec already uses `TestBed` with router and translate providers. Add the new tests at the bottom of the `describe` block.

- [ ] **Step 1: Write the failing tests**

Append these four tests inside the existing `describe('PublicLayoutComponent', ...)` block in `frontend/src/app/shared/components/public-layout.component.spec.ts`, before the closing `});`:

```typescript
it('renders a hamburger button visible only on small screens', () => {
  const btn: HTMLButtonElement = fixture.nativeElement.querySelector('[data-testid="mobile-menu-toggle"]');
  expect(btn).toBeTruthy();
  // The button must carry the sm:hidden class so it is hidden on desktop
  expect(btn.classList).toContain('sm:hidden');
});

it('mobile menu panel is hidden by default', () => {
  const panel: HTMLElement = fixture.nativeElement.querySelector('[data-testid="mobile-menu-panel"]');
  expect(panel).toBeNull();
});

it('clicking the hamburger button reveals the mobile menu panel', () => {
  const btn: HTMLButtonElement = fixture.nativeElement.querySelector('[data-testid="mobile-menu-toggle"]');
  btn.click();
  fixture.detectChanges();
  const panel: HTMLElement = fixture.nativeElement.querySelector('[data-testid="mobile-menu-panel"]');
  expect(panel).toBeTruthy();
});

it('mobile menu panel contains links to /, /help, /blog and /about', () => {
  const btn: HTMLButtonElement = fixture.nativeElement.querySelector('[data-testid="mobile-menu-toggle"]');
  btn.click();
  fixture.detectChanges();
  const links: NodeListOf<HTMLAnchorElement> = fixture.nativeElement.querySelectorAll('[data-testid="mobile-menu-panel"] a');
  const hrefs = Array.from(links).map(a => a.getAttribute('routerlink'));
  expect(hrefs).toContain('/');
  expect(hrefs).toContain('/help');
  expect(hrefs).toContain('/blog');
  expect(hrefs).toContain('/about');
});
```

Also update the translate stub in `beforeEach` — add the two new keys to the `LEGAL` object:
```typescript
LEGAL: {
  // ...existing keys...
  OPEN_MENU: 'Open navigation menu',
  CLOSE_MENU: 'Close navigation menu',
},
```

- [ ] **Step 2: Run the tests to confirm they fail**

```bash
cd frontend && npx ng test --watch=false --include="src/app/shared/components/public-layout.component.spec.ts"
```

Expected: 4 new tests FAIL with "Cannot read properties of null" or similar.

---

## Task 3: Implement the hamburger menu in PublicLayoutComponent

**Files:**
- Modify: `frontend/src/app/shared/components/public-layout.component.ts`

- [ ] **Step 1: Replace the component with the updated implementation**

Replace the entire content of `frontend/src/app/shared/components/public-layout.component.ts` with:

```typescript
import { Component, ElementRef, HostListener, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { LanguageSelectorComponent } from './language-selector.component';

@Component({
  selector: 'app-public-layout',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, TranslatePipe, LanguageSelectorComponent],
  template: `
    <div class="min-h-screen bg-brand-cream flex flex-col overflow-x-hidden">
      <header class="bg-white border-b border-brand-border sticky top-0 z-50">
        <div class="max-w-5xl mx-auto px-4 h-14 flex items-center justify-between gap-4">
          <a routerLink="/" class="flex items-center gap-2 flex-shrink-0">
            🌟 <span class="font-black text-brand-orange text-base hidden sm:inline">TinyHeroes</span>
          </a>
          <nav class="flex items-center gap-4 text-xs text-brand-muted flex-shrink-0">
            <a
              routerLink="/"
              routerLinkActive="text-brand-orange font-semibold"
              [routerLinkActiveOptions]="{ exact: true }"
              data-umami-event="pub_nav_home"
              class="hidden sm:inline hover:text-brand-orange transition-colors">
              {{ 'LEGAL.PUBLIC_NAV_HOME' | translate }}
            </a>
            <a
              routerLink="/help"
              routerLinkActive="text-brand-orange font-semibold"
              data-umami-event="pub_nav_help"
              class="hidden sm:inline hover:text-brand-orange transition-colors">
              {{ 'LEGAL.PUBLIC_NAV_HELP' | translate }}
            </a>
            <a
              routerLink="/blog"
              routerLinkActive="text-brand-orange font-semibold"
              data-umami-event="pub_nav_blog"
              class="hidden sm:inline hover:text-brand-orange transition-colors">
              {{ 'LEGAL.PUBLIC_NAV_BLOG' | translate }}
            </a>
            <a
              routerLink="/about"
              routerLinkActive="text-brand-orange font-semibold"
              data-umami-event="pub_nav_about"
              class="hidden sm:inline hover:text-brand-orange transition-colors">
              {{ 'LEGAL.PUBLIC_NAV_ABOUT' | translate }}
            </a>
            <app-language-selector />
            <a
              routerLink="/login"
              class="font-semibold text-brand-orange hover:text-orange-400 transition-colors">
              {{ 'AUTH.LOG_IN' | translate }}
            </a>
            <button
              data-testid="mobile-menu-toggle"
              (click)="menuOpen.set(!menuOpen())"
              [attr.aria-expanded]="menuOpen()"
              [attr.aria-label]="(menuOpen() ? 'LEGAL.CLOSE_MENU' : 'LEGAL.OPEN_MENU') | translate"
              class="sm:hidden flex flex-col gap-1 p-1">
              <span class="block w-5 h-0.5 bg-brand-text rounded"></span>
              <span class="block w-5 h-0.5 bg-brand-text rounded"></span>
              <span class="block w-5 h-0.5 bg-brand-text rounded"></span>
            </button>
          </nav>
        </div>

        @if (menuOpen()) {
          <div
            data-testid="mobile-menu-panel"
            class="sm:hidden border-t border-brand-border bg-white px-4 py-3 flex flex-col gap-3 text-sm">
            <a
              routerLink="/"
              routerLinkActive="text-brand-orange font-semibold"
              [routerLinkActiveOptions]="{ exact: true }"
              (click)="menuOpen.set(false)"
              data-umami-event="pub_nav_mobile_home"
              class="text-brand-muted hover:text-brand-orange transition-colors">
              {{ 'LEGAL.PUBLIC_NAV_HOME' | translate }}
            </a>
            <a
              routerLink="/help"
              routerLinkActive="text-brand-orange font-semibold"
              (click)="menuOpen.set(false)"
              data-umami-event="pub_nav_mobile_help"
              class="text-brand-muted hover:text-brand-orange transition-colors">
              {{ 'LEGAL.PUBLIC_NAV_HELP' | translate }}
            </a>
            <a
              routerLink="/blog"
              routerLinkActive="text-brand-orange font-semibold"
              (click)="menuOpen.set(false)"
              data-umami-event="pub_nav_mobile_blog"
              class="text-brand-muted hover:text-brand-orange transition-colors">
              {{ 'LEGAL.PUBLIC_NAV_BLOG' | translate }}
            </a>
            <a
              routerLink="/about"
              routerLinkActive="text-brand-orange font-semibold"
              (click)="menuOpen.set(false)"
              data-umami-event="pub_nav_mobile_about"
              class="text-brand-muted hover:text-brand-orange transition-colors">
              {{ 'LEGAL.PUBLIC_NAV_ABOUT' | translate }}
            </a>
          </div>
        }
      </header>

      <main class="flex-1 max-w-5xl mx-auto w-full px-4 py-10">
        <ng-content />
      </main>

      <footer class="border-t border-brand-border text-center text-xs text-brand-muted/50 py-6">
        © TinyHeroes ·
        <a routerLink="/changelog" class="hover:text-brand-orange transition-colors">{{ 'LEGAL.PUBLIC_NAV_CHANGELOG' | translate }}</a>
        ·
        <a routerLink="/contact" class="hover:text-brand-orange transition-colors">{{ 'LEGAL.PUBLIC_NAV_CONTACT' | translate }}</a>
        ·
        <a routerLink="/privacy" class="hover:text-brand-orange transition-colors">{{ 'LEGAL.PUBLIC_NAV_PRIVACY' | translate }}</a>
        ·
        <a routerLink="/terms" class="hover:text-brand-orange transition-colors">{{ 'LEGAL.PUBLIC_NAV_TERMS' | translate }}</a>
      </footer>
    </div>
  `,
})
export class PublicLayoutComponent {
  protected menuOpen = signal(false);
  private el = inject(ElementRef);

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event) {
    if (!this.el.nativeElement.contains(event.target)) {
      this.menuOpen.set(false);
    }
  }
}
```

- [ ] **Step 2: Run the new tests to verify they pass**

```bash
cd frontend && npx ng test --watch=false --include="src/app/shared/components/public-layout.component.spec.ts"
```

Expected: all tests PASS (including the 4 new ones).

- [ ] **Step 3: Run the full test suite to check for regressions**

```bash
cd frontend && npx ng test --watch=false
```

Expected: all tests PASS.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/app/shared/components/public-layout.component.ts \
        frontend/src/app/shared/components/public-layout.component.spec.ts
git commit -m "feat: add hamburger menu to public header for mobile"
```

---

## Task 4: Version bump and changelog

**Files:**
- Modify: `frontend/src/environments/environment.ts`
- Modify: `frontend/src/environments/environment.prod.ts`
- Modify: `CHANGELOG.md`

- [ ] **Step 1: Bump version in environment.ts**

In `frontend/src/environments/environment.ts`, change:
```typescript
version: '3.1.4'
```
to:
```typescript
version: '3.2.0'
```

- [ ] **Step 2: Bump version in environment.prod.ts**

In `frontend/src/environments/environment.prod.ts`, change:
```typescript
version: '3.1.4'
```
to:
```typescript
version: '3.2.0'
```

- [ ] **Step 3: Update CHANGELOG.md**

In `CHANGELOG.md`, find the `## [Unreleased]` section. If it doesn't exist, add it at the top after the `# Changelog` heading. Add or append to the `### Added` sub-section:

```markdown
## [3.2.0] - 2026-06-11

### Added
- Mobile navigation menu: a hamburger button in the public site header now reveals the full navigation (Home, Help, Blog, About) on small screens.
```

- [ ] **Step 4: Commit**

```bash
git add frontend/src/environments/environment.ts \
        frontend/src/environments/environment.prod.ts \
        CHANGELOG.md
git commit -m "chore: bump frontend version to 3.2.0"
```

---

## Task 5: Update help documentation

**Files:**
- Modify: `frontend/src/app/features/help/help.component.ts`

The help component describes the public layout. Find the section describing navigation and add a note that on mobile a hamburger menu reveals the navigation links.

- [ ] **Step 1: Locate the relevant section in help.component.ts**

Search for references to the public header/navigation in `frontend/src/app/features/help/help.component.ts`:
```bash
grep -n "header\|nav\|menu\|navigation" frontend/src/app/features/help/help.component.ts -i | head -20
```

- [ ] **Step 2: Add mobile nav description**

Find the description text that mentions the header or navigation in the help content. Add a sentence such as:

> On small screens, tap the hamburger icon (☰) in the top-right corner to reveal the navigation menu.

The exact position depends on what the grep above returns — insert it logically near the existing desktop nav description.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/app/features/help/help.component.ts
git commit -m "docs: mention mobile hamburger menu in help content"
```

---

## Self-Review

**Spec coverage:**
- ✅ Mobile users can see/access nav links via hamburger → Task 3
- ✅ Menu closes when tapping a link → `(click)="menuOpen.set(false)"` on each link in Task 3
- ✅ Menu closes on outside click → `HostListener` in Task 3
- ✅ Accessible aria-label and aria-expanded → Task 3
- ✅ Analytics events on mobile links → `data-umami-event` attributes in Task 3
- ✅ i18n for button label → Task 1, all 5 locales
- ✅ Tests → Task 2 + Task 3
- ✅ Version bump → Task 4
- ✅ Changelog → Task 4
- ✅ Help docs → Task 5

**Placeholder scan:** None found — all code blocks are complete.

**Type consistency:** `menuOpen` signal is `signal(false)` (boolean) — used consistently as `menuOpen()` in template and `menuOpen.set(false/true)` in handlers.
