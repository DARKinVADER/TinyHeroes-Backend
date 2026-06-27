# TinyHeroes

TinyHeroes is a family web app where parents track good deeds, compare weekly and monthly rankings, and assign prizes to winners.

It is built with Angular 21 on the frontend and ASP.NET Core 10 on the backend.

## Table of Contents

- [At a Glance](#at-a-glance)
- [Start Here](#start-here)
- [Configuration](#configuration)
- [Running with Docker](#running-with-docker)
- [Local Development](#local-development)
- [Debugging](#debugging)
- [Tech Stack](#tech-stack)
- [Documentation](#documentation)
- [Project Structure](#project-structure)
- [Features](#features)
- [Environment Variables](#environment-variables)
- [Notes](#notes)

---

## At a Glance

- Web UI for parents and co-parents
- Good deed tracking with preset and custom actions
- Weekly podium and monthly champion summaries
- Prize management for short- and long-term rewards
- Social login support for Google, Apple, and Facebook

## Start Here

If you want the fastest path to a running system, use Docker:

```bash
cp .env.example .env
docker compose up -d
```

Then open the Docker services:

| Service | URL |
|---------|-----|
| Frontend | http://localhost:4200 |
| API | http://localhost:5001 |
| OpenAPI JSON | http://localhost:5001/openapi/v1.json |
| Scalar UI | http://localhost:5001/scalar/v1 |
| Health check | http://localhost:5001/health |
| pgAdmin | http://localhost:5050 |
| Seq logs | http://localhost:5341 |

These URLs apply when you run the stack through Docker. If you are debugging the backend locally with `dotnet run`, use the launch-profile URLs in the local development section below.

## Configuration

### Root environment file

Create `.env` from `.env.example` before starting Docker. Use it for the shared runtime values needed by the backend, frontend, and database container.

### Backend configuration

- `backend/TinyHeroes.Api/appsettings.json` contains the app runtime settings for JWT, OAuth, storage, and allowed origins.
- `backend/TinyHeroes.Api/appsettings.Development.json` overrides logging sinks (Console + Seq) and sets the initial log level to `Information`.
- `backend/TinyHeroes.Api/Properties/launchSettings.json` controls local debug URLs and the `Development` environment.

### Frontend configuration

- `frontend/src/environments/environment.ts` points local Angular development to the API.
- `frontend/src/environments/environment.prod.ts` uses the production `/api` path behind nginx.

If you run the backend with the launch profile, the default HTTP URL is `http://localhost:5032` and the HTTPS URL is `https://localhost:7150`.

For local backend debugging, the API endpoints are:

| Service | URL |
|---------|-----|
| API | http://localhost:5032 |
| HTTPS API | https://localhost:7150 |
| OpenAPI JSON | http://localhost:5032/openapi/v1.json |
| Scalar UI | http://localhost:5032/scalar/v1 |

If you run the frontend separately outside Docker, update the Angular environment and proxy target to match the backend port you are using.

### Production configuration

In production, the frontend is served by nginx and proxies `/api` and `/uploads` to the backend. Keep the production frontend API path as `/api`, and provide the backend runtime values through your deployment environment or the Docker `.env` file.

## Running with Docker

The simplest way to run everything — no local toolchain required.

### Prerequisites

- Docker and Docker Compose

### Steps

```bash
# 1. Copy the env template and fill in your secrets
cp .env.example .env

# 2. Start all services (Postgres, API, frontend)
docker compose up -d

# 3. Verify
docker compose ps    # All 3 services should be "Up"
```

The frontend nginx container proxies `/api/` and `/uploads/` to the backend automatically. Uploaded files are stored in a Docker volume (`uploads_data`).

### pgAdmin (database GUI)

pgAdmin is included in the stack and available at http://localhost:5050.

**Login:** `admin@tinyheroes.com` / `admin`

To connect to the database, add a server after logging in:

| Field | Value |
|-------|-------|
| Host | `postgres` |
| Port | `5432` |
| Database | `tinyheroes` |
| Username | `tinyheroes` |
| Password | `changeme` (from `.env`) |

Use `postgres` as the host — pgAdmin runs inside the Docker network, not on the host machine.

```bash
# Stop everything
docker compose down

# Stop and remove data (database + uploads)
docker compose down -v
```

---

## Local Development

Run the backend and frontend separately for a faster edit-reload cycle with debugger support.

### Prerequisites

- [.NET 10 SDK](https://dot.net/download)
- [Node.js 26+](https://nodejs.org/)
- PostgreSQL 16 (or use Docker just for the database)

### 1. Database

Option A — Run Postgres in Docker (recommended):
```bash
docker compose up -d postgres
```

Option B — Use a local PostgreSQL instance and update the connection string in `backend/TinyHeroes.Api/appsettings.json`:
```json
"ConnectionStrings": {
  "Default": "Host=localhost;Database=tinyheroes;Username=tinyheroes;Password=localdev"
}
```

### 2. Backend

```bash
cd backend
dotnet run --project TinyHeroes.Api
```

The API starts with the launch settings defined in `backend/TinyHeroes.Api/Properties/launchSettings.json`. By default that is `http://localhost:5032` for HTTP and `https://localhost:7150` for HTTPS.

In Development, the API also exposes OpenAPI JSON at `/openapi/v1.json` and Scalar UI at `/scalar/v1`.

Uploaded files are saved to the path configured in `appsettings.json` → `Storage:ConnectionString` (default: `/app/uploads`). For local dev on Windows, change it to a local path:
```json
"Storage": {
  "ConnectionString": "disk://path=C:/temp/tinyheroes-uploads"
}
```

The repo also includes a ready-to-run REST Client file at [backend/TinyHeroes.Api/TinyHeroes.Api.http](backend/TinyHeroes.Api/TinyHeroes.Api.http) covering the full API flow. If you keep the backend on port `5032`, update `@host` in that file to match.

### 3. Frontend

```bash
cd frontend
npm install
npm start
```

The Angular dev server starts on `http://localhost:4200` and proxies `/api` and `/uploads` to `http://localhost:5000` when you are using the Docker API via [proxy.conf.json](frontend/proxy.conf.json). For backend debugging against `dotnet run`, change that target to `http://localhost:5032`.

### 4. Run Tests

```bash
# Backend integration tests (uses in-memory database, no Postgres needed)
cd backend && dotnet test

# Frontend build check
cd frontend && npx ng build --configuration production
```

### 5. What to Verify While Debugging Locally

- Backend starts cleanly and serves the API on the expected port.
- `GET /openapi/v1.json` returns the OpenAPI document in Development.
- `GET /scalar/v1` returns the Scalar UI in Development.
- Authentication works end-to-end: register, login, and call one authorized endpoint.
- CORS allows the frontend origin you are using.
- Uploads are written to the configured storage path.
- Frontend reads the right API base URL for the port you are running.

## Production Checklist

Use this when you build or deploy the app outside your local machine.

### Configuration to provide

- Backend connection string for PostgreSQL.
- JWT secret, issuer, and audience.
- OAuth client IDs and secrets for Google, Apple, and Facebook if you use social login.
- Storage connection string for uploads.
- Any AI image generation keys or endpoints used by the backend.

### What to test in production or staging

- Frontend loads from the nginx container or hosting target.
- API calls succeed through the `/api` proxy path.
- Uploads are available through `/uploads`.
- `GET /openapi/v1.json` and `GET /scalar/v1` are not exposed outside Development.
- The app can register, log in, create a family, add a child, and record a good deed.
- Tests still pass in CI before deployment: backend `dotnet test` and frontend production build.

---

## Debugging

### Backend (Visual Studio / VS Code / Rider)

Open the `backend/` folder or `TinyHeroes.slnx` solution. The launch profile is pre-configured:

- **Visual Studio**: Open `TinyHeroes.slnx`, set `TinyHeroes.Api` as startup project, press F5.
- **VS Code**: Open `backend/`, install C# Dev Kit extension, press F5 (uses `launch.json` or auto-detected profile).
- **Rider**: Open `TinyHeroes.slnx`, run `TinyHeroes.Api` configuration.
- **CLI with hot reload**: `cd backend && dotnet watch --project TinyHeroes.Api`

### Frontend (VS Code / WebStorm)

- **VS Code**: Open `frontend/`, run `npm start`, use the built-in JavaScript debugger (attach to Chrome) or press F5 with a launch.json pointing to `http://localhost:4200`.
- **WebStorm**: Open `frontend/`, run the `start` npm script, use the built-in debugger.
- **Browser DevTools**: Navigate to http://localhost:4200, open DevTools (F12). Source maps are enabled in development.

### Docker

```bash
# View logs
docker compose logs -f api
docker compose logs -f frontend

# Shell into a running container
docker compose exec api sh
docker compose exec frontend sh

# Rebuild after code changes
docker compose up -d --build
```

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Frontend | Angular 21, Tailwind 4, ngx-translate |
| Backend | ASP.NET Core 10, EF Core 10, PostgreSQL 16 |
| Auth | ASP.NET Identity + JWT + OAuth2 (Google, Apple, Facebook) |
| AI Images | Hugging Face Inference API (FLUX.1-schnell) |
| Storage | Local disk (swappable via config) |
| Deploy | Docker + nginx |

## Documentation

### Design Specification

- [Product Design Spec](docs/superpowers/specs/2026-05-16-tinyheroes-design.md) — Full feature spec, data model, screen descriptions

### Observability

- [Structured Logging](docs/logging.md) — Serilog setup, Seq (local), Azure Log Analytics (production), KQL queries, runtime level control
- [Distributed Tracing](docs/tracing.md) — OpenTelemetry setup, Application Insights, custom spans, correlating traces with logs
- [Deployment & Application Insights](docs/deployment.md) — Azure resource setup, health endpoints, cost controls

### API Samples

- [TinyHeroes.Api.http](backend/TinyHeroes.Api/TinyHeroes.Api.http) - REST Client samples for auth, families, children, deeds, presets, prizes, and summaries

### Screen Mockups

All mockups are viewable HTML files in `docs/superpowers/screens/`:

| Screens | File |
|---------|------|
| Auth & Onboarding | [screens-auth.html](docs/superpowers/screens/screens-auth.html) |
| Dashboard & Invite | [screens-dashboard.html](docs/superpowers/screens/screens-dashboard.html) |
| Add Child & Profile | [screens-children.html](docs/superpowers/screens/screens-children.html) |
| Add Good Deed | [screens-deed-v2.html](docs/superpowers/screens/screens-deed-v2.html) |
| Manage Presets | [screens-presets.html](docs/superpowers/screens/screens-presets.html) |
| Weekly Podium | [screens-podium.html](docs/superpowers/screens/screens-podium.html) |
| Monthly Champion | [screens-monthly-v2.html](docs/superpowers/screens/screens-monthly-v2.html) |
| History & Prizes Board | [screens-simplified.html](docs/superpowers/screens/screens-simplified.html) |
| Prize Editor | [screens-prize-editor-v2.html](docs/superpowers/screens/screens-prize-editor-v2.html) |
| Custom Prizes | [screens-custom-prizes.html](docs/superpowers/screens/screens-custom-prizes.html) |
| Settings | [screens-settings.html](docs/superpowers/screens/screens-settings.html) |
| My Profile | [screens-updates.html](docs/superpowers/screens/screens-updates.html) |
| Architecture | [architecture.html](docs/superpowers/screens/architecture.html) |

### Implementation Plans

| # | Plan | Status |
|---|------|--------|
| 1 | [Foundation & Auth](docs/superpowers/plans/2026-05-16-plan-1-foundation.md) | Done |
| 2 | [Family Management](docs/superpowers/plans/2026-05-16-plan-2-family-management.md) | Done |
| 3 | [Good Deeds & Podium](docs/superpowers/plans/2026-05-16-plan-3-good-deeds-podium.md) | Done |
| 4 | [Monthly & History](docs/superpowers/plans/2026-05-16-plan-4-monthly-history.md) | Done |
| 5 | [Prizes](docs/superpowers/plans/2026-05-16-plan-5-prizes.md) | Done |
| 6 | [Settings](docs/superpowers/plans/2026-05-17-plan-6-settings.md) | Done |
| 7 | [Prize Display](docs/superpowers/plans/2026-05-17-plan-7-prize-display.md) | Done |
| 8 | [AI Image Generation](docs/superpowers/plans/2026-05-17-plan-8-ai-image-generation.md) | Done |
| 9 | [Avatar Photo Upload](docs/superpowers/plans/2026-05-17-plan-9-avatar-photo-upload.md) | Done |
| 10 | [Multi-Language](docs/superpowers/plans/2026-05-17-plan-10-multi-language.md) | Done |
| 11 | [Docker Polish](docs/superpowers/plans/2026-05-17-plan-11-docker-polish.md) | Done |
| 12 | [Help Page](docs/superpowers/plans/2026-05-31-plan-12-help-page.md) | Done |
| — | [Customization in Settings](docs/superpowers/plans/2026-05-31-customization-in-settings.md) | Done |

## Project Structure

```
backend/
  TinyHeroes.Domain/         # Entities, enums, interfaces
  TinyHeroes.Application/    # DTOs, service interfaces
  TinyHeroes.Infrastructure/ # EF Core, services, DI
  TinyHeroes.Api/            # Controllers, middleware, Program.cs
  TinyHeroes.Tests/          # Integration and unit tests

frontend/
  src/app/core/              # Services, models, auth
  src/app/features/          # Dashboard, deeds, podium, prizes, settings
  src/app/shared/            # Shell, bottom nav
  public/assets/i18n/        # EN, HU, DE, FR, ES translations
```

## Features

- Family creation and co-parent invites
- Child profiles with emoji or photo avatars
- Good deed tracking with preset/custom actions
- AI-generated deed images (Hugging Face)
- Weekly podium with 1st/2nd/3rd prizes
- Monthly champion with grand prize
- Full history with rankings
- Prize management (custom + built-in suggestions)
- Customization section in Settings (deed presets, custom prizes, prize eligibility rules)
- Public help page at `/help` (9 sections, scroll-spy, 5-language support)
- 5-language support (English, Hungarian, German, French, Spanish)
- Social login (Google, Apple, Facebook)

## Environment Variables

See [.env.example](.env.example) for all configuration options including database, JWT, OAuth, storage, AI image, and logging settings.

## Notes

- The backend OpenAPI spec and Scalar UI are only enabled in Development.
- The frontend dev server proxies `/api` and `/uploads` to the backend during local development.
- The `/help` route is publicly accessible (no login required) and serves as the in-app user guide.
