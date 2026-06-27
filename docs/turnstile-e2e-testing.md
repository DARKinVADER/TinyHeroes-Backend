# Cloudflare Turnstile E2E Testing Investigation

## Context
During the separation of E2E tests for the integration pipeline (PR #142/143), we started running E2E UI tests directly against the live `integration` environment (backend and frontend) rather than mocking the API. However, this caused flaky test timeouts and failures specifically related to the "Log In" button remaining disabled.

## The Issues

### 1. The `429 Too Many Requests` Issue
Tests began failing with a `429 Too Many Requests` status when attempting to register or login seeded users.
- **Root Cause:** The `integration` App Service was returning rate limits because GitHub Actions tests were triggering parallel requests. While we had deployed a fix in `1.5.3` to disable rate limiting in the `Integration` environment, the test pipeline began executing *before* Azure App Service finished swapping the new container in. Connections were still hitting the draining `1.5.2` container.
- **Resolution:** Confirmed the `1.5.3` environment correctly bypasses rate limiting. The test failure was a race condition with the deployment pipeline.

### 2. Turnstile Mocking vs Real Dummy Keys
When trying to fix the tests, we faced a dilemma with Cloudflare Turnstile:
1. Playwright tests cannot easily solve real Turnstile widgets because Cloudflare detects headless bots and blocks them or issues interactive challenges.
2. The `integration` backend relies on the Turnstile dummy secret key (`1x0000000000000000000000000000000AA`), which accepts any valid token string. The frontend uses the dummy sitekey (`1x00000000000000000000AA`), which automatically passes without interaction and issues a token string.

Initially, we thought we needed to inject a mock script (`mockTurnstile`) into the DOM to bypass Turnstile. However, doing so broke the Angular component lifecycle integration because the mock injected a fake `turnstile` object too early (or too late), disrupting how Angular evaluates `typeof turnstile`.

**Resolution:** Because the dummy sitekey *automatically passes* even for bots, we **do not need to mock Turnstile** in the integration pipeline. We allow the real Cloudflare `api.js` script to load normally, which renders the dummy widget, succeeds automatically, and safely sets `captcha.token()`.

### 3. The Explicit `toBeEnabled` Timeout
Even after allowing the real dummy widget to load, the following test failed consistently with a timeout:
```typescript
test('shows error on failed login', async ({ page, request }) => {
  // ...
  await expect(page.getByRole('button', { name: 'Log In' })).toBeEnabled({ timeout: 10000 });
```

- **Root Cause:** Playwright was given a strict `10000ms` window. In a busy CI runner, downloading the external Cloudflare script, parsing it, rendering the iframe, completing the validation, and pushing the token back to Angular sometimes takes longer than 10 seconds.
- **Why other tests passed:** The other login tests did *not* have this explicit assertion. They simply ran `.click()`, which inherently waits up to the global test timeout (30s) for the element to become visible and enabled.

**Resolution:** Removed the restrictive `toBeEnabled` check. By directly invoking `.click()`, Playwright will naturally wait for the external Turnstile script to finish loading and enable the button, without failing prematurely.

## Takeaways for Future Testing
1. **Always use Cloudflare's Dummy Keys for E2E:** `1x00000000000000000000AA` ensures Turnstile automatically succeeds.
2. **Avoid Explicit Fast Timeouts:** When waiting for third-party scripts (like Captchas), rely on Playwright's default element action-ability waits (e.g., let `.click()` do the waiting up to 30s) rather than enforcing strict assertion timeouts (`toBeEnabled({ timeout: 10000 })`).
3. **Avoid `networkidle` waits:** Similar to the Turnstile issue, explicit `page.waitForLoadState('networkidle')` commands (which were removed from `dashboard.spec.ts`) fail frequently because external tracking/captcha scripts make continuous or delayed network requests. Rely on actionable UI locators instead.
