# Language Selector Fix for Authenticated Users

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix the language selector so that when an authenticated user picks a language, it stays selected and is not silently overridden by a subsequent `loadProfile()` HTTP response.

**Architecture:** Add a private `_langSetInSession` boolean flag to `UserService`. `loadProfile()` only applies the server's `preferredLanguage` value when the flag is `false` (i.e. on the very first load with no in-session choice). `selectLanguage()` (new shared method) sets the flag, persists to localStorage, calls `translate.use()`, and — when logged in — calls `updateProfile()`. Both `LanguageSelectorComponent` and `ProfileComponent` delegate to this single method instead of duplicating the logic.

**Tech Stack:** Angular 18+, `@ngx-translate/core`, Vitest unit tests (`*.spec.ts`).

---

## Root Cause

`ShellComponent.ngOnInit()` calls `userService.loadProfile()`, which fires an HTTP GET. If the response arrives *after* the user has already picked a language in the same session, the `translate.use(p.preferredLanguage)` call inside `loadProfile()` overwrites the user's in-session selection. `ProfileComponent` has its own `ngOnInit → loadProfile()` call, making the problem reproducible on every settings navigation.

## File Structure

**Modified files:**
- `frontend/src/app/core/services/user.service.ts` — add `_langSetInSession` flag; add `setLanguage(code)` helper; guard `loadProfile()` language apply
- `frontend/src/app/shared/components/language-selector.component.ts` — delegate `selectLanguage()` to `userService.setLanguage()`
- `frontend/src/app/features/settings/pages/profile.component.ts` — delegate `selectLanguage()` to `userService.setLanguage()`

**New test files:**
- `frontend/src/app/core/services/user.service.spec.ts` — unit tests for `loadProfile` language guard and `setLanguage`
- *(existing)* `frontend/src/app/shared/components/language-selector.component.spec.ts` — add test for delegation

---

### Task 1: Add `_langSetInSession` flag and `setLanguage()` to UserService

**Files:**
- Modify: `frontend/src/app/core/services/user.service.ts`
- Create: `frontend/src/app/core/services/user.service.spec.ts`

- [ ] **Step 1: Write failing tests**

Create `frontend/src/app/core/services/user.service.spec.ts`:

```typescript
import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { signal } from '@angular/core';
import { Subject } from 'rxjs';
import { vi } from 'vitest';
import { provideTranslateService, TranslateService } from '@ngx-translate/core';
import { UserService } from './user.service';
import { ThemeService } from './theme.service';

describe('UserService', () => {
  let service: UserService;
  let httpMock: HttpTestingController;
  let translateUseSpy: ReturnType<typeof vi.spyOn>;
  let themeApplySpy: ReturnType<typeof vi.spyOn>;

  const mockProfile = {
    userId: '1',
    displayName: 'Demo',
    email: 'demo@test.com',
    preferredLanguage: 'de',
    preferredTheme: 'default',
    pushNotificationsEnabled: false,
    weeklyEmailEnabled: false,
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        UserService,
        provideHttpClient(),
        provideHttpClientTesting(),
        provideTranslateService({ defaultLanguage: 'en' }),
        {
          provide: ThemeService,
          useValue: { apply: vi.fn(), current: signal('default') },
        },
      ],
    });
    service = TestBed.inject(UserService);
    httpMock = TestBed.inject(HttpTestingController);
    const translate = TestBed.inject(TranslateService);
    translateUseSpy = vi.spyOn(translate, 'use').mockReturnValue(new Subject() as any);
    themeApplySpy = vi.spyOn(TestBed.inject(ThemeService), 'apply');
  });

  afterEach(() => {
    httpMock.verify();
    vi.restoreAllMocks();
  });

  describe('loadProfile', () => {
    it('applies preferredLanguage from server on first load', () => {
      service.loadProfile();
      httpMock.expectOne('/api/users/me').flush(mockProfile);
      expect(translateUseSpy).toHaveBeenCalledWith('de');
    });

    it('does NOT apply server preferredLanguage if user already set language in session', () => {
      service.setLanguage('fr');
      translateUseSpy.mockClear();
      service.loadProfile();
      httpMock.expectOne('/api/users/me').flush(mockProfile);
      // translate.use should NOT have been called again with 'de'
      expect(translateUseSpy).not.toHaveBeenCalledWith('de');
    });

    it('still updates the profile signal on loadProfile even when lang is guarded', () => {
      service.setLanguage('fr');
      service.loadProfile();
      httpMock.expectOne('/api/users/me').flush(mockProfile);
      expect(service.profile()?.displayName).toBe('Demo');
    });

    it('applies theme from server regardless of lang guard', () => {
      service.setLanguage('fr');
      service.loadProfile();
      httpMock.expectOne('/api/users/me').flush(mockProfile);
      expect(themeApplySpy).toHaveBeenCalledWith('default');
    });
  });

  describe('setLanguage', () => {
    it('calls translate.use with the given code', () => {
      service.setLanguage('hu');
      expect(translateUseSpy).toHaveBeenCalledWith('hu');
    });

    it('persists the code to localStorage', () => {
      service.setLanguage('es');
      expect(localStorage.getItem('th_preferred_lang')).toBe('es');
    });

    it('sets the session flag so subsequent loadProfile does not override', () => {
      service.setLanguage('fr');
      translateUseSpy.mockClear();
      service.loadProfile();
      httpMock.expectOne('/api/users/me').flush({ ...mockProfile, preferredLanguage: 'de' });
      expect(translateUseSpy).not.toHaveBeenCalledWith('de');
    });
  });
});
```

- [ ] **Step 2: Run tests to confirm they fail**

```bash
cd /Volumes/PersonalProtected/GIT/TinyHeroes/frontend && npx ng test --watch=false --include="src/app/core/services/user.service.spec.ts" 2>&1 | tail -30
```

Expected: FAIL — `setLanguage` does not exist yet.

- [ ] **Step 3: Implement the fix in UserService**

Replace the entire content of `frontend/src/app/core/services/user.service.ts`:

```typescript
import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs';
import { TranslateService } from '@ngx-translate/core';
import { environment } from '../../../environments/environment';
import { UserProfile, UpdateProfileRequest } from '../models/user-profile.model';
import { ThemeService } from './theme.service';

@Injectable({ providedIn: 'root' })
export class UserService {
  private http = inject(HttpClient);
  private translate = inject(TranslateService);
  private themeService = inject(ThemeService);
  private _profile = signal<UserProfile | null>(null);
  private _langSetInSession = false;
  readonly profile = this._profile.asReadonly();

  setLanguage(code: string) {
    this._langSetInSession = true;
    localStorage.setItem('th_preferred_lang', code);
    this.translate.use(code);
  }

  loadProfile() {
    this.http.get<UserProfile>(`${environment.apiUrl}/users/me`).subscribe({
      next: (p) => {
        this._profile.set(p);
        if (p.preferredLanguage && !this._langSetInSession) {
          localStorage.setItem('th_preferred_lang', p.preferredLanguage);
          this.translate.use(p.preferredLanguage);
        }
        if (p.preferredTheme) {
          this.themeService.apply(p.preferredTheme);
        }
      },
      error: () => this._profile.set(null)
    });
  }

  updateProfile(req: UpdateProfileRequest) {
    return this.http.patch<UserProfile>(`${environment.apiUrl}/users/me`, req)
      .pipe(tap(p => this._profile.set(p)));
  }
}
```

- [ ] **Step 4: Run tests to confirm they pass**

```bash
cd /Volumes/PersonalProtected/GIT/TinyHeroes/frontend && npx ng test --watch=false --include="src/app/core/services/user.service.spec.ts" 2>&1 | tail -20
```

Expected: all tests PASS.

- [ ] **Step 5: Commit**

```bash
cd /Volumes/PersonalProtected/GIT/TinyHeroes && git add frontend/src/app/core/services/user.service.ts frontend/src/app/core/services/user.service.spec.ts && git commit -m "fix: guard loadProfile from overriding in-session language choice"
```

---

### Task 2: Update LanguageSelectorComponent to delegate to userService.setLanguage()

**Files:**
- Modify: `frontend/src/app/shared/components/language-selector.component.ts`
- Modify: `frontend/src/app/shared/components/language-selector.component.spec.ts`

- [ ] **Step 1: Add a failing test for delegation**

Open `frontend/src/app/shared/components/language-selector.component.spec.ts`. Add this test inside the existing `describe` block, after the existing tests:

```typescript
it('selectLanguage delegates to userService.setLanguage instead of calling translate.use directly', () => {
  const setLangSpy = vi.spyOn(userServiceMock as any, 'setLanguage').mockImplementation(() => {});
  (userServiceMock as any).setLanguage = setLangSpy;
  component.selectLanguage('hu');
  expect(setLangSpy).toHaveBeenCalledWith('hu');
  // translate.use should NOT have been called directly by the component
  expect(useSpy).not.toHaveBeenCalled();
});
```

Note: you must also add `setLanguage: vi.fn()` to `userServiceMock` in `beforeEach`:

```typescript
userServiceMock = {
  profile: signal(null),
  updateProfile: vi.fn().mockReturnValue(new Subject()),
  setLanguage: vi.fn(),
};
```

- [ ] **Step 2: Run test to confirm it fails**

```bash
cd /Volumes/PersonalProtected/GIT/TinyHeroes/frontend && npx ng test --watch=false --include="src/app/shared/components/language-selector.component.spec.ts" 2>&1 | tail -20
```

Expected: FAIL — `setLanguage` not called.

- [ ] **Step 3: Update LanguageSelectorComponent**

Replace `selectLanguage` in `frontend/src/app/shared/components/language-selector.component.ts`:

```typescript
selectLanguage(code: string) {
  this.userService.setLanguage(code);
  if (this.userService.profile()) {
    this.userService.updateProfile({ preferredLanguage: code }).subscribe();
  }
  this.open.set(false);
}
```

Remove the `private translate` injection from the component constructor — it is now only used for the `TranslatePipe` (the `import` stays, the injection goes). Keep the `onLangChange` subscription so the button label stays reactive.

Full updated component class:

```typescript
export class LanguageSelectorComponent {
  position = input<'down' | 'up'>('down');

  private translate = inject(TranslateService);
  private userService = inject(UserService);
  private destroyRef = inject(DestroyRef);

  protected open = signal(false);
  protected currentLang = signal(this.translate.currentLang ?? 'en');

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
    (this.position() === 'up' ? 'bottom-full mb-1 right-0' : 'top-full mt-1 right-0')
  );

  constructor() {
    this.translate.onLangChange
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(({ lang }) => this.currentLang.set(lang));
  }

  selectLanguage(code: string) {
    this.userService.setLanguage(code);
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

- [ ] **Step 4: Run all language-selector tests**

```bash
cd /Volumes/PersonalProtected/GIT/TinyHeroes/frontend && npx ng test --watch=false --include="src/app/shared/components/language-selector.component.spec.ts" 2>&1 | tail -20
```

Expected: all tests PASS.

- [ ] **Step 5: Commit**

```bash
cd /Volumes/PersonalProtected/GIT/TinyHeroes && git add frontend/src/app/shared/components/language-selector.component.ts frontend/src/app/shared/components/language-selector.component.spec.ts && git commit -m "fix: delegate language selection to userService.setLanguage in nav selector"
```

---

### Task 3: Update ProfileComponent to delegate to userService.setLanguage()

**Files:**
- Modify: `frontend/src/app/features/settings/pages/profile.component.ts`

- [ ] **Step 1: Update `selectLanguage` in ProfileComponent**

In `frontend/src/app/features/settings/pages/profile.component.ts`, replace the `selectLanguage` method:

```typescript
selectLanguage(code: string) {
  this.userService.setLanguage(code);
  this.userService.updateProfile({ preferredLanguage: code }).subscribe();
  this.showLangPicker.set(false);
}
```

Also remove the `private translateService = inject(TranslateService)` injection from `ProfileComponent` if it is now unused. Check first — if `translateService` is used elsewhere in the component, keep it.

- [ ] **Step 2: Run the full test suite**

```bash
cd /Volumes/PersonalProtected/GIT/TinyHeroes/frontend && npx ng test --watch=false 2>&1 | tail -30
```

Expected: all tests PASS with no regressions.

- [ ] **Step 3: Commit**

```bash
cd /Volumes/PersonalProtected/GIT/TinyHeroes && git add frontend/src/app/features/settings/pages/profile.component.ts && git commit -m "fix: delegate language selection to userService.setLanguage in profile settings"
```

---

### Task 4: Version bump and CHANGELOG

**Files:**
- Modify: `frontend/src/environments/environment.ts`
- Modify: `frontend/src/environments/environment.prod.ts`
- Modify: `CHANGELOG.md`

- [ ] **Step 1: Read current version**

Read `frontend/src/environments/environment.ts` and note the `version` value (currently `3.1.1`).

- [ ] **Step 2: Bump version to 3.1.2**

Update `version` in both:
- `frontend/src/environments/environment.ts` → `'3.1.2'`
- `frontend/src/environments/environment.prod.ts` → `'3.1.2'`

- [ ] **Step 3: Update CHANGELOG.md**

Add under a new `## [3.1.2] - 2026-06-09` section (above the existing `## [3.1.1]` entry):

```markdown
## [3.1.2] - 2026-06-09

### Fixed
- Language selector now correctly keeps the user's chosen language after logging in; switching language no longer reverts when the app reloads user preferences in the background.
```

- [ ] **Step 4: Commit**

```bash
cd /Volumes/PersonalProtected/GIT/TinyHeroes && git add frontend/src/environments/environment.ts frontend/src/environments/environment.prod.ts CHANGELOG.md && git commit -m "chore: bump frontend version to 3.1.2"
```
