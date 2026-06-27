# Azure Hosting & CI/CD Design — TinyHeroes

**Date:** 2026-05-25
**Status:** Approved

## Context

Host TinyHeroes under `tinyheroes.czibik.hu` (owned domain) on Azure with the most
cost-effective setup that can scale when the app opens to other families. The app is
currently personal (2–5 users) but must not require a full redesign to go public.

## Architecture

```
User browser
    │
    ├── tinyheroes.czibik.hu ──► Azure Static Web Apps (Angular SPA, Free tier)
    │                                    │  HTTPS API calls to absolute URL
    │                                    ▼
    └── api.tinyheroes.czibik.hu ──► Azure App Service B1 Linux
                                      (.NET 10 API — Docker container)
                                          │              │
                               PostgreSQL Flexible   Azure Blob Storage
                               Server (B1ms)         (uploads container)
                                                         ▲
                                          ACR (Basic) ───┘
                                     tinyheroes.azurecr.io
```

Two DNS CNAME records on czibik.hu. Both services provide free managed SSL automatically.

## Azure Resources

| Resource | Tier | Purpose | Est. $/mo |
|---|---|---|---|
| Azure Static Web Apps | Free | Angular SPA + CDN + auto SSL | $0 |
| Azure App Service Plan | B1 Linux | Hosts .NET API container, always on | $13 |
| Azure Container Registry | Basic | Docker image store | $5 |
| Azure Database for PostgreSQL Flexible Server | B1ms (1 vCore, 2 GB) | Managed Postgres 16, auto-backups | $13 |
| Azure Blob Storage | LRS Standard | File uploads (replaces local disk) | ~$1 |
| **Total** | | | **~$32/mo** |

## IaC — Bicep

```
infra/
├── main.bicep              # orchestrates modules, outputs hostnames
├── main.prod.bicepparam    # non-secret parameter values
└── modules/
    ├── acr.bicep           # Container Registry (admin enabled)
    ├── postgres.bicep      # Flexible Server + Azure firewall rule + DB user
    ├── storage.bicep       # Storage Account + uploads blob container (public blob)
    ├── appservice.bicep    # App Service Plan + Web App (Docker) + all env vars
    └── swa.bicep           # Static Web Apps linked to GitHub repo
```

Secrets (`postgresAdminPassword`, `jwtSecret`, `repositoryToken`, `huggingFaceApiKey`)
are passed as parameters at deploy time — never committed to git.

**One-time provisioning command:**
```bash
az group create --name rg-tinyheroes --location westeurope
az deployment group create \
  --resource-group rg-tinyheroes \
  --template-file infra/main.bicep \
  --parameters infra/main.prod.bicepparam \
  --parameters postgresAdminPassword="…" jwtSecret="…" \
               repositoryToken="…" huggingFaceApiKey="…"
```

Infrastructure is deployed manually. Only application code deploys automatically on push.

## CI/CD Pipeline — GitHub Actions

**File:** `.github/workflows/deploy.yml`
**Trigger:** push to `main`
**Jobs:** two parallel jobs

### deploy-frontend
1. `actions/checkout@v4`
2. `actions/setup-node@v4` (Node 22, npm cache)
3. `npm ci` + `npm run build` in `frontend/`
4. `Azure/static-web-apps-deploy@v1` — uploads `dist/frontend/browser/` to SWA

### deploy-api
1. `actions/checkout@v4`
2. `docker/login-action@v3` → ACR
3. `docker/build-push-action@v5` → builds `backend/TinyHeroes.Api/Dockerfile`, pushes `tinyheroes-api:latest`
4. `azure/webapps-deploy@v3` → tells App Service to pull new image

### Required GitHub Secrets

| Secret | Source |
|---|---|
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | SWA portal → Manage deployment token |
| `ACR_LOGIN_SERVER` | ACR → Login server (e.g. `tinyheroes.azurecr.io`) |
| `ACR_USERNAME` | ACR → Access keys → Username |
| `ACR_PASSWORD` | ACR → Access keys → Password |
| `AZURE_APP_SERVICE_NAME` | App Service resource name |
| `AZURE_WEBAPP_PUBLISH_PROFILE` | App Service → Get publish profile (full XML) |

## Code Changes

### 1. `frontend/src/environments/environment.prod.ts`
Change `apiUrl` from `'/api'` to `'https://api.tinyheroes.czibik.hu'`.

### 2. `frontend/public/staticwebapps.config.json` (new)
```json
{
  "navigationFallback": {
    "rewrite": "/index.html",
    "exclude": ["/assets/*"]
  }
}
```
Without this, any direct URL (e.g. `/podium/this-week`) returns 404 on SWA.

### 3. `backend/TinyHeroes.Infrastructure/Services/AzureBlobStorageService.cs` (new)
Implement `IFileStorageService` using `Azure.Storage.Blobs` NuGet package.
Connection string format: `azureblob://AccountName=…;AccountKey=…;ContainerName=…`
Returns the public blob URL after upload.

### 4. `backend/TinyHeroes.Infrastructure/DependencyInjection.cs`
Pick implementation based on connection string prefix:
```csharp
var storageConn = config["Storage:ConnectionString"] ?? "";
if (storageConn.StartsWith("azureblob://"))
    services.AddScoped<IFileStorageService, AzureBlobStorageService>();
else
    services.AddScoped<IFileStorageService, LocalFileStorageService>();
```

CORS (`AllowedOrigins`) is already config-driven — no code change needed, only an App Service
environment variable: `AllowedOrigins__0=https://tinyheroes.czibik.hu`.

Nginx container and `nginx.conf` remain in the repo for local dev but are not used in production.

## App Service Environment Variables (set via Bicep)

| Key | Value |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__Default` | `Host=<pg-fqdn>;Database=tinyheroes;Username=tinyheroes;Password=…;SslMode=Require` |
| `Jwt__Secret` | (Bicep parameter) |
| `Jwt__Issuer` | `tinyheroes-api` |
| `Jwt__Audience` | `tinyheroes-frontend` |
| `Jwt__ExpiryMinutes` | `60` |
| `Storage__ConnectionString` | `azureblob://AccountName=…;AccountKey=…;ContainerName=uploads` |
| `AiImage__HuggingFace__ApiKey` | (Bicep parameter) |
| `AiImage__HuggingFace__Model` | `black-forest-labs/FLUX.1-schnell` |
| `AllowedOrigins__0` | `https://tinyheroes.czibik.hu` |
| `Auth__Google__ClientId` | (optional) |
| `Auth__Google__ClientSecret` | (optional) |
| `Auth__Facebook__AppId` | (optional) |
| `Auth__Facebook__AppSecret` | (optional) |

## DNS Configuration

Add two CNAME records on the czibik.hu nameserver:
```
tinyheroes.czibik.hu     CNAME  <swa-name>.azurestaticapps.net
api.tinyheroes.czibik.hu CNAME  <appservice-name>.azurewebsites.net
```
Then verify custom domains in each Azure portal resource. SSL certificates auto-provision.

## Scaling Path (when opening to other families)

- **Database:** upgrade Postgres tier (B1ms → B2ms or General Purpose)
- **API:** upgrade App Service Plan (B1 → B2/P1) or migrate to Container Apps with `minReplicas: 1`
- **Storage:** no change needed (Blob scales automatically)
- **Frontend:** no change needed (SWA Standard plan if API linking needed later)
- **Secrets:** add Key Vault and switch App Service to managed identity

## Verification

1. `az deployment group create …` completes — 5 resources visible in portal
2. Push a commit to `main` → both GitHub Actions jobs pass
3. `https://tinyheroes.czibik.hu` loads the Angular app
4. Login with `testuser@demo.com / Password1!` — JWT auth works
5. Create a deed — image generates via HuggingFace, uploads to Blob
6. Image URL in response is a Blob Storage URL (not local path)
