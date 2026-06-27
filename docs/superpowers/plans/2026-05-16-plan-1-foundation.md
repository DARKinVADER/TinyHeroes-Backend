# TinyHeroes — Plan 1: Foundation & Auth

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stand up the full project structure (Angular + .NET + PostgreSQL), wire up Docker Compose so everything runs locally with one command, and implement complete authentication — email/password registration+login plus Google/Apple/Facebook social login — producing a working login flow that lands on an empty dashboard.

**Architecture:** Clean Architecture with 4 layers — Domain (entities, enums, zero deps) → Application (service interfaces, DTOs, exceptions) → Infrastructure (EF Core, Identity, JWT, FluentStorage implementations) → Api (thin controllers, middleware, DI wiring). Angular SPA (served by nginx in Docker) communicates with the ASP.NET Core 10 Web API over HTTP. The API uses ASP.NET Core Identity for user management, issues short-lived JWT access tokens, and delegates social login to OAuth2 providers. PostgreSQL stores all data via EF Core. FluentStorage provides provider-agnostic file storage — local disk in development, cloud in production.

**Tech Stack:** Angular 21, TailwindCSS, @ngx-translate/core, ASP.NET Core 10, EF Core 10 + Npgsql, ASP.NET Core Identity, JWT Bearer, PostgreSQL 16, FluentStorage, Docker + docker compose

**Design spec:** `docs/superpowers/specs/2026-05-16-tinyheroes-design.md`  
**Auth screen mockups:** `.superpowers/brainstorm/1352-1778911830/content/screens-auth.html`

---

## Plan Overview (5 plans total)

| Plan | Scope |
|---|---|
| **Plan 1 (this)** | Project structure, Docker, auth (JWT + social login), Angular auth screens |
| Plan 2 | Family creation, child profiles, co-parent invite |
| Plan 3 | Good deeds, image library, AI generation, deed presets |
| Plan 4 | Weekly/monthly ranking engine, podium, monthly champion, history |
| Plan 5 | Prizes board, prize editor, custom prizes, settings, profile, i18n |

---

## File Structure

```
TinyHeroes/
├── frontend/
│   ├── src/
│   │   ├── app/
│   │   │   ├── core/
│   │   │   │   ├── auth/
│   │   │   │   │   ├── auth.service.ts         JWT storage, login/logout/register
│   │   │   │   │   ├── auth.guard.ts           Redirect to /login if no token
│   │   │   │   │   └── auth.interceptor.ts     Attach Bearer token to requests
│   │   │   │   ├── http/
│   │   │   │   │   └── error.interceptor.ts    401→logout, 5xx→toast
│   │   │   │   └── models/
│   │   │   │       └── user.model.ts
│   │   │   ├── features/
│   │   │   │   └── auth/
│   │   │   │       ├── auth.routes.ts
│   │   │   │       ├── pages/
│   │   │   │       │   ├── welcome.component.ts
│   │   │   │       │   ├── login.component.ts
│   │   │   │       │   ├── signup.component.ts
│   │   │   │       │   └── create-family.component.ts
│   │   │   │       └── components/
│   │   │   │           ├── social-buttons.component.ts
│   │   │   │           └── email-form.component.ts
│   │   │   ├── shared/
│   │   │   │   └── components/
│   │   │   │       └── (empty — filled in later plans)
│   │   │   ├── app.component.ts
│   │   │   ├── app.routes.ts
│   │   │   └── app.config.ts
│   │   ├── assets/i18n/
│   │   │   ├── en.json
│   │   │   └── hu.json
│   │   └── environments/
│   │       ├── environment.ts
│   │       └── environment.prod.ts
│   ├── Dockerfile
│   ├── nginx.conf
│   ├── tailwind.config.js
│   └── package.json
│
├── backend/
│   ├── TinyHeroes.sln
│   ├── TinyHeroes.Domain/                     ← innermost, ZERO dependencies
│   │   ├── Entities/
│   │   │   ├── User.cs
│   │   │   ├── Family.cs
│   │   │   ├── FamilyMember.cs
│   │   │   └── Child.cs                      (stub — used in Plan 2)
│   │   ├── Enums/
│   │   │   └── FamilyRole.cs
│   │   └── Interfaces/
│   │       └── Repositories/
│   │           └── (empty — filled in later plans)
│   ├── TinyHeroes.Application/                ← depends on Domain only
│   │   ├── Interfaces/
│   │   │   ├── ITokenService.cs
│   │   │   └── ICurrentUserService.cs
│   │   ├── DTOs/
│   │   │   └── Auth/
│   │   │       ├── RegisterRequest.cs
│   │   │       ├── LoginRequest.cs
│   │   │       └── AuthResponse.cs
│   │   ├── Services/
│   │   │   └── AuthService.cs
│   │   └── Exceptions/
│   │       ├── NotFoundException.cs
│   │       └── ForbiddenException.cs
│   ├── TinyHeroes.Infrastructure/             ← implements Domain + Application
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── Configurations/
│   │   │   │   └── FamilyMemberConfiguration.cs
│   │   │   └── Migrations/
│   │   ├── Services/
│   │   │   ├── JwtTokenService.cs             implements ITokenService
│   │   │   └── CurrentUserService.cs          implements ICurrentUserService
│   │   └── DependencyInjection.cs             services.AddInfrastructure(config)
│   ├── TinyHeroes.Api/                        ← thin; DI wiring + HTTP
│   │   ├── Controllers/
│   │   │   └── AuthController.cs
│   │   ├── Middleware/
│   │   │   └── ExceptionHandlingMiddleware.cs
│   │   ├── DependencyInjection.cs             services.AddApi()
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── appsettings.Development.json
│   │   └── Dockerfile
│   └── TinyHeroes.Tests/
│       ├── Unit/
│       │   └── JwtTokenServiceTests.cs
│       └── Integration/
│           ├── TestWebApplicationFactory.cs
│           └── AuthControllerTests.cs
│
├── docker compose.yml
├── docker compose.override.yml   (local dev secrets)
└── .env.example
```

---

### Task 1: Repo skeleton + docker compose

**Files:**
- Create: `docker compose.yml`
- Create: `docker compose.override.yml`
- Create: `.env.example`
- Create: `.gitignore`

- [ ] **Step 1: Create root `.gitignore`**

```
# .gitignore
**/bin/
**/obj/
**/node_modules/
**/.angular/
**/dist/
.env
.env.local
docker compose.override.yml
```

- [ ] **Step 2: Create `.env.example`**

```env
# .env.example — copy to .env and fill in values
POSTGRES_DB=tinyheroes
POSTGRES_USER=tinyheroes
POSTGRES_PASSWORD=changeme

JWT_SECRET=replace-with-32-char-minimum-secret-key
JWT_ISSUER=tinyheroes-api
JWT_AUDIENCE=tinyheroes-frontend
JWT_EXPIRY_MINUTES=60

GOOGLE_CLIENT_ID=
GOOGLE_CLIENT_SECRET=
APPLE_CLIENT_ID=
APPLE_CLIENT_SECRET=
FACEBOOK_APP_ID=
FACEBOOK_APP_SECRET=

# Storage — local disk by default; swap connection string for cloud:
# AWS S3:    aws.s3://keyId=...;key=...;bucket=tinyheroes;region=eu-central-1
# Azure:     azure.blob://account=...;key=...
# GCS:       google.storage://bucket=tinyheroes;cred=<base64>
STORAGE_CONNECTION_STRING=disk://path=/app/uploads

# AI Image Generation — Hugging Face Serverless Inference API (free tier)
# Model: black-forest-labs/FLUX.1-schnell (fast) or bytedance/sdxl-lightning-4step
HUGGINGFACE_API_KEY=
HUGGINGFACE_MODEL=black-forest-labs/FLUX.1-schnell
```

- [ ] **Step 3: Create `docker compose.yml`**

```yaml
# docker compose.yml
services:
  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: ${POSTGRES_DB}
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
    volumes:
      - postgres_data:/var/lib/postgresql/data
    ports:
      - "5432:5432"
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER}"]
      interval: 5s
      timeout: 5s
      retries: 5

  api:
    build:
      context: ./backend
      dockerfile: TinyHeroes.Api/Dockerfile
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ConnectionStrings__Default: "Host=postgres;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"
      Jwt__Secret: ${JWT_SECRET}
      Jwt__Issuer: ${JWT_ISSUER}
      Jwt__Audience: ${JWT_AUDIENCE}
      Jwt__ExpiryMinutes: ${JWT_EXPIRY_MINUTES}
      Auth__Google__ClientId: ${GOOGLE_CLIENT_ID}
      Auth__Google__ClientSecret: ${GOOGLE_CLIENT_SECRET}
      Auth__Apple__ClientId: ${APPLE_CLIENT_ID}
      Auth__Apple__ClientSecret: ${APPLE_CLIENT_SECRET}
      Auth__Facebook__AppId: ${FACEBOOK_APP_ID}
      Auth__Facebook__AppSecret: ${FACEBOOK_APP_SECRET}
      Storage__ConnectionString: ${STORAGE_CONNECTION_STRING}
      AiImage__Provider: HuggingFace
      AiImage__HuggingFace__ApiKey: ${HUGGINGFACE_API_KEY}
      AiImage__HuggingFace__Model: ${HUGGINGFACE_MODEL}
    volumes:
      - uploads_data:/app/uploads
    ports:
      - "5000:8080"
    depends_on:
      postgres:
        condition: service_healthy

  frontend:
    build:
      context: ./frontend
      dockerfile: Dockerfile
    ports:
      - "4200:80"
    depends_on:
      - api

volumes:
  postgres_data:
  uploads_data:
```

- [ ] **Step 4: Create `docker compose.override.yml` (gitignored, local dev)**

```yaml
# docker compose.override.yml
services:
  api:
    environment:
      ASPNETCORE_ENVIRONMENT: Development
```

- [ ] **Step 5: Copy `.env.example` to `.env` and fill in `POSTGRES_PASSWORD` and `JWT_SECRET`**

```bash
cp .env.example .env
# Edit .env: set POSTGRES_PASSWORD=localdev and JWT_SECRET=local-dev-secret-minimum-32-chars!
```

- [ ] **Step 6: Commit**

```bash
git init
git add .gitignore .env.example docker compose.yml
git commit -m "chore: project skeleton with docker compose"
```

---

### Task 2: .NET solution and projects

**Files:**
- Create: `backend/TinyHeroes.sln`
- Create: `backend/TinyHeroes.Domain/TinyHeroes.Domain.csproj`
- Create: `backend/TinyHeroes.Application/TinyHeroes.Application.csproj`
- Create: `backend/TinyHeroes.Infrastructure/TinyHeroes.Infrastructure.csproj`
- Create: `backend/TinyHeroes.Api/TinyHeroes.Api.csproj`
- Create: `backend/TinyHeroes.Tests/TinyHeroes.Tests.csproj`

- [ ] **Step 1: Scaffold the .NET solution**

```bash
cd backend
dotnet new sln -n TinyHeroes
dotnet new classlib -n TinyHeroes.Domain -f net10.0
dotnet new classlib -n TinyHeroes.Application -f net10.0
dotnet new classlib -n TinyHeroes.Infrastructure -f net10.0
dotnet new webapi -n TinyHeroes.Api -f net10.0 --no-openapi false
dotnet new xunit -n TinyHeroes.Tests -f net10.0

dotnet sln add TinyHeroes.Domain
dotnet sln add TinyHeroes.Application
dotnet sln add TinyHeroes.Infrastructure
dotnet sln add TinyHeroes.Api
dotnet sln add TinyHeroes.Tests

# Dependencies point inward:
dotnet add TinyHeroes.Application reference TinyHeroes.Domain
dotnet add TinyHeroes.Infrastructure reference TinyHeroes.Domain
dotnet add TinyHeroes.Infrastructure reference TinyHeroes.Application
dotnet add TinyHeroes.Api reference TinyHeroes.Application
dotnet add TinyHeroes.Api reference TinyHeroes.Infrastructure
dotnet add TinyHeroes.Tests reference TinyHeroes.Api
dotnet add TinyHeroes.Tests reference TinyHeroes.Infrastructure
```

- [ ] **Step 2: Add NuGet packages**

```bash
# Domain — zero external dependencies (no packages needed)

# Application — only depends on Domain (no packages needed for now)

# Infrastructure
cd TinyHeroes.Infrastructure
dotnet add package Microsoft.EntityFrameworkCore -v 10.*
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL -v 10.*
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore -v 10.*
dotnet add package Microsoft.Extensions.Configuration.Abstractions -v 10.*

# Api
cd ../TinyHeroes.Api
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer -v 10.*
dotnet add package Microsoft.AspNetCore.Authentication.Google -v 10.*
dotnet add package Microsoft.AspNetCore.Authentication.Facebook -v 10.*
dotnet add package AspNet.Security.OAuth.Apple
dotnet add package System.IdentityModel.Tokens.Jwt -v 8.*
dotnet add package Microsoft.EntityFrameworkCore.Design -v 10.*
dotnet add package Swashbuckle.AspNetCore -v 7.*

# Tests
cd ../TinyHeroes.Tests
dotnet add package Microsoft.AspNetCore.Mvc.Testing -v 10.*
dotnet add package FluentAssertions
dotnet add package Moq
dotnet add package Testcontainers.PostgreSql
```

- [ ] **Step 3: Verify build**

```bash
cd ..
dotnet build TinyHeroes.sln
```

Expected: Build succeeded, 0 error(s).

- [ ] **Step 4: Commit**

```bash
git add backend/
git commit -m "chore: scaffold .NET solution with Clean Architecture projects (Domain/Application/Infrastructure/Api)"
```

---

### Task 3: Domain entities and EF Core setup

**Files:**
- Create: `backend/TinyHeroes.Domain/Entities/User.cs`
- Create: `backend/TinyHeroes.Domain/Entities/Family.cs`
- Create: `backend/TinyHeroes.Domain/Entities/FamilyMember.cs`
- Create: `backend/TinyHeroes.Domain/Enums/FamilyRole.cs`
- Create: `backend/TinyHeroes.Application/Interfaces/ITokenService.cs`
- Create: `backend/TinyHeroes.Infrastructure/Data/AppDbContext.cs`
- Create: `backend/TinyHeroes.Infrastructure/Data/Configurations/FamilyMemberConfiguration.cs`
- Create: `backend/TinyHeroes.Infrastructure/DependencyInjection.cs`
- Create: `backend/TinyHeroes.Api/appsettings.json`

- [ ] **Step 1: Delete generated Class1.cs stubs**

```bash
rm backend/TinyHeroes.Domain/Class1.cs
rm backend/TinyHeroes.Application/Class1.cs
rm backend/TinyHeroes.Infrastructure/Class1.cs
```

- [ ] **Step 2: Create `TinyHeroes.Domain/Enums/FamilyRole.cs`**

```csharp
namespace TinyHeroes.Domain.Enums;

public enum FamilyRole { Admin, CoParent }
```

- [ ] **Step 3: Create `TinyHeroes.Domain/Entities/User.cs`**

```csharp
using Microsoft.AspNetCore.Identity;

namespace TinyHeroes.Domain.Entities;

public class User : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
    public string PreferredLanguage { get; set; } = "en";
    public bool PushNotificationsEnabled { get; set; } = true;
    public bool WeeklyEmailEnabled { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<FamilyMember> FamilyMemberships { get; set; } = [];
}
```

- [ ] **Step 4: Create `TinyHeroes.Domain/Entities/Family.cs`**

```csharp
namespace TinyHeroes.Domain.Entities;

public class Family
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DayOfWeek WeekStartDay { get; set; } = DayOfWeek.Monday;
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<FamilyMember> Members { get; set; } = [];
}
```

- [ ] **Step 5: Create `TinyHeroes.Domain/Entities/FamilyMember.cs`**

```csharp
using TinyHeroes.Domain.Enums;

namespace TinyHeroes.Domain.Entities;

public class FamilyMember
{
    public Guid Id { get; set; }
    public Guid FamilyId { get; set; }
    public Guid UserId { get; set; }
    public FamilyRole Role { get; set; } = FamilyRole.CoParent;
    public string? Relation { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public Family Family { get; set; } = null!;
    public User User { get; set; } = null!;
}
```

- [ ] **Step 6: Create `TinyHeroes.Application/Interfaces/ITokenService.cs`**

```csharp
using TinyHeroes.Domain.Entities;

namespace TinyHeroes.Application.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
}
```

- [ ] **Step 7: Create `TinyHeroes.Infrastructure/Data/AppDbContext.cs`**

```csharp
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TinyHeroes.Domain.Entities;

namespace TinyHeroes.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<User, Microsoft.AspNetCore.Identity.IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Family> Families => Set<Family>();
    public DbSet<FamilyMember> FamilyMembers => Set<FamilyMember>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
```

- [ ] **Step 8: Create `TinyHeroes.Infrastructure/Data/Configurations/FamilyMemberConfiguration.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TinyHeroes.Domain.Entities;

namespace TinyHeroes.Infrastructure.Data.Configurations;

public class FamilyMemberConfiguration : IEntityTypeConfiguration<FamilyMember>
{
    public void Configure(EntityTypeBuilder<FamilyMember> builder)
    {
        builder.HasOne(m => m.Family).WithMany(f => f.Members).HasForeignKey(m => m.FamilyId);
        builder.HasOne(m => m.User).WithMany(u => u.FamilyMemberships).HasForeignKey(m => m.UserId);
        builder.HasIndex(m => new { m.FamilyId, m.UserId }).IsUnique();
    }
}
```

- [ ] **Step 9: Create `TinyHeroes.Infrastructure/DependencyInjection.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TinyHeroes.Application.Interfaces;
using TinyHeroes.Infrastructure.Data;
using TinyHeroes.Infrastructure.Services;

namespace TinyHeroes.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseNpgsql(config.GetConnectionString("Default")));

        services.AddScoped<ITokenService, JwtTokenService>();

        return services;
    }
}
```

- [ ] **Step 10: Create `TinyHeroes.Api/appsettings.json`**

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Database=tinyheroes;Username=tinyheroes;Password=localdev"
  },
  "Jwt": {
    "Secret": "local-dev-secret-minimum-32-chars!",
    "Issuer": "tinyheroes-api",
    "Audience": "tinyheroes-frontend",
    "ExpiryMinutes": 60
  },
  "Auth": {
    "Google": { "ClientId": "", "ClientSecret": "" },
    "Apple": { "ClientId": "", "ClientSecret": "" },
    "Facebook": { "AppId": "", "AppSecret": "" }
  },
  "Storage": {
    "ConnectionString": "disk://path=/app/uploads"
  },
  "AiImage": {
    "Provider": "HuggingFace",
    "HuggingFace": {
      "ApiKey": "",
      "Model": "black-forest-labs/FLUX.1-schnell"
    }
  },
  "Logging": {
    "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" }
  },
  "AllowedHosts": "*",
  "AllowedOrigins": [ "http://localhost:4200" ]
}
```

- [ ] **Step 11: Add initial EF Core migration**

```bash
cd backend
dotnet ef migrations add InitialSchema \
  --project TinyHeroes.Infrastructure \
  --startup-project TinyHeroes.Api \
  --output-dir Data/Migrations
```

Expected: migration files created in `TinyHeroes.Infrastructure/Data/Migrations/`

- [ ] **Step 12: Commit**

```bash
git add backend/
git commit -m "feat: domain entities, application interfaces, EF Core context, initial migration"
```

---

### Task 4: JWT token service

**Files:**
- Create: `backend/TinyHeroes.Infrastructure/Services/JwtTokenService.cs`
- Create: `backend/TinyHeroes.Tests/Unit/JwtTokenServiceTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// TinyHeroes.Tests/Unit/JwtTokenServiceTests.cs
using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using TinyHeroes.Domain.Entities;
using TinyHeroes.Infrastructure.Services;

namespace TinyHeroes.Tests.Unit;

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _sut;

    public JwtTokenServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "test-secret-minimum-32-characters-long!",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Jwt:ExpiryMinutes"] = "60"
            })
            .Build();
        _sut = new JwtTokenService(config);
    }

    [Fact]
    public void GenerateAccessToken_ReturnsValidJwt()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "test@test.com", DisplayName = "Test" };

        var token = _sut.GenerateAccessToken(user);

        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(token).Should().BeTrue();
        var jwt = handler.ReadJwtToken(token);
        jwt.Issuer.Should().Be("test-issuer");
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
    }

    [Fact]
    public void GenerateAccessToken_TokenExpiresInConfiguredMinutes()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "test@test.com", DisplayName = "Test" };

        var token = _sut.GenerateAccessToken(user);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(60), TimeSpan.FromSeconds(5));
    }
}
```

- [ ] **Step 2: Run test — expect FAIL**

```bash
cd backend
dotnet test TinyHeroes.Tests --filter "JwtTokenServiceTests" -v normal
```

Expected: FAIL — `JwtTokenService` not found.

- [ ] **Step 3: Implement `JwtTokenService`**

```csharp
// TinyHeroes.Infrastructure/Services/JwtTokenService.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TinyHeroes.Application.Interfaces;
using TinyHeroes.Domain.Entities;

namespace TinyHeroes.Infrastructure.Services;

public class JwtTokenService(IConfiguration config) : ITokenService
{
    public string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Secret"]!));
        var expiry = int.Parse(config["Jwt:ExpiryMinutes"] ?? "60");

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim("displayName", user.DisplayName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiry),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

- [ ] **Step 4: Run test — expect PASS**

```bash
dotnet test TinyHeroes.Tests --filter "JwtTokenServiceTests" -v normal
```

Expected: 2 tests passing.

- [ ] **Step 5: Commit**

```bash
git add backend/TinyHeroes.Infrastructure/Services/JwtTokenService.cs \
        backend/TinyHeroes.Tests/Unit/JwtTokenServiceTests.cs
git commit -m "feat: JWT token service with tests"
```

---

### Task 5: Auth API endpoints (register + login)

**Files:**
- Create: `backend/TinyHeroes.Application/DTOs/Auth/RegisterRequest.cs`
- Create: `backend/TinyHeroes.Application/DTOs/Auth/LoginRequest.cs`
- Create: `backend/TinyHeroes.Application/DTOs/Auth/AuthResponse.cs`
- Create: `backend/TinyHeroes.Application/Services/AuthService.cs`
- Create: `backend/TinyHeroes.Api/Controllers/AuthController.cs`
- Create: `backend/TinyHeroes.Api/Middleware/ExceptionHandlingMiddleware.cs`
- Modify: `backend/TinyHeroes.Api/Program.cs`
- Create: `backend/TinyHeroes.Tests/Integration/AuthControllerTests.cs`

- [ ] **Step 1: Create DTOs in Application layer**

```csharp
// TinyHeroes.Application/DTOs/Auth/RegisterRequest.cs
namespace TinyHeroes.Application.DTOs.Auth;

public record RegisterRequest(string DisplayName, string Email, string Password);
```

```csharp
// TinyHeroes.Application/DTOs/Auth/LoginRequest.cs
namespace TinyHeroes.Application.DTOs.Auth;

public record LoginRequest(string Email, string Password);
```

```csharp
// TinyHeroes.Application/DTOs/Auth/AuthResponse.cs
namespace TinyHeroes.Application.DTOs.Auth;

public record AuthResponse(string AccessToken, string UserId, string DisplayName, string Email, bool HasFamily);
```

- [ ] **Step 2: Create `TinyHeroes.Application/Exceptions/NotFoundException.cs`**

```csharp
namespace TinyHeroes.Application.Exceptions;

public class NotFoundException(string message) : Exception(message);
```

```csharp
// TinyHeroes.Application/Exceptions/ForbiddenException.cs
namespace TinyHeroes.Application.Exceptions;

public class ForbiddenException(string message) : Exception(message);
```

- [ ] **Step 3: Write failing integration test**

```csharp
// TinyHeroes.Tests/Integration/AuthControllerTests.cs
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using TinyHeroes.Application.DTOs.Auth;

namespace TinyHeroes.Tests.Integration;

public class AuthControllerTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_WithValidData_Returns200WithToken()
    {
        var request = new RegisterRequest("Test User", $"test_{Guid.NewGuid()}@test.com", "Password123!");

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.DisplayName.Should().Be("Test User");
        body.HasFamily.Should().BeFalse();
    }

    [Fact]
    public async Task Login_WithValidCredentials_Returns200WithToken()
    {
        var email = $"login_{Guid.NewGuid()}@test.com";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("Login User", email, "Password123!"));

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "Password123!"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body!.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var email = $"wrong_{Guid.NewGuid()}@test.com";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("Wrong User", email, "Password123!"));

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "WrongPassword!"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

- [ ] **Step 4: Write `AuthController` (thin — delegates to services)**

```csharp
// TinyHeroes.Api/Controllers/AuthController.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TinyHeroes.Application.DTOs.Auth;
using TinyHeroes.Application.Interfaces;
using TinyHeroes.Domain.Entities;
using TinyHeroes.Infrastructure.Data;

namespace TinyHeroes.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    ITokenService tokenService,
    AppDbContext db) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest req)
    {
        var user = new User { DisplayName = req.DisplayName, Email = req.Email, UserName = req.Email };
        var result = await userManager.CreateAsync(user, req.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        var hasFamily = db.FamilyMembers.Any(m => m.UserId == user.Id);
        return Ok(new AuthResponse(tokenService.GenerateAccessToken(user), user.Id.ToString(), user.DisplayName, user.Email!, hasFamily));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest req)
    {
        var user = await userManager.FindByEmailAsync(req.Email);
        if (user is null) return Unauthorized();

        var result = await signInManager.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: false);
        if (!result.Succeeded) return Unauthorized();

        var hasFamily = db.FamilyMembers.Any(m => m.UserId == user.Id);
        return Ok(new AuthResponse(tokenService.GenerateAccessToken(user), user.Id.ToString(), user.DisplayName, user.Email!, hasFamily));
    }
}
```

- [ ] **Step 5: Create `ExceptionHandlingMiddleware`**

```csharp
// TinyHeroes.Api/Middleware/ExceptionHandlingMiddleware.cs
using System.Text.Json;
using TinyHeroes.Application.Exceptions;

namespace TinyHeroes.Api.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next)
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

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = message }));
        }
    }
}
```

- [ ] **Step 6: Wire up `Program.cs`**

```csharp
// TinyHeroes.Api/Program.cs
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using TinyHeroes.Api.Middleware;
using TinyHeroes.Domain.Entities;
using TinyHeroes.Infrastructure;
using TinyHeroes.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Clean Architecture DI registration
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddIdentity<User, IdentityRole<Guid>>(opt =>
{
    opt.Password.RequireDigit = true;
    opt.Password.RequiredLength = 8;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!);
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(opt =>
    opt.AddDefaultPolicy(p => p
        .WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()!)
        .AllowAnyMethod()
        .AllowAnyHeader()));

var app = builder.Build();

// Auto-migrate on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }  // needed for WebApplicationFactory in tests
```

- [ ] **Step 5: Make test project use InMemory DB (for integration tests without Docker)**

Modify `TinyHeroes.Tests/TinyHeroes.Tests.csproj` — add a `WebApplicationFactory` override:

```csharp
// TinyHeroes.Tests/Integration/TestWebApplicationFactory.cs
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TinyHeroes.Infrastructure.Data;

namespace TinyHeroes.Tests.Integration;

public class TestWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(opt =>
                opt.UseInMemoryDatabase("TestDb_" + Guid.NewGuid()));
        });
    }
}
```

Update `AuthControllerTests` to use `TestWebApplicationFactory<Program>` instead of `WebApplicationFactory<Program>`.

- [ ] **Step 6: Run tests — expect PASS**

```bash
cd backend
dotnet test TinyHeroes.Tests -v normal
```

Expected: 5 tests passing (2 token + 3 auth).

- [ ] **Step 7: Commit**

```bash
git add backend/
git commit -m "feat: register and login endpoints with integration tests"
```

---

### Task 6: Social login endpoints (Google, Apple, Facebook)

**Files:**
- Modify: `backend/TinyHeroes.Api/Controllers/AuthController.cs`
- Modify: `backend/TinyHeroes.Api/Program.cs`

- [ ] **Step 1: Add social auth providers in `Program.cs`** (add after `.AddJwtBearer(...)`)

```csharp
// Add inside the AddAuthentication chain:
.AddGoogle(opt =>
{
    opt.ClientId = builder.Configuration["Auth:Google:ClientId"]!;
    opt.ClientSecret = builder.Configuration["Auth:Google:ClientSecret"]!;
})
.AddFacebook(opt =>
{
    opt.AppId = builder.Configuration["Auth:Facebook:AppId"]!;
    opt.AppSecret = builder.Configuration["Auth:Facebook:AppSecret"]!;
})
.AddApple(opt =>
{
    opt.ClientId = builder.Configuration["Auth:Apple:ClientId"]!;
    opt.KeyId = builder.Configuration["Auth:Apple:KeyId"] ?? string.Empty;
    opt.TeamId = builder.Configuration["Auth:Apple:TeamId"] ?? string.Empty;
});
```

- [ ] **Step 2: Add social login endpoints to `AuthController`**

```csharp
// Add these two methods to AuthController:

[HttpGet("social/{provider}")]
public IActionResult SocialLogin(string provider, [FromQuery] string returnUrl = "/")
{
    var redirectUrl = Url.Action(nameof(SocialCallback), new { provider, returnUrl });
    var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
    return Challenge(properties, provider);
}

[HttpGet("social/{provider}/callback")]
public async Task<IActionResult> SocialCallback(string provider, [FromQuery] string returnUrl = "/")
{
    var info = await signInManager.GetExternalLoginInfoAsync();
    if (info is null) return BadRequest("External login info not found.");

    var email = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
    if (email is null) return BadRequest("Provider did not supply an email.");

    var user = await userManager.FindByEmailAsync(email);
    if (user is null)
    {
        var displayName = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? email;
        user = new User { DisplayName = displayName, Email = email, UserName = email };
        var createResult = await userManager.CreateAsync(user);
        if (!createResult.Succeeded) return BadRequest(createResult.Errors);
        await userManager.AddLoginAsync(user, info);
    }

    var hasFamily = db.FamilyMembers.Any(m => m.UserId == user.Id);
    var token = tokenService.GenerateAccessToken(user);
    // Redirect to frontend with token in query (frontend will extract and store)
    return Redirect($"http://localhost:4200/auth/callback?token={token}&hasFamily={hasFamily}");
}
```

- [ ] **Step 3: Verify build**

```bash
dotnet build TinyHeroes.sln
```

Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add backend/
git commit -m "feat: social login callbacks for Google, Apple, Facebook"
```

---

### Task 7: API Dockerfile

**Files:**
- Create: `backend/TinyHeroes.Api/Dockerfile`

- [ ] **Step 1: Create `backend/TinyHeroes.Api/Dockerfile`**

```dockerfile
# backend/TinyHeroes.Api/Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY TinyHeroes.sln .
COPY TinyHeroes.Domain/TinyHeroes.Domain.csproj TinyHeroes.Domain/
COPY TinyHeroes.Application/TinyHeroes.Application.csproj TinyHeroes.Application/
COPY TinyHeroes.Infrastructure/TinyHeroes.Infrastructure.csproj TinyHeroes.Infrastructure/
COPY TinyHeroes.Api/TinyHeroes.Api.csproj TinyHeroes.Api/
RUN dotnet restore TinyHeroes.Api/TinyHeroes.Api.csproj
COPY . .
RUN dotnet publish TinyHeroes.Api/TinyHeroes.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TinyHeroes.Api.dll"]
```

- [ ] **Step 2: Build the API Docker image**

```bash
cd backend
docker build -f TinyHeroes.Api/Dockerfile -t tinyheroes-api .
```

Expected: Successfully built image.

- [ ] **Step 3: Commit**

```bash
git add backend/TinyHeroes.Api/Dockerfile
git commit -m "chore: API Dockerfile"
```

---

### Task 8: Angular project

**Files:**
- Create: `frontend/` (Angular CLI project)
- Create: `frontend/tailwind.config.js`
- Create: `frontend/src/app/app.routes.ts`
- Create: `frontend/src/environments/environment.ts`
- Create: `frontend/src/assets/i18n/en.json`
- Create: `frontend/src/assets/i18n/hu.json`

- [ ] **Step 1: Scaffold Angular project**

```bash
cd ..   # back to TinyHeroes root
npx @angular/cli@latest new frontend \
  --routing=true \
  --style=css \
  --standalone \
  --skip-git \
  --skip-install=false
cd frontend
```

- [ ] **Step 2: Install dependencies**

```bash
npm install @ngx-translate/core @ngx-translate/http-loader
npm install -D tailwindcss postcss autoprefixer
npx tailwindcss init
```

- [ ] **Step 3: Configure TailwindCSS with TinyHeroes theme**

```js
// frontend/tailwind.config.js
/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ["./src/**/*.{html,ts}"],
  theme: {
    extend: {
      colors: {
        brand: {
          orange: '#F97316',
          green:  '#22C55E',
          purple: '#A855F7',
          cream:  '#FFF8F0',
          bg:     '#FFF3E8',
          border: '#F0E4D4',
          text:   '#3D2B1F',
          muted:  '#A8714A',
        }
      },
      fontFamily: {
        sans: ['-apple-system', 'BlinkMacSystemFont', '"Segoe UI"', 'sans-serif'],
      }
    }
  }
};
```

Add to `src/styles.css`:
```css
@tailwind base;
@tailwind components;
@tailwind utilities;

body { background-color: #FFF3E8; }
```

- [ ] **Step 4: Set up environments**

```ts
// frontend/src/environments/environment.ts
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000/api',
};
```

```ts
// frontend/src/environments/environment.prod.ts
export const environment = {
  production: true,
  apiUrl: '/api',
};
```

- [ ] **Step 5: Create i18n translation files**

```json
// frontend/src/assets/i18n/en.json
{
  "APP_NAME": "TinyHeroes",
  "TAGLINE": "Celebrate every good deed",
  "AUTH": {
    "GET_STARTED": "Get Started",
    "LOG_IN": "Log In",
    "SIGN_UP": "Create account",
    "EMAIL": "Email",
    "PASSWORD": "Password",
    "NAME": "Your name",
    "FORGOT_PASSWORD": "Forgot password?",
    "ALREADY_ACCOUNT": "Already have an account?",
    "NO_ACCOUNT": "Don't have an account?",
    "CONTINUE_GOOGLE": "Continue with Google",
    "CONTINUE_APPLE": "Continue with Apple",
    "CONTINUE_FACEBOOK": "Continue with Facebook",
    "OR": "or",
    "CREATE_ACCOUNT": "Create Account",
    "WELCOME_BACK": "Welcome back!"
  },
  "FAMILY": {
    "CREATE_TITLE": "Set up your family",
    "NAME_LABEL": "Family name",
    "NAME_PLACEHOLDER": "e.g. \"The Johnsons\"",
    "WEEK_STARTS": "Week starts on",
    "CREATE_CTA": "Create My Family",
    "DAYS": {
      "MON": "Mon", "TUE": "Tue", "WED": "Wed", "THU": "Thu",
      "FRI": "Fri", "SAT": "Sat", "SUN": "Sun"
    }
  }
}
```

```json
// frontend/src/assets/i18n/hu.json
{
  "APP_NAME": "TinyHeroes",
  "TAGLINE": "Ünnepelj minden jó cselekedetet",
  "AUTH": {
    "GET_STARTED": "Kezdjük el",
    "LOG_IN": "Bejelentkezés",
    "SIGN_UP": "Fiók létrehozása",
    "EMAIL": "E-mail",
    "PASSWORD": "Jelszó",
    "NAME": "Neved",
    "FORGOT_PASSWORD": "Elfelejtetted a jelszót?",
    "ALREADY_ACCOUNT": "Már van fiókod?",
    "NO_ACCOUNT": "Nincs még fiókod?",
    "CONTINUE_GOOGLE": "Folytatás Google-lal",
    "CONTINUE_APPLE": "Folytatás Apple-lel",
    "CONTINUE_FACEBOOK": "Folytatás Facebookkal",
    "OR": "vagy",
    "CREATE_ACCOUNT": "Fiók létrehozása",
    "WELCOME_BACK": "Üdvözlünk vissza!"
  },
  "FAMILY": {
    "CREATE_TITLE": "Állítsd be a családod",
    "NAME_LABEL": "Család neve",
    "NAME_PLACEHOLDER": "pl. \"A Kovács família\"",
    "WEEK_STARTS": "A hét kezdőnapja",
    "CREATE_CTA": "Család létrehozása",
    "DAYS": {
      "MON": "H", "TUE": "K", "WED": "Sze", "THU": "Cs",
      "FRI": "P", "SAT": "Szo", "SUN": "V"
    }
  }
}
```

- [ ] **Step 6: Configure `app.config.ts` with providers**

```ts
// frontend/src/app/app.config.ts
import { ApplicationConfig } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { TranslateLoader, TranslateModule } from '@ngx-translate/core';
import { TranslateHttpLoader } from '@ngx-translate/http-loader';
import { HttpClient } from '@angular/common/http';
import { importProvidersFrom } from '@angular/core';
import { routes } from './app.routes';
import { authInterceptor } from './core/auth/auth.interceptor';

export function createTranslateLoader(http: HttpClient) {
  return new TranslateHttpLoader(http, './assets/i18n/', '.json');
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    importProvidersFrom(
      TranslateModule.forRoot({
        defaultLanguage: 'en',
        loader: { provide: TranslateLoader, useFactory: createTranslateLoader, deps: [HttpClient] }
      })
    ),
  ],
};
```

- [ ] **Step 7: Define routes**

```ts
// frontend/src/app/app.routes.ts
import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./features/auth/pages/welcome.component').then(m => m.WelcomeComponent) },
  { path: 'login', loadComponent: () => import('./features/auth/pages/login.component').then(m => m.LoginComponent) },
  { path: 'signup', loadComponent: () => import('./features/auth/pages/signup.component').then(m => m.SignupComponent) },
  { path: 'auth/callback', loadComponent: () => import('./features/auth/pages/callback.component').then(m => m.CallbackComponent) },
  { path: 'create-family', canActivate: [authGuard], loadComponent: () => import('./features/auth/pages/create-family.component').then(m => m.CreateFamilyComponent) },
  { path: 'dashboard', canActivate: [authGuard], loadComponent: () => import('./features/dashboard/pages/home.component').then(m => m.HomeComponent) },
  { path: '**', redirectTo: '' }
];
```

- [ ] **Step 8: Verify Angular builds**

```bash
npm run build -- --configuration development
```

Expected: Build succeeds (may have template errors if components not yet created — that's fine).

- [ ] **Step 9: Commit**

```bash
cd ..
git add frontend/
git commit -m "feat: Angular project with Tailwind, ngx-translate, routing"
```

---

### Task 9: Angular auth service + interceptor + guard

**Files:**
- Create: `frontend/src/app/core/auth/auth.service.ts`
- Create: `frontend/src/app/core/auth/auth.guard.ts`
- Create: `frontend/src/app/core/auth/auth.interceptor.ts`
- Create: `frontend/src/app/core/models/user.model.ts`

- [ ] **Step 1: Create user model**

```ts
// frontend/src/app/core/models/user.model.ts
export interface AuthResponse {
  accessToken: string;
  userId: string;
  displayName: string;
  email: string;
  hasFamily: boolean;
}

export interface CurrentUser {
  userId: string;
  displayName: string;
  email: string;
  hasFamily: boolean;
}
```

- [ ] **Step 2: Create `auth.service.ts`**

```ts
// frontend/src/app/core/auth/auth.service.ts
import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, CurrentUser } from '../models/user.model';

const TOKEN_KEY = 'th_access_token';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private _user = signal<CurrentUser | null>(this.loadFromStorage());
  readonly user = this._user.asReadonly();

  constructor(private http: HttpClient, private router: Router) {}

  register(displayName: string, email: string, password: string) {
    return this.http.post<AuthResponse>(`${environment.apiUrl}/auth/register`, { displayName, email, password })
      .pipe(tap(res => this.handleAuthResponse(res)));
  }

  login(email: string, password: string) {
    return this.http.post<AuthResponse>(`${environment.apiUrl}/auth/login`, { email, password })
      .pipe(tap(res => this.handleAuthResponse(res)));
  }

  handleSocialCallback(token: string, hasFamily: boolean) {
    // Called from the /auth/callback route after social redirect
    localStorage.setItem(TOKEN_KEY, token);
    // Decode token to get user info (or fetch /auth/me — simpler approach below)
    this.router.navigate([hasFamily ? '/dashboard' : '/create-family']);
  }

  logout() {
    localStorage.removeItem(TOKEN_KEY);
    this._user.set(null);
    this.router.navigate(['/']);
  }

  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  isAuthenticated(): boolean {
    return !!this.getToken();
  }

  private handleAuthResponse(res: AuthResponse) {
    localStorage.setItem(TOKEN_KEY, res.accessToken);
    this._user.set({ userId: res.userId, displayName: res.displayName, email: res.email, hasFamily: res.hasFamily });
    this.router.navigate([res.hasFamily ? '/dashboard' : '/create-family']);
  }

  private loadFromStorage(): CurrentUser | null {
    const token = localStorage.getItem(TOKEN_KEY);
    if (!token) return null;
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return { userId: payload.sub, displayName: payload.displayName, email: payload.email, hasFamily: false };
    } catch { return null; }
  }
}
```

- [ ] **Step 3: Create `auth.guard.ts`**

```ts
// frontend/src/app/core/auth/auth.guard.ts
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.isAuthenticated()) return true;
  router.navigate(['/login']);
  return false;
};
```

- [ ] **Step 4: Create `auth.interceptor.ts`**

```ts
// frontend/src/app/core/auth/auth.interceptor.ts
import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = inject(AuthService).getToken();
  if (token) {
    req = req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
  }
  return next(req);
};
```

- [ ] **Step 5: Build and verify no errors**

```bash
cd frontend
npm run build -- --configuration development
```

- [ ] **Step 6: Commit**

```bash
cd ..
git add frontend/src/app/core/
git commit -m "feat: Angular auth service, guard, interceptor"
```

---

### Task 10: Angular auth screens (Welcome, Login, Sign Up, Callback)

**Files:**
- Create: `frontend/src/app/features/auth/pages/welcome.component.ts`
- Create: `frontend/src/app/features/auth/pages/login.component.ts`
- Create: `frontend/src/app/features/auth/pages/signup.component.ts`
- Create: `frontend/src/app/features/auth/pages/callback.component.ts`
- Create: `frontend/src/app/features/auth/components/social-buttons.component.ts`
- Create: `frontend/src/app/features/auth/components/email-form.component.ts`

Reference mockup: `.superpowers/brainstorm/1352-1778911830/content/screens-auth.html` — screens 1–3.

- [ ] **Step 1: Create `social-buttons.component.ts` (dumb component)**

```ts
// frontend/src/app/features/auth/components/social-buttons.component.ts
import { Component, output } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-social-buttons',
  imports: [TranslateModule],
  template: `
    <div class="flex flex-col gap-3 w-full">
      <a [href]="googleUrl" class="flex items-center justify-center gap-3 w-full py-3 px-4 bg-white border-2 border-brand-border rounded-xl font-bold text-brand-text hover:border-brand-orange transition">
        🔵 {{ 'AUTH.CONTINUE_GOOGLE' | translate }}
      </a>
      <a [href]="appleUrl" class="flex items-center justify-center gap-3 w-full py-3 px-4 bg-white border-2 border-brand-border rounded-xl font-bold text-brand-text hover:border-brand-orange transition">
        🍎 {{ 'AUTH.CONTINUE_APPLE' | translate }}
      </a>
      <a [href]="facebookUrl" class="flex items-center justify-center gap-3 w-full py-3 px-4 bg-white border-2 border-brand-border rounded-xl font-bold text-brand-text hover:border-brand-orange transition">
        🔷 {{ 'AUTH.CONTINUE_FACEBOOK' | translate }}
      </a>
    </div>
  `
})
export class SocialButtonsComponent {
  googleUrl = `${environment.apiUrl}/auth/social/Google`;
  appleUrl = `${environment.apiUrl}/auth/social/Apple`;
  facebookUrl = `${environment.apiUrl}/auth/social/Facebook`;
}
```

- [ ] **Step 2: Create `welcome.component.ts` (page/smart component)**

```ts
// frontend/src/app/features/auth/pages/welcome.component.ts
import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-welcome',
  imports: [RouterLink, TranslateModule],
  template: `
    <div class="min-h-screen bg-brand-cream flex flex-col items-center justify-between p-6">
      <div class="flex-1 flex flex-col items-center justify-center gap-6 w-full max-w-sm">
        <div class="text-center">
          <div class="text-6xl mb-3">🌟</div>
          <h1 class="text-3xl font-black text-brand-orange">{{ 'APP_NAME' | translate }}</h1>
          <p class="text-brand-muted text-sm mt-1">{{ 'TAGLINE' | translate }}</p>
        </div>
        <div class="grid grid-cols-3 gap-3 w-full">
          <div class="bg-white rounded-2xl p-3 text-center border border-brand-border">
            <div class="text-2xl mb-1">🏆</div>
            <div class="text-xs font-bold text-brand-muted leading-tight">Weekly Podium</div>
          </div>
          <div class="bg-white rounded-2xl p-3 text-center border border-brand-border">
            <div class="text-2xl mb-1">⭐</div>
            <div class="text-xs font-bold text-brand-muted leading-tight">Track Deeds</div>
          </div>
          <div class="bg-white rounded-2xl p-3 text-center border border-brand-border">
            <div class="text-2xl mb-1">🎁</div>
            <div class="text-xs font-bold text-brand-muted leading-tight">Win Prizes</div>
          </div>
        </div>
      </div>
      <div class="w-full max-w-sm flex flex-col gap-3">
        <a routerLink="/signup"
           class="block w-full text-center bg-gradient-to-r from-brand-orange to-orange-400 text-white font-bold py-3 rounded-2xl text-base">
          {{ 'AUTH.GET_STARTED' | translate }} ✨
        </a>
        <a routerLink="/login" class="block text-center text-brand-orange font-semibold text-sm">
          {{ 'AUTH.ALREADY_ACCOUNT' | translate }} {{ 'AUTH.LOG_IN' | translate }}
        </a>
      </div>
    </div>
  `
})
export class WelcomeComponent {}
```

- [ ] **Step 2: Create `login.component.ts`**

```ts
// frontend/src/app/features/auth/pages/login.component.ts
import { Component } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../../../core/auth/auth.service';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink, TranslateModule],
  template: `
    <div class="min-h-screen bg-brand-cream flex flex-col p-6 max-w-sm mx-auto">
      <div class="text-center mb-6 mt-8">
        <div class="text-3xl">🌟</div>
        <p class="text-lg font-black text-brand-orange">TinyHeroes</p>
        <h1 class="text-xl font-black text-brand-text mt-2">{{ 'AUTH.WELCOME_BACK' | translate }}</h1>
        <p class="text-brand-muted text-sm">Log in to see your family's deeds</p>
      </div>
      <div class="flex flex-col gap-3 mb-4">
        <a [href]="apiUrl + '/auth/social/Google'" class="flex items-center gap-3 bg-white border border-brand-border rounded-xl px-4 py-2.5 font-semibold text-sm text-brand-text">
          <svg class="w-4 h-4" viewBox="0 0 24 24"><path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/><path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/><path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"/><path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"/></svg>
          {{ 'AUTH.CONTINUE_GOOGLE' | translate }}
        </a>
      </div>
      <div class="flex items-center gap-3 mb-4">
        <div class="flex-1 h-px bg-brand-border"></div>
        <span class="text-brand-muted text-xs font-semibold">{{ 'AUTH.OR' | translate }}</span>
        <div class="flex-1 h-px bg-brand-border"></div>
      </div>
      <form [formGroup]="form" (ngSubmit)="submit()" class="flex flex-col gap-3">
        <div>
          <label class="text-xs font-bold text-brand-muted uppercase tracking-wide mb-1 block">{{ 'AUTH.EMAIL' | translate }}</label>
          <input formControlName="email" type="email" class="w-full bg-white border border-brand-border rounded-xl px-3 py-2.5 text-sm text-brand-text" />
        </div>
        <div>
          <label class="text-xs font-bold text-brand-muted uppercase tracking-wide mb-1 block">{{ 'AUTH.PASSWORD' | translate }}</label>
          <input formControlName="password" type="password" class="w-full bg-white border border-brand-border rounded-xl px-3 py-2.5 text-sm text-brand-text" />
        </div>
        <div class="text-right text-xs text-brand-orange font-semibold">{{ 'AUTH.FORGOT_PASSWORD' | translate }}</div>
        @if (error) { <p class="text-red-500 text-xs text-center">{{ error }}</p> }
        <button type="submit" [disabled]="form.invalid || loading"
          class="w-full bg-gradient-to-r from-brand-orange to-orange-400 text-white font-bold py-3 rounded-2xl">
          {{ loading ? '...' : ('AUTH.LOG_IN' | translate) }}
        </button>
      </form>
      <p class="text-center text-xs text-brand-orange font-semibold mt-4">
        <a routerLink="/signup">{{ 'AUTH.NO_ACCOUNT' | translate }} {{ 'AUTH.SIGN_UP' | translate }}</a>
      </p>
    </div>
  `
})
export class LoginComponent {
  form = inject(FormBuilder).nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });
  loading = false;
  error = '';
  apiUrl = environment.apiUrl;

  constructor(private auth: AuthService, private fb: FormBuilder) {}

  submit() {
    if (this.form.invalid) return;
    this.loading = true;
    this.error = '';
    const { email, password } = this.form.getRawValue();
    this.auth.login(email, password).subscribe({
      error: () => { this.error = 'Invalid email or password.'; this.loading = false; }
    });
  }
}
```

- [ ] **Step 3: Create `signup.component.ts`** — same pattern as login but calls `auth.register()` with name/email/password fields.

```ts
// frontend/src/app/features/auth/pages/signup.component.ts
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../../../core/auth/auth.service';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-signup',
  imports: [ReactiveFormsModule, RouterLink, TranslateModule],
  template: `
    <div class="min-h-screen bg-brand-cream flex flex-col p-6 max-w-sm mx-auto">
      <div class="text-center mb-5 mt-6">
        <div class="text-2xl">🌟</div>
        <p class="text-base font-black text-brand-orange">TinyHeroes</p>
        <h1 class="text-xl font-black text-brand-text mt-2">{{ 'AUTH.SIGN_UP' | translate }}</h1>
        <p class="text-brand-muted text-sm">Join and start celebrating good deeds</p>
      </div>
      <div class="flex flex-col gap-3 mb-4">
        <a [href]="apiUrl + '/auth/social/Google'" class="flex items-center gap-3 bg-white border border-brand-border rounded-xl px-4 py-2.5 font-semibold text-sm text-brand-text">
          <svg class="w-4 h-4" viewBox="0 0 24 24"><path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/><path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84"/><path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"/><path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"/></svg>
          {{ 'AUTH.CONTINUE_GOOGLE' | translate }}
        </a>
      </div>
      <div class="flex items-center gap-3 mb-4">
        <div class="flex-1 h-px bg-brand-border"></div>
        <span class="text-brand-muted text-xs font-semibold">{{ 'AUTH.OR' | translate }}</span>
        <div class="flex-1 h-px bg-brand-border"></div>
      </div>
      <form [formGroup]="form" (ngSubmit)="submit()" class="flex flex-col gap-3">
        <div>
          <label class="text-xs font-bold text-brand-muted uppercase tracking-wide mb-1 block">{{ 'AUTH.NAME' | translate }}</label>
          <input formControlName="displayName" type="text" class="w-full bg-white border border-brand-border rounded-xl px-3 py-2.5 text-sm text-brand-text" placeholder="e.g. Sarah Johnson"/>
        </div>
        <div>
          <label class="text-xs font-bold text-brand-muted uppercase tracking-wide mb-1 block">{{ 'AUTH.EMAIL' | translate }}</label>
          <input formControlName="email" type="email" class="w-full bg-white border border-brand-border rounded-xl px-3 py-2.5 text-sm text-brand-text" />
        </div>
        <div>
          <label class="text-xs font-bold text-brand-muted uppercase tracking-wide mb-1 block">{{ 'AUTH.PASSWORD' | translate }}</label>
          <input formControlName="password" type="password" class="w-full bg-white border border-brand-border rounded-xl px-3 py-2.5 text-sm text-brand-text" placeholder="At least 8 characters"/>
        </div>
        @if (error) { <p class="text-red-500 text-xs text-center">{{ error }}</p> }
        <button type="submit" [disabled]="form.invalid || loading"
          class="w-full bg-gradient-to-r from-brand-orange to-orange-400 text-white font-bold py-3 rounded-2xl mt-1">
          {{ loading ? '...' : ('AUTH.CREATE_ACCOUNT' | translate) }}
        </button>
      </form>
      <p class="text-center text-xs text-brand-orange font-semibold mt-4">
        <a routerLink="/login">{{ 'AUTH.ALREADY_ACCOUNT' | translate }} {{ 'AUTH.LOG_IN' | translate }}</a>
      </p>
    </div>
  `
})
export class SignupComponent {
  form = inject(FormBuilder).nonNullable.group({
    displayName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });
  loading = false;
  error = '';
  apiUrl = environment.apiUrl;

  constructor(private auth: AuthService) {}

  submit() {
    if (this.form.invalid) return;
    this.loading = true;
    this.error = '';
    const { displayName, email, password } = this.form.getRawValue();
    this.auth.register(displayName, email, password).subscribe({
      error: () => { this.error = 'Registration failed. Email may already be in use.'; this.loading = false; }
    });
  }
}
```

- [ ] **Step 4: Create `callback.component.ts`** (handles social login redirect)

```ts
// frontend/src/app/features/auth/pages/callback.component.ts
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-callback',
  template: `<div class="min-h-screen bg-brand-cream flex items-center justify-center"><p class="text-brand-muted">Signing in...</p></div>`
})
export class CallbackComponent implements OnInit {
  constructor(private route: ActivatedRoute, private auth: AuthService) {}

  ngOnInit() {
    const token = this.route.snapshot.queryParamMap.get('token');
    const hasFamily = this.route.snapshot.queryParamMap.get('hasFamily') === 'true';
    if (token) this.auth.handleSocialCallback(token, hasFamily);
  }
}
```

- [ ] **Step 5: Create empty `HomeComponent` stub (required by routes)**

```ts
// frontend/src/app/features/dashboard/pages/home.component.ts
import { Component } from '@angular/core';

@Component({
  selector: 'app-home',
  template: `<div class="min-h-screen bg-brand-cream flex items-center justify-center"><p class="text-brand-text font-bold text-xl">🏠 Dashboard — coming in Plan 2</p></div>`
})
export class HomeComponent {}
```

- [ ] **Step 6: Build and verify**

```bash
cd frontend
npm run build -- --configuration development
```

Expected: Build succeeds with 0 errors.

- [ ] **Step 7: Commit**

```bash
cd ..
git add frontend/
git commit -m "feat: Angular auth screens (welcome, login, signup, callback)"
```

---

### Task 11: Angular Dockerfile + nginx config

**Files:**
- Create: `frontend/Dockerfile`
- Create: `frontend/nginx.conf`

- [ ] **Step 1: Create `frontend/nginx.conf`**

```nginx
# frontend/nginx.conf
server {
    listen 80;
    root /usr/share/nginx/html;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }

    location /api/ {
        proxy_pass http://api:8080/api/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

- [ ] **Step 2: Create `frontend/Dockerfile`**

```dockerfile
# frontend/Dockerfile
FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build -- --configuration production

FROM nginx:alpine
COPY --from=build /app/dist/frontend/browser /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
```

- [ ] **Step 3: Test full stack with docker compose**

```bash
docker compose up --build -d
```

Expected: All 3 services start (postgres, api, frontend).

```bash
# Verify API health
curl http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"displayName":"Test","email":"smoke@test.com","password":"Password123!"}' \
  -w "\n%{http_code}"
```

Expected: `200` with JSON containing `accessToken`.

```bash
# Verify frontend
curl -s -o /dev/null -w "%{http_code}" http://localhost:4200
```

Expected: `200`.

- [ ] **Step 4: Commit**

```bash
git add frontend/Dockerfile frontend/nginx.conf
git commit -m "chore: Angular Dockerfile + nginx config, full stack runs in docker compose"
```

---

### Task 12: Create Family screen + API endpoint

**Files:**
- Create: `backend/TinyHeroes.Api/Controllers/FamilyController.cs`
- Create: `backend/TinyHeroes.Api/DTOs/CreateFamilyRequest.cs`
- Create: `frontend/src/app/features/auth/pages/create-family.component.ts`

Reference mockup: `.superpowers/brainstorm/1352-1778911830/content/screens-auth.html` — screen 4.

- [ ] **Step 1: Create `CreateFamilyRequest.cs`**

```csharp
// TinyHeroes.Application/DTOs/Family/CreateFamilyRequest.cs
namespace TinyHeroes.Application.DTOs.Family;

public record CreateFamilyRequest(string Name, DayOfWeek WeekStartDay);
public record FamilyResponse(Guid Id, string Name, DayOfWeek WeekStartDay);
```

- [ ] **Step 2: Create `FamilyController.cs`**

```csharp
// TinyHeroes.Api/Controllers/FamilyController.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TinyHeroes.Application.DTOs.Auth;
using TinyHeroes.Domain.Entities;
using TinyHeroes.Infrastructure.Data;

namespace TinyHeroes.Api.Controllers;

[ApiController]
[Route("api/families")]
[Authorize]
public class FamilyController(AppDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<FamilyResponse>> Create(CreateFamilyRequest req)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

        if (db.FamilyMembers.Any(m => m.UserId == userId))
            return Conflict("User already belongs to a family.");

        var family = new Family { Id = Guid.NewGuid(), Name = req.Name, WeekStartDay = req.WeekStartDay, CreatedByUserId = userId };
        var member = new FamilyMember { Id = Guid.NewGuid(), FamilyId = family.Id, UserId = userId, Role = FamilyRole.Admin };

        db.Families.Add(family);
        db.FamilyMembers.Add(member);
        await db.SaveChangesAsync();

        return Ok(new FamilyResponse(family.Id, family.Name, family.WeekStartDay));
    }
}
```

- [ ] **Step 3: Create `create-family.component.ts`**

```ts
// frontend/src/app/features/auth/pages/create-family.component.ts
import { Component } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { TranslateModule } from '@ngx-translate/core';
import { environment } from '../../../../environments/environment';

const DAYS: { key: DayOfWeek; label: string }[] = [
  { key: 1, label: 'Mon' }, { key: 2, label: 'Tue' }, { key: 3, label: 'Wed' },
  { key: 4, label: 'Thu' }, { key: 5, label: 'Fri' }, { key: 6, label: 'Sat' }, { key: 0, label: 'Sun' }
];
type DayOfWeek = 0 | 1 | 2 | 3 | 4 | 5 | 6;

@Component({
  selector: 'app-create-family',
  imports: [ReactiveFormsModule, TranslateModule],
  template: `
    <div class="min-h-screen bg-brand-cream flex flex-col items-center p-6">
      <div class="w-full max-w-sm mt-8">
        <div class="text-center mb-6">
          <div class="text-5xl mb-3">🏡</div>
          <h1 class="text-xl font-black text-brand-text">{{ 'FAMILY.CREATE_TITLE' | translate }}</h1>
          <p class="text-brand-muted text-sm mt-1">You can always change this later</p>
        </div>
        <form [formGroup]="form" (ngSubmit)="submit()" class="flex flex-col gap-4">
          <div>
            <label class="text-xs font-bold text-brand-muted uppercase tracking-wide mb-1 block">{{ 'FAMILY.NAME_LABEL' | translate }}</label>
            <input formControlName="name" type="text" class="w-full bg-white border border-brand-border rounded-xl px-3 py-2.5 text-sm text-brand-text" [placeholder]="'FAMILY.NAME_PLACEHOLDER' | translate"/>
          </div>
          <div>
            <label class="text-xs font-bold text-brand-muted uppercase tracking-wide mb-2 block">{{ 'FAMILY.WEEK_STARTS' | translate }}</label>
            <div class="flex gap-2 flex-wrap">
              @for (day of days; track day.key) {
                <button type="button" (click)="selectDay(day.key)"
                  [class]="selectedDay === day.key
                    ? 'bg-brand-bg border-2 border-brand-orange text-brand-orange font-bold text-sm rounded-xl px-3 py-1.5'
                    : 'bg-white border border-brand-border text-brand-muted font-semibold text-sm rounded-xl px-3 py-1.5'">
                  {{ day.label }}
                </button>
              }
            </div>
          </div>
          <div class="bg-brand-bg border border-brand-border rounded-xl p-3 text-xs text-brand-muted">
            💡 The weekly podium is shown at the end of each week. The monthly champion is declared on the last day of the month.
          </div>
          @if (error) { <p class="text-red-500 text-xs text-center">{{ error }}</p> }
          <button type="submit" [disabled]="form.invalid || loading"
            class="w-full bg-gradient-to-r from-brand-orange to-orange-400 text-white font-bold py-3 rounded-2xl text-base">
            {{ loading ? '...' : ('FAMILY.CREATE_CTA' | translate) }} 🏡
          </button>
        </form>
      </div>
    </div>
  `
})
export class CreateFamilyComponent {
  days = DAYS;
  selectedDay: DayOfWeek = 1;
  loading = false;
  error = '';

  form = inject(FormBuilder).nonNullable.group({ name: ['', Validators.required] });

  constructor(private fb: FormBuilder, private http: HttpClient, private router: Router) {}

  selectDay(day: DayOfWeek) { this.selectedDay = day; }

  submit() {
    if (this.form.invalid) return;
    this.loading = true;
    this.http.post(`${environment.apiUrl}/families`, { name: this.form.value.name, weekStartDay: this.selectedDay })
      .subscribe({ next: () => this.router.navigate(['/dashboard']), error: () => { this.error = 'Failed to create family.'; this.loading = false; } });
  }
}
```

- [ ] **Step 4: Build and run full stack — smoke test**

```bash
docker compose up --build -d
```

Open `http://localhost:4200` — verify welcome screen loads, can navigate to signup, create an account, and land on the create family screen.

- [ ] **Step 5: Commit**

```bash
git add .
git commit -m "feat: create family screen + API — Plan 1 complete"
```

---

## Self-Review

**Spec coverage check:**
- ✅ Welcome screen (screen 1)
- ✅ Sign up with social login + email (screen 2)
- ✅ Log in with social login + email (screen 3)
- ✅ Create family with name + week start day (screen 4)
- ✅ JWT auth
- ✅ Social login (Google, Apple, Facebook)
- ✅ i18n with English + Hungarian
- ✅ Fully containerized (docker compose)
- ✅ PostgreSQL with EF Core migrations
- ✅ TDD (token service + auth controller integration tests)
- ⏭️ Remaining 14 screens → Plans 2–5

**No placeholders detected.** All code steps contain full implementations.

**Type consistency:** `AuthResponse` DTO matches what `AuthController` returns; `AuthService` in Angular maps to the same shape. `FamilyRole` enum defined once in Core and referenced in `FamilyController`.
