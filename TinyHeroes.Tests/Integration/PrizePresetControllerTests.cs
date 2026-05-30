using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TinyHeroes.Application.DTOs.Prize;
using TinyHeroes.Tests.Integration.Helpers;

namespace TinyHeroes.Tests.Integration;

public class PrizePresetControllerTests(TestWebApplicationFactory<Program> factory)
    : IClassFixture<TestWebApplicationFactory<Program>>
{
    [Fact]
    public async Task List_ReturnsSystemAndCustomPresets()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);

        // Create a custom prize preset
        await client.PostAsJsonAsync("/api/prize-presets", new CreatePrizePresetRequest("Custom prize", "🏆"));

        var response = await client.GetAsync("/api/prize-presets");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var presets = await response.Content.ReadFromJsonAsync<List<PrizePresetResponse>>(TestWebApplicationFactory<Program>.JsonOptions);
        presets.Should().NotBeNull();

        // 12 system presets + 1 custom
        presets!.Where(p => p.IsSystem).Should().HaveCount(12);
        presets.Any(p => !p.IsSystem && p.Label == "Custom prize").Should().BeTrue();
    }

    [Fact]
    public async Task Create_AsAdmin_Succeeds()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);

        var response = await client.PostAsJsonAsync("/api/prize-presets", new CreatePrizePresetRequest("Trampoline time", "🤸"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var preset = await response.Content.ReadFromJsonAsync<PrizePresetResponse>(TestWebApplicationFactory<Program>.JsonOptions);
        preset.Should().NotBeNull();
        preset!.Label.Should().Be("Trampoline time");
        preset.Emoji.Should().Be("🤸");
        preset.IsSystem.Should().BeFalse();

        // Verify it appears in list
        var listResponse = await client.GetAsync("/api/prize-presets");
        var presets = await listResponse.Content.ReadFromJsonAsync<List<PrizePresetResponse>>(TestWebApplicationFactory<Program>.JsonOptions);
        presets!.Any(p => p.Id == preset.Id && !p.IsSystem).Should().BeTrue();
    }

    [Fact]
    public async Task Create_WithoutFamily_Returns400()
    {
        var client = await TestAuthHelper.RegisterOnly(factory);

        var response = await client.PostAsJsonAsync("/api/prize-presets", new CreatePrizePresetRequest("Some prize", "⭐"));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_AsAdmin_Succeeds()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);

        // Create a custom preset
        var createResponse = await client.PostAsJsonAsync("/api/prize-presets", new CreatePrizePresetRequest("Delete me", "🗑️"));
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var preset = await createResponse.Content.ReadFromJsonAsync<PrizePresetResponse>(TestWebApplicationFactory<Program>.JsonOptions);

        // Delete it
        var deleteResponse = await client.DeleteAsync($"/api/prize-presets/{preset!.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify it's gone from list
        var listResponse = await client.GetAsync("/api/prize-presets");
        var presets = await listResponse.Content.ReadFromJsonAsync<List<PrizePresetResponse>>(TestWebApplicationFactory<Program>.JsonOptions);
        presets!.Any(p => p.Id == preset.Id).Should().BeFalse();
    }

    [Fact]
    public async Task Delete_SystemPreset_Returns403()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);

        // Try to delete a seeded system preset
        var systemPresetId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var response = await client.DeleteAsync($"/api/prize-presets/{systemPresetId}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
