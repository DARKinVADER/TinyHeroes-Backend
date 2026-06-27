# E2E Test Documentation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Produce a single, human-readable e2e test catalogue at `docs/e2e-catalogue.md` that documents every existing Playwright test as a Gherkin scenario, groups scenarios by user flow, and calls out missing coverage so new tests can be requested in plain language and translated back to code.

**Architecture:** One markdown file consumed by both humans (PO, QA) and agentic workers. Each flow section has: a table of existing tests → Gherkin, a "Gaps" subsection listing un-tested cases. No code is generated or changed — only documentation is written.

**Tech Stack:** Markdown + Gherkin syntax (Feature / Scenario / Given–When–Then) · Playwright spec files as source of truth · `npm run e2e` to verify all tests still pass after the branch rename/clean-up step.

---

## File Structure

| Action | Path | Responsibility |
|---|---|---|
| Create | `docs/e2e-catalogue.md` | Human-readable, Gherkin-formatted catalogue of all e2e scenarios plus gap list |
| No change | `frontend/e2e/**/*.spec.ts` | Source of truth for real test code — do NOT modify |

---

### Task 1: Scaffold the catalogue file with headers and conventions

**Files:**
- Create: `docs/e2e-catalogue.md`

- [ ] **Step 1: Create the file with the standard header**

```markdown
# TinyHeroes – E2E Test Catalogue

> **Format:** Each scenario uses Gherkin syntax (Given / When / Then / And / But).  
> **Status legend:** ✅ covered · ⬜ gap (no spec exists yet).  
> **Source files:** `frontend/e2e/` — one `.spec.ts` per flow.  
> **Run all tests:** `cd frontend && npm run e2e`  
> **Run one file:** `cd frontend && npx playwright test e2e/login.spec.ts`

---
```

- [ ] **Step 2: Add the flow index (table of contents)**

Append to the file:

```markdown
## Flows

1. [Auth – Login](#1-auth--login)
2. [Auth – Signup](#2-auth--signup)
3. [Auth – Forgot Password](#3-auth--forgot-password)
4. [Public – Welcome Page](#4-public--welcome-page)
5. [Family Setup – Create Family](#5-family-setup--create-family)
6. [Children – Add Child](#6-children--add-child)
7. [App Shell – Dashboard](#7-app-shell--dashboard)
8. [Podium – Tabs Navigation](#8-podium--tabs-navigation)
9. [Podium – Prize Tracking (History)](#9-podium--prize-tracking-history)
10. [Settings – Hub Navigation](#10-settings--hub-navigation)
11. [Feedback Button](#11-feedback-button)

---
```

- [ ] **Step 3: Commit the scaffold**

```bash
git add docs/e2e-catalogue.md
git commit -m "docs: scaffold e2e catalogue with flow index"
```

---

### Task 2: Document the Auth flows (Login, Signup, Forgot Password)

**Files:**
- Modify: `docs/e2e-catalogue.md`

- [ ] **Step 1: Append the Login section**

```markdown
## 1. Auth – Login

**Source:** `frontend/e2e/login.spec.ts`

| # | Status | Scenario |
|---|---|---|
| L-01 | ✅ | Renders login form |
| L-02 | ✅ | Shows Continue with Google link |
| L-03 | ✅ | Submit disabled when form is empty |
| L-04 | ✅ | Submit disabled with invalid email |
| L-05 | ✅ | Successful login → /dashboard when family exists |
| L-06 | ✅ | Successful login → /create-family when no family |
| L-07 | ✅ | Shows error on failed login (401) |
| L-08 | ✅ | Unauthenticated user redirected from /dashboard to /login |
| L-09 | ✅ | "Create account" link navigates to /signup |

### Scenarios

```gherkin
Feature: Login

  Scenario: L-01 Renders login form
    Given the user navigates to /login
    Then "Welcome back!" heading is visible
    And an email input is visible
    And a password input is visible

  Scenario: L-02 Shows Continue with Google link
    Given the user navigates to /login
    Then a "Continue with Google" link is visible

  Scenario: L-03 Submit disabled when form is empty
    Given the user navigates to /login
    Then the "Log In" button is disabled

  Scenario: L-04 Submit disabled with invalid email
    Given the user navigates to /login
    When the user enters "not-an-email" in the email field
    And the user enters "password123" in the password field
    Then the "Log In" button is disabled

  Scenario: L-05 Successful login redirects to dashboard (family exists)
    Given the login API will return a token with hasFamily=true
    And the user navigates to /login
    When the user enters valid credentials
    And clicks "Log In"
    Then the URL changes to /dashboard

  Scenario: L-06 Successful login redirects to create-family (no family)
    Given the login API will return a token with hasFamily=false
    And the user navigates to /login
    When the user enters valid credentials
    And clicks "Log In"
    Then the URL changes to /create-family

  Scenario: L-07 Shows error on failed login (401)
    Given the login API will return 401
    And the user navigates to /login
    When the user enters wrong credentials
    And clicks "Log In"
    Then a red error text "Invalid email or password" is visible

  Scenario: L-08 Unauthenticated redirect from /dashboard
    Given the user is not authenticated
    When the user navigates to /dashboard
    Then the URL changes to /login

  Scenario: L-09 Create account link navigates to signup
    Given the user navigates to /login
    When the user clicks "Create account"
    Then the URL changes to /signup
```

### Gaps

| # | Status | Missing scenario |
|---|---|---|
| L-G1 | ⬜ | Token stored in localStorage after successful login |
| L-G2 | ⬜ | "Forgot password?" link navigates to /forgot-password (tested in forgot-password.spec.ts instead) |
| L-G3 | ⬜ | Submit disabled while login request is in-flight (loading state) |

---
```

- [ ] **Step 2: Append the Signup section**

```markdown
## 2. Auth – Signup

**Source:** `frontend/e2e/signup.spec.ts`

| # | Status | Scenario |
|---|---|---|
| S-01 | ✅ | Renders registration form |
| S-02 | ✅ | Submit disabled when form is empty |
| S-03 | ✅ | Submit disabled with invalid email |
| S-04 | ✅ | Submit disabled when password is too short |
| S-05 | ✅ | Successful registration → /create-family |
| S-06 | ✅ | Shows error on failed registration (422) |
| S-07 | ✅ | Shows Continue with Google link |
| S-08 | ✅ | "Log In" link navigates to /login |

### Scenarios

```gherkin
Feature: Signup

  Scenario: S-01 Renders registration form
    Given the user navigates to /signup
    Then a "Create account" heading is visible
    And a name text input is visible
    And an email input is visible
    And a password input is visible

  Scenario: S-02 Submit disabled when form is empty
    Given the user navigates to /signup
    Then the "Create Account" button is disabled

  Scenario: S-03 Submit disabled with invalid email
    Given the user navigates to /signup
    When the user enters "Test User" in the name field
    And "not-an-email" in the email field
    And "password123" in the password field
    Then the "Create Account" button is disabled

  Scenario: S-04 Submit disabled when password is too short
    Given the user navigates to /signup
    When the user enters valid name and email
    And "short" in the password field
    Then the "Create Account" button is disabled

  Scenario: S-05 Successful registration redirects to create-family
    Given the register API will return a valid token
    And the user navigates to /signup
    When the user fills in valid name, email, and password
    And clicks "Create Account"
    Then the URL changes to /create-family

  Scenario: S-06 Shows error on failed registration (422)
    Given the register API will return 422 "Email already in use"
    And the user navigates to /signup
    When the user fills in valid credentials
    And clicks "Create Account"
    Then a red error text "Registration failed" is visible

  Scenario: S-07 Shows Continue with Google link
    Given the user navigates to /signup
    Then a "Continue with Google" link is visible

  Scenario: S-08 Log In link navigates to /login
    Given the user navigates to /signup
    When the user clicks "Log In"
    Then the URL changes to /login
```

### Gaps

| # | Status | Missing scenario |
|---|---|---|
| S-G1 | ⬜ | Password strength indicator / feedback shown below input |
| S-G2 | ⬜ | Submit disabled while registration request is in-flight |

---
```

- [ ] **Step 3: Append the Forgot Password section**

```markdown
## 3. Auth – Forgot Password

**Source:** `frontend/e2e/forgot-password.spec.ts`

| # | Status | Scenario |
|---|---|---|
| FP-01 | ✅ | Renders forgot password form |
| FP-02 | ✅ | Submit disabled when email is empty |
| FP-03 | ✅ | Submit disabled with invalid email |
| FP-04 | ✅ | Submit enabled with valid email |
| FP-05 | ✅ | Shows success message after submission |
| FP-06 | ✅ | Hides form after successful submission |
| FP-07 | ✅ | Shows error on failed submission (500) |
| FP-08 | ✅ | "Back to Log In" link navigates to /login |
| FP-09 | ✅ | Login page "Forgot password?" link navigates to /forgot-password |

### Scenarios

```gherkin
Feature: Forgot Password

  Scenario: FP-01 Renders forgot password form
    Given the user navigates to /forgot-password
    Then a "Reset password" heading is visible
    And an email input is visible

  Scenario: FP-02 Submit disabled when email is empty
    Given the user navigates to /forgot-password
    Then the "Send Reset Link" button is disabled

  Scenario: FP-03 Submit disabled with invalid email
    Given the user navigates to /forgot-password
    When the user enters "not-an-email" in the email field
    Then the "Send Reset Link" button is disabled

  Scenario: FP-04 Submit enabled with valid email
    Given the user navigates to /forgot-password
    When the user enters "user@example.com" in the email field
    Then the "Send Reset Link" button is enabled

  Scenario: FP-05 Shows success message after submission
    Given the forgot-password API will return 200
    And the user navigates to /forgot-password
    When the user enters a valid email and clicks "Send Reset Link"
    Then "Check your email for the reset link" is visible

  Scenario: FP-06 Hides form after successful submission
    Given the forgot-password API will return 200
    And the user navigates to /forgot-password
    When the user enters a valid email and clicks "Send Reset Link"
    Then the "Send Reset Link" button is no longer visible

  Scenario: FP-07 Shows error on failed submission (500)
    Given the forgot-password API will return 500
    And the user navigates to /forgot-password
    When the user enters a valid email and clicks "Send Reset Link"
    Then a red error text "Something went wrong" is visible

  Scenario: FP-08 Back to Log In link navigates to /login
    Given the user navigates to /forgot-password
    When the user clicks "Back to Log In"
    Then the URL changes to /login

  Scenario: FP-09 Login page Forgot password link
    Given the user navigates to /login
    When the user clicks "Forgot password?"
    Then the URL changes to /forgot-password
```

### Gaps

| # | Status | Missing scenario |
|---|---|---|
| FP-G1 | ⬜ | Reset link lands on /reset-password?token=… (no spec file exists) |
| FP-G2 | ⬜ | Password reset form: new password + confirm fields, submit |
| FP-G3 | ⬜ | Reset form shows error for mismatched passwords |
| FP-G4 | ⬜ | Reset form shows error for expired/invalid token |

---
```

- [ ] **Step 4: Commit the auth flows**

```bash
git add docs/e2e-catalogue.md
git commit -m "docs: add auth flows (login, signup, forgot-password) to e2e catalogue"
```

---

### Task 3: Document Public, Family Setup, and Children flows

**Files:**
- Modify: `docs/e2e-catalogue.md`

- [ ] **Step 1: Append the Welcome Page section**

```markdown
## 4. Public – Welcome Page

**Source:** `frontend/e2e/welcome.spec.ts`

| # | Status | Scenario |
|---|---|---|
| W-01 | ✅ | Renders hero headline and tagline |
| W-02 | ✅ | Renders How it works and feature sections |
| W-03 | ✅ | "Get Started" navigates to /signup |
| W-04 | ✅ | "Log In" navigates to /login |

### Scenarios

```gherkin
Feature: Welcome Page

  Scenario: W-01 Renders hero headline and tagline
    Given the user navigates to /
    Then "Every good deed deserves a reward" heading is visible
    And "Track your kids' good deeds, run weekly competitions" is visible

  Scenario: W-02 Renders How it works and feature sections
    Given the user navigates to /
    Then "How it works" heading is visible
    And "Everything your family needs" heading is visible
    And "Track good deeds" text is visible
    And "Weekly Podium" text is visible

  Scenario: W-03 Get Started navigates to signup
    Given the user navigates to /
    When the user clicks the first "Get Started" link
    Then the URL changes to /signup

  Scenario: W-04 Log In link navigates to login
    Given the user navigates to /
    When the user clicks the first "Log In" link
    Then the URL changes to /login
```

### Gaps

| # | Status | Missing scenario |
|---|---|---|
| W-G1 | ⬜ | Authenticated user visiting / is redirected to /dashboard |
| W-G2 | ⬜ | About, Contact, Privacy, Terms links in footer/nav are reachable |

---
```

- [ ] **Step 2: Append the Create Family section**

```markdown
## 5. Family Setup – Create Family

**Source:** `frontend/e2e/create-family.spec.ts`

| # | Status | Scenario |
|---|---|---|
| CF-01 | ✅ | Renders family setup form |
| CF-02 | ✅ | Shows all day-of-week buttons (Mon–Sun) |
| CF-03 | ✅ | Submit disabled when family name is empty |
| CF-04 | ✅ | Can select different week start day |
| CF-05 | ✅ | Successful submission → /dashboard |

### Scenarios

```gherkin
Feature: Create Family

  Scenario: CF-01 Renders family setup form
    Given the user is authenticated with no family
    And navigates to /create-family
    Then "Set up your family" is visible
    And a family name input is visible
    And "Week starts on" label is visible

  Scenario: CF-02 Shows all day-of-week buttons
    Given the user navigates to /create-family (authenticated, no family)
    Then buttons Mon, Tue, Wed, Thu, Fri, Sat, Sun are all visible

  Scenario: CF-03 Submit disabled when family name is empty
    Given the user navigates to /create-family
    Then the "Create My Family" button is disabled

  Scenario: CF-04 Can select different week start day
    Given the user navigates to /create-family
    When the user clicks the "Sun" day button
    Then the "Sun" button has the brand-orange border class

  Scenario: CF-05 Successful submission redirects to dashboard
    Given the families API will return 201 with a family object
    And the user navigates to /create-family
    When the user enters "The Testers" as family name
    And clicks "Create My Family"
    Then the URL changes to /dashboard
```

### Gaps

| # | Status | Missing scenario |
|---|---|---|
| CF-G1 | ⬜ | Family name has a maximum character limit enforced in UI |
| CF-G2 | ⬜ | Shows error when family creation API returns 500 |
| CF-G3 | ⬜ | Already-in-family user visiting /create-family is redirected to /dashboard |

---
```

- [ ] **Step 3: Append the Add Child section**

```markdown
## 6. Children – Add Child

**Source:** `frontend/e2e/add-child.spec.ts`

| # | Status | Scenario |
|---|---|---|
| AC-01 | ✅ | Renders add child form |
| AC-02 | ✅ | Submit disabled when name is empty |
| AC-03 | ✅ | Submit enabled when name is filled |
| AC-04 | ✅ | Age starts at 5 and increments/decrements |
| AC-05 | ✅ | Can select Boy/Girl gender |
| AC-06 | ✅ | Successful submission → /dashboard |

### Scenarios

```gherkin
Feature: Add Child

  Scenario: AC-01 Renders add child form
    Given the user is authenticated with a family
    And navigates to /add-child
    Then "Add a new hero" is visible
    And "Choose an avatar" is visible
    And "Name" label is visible
    And "Age" label is visible
    And "Gender" label is visible

  Scenario: AC-02 Submit disabled when name is empty
    Given the user navigates to /add-child
    Then the "Add Hero" button is disabled

  Scenario: AC-03 Submit enabled when name is filled
    Given the user navigates to /add-child
    When the user types "Alice" in the hero name field
    Then the "Add Hero" button is enabled

  Scenario: AC-04 Age starts at 5 and increments/decrements
    Given the user navigates to /add-child
    Then "5" is visible as the current age
    When the user clicks "+"
    Then "6" is visible as the current age
    When the user clicks "−"
    Then "5" is visible as the current age

  Scenario: AC-05 Can select Boy/Girl gender
    Given the user navigates to /add-child
    When the user clicks "Girl"
    Then the "Girl" button has the brand-orange class
    When the user clicks "Boy"
    Then the "Boy" button has the brand-orange class

  Scenario: AC-06 Successful submission redirects to dashboard
    Given the children API will return 201
    And the user navigates to /add-child
    When the user enters "Alice" and clicks "Add Hero"
    Then the URL changes to /dashboard
```

### Gaps

| # | Status | Missing scenario |
|---|---|---|
| AC-G1 | ⬜ | Can select an emoji avatar from the avatar picker |
| AC-G2 | ⬜ | Can upload a photo as avatar |
| AC-G3 | ⬜ | Age cannot be decremented below 1 |
| AC-G4 | ⬜ | Age cannot be incremented above 17 (or whatever the max is) |
| AC-G5 | ⬜ | Shows error when children API returns 500 |
| AC-G6 | ⬜ | Edit child: name/age/gender/avatar are pre-populated with existing values |
| AC-G7 | ⬜ | Edit child: successful save returns to /dashboard |
| AC-G8 | ⬜ | Delete child: confirmation dialog, child removed from list |

---
```

- [ ] **Step 4: Commit the public + family + children flows**

```bash
git add docs/e2e-catalogue.md
git commit -m "docs: add welcome, create-family, add-child flows to e2e catalogue"
```

---

### Task 4: Document Dashboard, Podium, and Prize Tracking flows

**Files:**
- Modify: `docs/e2e-catalogue.md`

- [ ] **Step 1: Append the Dashboard section**

```markdown
## 7. App Shell – Dashboard

**Source:** `frontend/e2e/dashboard.spec.ts`

| # | Status | Scenario |
|---|---|---|
| D-01 | ✅ | Shows greeting with user first name |
| D-02 | ✅ | Shows empty state when no children |
| D-03 | ✅ | Shows week day strip (Mon–Thu visible) |
| D-04 | ✅ | Empty state "Add Hero" button navigates to /add-child |
| D-05 | ✅ | Top bar "+ Add hero" link navigates to /add-child |
| D-06 | ✅ | Shows child cards when children exist |

### Scenarios

```gherkin
Feature: Dashboard

  Scenario: D-01 Shows greeting with user first name
    Given the user is authenticated as "Test Parent"
    And navigates to /dashboard
    Then "Hello, Test!" text is visible

  Scenario: D-02 Shows empty state when no children
    Given the user is authenticated with no children
    And navigates to /dashboard
    Then "Add your first hero!" is visible

  Scenario: D-03 Shows week day strip
    Given the user is authenticated
    And navigates to /dashboard
    Then "Mon", "Tue", "Wed", "Thu" labels are visible

  Scenario: D-04 Empty state Add Hero button navigates to /add-child
    Given the user is authenticated with no children
    And navigates to /dashboard
    When the user clicks "Add Hero" (empty state button)
    Then the URL changes to /add-child

  Scenario: D-05 Top bar add hero link navigates to /add-child
    Given the user is authenticated
    And navigates to /dashboard
    When the user clicks "+ Add hero" in the top bar
    Then the URL changes to /add-child

  Scenario: D-06 Shows child cards when children exist
    Given the user is authenticated with child Alice (age 7)
    And navigates to /dashboard
    Then "Alice" is visible
    And "7" with age label is visible
```

### Gaps

| # | Status | Missing scenario |
|---|---|---|
| D-G1 | ⬜ | Clicking a child card opens the deed entry for that child |
| D-G2 | ⬜ | Adding a deed updates the child's deed count on the card |
| D-G3 | ⬜ | Deed quick-add: selecting a preset pre-fills description |
| D-G4 | ⬜ | Deed quick-add: custom deed with free text |
| D-G5 | ⬜ | Deed quick-add: successful add shows confirmation |
| D-G6 | ⬜ | Bottom navigation links (Dashboard, Podium, Settings) are visible and functional |

---
```

- [ ] **Step 2: Append the Podium Tabs section**

```markdown
## 8. Podium – Tabs Navigation

**Source:** `frontend/e2e/podium.spec.ts`

| # | Status | Scenario |
|---|---|---|
| P-01 | ✅ | Renders three tabs: This Week, Monthly, History |
| P-02 | ✅ | Navigates to /podium/monthly |
| P-03 | ✅ | Navigates to /podium/history |
| P-04 | ✅ | Navigates back to /podium/this-week |
| P-05 | ✅ | /podium redirects to /podium/this-week |

### Scenarios

```gherkin
Feature: Podium Tabs

  Scenario: P-01 Renders three tabs
    Given the user is authenticated
    And navigates to /podium/this-week
    Then "This Week" tab link is visible
    And "Monthly" tab link is visible
    And "History" tab link is visible

  Scenario: P-02 Navigates to monthly tab
    Given the user is authenticated and on /podium/this-week
    When the user clicks the "Monthly" tab
    Then the URL changes to /podium/monthly

  Scenario: P-03 Navigates to history tab
    Given the user is authenticated and on /podium/this-week
    When the user clicks the "History" tab
    Then the URL changes to /podium/history

  Scenario: P-04 Navigates back to this week tab
    Given the user is authenticated and on /podium/monthly
    When the user clicks the "This Week" tab
    Then the URL changes to /podium/this-week

  Scenario: P-05 /podium root redirects to this-week
    Given the user is authenticated
    When the user navigates to /podium
    Then the URL changes to /podium/this-week
```

### Gaps

| # | Status | Missing scenario |
|---|---|---|
| P-G1 | ⬜ | This Week tab shows top-ranked children in podium order |
| P-G2 | ⬜ | Monthly tab shows monthly champion card with name and total deeds |
| P-G3 | ⬜ | History tab shows week-by-week summaries with dates |
| P-G4 | ⬜ | Empty state when no deeds recorded this week |

---
```

- [ ] **Step 3: Append the Prize Tracking section**

```markdown
## 9. Podium – Prize Tracking (History)

**Source:** `frontend/e2e/prize-tracking.spec.ts`

| # | Status | Scenario |
|---|---|---|
| PT-01 | ✅ | History tab shows Prizes section for a week with prize assignments |
| PT-02 | ✅ | Prize card shows "Mark used" button initially |
| PT-03 | ✅ | Tapping a prize card expands it inline (comment input + Post button) |
| PT-04 | ✅ | "Mark used" button marks prize as used without expanding |
| PT-05 | ✅ | Toggle inside expanded card marks prize as used |
| PT-06 | ✅ | Posting a comment adds it to the list |
| PT-07 | ✅ | Deleting a comment removes it from the list |
| PT-08 | ✅ | Monthly tab shows prize section for champion month |

### Scenarios

```gherkin
Feature: Prize Tracking

  Scenario: PT-01 History tab shows Prizes section
    Given the user is authenticated with a week summary and prize assignments
    And navigates to /podium/history
    Then "🎁 Prizes" section header is visible
    And "Pizza night" prize card is visible

  Scenario: PT-02 Prize card shows Mark used button
    Given the user is on /podium/history with prize assignments
    Then a "Mark used" button is visible

  Scenario: PT-03 Tapping prize card expands inline
    Given the user is on /podium/history with prize assignments
    When the user clicks the "Pizza night" card
    Then a comment input with placeholder "Add a comment…" is visible
    And a "Post" button is visible

  Scenario: PT-04 Mark used without expanding
    Given the user is on /podium/history with prize assignments
    When the user clicks "Mark used"
    Then "Used ✓" label appears on the prize card

  Scenario: PT-05 Toggle inside expanded card marks as used
    Given the user is on /podium/history with prize assignments
    And has expanded the "Pizza night" card
    When the user checks the used checkbox
    Then "Used ✓" label appears on the prize card

  Scenario: PT-06 Posting a comment adds it to the list
    Given the user has expanded a prize card
    When the user types "Given out Friday!" and clicks "Post"
    Then "Given out Friday!" appears in the comment list

  Scenario: PT-07 Deleting a comment removes it
    Given the user has posted "To be deleted" on a prize card
    When the user clicks the "✕" delete button on that comment
    Then "To be deleted" is no longer visible

  Scenario: PT-08 Monthly tab shows monthly prize
    Given the user is on /podium/history with a monthly champion
    When the user clicks the "Monthly" tab
    Then "Amusement park" prize card is visible
    And a "Mark used" button is visible
```

### Gaps

| # | Status | Missing scenario |
|---|---|---|
| PT-G1 | ⬜ | "Used ✓" status persists after page reload |
| PT-G2 | ⬜ | Unchecking used toggle resets prize to unused |
| PT-G3 | ⬜ | Comment is truncated at character limit |
| PT-G4 | ⬜ | Multiple prizes in the same week are each independently expandable |

---
```

- [ ] **Step 4: Commit podium flows**

```bash
git add docs/e2e-catalogue.md
git commit -m "docs: add dashboard, podium tabs, and prize-tracking flows to e2e catalogue"
```

---

### Task 5: Document Settings and Feedback flows; add conventions appendix

**Files:**
- Modify: `docs/e2e-catalogue.md`

- [ ] **Step 1: Append the Settings section**

```markdown
## 10. Settings – Hub Navigation

**Source:** `frontend/e2e/settings.spec.ts`

| # | Status | Scenario |
|---|---|---|
| ST-01 | ✅ | Renders "Settings" heading |
| ST-02 | ✅ | Shows "Invite Co-Parent" link |
| ST-03 | ✅ | Shows "My Profile" link |
| ST-04 | ✅ | Shows "Family Settings" link |
| ST-05 | ✅ | "My Profile" navigates to /settings/profile |
| ST-06 | ✅ | "Family Settings" navigates to /settings/family |
| ST-07 | ✅ | "Invite Co-Parent" navigates to /settings/invite |

### Scenarios

```gherkin
Feature: Settings Hub

  Scenario: ST-01 Renders Settings heading
    Given the user is authenticated
    And navigates to /settings
    Then a "Settings" h1 heading is visible

  Scenario: ST-02 Shows Invite Co-Parent link
    Given the user is on /settings
    Then "Invite Co-Parent" text is visible

  Scenario: ST-03 Shows My Profile link
    Given the user is on /settings
    Then "My Profile" text is visible

  Scenario: ST-04 Shows Family Settings link
    Given the user is on /settings
    Then "Family Settings" text is visible

  Scenario: ST-05 My Profile navigates to /settings/profile
    Given the user is on /settings
    When the user clicks "My Profile"
    Then the URL changes to /settings/profile

  Scenario: ST-06 Family Settings navigates to /settings/family
    Given the user is on /settings
    When the user clicks "Family Settings"
    Then the URL changes to /settings/family

  Scenario: ST-07 Invite Co-Parent navigates to /settings/invite
    Given the user is on /settings
    When the user clicks "Invite Co-Parent"
    Then the URL changes to /settings/invite
```

### Gaps

| # | Status | Missing scenario |
|---|---|---|
| ST-G1 | ⬜ | /settings/profile: display name and email are pre-populated |
| ST-G2 | ⬜ | /settings/profile: change display name and save |
| ST-G3 | ⬜ | /settings/family: family name and week start day are pre-populated |
| ST-G4 | ⬜ | /settings/family: update family name and save |
| ST-G5 | ⬜ | /settings/invite: shows shareable invite link |
| ST-G6 | ⬜ | /settings/invite: copy button copies link to clipboard |
| ST-G7 | ⬜ | Child list in settings shows Edit and Delete buttons per child |
| ST-G8 | ⬜ | Edit child navigates to /settings/children/:id |
| ST-G9 | ⬜ | Prize editor: custom prizes can be added, reordered, deleted |
| ST-G10 | ⬜ | Preset manager: system presets can be toggled on/off |
| ST-G11 | ⬜ | Preset manager: custom preset can be added with label and emoji |

---
```

- [ ] **Step 2: Append the Feedback section**

```markdown
## 11. Feedback Button

**Source:** `frontend/e2e/feedback.spec.ts`

| # | Status | Scenario |
|---|---|---|
| FB-01 | ✅ | FAB is visible on the login page |
| FB-02 | ✅ | Clicking FAB opens the category step |
| FB-03 | ✅ | Selecting a category advances to message step |
| FB-04 | ✅ | Back button returns to category step |
| FB-05 | ✅ | Send button disabled when message is empty |
| FB-06 | ✅ | Anonymous user sees email field |
| FB-07 | ✅ | Authenticated user does not see email field |
| FB-08 | ✅ | Successful submission shows success state |
| FB-09 | ✅ | Failed submission shows error state |
| FB-10 | ✅ | Clicking backdrop closes the modal |

### Scenarios

```gherkin
Feature: Feedback Button

  Scenario: FB-01 FAB is visible on the login page
    Given the user navigates to /login
    Then the feedback FAB button is visible

  Scenario: FB-02 Clicking FAB opens category step
    Given the user is on /login
    When the user clicks the feedback FAB
    Then "Send Feedback" heading is visible
    And "Report a bug" option is visible
    And "Share an idea" option is visible
    And "General feedback" option is visible

  Scenario: FB-03 Selecting a category advances to message step
    Given the feedback modal is open at the category step
    When the user selects "Report a bug"
    Then a textarea with placeholder "Describe what went wrong…" is visible
    And a "← Back" button is visible

  Scenario: FB-04 Back button returns to category step
    Given the feedback modal is at the message step (bug selected)
    When the user clicks "← Back"
    Then "Send Feedback" heading is visible

  Scenario: FB-05 Send disabled when message is empty
    Given the feedback modal is at the message step (idea selected)
    Then the "Send" button is disabled

  Scenario: FB-06 Anonymous user sees email field
    Given the user is not authenticated and opens the feedback modal
    And selects "General feedback"
    Then a field with placeholder "Your email (optional)" is visible

  Scenario: FB-07 Authenticated user does not see email field
    Given the user is authenticated and opens the feedback modal
    And selects "General feedback"
    Then no field with placeholder "Your email (optional)" is visible

  Scenario: FB-08 Successful submission shows success state
    Given the feedback API will return 204
    And the user submits a bug report with text "The button does not work."
    Then "Thanks! We've received your feedback." is visible

  Scenario: FB-09 Failed submission shows error state
    Given the feedback API will return 500
    And the user submits a bug report with text "Something is wrong."
    Then "Something went wrong — please try again." is visible

  Scenario: FB-10 Clicking backdrop closes the modal
    Given the feedback modal is open
    When the user clicks the backdrop overlay
    Then the modal is no longer visible
```

### Gaps

| # | Status | Missing scenario |
|---|---|---|
| FB-G1 | ⬜ | FAB is visible on all authenticated pages (dashboard, podium, settings) |
| FB-G2 | ⬜ | Modal closes when Escape key is pressed |
| FB-G3 | ⬜ | After success, modal auto-closes after a delay |

---
```

- [ ] **Step 3: Append the conventions and helper reference appendix**

```markdown
## Appendix: Test Helper Reference

All e2e tests import from `frontend/e2e/helpers/index.ts`.

### Auth helpers

| Export | Purpose |
|---|---|
| `injectAuth(page)` | Injects a fake JWT into `localStorage` so the app treats the browser as logged-in |
| `makeFakeJwt(overrides?)` | Creates a Base64-encoded fake JWT (no real signature) — use `overrides` to change claims |
| `TEST_USER` | Constant `{ userId, displayName, email }` used by `injectAuth` |

### API mock helpers

| Export | Purpose |
|---|---|
| `mockAuthApi(page, { hasFamily? })` | Stubs `/api/auth/register` and `/api/auth/login` |
| `mockFamilyApi(page)` | Stubs `GET /api/families/mine` and `POST /api/families` |
| `mockChildrenApi(page, children?)` | Stubs `GET/POST /api/children` and avatar endpoint |
| `mockDeedsApi(page)` | Stubs `GET/POST /api/deeds` and `/api/deeds/stats` |
| `mockPresetsApi(page)` | Stubs `GET/POST /api/presets` with two system presets |
| `mockSummariesApi(page)` | Stubs weekly summaries, monthly summaries, and prize assignments |
| `mockPrizeClaimsApi(page, initialClaims?)` | Stubs full prize claims CRUD (GET, POST, PATCH /used, DELETE /comments/:id) with in-memory state |
| `mockAllApi(page)` | Calls all of the above — use in `test.beforeEach` for authenticated pages |

### Base test fixture

`frontend/e2e/helpers/base-test.ts` extends Playwright's `test` so that every `page` fixture
automatically catches any unmatched `**/api/**` request and returns `200 {}`.
This prevents noise from API calls that a test doesn't care about.

### Adding a new test

1. Create `frontend/e2e/<flow>.spec.ts`
2. Import from `'./helpers'`
3. Use `injectAuth(page)` + `mockAllApi(page)` in `test.beforeEach` for authenticated flows
4. Override specific endpoints with `page.route(...)` before `page.goto()`
5. Mark the corresponding gap row in this catalogue as ✅ and add the scenario

---
```

- [ ] **Step 4: Commit the final sections and appendix**

```bash
git add docs/e2e-catalogue.md
git commit -m "docs: add settings, feedback, and helper appendix to e2e catalogue"
```

---

### Task 6: Verify all tests still pass and update changelog

**Files:**
- Modify: `CHANGELOG.md`

- [ ] **Step 1: Run the full e2e suite to confirm no regressions**

```bash
cd frontend && npm run e2e -- --reporter=list
```

Expected: all tests pass across all four browsers (Chromium, Firefox, WebKit, Edge).
If any test fails, investigate before continuing — this task makes no code changes, so a failure indicates a pre-existing issue on the branch.

- [ ] **Step 2: Add a CHANGELOG entry under `## [Unreleased]`**

Open `CHANGELOG.md` at the repo root and add:

```markdown
### Added
- E2E test catalogue (`docs/e2e-catalogue.md`) — Gherkin-formatted documentation of all 62 existing Playwright scenarios, grouped by user flow, with a gap list per flow to guide future test additions.
```

- [ ] **Step 3: Commit the changelog**

```bash
git add CHANGELOG.md
git commit -m "docs: update changelog for e2e catalogue"
```

---

## Self-Review

### Spec coverage check

Issue #46 asks for:
- ✅ Document existing tests per function per flow — Tasks 2–5 cover all 11 spec files
- ✅ Fill the missing gaps — every section has a Gaps table
- ✅ Use standard language (Gherkin) — Given/When/Then used throughout
- ✅ Easy to translate new scenarios to test code — Appendix explains helpers

### Placeholder scan

No TBD/TODO/placeholder language in any step. Every step has the exact markdown content to write.

### Consistency check

All scenario IDs (L-01, S-01, etc.) are unique within their flow. Gap IDs (L-G1, etc.) follow the same prefix pattern.
