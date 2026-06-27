# Public Changelog Page Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a publicly accessible `/changelog` route that renders `CHANGELOG.md` from a static asset as formatted HTML.

**Architecture:** `CHANGELOG.md` is placed at the repo root (source of truth) and copied to `frontend/public/assets/CHANGELOG.md` so Angular can fetch it as a static asset under the `/assets/` path (which `staticwebapp.config.json` excludes from the SPA fallback rewrite). The `ChangelogComponent` fetches it via `HttpClient`, parses it with `marked`, and renders the HTML using Angular's `DomSanitizer`. The component is self-contained with its own header, following the same pattern as the existing `HelpComponent` (no dependency on the not-yet-implemented `PublicLayoutComponent` from issue #4).

**Tech Stack:** Angular 21 standalone, `marked` npm package (markdown → HTML), `DomSanitizer`, Tailwind v4 utility classes (no `prose` plugin — not installed)

---

## File Map

| File | Action |
|------|--------|
| `CHANGELOG.md` | Create — repo root, Keep a Changelog format |
| `frontend/public/assets/CHANGELOG.md` | Create — copy (not symlink) of root `CHANGELOG.md` |
| `frontend/src/app/features/public/pages/changelog.component.ts` | Create — page component |
| `frontend/src/app/features/public/pages/changelog.component.spec.ts` | Create — component tests |
| `frontend/src/app/app.routes.ts` | Modify — add `/changelog` route |
| `CLAUDE.md` | Modify — add changelog-update rule to Versioning section |

---

## Task 1: Create `CHANGELOG.md` at repo root

**Files:**
- Create: `CHANGELOG.md`

- [ ] **Step 1: Write the file**

```markdown
# Changelog

All notable changes to TinyHeroes are documented here.
Format: [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [Unreleased]

## [1.2.0] - 2026-05-31
### Added
- Prize eligibility: families can set minimum weekly/monthly deed thresholds
- Prize Rules settings page (`/settings/prize-rules`)
- Weekly podium: ineligible children are dimmed with a motivational empty state
- Monthly champion: ineligible champion is hidden with an empty state

## [1.0.0] - 2026-05-16
### Added
- Initial release: family creation, child profiles, good deed tracking
- Weekly podium with 1st/2nd/3rd prizes
- Monthly champion with grand prize
- Prize management (built-in presets + custom prizes)
- Social login (Google, Apple, Facebook)
- 5-language support (EN, HU, DE, FR, ES)
- AI-generated deed images (Hugging Face FLUX.1-schnell)
```

Write this to `/Volumes/PersonalProtected/GIT/TinyHeroes/CHANGELOG.md`.

- [ ] **Step 2: Copy to frontend public assets**

```bash
cp CHANGELOG.md frontend/public/assets/CHANGELOG.md
```

Note: this is a plain copy, not a symlink. Angular's build tool serves `frontend/public/` as static files. The `staticwebapp.config.json` excludes `/assets/*` from SPA fallback rewrites, so `GET /assets/CHANGELOG.md` returns the file as-is.

- [ ] **Step 3: Commit**

```bash
git add CHANGELOG.md frontend/public/assets/CHANGELOG.md
git commit -m "docs: add CHANGELOG.md at repo root and as public asset"
```

---

## Task 2: Install `marked` dependency

**Files:**
- Modify: `frontend/package.json`

- [ ] **Step 1: Install the package**

```bash
cd frontend && npm install marked
```

Expected: `marked` appears in `dependencies` in `package.json`.

- [ ] **Step 2: Verify install**

```bash
cd frontend && node -e "const { marked } = require('marked'); console.log(marked('# ok'))"
```

Expected output: `<h1>ok</h1>`

- [ ] **Step 3: Commit**

```bash
git add frontend/package.json frontend/package-lock.json
git commit -m "feat: add marked dependency for markdown rendering"
```

---

## Task 3: Write failing component test

**Files:**
- Create: `frontend/src/app/features/public/pages/changelog.component.spec.ts`

- [ ] **Step 1: Write the test**

Create the file at `frontend/src/app/features/public/pages/changelog.component.spec.ts`:

```typescript
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ChangelogComponent } from './changelog.component';

describe('ChangelogComponent', () => {
  let fixture: ComponentFixture<ChangelogComponent>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ChangelogComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(ChangelogComponent);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('renders a heading from the fetched markdown', () => {
    fixture.detectChanges();

    const req = httpMock.expectOne('/assets/CHANGELOG.md');
    req.flush('# Changelog\n\n## [1.0.0] - 2026-05-16\n### Added\n- Initial release');

    fixture.detectChanges();

    const h1 = fixture.nativeElement.querySelector('h1');
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

- [ ] **Step 2: Run the tests to confirm they fail**

```bash
cd frontend && npx ng test --watch=false --include="src/app/features/public/pages/changelog.component.spec.ts"
```

Expected: compilation error — `changelog.component` does not exist yet.

---

## Task 4: Implement `ChangelogComponent`

**Files:**
- Create: `frontend/src/app/features/public/pages/changelog.component.ts`

- [ ] **Step 1: Create the directory**

```bash
mkdir -p frontend/src/app/features/public/pages
```

- [ ] **Step 2: Write the component**

Create `frontend/src/app/features/public/pages/changelog.component.ts`:

```typescript
import { Component, OnInit, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { marked } from 'marked';

@Component({
  selector: 'app-changelog',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="min-h-screen bg-brand-cream">

      <!-- Header -->
      <header class="bg-white border-b border-brand-border sticky top-0 z-50">
        <div class="max-w-3xl mx-auto px-4 h-14 flex items-center justify-between">
          <a routerLink="/" class="flex items-center gap-2">
            <span class="text-xl">🌟</span>
            <span class="font-black text-brand-orange text-base hidden sm:inline">TinyHeroes</span>
          </a>
          <span class="font-bold text-brand-text text-sm sm:text-base">Changelog</span>
          <div class="w-24"></div>
        </div>
      </header>

      <main class="max-w-3xl mx-auto px-4 py-8">
        @if (loading()) {
          <p class="text-brand-muted text-sm">Loading…</p>
        } @else if (error()) {
          <p class="text-red-500 text-sm">Failed to load changelog.</p>
        } @else {
          <div class="changelog-body" [innerHTML]="html()"></div>
        }
      </main>

      <!-- Footer -->
      <footer class="text-center text-xs text-brand-muted py-8 border-t border-brand-border">
        <a routerLink="/" class="text-brand-orange font-semibold hover:underline">← TinyHeroes</a>
      </footer>

    </div>
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

  html = signal<SafeHtml>('');
  loading = signal(true);
  error = signal(false);

  ngOnInit() {
    this.http.get('/assets/CHANGELOG.md', { responseType: 'text' }).subscribe({
      next: md => {
        this.html.set(this.sanitizer.bypassSecurityTrustHtml(marked.parse(md) as string));
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

- [ ] **Step 3: Run the tests**

```bash
cd frontend && npx ng test --watch=false --include="src/app/features/public/pages/changelog.component.spec.ts"
```

Expected: all 3 tests pass.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/app/features/public/pages/changelog.component.ts \
        frontend/src/app/features/public/pages/changelog.component.spec.ts
git commit -m "feat: add ChangelogComponent with markdown rendering"
```

---

## Task 5: Wire the route

**Files:**
- Modify: `frontend/src/app/app.routes.ts`

- [ ] **Step 1: Add the route**

In `frontend/src/app/app.routes.ts`, add the `/changelog` route in the unauthenticated section, just before the `{ path: '**', redirectTo: '' }` wildcard (i.e. as the last unauthenticated route, after the `help` route):

```typescript
{ path: 'changelog', loadComponent: () => import('./features/public/pages/changelog.component').then(m => m.ChangelogComponent) },
```

The full unauthenticated block (top-level routes, not inside the shell) should now include:

```typescript
{ path: '', loadComponent: () => import('./features/auth/pages/welcome.component').then(m => m.WelcomeComponent) },
{ path: 'login', loadComponent: () => import('./features/auth/pages/login.component').then(m => m.LoginComponent) },
{ path: 'signup', loadComponent: () => import('./features/auth/pages/signup.component').then(m => m.SignupComponent) },
{ path: 'forgot-password', loadComponent: () => import('./features/auth/pages/forgot-password.component').then(m => m.ForgotPasswordComponent) },
{ path: 'auth/callback', loadComponent: () => import('./features/auth/pages/callback.component').then(m => m.CallbackComponent) },
{ path: 'help', loadComponent: () => import('./features/help/help.component').then(m => m.HelpComponent) },
{ path: 'changelog', loadComponent: () => import('./features/public/pages/changelog.component').then(m => m.ChangelogComponent) },
```

- [ ] **Step 2: Run the production build check**

```bash
cd frontend && npx ng build --configuration production
```

Expected: build succeeds with no errors.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/app/app.routes.ts
git commit -m "feat: register /changelog route"
```

---

## Task 6: Update CLAUDE.md versioning section

**Files:**
- Modify: `CLAUDE.md`

- [ ] **Step 1: Locate the versioning section**

Find the line in `CLAUDE.md` that begins "When bumping the **backend** version, update only:" (around line 40–50 of the Backend — Versioning section).

- [ ] **Step 2: Add the changelog rule**

After the sentence "Both follow semantic versioning…" and before the end of the Versioning section, append:

```markdown
When making a version bump, also update `CHANGELOG.md` at the repo root and copy it to `frontend/public/assets/CHANGELOG.md` in the same commit. Commit message: `chore: bump <api|frontend> version to X.Y.Z`.
```

- [ ] **Step 3: Commit**

```bash
git add CLAUDE.md
git commit -m "docs: add changelog-update rule to CLAUDE.md versioning section"
```

---

## Task 7: Manual verification

- [ ] **Step 1: Start the dev server**

```bash
cd frontend && npm start
```

Wait for "Compiled successfully" output.

- [ ] **Step 2: Navigate to `/changelog`**

Open `http://localhost:4200/changelog` in a browser.

Expected:
- Page header shows "🌟 TinyHeroes" logo on left and "Changelog" title in centre
- Release `[1.2.0]` appears as the first release under the heading with its Added items as bullet points
- Release `[1.0.0]` appears below it
- Headings have visual hierarchy (h2 bigger than h3)
- Footer shows "← TinyHeroes" link back to `/`

- [ ] **Step 3: Verify it's publicly accessible (no auth redirect)**

Log out of the app (or open a private window) and navigate directly to `http://localhost:4200/changelog`.

Expected: page loads without being redirected to `/login` or `/`.

- [ ] **Step 4: Check page works without JavaScript-enabled deep-link issues**

Reload the page directly at `http://localhost:4200/changelog` (not via SPA navigation).

Expected: page loads correctly (the Angular router handles the route).

---

## Self-Review Checklist

All acceptance criteria from issue #6 are covered:

| Criterion | Task |
|-----------|------|
| `/changelog` route is publicly accessible | Task 5 |
| Content follows Keep a Changelog format | Task 1 |
| `CHANGELOG.md` updated in same commit as version bumps going forward | Task 6 |
| Uses shared header/footer layout | Task 4 (self-contained header matching `HelpComponent` pattern) |

**Note on `PublicLayoutComponent`:** Issue #4 introduces a `PublicLayoutComponent` that issue #6 was designed to reuse. Since issue #4 is not yet implemented, this plan follows the existing pattern (`HelpComponent` is also self-contained with its own header). If/when issue #4 is implemented, the `ChangelogComponent` header can be trivially refactored to use `PublicLayoutComponent` — it's a pure cosmetic refactor.

**Static asset path decision:** The changelog is fetched from `/assets/CHANGELOG.md` (not `/CHANGELOG.md`). This is because `staticwebapp.config.json` only excludes `/assets/*` from SPA rewrite — fetching from `/CHANGELOG.md` would return `index.html` instead of the file.
