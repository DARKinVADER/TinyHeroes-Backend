# Domain Migration: tinyheroes.czibik.hu → mytinyheroes.net

**Date:** 2026-06-03
**Issue:** #34
**Status:** In Progress — DNS and Azure custom domains already done; code PR remaining

---

## Goal

Migrate the app from `tinyheroes.czibik.hu` to `mytinyheroes.net`. Hard cutover — no redirects from old domain.

---

## New URLs

| Environment | Old | New |
|---|---|---|
| Production frontend | `tinyheroes.czibik.hu` | `mytinyheroes.net` |
| Production API | `api.tinyheroes.czibik.hu` | `api.mytinyheroes.net` |
| Integration frontend | `integration.tinyheroes.czibik.hu` | `integration.mytinyheroes.net` |
| Integration API | `api.integration.tinyheroes.czibik.hu` | `api.integration.mytinyheroes.net` |

---

## Manual Steps (already completed)

- Cloudflare DNS: CNAME flatten for apex `mytinyheroes.net`, CNAMEs for `api`, `integration`, `api.integration` subdomains; TXT `asuid.*` records for Azure validation
- Azure custom domains registered on all 4 resources (prod SWA, prod App Service, integration SWA, integration App Service); SSL certs issued
- Azure App Service env vars updated on both prod and integration:
  - `AllowedOrigins__0` → new frontend URL
  - `Auth__FrontendUrl` → new frontend URL
- Google OAuth redirect URI updated to `https://api.mytinyheroes.net/api/auth/social/google/callback`

---

## Code Changes (PR)

### `frontend/src/environments/environment.prod.ts`
```
apiUrl: 'https://api.mytinyheroes.net/api'
```

### `frontend/src/environments/environment.integration.ts`
```
apiUrl: 'https://api.integration.mytinyheroes.net/api'
```

### `infra/main.bicep`
```
allowedOrigin: 'https://mytinyheroes.net'
```

### `infra/main.integration.bicep`
```
allowedOrigin: 'https://integration.mytinyheroes.net'
```

### `backend/http/http-client.env.json`
```
"host": "https://api.mytinyheroes.net"
```

### Docs
- `docs/deployment.md` — update all URL references
- `docs/development.md` — update OAuth callback URL reference
- `CLAUDE.md` — update integration smoke test URL
- `CHANGELOG.md` — add entry under `## [Unreleased]`

---

## Post-PR Steps

1. Remove old `tinyheroes.czibik.hu` custom domains from Azure (SWA + App Service)
2. Remove old `integration.tinyheroes.czibik.hu` custom domains from Azure
3. Smoke test `https://mytinyheroes.net` — login, dashboard, deed creation
4. Smoke test `https://api.mytinyheroes.net/api/info` — returns version + environment
5. Smoke test `https://integration.mytinyheroes.net` with `testuser@demo.com / Password1!`

---

## What Does Not Change

- Azure resource names
- GitHub Secrets (reference App Service names and ACR credentials, not domain URLs)
- CI/CD pipeline workflows
- ACR image tags
- Database connection strings
