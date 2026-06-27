# Backend Structured Logging (Serilog) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace ASP.NET's built-in unstructured logging with Serilog — structured JSON output, per-environment sinks, request/user enrichment, fix for silent exception swallowing, and a runtime log-level switch controllable via an API endpoint.

**Architecture:** Serilog is wired into the ASP.NET host via `UseSerilog`. A `LoggingLevelSwitch` singleton controls the minimum level at runtime (starts at `Warning` in production, `Information` in dev); a protected `POST /api/admin/log-level` endpoint mutates it on the fly without restarting the process. Dev → Console + Seq. Integration and Production → Console + Azure Log Analytics, writing to different tables (`TinyHeroesApiIntegration_CL` vs `TinyHeroesApi_CL`) in the same workspace. Grafana Cloud connects to that workspace via the Azure Monitor data source for dashboards — no extra ingestion pipeline needed. The `ILogger<T>` abstraction is preserved throughout; no test changes needed for the logging infrastructure.

**Tech Stack:** Serilog.AspNetCore 10.0.0, Serilog.Sinks.Seq 9.1.0, Serilog.Sinks.AzureLogAnalytics 7.0.0, Serilog.Enrichers.Environment 3.0.1, Serilog.Enrichers.Thread 4.0.0, Seq (local dev only), Grafana Cloud (Azure Monitor data source — read-only, no extra ingestion)

---

## Sink Decision — Choose Before Implementing

The plan is written for **Azure Log Analytics + Grafana dashboards**. Three free alternatives are listed. **Pick one before Task 2** and substitute wherever `AzureLogAnalytics` appears.

### Option A — Azure Log Analytics + Grafana Cloud dashboards (default in this plan)
- **Package:** `Serilog.Sinks.AzureLogAnalytics` `7.0.0`
- **Important — v7 uses the new Azure Logs Ingestion API (DCE/DCR)**, not the old workspace key pair. This requires more one-time Azure setup (see Task 2 Step 3), but the old HTTP Data Collector API used by abandoned packages is deprecated by Microsoft.
- **Cost:** ~$2.30/GB ingested, 31-day retention free. Grafana Cloud free tier (3 users) is used only for dashboards — it never ingests log data, so the 50 GB/month Grafana limit is untouched.
- **Why choose it:** Already on Azure App Service; 31-day retention; tight Application Insights correlation when OpenTelemetry (issue #12) is added; KQL is powerful for structured queries. Grafana Cloud adds a better dashboard builder than Azure Workbooks.
- **Azure pre-setup (one-time, before implementing):**
  1. Azure Portal → Entra ID → App Registrations → New registration → note **Tenant ID**, **Client ID**
  2. Under the registration → Certificates & secrets → New client secret → note **Client Secret**
  3. Azure Portal → Monitor → Data Collection Endpoints → Create (one endpoint, used by both envs)
  4. Azure Portal → Monitor → Data Collection Rules → Create → define a Custom Log table stream → link to the endpoint → note the **Immutable ID** and **Stream Name**
  5. Assign `Monitoring Metrics Publisher` role to the App Registration on the DCR
  6. Log Analytics Workspace → Tables → Custom log table → note the table name (becomes `<tableName>_CL`)
- **Grafana dashboard setup (one-time, after logs are flowing):**
  1. Azure Portal → Entra ID → App Registrations → same or new registration → assign `Log Analytics Reader` role on the workspace
  2. Grafana Cloud → Connections → Add data source → Azure Monitor → paste Tenant ID, Client ID, Client Secret
  3. Create panels with KQL, e.g.: `TinyHeroesApi_CL | where Level_s == "Error" | summarize count() by bin(TimeGenerated, 5m)`
  4. Cross-environment panel: `union TinyHeroesApi_CL, TinyHeroesApiIntegration_CL | extend env = iif(Type == "TinyHeroesApi_CL", "prod", "integration")`

### Option B — Grafana Cloud (Loki) — free, cloud, no Azure required
- **Package:** `Serilog.Sinks.Grafana.Loki` `8.3.2`
- **Free tier:** 50 GB/month total telemetry · 14-day retention · 3 users — no credit card.
- **Differentiation:** `Args.labels` with `[{ "key": "environment", "value": "integration" }]` vs `"production"`. LogQL: `{app="tinyheroes-api", environment="integration"}`.

### Option C — Better Stack (Logtail) — free, cloud, minimal setup
- **Package:** `BetterStack.Logs.Serilog` `1.2.0`
- **Free tier:** 3 GB/month · 3-day retention. Tight — viable only for low-traffic apps.
- **Differentiation:** Separate source token per environment; Better Stack auto-tags logs by source.
- **Note:** Registered in code (`WriteTo.BetterStack("TOKEN")`), not via JSON sink name.

### Option D — SigNoz Community (OpenTelemetry) — free, self-hosted
- **Package:** `Serilog.Sinks.OpenTelemetry` `4.2.0`
- **Free tier:** Unlimited (self-host); requires ~4 GB RAM.
- **Differentiation:** `Args.resourceAttributes` `["deployment.environment=integration"]` vs `"production"`.

---

## File Map

| File | Action | What changes |
|---|---|---|
| `backend/TinyHeroes.Api/TinyHeroes.Api.csproj` | Modify | Add 5 Serilog NuGet packages (Serilog.AspNetCore 10.0.0 already pulls Console transitively) |
| `backend/TinyHeroes.Api/appsettings.json` | Modify | Replace `Logging` with `Serilog` (WriteTo only; level in code) |
| `backend/TinyHeroes.Api/appsettings.Development.json` | Modify | Serilog: Console + Seq; `InitialMinimumLevel: Information` |
| `backend/TinyHeroes.Api/appsettings.Integration.json` | Create | Serilog: Console + Azure Logs Ingestion sink, streamName `Custom-TinyHeroesApiIntegration` |
| `backend/TinyHeroes.Api/appsettings.Production.json` | Create | Serilog: Console + Azure Logs Ingestion sink, streamName `Custom-TinyHeroesApi` |
| `backend/TinyHeroes.Api/Program.cs` | Modify | Create `LoggingLevelSwitch`; `UseSerilog`; `UseSerilogRequestLogging` |
| `backend/TinyHeroes.Api/Middleware/ExceptionHandlingMiddleware.cs` | Modify | Inject `ILogger`; log unhandled exceptions |
| `backend/TinyHeroes.Api/Controllers/AdminController.cs` | Create | `GET/POST /api/admin/log-level` |
| `backend/TinyHeroes.Tests/Integration/AdminControllerTests.cs` | Create | Integration tests for log level endpoint |
| `docker-compose.yml` | Modify | Add `seq` service and `seq_data` volume |
| `.env.example` | Modify | Add `SEQ_URL` and `AZURE_LOG_ANALYTICS_*` vars |
| `CHANGELOG.md` | Modify | Entry under `## [Unreleased]` |

---

## Task 1: Add NuGet packages

**Files:**
- Modify: `backend/TinyHeroes.Api/TinyHeroes.Api.csproj`

- [ ] **Step 1: Add the Serilog packages inside the existing `<ItemGroup>`**

Open `backend/TinyHeroes.Api/TinyHeroes.Api.csproj`. After the `Scalar.AspNetCore` line, add:

```xml
<PackageReference Include="Serilog.AspNetCore" Version="10.0.0" />
<PackageReference Include="Serilog.Sinks.Seq" Version="9.1.0" />
<PackageReference Include="Serilog.Enrichers.Environment" Version="3.0.1" />
<PackageReference Include="Serilog.Enrichers.Thread" Version="4.0.0" />
<PackageReference Include="Serilog.Sinks.AzureLogAnalytics" Version="7.0.0" />
```

`Serilog.Sinks.Console` 6.1.1 is pulled in transitively by `Serilog.AspNetCore` 10.0.0 — no explicit entry needed.

If you chose Option B, replace the last line with `<PackageReference Include="Serilog.Sinks.Grafana.Loki" Version="8.3.2" />`.
If Option C: `<PackageReference Include="BetterStack.Logs.Serilog" Version="1.2.0" />`.
If Option D: `<PackageReference Include="Serilog.Sinks.OpenTelemetry" Version="4.2.0" />`.

- [ ] **Step 2: Restore packages**

```bash
cd backend && dotnet restore TinyHeroes.Api/TinyHeroes.Api.csproj
```

Expected: no errors; the five Serilog packages listed in the restore output.

- [ ] **Step 3: Build**

```bash
cd backend && dotnet build TinyHeroes.Api/TinyHeroes.Api.csproj
```

Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 4: Commit**

```bash
git add backend/TinyHeroes.Api/TinyHeroes.Api.csproj
git commit -m "chore: add Serilog NuGet packages"
```

---

## Task 2: Configure Serilog in appsettings files

**Files:**
- Modify: `backend/TinyHeroes.Api/appsettings.json`
- Modify: `backend/TinyHeroes.Api/appsettings.Development.json`
- Create: `backend/TinyHeroes.Api/appsettings.Integration.json`
- Create: `backend/TinyHeroes.Api/appsettings.Production.json`

**Layering strategy:** `appsettings.json` holds the base `WriteTo` (Console only) and the `InitialMinimumLevel` default (`Warning`). Each environment file overrides `WriteTo` and optionally `InitialMinimumLevel`. The `MinimumLevel` and `Enrich` config is intentionally absent from JSON — those are set in code via `LoggingLevelSwitch` and explicit `.Enrich.*()` calls (Task 3). This avoids a conflict where `ReadFrom.Configuration` would overwrite the `ControlledBy` switch.

Integration and Production share one Azure Log Analytics workspace. The v7 `Serilog.Sinks.AzureLogAnalytics` sink uses the new Logs Ingestion API — it requires a **Data Collection Endpoint (DCE)** and a **Data Collection Rule (DCR)** with a custom log stream. The DCR stream name is what creates the Log Analytics table; use different stream names per environment (`Custom-TinyHeroesApiIntegration` → table `TinyHeroesApiIntegration_CL`, `Custom-TinyHeroesApi` → table `TinyHeroesApi_CL`). Cross-env KQL: `union TinyHeroesApi_CL, TinyHeroesApiIntegration_CL`.

- [ ] **Step 1: Rewrite `appsettings.json`**

Replace the entire file with:

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Database=tinyheroes;Username=tinyheroes;Password=changeme"
  },
  "Jwt": {
    "Secret": "local-dev-secret-key-minimum-32chars!",
    "Issuer": "tinyheroes-api",
    "Audience": "tinyheroes-frontend",
    "ExpiryMinutes": 60
  },
  "Auth": {
    "FrontendUrl": "http://localhost:4200",
    "Google": { "ClientId": "", "ClientSecret": "" },
    "Apple": { "ClientId": "", "ClientSecret": "" },
    "Facebook": { "AppId": "", "AppSecret": "" }
  },
  "Storage": {
    "ConnectionString": "disk://path=./uploads"
  },
  "AiImage": {
    "Provider": "HuggingFace",
    "HuggingFace": {
      "ApiKey": "",
      "Model": "black-forest-labs/FLUX.1-schnell"
    }
  },
  "Serilog": {
    "InitialMinimumLevel": "Warning",
    "WriteTo": [{ "Name": "Console" }]
  },
  "AllowedHosts": "*",
  "AllowedOrigins": [ "http://localhost:4200" ]
}
```

`InitialMinimumLevel` is a custom key — not a native Serilog config key. `Program.cs` reads it explicitly to seed the `LoggingLevelSwitch` (Task 3). It is not passed to `ReadFrom.Configuration`.

- [ ] **Step 2: Rewrite `appsettings.Development.json`**

Replace the entire file with:

```json
{
  "Serilog": {
    "InitialMinimumLevel": "Information",
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "Seq",
        "Args": { "serverUrl": "http://localhost:5341" }
      }
    ]
  },
  "Auth": {
    "FrontendUrl": "http://localhost:4200",
    "Google": {
      "ClientId": "your-client-id.apps.googleusercontent.com",
      "ClientSecret": "GOCSPX-your-secret"
    }
  }
}
```

- [ ] **Step 3: Create `appsettings.Integration.json`**

Create `backend/TinyHeroes.Api/appsettings.Integration.json`:

```json
{
  "Serilog": {
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "AzureLogAnalytics",
        "Args": {
          "credentials": {
            "endpoint": "",
            "immutableId": "",
            "streamName": "Custom-TinyHeroesApiIntegration",
            "tenantId": "",
            "clientId": "",
            "clientSecret": ""
          }
        }
      }
    ]
  }
}
```

All credential fields are empty — injected into the Integration App Service as Application Settings. Add these in Azure Portal → App Service → Configuration → Application Settings:
```
Serilog__WriteTo__1__Args__credentials__endpoint      = <DCE Logs Ingestion URL>
Serilog__WriteTo__1__Args__credentials__immutableId   = <DCR Immutable ID>
Serilog__WriteTo__1__Args__credentials__tenantId      = <Entra Tenant ID>
Serilog__WriteTo__1__Args__credentials__clientId      = <App Registration Client ID>
Serilog__WriteTo__1__Args__credentials__clientSecret  = <App Registration Client Secret>
```
The `streamName` (`Custom-TinyHeroesApiIntegration`) is already set in the file and drives which Log Analytics table receives the logs. It must match the stream name configured in the DCR.

- [ ] **Step 4: Create `appsettings.Production.json`**

Create `backend/TinyHeroes.Api/appsettings.Production.json`:

```json
{
  "Serilog": {
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "AzureLogAnalytics",
        "Args": {
          "credentials": {
            "endpoint": "",
            "immutableId": "",
            "streamName": "Custom-TinyHeroesApi",
            "tenantId": "",
            "clientId": "",
            "clientSecret": ""
          }
        }
      }
    ]
  }
}
```

Same credential structure — injected via Production App Service Application Settings. The only difference from Integration is `streamName: "Custom-TinyHeroesApi"` → this routes to the `TinyHeroesApi_CL` table. The DCE endpoint and DCR immutable ID can be shared across both environments (one DCR with two output streams is valid).

- [ ] **Step 5: Build**

```bash
cd backend && dotnet build TinyHeroes.Api/TinyHeroes.Api.csproj
```

Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 6: Commit**

```bash
git add backend/TinyHeroes.Api/appsettings.json \
        backend/TinyHeroes.Api/appsettings.Development.json \
        backend/TinyHeroes.Api/appsettings.Integration.json \
        backend/TinyHeroes.Api/appsettings.Production.json
git commit -m "feat: configure Serilog sinks in appsettings (dev=Seq, integration+prod=Azure)"
```

---

## Task 3: Wire Serilog into Program.cs

**Files:**
- Modify: `backend/TinyHeroes.Api/Program.cs`

- [ ] **Step 1: Add the `using Serilog;` statement**

At the top of `backend/TinyHeroes.Api/Program.cs`, add alphabetically with the other usings (between `Microsoft.OpenApi` and `Scalar.AspNetCore`):

```csharp
using Serilog;
using Serilog.Events;
```

- [ ] **Step 2: Create the `LoggingLevelSwitch` and register it as a singleton**

Immediately after `var builder = WebApplication.CreateBuilder(args);` (line 21), insert:

```csharp
// Read the initial log level from config (Warning in production, Information in dev).
// The switch can be changed at runtime via POST /api/admin/log-level without restarting.
var initialLevel = Enum.TryParse<LogEventLevel>(
    builder.Configuration["Serilog:InitialMinimumLevel"], out var parsedLevel)
    ? parsedLevel
    : LogEventLevel.Warning;
var levelSwitch = new LoggingLevelSwitch(initialLevel);
builder.Services.AddSingleton(levelSwitch);
```

`LoggingLevelSwitch` is a concrete class from `Serilog.Core` — no interface needed. It is registered as a singleton so `AdminController` (Task 5) can inject and mutate it.

- [ ] **Step 3: Replace the default logging with Serilog**

Directly after the `levelSwitch` block from Step 2, insert:

```csharp
builder.Host.UseSerilog((ctx, cfg) =>
    cfg.MinimumLevel.ControlledBy(levelSwitch)
       .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
       .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
       .Enrich.FromLogContext()
       .Enrich.WithMachineName()
       .Enrich.WithThreadId()
       .ReadFrom.Configuration(ctx.Configuration));
```

`ReadFrom.Configuration` reads only the `WriteTo` array from `appsettings.json` (sinks vary by environment). `MinimumLevel` and `Enrich` are set in code so the `ControlledBy` switch is unambiguous — no conflict with any `MinimumLevel.Default` that `ReadFrom.Configuration` would otherwise apply.

- [ ] **Step 4: Add `UseSerilogRequestLogging` in the pipeline**

Find `app.UseForwardedHeaders();` (currently around line 154 in the original file). Add directly after it, before the database migration block:

```csharp
app.UseForwardedHeaders();

app.UseSerilogRequestLogging(opts =>
{
    opts.EnrichDiagnosticContext = (diagCtx, httpCtx) =>
    {
        diagCtx.Set("RequestId", httpCtx.TraceIdentifier);
        diagCtx.Set("UserId",
            httpCtx.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier) ?? "anon");
        diagCtx.Set("UserAgent", httpCtx.Request.Headers.UserAgent.ToString());
    };
});
```

- [ ] **Step 5: Build**

```bash
cd backend && dotnet build TinyHeroes.Api/TinyHeroes.Api.csproj
```

Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 6: Run all tests**

```bash
cd backend && dotnet test
```

Expected: all tests pass. The `TestWebApplicationFactory` uses `UseEnvironment("Testing")` — no `appsettings.Testing.json` exists, so Serilog gets the base config (Console only at `Warning` level). Tests are unaffected.

- [ ] **Step 7: Commit**

```bash
git add backend/TinyHeroes.Api/Program.cs
git commit -m "feat: wire Serilog with runtime level switch into ASP.NET host"
```

---

## Task 4: Fix ExceptionHandlingMiddleware

**Files:**
- Modify: `backend/TinyHeroes.Api/Middleware/ExceptionHandlingMiddleware.cs`

The current middleware silently swallows all `Exception` instances. This task injects `ILogger` and adds `LogError` for the catch-all (500) branch only. `NotFoundException` and `ForbiddenException` remain silent — they are expected business control flow.

- [ ] **Step 1: Rewrite `ExceptionHandlingMiddleware.cs`**

Replace the entire file with:

```csharp
using System.Text.Json;
using TinyHeroes.Application.Exceptions;

namespace TinyHeroes.Api.Middleware;

public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var (statusCode, message) = ex switch
            {
                NotFoundException e => (StatusCodes.Status404NotFound, e.Message),
                ForbiddenException e => (StatusCodes.Status403Forbidden, e.Message),
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
            };

            if (statusCode == StatusCodes.Status500InternalServerError)
            {
                logger.LogError(ex,
                    "Unhandled exception {Method} {Path} {RequestId}",
                    context.Request.Method,
                    context.Request.Path,
                    context.TraceIdentifier);
            }

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = message }));
        }
    }
}
```

`ILogger<ExceptionHandlingMiddleware>` is resolved from DI automatically when `UseMiddleware<ExceptionHandlingMiddleware>()` is called — no change to `Program.cs` is needed.

- [ ] **Step 2: Build**

```bash
cd backend && dotnet build TinyHeroes.Api/TinyHeroes.Api.csproj
```

Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 3: Run all tests**

```bash
cd backend && dotnet test
```

Expected: all tests pass. The `{ error: "..." }` JSON response shape is unchanged.

- [ ] **Step 4: Commit**

```bash
git add backend/TinyHeroes.Api/Middleware/ExceptionHandlingMiddleware.cs
git commit -m "fix: log unhandled exceptions with stack trace in ExceptionHandlingMiddleware"
```

---

## Task 5: Admin log-level endpoint

**Files:**
- Create: `backend/TinyHeroes.Api/Controllers/AdminController.cs`
- Create: `backend/TinyHeroes.Tests/Integration/AdminControllerTests.cs`

This task adds `GET /api/admin/log-level` and `POST /api/admin/log-level`. Both require authentication (any valid JWT). Changing the log level takes effect immediately for all subsequent log events — no restart needed. The level resets to `InitialMinimumLevel` on the next process restart (e.g. redeploy), which is intentional: you don't want Debug permanently in production.

Valid level values (case-insensitive): `Verbose`, `Debug`, `Information`, `Warning`, `Error`, `Fatal`.

- [ ] **Step 1: Write the failing tests first**

Create `backend/TinyHeroes.Tests/Integration/AdminControllerTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace TinyHeroes.Tests.Integration;

public class AdminControllerTests(TestWebApplicationFactory<Program> factory)
    : IClassFixture<TestWebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOpts = TestWebApplicationFactory<Program>.JsonOptions;

    [Fact]
    public async Task GetLogLevel_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/admin/log-level");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetLogLevel_Authenticated_ReturnsCurrentLevel()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);
        var response = await client.GetAsync("/api/admin/log-level");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = JsonSerializer.Deserialize<JsonElement>(
            await response.Content.ReadAsStringAsync(), JsonOpts);
        var level = body.GetProperty("level").GetString();
        Assert.NotNull(level);
        Assert.True(Enum.TryParse<Serilog.Events.LogEventLevel>(level, out _));
    }

    [Fact]
    public async Task SetLogLevel_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/admin/log-level", new { level = "Debug" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SetLogLevel_ValidLevel_ChangesLevel()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);

        var setResponse = await client.PostAsJsonAsync("/api/admin/log-level", new { level = "Debug" });
        Assert.Equal(HttpStatusCode.OK, setResponse.StatusCode);

        var getResponse = await client.GetAsync("/api/admin/log-level");
        var body = JsonSerializer.Deserialize<JsonElement>(
            await getResponse.Content.ReadAsStringAsync(), JsonOpts);
        Assert.Equal("Debug", body.GetProperty("level").GetString());
    }

    [Fact]
    public async Task SetLogLevel_InvalidLevel_Returns400()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);
        var response = await client.PostAsJsonAsync("/api/admin/log-level", new { level = "NotALevel" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SetLogLevel_MissingLevel_Returns400()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);
        var response = await client.PostAsJsonAsync("/api/admin/log-level", new { });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run the tests — verify they all fail**

```bash
cd backend && dotnet test --filter "FullyQualifiedName~AdminControllerTests"
```

Expected: compilation error (controller not found) or 5 failing tests.

- [ ] **Step 3: Create `AdminController.cs`**

Create `backend/TinyHeroes.Api/Controllers/AdminController.cs`:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog.Core;
using Serilog.Events;

namespace TinyHeroes.Api.Controllers;

[Authorize]
[Route("api/admin")]
public class AdminController(LoggingLevelSwitch levelSwitch) : ApiControllerBase
{
    [HttpGet("log-level")]
    public IActionResult GetLogLevel()
        => Ok(new { level = levelSwitch.MinimumLevel.ToString() });

    [HttpPost("log-level")]
    public IActionResult SetLogLevel([FromBody] SetLogLevelRequest request)
    {
        if (!Enum.TryParse<LogEventLevel>(request.Level, ignoreCase: true, out var level))
            return BadRequest(new { error = $"Invalid log level '{request.Level}'. Valid values: Verbose, Debug, Information, Warning, Error, Fatal." });

        levelSwitch.MinimumLevel = level;
        return Ok(new { level = levelSwitch.MinimumLevel.ToString() });
    }
}

public record SetLogLevelRequest(string? Level);
```

`LoggingLevelSwitch` is injected as a singleton — it is the same instance created in `Program.cs` and passed to `UseSerilog`, so mutating `.MinimumLevel` here immediately affects all log events from that point forward.

The `[Authorize]` attribute requires a valid JWT. Any authenticated user can change the level. This is acceptable for a single-family hobby app; if stricter control is needed later, add `[Authorize(Roles = "Admin")]` once a system admin role is seeded.

- [ ] **Step 4: Run the tests — verify they all pass**

```bash
cd backend && dotnet test --filter "FullyQualifiedName~AdminControllerTests"
```

Expected: 5 tests pass.

- [ ] **Step 5: Run all tests**

```bash
cd backend && dotnet test
```

Expected: all tests pass.

- [ ] **Step 6: Commit**

```bash
git add backend/TinyHeroes.Api/Controllers/AdminController.cs \
        backend/TinyHeroes.Tests/Integration/AdminControllerTests.cs
git commit -m "feat: add GET/POST /api/admin/log-level for runtime log level control"
```

---

## Task 6: Add Seq to Docker Compose and document env vars

**Files:**
- Modify: `docker-compose.yml`
- Modify: `.env.example`

- [ ] **Step 1: Add the `seq` service block**

In `docker-compose.yml`, after the `pgadmin` service block (before `volumes:`), add:

```yaml
  seq:
    image: datalust/seq:latest
    ports:
      - "5341:80"
    environment:
      ACCEPT_EULA: Y
    volumes:
      - seq_data:/data
```

- [ ] **Step 2: Add `seq_data` to the top-level `volumes` section**

The current `volumes:` block reads:

```yaml
volumes:
  postgres_data:
  uploads_data:
```

Change it to:

```yaml
volumes:
  postgres_data:
  uploads_data:
  seq_data:
```

- [ ] **Step 3: Add `seq` to the `api` service `depends_on`**

The current `api` `depends_on` reads:

```yaml
    depends_on:
      postgres:
        condition: service_healthy
```

Change it to:

```yaml
    depends_on:
      postgres:
        condition: service_healthy
      seq:
        condition: service_started
```

- [ ] **Step 4: Add env vars to `.env.example`**

At the end of `.env.example`, add:

```env
# Logging — Seq (local dev log viewer; see docs/deployment.md for the local URL)
SEQ_URL=http://seq:5341

# Logging — Azure Logs Ingestion API (integration + production)
# Both environments share the same DCE endpoint and Entra app registration.
# The streamName in appsettings.Integration/Production.json separates the Log Analytics tables.
# Add the five credential values as Application Settings in each Azure App Service:
#   Serilog__WriteTo__1__Args__credentials__endpoint      = <DCE Logs Ingestion URL>
#   Serilog__WriteTo__1__Args__credentials__immutableId   = <DCR Immutable ID>
#   Serilog__WriteTo__1__Args__credentials__tenantId      = <Entra Tenant ID>
#   Serilog__WriteTo__1__Args__credentials__clientId      = <App Registration Client ID>
#   Serilog__WriteTo__1__Args__credentials__clientSecret  = <App Registration Client Secret>
AZURE_LOG_ANALYTICS_ENDPOINT=
AZURE_LOG_ANALYTICS_IMMUTABLE_ID=
AZURE_LOG_ANALYTICS_TENANT_ID=
AZURE_LOG_ANALYTICS_CLIENT_ID=
AZURE_LOG_ANALYTICS_CLIENT_SECRET=
```

- [ ] **Step 5: Verify Docker Compose is valid**

```bash
docker compose config --quiet
```

Expected: exits with code 0 and no errors.

- [ ] **Step 6: Commit**

```bash
git add docker-compose.yml .env.example
git commit -m "feat: add Seq service to Docker Compose; document Azure logging env vars"
```

---

## Task 7: Update CHANGELOG

**Files:**
- Modify: `CHANGELOG.md`

- [ ] **Step 1: Add entry under `## [Unreleased]`**

Open `CHANGELOG.md` at the repo root. Under `## [Unreleased]`, add:

```markdown
### Added
- Structured JSON logging: each API log entry includes timestamp, level, request ID, user ID, and user agent
- `GET /api/admin/log-level` — returns the current minimum log level (requires authentication)
- `POST /api/admin/log-level` — changes the minimum log level at runtime without restarting (requires authentication); resets to default on next deploy

### Fixed
- Unhandled server errors now log the full stack trace and request context; previously they were silently swallowed
```

Do not include sink or infrastructure details (Seq, Azure Log Analytics, Docker) — the CHANGELOG audience is end users and API consumers, not operators.

No version bump — this is an operational and internal API change with no visible UI impact.

- [ ] **Step 2: Commit**

```bash
git add CHANGELOG.md
git commit -m "docs: add structured logging changelog entry"
```

---

## Task 8: End-to-end smoke test + PR

No code changes in this task. Smoke tests are run against the local dev stack.

- [ ] **Step 1: Start the local dev stack**

```bash
docker compose up -d --build
```

Expected: all services start including Seq.

- [ ] **Step 2: Verify structured request logs appear in Seq**

Open the Seq UI (port 5341 on localhost, as configured in `docker-compose.yml`).
Expected: Seq web UI loads.

Make an API call:

```bash
curl -s http://localhost:5001/api/info
```

Check the Seq Events stream.
Expected: a log entry for `/api/info` appears with discrete structured fields `RequestId`, `UserId` (`anon`), `UserAgent`.

- [ ] **Step 3: Verify structured fields are queryable**

In Seq, click any log event.
Expected: fields panel shows `RequestId`, `UserId`, `UserAgent`, `MachineName`, `ThreadId` as separate named fields.

In the Seq search bar type `UserId = 'anon'` and press Enter.
Expected: only unauthenticated request events are shown.

- [ ] **Step 4: Verify dynamic log level works**

Register a user and capture the JWT, then exercise the log-level endpoint:

```bash
TOKEN=$(curl -s -X POST http://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"logtest@test.com","password":"Password1!","displayName":"LogTest"}' \
  | jq -r '.token')

curl -s http://localhost:5001/api/admin/log-level \
  -H "Authorization: Bearer $TOKEN"
```

Expected: `{"level":"Information"}` (dev default).

```bash
curl -s -X POST http://localhost:5001/api/admin/log-level \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"level":"Debug"}'
```

Expected: `{"level":"Debug"}`. Make further API calls and check Seq — Debug-level entries from ASP.NET internals should now appear.

```bash
curl -s -X POST http://localhost:5001/api/admin/log-level \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"level":"Nonsense"}'
```

Expected: `400 Bad Request` with `{ "error": "Invalid log level..." }`.

- [ ] **Step 5: Unauthenticated access is rejected**

```bash
curl -s http://localhost:5001/api/admin/log-level
```

Expected: `401 Unauthorized`.

- [ ] **Step 6: Run all backend tests**

```bash
cd backend && dotnet test
```

Expected: all tests pass.

- [ ] **Step 7: Push and open PR**

```bash
git push -u origin feat/11-serilog-structured-logging
gh pr create --base master \
  --title "feat: backend structured logging (Serilog + runtime level control)" \
  --body "Closes #11

## Summary
- Serilog replaces built-in ASP.NET logging with structured JSON output
- Dev: Console + Seq; Integration + Production: Console + Azure Log Analytics (separate tables per environment)
- Grafana Cloud dashboards via Azure Monitor data source — no extra ingestion
- \`LoggingLevelSwitch\` singleton: default Warning (prod/integration), Information (dev)
- \`GET/POST /api/admin/log-level\` — change log level on the fly, resets on next deploy
- Each request log enriched with RequestId, UserId, UserAgent
- Unhandled exceptions now log full stack trace (previously silently swallowed)

## Test plan
- [ ] Local dev stack starts including Seq; request logs appear with structured fields
- [ ] \`GET /api/admin/log-level\` returns current level (auth required)
- [ ] \`POST /api/admin/log-level {level:Debug}\` changes level; Debug events appear in Seq
- [ ] \`POST /api/admin/log-level {level:Nonsense}\` returns 400
- [ ] Unauthenticated call to log-level returns 401
- [ ] \`dotnet test\` — all tests pass
- [ ] Azure App Service Application Settings configured for integration and production"
```

---

## Self-Review Notes

**Spec coverage:**
- ✅ All API logs are structured JSON
- ✅ Each entry: timestamp, level, RequestId, UserId, UserAgent
- ✅ Dev: Console + Seq
- ✅ Unhandled exceptions logged with full stack trace (Task 4)
- ✅ Production: Azure Log Analytics (Task 2)
- ✅ Integration: same DCE/DCR, differentiated by `streamName` → separate Log Analytics tables (Task 2)
- ✅ Existing tests unaffected — `ILogger<T>` abstraction preserved; verified Task 3 Step 6 and Task 4 Step 3
- ✅ Seq in Docker Compose (Task 6)
- ✅ `.env.example` updated (Task 6)
- ✅ Dynamic log level: `LoggingLevelSwitch` singleton + `AdminController` (Task 5)
- ✅ Grafana Cloud dashboard option via Azure Monitor data source (Sink Decision section)

**Noted gap — FamilyId enrichment:** Acceptance criteria mentions family ID on family-scoped requests. Deferred — not in the issue's code sample. Individual controllers can call `Log.ForContext("FamilyId", familyId)` when traceability is needed.

**Noted gap — Azure sink credentials:** `appsettings.Integration.json` and `appsettings.Production.json` have empty credential fields. Must be set as Azure App Service Application Settings manually (exact key names documented in Task 6 Step 4). The Azure pre-setup (DCE, DCR, Entra App Registration) documented in the Sink Decision section must be completed first — this cannot be automated without ACR/ARM access.

**Noted gap — admin endpoint authorization:** `AdminController` uses `[Authorize]` (any valid JWT). Acceptable for a single-family app; tighten later with `[Authorize(Roles = "Admin")]` once a system admin role is seeded.

**Placeholder scan:** No TBD/TODO items. All code blocks are complete. All commands include expected output.

**Type consistency:** `LoggingLevelSwitch` from `Serilog.Core` is used consistently across `Program.cs`, `AdminController`, and tests. `SetLogLevelRequest` record defined in same file as `AdminController` — no cross-file type reference needed. `System.Security.Claims.ClaimTypes` fully-qualified in Task 3 to avoid namespace conflict.
