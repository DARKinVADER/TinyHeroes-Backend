# Default Language Based on Location Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Auto-detect the visitor's preferred language from `navigator.language` on first visit, and add a flag+code language-selector pill to the public nav, authenticated side-nav, and bottom-nav so users can override it at any time.

**Architecture:** A new shared `LanguageSelectorComponent` owns the pill UI and dropdown; it is dropped into all three nav layouts with a `position` input to handle the bottom-nav's upward-opening requirement. Language detection is a one-liner change to the existing `APP_INITIALIZER` in `app.config.ts`. The existing `th_preferred_lang` localStorage key and `translate.use()` pattern are reused throughout.

**Tech Stack:** Angular 19 standalone components, `@ngx-translate/core`, Angular Signals, Vitest + Angular Testing Library (`@angular/core/testing`).

---

## File Map

| Action | Path | Responsibility |
|---|---|---|
| Modify | `frontend/src/app/app.config.ts` | Browser-locale detection in `APP_INITIALIZER` |
| **Create** | `frontend/src/app/shared/components/language-selector.component.ts` | Flag+code pill, dropdown, language switching |
| **Create** | `frontend/src/app/shared/components/language-selector.component.spec.ts` | Unit tests for the selector |
| Modify | `frontend/src/app/shared/components/public-layout.component.ts` | Add selector to public nav |
| Modify | `frontend/src/app/shared/components/public-layout.component.spec.ts` | Test selector presence |
| Modify | `frontend/src/app/shared/components/side-nav.component.ts` | Add selector to authenticated side-nav |
| Modify | `frontend/src/app/shared/components/bottom-nav.component.ts` | Add selector to mobile bottom-nav |
| Modify | `frontend/src/app/features/help/help.component.ts` | Doc update: mention language selector |
| Modify | `CHANGELOG.md` | Add `2.8.0` entry |
| Modify | `frontend/src/environments/environment.ts` | Bump version to `2.8.0` |
| Modify | `frontend/src/environments/environment.prod.ts` | Bump version to `2.8.0` |

---

## Task 1: Browser-locale detection in `APP_INITIALIZER`

**Files:**
- Modify: `frontend/src/app/app.config.ts`

- [ ] **Step 1: Update the language initializer**

Open `frontend/src/app/app.config.ts`. The current initializer reads:

```ts
{
  provide: APP_INITIALIZER,
  useFactory: (translate: TranslateService) => () => {
    const lang = localStorage.getItem('th_preferred_lang') ?? 'en';
    return firstValueFrom(translate.use(lang));
  },
  deps: [TranslateService],
  multi: true,
},
```

Replace only the factory body so it reads:

```ts
{
  provide: APP_INITIALIZER,
  useFactory: (translate: TranslateService) => () => {
    const SUPPORTED = ['en', 'hu', 'de', 'fr', 'es'];
    const stored = localStorage.getItem('th_preferred_lang');
    const detected = navigator.language?.slice(0, 2).toLowerCase() ?? 'en';
    const lang = stored ?? (SUPPORTED.includes(detected) ? detected : 'en');
    return firstValueFrom(translate.use(lang));
  },
  deps: [TranslateService],
  multi: true,
},
```

The logic: stored preference wins; if none, try the browser locale; if the locale isn't in the supported set, fall back to `'en'`. Nothing is written to localStorage here — that only happens when the user explicitly picks a language.

- [ ] **Step 2: Run the tests**

```bash
cd frontend && npx ng test --watch=false --testPathPattern="app.config"
```

There are no existing `app.config` tests (the initializer logic was inline), so this just verifies the build compiles.

Expected: 0 failures, build succeeds.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/app/app.config.ts
git commit -m "feat: detect browser locale for default language on first visit"
```

---

## Task 2: Create `LanguageSelectorComponent`

**Files:**
- Create: `frontend/src/app/shared/components/language-selector.component.ts`
- Create: `frontend/src/app/shared/components/language-selector.component.spec.ts`

- [ ] **Step 1: Write the failing tests first**

Create `frontend/src/app/shared/components/language-selector.component.spec.ts`:

```ts
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { vi } from 'vitest';
import { Subject } from 'rxjs';
import { provideTranslateService, TranslateService } from '@ngx-translate/core';
import { LanguageSelectorComponent } from './language-selector.component';
import { UserService } from '../../core/services/user.service';

describe('LanguageSelectorComponent', () => {
  let fixture: ComponentFixture<LanguageSelectorComponent>;
  let component: LanguageSelectorComponent;
  let translateMock: { use: ReturnType<typeof vi.fn>; currentLang: string; onLangChange: Subject<{ lang: string }> };
  let userServiceMock: { profile: ReturnType<typeof signal<{ preferredLanguage: string } | null>>; updateProfile: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    const langChangeSubject = new Subject<{ lang: string }>();
    translateMock = {
      use: vi.fn().mockReturnValue(new Subject()),
      currentLang: 'en',
      onLangChange: langChangeSubject,
    };
    userServiceMock = {
      profile: signal(null),
      updateProfile: vi.fn().mockReturnValue(new Subject()),
    };

    await TestBed.configureTestingModule({
      imports: [LanguageSelectorComponent],
      providers: [
        provideTranslateService({ defaultLanguage: 'en' }),
        { provide: TranslateService, useValue: translateMock },
        { provide: UserService, useValue: userServiceMock },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(LanguageSelectorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('renders a button showing the flag and code for the current language', () => {
    const button: HTMLButtonElement = fixture.nativeElement.querySelector('button');
    expect(button?.textContent).toContain('🇬🇧');
    expect(button?.textContent).toContain('EN');
  });

  it('dropdown is hidden by default', () => {
    const dropdown = fixture.nativeElement.querySelector('[data-testid="lang-dropdown"]');
    expect(dropdown).toBeNull();
  });

  it('dropdown opens when button is clicked', () => {
    fixture.nativeElement.querySelector('button').click();
    fixture.detectChanges();
    const dropdown = fixture.nativeElement.querySelector('[data-testid="lang-dropdown"]');
    expect(dropdown).toBeTruthy();
  });

  it('selectLanguage sets th_preferred_lang in localStorage', () => {
    component.selectLanguage('hu');
    expect(localStorage.getItem('th_preferred_lang')).toBe('hu');
    localStorage.removeItem('th_preferred_lang');
  });

  it('selectLanguage calls translate.use with the chosen code', () => {
    component.selectLanguage('de');
    expect(translateMock.use).toHaveBeenCalledWith('de');
  });

  it('selectLanguage does NOT call updateProfile when user is not logged in', () => {
    userServiceMock.profile.set(null);
    component.selectLanguage('fr');
    expect(userServiceMock.updateProfile).not.toHaveBeenCalled();
  });

  it('selectLanguage calls updateProfile when user is logged in', () => {
    userServiceMock.profile.set({ preferredLanguage: 'en' } as any);
    component.selectLanguage('fr');
    expect(userServiceMock.updateProfile).toHaveBeenCalledWith({ preferredLanguage: 'fr' });
  });

  it('currentLang signal updates when onLangChange fires', () => {
    translateMock.onLangChange.next({ lang: 'hu' });
    fixture.detectChanges();
    const button: HTMLButtonElement = fixture.nativeElement.querySelector('button');
    expect(button?.textContent).toContain('HU');
  });
});
```

- [ ] **Step 2: Run — verify tests fail**

```bash
cd frontend && npx ng test --watch=false --testPathPattern="language-selector"
```

Expected: FAIL — `Cannot find module './language-selector.component'`

- [ ] **Step 3: Implement `LanguageSelectorComponent`**

Create `frontend/src/app/shared/components/language-selector.component.ts`:

```ts
import { Component, HostListener, Input, OnInit, computed, inject, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { UserService } from '../../core/services/user.service';

@Component({
  selector: 'app-language-selector',
  standalone: true,
  template: `
    <div class="relative" (click)="$event.stopPropagation()">
      <button
        (click)="open.set(!open())"
        class="flex items-center gap-1 border border-brand-border rounded-lg px-2 py-1 bg-white hover:border-brand-orange transition-colors text-xs font-semibold text-brand-text">
        <span>{{ currentEntry().flag }}</span>
        <span>{{ currentEntry().code.toUpperCase() }}</span>
        <span class="text-[9px] text-brand-muted">▼</span>
      </button>

      @if (open()) {
        <div
          data-testid="lang-dropdown"
          [class]="dropdownClass()">
          @for (lang of languages; track lang.code) {
            <div
              (click)="selectLanguage(lang.code)"
              class="flex items-center justify-between px-3 py-2 cursor-pointer hover:bg-brand-cream text-sm text-brand-text border-b border-brand-border last:border-0">
              <span>{{ lang.flag }} {{ lang.name }}</span>
              @if (lang.code === currentLang()) {
                <span class="text-brand-orange text-xs font-bold">✓</span>
              }
            </div>
          }
        </div>
      }
    </div>
  `,
})
export class LanguageSelectorComponent implements OnInit {
  @Input() position: 'down' | 'up' = 'down';

  private translate = inject(TranslateService);
  private userService = inject(UserService);

  open = signal(false);
  currentLang = signal(this.translate.currentLang ?? 'en');

  languages = [
    { code: 'en', flag: '🇬🇧', name: 'English' },
    { code: 'hu', flag: '🇭🇺', name: 'Magyar' },
    { code: 'de', flag: '🇩🇪', name: 'Deutsch' },
    { code: 'fr', flag: '🇫🇷', name: 'Français' },
    { code: 'es', flag: '🇪🇸', name: 'Español' },
  ];

  currentEntry = computed(() =>
    this.languages.find(l => l.code === this.currentLang()) ?? this.languages[0]
  );

  dropdownClass = computed(() =>
    `absolute z-50 bg-white border border-brand-border rounded-xl shadow-lg w-44 overflow-hidden ` +
    (this.position === 'up' ? 'bottom-full mb-1 right-0' : 'top-full mt-1 right-0')
  );

  ngOnInit() {
    this.translate.onLangChange.subscribe(({ lang }) => this.currentLang.set(lang));
  }

  selectLanguage(code: string) {
    localStorage.setItem('th_preferred_lang', code);
    this.translate.use(code);
    this.currentLang.set(code);
    if (this.userService.profile()) {
      this.userService.updateProfile({ preferredLanguage: code }).subscribe();
    }
    this.open.set(false);
  }

  @HostListener('document:click')
  onDocumentClick() {
    this.open.set(false);
  }
}
```

- [ ] **Step 4: Run tests — verify they pass**

```bash
cd frontend && npx ng test --watch=false --testPathPattern="language-selector"
```

Expected: All tests PASS.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/app/shared/components/language-selector.component.ts \
        frontend/src/app/shared/components/language-selector.component.spec.ts
git commit -m "feat: add LanguageSelectorComponent with flag+code pill and dropdown"
```

---

## Task 3: Add selector to `PublicLayoutComponent`

**Files:**
- Modify: `frontend/src/app/shared/components/public-layout.component.ts`
- Modify: `frontend/src/app/shared/components/public-layout.component.spec.ts`

- [ ] **Step 1: Add a failing test to the existing spec**

Open `frontend/src/app/shared/components/public-layout.component.spec.ts`. Add this test inside the existing `describe` block, after the last `it(...)`:

```ts
it('renders the language selector in the header nav', () => {
  const selector = fixture.nativeElement.querySelector('app-language-selector');
  expect(selector).toBeTruthy();
});
```

Also add `'SHARED.LANGUAGE_SELECTOR': 'Language'` inside the existing `translate.setTranslation('en', { ... })` call so translations don't fail if the component uses any translate keys.

- [ ] **Step 2: Run — verify the new test fails**

```bash
cd frontend && npx ng test --watch=false --testPathPattern="public-layout"
```

Expected: the new test FAILS — `app-language-selector` not found.

- [ ] **Step 3: Update `PublicLayoutComponent`**

Open `frontend/src/app/shared/components/public-layout.component.ts`.

Add to the import list at the top:
```ts
import { LanguageSelectorComponent } from './language-selector.component';
```

Add `LanguageSelectorComponent` to the `imports` array:
```ts
imports: [RouterLink, RouterLinkActive, TranslatePipe, LanguageSelectorComponent],
```

In the template, find the About link and the Log In link:
```html
            <a
              routerLink="/about"
              ...>
              {{ 'LEGAL.PUBLIC_NAV_ABOUT' | translate }}
            </a>
            <a
              routerLink="/login"
              ...>
```

Insert `<app-language-selector />` between them:
```html
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
```

- [ ] **Step 4: Run tests — verify all pass**

```bash
cd frontend && npx ng test --watch=false --testPathPattern="public-layout"
```

Expected: All tests PASS (including the new one).

- [ ] **Step 5: Commit**

```bash
git add frontend/src/app/shared/components/public-layout.component.ts \
        frontend/src/app/shared/components/public-layout.component.spec.ts
git commit -m "feat: add language selector to public nav"
```

---

## Task 4: Add selector to `SideNavComponent`

**Files:**
- Modify: `frontend/src/app/shared/components/side-nav.component.ts`

- [ ] **Step 1: Update `SideNavComponent`**

Open `frontend/src/app/shared/components/side-nav.component.ts`.

Add to imports at the top:
```ts
import { LanguageSelectorComponent } from './language-selector.component';
```

Add `LanguageSelectorComponent` to the component `imports` array:
```ts
imports: [RouterLink, RouterLinkActive, TranslateModule, LanguageSelectorComponent],
```

In the template, find the closing `</div>` of the `flex flex-col py-2 px-1 lg:px-2 flex-1` div (which wraps the `@for` tabs). Insert a bottom-pinned language selector **before** that closing tag:

```html
      <div class="flex flex-col py-2 px-1 lg:px-2 flex-1">
        @for (tab of tabs; track tab.path) {
          <!-- existing tab links unchanged -->
        }
        <div class="mt-auto px-1 lg:px-2 pb-2 flex justify-center lg:justify-start">
          <app-language-selector />
        </div>
      </div>
```

- [ ] **Step 2: Run the tests**

```bash
cd frontend && npx ng test --watch=false
```

Expected: All tests PASS. (No existing side-nav spec to update — the change is additive.)

- [ ] **Step 3: Commit**

```bash
git add frontend/src/app/shared/components/side-nav.component.ts
git commit -m "feat: add language selector to authenticated side-nav"
```

---

## Task 5: Add selector to `BottomNavComponent`

**Files:**
- Modify: `frontend/src/app/shared/components/bottom-nav.component.ts`

- [ ] **Step 1: Update `BottomNavComponent`**

Open `frontend/src/app/shared/components/bottom-nav.component.ts`.

Add to imports at the top:
```ts
import { LanguageSelectorComponent } from './language-selector.component';
```

Add `LanguageSelectorComponent` to the component `imports` array:
```ts
imports: [RouterLink, RouterLinkActive, TranslateModule, LanguageSelectorComponent],
```

In the template, the `<nav>` contains a single `@for` loop. Add a fifth slot **after** the `@for` loop that uses `position="up"` so the dropdown opens upward (above the fixed bottom bar):

```html
    <nav class="md:hidden fixed bottom-0 left-0 right-0 bg-white border-t border-brand-border flex justify-around items-center h-16 z-50">
      @for (tab of tabs; track tab.path) {
        <a [routerLink]="tab.path" routerLinkActive="!text-brand-orange" [routerLinkActiveOptions]="{ exact: tab.exact }"
           [attr.data-umami-event]="'nav_' + tab.path.slice(1)"
           class="flex flex-col items-center gap-0.5 text-brand-muted text-xs font-medium py-2 px-3">
          <span class="text-xl">{{ tab.icon }}</span>
          <span>{{ tab.labelKey | translate }}</span>
        </a>
      }
      <div class="flex flex-col items-center justify-center py-2 px-3">
        <app-language-selector position="up" />
      </div>
    </nav>
```

- [ ] **Step 2: Run the tests**

```bash
cd frontend && npx ng test --watch=false
```

Expected: All tests PASS.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/app/shared/components/bottom-nav.component.ts
git commit -m "feat: add language selector to mobile bottom-nav (opens upward)"
```

---

## Task 6: Documentation and version bump

**Files:**
- Modify: `frontend/src/app/features/help/help.component.ts`
- Modify: `CHANGELOG.md`
- Modify: `frontend/src/environments/environment.ts`
- Modify: `frontend/src/environments/environment.prod.ts`

- [ ] **Step 1: Update `help.component.ts`**

In `frontend/src/app/features/help/help.component.ts`, find the section that describes the Profile / Settings screen (search for `'language'` or `'HELP.SECTION'` keys). Add or update the language preference step to mention that the language selector is also available in the navigation bar. A one-sentence addition is enough — e.g. adding to whichever steps array entry covers language:

```ts
// Find the existing step about language in the steps array and update its description:
{
  titleKey: 'HELP.STEP_LANGUAGE_TITLE',
  descKey: 'HELP.STEP_LANGUAGE_DESC',  // update the translation value if needed, but no ts change required if the key already exists
}
```

If the help component doesn't have a dedicated language step, add a note in the intro text or to the relevant profile section. The exact placement depends on the current step list — read it and pick the nearest context.

- [ ] **Step 2: Update `CHANGELOG.md`**

Add a new section at the top (above `## [2.7.0]`):

```markdown
## [2.8.0] - 2026-06-06

### Added
- The app now detects your browser language on first visit and displays the interface in the matching language automatically (English, Hungarian, German, French, or Spanish). If your browser language is not supported, English is used.
- A language selector pill (🇬🇧 EN) has been added to the navigation bar. Click it to switch language at any time. Your choice is remembered across sessions.
```

- [ ] **Step 3: Bump version in environment files**

In `frontend/src/environments/environment.ts`, change:
```ts
version: '2.7.0',
```
to:
```ts
version: '2.8.0',
```

In `frontend/src/environments/environment.prod.ts`, make the same change.

- [ ] **Step 4: Run full test suite**

```bash
cd frontend && npx ng test --watch=false
```

Expected: All tests PASS.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/app/features/help/help.component.ts \
        CHANGELOG.md \
        frontend/src/environments/environment.ts \
        frontend/src/environments/environment.prod.ts
git commit -m "chore: bump frontend version to 2.8.0, update docs and changelog"
```

---

## Verification Checklist

After all tasks are complete, verify end-to-end:

1. **Start the dev server:** `cd frontend && npm start`

2. **First-visit detection:**
   - Open DevTools → Application → Local Storage → delete `th_preferred_lang`
   - In Chrome: launch with `--lang=hu` flag or override `navigator.language` via DevTools Console: `Object.defineProperty(navigator, 'language', { value: 'hu-HU', configurable: true })`; then hard-refresh
   - Expected: page renders in Hungarian immediately (no explicit selection)

3. **Override via public nav:**
   - Click the `🇭🇺 HU` pill in the top-right of the public nav
   - Dropdown opens with 5 languages, `🇭🇺 Magyar` has a `✓`
   - Click `🇬🇧 English` → page switches to English
   - Refresh → stays English (`th_preferred_lang=en` in localStorage)

4. **Unsupported locale fallback:**
   - Set `navigator.language` to `'ja-JP'`, delete `th_preferred_lang`, hard-refresh
   - Expected: page renders in English

5. **Authenticated side-nav:**
   - Log in as `testuser@demo.com / Password1!`
   - Language pill appears at the bottom of the side-nav (desktop) and as a 5th slot in the bottom-nav (mobile)
   - Switching language from either location updates the UI immediately

6. **Profile preference persists:**
   - While logged in, switch language to German via the side-nav pill
   - Log out → log back in → language is still German (API stored it)

7. **Mobile bottom-nav dropdown opens upward:**
   - Resize browser to < 768 px
   - Tap the language pill in the bottom nav
   - Dropdown opens above the nav bar (not below it into oblivion)
