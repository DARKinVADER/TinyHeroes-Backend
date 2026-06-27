# Azure Hosting & CI/CD Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deploy TinyHeroes to Azure under `tinyheroes.czibik.hu` (frontend) and `api.tinyheroes.czibik.hu` (API) with automatic deployment on every push to `main`.

**Architecture:** Angular SPA on Azure Static Web Apps (Free tier), .NET 10 API as a Docker container on Azure App Service B1, PostgreSQL Flexible Server for the database, Azure Blob Storage for uploaded images. Infrastructure provisioned once via Bicep; application code re-deployed automatically via GitHub Actions on every push to `main`.

**Tech Stack:** Bicep (IaC), GitHub Actions (CI/CD), `Azure.Storage.Blobs` SDK (.NET), xunit + Moq + FluentAssertions (tests), Azure CLI for provisioning.

---

## File Map

| Action | Path |
|---|---|
| Create | `infra/main.bicep` |
| Create | `infra/main.prod.bicepparam` |
| Create | `infra/modules/acr.bicep` |
| Create | `infra/modules/postgres.bicep` |
| Create | `infra/modules/storage.bicep` |
| Create | `infra/modules/appservice.bicep` |
| Create | `infra/modules/swa.bicep` |
| Create | `.github/workflows/deploy.yml` |
| Modify | `frontend/src/environments/environment.prod.ts` |
| Create | `frontend/public/staticwebapps.config.json` |
| Modify | `backend/TinyHeroes.Infrastructure/TinyHeroes.Infrastructure.csproj` |
| Create | `backend/TinyHeroes.Infrastructure/Services/AzureBlobStorageService.cs` |
| Modify | `backend/TinyHeroes.Infrastructure/DependencyInjection.cs` |
| Create | `backend/TinyHeroes.Tests/Unit/AzureBlobStorageServiceTests.cs` |

---

## Task 1: AzureBlobStorageService — add NuGet package

**Files:**
- Modify: `backend/TinyHeroes.Infrastructure/TinyHeroes.Infrastructure.csproj`

- [ ] **Step 1: Add Azure.Storage.Blobs package**

Open `backend/TinyHeroes.Infrastructure/TinyHeroes.Infrastructure.csproj` and add inside the existing `<ItemGroup>` with packages:

```xml
<PackageReference Include="Azure.Storage.Blobs" Version="12.*" />
```

- [ ] **Step 2: Restore to confirm the package resolves**

```bash
cd backend && dotnet restore TinyHeroes.Infrastructure
```

Expected: output ends with `Restore succeeded.`

---

## Task 2: AzureBlobStorageService — write failing tests first

**Files:**
- Create: `backend/TinyHeroes.Tests/Unit/AzureBlobStorageServiceTests.cs`

The connection string format used throughout this plan is:
`azureblob://AccountName=myaccount;AccountKey=mykey==;ContainerName=uploads`

- [ ] **Step 1: Create the test file**

```csharp
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TinyHeroes.Application.Interfaces;
using TinyHeroes.Infrastructure;
using TinyHeroes.Infrastructure.Services;

namespace TinyHeroes.Tests.Unit;

public class AzureBlobStorageServiceTests
{
    [Fact]
    public void ParseConnectionString_ExtractsAccountName()
    {
        var cs = "azureblob://AccountName=testaccount;AccountKey=dGVzdA==;ContainerName=uploads";
        var parsed = AzureBlobStorageService.ParseConnectionString(cs);
        parsed.AccountName.Should().Be("testaccount");
    }

    [Fact]
    public void ParseConnectionString_ExtractsAccountKey()
    {
        var cs = "azureblob://AccountName=testaccount;AccountKey=dGVzdA==;ContainerName=uploads";
        var parsed = AzureBlobStorageService.ParseConnectionString(cs);
        parsed.AccountKey.Should().Be("dGVzdA==");
    }

    [Fact]
    public void ParseConnectionString_ExtractsContainerName()
    {
        var cs = "azureblob://AccountName=testaccount;AccountKey=dGVzdA==;ContainerName=uploads";
        var parsed = AzureBlobStorageService.ParseConnectionString(cs);
        parsed.ContainerName.Should().Be("uploads");
    }

    [Fact]
    public void ParseConnectionString_ThrowsOnMissingAccountName()
    {
        var cs = "azureblob://AccountKey=dGVzdA==;ContainerName=uploads";
        var act = () => AzureBlobStorageService.ParseConnectionString(cs);
        act.Should().Throw<InvalidOperationException>().WithMessage("*AccountName*");
    }

    [Theory]
    [InlineData("azureblob://AccountName=a;AccountKey=b;ContainerName=uploads", typeof(AzureBlobStorageService))]
    [InlineData("disk://path=/app/uploads", typeof(LocalFileStorageService))]
    public void DependencyInjection_RegistersCorrectImplementation(string connectionString, Type expectedType)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:ConnectionString"] = connectionString,
                ["ConnectionStrings:Default"] = "Host=localhost;Database=test",
                ["Jwt:Secret"] = "test-secret-that-is-long-enough-32chars",
                ["Jwt:Issuer"] = "test",
                ["Jwt:Audience"] = "test",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient();
        services.AddInfrastructure(config);

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IFileStorageService>();
        service.Should().BeOfType(expectedType);
    }
}
```

- [ ] **Step 2: Run tests — verify they fail with the right reason**

```bash
cd backend && dotnet test --filter "FullyQualifiedName~AzureBlobStorageServiceTests" --no-build 2>&1 | tail -20
```

Expected: compile error — `AzureBlobStorageService` does not exist yet.

---

## Task 3: AzureBlobStorageService — implement

**Files:**
- Create: `backend/TinyHeroes.Infrastructure/Services/AzureBlobStorageService.cs`

- [ ] **Step 1: Create the implementation**

```csharp
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using TinyHeroes.Application.Interfaces;

namespace TinyHeroes.Infrastructure.Services;

public class AzureBlobStorageService(IConfiguration config) : IFileStorageService
{
    public record BlobConnectionInfo(string AccountName, string AccountKey, string ContainerName);

    public async Task<string> SaveAsync(Stream content, string subPath, string fileName, CancellationToken ct = default)
    {
        var info = ParseConnectionString(config["Storage:ConnectionString"]);
        var container = CreateContainerClient(info);
        var blobName = $"{subPath}/{fileName}";
        var blob = container.GetBlobClient(blobName);
        await blob.UploadAsync(content, new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders() }, ct);
        return $"https://{info.AccountName}.blob.core.windows.net/{info.ContainerName}/{blobName}";
    }

    public void Delete(string subPath, string fileName)
    {
        var info = ParseConnectionString(config["Storage:ConnectionString"]);
        var container = CreateContainerClient(info);
        container.GetBlobClient($"{subPath}/{fileName}").DeleteIfExists();
    }

    public static BlobConnectionInfo ParseConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Storage:ConnectionString is not configured.");

        var body = connectionString.StartsWith("azureblob://", StringComparison.OrdinalIgnoreCase)
            ? connectionString["azureblob://".Length..]
            : connectionString;

        var parts = body.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split('=', 2))
            .Where(p => p.Length == 2)
            .ToDictionary(p => p[0].Trim(), p => p[1].Trim(), StringComparer.OrdinalIgnoreCase);

        if (!parts.TryGetValue("AccountName", out var accountName) || string.IsNullOrEmpty(accountName))
            throw new InvalidOperationException("Storage connection string is missing AccountName.");
        if (!parts.TryGetValue("AccountKey", out var accountKey) || string.IsNullOrEmpty(accountKey))
            throw new InvalidOperationException("Storage connection string is missing AccountKey.");
        if (!parts.TryGetValue("ContainerName", out var containerName) || string.IsNullOrEmpty(containerName))
            throw new InvalidOperationException("Storage connection string is missing ContainerName.");

        return new BlobConnectionInfo(accountName, accountKey, containerName);
    }

    private static BlobContainerClient CreateContainerClient(BlobConnectionInfo info)
    {
        var serviceUri = new Uri($"https://{info.AccountName}.blob.core.windows.net");
        var credential = new Azure.Storage.StorageSharedKeyCredential(info.AccountName, info.AccountKey);
        return new BlobServiceClient(serviceUri, credential).GetBlobContainerClient(info.ContainerName);
    }
}
```

- [ ] **Step 2: Run the unit tests — verify they pass**

```bash
cd backend && dotnet test --filter "FullyQualifiedName~AzureBlobStorageServiceTests" -v normal 2>&1 | tail -20
```

Expected: `Passed: 5, Failed: 0`

---

## Task 4: Update DependencyInjection to select storage by prefix

**Files:**
- Modify: `backend/TinyHeroes.Infrastructure/DependencyInjection.cs`

- [ ] **Step 1: Replace the hard-coded `LocalFileStorageService` registration**

Replace this line:
```csharp
services.AddScoped<IFileStorageService, LocalFileStorageService>();
```

With:
```csharp
var storageConn = config["Storage:ConnectionString"] ?? "";
if (storageConn.StartsWith("azureblob://", StringComparison.OrdinalIgnoreCase))
    services.AddScoped<IFileStorageService, AzureBlobStorageService>();
else
    services.AddScoped<IFileStorageService, LocalFileStorageService>();
```

- [ ] **Step 2: Run all backend tests to confirm nothing is broken**

```bash
cd backend && dotnet test 2>&1 | tail -10
```

Expected: all tests pass (DI test from Task 2 now passes too).

- [ ] **Step 3: Commit**

```bash
git add backend/TinyHeroes.Infrastructure/TinyHeroes.Infrastructure.csproj \
        backend/TinyHeroes.Infrastructure/Services/AzureBlobStorageService.cs \
        backend/TinyHeroes.Infrastructure/DependencyInjection.cs \
        backend/TinyHeroes.Tests/Unit/AzureBlobStorageServiceTests.cs
git commit -m "feat: add AzureBlobStorageService with prefix-based DI selection"
```

---

## Task 5: Frontend — production API URL and SWA routing config

**Files:**
- Modify: `frontend/src/environments/environment.prod.ts`
- Create: `frontend/public/staticwebapps.config.json`

- [ ] **Step 1: Update Angular production environment**

Replace the entire content of `frontend/src/environments/environment.prod.ts`:

```typescript
export const environment = {
  production: true,
  apiUrl: 'https://api.tinyheroes.czibik.hu',
};
```

- [ ] **Step 2: Create the SWA routing fallback config**

Create `frontend/public/staticwebapps.config.json`:

```json
{
  "navigationFallback": {
    "rewrite": "/index.html",
    "exclude": ["/assets/*"]
  }
}
```

This tells Azure Static Web Apps to serve `index.html` for any unmatched route (Angular handles routing client-side). Without it, navigating directly to `/podium/this-week` returns 404.

- [ ] **Step 3: Verify a production build completes**

```bash
cd frontend && npx ng build --configuration production 2>&1 | tail -5
```

Expected: `Application bundle generation complete.` with no errors. Check that `dist/frontend/browser/staticwebapps.config.json` exists in the output.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/environments/environment.prod.ts \
        frontend/public/staticwebapps.config.json
git commit -m "feat: configure frontend for Azure production deployment"
```

---

## Task 6: Bicep — ACR module

**Files:**
- Create: `infra/modules/acr.bicep`

- [ ] **Step 1: Create the directory and file**

```bash
mkdir -p /path/to/repo/infra/modules
```

Create `infra/modules/acr.bicep`:

```bicep
param name string
param location string

resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: name
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: true
  }
}

output loginServer string = acr.properties.loginServer
output adminUsername string = acr.name

@secure()
output adminPassword string = listCredentials(acr.id, '2023-07-01').passwords[0].value
```

- [ ] **Step 2: Validate the Bicep file**

```bash
az bicep build --file infra/modules/acr.bicep
```

Expected: no errors, generates `infra/modules/acr.json` (delete it after — it's just a validation artifact).

```bash
rm infra/modules/acr.json
```

---

## Task 7: Bicep — PostgreSQL Flexible Server module

**Files:**
- Create: `infra/modules/postgres.bicep`

- [ ] **Step 1: Create the file**

```bicep
param name string
param location string

@secure()
param adminPassword string

param administratorLogin string = 'tinyheroes'

resource postgres 'Microsoft.DBforPostgreSQL/flexibleServers@2023-06-01-preview' = {
  name: name
  location: location
  sku: {
    name: 'Standard_B1ms'
    tier: 'Burstable'
  }
  properties: {
    administratorLogin: administratorLogin
    administratorLoginPassword: adminPassword
    version: '16'
    storage: {
      storageSizeGB: 32
    }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
    highAvailability: {
      mode: 'Disabled'
    }
  }
}

resource allowAzureServices 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2023-06-01-preview' = {
  parent: postgres
  name: 'AllowAllAzureIPs'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource database 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-06-01-preview' = {
  parent: postgres
  name: 'tinyheroes'
}

output fqdn string = postgres.properties.fullyQualifiedDomainName
output administratorLogin string = administratorLogin
```

- [ ] **Step 2: Validate**

```bash
az bicep build --file infra/modules/postgres.bicep && rm infra/modules/postgres.json
```

Expected: no errors.

---

## Task 8: Bicep — Storage Account module

**Files:**
- Create: `infra/modules/storage.bicep`

- [ ] **Step 1: Create the file**

```bicep
param name string
param location string

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: name
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: true
    minimumTlsVersion: 'TLS1_2'
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-01-01' = {
  parent: storageAccount
  name: 'default'
}

resource uploadsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  parent: blobService
  name: 'uploads'
  properties: {
    publicAccess: 'Blob'
  }
}

output accountName string = storageAccount.name

@secure()
output primaryKey string = storageAccount.listKeys().keys[0].value

output connectionString string = 'azureblob://AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};ContainerName=uploads'
```

- [ ] **Step 2: Validate**

```bash
az bicep build --file infra/modules/storage.bicep && rm infra/modules/storage.json
```

Expected: no errors.

---

## Task 9: Bicep — App Service module

**Files:**
- Create: `infra/modules/appservice.bicep`

- [ ] **Step 1: Create the file**

```bicep
param name string
param location string
param acrLoginServer string
param acrUsername string

@secure()
param acrPassword string

param postgresConnectionString string

@secure()
param jwtSecret string

param storageConnectionString string

@secure()
param huggingFaceApiKey string

param allowedOrigin string

resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: '${name}-plan'
  location: location
  kind: 'Linux'
  properties: {
    reserved: true
  }
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
}

resource webApp 'Microsoft.Web/sites@2023-01-01' = {
  name: name
  location: location
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOCKER|${acrLoginServer}/tinyheroes-api:latest'
      appSettings: [
        { name: 'DOCKER_REGISTRY_SERVER_URL', value: 'https://${acrLoginServer}' }
        { name: 'DOCKER_REGISTRY_SERVER_USERNAME', value: acrUsername }
        { name: 'DOCKER_REGISTRY_SERVER_PASSWORD', value: acrPassword }
        { name: 'WEBSITES_ENABLE_APP_SERVICE_STORAGE', value: 'false' }
        { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
        { name: 'ConnectionStrings__Default', value: postgresConnectionString }
        { name: 'Jwt__Secret', value: jwtSecret }
        { name: 'Jwt__Issuer', value: 'tinyheroes-api' }
        { name: 'Jwt__Audience', value: 'tinyheroes-frontend' }
        { name: 'Jwt__ExpiryMinutes', value: '60' }
        { name: 'Storage__ConnectionString', value: storageConnectionString }
        { name: 'AiImage__HuggingFace__ApiKey', value: huggingFaceApiKey }
        { name: 'AiImage__HuggingFace__Model', value: 'black-forest-labs/FLUX.1-schnell' }
        { name: 'AllowedOrigins__0', value: allowedOrigin }
      ]
    }
  }
}

output hostname string = webApp.properties.defaultHostName
output name string = webApp.name
```

- [ ] **Step 2: Validate**

```bash
az bicep build --file infra/modules/appservice.bicep && rm infra/modules/appservice.json
```

Expected: no errors.

---

## Task 10: Bicep — Static Web Apps module

**Files:**
- Create: `infra/modules/swa.bicep`

- [ ] **Step 1: Create the file**

```bicep
param name string
param location string
param repositoryUrl string
param branch string = 'main'

@secure()
param repositoryToken string

resource swa 'Microsoft.Web/staticSites@2023-01-01' = {
  name: name
  location: location
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    repositoryUrl: repositoryUrl
    branch: branch
    repositoryToken: repositoryToken
    buildProperties: {
      appLocation: 'frontend'
      outputLocation: 'dist/frontend/browser'
      skipGithubActionWorkflowGeneration: true
    }
  }
}

output hostname string = swa.properties.defaultHostname

@secure()
output deploymentToken string = swa.listSecrets().properties.apiKey
```

Note: `skipGithubActionWorkflowGeneration: true` because we write our own workflow in Task 12.

- [ ] **Step 2: Validate**

```bash
az bicep build --file infra/modules/swa.bicep && rm infra/modules/swa.json
```

Expected: no errors.

---

## Task 11: Bicep — main orchestrator and parameters file

**Files:**
- Create: `infra/main.bicep`
- Create: `infra/main.prod.bicepparam`

- [ ] **Step 1: Create `infra/main.bicep`**

```bicep
param location string = 'westeurope'
param prefix string = 'tinyheroes'
param repositoryUrl string

@secure()
param postgresAdminPassword string

@secure()
param jwtSecret string

@secure()
param repositoryToken string

@secure()
param huggingFaceApiKey string

module acr 'modules/acr.bicep' = {
  name: 'acr'
  params: {
    name: '${prefix}acr'
    location: location
  }
}

module postgres 'modules/postgres.bicep' = {
  name: 'postgres'
  params: {
    name: '${prefix}-pg'
    location: location
    adminPassword: postgresAdminPassword
  }
}

module storage 'modules/storage.bicep' = {
  name: 'storage'
  params: {
    name: '${prefix}stor'
    location: location
  }
}

module appservice 'modules/appservice.bicep' = {
  name: 'appservice'
  params: {
    name: '${prefix}-api'
    location: location
    acrLoginServer: acr.outputs.loginServer
    acrUsername: acr.outputs.adminUsername
    acrPassword: acr.outputs.adminPassword
    postgresConnectionString: 'Host=${postgres.outputs.fqdn};Database=tinyheroes;Username=${postgres.outputs.administratorLogin};Password=${postgresAdminPassword};SslMode=Require'
    jwtSecret: jwtSecret
    storageConnectionString: storage.outputs.connectionString
    huggingFaceApiKey: huggingFaceApiKey
    allowedOrigin: 'https://tinyheroes.czibik.hu'
  }
}

module swa 'modules/swa.bicep' = {
  name: 'swa'
  params: {
    name: '${prefix}-frontend'
    location: location
    repositoryUrl: repositoryUrl
    repositoryToken: repositoryToken
  }
}

output acrLoginServer string = acr.outputs.loginServer
output acrUsername string = acr.outputs.adminUsername
output appServiceHostname string = appservice.outputs.hostname
output appServiceName string = appservice.outputs.name
output swaHostname string = swa.outputs.hostname
output postgresFqdn string = postgres.outputs.fqdn
```

- [ ] **Step 2: Create `infra/main.prod.bicepparam`**

```bicep
using './main.bicep'

param location = 'westeurope'
param prefix = 'tinyheroes'
param repositoryUrl = 'https://github.com/YOUR_GITHUB_USERNAME/TinyHeroes'

// Secrets are passed via CLI --parameters flags — never committed here:
// postgresAdminPassword, jwtSecret, repositoryToken, huggingFaceApiKey
```

Replace `YOUR_GITHUB_USERNAME` with the actual GitHub username.

- [ ] **Step 3: Validate the full template**

```bash
az bicep build --file infra/main.bicep && rm infra/main.json
```

Expected: no errors.

- [ ] **Step 4: Commit all Bicep files**

```bash
git add infra/
git commit -m "feat: add Bicep IaC for Azure infrastructure"
```

---

## Task 12: GitHub Actions deployment workflow

**Files:**
- Create: `.github/workflows/deploy.yml`

- [ ] **Step 1: Create the directory and file**

```bash
mkdir -p .github/workflows
```

Create `.github/workflows/deploy.yml`:

```yaml
name: Deploy to Azure

on:
  push:
    branches: [main]

jobs:
  deploy-frontend:
    name: Frontend → Static Web Apps
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-node@v4
        with:
          node-version: '22'
          cache: npm
          cache-dependency-path: frontend/package-lock.json

      - name: Install dependencies
        run: npm ci
        working-directory: frontend

      - name: Build
        run: npm run build
        working-directory: frontend

      - name: Deploy to Static Web Apps
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
          action: upload
          app_location: frontend
          output_location: dist/frontend/browser
          skip_app_build: true

  deploy-api:
    name: Backend → App Service
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Log in to Azure Container Registry
        uses: docker/login-action@v3
        with:
          registry: ${{ secrets.ACR_LOGIN_SERVER }}
          username: ${{ secrets.ACR_USERNAME }}
          password: ${{ secrets.ACR_PASSWORD }}

      - name: Build and push Docker image
        uses: docker/build-push-action@v5
        with:
          context: backend
          file: backend/TinyHeroes.Api/Dockerfile
          push: true
          tags: ${{ secrets.ACR_LOGIN_SERVER }}/tinyheroes-api:latest

      - name: Deploy to App Service
        uses: azure/webapps-deploy@v3
        with:
          app-name: ${{ secrets.AZURE_APP_SERVICE_NAME }}
          publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
          images: ${{ secrets.ACR_LOGIN_SERVER }}/tinyheroes-api:latest
```

- [ ] **Step 2: Validate YAML syntax**

```bash
python3 -c "import yaml; yaml.safe_load(open('.github/workflows/deploy.yml'))" && echo "YAML valid"
```

Expected: `YAML valid`

- [ ] **Step 3: Commit**

```bash
git add .github/workflows/deploy.yml
git commit -m "feat: add GitHub Actions CI/CD pipeline for Azure deployment"
```

---

## Task 13: Provision Azure resources (one-time manual step)

This task is a human operator task — run once after all code is merged to `main`.

- [ ] **Step 1: Log in to Azure**

```bash
az login
az account set --subscription "<your-subscription-id>"
```

- [ ] **Step 2: Create the resource group**

```bash
az group create --name rg-tinyheroes --location westeurope
```

- [ ] **Step 3: Deploy the Bicep template**

```bash
az deployment group create \
  --resource-group rg-tinyheroes \
  --template-file infra/main.bicep \
  --parameters infra/main.prod.bicepparam \
  --parameters postgresAdminPassword="<strong-password>" \
               jwtSecret="<32-char-minimum-random-string>" \
               repositoryToken="<github-pat-with-repo-scope>" \
               huggingFaceApiKey="<huggingface-api-key>"
```

The command outputs `acrLoginServer`, `appServiceName`, `swaHostname`, `postgresFqdn` — save these.

- [ ] **Step 4: Set GitHub Secrets**

In the GitHub repo → Settings → Secrets and variables → Actions, add:

| Secret | Value |
|---|---|
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | SWA portal → Overview → Manage deployment token |
| `ACR_LOGIN_SERVER` | from Bicep output `acrLoginServer` |
| `ACR_USERNAME` | from Bicep output `acrUsername` |
| `ACR_PASSWORD` | ACR portal → Access keys → Password |
| `AZURE_APP_SERVICE_NAME` | from Bicep output `appServiceName` |
| `AZURE_WEBAPP_PUBLISH_PROFILE` | App Service portal → Overview → Get publish profile → paste full XML |

- [ ] **Step 5: Add DNS CNAME records on czibik.hu**

At your DNS provider for czibik.hu, add:

```
tinyheroes     CNAME  <value from swaHostname output>.azurestaticapps.net
api.tinyheroes CNAME  <value from appServiceHostname output>
```

- [ ] **Step 6: Verify custom domains in Azure portal**

- SWA portal → Custom domains → Add → `tinyheroes.czibik.hu` → validate → Azure issues SSL cert
- App Service portal → Custom domains → Add → `api.tinyheroes.czibik.hu` → validate → bind SSL cert

---

## Task 14: End-to-end verification

- [ ] **Step 1: Trigger a deployment by pushing to main**

```bash
git push origin main
```

Watch GitHub Actions (repo → Actions tab) — both jobs should go green within ~5 minutes.

- [ ] **Step 2: Check the Angular app loads**

Open `https://tinyheroes.czibik.hu` in a browser. The app should load.

- [ ] **Step 3: Verify API authentication**

Log in with `testuser@demo.com / Password1!`. If login succeeds, the API is reachable and the database is connected.

- [ ] **Step 4: Verify Blob Storage**

Create a deed. If the deed image appears and its URL starts with `https://<storageaccount>.blob.core.windows.net/uploads/`, Blob Storage is working.

- [ ] **Step 5: Verify deep linking**

Navigate directly to `https://tinyheroes.czibik.hu/podium/this-week`. It should load the Angular app (not a 404), confirming the SWA routing fallback config works.
