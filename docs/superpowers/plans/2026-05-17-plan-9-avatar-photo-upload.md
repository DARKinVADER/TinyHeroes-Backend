# TinyHeroes — Plan 9: Child Avatar Photo Upload

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Allow parents to upload a custom photo as a child's avatar instead of choosing an emoji — photos are saved on disk and displayed throughout the app.

**Architecture:** Backend adds a nullable `AvatarUrl` column to `Child`, a `LocalFileStorageService` backed by `System.IO`, static file serving for the uploads directory, and a `POST /api/children/{id}/avatar` endpoint. The frontend adds a "Upload photo" option below the emoji grid in add-child; the child model gains `avatarUrl?: string`; every place that renders a child avatar conditionally shows `<img>` or emoji. No EF migration needed for tests (InMemory picks up schema from model); the column is additive and non-breaking for existing rows (nullable, no default required).

**Tech Stack:** ASP.NET Core 10, Angular 21, Tailwind 4, ngx-translate

---

## Context

Plans 1–9 in progress. `Child` entity has `AvatarEmoji` (emoji string). `ChildResponse` DTO exposes `AvatarEmoji`. No file storage infrastructure exists yet. `appsettings.json` already has `"Storage": { "ConnectionString": "disk://path=/app/uploads" }` — the path segment after `path=` is where uploads are stored. Frontend `Child` model only has `avatarEmoji`, `ChildService` uses constructor DI pattern (should be migrated to `inject()`).

**No EF migrations folder exists.** The tests use InMemory (`EnsureCreated()`) and pick up schema changes automatically. For a production Postgres DB the DBA runs `ALTER TABLE "Children" ADD COLUMN "AvatarUrl" TEXT NULL;` — this is out of scope for this plan.

**Static file serving:** `Program.cs` will serve files from the uploads directory using `app.UseStaticFiles(new StaticFileOptions { ... })`. Uploaded avatar files are written to `{storagePath}/avatars/{guid}.ext` and served at `/uploads/avatars/{guid}.ext`.

---

## Task Overview (4 Tasks)

| # | Task | Layer |
|---|------|-------|
| 1 | AvatarUrl field + IFileStorageService + static file serving | Backend |
| 2 | Avatar upload endpoint + tests | Backend |
| 3 | Photo upload UI in add-child + ChildService update | Frontend |
| 4 | Photo display throughout app + i18n + build | Frontend |

---

### Task 1: AvatarUrl Field + IFileStorageService + Static File Serving

**Files:**
- Modify: `backend/TinyHeroes.Domain/Entities/Child.cs`
- Modify: `backend/TinyHeroes.Application/DTOs/Child/CreateChildRequest.cs`
- Create: `backend/TinyHeroes.Application/Interfaces/IFileStorageService.cs`
- Create: `backend/TinyHeroes.Infrastructure/Services/LocalFileStorageService.cs`
- Modify: `backend/TinyHeroes.Infrastructure/DependencyInjection.cs`
- Modify: `backend/TinyHeroes.Api/Program.cs`
- Modify: `backend/TinyHeroes.Api/Controllers/ChildController.cs` (update projections only)

#### Child entity — add AvatarUrl

```csharp
// backend/TinyHeroes.Domain/Entities/Child.cs
using TinyHeroes.Domain.Enums;

namespace TinyHeroes.Domain.Entities;

public class Child
{
    public Guid Id { get; set; }
    public Guid FamilyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public Gender Gender { get; set; }
    public string AvatarEmoji { get; set; } = "🦸";
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Family Family { get; set; } = null!;
    public ICollection<GoodDeed> Deeds { get; set; } = [];
}
```

#### ChildResponse — add AvatarUrl

```csharp
// backend/TinyHeroes.Application/DTOs/Child/CreateChildRequest.cs
using TinyHeroes.Domain.Enums;

namespace TinyHeroes.Application.DTOs.Child;

public record CreateChildRequest(string Name, int Age, Gender Gender, string AvatarEmoji);
public record UpdateChildRequest(string Name, int Age, Gender Gender, string AvatarEmoji);
public record ChildResponse(Guid Id, string Name, int Age, Gender Gender, string AvatarEmoji, string? AvatarUrl);
```

#### Update ChildController projections

All `new ChildResponse(...)` calls must pass `child.AvatarUrl` as the last argument. Current calls:
- `Create`: `new ChildResponse(child.Id, child.Name, child.Age, child.Gender, child.AvatarEmoji)` → add `, child.AvatarUrl`
- `List` LINQ projection: `new ChildResponse(c.Id, c.Name, c.Age, c.Gender, c.AvatarEmoji)` → add `, c.AvatarUrl`
- `Get`: `new ChildResponse(child.Id, child.Name, child.Age, child.Gender, child.AvatarEmoji)` → add `, child.AvatarUrl`
- `Update`: `new ChildResponse(child.Id, child.Name, child.Age, child.Gender, child.AvatarEmoji)` → add `, child.AvatarUrl`

#### IFileStorageService interface

```csharp
// backend/TinyHeroes.Application/Interfaces/IFileStorageService.cs
namespace TinyHeroes.Application.Interfaces;

public interface IFileStorageService
{
    /// <summary>
    /// Saves the stream to the given subpath and returns the public URL path (e.g. "/uploads/avatars/guid.jpg").
    /// </summary>
    Task<string> SaveAsync(Stream content, string subPath, string fileName, CancellationToken ct = default);

    void Delete(string subPath, string fileName);
}
```

#### LocalFileStorageService implementation

```csharp
// backend/TinyHeroes.Infrastructure/Services/LocalFileStorageService.cs
using Microsoft.Extensions.Configuration;
using TinyHeroes.Application.Interfaces;

namespace TinyHeroes.Infrastructure.Services;

public class LocalFileStorageService(IConfiguration config) : IFileStorageService
{
    private string BasePath => ParseBasePath(config["Storage:ConnectionString"]);

    public async Task<string> SaveAsync(Stream content, string subPath, string fileName, CancellationToken ct = default)
    {
        var dir = Path.Combine(BasePath, subPath);
        Directory.CreateDirectory(dir);
        var fullPath = Path.Combine(dir, fileName);
        await using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        await content.CopyToAsync(fs, ct);
        return $"/uploads/{subPath}/{fileName}";
    }

    public void Delete(string subPath, string fileName)
    {
        var fullPath = Path.Combine(BasePath, subPath, fileName);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }

    private static string ParseBasePath(string? connectionString)
    {
        // Parses "disk://path=/app/uploads" → "/app/uploads"
        if (string.IsNullOrWhiteSpace(connectionString)) return "/app/uploads";
        var idx = connectionString.IndexOf("path=", StringComparison.OrdinalIgnoreCase);
        return idx >= 0 ? connectionString[(idx + 5)..] : "/app/uploads";
    }
}
```

#### DependencyInjection.cs — add IFileStorageService

```csharp
// Add inside AddInfrastructure():
services.AddScoped<IFileStorageService, LocalFileStorageService>();
```

Full updated file:
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

        services.AddHttpClient();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<ISummaryService, SummaryService>();
        services.AddScoped<IAiImageService, HuggingFaceImageService>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        return services;
    }
}
```

#### Program.cs — add static file serving for uploads

Add this block right after `app.UseMiddleware<ExceptionHandlingMiddleware>();`:

```csharp
// Serve uploaded files (avatars, etc.)
var uploadsPath = ParseUploadsPath(app.Configuration["Storage:ConnectionString"]);
Directory.CreateDirectory(uploadsPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});
```

Add this helper at the bottom of Program.cs (after `public partial class Program { }`):

```csharp
static string ParseUploadsPath(string? connectionString)
{
    if (string.IsNullOrWhiteSpace(connectionString)) return "/app/uploads";
    var idx = connectionString.IndexOf("path=", StringComparison.OrdinalIgnoreCase);
    return idx >= 0 ? connectionString[(idx + 5)..] : "/app/uploads";
}
```

Add required using at the top of Program.cs:
```csharp
using Microsoft.Extensions.FileProviders;
```

Run: `cd backend && dotnet build`
Expected: 0 errors.

**Commit:** `feat: AvatarUrl field, IFileStorageService, static file serving`

---

### Task 2: Avatar Upload Endpoint + Tests

**Files:**
- Modify: `backend/TinyHeroes.Api/Controllers/ChildController.cs`
- Modify: `backend/TinyHeroes.Tests/Integration/ChildControllerTests.cs` (add new tests)

#### Add UploadAvatar endpoint to ChildController

The controller signature must include `IFileStorageService`:
```csharp
public class ChildController(AppDbContext db, IFileStorageService fileStorage) : ControllerBase
```

Add using:
```csharp
using TinyHeroes.Application.Interfaces;
```

Add the endpoint:
```csharp
[HttpPost("{id:guid}/avatar")]
[RequestSizeLimit(5 * 1024 * 1024)] // 5 MB
public async Task<ActionResult<ChildResponse>> UploadAvatar(Guid id, IFormFile file, CancellationToken ct)
{
    var userId = GetUserId();
    var membership = await db.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
    if (membership is null) return BadRequest("User does not belong to a family.");

    var child = await db.Children.FirstOrDefaultAsync(c => c.Id == id && c.FamilyId == membership.FamilyId);
    if (child is null) return NotFound();

    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
    if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp"))
        return BadRequest("Only .jpg, .jpeg, .png, and .webp files are allowed.");

    // Delete previous avatar file if it was an upload
    if (!string.IsNullOrEmpty(child.AvatarUrl))
    {
        var oldFileName = Path.GetFileName(child.AvatarUrl);
        fileStorage.Delete("avatars", oldFileName);
    }

    var fileName = $"{Guid.NewGuid()}{ext}";
    await using var stream = file.OpenReadStream();
    var url = await fileStorage.SaveAsync(stream, "avatars", fileName, ct);

    child.AvatarUrl = url;
    await db.SaveChangesAsync();

    return Ok(new ChildResponse(child.Id, child.Name, child.Age, child.Gender, child.AvatarEmoji, child.AvatarUrl));
}
```

#### Tests

The tests need a `FakeFileStorageService` that stores files in memory. Register it in `TestWebApplicationFactory`:

```csharp
// Add to TestWebApplicationFactory.ConfigureWebHost ConfigureServices:
var fsDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IFileStorageService));
if (fsDescriptor != null) services.Remove(fsDescriptor);
services.AddScoped<IFileStorageService>(_ => new FakeFileStorageService());
```

Add FakeFileStorageService nested class:
```csharp
private class FakeFileStorageService : IFileStorageService
{
    public Task<string> SaveAsync(Stream content, string subPath, string fileName, CancellationToken ct = default)
        => Task.FromResult($"/uploads/{subPath}/{fileName}");
    public void Delete(string subPath, string fileName) { }
}
```

Add 3 new test methods to `ChildControllerTests.cs` (or create if not existing):

```csharp
[Fact]
public async Task UploadAvatar_WithValidJpeg_SetsAvatarUrl()
{
    var (client, _) = await TestAuthHelper.RegisterWithFamily(_factory);
    var childResp = await client.PostAsJsonAsync("/api/children", new { name = "Hero", age = 7, gender = "Boy", avatarEmoji = "🦸" });
    var child = await childResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
    var childId = child.GetProperty("id").GetString()!;

    using var content = new MultipartFormDataContent();
    var imageBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }; // JPEG magic bytes
    content.Add(new ByteArrayContent(imageBytes) { Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg") } }, "file", "avatar.jpg");

    var resp = await client.PostAsync($"/api/children/{childId}/avatar", content);
    resp.StatusCode.Should().Be(HttpStatusCode.OK);

    var updated = await resp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
    updated.GetProperty("avatarUrl").GetString().Should().Contain("/uploads/avatars/");
}

[Fact]
public async Task UploadAvatar_WithInvalidExtension_Returns400()
{
    var (client, _) = await TestAuthHelper.RegisterWithFamily(_factory);
    var childResp = await client.PostAsJsonAsync("/api/children", new { name = "Hero", age = 7, gender = "Boy", avatarEmoji = "🦸" });
    var child = await childResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
    var childId = child.GetProperty("id").GetString()!;

    using var content = new MultipartFormDataContent();
    content.Add(new ByteArrayContent([0x00]) { Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream") } }, "file", "avatar.gif");

    var resp = await client.PostAsync($"/api/children/{childId}/avatar", content);
    resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
}

[Fact]
public async Task UploadAvatar_ForOtherFamilyChild_Returns404()
{
    var (client1, _) = await TestAuthHelper.RegisterWithFamily(_factory);
    var (client2, _) = await TestAuthHelper.RegisterWithFamily(_factory);

    var childResp = await client1.PostAsJsonAsync("/api/children", new { name = "Hero", age = 7, gender = "Boy", avatarEmoji = "🦸" });
    var child = await childResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
    var childId = child.GetProperty("id").GetString()!;

    using var content = new MultipartFormDataContent();
    content.Add(new ByteArrayContent([0xFF, 0xD8]) { Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg") } }, "file", "avatar.jpg");

    var resp = await client2.PostAsync($"/api/children/{childId}/avatar", content);
    resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
}
```

Run: `cd backend && dotnet test`
Expected: all prior tests + 3 new = 61 passing.

**Commit:** `feat: avatar upload endpoint with file validation and tests`

---

### Task 3: Photo Upload UI in Add-Child + ChildService Update

**Files:**
- Modify: `frontend/src/app/core/models/child.model.ts`
- Modify: `frontend/src/app/core/services/child.service.ts`
- Modify: `frontend/src/app/features/dashboard/pages/add-child.component.ts`

#### Update child.model.ts

```typescript
export interface Child {
  id: string;
  name: string;
  age: number;
  gender: string;
  avatarEmoji: string;
  avatarUrl?: string;
}

export interface CreateChildRequest {
  name: string;
  age: number;
  gender: string;
  avatarEmoji: string;
}
```

#### Update child.service.ts

Migrate to `inject()` pattern and add `uploadAvatar()`:

```typescript
import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { Child, CreateChildRequest } from '../models/child.model';

@Injectable({ providedIn: 'root' })
export class ChildService {
  private http = inject(HttpClient);

  private _children = signal<Child[]>([]);
  readonly children = this._children.asReadonly();

  loadChildren() {
    return this.http.get<Child[]>(`${environment.apiUrl}/children`).subscribe({
      next: (children) => this._children.set(children),
      error: () => this._children.set([])
    });
  }

  createChild(req: CreateChildRequest) {
    return this.http.post<Child>(`${environment.apiUrl}/children`, req);
  }

  uploadAvatar(childId: string, file: File) {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<Child>(`${environment.apiUrl}/children/${childId}/avatar`, formData);
  }
}
```

#### Update add-child.component.ts

Add a photo upload option below the emoji grid. When a photo is selected, it replaces the emoji avatar selection. On submit, create child then upload avatar if a photo was chosen.

```typescript
import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { ChildService } from '../../../core/services/child.service';

@Component({
  selector: 'app-add-child',
  imports: [FormsModule, TranslateModule],
  template: `
    <div class="p-4 max-w-lg mx-auto">
      <h1 class="text-2xl font-bold text-brand-text mb-6">{{ 'CHILD.ADD_TITLE' | translate }}</h1>

      <!-- Avatar Picker -->
      <div class="mb-6">
        <label class="text-sm font-medium text-brand-text mb-2 block">{{ 'CHILD.CHOOSE_AVATAR' | translate }}</label>

        <!-- Photo preview or emoji grid -->
        @if (photoPreviewUrl()) {
          <div class="flex items-center gap-4 mb-3">
            <div class="w-20 h-20 rounded-full overflow-hidden border-4 border-brand-orange shrink-0">
              <img [src]="photoPreviewUrl()!" class="w-full h-full object-cover" alt="Avatar preview" />
            </div>
            <div>
              <p class="text-sm font-medium text-brand-text">{{ 'CHILD.PHOTO_SELECTED' | translate }}</p>
              <button (click)="clearPhoto()" class="text-xs text-brand-orange mt-1">{{ 'CHILD.REMOVE_PHOTO' | translate }}</button>
            </div>
          </div>
        } @else {
          <div class="grid grid-cols-6 gap-3 mb-3">
            @for (emoji of avatars; track emoji) {
              <button (click)="selectedAvatar.set(emoji)"
                [class]="'w-12 h-12 rounded-full flex items-center justify-center text-2xl transition-all ' +
                  (selectedAvatar() === emoji ? 'ring-3 ring-brand-orange bg-brand-cream scale-110' : 'bg-white border border-brand-border hover:scale-105')">
                {{ emoji }}
              </button>
            }
          </div>
        }

        <!-- Upload photo option -->
        <label class="flex items-center gap-2 cursor-pointer text-sm text-brand-orange font-medium">
          <input type="file" accept="image/jpeg,image/png,image/webp" class="hidden" (change)="onFileSelected($event)" #fileInput />
          <span>📷 {{ 'CHILD.UPLOAD_PHOTO' | translate }}</span>
        </label>
      </div>

      <!-- Name Input -->
      <div class="mb-6">
        <label class="text-sm font-medium text-brand-text mb-2 block">{{ 'CHILD.NAME_LABEL' | translate }}</label>
        <input type="text" [(ngModel)]="name" [placeholder]="'CHILD.NAME_PLACEHOLDER' | translate"
          class="w-full px-4 py-3 rounded-xl border border-brand-border bg-white text-brand-text focus:outline-none focus:ring-2 focus:ring-brand-orange" />
      </div>

      <!-- Age Spinner -->
      <div class="mb-6">
        <label class="text-sm font-medium text-brand-text mb-2 block">{{ 'CHILD.AGE_LABEL' | translate }}</label>
        <div class="flex items-center gap-4">
          <button (click)="decrementAge()"
            class="w-10 h-10 rounded-full bg-white border border-brand-border flex items-center justify-center text-xl font-bold text-brand-text hover:bg-brand-cream">&minus;</button>
          <span class="text-2xl font-bold text-brand-text w-8 text-center">{{ age() }}</span>
          <button (click)="incrementAge()"
            class="w-10 h-10 rounded-full bg-white border border-brand-border flex items-center justify-center text-xl font-bold text-brand-text hover:bg-brand-cream">+</button>
        </div>
      </div>

      <!-- Gender Selector -->
      <div class="mb-8">
        <label class="text-sm font-medium text-brand-text mb-2 block">{{ 'CHILD.GENDER_LABEL' | translate }}</label>
        <div class="flex gap-3">
          <button (click)="gender.set('Boy')"
            [class]="'flex-1 py-3 rounded-xl font-medium text-center transition-all ' +
              (gender() === 'Boy' ? 'bg-brand-orange text-white' : 'bg-white border border-brand-border text-brand-text')">
            {{ 'CHILD.BOY' | translate }}
          </button>
          <button (click)="gender.set('Girl')"
            [class]="'flex-1 py-3 rounded-xl font-medium text-center transition-all ' +
              (gender() === 'Girl' ? 'bg-brand-orange text-white' : 'bg-white border border-brand-border text-brand-text')">
            {{ 'CHILD.GIRL' | translate }}
          </button>
        </div>
      </div>

      <!-- Submit Button -->
      <button (click)="submit()" [disabled]="!name.trim() || saving()"
        class="w-full py-3 bg-brand-orange text-white font-bold rounded-full text-lg disabled:opacity-50 disabled:cursor-not-allowed">
        {{ 'CHILD.ADD_CTA' | translate }}
      </button>
    </div>
  `
})
export class AddChildComponent {
  private childService = inject(ChildService);
  private router = inject(Router);

  avatars = ['🦸', '🦹', '🧙', '🧚', '🧜', '🦄', '🐉', '🦁', '🐯', '🦊', '🐼', '🐨'];
  selectedAvatar = signal('🦸');
  name = '';
  age = signal(5);
  gender = signal<'Boy' | 'Girl'>('Boy');
  photoFile = signal<File | null>(null);
  photoPreviewUrl = signal<string | null>(null);
  saving = signal(false);

  incrementAge() { if (this.age() < 16) this.age.update(a => a + 1); }
  decrementAge() { if (this.age() > 2) this.age.update(a => a - 1); }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    this.photoFile.set(file);
    const reader = new FileReader();
    reader.onload = (e) => this.photoPreviewUrl.set(e.target?.result as string);
    reader.readAsDataURL(file);
  }

  clearPhoto() {
    this.photoFile.set(null);
    this.photoPreviewUrl.set(null);
  }

  submit() {
    if (!this.name.trim() || this.saving()) return;
    this.saving.set(true);
    this.childService.createChild({
      name: this.name.trim(),
      age: this.age(),
      gender: this.gender(),
      avatarEmoji: this.selectedAvatar()
    }).subscribe({
      next: (child) => {
        const file = this.photoFile();
        if (file) {
          this.childService.uploadAvatar(child.id, file).subscribe({
            next: () => this.router.navigate(['/dashboard']),
            error: () => this.router.navigate(['/dashboard'])
          });
        } else {
          this.router.navigate(['/dashboard']);
        }
      },
      error: () => this.saving.set(false)
    });
  }
}
```

**Commit:** `feat: photo upload in add-child, ChildService inject() migration`

---

### Task 4: Photo Display Throughout App + i18n + Build

**Files:**
- Modify: `frontend/src/app/features/dashboard/pages/home.component.ts`
- Modify: `frontend/src/app/features/dashboard/pages/child-profile.component.ts`
- Modify: `frontend/src/app/features/deeds/pages/add-deed.component.ts`
- Modify: `frontend/public/assets/i18n/en.json`
- Modify: `frontend/public/assets/i18n/hu.json`

#### Helper: avatar rendering pattern

Wherever a child avatar is displayed, use this pattern (adjust size classes as needed):
- If `child.avatarUrl` is set: `<img [src]="child.avatarUrl" class="w-full h-full object-cover rounded-full" alt="" />`
- Otherwise: `{{ child.avatarEmoji }}`

All avatar containers need `overflow-hidden`.

#### home.component.ts — child card avatar

Current:
```html
<div class="w-12 h-12 rounded-full bg-brand-cream flex items-center justify-center text-2xl">
  {{ child.avatarEmoji }}
</div>
```

Replace with:
```html
<div class="w-12 h-12 rounded-full bg-brand-cream flex items-center justify-center text-2xl overflow-hidden">
  @if (child.avatarUrl) {
    <img [src]="child.avatarUrl" class="w-full h-full object-cover" alt="" />
  } @else {
    {{ child.avatarEmoji }}
  }
</div>
```

#### child-profile.component.ts — hero header avatar

Current:
```html
<div class="w-24 h-24 rounded-full bg-brand-cream border-4 border-brand-orange flex items-center justify-center text-5xl mb-3">
  {{ c.avatarEmoji }}
</div>
```

Replace with:
```html
<div class="w-24 h-24 rounded-full bg-brand-cream border-4 border-brand-orange flex items-center justify-center text-5xl mb-3 overflow-hidden">
  @if (c.avatarUrl) {
    <img [src]="c.avatarUrl" class="w-full h-full object-cover" alt="" />
  } @else {
    {{ c.avatarEmoji }}
  }
</div>
```

#### add-deed.component.ts — child header avatar

Current:
```html
<div class="w-12 h-12 rounded-full bg-brand-cream flex items-center justify-center text-2xl">
  {{ c.avatarEmoji }}
</div>
```

Replace with:
```html
<div class="w-12 h-12 rounded-full bg-brand-cream flex items-center justify-center text-2xl overflow-hidden">
  @if (c.avatarUrl) {
    <img [src]="c.avatarUrl" class="w-full h-full object-cover" alt="" />
  } @else {
    {{ c.avatarEmoji }}
  }
</div>
```

#### i18n — en.json — CHILD section additions

Add 3 keys to the `CHILD` object:
```json
"PHOTO_SELECTED": "Photo selected",
"REMOVE_PHOTO": "Remove photo",
"UPLOAD_PHOTO": "Upload a photo"
```

#### i18n — hu.json — CHILD section additions

Add 3 keys to the `CHILD` object:
```json
"PHOTO_SELECTED": "Fotó kiválasztva",
"REMOVE_PHOTO": "Fotó eltávolítása",
"UPLOAD_PHOTO": "Fotó feltöltése"
```

#### Verification

1. `cd backend && dotnet test`
   Expected: 61 tests pass

2. `cd frontend && npx ng build --configuration production`
   Expected: 0 errors

**Commit:** `feat: photo avatar display throughout app, i18n — Plan 9 complete`

---

## Verification Checklist

- [ ] Frontend builds with 0 errors (prod config)
- [ ] Backend tests: 61 passed
- [ ] `Child` entity has nullable `AvatarUrl` property
- [ ] `ChildResponse` includes `avatarUrl?: string` (camelCase in JSON)
- [ ] `GET /api/children` returns `avatarUrl` field for each child
- [ ] `POST /api/children/{id}/avatar` accepts JPEG/PNG/WebP, rejects others (400)
- [ ] Uploaded files are stored and accessible at `/uploads/avatars/{filename}`
- [ ] Add Child page shows emoji grid + "Upload a photo" option
- [ ] When photo selected: preview shown, emoji grid hidden, "Remove photo" option available
- [ ] After save with photo: child has `avatarUrl` populated, dashboard shows photo
- [ ] Dashboard, Child Profile, Add Deed header show photo if `avatarUrl` set, emoji otherwise
- [ ] All new i18n keys in en.json and hu.json
