# Design: Scalar API UI + HTTP Sample Calls

**Date:** 2026-05-17  
**Status:** Approved

## Goal

Replace the existing Swagger UI (Swashbuckle) with Scalar and add a `.http` file covering all API endpoints for local testing.

## Approach

Option A — .NET 10 built-in OpenAPI (`Microsoft.AspNetCore.OpenApi`) + `Scalar.AspNetCore`. Swashbuckle is removed entirely. Cleaner dependency tree, idiomatic for .NET 10.

## 1. NuGet Changes

**File:** `backend/TinyHeroes.Api/TinyHeroes.Api.csproj`

| Action | Package |
|--------|---------|
| Remove | `Swashbuckle.AspNetCore` |
| Add | `Microsoft.AspNetCore.OpenApi` version `10.*` |
| Add | `Scalar.AspNetCore` latest stable (no .NET 10-specific version; use `2.*`) |

## 2. Program.cs Changes

### Services (builder section)

Remove:
```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
```

Add:
```csharp
builder.Services.AddOpenApi(options =>
{
    // Register Bearer scheme in the document
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

    // Add security requirement to every operation that has [Authorize]
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
                    [new OpenApiSecurityScheme { Reference = new OpenApiReference { Id = "Bearer", Type = ReferenceType.SecurityScheme } }] = []
                }
            ];
        }
        return Task.CompletedTask;
    });
});
```

Required usings: `Microsoft.OpenApi.Models`, `Microsoft.AspNetCore.Authorization`.

### Middleware (app section)

Remove:
```csharp
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

Add:
```csharp
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();                  // → /openapi/v1.json
    app.MapScalarApiReference();       // → /scalar/v1
}
```

Both remain Development-only, consistent with the existing Swagger guard.

## 3. HTTP File

**File:** `backend/TinyHeroes.Api/TinyHeroes.Api.http`

Picked up automatically by VS Code REST Client and JetBrains Rider HTTP Client.

### Structure

```
@host = http://localhost:5000
@token = <paste token from login response>
```

Sections (in happy-path execution order):

| Section | Endpoints |
|---------|-----------|
| Auth | `POST /api/auth/register`, `POST /api/auth/login` |
| Users | `GET /api/users/me`, `PATCH /api/users/me` |
| Families | `POST /api/families`, `GET /api/families/mine`, `PATCH /api/families/mine` |
| Invites | `POST /api/invites`, `POST /api/invites/{token}/accept` |
| Children | `POST /api/children`, `GET /api/children`, `GET /api/children/{id}`, `PUT /api/children/{id}`, `DELETE /api/children/{id}` |
| Deeds | `POST /api/deeds`, `GET /api/deeds?childId=`, `GET /api/deeds/stats`, `POST /api/deeds/generate-image` |
| Presets | `GET /api/presets`, `POST /api/presets`, `DELETE /api/presets/{id}` |
| Prize Presets | `GET /api/prize-presets`, `POST /api/prize-presets`, `DELETE /api/prize-presets/{id}` |
| Prize Assignments | `GET /api/prize-assignments`, `PUT /api/prize-assignments` |
| Summaries | `GET /api/summaries/weeks`, `GET /api/summaries/months`, `GET /api/summaries/current-month` |

Each request uses `Authorization: Bearer {{token}}` except the two auth endpoints.  
Placeholder GUIDs (e.g. `{{childId}}`) are defined as variables at the top of the file.

## Testing

- Start stack: `docker compose up --build -d`
- Scalar UI: http://localhost:5000/scalar/v1
- OpenAPI JSON: http://localhost:5000/openapi/v1.json
- HTTP file: open `TinyHeroes.Api.http` in VS Code or Rider, run requests top-to-bottom

## Out of Scope

- Avatar upload (`POST /api/children/{id}/avatar`) — multipart/form-data; not easily expressed in `.http` variable flow, skip for now
- Social auth callbacks — browser redirect flow, not testable via HTTP file
