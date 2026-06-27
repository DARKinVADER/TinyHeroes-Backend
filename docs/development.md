# Development Setup

## Table of Contents

- [Prerequisites](#prerequisites)
- [Quick Start (Docker — recommended)](#quick-start-docker--recommended)
- [Environment Variables (`.env`)](#environment-variables-env)
- [Local Development (without Docker)](#local-development-without-docker)
- [Database Migrations](#database-migrations)
- [Demo Account](#demo-account)
- [pgAdmin](#pgadmin)
- [Configuration Consistency](#configuration-consistency)
- [Setting Up Google Login](#setting-up-google-login)

---

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) — provides both `docker` and `docker compose` (v2)
- [.NET 10 SDK](https://dotnet.microsoft.com/download) — for running the backend locally or running EF migrations
- [Node.js 26+](https://nodejs.org/) — for running the frontend locally

> **macOS note:** Use `docker compose` (with a space). The old `docker-compose` hyphenated binary is v1 and not installed by Docker Desktop.

---

## Quick Start (Docker — recommended)

```bash
cp .env.example .env        # first time only; edit values as needed
docker compose up -d        # start postgres, api, frontend, pgadmin
docker compose logs -f api  # tail API logs
```

Services after startup:

| Service  | URL                   |
|----------|-----------------------|
| Frontend | http://localhost:4200 |
| API      | http://localhost:5001 |
| pgAdmin  | http://localhost:5050 |
| Seq logs | http://localhost:5341 |

Rebuild after code changes:

```bash
docker compose up -d --build
```

---

## Environment Variables (`.env`)

Copy from `.env.example` and fill in values. Key variables:

| Variable                 | Default in `.env.example`               | Notes                          |
|--------------------------|-----------------------------------------|--------------------------------|
| `POSTGRES_DB`            | `tinyheroes`                            |                                |
| `POSTGRES_USER`          | `tinyheroes`                            |                                |
| `POSTGRES_PASSWORD`      | `changeme`                              | Must match `appsettings.json`  |
| `JWT_SECRET`             | _(placeholder — replace with 32+ chars)_|                                |
| `JWT_ISSUER`             | `tinyheroes-api`                        |                                |
| `JWT_AUDIENCE`           | `tinyheroes-frontend`                   |                                |
| `JWT_EXPIRY_MINUTES`     | `60`                                    |                                |
| `STORAGE_CONNECTION_STRING` | `disk://path=/app/uploads`           | Container path — do not change for Docker |
| `HUGGINGFACE_API_KEY`    | _(empty)_                               | AI image generation (optional) |
| `HUGGINGFACE_MODEL`      | `black-forest-labs/FLUX.1-schnell`      |                                |
| `GOOGLE_CLIENT_ID/SECRET`| _(empty)_                               | Social login (optional)        |
| `APPLE_CLIENT_ID/SECRET` | _(empty)_                               | Social login (optional)        |
| `FACEBOOK_APP_ID/SECRET` | _(empty)_                               | Social login (optional)        |

---

## Local Development (without Docker)

### Backend

Requires the Docker Postgres container running (`docker compose up -d postgres`).

```bash
cd backend && dotnet run --project TinyHeroes.Api    # http://localhost:5032
cd backend && dotnet watch --project TinyHeroes.Api  # with hot reload
```

`appsettings.json` contains the local connection string (`Password=changeme`, which matches the Docker Postgres). Storage writes to `./uploads` relative to the running project.

When running the backend locally instead of via Docker, update `frontend/proxy.conf.json` target from `http://localhost:5000` to `http://localhost:5032`.

### Frontend

```bash
cd frontend && npm start    # http://localhost:4200
```

Proxies `/api` and `/uploads` to `http://localhost:5000` (Docker API) by default — see `proxy.conf.json`.

---

## Database Migrations

### Install the EF tool (once)

```bash
dotnet tool install --global dotnet-ef
```

Add `~/.dotnet/tools` to your PATH (the installer prints the exact command for your shell).

### Create a migration (after schema changes)

```bash
cd backend && dotnet ef migrations add <Name> \
  --project TinyHeroes.Infrastructure \
  --startup-project TinyHeroes.Api
```

### Apply migrations manually

```bash
cd backend && dotnet ef database update \
  --project TinyHeroes.Infrastructure \
  --startup-project TinyHeroes.Api
```

Migrations also run automatically on API startup (`Database.Migrate()` in `Program.cs`), so the Docker stack self-migrates on `docker compose up`.

---

## Demo Account

A pre-seeded account is available in the local Docker stack for manual testing — no sign-up required.

| Field    | Value                  |
|----------|------------------------|
| Email    | `testuser@demo.com`    |
| Password | `Password1!`           |
| Family   | Demo Family            |
| Children | Alice (5, Girl), Bob (7, Boy) |

> The account lives in the Postgres Docker volume and persists across container restarts. It is wiped if you remove the volume (`docker compose down -v`).

---

## pgAdmin

Navigate to http://localhost:5050 and log in:

| Field    | Value                    |
|----------|--------------------------|
| Email    | `admin@tinyheroes.com`   |
| Password | `admin`                  |

Register a server with:

| Field    | Value        |
|----------|--------------|
| Host     | `postgres`   |
| Port     | `5432`       |
| Database | `tinyheroes` |
| Username | `tinyheroes` |
| Password | `changeme`   |

> Use `postgres` as the host (the Docker service name) — pgAdmin runs inside the Docker network.
> If connecting from a tool outside Docker (e.g. TablePlus, DBeaver), use `localhost` instead.

---

## Configuration Consistency

`appsettings.json` is for local `dotnet run`. Docker overrides all settings via environment variables in `docker-compose.yml` sourced from `.env`. Values that must stay aligned:

| Setting           | `appsettings.json`  | `.env`       |
|-------------------|---------------------|--------------|
| Postgres password | `changeme`          | `changeme`   |
| Storage path      | `./uploads`         | `/app/uploads` (container path — intentionally different) |
| JWT secret        | independent value   | independent value — each run mode signs its own tokens |

---

## Setting Up Google Login

Google login is optional. When `GOOGLE_CLIENT_ID` is empty, the button still renders but the OAuth handshake will fail. Follow these steps to make it work.

### 1. Create a Google Cloud project

1. Go to [console.cloud.google.com](https://console.cloud.google.com/).
2. Click the project dropdown at the top → **New Project**.
3. Give it a name (e.g. `TinyHeroes`) and click **Create**.
4. Make sure the new project is selected in the dropdown.

### 2. Enable the Google+ / People API

1. In the left sidebar: **APIs & Services → Library**.
2. Search for **"Google+ API"** (or **"Google Identity"**), open it and click **Enable**.

> Newer projects may not need this step — the OAuth consent screen alone is sufficient. If the step above returns a "not found" page, skip it.

### 3. Configure the OAuth consent screen

1. **APIs & Services → OAuth consent screen**.
2. Choose **External** (unless this is an internal GSuite app) → **Create**.
3. Fill in the required fields:
   - **App name**: TinyHeroes
   - **User support email**: your Google account email
   - **Developer contact information**: same email
4. Click **Save and Continue** through Scopes (no extra scopes needed — `email` and `profile` are included by default).
5. Under **Test users**, add any Google accounts you want to test with during development.
6. Click **Save and Continue** → **Back to Dashboard**.

> While the app is in **Testing** status only the test users you added can log in. To open it up, click **Publish App** (requires Google verification for >100 users).

### 4. Create OAuth credentials

1. **APIs & Services → Credentials → Create Credentials → OAuth client ID**.
2. **Application type**: Web application.
3. **Name**: `TinyHeroes Web` (or any label).
4. Under **Authorised redirect URIs**, add the callback URL for each environment:

| Environment | Redirect URI |
|-------------|-------------|
| Local (Docker) | `http://localhost:5001/api/auth/social/google/callback` |
| Local (dotnet run) | `http://localhost:5032/api/auth/social/google/callback` |
| Production | `https://api.mytinyheroes.net/api/auth/social/google/callback` |

> Add all environments you plan to use — you can always come back and add more.

5. Click **Create**. A dialog shows your **Client ID** and **Client Secret** — copy both.

### 5. Set the credentials in your environment

**Docker (recommended):**

Open `.env` (copied from `.env.example`) and fill in:

```env
GOOGLE_CLIENT_ID=your-client-id.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=GOCSPX-your-secret
FRONTEND_URL=http://localhost:4200
```

Restart the stack to pick up the new values:

```bash
docker compose up -d --build
```

**Local `dotnet run` (without Docker):**

Either set environment variables in your shell:

```bash
export Auth__Google__ClientId="your-client-id.apps.googleusercontent.com"
export Auth__Google__ClientSecret="GOCSPX-your-secret"
```

Or add them to `backend/TinyHeroes.Api/appsettings.Development.json` (never commit real secrets):

```json
{
  "Auth": {
    "FrontendUrl": "http://localhost:4200",
    "Google": {
      "ClientId": "your-client-id.apps.googleusercontent.com",
      "ClientSecret": "GOCSPX-your-secret"
    }
  }
}
```

### 6. Test the flow

1. Open [http://localhost:4200/login](http://localhost:4200/login).
2. Click **Continue with Google**.
3. Complete the Google sign-in. You should be redirected to `/dashboard` (existing family) or `/create-family` (new user).

**If it doesn't work**, check:
- The redirect URI in Google Console matches exactly (scheme, host, port, path — no trailing slash).
- `GOOGLE_CLIENT_ID` / `GOOGLE_CLIENT_SECRET` are non-empty in `.env`.
- The Google account you're using is listed as a **test user** (while the consent screen is in Testing mode).
- Browser console and API logs (`docker compose logs -f api`) for OAuth error details.

### Production deployment

Set these environment variables on the production host (Azure Static Web Apps config / server env):

```
GOOGLE_CLIENT_ID=your-production-client-id.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=GOCSPX-your-production-secret
FRONTEND_URL=https://mytinyheroes.net
```

The production redirect URI registered in Google Console must be:

```
https://api.mytinyheroes.net/api/auth/social/google/callback
```
