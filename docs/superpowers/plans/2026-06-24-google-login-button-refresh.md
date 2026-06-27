# Google Login Button Refresh Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the full-width text-based "Continue with Google" button in the login and signup forms with a modern centered square button containing only the official Google "G" logo SVG.

**Architecture:** Update the HTML templates in LoginComponent and SignupComponent to center-align the icon-only anchor button using Tailwind classes. Remove obsolete translation strings from the five locale files and update unit tests using TDD to search for the button via its destination URL/SVG element.

**Tech Stack:** Angular 21, Vitest, Tailwind CSS, ngx-translate

---

### Task 1: Update localization files

**Files:**
- Modify: [en.json](file:///Volumes/PersonalProtected/GIT/TinyHeroes/frontend/public/assets/i18n/en.json)
- Modify: [hu.json](file:///Volumes/PersonalProtected/GIT/TinyHeroes/frontend/public/assets/i18n/hu.json)
- Modify: [de.json](file:///Volumes/PersonalProtected/GIT/TinyHeroes/frontend/public/assets/i18n/de.json)
- Modify: [fr.json](file:///Volumes/PersonalProtected/GIT/TinyHeroes/frontend/public/assets/i18n/fr.json)
- Modify: [es.json](file:///Volumes/PersonalProtected/GIT/TinyHeroes/frontend/public/assets/i18n/es.json)

- [ ] **Step 1: Remove key CONTINUE_GOOGLE from i18n files**
  Delete line 14 containing `"CONTINUE_GOOGLE": "...",` from the `AUTH` block of all 5 i18n files.

- [ ] **Step 2: Verify i18n files are valid JSON**
  Run: `node -e "['en','hu','de','fr','es'].forEach(l => JSON.parse(require('fs').readFileSync('frontend/public/assets/i18n/' + l + '.json')))"`
  Expected: Command completes with no output (files parse successfully).

- [ ] **Step 3: Commit i18n changes**
  ```bash
  git add frontend/public/assets/i18n/*.json
  git commit -m "chore: remove obsolete CONTINUE_GOOGLE localization keys"
  ```

---

### Task 2: Update LoginComponent test and template

**Files:**
- Modify: [login.component.spec.ts](file:///Volumes/PersonalProtected/GIT/TinyHeroes/frontend/src/app/features/auth/pages/login.component.spec.ts)
- Modify: [login.component.ts](file:///Volumes/PersonalProtected/GIT/TinyHeroes/frontend/src/app/features/auth/pages/login.component.ts)

- [ ] **Step 1: Update the spec file with the failing test**
  In [login.component.spec.ts](file:///Volumes/PersonalProtected/GIT/TinyHeroes/frontend/src/app/features/auth/pages/login.component.spec.ts):
  - Remove `CONTINUE_GOOGLE: 'Continue with Google',` from the `TRANSLATIONS` mock object.
  - Replace the existing `shows Continue with Google link` test with:
  ```typescript
  it('shows Continue with Google link', () => {
    const links = Array.from(el.querySelectorAll('a')) as HTMLAnchorElement[];
    const googleLink = links.find(
      a => a.href.includes('/auth/social/Google') || a.getAttribute('href')?.includes('/auth/social/Google')
    );
    expect(googleLink).toBeTruthy();
    expect(googleLink!.querySelector('svg')).toBeTruthy();
    expect(googleLink!.textContent).not.toContain('Continue');
  });
  ```

- [ ] **Step 2: Run tests to verify the failure**
  Run: `npx vitest run login.component.spec.ts` inside `frontend/`
  Expected: Test `shows Continue with Google link` FAILS because the current LoginComponent template has the text and lacks the SVG.

- [ ] **Step 3: Modify the LoginComponent template**
  In [login.component.ts](file:///Volumes/PersonalProtected/GIT/TinyHeroes/frontend/src/app/features/auth/pages/login.component.ts), replace the social login block:
  ```html
        <div class="flex flex-col gap-3 mb-4">
          <a [href]="apiUrl + '/auth/social/Google'" class="flex items-center gap-3 bg-white border border-brand-border rounded-xl px-4 py-2.5 font-semibold text-sm text-brand-text">
            🔵 {{ 'AUTH.CONTINUE_GOOGLE' | translate }}
          </a>
        </div>
  ```
  with:
  ```html
        <div class="flex justify-center mb-4">
          <a [href]="apiUrl + '/auth/social/Google'" 
             class="flex items-center justify-center w-12 h-12 bg-white border border-brand-border rounded-xl shadow-sm hover:shadow hover:bg-gray-50 transition-all duration-150">
            <svg class="w-6 h-6" viewBox="0 0 48 48">
              <path fill="#EA4335" d="M24 9.5c3.54 0 6.71 1.22 9.21 3.6l6.85-6.85C35.9 2.38 30.47 0 24 0 14.62 0 6.51 5.38 2.56 13.22l7.98 6.19C12.43 13.72 17.74 9.5 24 9.5z"/>
              <path fill="#4285F4" d="M46.5 24c0-1.61-.15-3.16-.42-4.69H24v8.89h12.66C36.1 31.06 32.74 34 28.53 36.83l7.67 5.94C40.67 38.64 46.5 32.06 46.5 24z"/>
              <path fill="#FBBC05" d="M10.54 28.59c-.48-1.45-.76-2.99-.76-4.59s.27-3.14.76-4.59l-7.98-6.19C.92 16.46 0 20.12 0 24c0 3.88.92 7.54 2.56 10.78l7.98-6.19z"/>
              <path fill="#34A853" d="M24 38.5c-6.26 0-11.57-4.22-13.46-9.91l-7.98 6.19C6.51 42.62 14.62 48 24 48c6.48 0 11.93-2.13 15.89-5.81l-7.56-5.86c-2.11 1.4-4.81 2.17-8.33 2.17z"/>
            </svg>
          </a>
        </div>
  ```

- [ ] **Step 4: Run tests to verify they pass**
  Run: `npx vitest run login.component.spec.ts` inside `frontend/`
  Expected: PASS

- [ ] **Step 5: Commit LoginComponent changes**
  ```bash
  git add frontend/src/app/features/auth/pages/login.component.*
  git commit -m "feat: refresh Google login button on Login page with centered G SVG"
  ```

---

### Task 3: Update SignupComponent test and template

**Files:**
- Modify: [signup.component.spec.ts](file:///Volumes/PersonalProtected/GIT/TinyHeroes/frontend/src/app/features/auth/pages/signup.component.spec.ts)
- Modify: [signup.component.ts](file:///Volumes/PersonalProtected/GIT/TinyHeroes/frontend/src/app/features/auth/pages/signup.component.ts)

- [ ] **Step 1: Update the spec file with the failing test**
  In [signup.component.spec.ts](file:///Volumes/PersonalProtected/GIT/TinyHeroes/frontend/src/app/features/auth/pages/signup.component.spec.ts):
  - Remove `CONTINUE_GOOGLE: 'Continue with Google',` from the `TRANSLATIONS` mock object.
  - Replace the existing `shows Continue with Google link` test with:
  ```typescript
  it('shows Continue with Google link', () => {
    const links = Array.from(el.querySelectorAll('a')) as HTMLAnchorElement[];
    const googleLink = links.find(
      a => a.href.includes('/auth/social/Google') || a.getAttribute('href')?.includes('/auth/social/Google')
    );
    expect(googleLink).toBeTruthy();
    expect(googleLink!.querySelector('svg')).toBeTruthy();
    expect(googleLink!.textContent).not.toContain('Continue');
  });
  ```

- [ ] **Step 2: Run tests to verify the failure**
  Run: `npx vitest run signup.component.spec.ts` inside `frontend/`
  Expected: Test `shows Continue with Google link` FAILS.

- [ ] **Step 3: Modify the SignupComponent template**
  In [signup.component.ts](file:///Volumes/PersonalProtected/GIT/TinyHeroes/frontend/src/app/features/auth/pages/signup.component.ts), replace the social login block:
  ```html
        <div class="flex flex-col gap-3 mb-4">
          <a [href]="apiUrl + '/auth/social/Google'" class="flex items-center gap-3 bg-white border border-brand-border rounded-xl px-4 py-2.5 font-semibold text-sm text-brand-text">
            🔵 {{ 'AUTH.CONTINUE_GOOGLE' | translate }}
          </a>
        </div>
  ```
  with:
  ```html
        <div class="flex justify-center mb-4">
          <a [href]="apiUrl + '/auth/social/Google'" 
             class="flex items-center justify-center w-12 h-12 bg-white border border-brand-border rounded-xl shadow-sm hover:shadow hover:bg-gray-50 transition-all duration-150">
            <svg class="w-6 h-6" viewBox="0 0 48 48">
              <path fill="#EA4335" d="M24 9.5c3.54 0 6.71 1.22 9.21 3.6l6.85-6.85C35.9 2.38 30.47 0 24 0 14.62 0 6.51 5.38 2.56 13.22l7.98 6.19C12.43 13.72 17.74 9.5 24 9.5z"/>
              <path fill="#4285F4" d="M46.5 24c0-1.61-.15-3.16-.42-4.69H24v8.89h12.66C36.1 31.06 32.74 34 28.53 36.83l7.67 5.94C40.67 38.64 46.5 32.06 46.5 24z"/>
              <path fill="#FBBC05" d="M10.54 28.59c-.48-1.45-.76-2.99-.76-4.59s.27-3.14.76-4.59l-7.98-6.19C.92 16.46 0 20.12 0 24c0 3.88.92 7.54 2.56 10.78l7.98-6.19z"/>
              <path fill="#34A853" d="M24 38.5c-6.26 0-11.57-4.22-13.46-9.91l-7.98 6.19C6.51 42.62 14.62 48 24 48c6.48 0 11.93-2.13 15.89-5.81l-7.56-5.86c-2.11 1.4-4.81 2.17-8.33 2.17z"/>
            </svg>
          </a>
        </div>
  ```

- [ ] **Step 4: Run tests to verify they pass**
  Run: `npx vitest run signup.component.spec.ts` inside `frontend/`
  Expected: PASS

- [ ] **Step 5: Commit SignupComponent changes**
  ```bash
  git add frontend/src/app/features/auth/pages/signup.component.*
  git commit -m "feat: refresh Google login button on Signup page with centered G SVG"
  ```
