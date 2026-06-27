# Domain Migration: mytinyheroes.net Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Update all hard-coded `tinyheroes.czibik.hu` references in the codebase to `mytinyheroes.net` so the CI/CD pipeline deploys to the new domain.

**Architecture:** Pure find-and-replace across 5 code files and 4 doc files. No logic changes. DNS and Azure custom domain registration have already been completed manually.

**Tech Stack:** Angular 21, ASP.NET Core 10, Bicep, GitHub Actions

---

## Files Modified

| File | Change |
|---|---|
| `frontend/src/environments/environment.prod.ts` | `apiUrl` → `https://api.mytinyheroes.net/api` |
| `frontend/src/environments/environment.integration.ts` | `apiUrl` → `https://api.integration.mytinyheroes.net/api` |
| `infra/main.bicep` | `allowedOrigin` → `https://mytinyheroes.net` |
| `infra/main.integration.bicep` | `allowedOrigin` → `https://integration.mytinyheroes.net` |
| `backend/http/http-client.env.json` | `host` (prod entry) → `https://api.mytinyheroes.net` |
| `docs/deployment.md` | All URL references updated |
| `docs/development.md` | OAuth callback URL reference updated |
| `CLAUDE.md` | Integration smoke test URL updated |
| `CHANGELOG.md` | Entry added under `## [Unreleased]` |

---

### Task 1: Update frontend environment files

**Files:**
- Modify: `frontend/src/environments/environment.prod.ts`
- Modify: `frontend/src/environments/environment.integration.ts`

- [ ] **Step 1: Update production environment**

Replace the entire file content of `frontend/src/environments/environment.prod.ts`:

```typescript
export const environment = {
  production: true,
  apiUrl: 'https://api.mytinyheroes.net/api',
  version: '2.4.2',
};
```

- [ ] **Step 2: Update integration environment**

Replace the entire file content of `frontend/src/environments/environment.integration.ts`:

```typescript
export const environment = {
  production: true,
  apiUrl: 'https://api.integration.mytinyheroes.net/api',
  version: '2.4.0',
};
```

- [ ] **Step 3: Verify no old domain remains in environment files**

```bash
grep -r "czibik" frontend/src/environments/
```

Expected: no output.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/environments/environment.prod.ts \
        frontend/src/environments/environment.integration.ts
git commit -m "chore: update frontend apiUrl to mytinyheroes.net"
```

---

### Task 2: Update Bicep infrastructure

**Files:**
- Modify: `infra/main.bicep`
- Modify: `infra/main.integration.bicep`

- [ ] **Step 1: Update prod Bicep allowedOrigin**

In `infra/main.bicep`, find line 55 and change:
```
allowedOrigin: 'https://tinyheroes.czibik.hu'
```
to:
```
allowedOrigin: 'https://mytinyheroes.net'
```

- [ ] **Step 2: Update integration Bicep allowedOrigin**

In `infra/main.integration.bicep`, find line 42 and change:
```
allowedOrigin: 'https://integration.tinyheroes.czibik.hu'
```
to:
```
allowedOrigin: 'https://integration.mytinyheroes.net'
```

- [ ] **Step 3: Verify no old domain remains in infra**

```bash
grep -r "czibik" infra/
```

Expected: no output.

- [ ] **Step 4: Commit**

```bash
git add infra/main.bicep infra/main.integration.bicep
git commit -m "chore: update Bicep allowedOrigin to mytinyheroes.net"
```

---

### Task 3: Update backend HTTP client dev file

**Files:**
- Modify: `backend/http/http-client.env.json`

- [ ] **Step 1: Update production host**

In `backend/http/http-client.env.json`, find line 14 and change:
```json
"host": "https://api.tinyheroes.czibik.hu"
```
to:
```json
"host": "https://api.mytinyheroes.net"
```

The `localhost` entry on line 3 must remain unchanged.

- [ ] **Step 2: Verify**

```bash
grep "czibik" backend/http/http-client.env.json
```

Expected: no output.

- [ ] **Step 3: Commit**

```bash
git add backend/http/http-client.env.json
git commit -m "chore: update http-client prod host to mytinyheroes.net"
```

---

### Task 4: Update docs/deployment.md

**Files:**
- Modify: `docs/deployment.md`

- [ ] **Step 1: Replace all URL references**

Run this sed command to replace all occurrences at once:

```bash
sed -i '' \
  -e 's|tinyheroes\.czibik\.hu|mytinyheroes.net|g' \
  -e 's|api\.tinyheroes\.czibik\.hu|api.mytinyheroes.net|g' \
  -e 's|integration\.tinyheroes\.czibik\.hu|integration.mytinyheroes.net|g' \
  -e 's|api\.integration\.tinyheroes\.czibik\.hu|api.integration.mytinyheroes.net|g' \
  -e 's|czibik\.hu nameserver|Cloudflare (mytinyheroes.net nameserver)|g' \
  docs/deployment.md
```

- [ ] **Step 2: Update the DNS section text manually**

The DNS section describes CNAME records with `czibik.hu` zone examples. Open `docs/deployment.md` and update the two DNS code blocks (around lines 332–336 and 286–294) to reflect Cloudflare and the new domain:

```
# Production DNS (Cloudflare — mytinyheroes.net)
mytinyheroes.net         CNAME flatten  <swa-name>.azurestaticapps.net
api.mytinyheroes.net     CNAME          <appservice-name>.azurewebsites.net

# Integration DNS (Cloudflare — mytinyheroes.net)
integration.mytinyheroes.net      CNAME  <integration-swa-name>.azurestaticapps.net
api.integration.mytinyheroes.net  CNAME  <integration-appservice-name>.azurewebsites.net
```

Also update the App Service env var table values (around line 320):
```
AllowedOrigins__0  →  https://mytinyheroes.net
Auth__FrontendUrl  →  https://mytinyheroes.net
```

- [ ] **Step 3: Verify no old domain remains**

```bash
grep "czibik" docs/deployment.md
```

Expected: no output.

- [ ] **Step 4: Commit**

```bash
git add docs/deployment.md
git commit -m "docs: update deployment.md URLs to mytinyheroes.net"
```

---

### Task 5: Update docs/development.md and CLAUDE.md

**Files:**
- Modify: `docs/development.md`
- Modify: `CLAUDE.md`

- [ ] **Step 1: Update development.md OAuth callback URLs**

```bash
sed -i '' \
  -e 's|api\.tinyheroes\.czibik\.hu|api.mytinyheroes.net|g' \
  -e 's|FRONTEND_URL=https://tinyheroes\.czibik\.hu|FRONTEND_URL=https://mytinyheroes.net|g' \
  docs/development.md
```

- [ ] **Step 2: Verify development.md**

```bash
grep "czibik" docs/development.md
```

Expected: no output.

- [ ] **Step 3: Update CLAUDE.md smoke test URL**

In `CLAUDE.md`, find line 232:
```
open `https://integration.tinyheroes.czibik.hu`
```
and change to:
```
open `https://integration.mytinyheroes.net`
```

- [ ] **Step 4: Verify CLAUDE.md**

```bash
grep "czibik" CLAUDE.md
```

Expected: no output (the personal email/LinkedIn references like `gabor.czibik@gmail.com` are in source components, not CLAUDE.md — those should remain unchanged).

- [ ] **Step 5: Commit**

```bash
git add docs/development.md CLAUDE.md
git commit -m "docs: update development.md and CLAUDE.md URLs to mytinyheroes.net"
```

---

### Task 6: Update CHANGELOG.md and open PR

**Files:**
- Modify: `CHANGELOG.md`

- [ ] **Step 1: Add changelog entry**

Open `CHANGELOG.md` and add under `## [Unreleased]`:

```markdown
### Changed
- Migrated app domain from `tinyheroes.czibik.hu` to `mytinyheroes.net` (production and integration environments)
```

- [ ] **Step 2: Final scan — no old domain anywhere in tracked files**

```bash
grep -r "tinyheroes\.czibik\.hu\|integration\.tinyheroes\.czibik" \
  frontend/src/ infra/ backend/http/ docs/ CLAUDE.md CHANGELOG.md \
  --include="*.ts" --include="*.json" --include="*.bicep" --include="*.md"
```

Expected: no output.

- [ ] **Step 3: Commit changelog**

```bash
git add CHANGELOG.md
git commit -m "chore: add domain migration changelog entry"
```

- [ ] **Step 4: Push and open PR**

```bash
git push -u origin fix/34-domain-migration
gh pr create \
  --base master \
  --title "chore: migrate domain to mytinyheroes.net" \
  --body "Closes #34

Updates all hard-coded \`tinyheroes.czibik.hu\` references to \`mytinyheroes.net\`.

DNS, Azure custom domains, App Service env vars, and Google OAuth have already been updated manually. This PR updates the code and docs to match.

### Changes
- \`environment.prod.ts\` — apiUrl
- \`environment.integration.ts\` — apiUrl  
- \`infra/main.bicep\` — allowedOrigin
- \`infra/main.integration.bicep\` — allowedOrigin
- \`backend/http/http-client.env.json\` — prod host
- \`docs/deployment.md\`, \`docs/development.md\`, \`CLAUDE.md\`, \`CHANGELOG.md\` — URL references

### Smoke test after merge
- https://mytinyheroes.net
- https://api.mytinyheroes.net/api/info
- https://integration.mytinyheroes.net"
```

---

### Task 7: Post-merge cleanup

- [ ] **Step 1: Smoke test production**

After the deploy completes (~5 min after merge):

```
https://mytinyheroes.net               → Angular app loads
https://api.mytinyheroes.net/api/info  → returns JSON with version + environment
```

Log in with the demo account (`testuser@demo.com / Password1!`) and verify the dashboard loads.

- [ ] **Step 2: Smoke test integration**

```
https://integration.mytinyheroes.net  → Angular app loads
```

Log in with `testuser@demo.com / Password1!` and verify the dashboard loads.

- [ ] **Step 3: Remove old custom domains from Azure**

In Azure Portal:
- Prod SWA → Custom domains → remove `tinyheroes.czibik.hu`
- Prod App Service → Custom domains → remove `api.tinyheroes.czibik.hu`
- Integration SWA → Custom domains → remove `integration.tinyheroes.czibik.hu`
- Integration App Service → Custom domains → remove `api.integration.tinyheroes.czibik.hu`

- [ ] **Step 4: Close issue**

```bash
gh issue close 34 --comment "Implemented in the domain migration PR. All four environments verified at mytinyheroes.net."
```
