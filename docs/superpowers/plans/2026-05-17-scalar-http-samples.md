# Scalar API UI + HTTP Sample Calls Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace Swashbuckle with .NET 10 built-in OpenAPI + Scalar UI, and add a `.http` file covering every endpoint.

**Architecture:** `Microsoft.AspNetCore.OpenApi` generates the spec at `/openapi/v1.json`; `Scalar.AspNetCore` serves the interactive UI at `/scalar/v1`. Both are gated behind `IsDevelopment()`. A Bearer JWT security scheme is registered via a document transformer; per-operation security requirements are injected via an operation transformer that inspects `[Authorize]` metadata.

**Tech Stack:** .NET 10, `Microsoft.AspNetCore.OpenApi 10.*`, `Scalar.AspNetCore 2.*`, VS Code REST Client `.http` format (also works in JetBrains Rider).

---

## Files

| Action | Path |
|--------|------|
| Modify | `backend/TinyHeroes.Api/TinyHeroes.Api.csproj` |
| Modify | `backend/TinyHeroes.Api/Program.cs` |
| Create | `backend/TinyHeroes.Tests/Integration/OpenApiTests.cs` |
| Create | `backend/TinyHeroes.Api/TinyHeroes.Api.http` |

---

### Task 1: Write failing tests for OpenAPI + Scalar endpoints

**Files:**
- Create: `backend/TinyHeroes.Tests/Integration/OpenApiTests.cs`

- [ ] **Step 1: Create the test file**

```csharp
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;

namespace TinyHeroes.Tests.Integration;

public class OpenApiTests
{
    [Fact]
    public async Task OpenApiJson_Returns200_InDevelopment()
    {
        await using var factory = new DevFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task ScalarUi_ReturnsSuccessOrRedirect_InDevelopment()
    {
        await using var factory = new DevFactory();
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/scalar/v1");

        ((int)response.StatusCode).Should().BeOneOf(200, 301, 302);
    }

    // Inherits fake DB, AI, and file services from TestWebApplicationFactory
    // then overrides the environment to Development so MapOpenApi/MapScalarApiReference are registered.
    private class DevFactory : TestWebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseEnvironment("Development");
        }
    }
}
```

- [ ] **Step 2: Run tests to confirm they fail**

```bash
dotnet test backend/TinyHeroes.Tests/TinyHeroes.Tests.csproj --filter "FullyQualifiedName~OpenApiTests" -v normal
```

Expected: both tests FAIL with 404 (Swashbuckle serves `/swagger`, not `/openapi/v1.json` or `/scalar/v1`).

---

### Task 2: Swap NuGet packages

**Files:**
- Modify: `backend/TinyHeroes.Api/TinyHeroes.Api.csproj`

- [ ] **Step 1: Replace the package references**

Open `backend/TinyHeroes.Api/TinyHeroes.Api.csproj` and replace:

```xml
<PackageReference Include="Swashbuckle.AspNetCore" Version="8.*" />
```

with:

```xml
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.*" />
<PackageReference Include="Scalar.AspNetCore" Version="2.*" />
```

- [ ] **Step 2: Restore packages**

```bash
dotnet restore backend/TinyHeroes.Api/TinyHeroes.Api.csproj
```

Expected: restore succeeds, no errors.

---

### Task 3: Update Program.cs — replace Swagger with OpenAPI + Scalar

**Files:**
- Modify: `backend/TinyHeroes.Api/Program.cs`

- [ ] **Step 1: Add required usings at the top of Program.cs**

Add these two lines alongside the existing `using` block:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
```

- [ ] **Step 2: Replace the Swagger service registrations**

Find and remove:

```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
```

Add in their place:

```csharp
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Components ??= new();
        document.Components.SecuritySchemes = new Dictionary<string, OpenApiSecurityScheme>
        {
            ["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Paste your JWT from POST /api/auth/login"
            }
        };
        return Task.CompletedTask;
    });

    options.AddOperationTransformer((operation, context, cancellationToken) =>
    {
        var hasAuthorize = context.Description.ActionDescriptor.EndpointMetadata
            .OfType<IAuthorizeData>().Any();
        if (hasAuthorize)
        {
            operation.Security =
            [
                new OpenApiSecurityRequirement
                {
                    [new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Id = "Bearer",
                            Type = ReferenceType.SecurityScheme
                        }
                    }] = []
                }
            ];
        }
        return Task.CompletedTask;
    });
});
```

- [ ] **Step 3: Replace the Swagger middleware block**

Find and remove:

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

Add in its place:

```csharp
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
```

- [ ] **Step 4: Build to confirm no compile errors**

```bash
dotnet build backend/TinyHeroes.Api/TinyHeroes.Api.csproj
```

Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 5: Run the OpenAPI tests to confirm they pass**

```bash
dotnet test backend/TinyHeroes.Tests/TinyHeroes.Tests.csproj --filter "FullyQualifiedName~OpenApiTests" -v normal
```

Expected: both tests PASS.

- [ ] **Step 6: Run the full test suite to confirm no regressions**

```bash
dotnet test backend/TinyHeroes.Tests/TinyHeroes.Tests.csproj -v normal
```

Expected: all existing tests still PASS (the Swagger removal does not affect any existing test since the integration tests use `UseEnvironment("Testing")` which already skipped the Swagger middleware).

- [ ] **Step 7: Commit**

```bash
git add backend/TinyHeroes.Api/TinyHeroes.Api.csproj backend/TinyHeroes.Api/Program.cs backend/TinyHeroes.Tests/Integration/OpenApiTests.cs
git commit -m "feat: replace Swashbuckle with built-in OpenAPI + Scalar"
```

---

### Task 4: Create the HTTP sample file

**Files:**
- Create: `backend/TinyHeroes.Api/TinyHeroes.Api.http`

- [ ] **Step 1: Create the file with the full content**

```http
### TinyHeroes API — sample requests
### Run top-to-bottom for happy-path flow.
### After "Login", copy the token value from the response and set @token below.

@host = http://localhost:5000
@token = PASTE_TOKEN_HERE
@childId = PASTE_CHILD_ID_HERE
@inviteToken = PASTE_INVITE_TOKEN_HERE
@presetId = PASTE_PRESET_ID_HERE
@prizePresetId = PASTE_PRIZE_PRESET_ID_HERE

### ── AUTH ──────────────────────────────────────────────────────────────────

### Register
POST {{host}}/api/auth/register
Content-Type: application/json

{
  "displayName": "Alex Parent",
  "email": "parent@example.com",
  "password": "P@ssw0rd1!"
}

### Login — copy "token" from response, paste into @token above
POST {{host}}/api/auth/login
Content-Type: application/json

{
  "email": "parent@example.com",
  "password": "P@ssw0rd1!"
}

### ── USERS ─────────────────────────────────────────────────────────────────

### Get my profile
GET {{host}}/api/users/me
Authorization: Bearer {{token}}

### Update my profile
PATCH {{host}}/api/users/me
Authorization: Bearer {{token}}
Content-Type: application/json

{
  "displayName": "Alex",
  "preferredLanguage": "en",
  "pushNotificationsEnabled": false,
  "weeklyEmailEnabled": true
}

### ── FAMILIES ──────────────────────────────────────────────────────────────

### Create family
POST {{host}}/api/families
Authorization: Bearer {{token}}
Content-Type: application/json

{
  "name": "Smith Family",
  "weekStartDay": 1
}

### Get my family
GET {{host}}/api/families/mine
Authorization: Bearer {{token}}

### Update my family
PATCH {{host}}/api/families/mine
Authorization: Bearer {{token}}
Content-Type: application/json

{
  "name": "Smith Family",
  "weekStartDay": 1
}

### ── INVITES ───────────────────────────────────────────────────────────────

### Create invite — copy "token" from response, paste into @inviteToken above
POST {{host}}/api/invites
Authorization: Bearer {{token}}
Content-Type: application/json

{
  "email": "partner@example.com"
}

### Accept invite (run as the invited user after they register/login)
POST {{host}}/api/invites/{{inviteToken}}/accept
Authorization: Bearer {{token}}

### ── CHILDREN ──────────────────────────────────────────────────────────────

### Create child — copy "id" from response, paste into @childId above
### gender: 0=Boy 1=Girl  |  weekStartDay: 0=Sun 1=Mon 2=Tue 3=Wed 4=Thu 5=Fri 6=Sat
POST {{host}}/api/children
Authorization: Bearer {{token}}
Content-Type: application/json

{
  "name": "Emma",
  "age": 8,
  "gender": 1,
  "avatarEmoji": "👧"
}

### List children
GET {{host}}/api/children
Authorization: Bearer {{token}}

### Get child by ID
GET {{host}}/api/children/{{childId}}
Authorization: Bearer {{token}}

### Update child
PUT {{host}}/api/children/{{childId}}
Authorization: Bearer {{token}}
Content-Type: application/json

{
  "name": "Emma",
  "age": 9,
  "gender": 1,
  "avatarEmoji": "👧"
}

### Delete child
DELETE {{host}}/api/children/{{childId}}
Authorization: Bearer {{token}}

### ── DEEDS ─────────────────────────────────────────────────────────────────

### Log a good deed
POST {{host}}/api/deeds
Authorization: Bearer {{token}}
Content-Type: application/json

{
  "childId": "{{childId}}",
  "description": "Helped with dishes",
  "imageType": "emoji",
  "imageValue": "🍽️"
}

### List deeds for a child
GET {{host}}/api/deeds?childId={{childId}}
Authorization: Bearer {{token}}

### Get deed stats (weekly + total per child)
GET {{host}}/api/deeds/stats
Authorization: Bearer {{token}}

### Generate AI image for a deed
POST {{host}}/api/deeds/generate-image
Authorization: Bearer {{token}}
Content-Type: application/json

{
  "prompt": "a child helping with dishes, cartoon style"
}

### ── PRESETS ───────────────────────────────────────────────────────────────

### List deed presets (system + family)
GET {{host}}/api/presets
Authorization: Bearer {{token}}

### Create custom deed preset — copy "id" from response, paste into @presetId above
POST {{host}}/api/presets
Authorization: Bearer {{token}}
Content-Type: application/json

{
  "label": "Set the table",
  "imageValue": "🍽️"
}

### Delete custom deed preset (admin only, system presets are protected)
DELETE {{host}}/api/presets/{{presetId}}
Authorization: Bearer {{token}}

### ── PRIZE PRESETS ─────────────────────────────────────────────────────────

### List prize presets
GET {{host}}/api/prize-presets
Authorization: Bearer {{token}}

### Create prize preset — copy "id" from response, paste into @prizePresetId above
POST {{host}}/api/prize-presets
Authorization: Bearer {{token}}
Content-Type: application/json

{
  "label": "Ice cream",
  "emoji": "🍦"
}

### Delete prize preset (admin only)
DELETE {{host}}/api/prize-presets/{{prizePresetId}}
Authorization: Bearer {{token}}

### ── PRIZE ASSIGNMENTS ─────────────────────────────────────────────────────

### List prize assignments for my family
GET {{host}}/api/prize-assignments
Authorization: Bearer {{token}}

### Set/upsert a prize assignment (admin only)
PUT {{host}}/api/prize-assignments
Authorization: Bearer {{token}}
Content-Type: application/json

{
  "scope": "Week",
  "rank": 1,
  "emoji": "🥇",
  "label": "Gold Star"
}

### ── SUMMARIES ─────────────────────────────────────────────────────────────

### Get weekly summaries
GET {{host}}/api/summaries/weeks
Authorization: Bearer {{token}}

### Get monthly summaries
GET {{host}}/api/summaries/months
Authorization: Bearer {{token}}

### Get current month live standings
GET {{host}}/api/summaries/current-month
Authorization: Bearer {{token}}
```

- [ ] **Step 2: Commit**

```bash
git add backend/TinyHeroes.Api/TinyHeroes.Api.http
git commit -m "feat: add HTTP sample calls for all API endpoints"
```

---

## Manual Smoke Test

After `docker compose up --build -d`:

1. Open http://localhost:5000/scalar/v1 — Scalar UI loads, all endpoints visible, lock icon shown on protected routes.
2. Open http://localhost:5000/openapi/v1.json — raw OpenAPI JSON returned.
3. Open `TinyHeroes.Api.http` in VS Code (with REST Client extension) or Rider.
4. Run **Register**, then **Login**, copy the `token` field from the response.
5. Paste token into `@token` at the top of the file.
6. Run each request top-to-bottom, checking for 200/201/204 responses.
