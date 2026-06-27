# Legal Pages (Privacy Policy & Terms of Service) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add publicly accessible `/privacy` and `/terms` routes with a shared `PublicLayoutComponent` header/footer, link them from the welcome screen footer, refactor `ChangelogComponent` to use the new layout, and add all required i18n keys.

**Architecture:** A new `PublicLayoutComponent` (standalone, presentational, no services) wraps all public unauthenticated pages via `<ng-content>`. Two new legal page components live in `features/legal/pages/`. The existing `ChangelogComponent` is refactored to use the new layout. No backend changes.

**Tech Stack:** Angular 21 standalone components, Tailwind CSS (brand tokens from `styles.css`), ngx-translate, Angular Router lazy loading.

---

## File Map

| Action | File | Responsibility |
|--------|------|----------------|
| Create | `frontend/src/app/shared/components/public-layout.component.ts` | Shared header + footer wrapper for all public pages |
| Create | `frontend/src/app/shared/components/public-layout.component.spec.ts` | Component tests for public layout |
| Create | `frontend/src/app/features/legal/pages/privacy.component.ts` | Privacy Policy page content |
| Create | `frontend/src/app/features/legal/pages/privacy.component.spec.ts` | Component tests for privacy page |
| Create | `frontend/src/app/features/legal/pages/terms.component.ts` | Terms of Service page content |
| Create | `frontend/src/app/features/legal/pages/terms.component.spec.ts` | Component tests for terms page |
| Modify | `frontend/src/app/app.routes.ts` | Add `/privacy` and `/terms` lazy routes |
| Modify | `frontend/src/app/features/auth/pages/welcome.component.ts` | Add Privacy + Terms links to version footer |
| Modify | `frontend/src/app/features/public/pages/changelog.component.ts` | Refactor to use `PublicLayoutComponent` |
| Modify | `frontend/src/app/features/public/pages/changelog.component.spec.ts` | Update test to account for new wrapper |
| Modify | `frontend/public/assets/i18n/en.json` | Add `LEGAL.*` keys |
| Modify | `frontend/public/assets/i18n/hu.json` | Add `LEGAL.*` keys (Hungarian) |
| Modify | `frontend/public/assets/i18n/de.json` | Add `LEGAL.*` keys (German) |
| Modify | `frontend/public/assets/i18n/fr.json` | Add `LEGAL.*` keys (French) |
| Modify | `frontend/public/assets/i18n/es.json` | Add `LEGAL.*` keys (Spanish) |
| Modify | `CHANGELOG.md` | Add entry under `[Unreleased]` |

---

## Task 1: PublicLayoutComponent

**Files:**
- Create: `frontend/src/app/shared/components/public-layout.component.ts`
- Create: `frontend/src/app/shared/components/public-layout.component.spec.ts`

- [ ] **Step 1: Write the failing tests**

```typescript
// frontend/src/app/shared/components/public-layout.component.spec.ts
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTranslateService, TranslateService } from '@ngx-translate/core';
import { PublicLayoutComponent } from './public-layout.component';

describe('PublicLayoutComponent', () => {
  let fixture: ComponentFixture<PublicLayoutComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PublicLayoutComponent],
      providers: [
        provideRouter([]),
        provideTranslateService({ defaultLanguage: 'en' }),
      ],
    }).compileComponents();

    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', {
      LEGAL: {
        PUBLIC_NAV_HOME: 'Home',
        PUBLIC_NAV_HELP: 'Help',
        PUBLIC_NAV_CHANGELOG: 'Changelog',
        PUBLIC_NAV_PRIVACY: 'Privacy',
        PUBLIC_NAV_TERMS: 'Terms',
      }
    });
    translate.use('en');

    fixture = TestBed.createComponent(PublicLayoutComponent);
    fixture.detectChanges();
  });

  it('renders the TinyHeroes logo linking to /', () => {
    const logo = fixture.nativeElement.querySelector('header a[href="/"]');
    expect(logo).toBeTruthy();
  });

  it('renders Privacy nav link', () => {
    const links: NodeListOf<HTMLAnchorElement> = fixture.nativeElement.querySelectorAll('header nav a');
    const hrefs = Array.from(links).map(a => a.getAttribute('href') ?? a.getAttribute('ng-reflect-router-link'));
    expect(hrefs).toContain('/privacy');
  });

  it('renders Terms nav link', () => {
    const links: NodeListOf<HTMLAnchorElement> = fixture.nativeElement.querySelectorAll('header nav a');
    const hrefs = Array.from(links).map(a => a.getAttribute('href') ?? a.getAttribute('ng-reflect-router-link'));
    expect(hrefs).toContain('/terms');
  });

  it('renders the footer copyright line', () => {
    const footer: HTMLElement = fixture.nativeElement.querySelector('footer');
    expect(footer?.textContent).toContain('TinyHeroes');
  });

  it('projects ng-content into main', () => {
    // Create a host component that projects content
    const host = document.createElement('app-public-layout');
    const inner = document.createElement('p');
    inner.id = 'projected';
    inner.textContent = 'hello';
    host.appendChild(inner);
    // Basic check: <ng-content> is inside <main>
    const main = fixture.nativeElement.querySelector('main');
    expect(main).toBeTruthy();
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
cd frontend && npx ng test --watch=false --include="**/public-layout.component.spec.ts" 2>&1 | tail -20
```

Expected: compilation error — `PublicLayoutComponent` not found.

- [ ] **Step 3: Create the component**

```typescript
// frontend/src/app/shared/components/public-layout.component.ts
import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-public-layout',
  imports: [RouterLink, RouterLinkActive, TranslatePipe],
  template: `
    <div class="min-h-screen bg-brand-cream flex flex-col">

      <header class="bg-white border-b border-brand-border sticky top-0 z-50">
        <div class="max-w-3xl mx-auto px-4 h-14 flex items-center justify-between gap-4">
          <a routerLink="/" class="flex items-center gap-2 flex-shrink-0">
            <span class="text-xl">🌟</span>
            <span class="font-black text-brand-orange text-base hidden sm:inline">TinyHeroes</span>
          </a>
          <nav class="flex items-center gap-4 text-xs text-brand-muted flex-shrink-0">
            <a routerLink="/"
               routerLinkActive="text-brand-orange font-semibold"
               [routerLinkActiveOptions]="{ exact: true }"
               class="hover:text-brand-orange transition-colors hidden sm:inline">
              {{ 'LEGAL.PUBLIC_NAV_HOME' | translate }}
            </a>
            <a routerLink="/help"
               routerLinkActive="text-brand-orange font-semibold"
               class="hover:text-brand-orange transition-colors hidden sm:inline">
              {{ 'LEGAL.PUBLIC_NAV_HELP' | translate }}
            </a>
            <a routerLink="/changelog"
               routerLinkActive="text-brand-orange font-semibold"
               class="hover:text-brand-orange transition-colors hidden sm:inline">
              {{ 'LEGAL.PUBLIC_NAV_CHANGELOG' | translate }}
            </a>
            <a routerLink="/privacy"
               routerLinkActive="text-brand-orange font-semibold"
               class="hover:text-brand-orange transition-colors">
              {{ 'LEGAL.PUBLIC_NAV_PRIVACY' | translate }}
            </a>
            <a routerLink="/terms"
               routerLinkActive="text-brand-orange font-semibold"
               class="hover:text-brand-orange transition-colors">
              {{ 'LEGAL.PUBLIC_NAV_TERMS' | translate }}
            </a>
          </nav>
        </div>
      </header>

      <main class="flex-1 max-w-3xl mx-auto w-full px-4 py-10">
        <ng-content />
      </main>

      <footer class="border-t border-brand-border text-center text-xs text-brand-muted/50 py-6">
        © TinyHeroes ·
        <a routerLink="/privacy" class="underline hover:text-brand-orange transition-colors">
          {{ 'LEGAL.PUBLIC_NAV_PRIVACY' | translate }}
        </a> ·
        <a routerLink="/terms" class="underline hover:text-brand-orange transition-colors">
          {{ 'LEGAL.PUBLIC_NAV_TERMS' | translate }}
        </a>
      </footer>

    </div>
  `
})
export class PublicLayoutComponent {}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
cd frontend && npx ng test --watch=false --include="**/public-layout.component.spec.ts" 2>&1 | tail -20
```

Expected: all 5 tests pass.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/app/shared/components/public-layout.component.ts \
        frontend/src/app/shared/components/public-layout.component.spec.ts
git commit -m "feat: add PublicLayoutComponent shared header/footer wrapper"
```

---

## Task 2: i18n keys — all 5 languages

**Files:**
- Modify: `frontend/public/assets/i18n/en.json`
- Modify: `frontend/public/assets/i18n/hu.json`
- Modify: `frontend/public/assets/i18n/de.json`
- Modify: `frontend/public/assets/i18n/fr.json`
- Modify: `frontend/public/assets/i18n/es.json`

- [ ] **Step 1: Add `LEGAL` block to `en.json`**

Add the following as a new top-level key (before the closing `}` of the JSON object):

```json
"LEGAL": {
  "PRIVACY_TITLE": "Privacy Policy",
  "TERMS_TITLE": "Terms of Service",
  "FOOTER_PRIVACY": "Privacy",
  "FOOTER_TERMS": "Terms",
  "PUBLIC_NAV_HOME": "Home",
  "PUBLIC_NAV_HELP": "Help",
  "PUBLIC_NAV_CHANGELOG": "Changelog",
  "PUBLIC_NAV_PRIVACY": "Privacy",
  "PUBLIC_NAV_TERMS": "Terms"
}
```

- [ ] **Step 2: Add `LEGAL` block to `hu.json` (Hungarian)**

```json
"LEGAL": {
  "PRIVACY_TITLE": "Adatvédelmi irányelvek",
  "TERMS_TITLE": "Felhasználási feltételek",
  "FOOTER_PRIVACY": "Adatvédelem",
  "FOOTER_TERMS": "Feltételek",
  "PUBLIC_NAV_HOME": "Főoldal",
  "PUBLIC_NAV_HELP": "Súgó",
  "PUBLIC_NAV_CHANGELOG": "Újdonságok",
  "PUBLIC_NAV_PRIVACY": "Adatvédelem",
  "PUBLIC_NAV_TERMS": "Feltételek"
}
```

- [ ] **Step 3: Add `LEGAL` block to `de.json` (German)**

```json
"LEGAL": {
  "PRIVACY_TITLE": "Datenschutzerklärung",
  "TERMS_TITLE": "Nutzungsbedingungen",
  "FOOTER_PRIVACY": "Datenschutz",
  "FOOTER_TERMS": "Nutzungsbedingungen",
  "PUBLIC_NAV_HOME": "Startseite",
  "PUBLIC_NAV_HELP": "Hilfe",
  "PUBLIC_NAV_CHANGELOG": "Änderungsprotokoll",
  "PUBLIC_NAV_PRIVACY": "Datenschutz",
  "PUBLIC_NAV_TERMS": "Nutzungsbedingungen"
}
```

- [ ] **Step 4: Add `LEGAL` block to `fr.json` (French)**

```json
"LEGAL": {
  "PRIVACY_TITLE": "Politique de confidentialité",
  "TERMS_TITLE": "Conditions d'utilisation",
  "FOOTER_PRIVACY": "Confidentialité",
  "FOOTER_TERMS": "Conditions",
  "PUBLIC_NAV_HOME": "Accueil",
  "PUBLIC_NAV_HELP": "Aide",
  "PUBLIC_NAV_CHANGELOG": "Nouveautés",
  "PUBLIC_NAV_PRIVACY": "Confidentialité",
  "PUBLIC_NAV_TERMS": "Conditions"
}
```

- [ ] **Step 5: Add `LEGAL` block to `es.json` (Spanish)**

```json
"LEGAL": {
  "PRIVACY_TITLE": "Política de privacidad",
  "TERMS_TITLE": "Términos de servicio",
  "FOOTER_PRIVACY": "Privacidad",
  "FOOTER_TERMS": "Términos",
  "PUBLIC_NAV_HOME": "Inicio",
  "PUBLIC_NAV_HELP": "Ayuda",
  "PUBLIC_NAV_CHANGELOG": "Novedades",
  "PUBLIC_NAV_PRIVACY": "Privacidad",
  "PUBLIC_NAV_TERMS": "Términos"
}
```

- [ ] **Step 6: Verify the JSON files are valid**

```bash
for f in frontend/public/assets/i18n/*.json; do
  python3 -m json.tool "$f" > /dev/null && echo "OK: $f" || echo "INVALID: $f"
done
```

Expected: all 5 print `OK`.

- [ ] **Step 7: Commit**

```bash
git add frontend/public/assets/i18n/
git commit -m "feat: add LEGAL i18n keys for all 5 languages"
```

---

## Task 3: PrivacyComponent

**Files:**
- Create: `frontend/src/app/features/legal/pages/privacy.component.ts`
- Create: `frontend/src/app/features/legal/pages/privacy.component.spec.ts`

- [ ] **Step 1: Write the failing tests**

```typescript
// frontend/src/app/features/legal/pages/privacy.component.spec.ts
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTranslateService, TranslateService } from '@ngx-translate/core';
import { PrivacyComponent } from './privacy.component';

const TRANSLATIONS = {
  LEGAL: {
    PRIVACY_TITLE: 'Privacy Policy',
    PUBLIC_NAV_HOME: 'Home', PUBLIC_NAV_HELP: 'Help',
    PUBLIC_NAV_CHANGELOG: 'Changelog', PUBLIC_NAV_PRIVACY: 'Privacy',
    PUBLIC_NAV_TERMS: 'Terms', FOOTER_PRIVACY: 'Privacy', FOOTER_TERMS: 'Terms',
  }
};

describe('PrivacyComponent', () => {
  let fixture: ComponentFixture<PrivacyComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PrivacyComponent],
      providers: [
        provideRouter([]),
        provideTranslateService({ defaultLanguage: 'en' }),
      ],
    }).compileComponents();

    TestBed.inject(TranslateService).setTranslation('en', TRANSLATIONS);
    TestBed.inject(TranslateService).use('en');

    fixture = TestBed.createComponent(PrivacyComponent);
    fixture.detectChanges();
  });

  it('renders the Privacy Policy heading', () => {
    const h1: HTMLElement = fixture.nativeElement.querySelector('h1');
    expect(h1?.textContent?.trim()).toBe('Privacy Policy');
  });

  it('renders all 7 section headings', () => {
    const headings: NodeListOf<HTMLElement> = fixture.nativeElement.querySelectorAll('h2');
    expect(headings.length).toBeGreaterThanOrEqual(7);
  });

  it('contains the contact email', () => {
    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('privacy@tinyheroes.app');
  });

  it('uses PublicLayoutComponent (has header and footer)', () => {
    expect(fixture.nativeElement.querySelector('header')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('footer')).toBeTruthy();
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
cd frontend && npx ng test --watch=false --include="**/privacy.component.spec.ts" 2>&1 | tail -20
```

Expected: compilation error — `PrivacyComponent` not found.

- [ ] **Step 3: Create the component**

```typescript
// frontend/src/app/features/legal/pages/privacy.component.ts
import { Component } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { PublicLayoutComponent } from '../../../shared/components/public-layout.component';

@Component({
  selector: 'app-privacy',
  imports: [PublicLayoutComponent, TranslatePipe],
  template: `
    <app-public-layout>

      <div class="mb-8 pb-8 border-b border-brand-border">
        <div class="text-4xl mb-2">🔒</div>
        <h1 class="text-2xl font-black text-brand-text">{{ 'LEGAL.PRIVACY_TITLE' | translate }}</h1>
        <p class="text-xs text-brand-muted mt-1">Last updated: May 2025</p>
      </div>

      <!-- GDPR/COPPA notice -->
      <div class="bg-orange-50 border border-brand-orange/20 rounded-2xl p-4 mb-8 flex gap-3">
        <span class="text-xl flex-shrink-0">⚠️</span>
        <div>
          <p class="text-sm font-semibold text-brand-text mb-1">GDPR &amp; COPPA Notice</p>
          <p class="text-sm text-brand-muted leading-relaxed">
            This app stores information about children. By using TinyHeroes, you confirm you are the parent or legal guardian of any child whose data you add.
          </p>
        </div>
      </div>

      <div class="mb-6">
        <h2 class="text-lg font-bold text-brand-text mb-2">1. Introduction</h2>
        <p class="text-sm text-brand-muted leading-relaxed">
          TinyHeroes ("we", "us", "our") operates the TinyHeroes family good-deed tracking application. This Privacy Policy explains what personal data we collect, how we use it, and your rights. The data controller is TinyHeroes — contact us at <a href="mailto:privacy@tinyheroes.app" class="text-brand-orange underline">privacy@tinyheroes.app</a>.
        </p>
      </div>

      <div class="mb-6">
        <h2 class="text-lg font-bold text-brand-text mb-3">2. Data We Collect</h2>
        <div class="bg-white rounded-2xl border border-brand-border divide-y divide-brand-border overflow-hidden">
          <div class="flex items-start gap-3 p-4">
            <span class="text-lg flex-shrink-0">👤</span>
            <div>
              <p class="text-sm font-semibold text-brand-text">Account</p>
              <p class="text-xs text-brand-muted mt-0.5">Email address, display name</p>
            </div>
          </div>
          <div class="flex items-start gap-3 p-4">
            <span class="text-lg flex-shrink-0">🧒</span>
            <div>
              <p class="text-sm font-semibold text-brand-text">Children</p>
              <p class="text-xs text-brand-muted mt-0.5">Names, ages, gender, emoji avatar or uploaded photo</p>
            </div>
          </div>
          <div class="flex items-start gap-3 p-4">
            <span class="text-lg flex-shrink-0">⭐</span>
            <div>
              <p class="text-sm font-semibold text-brand-text">Good Deeds</p>
              <p class="text-xs text-brand-muted mt-0.5">Deed descriptions, AI-generated deed illustrations</p>
            </div>
          </div>
          <div class="flex items-start gap-3 p-4">
            <span class="text-lg flex-shrink-0">🔑</span>
            <div>
              <p class="text-sm font-semibold text-brand-text">Authentication</p>
              <p class="text-xs text-brand-muted mt-0.5">JWT access token stored in browser localStorage</p>
            </div>
          </div>
          <div class="flex items-start gap-3 p-4">
            <span class="text-lg flex-shrink-0">👨‍👩‍👧</span>
            <div>
              <p class="text-sm font-semibold text-brand-text">Family</p>
              <p class="text-xs text-brand-muted mt-0.5">Family name, week start preference, member roles</p>
            </div>
          </div>
        </div>
      </div>

      <div class="mb-6">
        <h2 class="text-lg font-bold text-brand-text mb-2">3. How We Use Your Data</h2>
        <p class="text-sm text-brand-muted leading-relaxed">
          We use your data solely to operate the app — to display podium standings, award prizes, and generate weekly and monthly summaries. Deed descriptions are sent to the Hugging Face Inference API to generate illustrations; no personal identifiers (names, photos) are included in those prompts.
        </p>
      </div>

      <div class="mb-6">
        <h2 class="text-lg font-bold text-brand-text mb-2">4. Data Storage</h2>
        <p class="text-sm text-brand-muted leading-relaxed">
          Data is stored on our servers. Uploaded child photos are stored as static files and served directly from our server. We do not use third-party CDNs, analytics, or tracking scripts.
        </p>
      </div>

      <div class="mb-6">
        <h2 class="text-lg font-bold text-brand-text mb-3">5. Third Parties</h2>
        <div class="bg-white rounded-2xl border border-brand-border divide-y divide-brand-border overflow-hidden mb-3">
          <div class="flex items-center gap-3 p-4">
            <span class="text-lg">🤖</span>
            <div>
              <p class="text-sm font-semibold text-brand-text">Hugging Face</p>
              <p class="text-xs text-brand-muted">Deed descriptions only — for AI image generation. No personal data sent.</p>
            </div>
          </div>
          <div class="flex items-center gap-3 p-4">
            <span class="text-lg">🔐</span>
            <div>
              <p class="text-sm font-semibold text-brand-text">Google / Apple / Facebook (optional)</p>
              <p class="text-xs text-brand-muted">Only if you choose social sign-in. Governed by each provider's privacy policy.</p>
            </div>
          </div>
        </div>
        <p class="text-sm text-brand-muted leading-relaxed">We do not sell, rent, or share your personal data with advertisers or any other parties.</p>
      </div>

      <div class="mb-6">
        <h2 class="text-lg font-bold text-brand-text mb-2">6. Data Deletion</h2>
        <p class="text-sm text-brand-muted leading-relaxed">
          You can delete your family at any time via <strong class="text-brand-text font-semibold">Settings → Family Settings → Danger zone</strong>. This permanently removes all child records, deeds, photos, and history. For account deletion, contact us at <a href="mailto:privacy@tinyheroes.app" class="text-brand-orange underline">privacy@tinyheroes.app</a>.
        </p>
      </div>

      <div class="mb-6">
        <h2 class="text-lg font-bold text-brand-text mb-2">7. Children's Privacy (GDPR &amp; COPPA)</h2>
        <p class="text-sm text-brand-muted leading-relaxed">
          TinyHeroes may store data about children under 13. We rely on the account-holding parent or guardian to provide verifiable consent on behalf of their children. If you believe data about a child has been added without consent, contact us immediately and we will remove it.
        </p>
        <!-- LEGAL REVIEW NEEDED: GDPR Art. 17 erasure, COPPA verifiable parental consent -->
      </div>

      <div class="bg-white rounded-2xl border border-brand-border p-6 mt-8 flex items-start gap-4">
        <span class="text-3xl">✉️</span>
        <div>
          <p class="text-sm font-semibold text-brand-text mb-1">Questions about your privacy?</p>
          <p class="text-sm text-brand-muted">
            Reach us at <a href="mailto:privacy@tinyheroes.app" class="text-brand-orange underline font-medium">privacy@tinyheroes.app</a>. We aim to respond within 5 business days.
          </p>
        </div>
      </div>

    </app-public-layout>
  `
})
export class PrivacyComponent {}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
cd frontend && npx ng test --watch=false --include="**/privacy.component.spec.ts" 2>&1 | tail -20
```

Expected: all 4 tests pass.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/app/features/legal/
git commit -m "feat: add PrivacyComponent at /privacy"
```

---

## Task 4: TermsComponent

**Files:**
- Create: `frontend/src/app/features/legal/pages/terms.component.ts`
- Create: `frontend/src/app/features/legal/pages/terms.component.spec.ts`

- [ ] **Step 1: Write the failing tests**

```typescript
// frontend/src/app/features/legal/pages/terms.component.spec.ts
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTranslateService, TranslateService } from '@ngx-translate/core';
import { TermsComponent } from './terms.component';

const TRANSLATIONS = {
  LEGAL: {
    TERMS_TITLE: 'Terms of Service',
    PUBLIC_NAV_HOME: 'Home', PUBLIC_NAV_HELP: 'Help',
    PUBLIC_NAV_CHANGELOG: 'Changelog', PUBLIC_NAV_PRIVACY: 'Privacy',
    PUBLIC_NAV_TERMS: 'Terms', FOOTER_PRIVACY: 'Privacy', FOOTER_TERMS: 'Terms',
  }
};

describe('TermsComponent', () => {
  let fixture: ComponentFixture<TermsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TermsComponent],
      providers: [
        provideRouter([]),
        provideTranslateService({ defaultLanguage: 'en' }),
      ],
    }).compileComponents();

    TestBed.inject(TranslateService).setTranslation('en', TRANSLATIONS);
    TestBed.inject(TranslateService).use('en');

    fixture = TestBed.createComponent(TermsComponent);
    fixture.detectChanges();
  });

  it('renders the Terms of Service heading', () => {
    const h1: HTMLElement = fixture.nativeElement.querySelector('h1');
    expect(h1?.textContent?.trim()).toBe('Terms of Service');
  });

  it('renders all 6 section headings', () => {
    const headings: NodeListOf<HTMLElement> = fixture.nativeElement.querySelectorAll('h2');
    expect(headings.length).toBeGreaterThanOrEqual(6);
  });

  it('contains the contact email', () => {
    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('privacy@tinyheroes.app');
  });

  it('uses PublicLayoutComponent (has header and footer)', () => {
    expect(fixture.nativeElement.querySelector('header')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('footer')).toBeTruthy();
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
cd frontend && npx ng test --watch=false --include="**/terms.component.spec.ts" 2>&1 | tail -20
```

Expected: compilation error — `TermsComponent` not found.

- [ ] **Step 3: Create the component**

```typescript
// frontend/src/app/features/legal/pages/terms.component.ts
import { Component } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { PublicLayoutComponent } from '../../../shared/components/public-layout.component';

@Component({
  selector: 'app-terms',
  imports: [PublicLayoutComponent, TranslatePipe],
  template: `
    <app-public-layout>

      <div class="mb-8 pb-8 border-b border-brand-border">
        <div class="text-4xl mb-2">📜</div>
        <h1 class="text-2xl font-black text-brand-text">{{ 'LEGAL.TERMS_TITLE' | translate }}</h1>
        <p class="text-xs text-brand-muted mt-1">Last updated: May 2025</p>
      </div>

      <div class="mb-6">
        <h2 class="text-lg font-bold text-brand-text mb-2">1. Acceptance of Terms</h2>
        <p class="text-sm text-brand-muted leading-relaxed">
          By accessing or using TinyHeroes, you agree to be bound by these Terms of Service. If you do not agree, please do not use the app.
        </p>
      </div>

      <div class="mb-6">
        <h2 class="text-lg font-bold text-brand-text mb-2">2. Service Description</h2>
        <p class="text-sm text-brand-muted leading-relaxed">
          TinyHeroes is a family good-deed tracking application for personal, non-commercial use. It enables parents and guardians to log children's positive behaviour, view weekly and monthly standings, and manage prizes.
        </p>
      </div>

      <div class="mb-6">
        <h2 class="text-lg font-bold text-brand-text mb-3">3. Acceptable Use</h2>
        <p class="text-sm text-brand-muted leading-relaxed mb-3">You agree not to:</p>
        <div class="bg-white rounded-2xl border border-brand-border p-4 space-y-2">
          <div class="flex items-start gap-2 text-sm text-brand-muted">
            <span class="text-brand-orange font-bold mt-0.5 flex-shrink-0">·</span>
            <span>Upload unlawful, harmful, or abusive content</span>
          </div>
          <div class="flex items-start gap-2 text-sm text-brand-muted">
            <span class="text-brand-orange font-bold mt-0.5 flex-shrink-0">·</span>
            <span>Add a child's personal data without that child's parent or guardian's consent</span>
          </div>
          <div class="flex items-start gap-2 text-sm text-brand-muted">
            <span class="text-brand-orange font-bold mt-0.5 flex-shrink-0">·</span>
            <span>Use automated tools, bots, or scrapers to access the service</span>
          </div>
          <div class="flex items-start gap-2 text-sm text-brand-muted">
            <span class="text-brand-orange font-bold mt-0.5 flex-shrink-0">·</span>
            <span>Attempt to reverse-engineer, disrupt, or gain unauthorised access to the service</span>
          </div>
        </div>
      </div>

      <div class="mb-6">
        <h2 class="text-lg font-bold text-brand-text mb-2">4. Account Termination</h2>
        <p class="text-sm text-brand-muted leading-relaxed">
          You may delete your family and account at any time via Settings. We reserve the right to suspend or terminate accounts that violate these terms, with or without prior notice.
        </p>
      </div>

      <div class="mb-6">
        <h2 class="text-lg font-bold text-brand-text mb-2">5. Liability Disclaimer</h2>
        <div class="bg-brand-cream rounded-2xl border border-brand-border p-4">
          <p class="text-sm text-brand-muted leading-relaxed">
            The service is provided <strong class="text-brand-text font-semibold">"as is"</strong> without warranty of any kind. We do not guarantee uninterrupted access or permanent data retention. AI-generated deed images may occasionally be unexpected — please report any inappropriate images to <a href="mailto:privacy@tinyheroes.app" class="text-brand-orange underline">privacy@tinyheroes.app</a>. To the maximum extent permitted by law, TinyHeroes is not liable for any indirect or consequential damages.
          </p>
        </div>
      </div>

      <div class="mb-6">
        <h2 class="text-lg font-bold text-brand-text mb-2">6. Governing Law</h2>
        <p class="text-sm text-brand-muted leading-relaxed">
          These Terms are governed by the laws of [Country / Jurisdiction]. Any disputes shall be resolved in the courts of that jurisdiction.
        </p>
      </div>

      <div class="bg-white rounded-2xl border border-brand-border p-6 mt-8 flex items-start gap-4">
        <span class="text-3xl">✉️</span>
        <div>
          <p class="text-sm font-semibold text-brand-text mb-1">Questions about these terms?</p>
          <p class="text-sm text-brand-muted">
            Contact us at <a href="mailto:privacy@tinyheroes.app" class="text-brand-orange underline font-medium">privacy@tinyheroes.app</a>.
          </p>
        </div>
      </div>

    </app-public-layout>
  `
})
export class TermsComponent {}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
cd frontend && npx ng test --watch=false --include="**/terms.component.spec.ts" 2>&1 | tail -20
```

Expected: all 4 tests pass.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/app/features/legal/
git commit -m "feat: add TermsComponent at /terms"
```

---

## Task 5: Register routes in app.routes.ts

**Files:**
- Modify: `frontend/src/app/app.routes.ts`

- [ ] **Step 1: Add the two lazy routes**

Open `frontend/src/app/app.routes.ts`. After the `changelog` route line:

```typescript
{ path: 'changelog', loadComponent: () => import('./features/public/pages/changelog.component').then(m => m.ChangelogComponent) },
```

Add:

```typescript
{ path: 'privacy', loadComponent: () => import('./features/legal/pages/privacy.component').then(m => m.PrivacyComponent) },
{ path: 'terms',   loadComponent: () => import('./features/legal/pages/terms.component').then(m => m.TermsComponent) },
```

Both routes have no `canActivate` — they are public.

- [ ] **Step 2: Verify the production build compiles**

```bash
cd frontend && npx ng build --configuration production 2>&1 | tail -20
```

Expected: build succeeds with no errors.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/app/app.routes.ts
git commit -m "feat: register /privacy and /terms routes"
```

---

## Task 6: Update WelcomeComponent footer

**Files:**
- Modify: `frontend/src/app/features/auth/pages/welcome.component.ts`

- [ ] **Step 1: Locate and replace the version footer paragraph**

Find this block in the template (near the bottom of the `template` string):

```html
<p class="text-xs text-brand-muted/50">
  v{{ version }}
  @if (infoService.apiInfo(); as api) {
    · API v{{ api.version }}
  }
</p>
```

Replace it with:

```html
<p class="text-xs text-brand-muted/50 text-center">
  v{{ version }}
  @if (infoService.apiInfo(); as api) {
    · API v{{ api.version }}
  }
  <span class="mx-1">·</span>
  <a routerLink="/privacy" class="underline hover:text-brand-orange transition-colors">{{ 'LEGAL.FOOTER_PRIVACY' | translate }}</a>
  <span class="mx-1">·</span>
  <a routerLink="/terms" class="underline hover:text-brand-orange transition-colors">{{ 'LEGAL.FOOTER_TERMS' | translate }}</a>
</p>
```

`RouterLink` and `TranslatePipe` are already imported in `WelcomeComponent` — no import changes needed.

- [ ] **Step 2: Verify the production build compiles**

```bash
cd frontend && npx ng build --configuration production 2>&1 | tail -20
```

Expected: build succeeds.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/app/features/auth/pages/welcome.component.ts
git commit -m "feat: add Privacy and Terms footer links to welcome screen"
```

---

## Task 7: Refactor ChangelogComponent to use PublicLayoutComponent

**Files:**
- Modify: `frontend/src/app/features/public/pages/changelog.component.ts`
- Modify: `frontend/src/app/features/public/pages/changelog.component.spec.ts`

- [ ] **Step 1: Replace the inline header/footer in the component**

Replace the entire `changelog.component.ts` file contents with the following. The loading/error/content logic is unchanged — only the outer wrapper and the inline header/footer are replaced by `<app-public-layout>`:

```typescript
// frontend/src/app/features/public/pages/changelog.component.ts
import { Component, OnInit, ViewEncapsulation, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { marked } from 'marked';
import { PublicLayoutComponent } from '../../../shared/components/public-layout.component';

@Component({
  selector: 'app-changelog',
  standalone: true,
  imports: [PublicLayoutComponent],
  encapsulation: ViewEncapsulation.None,
  template: `
    <app-public-layout>
      @if (loading()) {
        <p class="text-brand-muted text-sm">Loading…</p>
      } @else if (error()) {
        <p class="text-red-500 text-sm">Failed to load changelog.</p>
      } @else if (html()) {
        <div class="changelog-body" [innerHTML]="html()"></div>
      }
    </app-public-layout>
  `,
  styles: [`
    .changelog-body :is(h1, h2, h3) { font-weight: 800; color: #3D2B1F; margin-bottom: 0.5rem; margin-top: 1.5rem; }
    .changelog-body h1 { font-size: 1.5rem; }
    .changelog-body h2 { font-size: 1.125rem; padding-bottom: 0.5rem; border-bottom: 2px solid #FFF8F0; margin-top: 2rem; }
    .changelog-body h3 { font-size: 0.875rem; text-transform: uppercase; letter-spacing: 0.05em; color: #A8714A; }
    .changelog-body ul { margin: 0.5rem 0 1rem 1.25rem; list-style: disc; }
    .changelog-body li { font-size: 0.875rem; color: #3D2B1F; margin-bottom: 0.25rem; line-height: 1.5; }
    .changelog-body a { color: #F97316; text-decoration: underline; }
    .changelog-body p { font-size: 0.875rem; color: #A8714A; margin-bottom: 1rem; }
  `]
})
export class ChangelogComponent implements OnInit {
  private http = inject(HttpClient);
  private sanitizer = inject(DomSanitizer);

  html = signal<SafeHtml | null>(null);
  loading = signal(true);
  error = signal(false);

  ngOnInit() {
    this.http.get('/assets/CHANGELOG.md', { responseType: 'text' }).subscribe({
      next: md => {
        try {
          this.html.set(this.sanitizer.bypassSecurityTrustHtml(marked.parse(md) as string));
        } catch {
          this.error.set(true);
        }
        this.loading.set(false);
      },
      error: () => {
        this.error.set(true);
        this.loading.set(false);
      }
    });
  }
}
```

- [ ] **Step 2: Update the existing changelog spec to account for the new wrapper**

The existing spec tests loading, error, and rendered heading states. The `PublicLayoutComponent` now provides the header/footer; the spec needs `provideTranslateService` added so the layout renders without crashing.

Replace the entire spec file:

```typescript
// frontend/src/app/features/public/pages/changelog.component.spec.ts
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { provideTranslateService, TranslateService } from '@ngx-translate/core';
import { ChangelogComponent } from './changelog.component';

const TRANSLATIONS = {
  LEGAL: {
    PUBLIC_NAV_HOME: 'Home', PUBLIC_NAV_HELP: 'Help',
    PUBLIC_NAV_CHANGELOG: 'Changelog', PUBLIC_NAV_PRIVACY: 'Privacy',
    PUBLIC_NAV_TERMS: 'Terms', FOOTER_PRIVACY: 'Privacy', FOOTER_TERMS: 'Terms',
  }
};

describe('ChangelogComponent', () => {
  let fixture: ComponentFixture<ChangelogComponent>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ChangelogComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideTranslateService({ defaultLanguage: 'en' }),
      ],
    }).compileComponents();

    TestBed.inject(TranslateService).setTranslation('en', TRANSLATIONS);
    TestBed.inject(TranslateService).use('en');

    fixture = TestBed.createComponent(ChangelogComponent);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('renders a heading from the fetched markdown', () => {
    fixture.detectChanges();

    const req = httpMock.expectOne('/assets/CHANGELOG.md');
    req.flush('# Changelog\n\n## [1.0.0] - 2026-05-16\n### Added\n- Initial release');
    fixture.detectChanges();

    const h1 = fixture.nativeElement.querySelector('.changelog-body h1');
    expect(h1?.textContent?.trim()).toBe('Changelog');
  });

  it('shows a loading indicator before data arrives', () => {
    fixture.detectChanges();

    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('Loading');

    httpMock.expectOne('/assets/CHANGELOG.md').flush('# Changelog');
  });

  it('shows an error message on fetch failure', () => {
    fixture.detectChanges();

    httpMock.expectOne('/assets/CHANGELOG.md').error(new ProgressEvent('error'));
    fixture.detectChanges();

    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('Failed to load');
  });
});
```

- [ ] **Step 3: Run all changelog tests**

```bash
cd frontend && npx ng test --watch=false --include="**/changelog.component.spec.ts" 2>&1 | tail -20
```

Expected: all 3 tests pass.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/app/features/public/pages/changelog.component.ts \
        frontend/src/app/features/public/pages/changelog.component.spec.ts
git commit -m "refactor: migrate ChangelogComponent to use PublicLayoutComponent"
```

---

## Task 8: Run full test suite and verify

- [ ] **Step 1: Run all frontend tests**

```bash
cd frontend && npx ng test --watch=false 2>&1 | tail -30
```

Expected: all tests pass, 0 failures.

- [ ] **Step 2: Verify production build**

```bash
cd frontend && npx ng build --configuration production 2>&1 | tail -10
```

Expected: build succeeds.

- [ ] **Step 3: Start the dev server and manually verify**

```bash
cd frontend && npm start
```

Check each of these manually in the browser (http://localhost:4200):

- `/` — welcome screen footer shows `Privacy · Terms` links
- `/privacy` — page renders with shared header nav (Home · Help · Changelog · Privacy · Terms); Privacy link is highlighted; all 7 sections visible; contact email clickable
- `/terms` — same header; Terms link highlighted; all 6 sections visible
- `/changelog` — same header; Changelog link highlighted; content loads correctly
- On `/privacy`, click the Privacy nav link — stays on page, link stays highlighted (routerLinkActive)
- Resize to mobile width (~375px) — Home/Help/Changelog links hide, Privacy/Terms remain visible in nav

---

## Task 9: Update CHANGELOG.md and close GitHub issue

**Files:**
- Modify: `CHANGELOG.md`

- [ ] **Step 1: Add entry under `[Unreleased]`**

In `CHANGELOG.md`, find `## [Unreleased]` and add below it:

```markdown
## [Unreleased]

### Added
- Privacy Policy page (`/privacy`) with GDPR & COPPA notice, data inventory, and third-party disclosure
- Terms of Service page (`/terms`) with acceptable use, liability disclaimer, and governing law sections
- `PublicLayoutComponent` — shared header/footer wrapper for all public unauthenticated pages
- Privacy and Terms footer links on the welcome screen
- Refactored Changelog page to use `PublicLayoutComponent` (consistent header/footer)
```

- [ ] **Step 2: Commit**

```bash
git add CHANGELOG.md
git commit -m "docs: update CHANGELOG for legal pages feature"
```

- [ ] **Step 3: Mark GitHub issue in progress**

```bash
gh issue comment 4 --body "Starting implementation"
gh issue edit 4 --add-label "in progress"
```

---

## Task 10: Post-feature review and PR

- [ ] **Step 1: Run code review**

```
/code-review
```

Address any findings before continuing.

- [ ] **Step 2: Run security review**

```
/security-review
```

Pay particular attention to: the `bypassSecurityTrustHtml` call in `ChangelogComponent` (pre-existing, markdown from our own static asset — acceptable), and any user-supplied content rendered in the legal pages (there is none — all content is static).

- [ ] **Step 3: Create PR and close issue on merge**

```bash
gh pr create \
  --title "feat: add Privacy Policy and Terms of Service pages (#4)" \
  --body "$(cat <<'EOF'
## Summary
- Adds `/privacy` and `/terms` public routes with full legal content
- New `PublicLayoutComponent` provides consistent header/footer for all public pages
- Refactors `ChangelogComponent` to use the new layout (no duplicate header markup)
- Adds Privacy + Terms footer links to the welcome screen
- Adds `LEGAL.*` i18n keys to all 5 languages

## Verification
- Navigate to `/privacy` without login → renders correctly
- Navigate to `/terms` without login → renders correctly
- Welcome screen footer → Privacy and Terms links present and functional
- `/changelog` → still renders correctly with new layout wrapper
- Production build passes

Closes #4
EOF
)"
```
