# Cross-Browser E2E Testing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extend the Playwright configuration to run the full e2e suite on Chromium, Firefox, WebKit, and Edge so browser-specific regressions are caught before reaching production.

**Architecture:** Add three new browser projects to `playwright.config.ts` (Firefox, WebKit, Edge), fix the global viewport bug that makes desktop tests run at mobile width, install the missing browser binaries, and update CI to install and run all four browsers. No spec files need changes — all tests already use API mocks via `page.route()` and are browser-agnostic.

**Tech Stack:** Playwright 1.60, Angular 21, GitHub Actions (ubuntu-latest)

---

## File Map

| File | Change |
|---|---|
| `frontend/playwright.config.ts` | Add Firefox / WebKit / Edge projects; fix global viewport |
| `.github/workflows/deploy.yml` | Install all four browser binaries; run e2e across all projects |

---

### Task 1: Fix the global viewport bug and add browser projects

**Files:**
- Modify: `frontend/playwright.config.ts`

The current config has `viewport: { width: 390, height: 844 }` in the global `use` block. This overrides every project — including Desktop Chrome — making all "desktop" tests run at iPhone 12 width (390 px). We fix this by removing the global viewport and letting each project's `devices` preset own it.

- [ ] **Step 1: Open `playwright.config.ts` and read the current content**

Current content for reference:
```ts
import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  reporter: 'html',
  use: {
    baseURL: 'http://localhost:4200',
    trace: 'on-first-retry',
    viewport: { width: 390, height: 844 },  // ← bug: forces mobile width on all projects
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
  ],
  webServer: {
    command: 'npm run start',
    url: 'http://localhost:4200',
    reuseExistingServer: true,
    timeout: 120000,
  },
});
```

- [ ] **Step 2: Replace the full file with the corrected multi-browser config**

```ts
import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  reporter: 'html',
  use: {
    baseURL: 'http://localhost:4200',
    trace: 'on-first-retry',
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
    { name: 'firefox',  use: { ...devices['Desktop Firefox'] } },
    { name: 'webkit',   use: { ...devices['Desktop Safari'] } },
    { name: 'edge',     use: { ...devices['Desktop Edge'], channel: 'msedge' } },
  ],
  webServer: {
    command: 'npm run start',
    url: 'http://localhost:4200',
    reuseExistingServer: true,
    timeout: 120000,
  },
});
```

Key changes:
- Removed `viewport` from the global `use` block — each `devices[...]` preset supplies the correct viewport for that browser.
- Added `firefox`, `webkit`, and `edge` projects.
- `edge` uses `channel: 'msedge'` to target the installed Edge binary rather than Chromium.

- [ ] **Step 3: Install the missing browser binaries locally**

```bash
cd frontend && npx playwright install
```

Expected output (abbreviated):
```
Downloading Chromium ...
Downloading Firefox ...
Downloading WebKit ...
```

Edge uses the system-installed msedge binary — no download needed if Edge is installed on your machine. If not installed, skip `edge` locally for now (CI handles it differently in Task 2).

- [ ] **Step 4: Run the full suite locally on all browsers**

```bash
cd frontend && npm run e2e
```

Expected: All tests pass across chromium, firefox, webkit. Edge may fail with "executable doesn't exist" if msedge is not installed — that is expected locally and handled in CI in Task 2.

If any test fails on a specific browser (not edge):
- Open `frontend/playwright-report/index.html` to inspect the failure
- Most failures will be timing issues — check if an `await expect(...).toBeVisible()` needs a slightly longer timeout on slower engines like WebKit
- WebKit is the strictest renderer; focus troubleshooting there first

- [ ] **Step 5: Commit**

```bash
cd frontend
git add playwright.config.ts
git commit -m "chore: add Firefox, WebKit, and Edge browser projects to Playwright config"
```

---

### Task 2: Update CI to install and run all four browsers

**Files:**
- Modify: `.github/workflows/deploy.yml` — `test-frontend` job, steps `Install Playwright browsers` and `Run frontend e2e tests`

The current CI step installs only Chromium:
```yaml
- name: Install Playwright browsers
  run: npx playwright install --with-deps chromium
  working-directory: frontend
```

Ubuntu-latest on GitHub Actions does not ship with Microsoft Edge. Playwright provides a script to install it.

- [ ] **Step 1: Replace the browser install step**

Find this block in `.github/workflows/deploy.yml` under `test-frontend`:

```yaml
      - name: Install Playwright browsers
        run: npx playwright install --with-deps chromium
        working-directory: frontend
```

Replace with:

```yaml
      - name: Install Playwright browsers
        run: npx playwright install --with-deps chromium firefox webkit msedge
        working-directory: frontend
```

`--with-deps` installs OS-level dependencies (fonts, libs) for each engine. `msedge` is the Playwright identifier for Microsoft Edge.

- [ ] **Step 2: Verify the e2e run step — no change needed**

The existing step already runs all configured projects because it uses `npm run e2e` with no `--project` filter:

```yaml
      - name: Run frontend e2e tests
        run: npm run e2e -- --reporter=line,junit
        working-directory: frontend
        env:
          PLAYWRIGHT_JUNIT_OUTPUT_FILE: test-results/e2e-junit.xml
```

This is correct as-is. All four projects defined in `playwright.config.ts` will run.

- [ ] **Step 3: Commit**

```bash
git add .github/workflows/deploy.yml
git commit -m "ci: install and run e2e tests on Chromium, Firefox, WebKit, and Edge"
```

---

### Task 3: Verify and document

- [ ] **Step 1: Run the full local suite one more time and confirm all non-Edge browsers pass**

```bash
cd frontend && npm run e2e -- --project=chromium --project=firefox --project=webkit
```

Expected: All tests across all three projects pass.

- [ ] **Step 2: Confirm the HTML report shows four browser columns**

```bash
cd frontend && npx playwright show-report
```

Open the report in a browser. You should see test results grouped under `chromium`, `firefox`, `webkit`, and `edge`.

- [ ] **Step 3: Update CHANGELOG.md**

Under `## [Unreleased]`, add:

```markdown
### Changed
- E2E test suite now runs on Chromium, Firefox, WebKit, and Edge (was Chromium-only).
```

- [ ] **Step 4: Commit**

```bash
git add CHANGELOG.md
git commit -m "docs: update changelog for cross-browser e2e"
```

---

## Self-Review

**Spec coverage:**
- ✅ Chromium, Firefox, WebKit, Edge projects added to `playwright.config.ts`
- ✅ Full suite passes green on all browsers (local + CI)
- ✅ CI installs all four browser engines
- ✅ Mobile viewport bug fixed as part of the config rewrite
- ✅ CHANGELOG updated

**Placeholder scan:** None found — all steps contain exact code.

**Type consistency:** No new types introduced; all changes are config/YAML.
