using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TinyHeroes.Application.DTOs.Preset;
using TinyHeroes.Tests.Integration.Helpers;

namespace TinyHeroes.Tests.Integration;

public class PresetControllerTests(TestWebApplicationFactory<Program> factory)
    : IClassFixture<TestWebApplicationFactory<Program>>
{
    [Fact]
    public async Task List_ReturnsSystemAndFamilyPresets()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);

        // Create a custom preset
        await client.PostAsJsonAsync("/api/presets", new CreatePresetRequest("Custom deed", "🌟"));

        var response = await client.GetAsync("/api/presets");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var presets = await response.Content.ReadFromJsonAsync<List<PresetResponse>>();
        presets.Should().NotBeNull();

        // System presets should appear
        presets!.Any(p => p.IsSystem).Should().BeTrue();
        // Custom preset should appear
        presets.Any(p => !p.IsSystem && p.Label == "Custom deed").Should().BeTrue();
    }

    [Fact]
    public async Task Create_AddsCustomPreset()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);

        var response = await client.PostAsJsonAsync("/api/presets", new CreatePresetRequest("Feed the cat", "🐱"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var preset = await response.Content.ReadFromJsonAsync<PresetResponse>();
        preset.Should().NotBeNull();
        preset!.Label.Should().Be("Feed the cat");
        preset.ImageValue.Should().Be("🐱");
        preset.IsSystem.Should().BeFalse();
        preset.Enabled.Should().BeTrue();

        // Verify it appears in list
        var listResponse = await client.GetAsync("/api/presets");
        var presets = await listResponse.Content.ReadFromJsonAsync<List<PresetResponse>>();
        presets!.Any(p => p.Id == preset.Id && !p.IsSystem).Should().BeTrue();
    }

    [Fact]
    public async Task Create_WithoutFamily_Returns400()
    {
        var client = await TestAuthHelper.RegisterOnly(factory);

        var response = await client.PostAsJsonAsync("/api/presets", new CreatePresetRequest("Some deed", "⭐"));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_CustomPreset_Succeeds()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);

        // Create a custom preset
        var createResponse = await client.PostAsJsonAsync("/api/presets", new CreatePresetRequest("Delete me", "🗑️"));
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var preset = await createResponse.Content.ReadFromJsonAsync<PresetResponse>();

        // Delete it
        var deleteResponse = await client.DeleteAsync($"/api/presets/{preset!.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify it's gone from list
        var listResponse = await client.GetAsync("/api/presets");
        var presets = await listResponse.Content.ReadFromJsonAsync<List<PresetResponse>>();
        presets!.Any(p => p.Id == preset.Id).Should().BeFalse();
    }

    [Fact]
    public async Task Delete_SystemPreset_Returns403()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);

        // Try to delete a seeded system preset
        var systemPresetId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var response = await client.DeleteAsync($"/api/presets/{systemPresetId}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
