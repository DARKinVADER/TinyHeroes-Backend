# Feedback & Issue Reporting — Design Spec

**Issue:** [#19 — Issue reporting](https://github.com/DARKinVADER/TinyHeroes/issues/19)
**Milestone:** v3.0 - MVP
**Date:** 2026-06-06

---

## Problem

Users have no way to report bugs or share feedback from within the app. The only path is a direct email or GitHub issue — neither of which is discoverable or low-friction for non-technical users.

---

## Solution Summary

A floating "Feedback" button (pill-shaped, bottom-right) visible on every page. Clicking it opens a two-step modal: step 1 picks a category (Bug / Idea / General), step 2 collects a freeform message plus an optional email address (for anonymous users only — authenticated users' emails are pre-filled silently). On submit, the backend sends an email to the admin address via MailKit over SMTP.

---

## UX Design

### Entry point

- **Extended FAB** — pill button, bottom-right corner, `💬 Feedback` label
- Visible on all routes including public pages (landing, login, signup)
- On mobile: collapses to icon-only (`💬`) to avoid overlapping page content
- z-index above page content but below modals

### Step 1 — Category selection

Three large tap-target cards:
| Icon | Label | Subtitle |
|---|---|---|
| 🐛 | Report a bug | Something isn't working |
| 💡 | Share an idea | I wish it could… |
| 💬 | General feedback | Something else |

### Step 2 — Message form

Fields:
- **Message** — textarea, required, placeholder adapts to category ("Describe the bug…", "What would you like to see?", "Tell us what you think…")
- **Email** — text input, optional, only shown to **anonymous users**; authenticated users' email captured silently from auth state, never shown in the form
- **Back** link returns to step 1
- **Send** button, disabled while submitting, shows spinner

### Success state

Modal body replaces with: `✅ Thanks! We've received your feedback.` with a close button. No redirect.

### Error state

Inline error below the form: `Something went wrong — please try again.` Submit button re-enabled.

---

## Architecture

### Frontend

**New component:** `FeedbackButtonComponent` (standalone)
- Self-contained: FAB + modal + form logic in one component
- Mounted once in `AppComponent` template, outside `<router-outlet>`
- Uses Angular signals for step state (`'category' | 'message' | 'success' | 'error'`)
- Calls `FeedbackService.submit(payload)` on send
- No route changes, no router dependency

**New service:** `FeedbackService`
- `submit(payload: FeedbackPayload): Observable<void>`
- `FeedbackPayload`: `{ category: 'bug' | 'idea' | 'general', message: string, email?: string }`
- For authenticated users: reads email from `AuthService` signal, sets `email` on payload, omits the email field from the form
- POSTs to `POST /api/feedback`

### Backend

**New endpoint:** `POST /api/feedback`
- Controller: `FeedbackController` in `TinyHeroes.Api/Controllers/`
- No authentication required (public endpoint)
- Rate-limited: 5 requests per hour per IP (new `"feedback"` rate limit policy)
- Request DTO: `FeedbackRequest { Category, Message, Email? }` with validation (message required, max 2000 chars; email format if provided)
- On valid request: calls `IEmailService.SendFeedbackAsync(...)`, returns `204 No Content`
- On validation failure: `400 Bad Request`
- On email send failure: logs error, still returns `204` (fire-and-forget — don't punish the user for infra issues)

**New service:** `IEmailService` / `EmailService`
- Lives in `TinyHeroes.Infrastructure/Services/`
- Uses **MailKit** (NuGet: `MailKit`) — no third-party SaaS dependency
- Sends to configured admin address via SMTP
- Config section `"Email"` in `appsettings.json`:
  ```json
  {
    "Email": {
      "SmtpHost": "smtp.resend.com",
      "SmtpPort": 587,
      "Username": "resend",
      "Password": "",
      "FromAddress": "noreply@mytinyheroes.net",
      "AdminAddress": "feedback@mytinyheroes.net"
    }
  }
  ```
- Corresponding env vars in `.env.example`: `SMTP_HOST`, `SMTP_PORT`, `SMTP_USERNAME`, `SMTP_PASSWORD`, `SMTP_FROM`, `SMTP_ADMIN_ADDRESS`

### Production SMTP setup (Cloudflare domain + Resend relay)

Cloudflare manages DNS but has no outbound SMTP. **Resend** is the standard relay for Cloudflare-managed domains — you add DNS records in Cloudflare, Resend handles delivery, emails genuinely come from `noreply@mytinyheroes.net`. Free tier: 3,000 emails/month, 100/day.

---

**Part A — Resend: verify your domain**

1. Go to [resend.com](https://resend.com) → **Sign up** (free, no card needed)
2. Dashboard → **Domains** → **Add Domain**
3. Enter `mytinyheroes.net` → click **Add**
4. Resend shows you a list of DNS records to add — keep this tab open

**Part B — Cloudflare: add the DNS records**

In a new tab: Cloudflare Dashboard → select `mytinyheroes.net` → **DNS** → **Records**

Add each record Resend listed. There will be 3–4:

| Type | Name | Value | Notes |
|---|---|---|---|
| `TXT` | `mytinyheroes.net` | `v=spf1 include:amazonses.com ~all` | SPF — merge with existing SPF if one exists |
| `TXT` | `resend._domainkey.mytinyheroes.net` | `p=MIGf…` (long key) | DKIM — copy exactly from Resend |
| `TXT` | `_dmarc.mytinyheroes.net` | `v=DMARC1; p=none;` | DMARC — skip if one already exists |
| `MX` | `send.mytinyheroes.net` | `feedback-smtp.us-east-1.amazonses.com` | Only if Resend asks for it |

> **Proxy toggle:** Set all these records to **DNS only** (grey cloud), not Proxied. DNS TXT/MX records must not be proxied.

5. Back in Resend → click **Verify DNS records**
6. Status turns green — usually under 5 minutes, occasionally up to 24h for DNS propagation

**Part C — Resend: create an API key**

1. Resend Dashboard → **API Keys** → **Create API Key**
2. Name: `TinyHeroes Production`
3. Permission: **Sending access** (not Full access)
4. Copy the key (shown once — save it now): `re_xxxxxxxxxxxxxxxxxxxx`

**Part D — Cloudflare Email Routing: forward received feedback to your inbox**

This is separate from sending — it lets `feedback@mytinyheroes.net` forward incoming replies to your personal email.

1. Cloudflare Dashboard → `mytinyheroes.net` → **Email** → **Email Routing**
2. Click **Enable Email Routing** (adds MX records automatically — Cloudflare warns if they conflict with Resend's MX, resolve by keeping Cloudflare's)
3. **Routing rules** → **Custom addresses** → **Create address**:
   - Address: `feedback`
   - Action: **Send to** → your personal email address
4. Verify your personal email when Cloudflare sends a confirmation link
5. Status shows **Active**

> **Note:** Resend handles *sending* (`noreply@` → user's inbox). Cloudflare Email Routing handles *receiving* (replies to `feedback@` → your inbox). They use different DNS records and don't conflict.

**Part E — Set environment variables in production**

```
SMTP_HOST=smtp.resend.com
SMTP_PORT=587
SMTP_USERNAME=resend                          # always the literal string "resend"
SMTP_PASSWORD=re_xxxxxxxxxxxxxxxxxxxx         # API key from Part C
SMTP_FROM=noreply@mytinyheroes.net            # must match verified Resend domain
SMTP_ADMIN_ADDRESS=feedback@mytinyheroes.net  # Cloudflare routes replies to your inbox
```

---

**Part F — Local dev: Mailpit**

Add to `docker-compose.yml`:
```yaml
mailpit:
  image: axllent/mailpit
  ports:
    - "1025:1025"   # SMTP
    - "8025:8025"   # Web UI — view all sent emails at http://localhost:8025
```
`appsettings.Development.json` defaults to `localhost:1025`, no auth — nothing leaves your machine.

**Email format:**
```
Subject: [TinyHeroes Feedback] 🐛 Bug report
From: noreply@mytinyheroes.net
To: feedback@mytinyheroes.net
Reply-To: <submitter email if provided>

Category: Bug
From: user@example.com (authenticated) / anonymous
Message:
<message text>
```

### Tests

**Backend:**
- `FeedbackControllerTests` integration test: valid payload → 204, missing message → 400, oversized message → 400, rate limit exceeded → 429
- `EmailService` unit test with a mock SMTP client (MailKit supports `IMailTransport` for mocking)

**Frontend:**
- Extend existing Playwright e2e: open feedback modal, complete two-step flow, assert success state shown
- Mock `POST /api/feedback` in e2e tests

---

## Out of scope

- Storing feedback in the database (no admin UI, no history)
- Screenshot attachment
- Feedback status tracking / replies from within the app
- Push notifications on new submission

---

## Documentation updates

- **help.component.ts** — no new screen but note the feedback button in the general help text
- **CHANGELOG.md** — add entry under `[Unreleased] / Added`
- **docs/deployment.md** — add SMTP environment variables section
- **README.md** — no changes needed
- **Version bump** — MINOR bump: `frontend` and `backend` both get new features

---

## Verification

1. `docker compose up -d --build` — full stack running
2. Navigate to any page — pill button visible bottom-right
3. Click button → step 1 appears with 3 category cards
4. Select "Bug" → step 2 with textarea and back link
5. Fill message, submit → success state shown
6. Check admin inbox — email received with correct subject, category, message
7. As authenticated user — email field absent from form, email still appears in received message
8. As anonymous user — email field present; fill in an address; check Reply-To header in received email
9. Submit 6 times rapidly → 6th returns 429 (rate limit)
10. Run `cd backend && dotnet test` and `cd frontend && npm run e2e` — all pass
