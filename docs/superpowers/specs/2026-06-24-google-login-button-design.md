# Google Login Button Refresh — Design

**Date:** 2026-06-24
**Status:** Approved

## Problem

The current Google login button uses a text-based layout ("Continue with Google") and contains a generic blue circle emoji (`🔵`) as a placeholder icon. It takes up a large amount of vertical space, doesn't look modern, and limits styling flexibility.

## Goal

Replace the "Continue with Google" text button with a modern square button containing only the official Google "G" logo vector SVG, centered nicely, and remove the obsolete text translations.

## What changes

### 1. LoginComponent and SignupComponent Templates

In both [login.component.ts](file:///Volumes/PersonalProtected/GIT/TinyHeroes/frontend/src/app/features/auth/pages/login.component.ts) and [signup.component.ts](file:///Volumes/PersonalProtected/GIT/TinyHeroes/frontend/src/app/features/auth/pages/signup.component.ts):

* Remove the full-width text anchor:
  ```html
  <div class="flex flex-col gap-3 mb-4">
    <a [href]="apiUrl + '/auth/social/Google'" class="flex items-center gap-3 bg-white border border-brand-border rounded-xl px-4 py-2.5 font-semibold text-sm text-brand-text">
      🔵 {{ 'AUTH.CONTINUE_GOOGLE' | translate }}
    </a>
  </div>
  ```
* Replace it with a centered square icon anchor containing the official Google "G" logo SVG:
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

### 2. Localization Keys

Remove `"CONTINUE_GOOGLE"` from the `AUTH` block of the following locale files since the button is now icon-only and does not require translation:
* [en.json](file:///Volumes/PersonalProtected/GIT/TinyHeroes/frontend/public/assets/i18n/en.json)
* [hu.json](file:///Volumes/PersonalProtected/GIT/TinyHeroes/frontend/public/assets/i18n/hu.json)
* [de.json](file:///Volumes/PersonalProtected/GIT/TinyHeroes/frontend/public/assets/i18n/de.json)
* [fr.json](file:///Volumes/PersonalProtected/GIT/TinyHeroes/frontend/public/assets/i18n/fr.json)
* [es.json](file:///Volumes/PersonalProtected/GIT/TinyHeroes/frontend/public/assets/i18n/es.json)

### 3. Unit Tests

Update [login.component.spec.ts](file:///Volumes/PersonalProtected/GIT/TinyHeroes/frontend/src/app/features/auth/pages/login.component.spec.ts) and [signup.component.spec.ts](file:///Volumes/PersonalProtected/GIT/TinyHeroes/frontend/src/app/features/auth/pages/signup.component.spec.ts):
* Remove `CONTINUE_GOOGLE` from the mocked `TRANSLATIONS` structure.
* Modify the `"shows Continue with Google link"` tests to locate the link by its `href` attribute containing `/auth/social/Google`:
  ```typescript
  it('shows Continue with Google link', () => {
    const links = Array.from(el.querySelectorAll('a')) as HTMLAnchorElement[];
    const googleLink = links.find(a => a.href.includes('/auth/social/Google') || a.getAttribute('href')?.includes('/auth/social/Google'));
    expect(googleLink).toBeTruthy();
  });
  ```

## What does NOT change

- The OAuth flow and backend routes for social login authentication `/api/auth/social/Google` — untouched.
- Other parts of the login/registration forms (inputs, validation, Turnstile CAPTCHA) — untouched.

## Verification

### Automated Tests
- Run `npm run test` inside the `frontend` folder to ensure that all 195 unit tests (including the updated login/signup specs) pass.
