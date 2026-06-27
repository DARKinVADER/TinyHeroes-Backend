# TinyHeroes — Plan 8: AI Image Generation for Good Deeds

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add AI-generated images to the Add Good Deed flow — a second tab alongside the emoji library picker that lets parents describe a deed in words and generate a matching image via Hugging Face FLUX.1-schnell.

**Architecture:** Backend adds `IAiImageService` + `HuggingFaceImageService`, and a new `POST /api/deeds/generate-image` endpoint that calls Hugging Face, encodes the result as a base64 data URL, and returns it to the frontend. The frontend adds an "AI Generate" tab to the image picker modal in `add-deed.component.ts`. Generated images are stored as base64 data URLs in the existing `ImageValue` column with `ImageType = "ai"`. Deed display in `child-profile.component.ts` renders AI images via `<img>` and emoji via text node. No FluentStorage or file system changes — base64 in DB is acceptable for MVP.

**Tech Stack:** ASP.NET Core 10, Angular 21, Tailwind 4, ngx-translate, Hugging Face Inference API

---

## Context

Plans 1–7 complete. The `GoodDeed` entity already has `ImageType` (string, default `"library"`) and `ImageValue` (string, stores emoji). `appsettings.json` already has `AiImage.HuggingFace.ApiKey` and `AiImage.HuggingFace.Model` keys. The `Application/Interfaces/` layer has `ITokenService` and `ISummaryService` as pattern examples. `DependencyInjection.cs` already imports `Application.Interfaces`.

**Current `CreateDeedRequest`:** `record CreateDeedRequest(Guid ChildId, string Description, string ImageValue)` — missing `ImageType`.

**Hugging Face Inference API:**
- `POST https://api-inference.huggingface.co/models/{model}`
- Header: `Authorization: Bearer {API_KEY}`
- Body: `{"inputs": "{prompt}"}`
- Response: binary image bytes (JPEG/PNG), raw response body

---

## Task Overview (4 Tasks)

| # | Task | Layer |
|---|------|-------|
| 1 | IAiImageService + HuggingFaceImageService + DI | Backend |
| 2 | Generate-image endpoint + CreateDeedRequest update + tests | Backend |
| 3 | AI Generate tab in add-deed + DeedService update | Frontend |
| 4 | Deed image display + i18n + build | Frontend |

---

### Task 1: IAiImageService + HuggingFaceImageService + DI

**Files:**
- Create: `backend/TinyHeroes.Application/Interfaces/IAiImageService.cs`
- Create: `backend/TinyHeroes.Infrastructure/Services/HuggingFaceImageService.cs`
- Modify: `backend/TinyHeroes.Infrastructure/DependencyInjection.cs`

#### IAiImageService interface

```csharp
// backend/TinyHeroes.Application/Interfaces/IAiImageService.cs
namespace TinyHeroes.Application.Interfaces;

public interface IAiImageService
{
    /// <summary>
    /// Generates an image from a text prompt.
    /// Returns the image as a base64-encoded data URL (e.g., "data:image/jpeg;base64,...").
    /// Throws InvalidOperationException if the provider is not configured.
    /// </summary>
    Task<string> GenerateDataUrlAsync(string prompt, CancellationToken ct = default);
}
```

#### HuggingFaceImageService implementation

```csharp
// backend/TinyHeroes.Infrastructure/Services/HuggingFaceImageService.cs
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using TinyHeroes.Application.Interfaces;

namespace TinyHeroes.Infrastructure.Services;

public class HuggingFaceImageService(IHttpClientFactory httpFactory, IConfiguration config) : IAiImageService
{
    public async Task<string> GenerateDataUrlAsync(string prompt, CancellationToken ct = default)
    {
        var apiKey = config["AiImage:HuggingFace:ApiKey"];
        var model  = config["AiImage:HuggingFace:Model"] ?? "black-forest-labs/FLUX.1-schnell";

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Hugging Face API key is not configured.");

        var client = httpFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var url  = $"https://api-inference.huggingface.co/models/{model}";
        var body = new { inputs = prompt };

        var response = await client.PostAsJsonAsync(url, body, ct);
        response.EnsureSuccessStatusCode();

        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
        return $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";
    }
}
```

#### DependencyInjection.cs — add IHttpClientFactory + IAiImageService

```csharp
// Modified DependencyInjection.cs — add these two lines inside AddInfrastructure():
services.AddHttpClient();
services.AddScoped<IAiImageService, HuggingFaceImageService>();
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

        return services;
    }
}
```

**Commit:** `feat: IAiImageService interface + HuggingFace implementation`

---

### Task 2: Generate-Image Endpoint + CreateDeedRequest Update + Tests

**Files:**
- Modify: `backend/TinyHeroes.Application/DTOs/Deed/CreateDeedRequest.cs`
- Modify: `backend/TinyHeroes.Api/Controllers/DeedController.cs`
- Modify: `backend/TinyHeroes.Tests/Integration/DeedControllerTests.cs` (if it exists — if not, create it)

#### Update CreateDeedRequest

```csharp
// backend/TinyHeroes.Application/DTOs/Deed/CreateDeedRequest.cs
namespace TinyHeroes.Application.DTOs.Deed;

public record CreateDeedRequest(Guid ChildId, string Description, string ImageValue, string ImageType = "library");
public record GenerateImageRequest(string Prompt);
public record GenerateImageResponse(string DataUrl);
public record DeedResponse(Guid Id, Guid ChildId, string Description, string ImageType, string ImageValue, string AddedByName, DateTime CreatedAt);
public record ChildStatsResponse(Guid ChildId, int WeeklyCount, int TotalCount);
```

Note: `ImageType` has a default value of `"library"` to keep existing callers working.

#### Update DeedController.Create — use req.ImageType

In the `Create` method, change `ImageType = "library"` to `ImageType = req.ImageType`:

```csharp
var deed = new GoodDeed
{
    Id = Guid.NewGuid(),
    ChildId = req.ChildId,
    AddedByUserId = userId,
    Description = req.Description,
    ImageType = req.ImageType,   // was hardcoded "library"
    ImageValue = req.ImageValue,
    CreatedAt = DateTime.UtcNow
};
```

#### Add GenerateImage endpoint to DeedController

The controller signature must change from `DeedController(AppDbContext db)` to `DeedController(AppDbContext db, IAiImageService aiImageService)`:

```csharp
[ApiController]
[Route("api/deeds")]
[Authorize]
public class DeedController(AppDbContext db, IAiImageService aiImageService) : ControllerBase
{
    // ... existing methods unchanged ...

    [HttpPost("generate-image")]
    public async Task<ActionResult<GenerateImageResponse>> GenerateImage(GenerateImageRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Prompt))
            return BadRequest("Prompt is required.");

        try
        {
            var dataUrl = await aiImageService.GenerateDataUrlAsync(req.Prompt, ct);
            return Ok(new GenerateImageResponse(dataUrl));
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(503, ex.Message);
        }
    }
}
```

Add the using at the top of DeedController.cs:
```csharp
using TinyHeroes.Application.Interfaces;
```

#### Integration tests

Check if `backend/TinyHeroes.Tests/Integration/DeedControllerTests.cs` exists. If not, create it. The tests need a fake `IAiImageService` to avoid real HTTP calls. Register the fake in the test WebApplicationFactory.

**Approach:** In the test class, override the DI registration using `WebApplicationFactory.WithWebHostBuilder`:

```csharp
// backend/TinyHeroes.Tests/Integration/DeedControllerTests.cs
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TinyHeroes.Application.DTOs.Deed;
using TinyHeroes.Application.Interfaces;
using Xunit;

namespace TinyHeroes.Tests.Integration;

public class DeedControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DeedControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                // Replace real HuggingFaceImageService with a fake
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAiImageService));
                if (descriptor != null) services.Remove(descriptor);
                services.AddScoped<IAiImageService>(_ => new FakeAiImageService());
            }));
    }

    [Fact]
    public async Task CreateDeed_WithLibraryImage_Succeeds()
    {
        var (client, _) = await TestAuthHelper.RegisterWithFamily(_factory);
        var children = await client.GetFromJsonAsync<List<dynamic>>("/api/children");
        // create a child first
        var childResp = await client.PostAsJsonAsync("/api/children", new { name = "Hero", age = 7, gender = "Boy", avatarEmoji = "🦸" });
        var child = await childResp.Content.ReadFromJsonAsync<dynamic>();
        string childId = child!.id.ToString();

        var resp = await client.PostAsJsonAsync("/api/deeds", new { childId, description = "Did homework", imageValue = "📚", imageType = "library" });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var deed = await resp.Content.ReadFromJsonAsync<DeedResponse>();
        deed!.ImageType.Should().Be("library");
        deed.ImageValue.Should().Be("📚");
    }

    [Fact]
    public async Task CreateDeed_WithAiImage_StoresDataUrl()
    {
        var (client, _) = await TestAuthHelper.RegisterWithFamily(_factory);
        var childResp = await client.PostAsJsonAsync("/api/children", new { name = "Hero", age = 7, gender = "Boy", avatarEmoji = "🦸" });
        var child = await childResp.Content.ReadFromJsonAsync<dynamic>();
        string childId = child!.id.ToString();

        var resp = await client.PostAsJsonAsync("/api/deeds", new
        {
            childId,
            description = "Drew a picture",
            imageValue = "data:image/jpeg;base64,FAKE",
            imageType = "ai"
        });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var deed = await resp.Content.ReadFromJsonAsync<DeedResponse>();
        deed!.ImageType.Should().Be("ai");
    }

    [Fact]
    public async Task GenerateImage_WithValidPrompt_ReturnsDataUrl()
    {
        var (client, _) = await TestAuthHelper.RegisterWithFamily(_factory);

        var resp = await client.PostAsJsonAsync("/api/deeds/generate-image", new { prompt = "A child helping with dishes" });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await resp.Content.ReadFromJsonAsync<GenerateImageResponse>();
        result!.DataUrl.Should().StartWith("data:image/");
    }

    [Fact]
    public async Task GenerateImage_WithEmptyPrompt_Returns400()
    {
        var (client, _) = await TestAuthHelper.RegisterWithFamily(_factory);
        var resp = await client.PostAsJsonAsync("/api/deeds/generate-image", new { prompt = "" });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private class FakeAiImageService : IAiImageService
    {
        public Task<string> GenerateDataUrlAsync(string prompt, CancellationToken ct = default)
            => Task.FromResult("data:image/jpeg;base64,/9j/FAKEDATA==");
    }
}
```

Run tests: `cd backend && dotnet test`
Expected: all previous tests + 4 new deed tests pass.

**Commit:** `feat: generate-image endpoint, imageType in CreateDeedRequest, deed tests`

---

### Task 3: AI Generate Tab in Add-Deed + DeedService Update

**Files:**
- Modify: `frontend/src/app/core/services/deed.service.ts`
- Modify: `frontend/src/app/features/deeds/pages/add-deed.component.ts`

#### Update DeedService

Migrate from constructor DI to `inject()` pattern and add `generateImage()`:

```typescript
// frontend/src/app/core/services/deed.service.ts
import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { GoodDeed, CreateDeedRequest, ChildStats } from '../models/deed.model';

@Injectable({ providedIn: 'root' })
export class DeedService {
  private http = inject(HttpClient);

  private _stats = signal<ChildStats[]>([]);
  readonly stats = this._stats.asReadonly();

  loadStats() {
    return this.http.get<ChildStats[]>(`${environment.apiUrl}/deeds/stats`).subscribe({
      next: (stats) => this._stats.set(stats),
      error: () => this._stats.set([])
    });
  }

  getDeeds(childId: string) {
    return this.http.get<GoodDeed[]>(`${environment.apiUrl}/deeds?childId=${childId}`);
  }

  createDeed(req: CreateDeedRequest) {
    return this.http.post<GoodDeed>(`${environment.apiUrl}/deeds`, req);
  }

  generateImage(prompt: string) {
    return this.http.post<{ dataUrl: string }>(`${environment.apiUrl}/deeds/generate-image`, { prompt });
  }
}
```

Also update the `CreateDeedRequest` interface in `frontend/src/app/core/models/deed.model.ts` to include `imageType`:

```typescript
// In deed.model.ts, update the CreateDeedRequest interface:
export interface CreateDeedRequest {
  childId: string;
  description: string;
  imageValue: string;
  imageType: string;
}
```

Also ensure `GoodDeed` interface has `imageType`:
```typescript
export interface GoodDeed {
  id: string;
  childId: string;
  description: string;
  imageType: string;
  imageValue: string;
  addedByName: string;
  createdAt: string;
}
```

Check the existing deed.model.ts first and update accordingly.

#### Update add-deed.component.ts

Key changes:
- Add `imageType` signal: `'library' | 'ai'` (default `'library'`)
- Add `aiGeneratedDataUrl` signal: `string | null`
- Add `pickerTab` signal: `'library' | 'ai'` (controls which tab is shown in the picker)
- Add `aiPrompt` field (string)
- Add `aiLoading` signal (boolean)
- In the picker overlay, replace the single emoji grid with a tabbed view
- Add `generateAiImage()` method
- Update `save()` to pass `imageType`

Full updated component:

```typescript
import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { DeedService } from '../../../core/services/deed.service';
import { PresetService } from '../../../core/services/preset.service';
import { ChildService } from '../../../core/services/child.service';

@Component({
  selector: 'app-add-deed',
  imports: [FormsModule, RouterLink, TranslateModule],
  template: `
    @if (child(); as c) {
      <div class="p-4 max-w-lg mx-auto">
        <!-- Child Header -->
        <div class="flex items-center gap-3 mb-6">
          <div class="w-12 h-12 rounded-full bg-brand-cream flex items-center justify-center text-2xl">
            {{ c.avatarEmoji }}
          </div>
          <div>
            <h1 class="text-xl font-bold text-brand-text">{{ 'DEED.ADD_TITLE' | translate }}</h1>
            <p class="text-sm text-brand-muted">for {{ c.name }}</p>
          </div>
        </div>

        <!-- Quick Pick -->
        <div class="flex items-center justify-between mb-3">
          <h2 class="text-sm font-medium text-brand-text">{{ 'DEED.QUICK_PICK' | translate }}</h2>
          <a routerLink="/manage-presets" class="text-xs text-brand-orange font-medium">{{ 'DEED.MANAGE_LINK' | translate }}</a>
        </div>
        <div class="grid grid-cols-4 gap-2 mb-6">
          @for (preset of presetService.presets(); track preset.id) {
            <button (click)="selectPreset(preset)"
              [class]="'flex flex-col items-center gap-1 p-2 rounded-xl text-center transition-all ' +
                (preset.isSystem ? 'bg-white border border-brand-border' : 'bg-brand-purple/10 border border-brand-purple/30') +
                (selectedPresetId() === preset.id ? ' ring-2 ring-brand-orange' : '')">
              <span class="text-xl">{{ preset.imageValue }}</span>
              <span class="text-xs text-brand-text leading-tight line-clamp-2">{{ preset.label }}</span>
            </button>
          }
          <a routerLink="/manage-presets" class="flex flex-col items-center justify-center gap-1 p-2 rounded-xl bg-gray-50 border border-dashed border-brand-border text-brand-muted">
            <span class="text-xl">+</span>
            <span class="text-xs">New</span>
          </a>
        </div>

        <!-- Description -->
        <div class="mb-6">
          <label class="text-sm font-medium text-brand-text mb-2 block">{{ 'DEED.DESCRIPTION_LABEL' | translate }}</label>
          <input type="text" [(ngModel)]="description" [placeholder]="'DEED.DESCRIPTION_PLACEHOLDER' | translate"
            class="w-full px-4 py-3 rounded-xl border border-brand-border bg-white text-brand-text focus:outline-none focus:ring-2 focus:ring-brand-orange" />
        </div>

        <!-- Image Selection -->
        <div class="mb-6">
          <label class="text-sm font-medium text-brand-text mb-2 block">{{ 'DEED.IMAGE_LABEL' | translate }}</label>
          <div class="flex items-center gap-3">
            <div class="w-16 h-16 rounded-xl bg-white border border-brand-border flex items-center justify-center overflow-hidden shrink-0">
              @if (imageType() === 'ai' && aiGeneratedDataUrl()) {
                <img [src]="aiGeneratedDataUrl()!" class="w-full h-full object-cover" alt="Generated image" />
              } @else {
                <span class="text-3xl">{{ selectedEmoji() }}</span>
              }
            </div>
            <button (click)="showPicker.set(true)" class="text-sm text-brand-orange font-medium">{{ 'DEED.CHANGE_IMAGE' | translate }}</button>
          </div>
        </div>

        <!-- Image Picker Overlay -->
        @if (showPicker()) {
          <div class="fixed inset-0 bg-black/50 z-50 flex items-end">
            <div class="bg-white w-full max-h-[80vh] rounded-t-2xl flex flex-col">
              <!-- Header -->
              <div class="flex items-center justify-between p-4 border-b border-brand-border shrink-0">
                <h3 class="font-bold text-brand-text">{{ 'DEED.IMAGE_LABEL' | translate }}</h3>
                <button (click)="showPicker.set(false)" class="text-brand-muted text-xl">✕</button>
              </div>
              <!-- Tabs -->
              <div class="flex border-b border-brand-border shrink-0">
                <button (click)="pickerTab.set('library')"
                  [class]="'flex-1 py-2.5 text-sm font-medium transition-colors ' + (pickerTab() === 'library' ? 'text-brand-orange border-b-2 border-brand-orange' : 'text-brand-muted')">
                  📚 {{ 'DEED.TAB_LIBRARY' | translate }}
                </button>
                <button (click)="pickerTab.set('ai')"
                  [class]="'flex-1 py-2.5 text-sm font-medium transition-colors ' + (pickerTab() === 'ai' ? 'text-brand-orange border-b-2 border-brand-orange' : 'text-brand-muted')">
                  ✨ {{ 'DEED.TAB_AI' | translate }}
                </button>
              </div>
              <!-- Tab Content -->
              <div class="overflow-y-auto flex-1 p-4">
                @if (pickerTab() === 'library') {
                  @for (cat of emojiLibrary; track cat.category) {
                    <div class="mb-4">
                      <h4 class="text-xs font-medium text-brand-muted mb-2">{{ cat.category }}</h4>
                      <div class="grid grid-cols-8 gap-2">
                        @for (emoji of cat.emojis; track emoji) {
                          <button (click)="pickEmoji(emoji)"
                            class="w-10 h-10 rounded-lg flex items-center justify-center text-xl hover:bg-brand-cream">
                            {{ emoji }}
                          </button>
                        }
                      </div>
                    </div>
                  }
                } @else {
                  <!-- AI Generate tab -->
                  <p class="text-sm text-brand-muted mb-3">{{ 'DEED.AI_HINT' | translate }}</p>
                  <textarea [(ngModel)]="aiPrompt" rows="3"
                    [placeholder]="'DEED.AI_PROMPT_PLACEHOLDER' | translate"
                    class="w-full px-3 py-2 rounded-xl border border-brand-border bg-white text-brand-text text-sm focus:outline-none focus:ring-2 focus:ring-brand-orange resize-none mb-3">
                  </textarea>
                  <button (click)="generateAiImage()" [disabled]="!aiPrompt.trim() || aiLoading()"
                    class="w-full py-2.5 bg-brand-orange text-white font-bold rounded-full text-sm disabled:opacity-50 disabled:cursor-not-allowed mb-4">
                    @if (aiLoading()) {
                      <span>{{ 'DEED.AI_GENERATING' | translate }}</span>
                    } @else {
                      <span>✨ {{ 'DEED.AI_GENERATE' | translate }}</span>
                    }
                  </button>
                  @if (aiGeneratedDataUrl()) {
                    <div class="rounded-xl overflow-hidden border border-brand-border">
                      <img [src]="aiGeneratedDataUrl()!" class="w-full" alt="AI generated image" />
                    </div>
                    <button (click)="pickAiImage()" class="w-full mt-3 py-2.5 bg-white border-2 border-brand-orange text-brand-orange font-bold rounded-full text-sm">
                      {{ 'DEED.AI_USE_IMAGE' | translate }}
                    </button>
                  }
                }
              </div>
            </div>
          </div>
        }

        <!-- Save Button -->
        <button (click)="save()" [disabled]="!description.trim()"
          class="w-full py-3 bg-brand-orange text-white font-bold rounded-full text-lg disabled:opacity-50 disabled:cursor-not-allowed">
          {{ 'DEED.SAVE' | translate }}
        </button>
      </div>
    }
  `
})
export class AddDeedComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private deedService = inject(DeedService);
  presetService = inject(PresetService);
  private childService = inject(ChildService);

  description = '';
  aiPrompt = '';
  selectedEmoji = signal('⭐');
  selectedPresetId = signal<string | null>(null);
  imageType = signal<'library' | 'ai'>('library');
  aiGeneratedDataUrl = signal<string | null>(null);
  showPicker = signal(false);
  pickerTab = signal<'library' | 'ai'>('library');
  aiLoading = signal(false);

  child = computed(() => {
    const id = this.route.snapshot.paramMap.get('childId');
    return this.childService.children().find(c => c.id === id) ?? null;
  });

  emojiLibrary = [
    { category: '🏠 Home', emojis: ['🧹', '🛏️', '🍳', '🧺', '🪴', '🗑️', '🧽', '🚿'] },
    { category: '📚 Learning', emojis: ['📚', '✏️', '🎨', '🎵', '🔬', '📐', '💻', '📖'] },
    { category: '🏃 Activity', emojis: ['🏃', '⚽', '🚴', '🏊', '🧘', '💪', '🎯', '🏆'] },
    { category: '🤝 Social', emojis: ['🤝', '🫂', '💝', '🙏', '👋', '🎁', '😊', '🌟'] },
    { category: '⭐ Other', emojis: ['⭐', '🌈', '🎉', '👏', '💫', '🦸', '🏅', '❤️'] },
  ];

  ngOnInit() {
    this.presetService.loadPresets();
    if (this.childService.children().length === 0) {
      this.childService.loadChildren();
    }
  }

  selectPreset(preset: { id: string; label: string; imageValue: string }) {
    this.selectedPresetId.set(preset.id);
    this.description = preset.label;
    this.selectedEmoji.set(preset.imageValue);
    this.imageType.set('library');
    this.aiGeneratedDataUrl.set(null);
  }

  pickEmoji(emoji: string) {
    this.selectedEmoji.set(emoji);
    this.imageType.set('library');
    this.aiGeneratedDataUrl.set(null);
    this.showPicker.set(false);
  }

  generateAiImage() {
    if (!this.aiPrompt.trim()) return;
    this.aiLoading.set(true);
    this.deedService.generateImage(this.aiPrompt.trim()).subscribe({
      next: (res) => {
        this.aiGeneratedDataUrl.set(res.dataUrl);
        this.aiLoading.set(false);
      },
      error: () => {
        this.aiLoading.set(false);
      }
    });
  }

  pickAiImage() {
    this.imageType.set('ai');
    this.showPicker.set(false);
  }

  save() {
    const childId = this.route.snapshot.paramMap.get('childId');
    if (!childId || !this.description.trim()) return;
    const imageValue = this.imageType() === 'ai' ? (this.aiGeneratedDataUrl() ?? '⭐') : this.selectedEmoji();
    this.deedService.createDeed({
      childId,
      description: this.description.trim(),
      imageValue,
      imageType: this.imageType()
    }).subscribe(() => this.router.navigate(['/child', childId]));
  }
}
```

**Commit:** `feat: AI Generate tab in add-deed image picker`

---

### Task 4: Deed Image Display + i18n + Build

**Files:**
- Modify: `frontend/src/app/features/dashboard/pages/child-profile.component.ts`
- Modify: `frontend/public/assets/i18n/en.json`
- Modify: `frontend/public/assets/i18n/hu.json`

#### child-profile.component.ts — update deed image rendering

In the deed list, the current template renders `{{ deed.imageValue }}` in a text node. This works for emojis but breaks for base64 data URLs. Change the deed thumbnail to conditionally render `<img>` or emoji:

Current:
```html
<div class="w-10 h-10 rounded-lg bg-brand-cream flex items-center justify-center text-xl shrink-0">
  {{ deed.imageValue }}
</div>
```

Replace with:
```html
<div class="w-10 h-10 rounded-lg bg-brand-cream flex items-center justify-center text-xl shrink-0 overflow-hidden">
  @if (deed.imageType === 'ai') {
    <img [src]="deed.imageValue" class="w-full h-full object-cover" alt="" />
  } @else {
    {{ deed.imageValue }}
  }
</div>
```

Also add `TranslateModule` to the component's `imports` array if not already present.

#### i18n — en.json — DEED section additions

Add these keys to the `DEED` object:
```json
"TAB_LIBRARY": "Library",
"TAB_AI": "AI Generate",
"AI_HINT": "Describe the good deed and we'll generate a picture for it.",
"AI_PROMPT_PLACEHOLDER": "e.g. A child helping wash the dishes",
"AI_GENERATE": "Generate Image",
"AI_GENERATING": "Generating...",
"AI_USE_IMAGE": "Use this image"
```

#### i18n — hu.json — DEED section additions

Add these keys to the `DEED` object:
```json
"TAB_LIBRARY": "Könyvtár",
"TAB_AI": "AI kép",
"AI_HINT": "Írd le a jó cselekedetet, és generálunk egy képet hozzá.",
"AI_PROMPT_PLACEHOLDER": "pl. Egy gyerek segít mosogatni",
"AI_GENERATE": "Kép generálása",
"AI_GENERATING": "Generálás...",
"AI_USE_IMAGE": "Ezt a képet használom"
```

#### Verification

1. `cd backend && dotnet test`
   Expected: all tests pass (54 prior + 4 new deed tests = 58 total)

2. `cd frontend && npx ng build --configuration production`
   Expected: 0 errors

**Commit:** `feat: deed AI image display, i18n — Plan 8 complete`

---

## Verification Checklist

- [ ] Frontend builds with 0 errors (prod config)
- [ ] Backend tests: 58 passed
- [ ] Add Deed page shows two tabs in image picker: 📚 Library and ✨ AI Generate
- [ ] Library tab shows emoji grid (unchanged behavior)
- [ ] AI Generate tab shows prompt textarea + Generate button
- [ ] Loading state ("Generating...") shown while API call is in progress
- [ ] Generated image preview shown after API response
- [ ] "Use this image" button selects the AI image
- [ ] Image preview in the form shows the AI image (not emoji) when AI image is selected
- [ ] Deed saved with imageType="ai" and base64 dataUrl as imageValue
- [ ] Child profile deed list shows AI images as `<img>` thumbnails
- [ ] AI tab hidden / empty state when no image generated yet
- [ ] All new i18n keys in en.json and hu.json
