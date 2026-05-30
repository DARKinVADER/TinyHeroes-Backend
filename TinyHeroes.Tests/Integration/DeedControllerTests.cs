using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TinyHeroes.Application.DTOs.Child;
using TinyHeroes.Application.DTOs.Deed;
using TinyHeroes.Domain.Enums;
using TinyHeroes.Tests.Integration.Helpers;

namespace TinyHeroes.Tests.Integration;

public class DeedControllerTests(TestWebApplicationFactory<Program> factory)
    : IClassFixture<TestWebApplicationFactory<Program>>
{
    [Fact]
    public async Task Create_WithValidChild_Returns200()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);
        var childResp = await client.PostAsJsonAsync("/api/children", new CreateChildRequest("Emma", 7, Gender.Girl, "🦄"));
        var child = await childResp.Content.ReadFromJsonAsync<ChildResponse>(TestWebApplicationFactory<Program>.JsonOptions);

        var response = await client.PostAsJsonAsync("/api/deeds", new CreateDeedRequest(child!.Id, "Helped with dishes", "🍽️"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var deed = await response.Content.ReadFromJsonAsync<DeedResponse>(TestWebApplicationFactory<Program>.JsonOptions);
        deed!.Description.Should().Be("Helped with dishes");
        deed.ImageValue.Should().Be("🍽️");
        deed.ChildId.Should().Be(child.Id);
    }

    [Fact]
    public async Task Create_WithChildNotInFamily_Returns404()
    {
        // Family A creates a child
        var clientA = await TestAuthHelper.RegisterWithFamily(factory);
        var childResp = await clientA.PostAsJsonAsync("/api/children", new CreateChildRequest("ChildA", 5, Gender.Boy, "🦁"));
        var childA = await childResp.Content.ReadFromJsonAsync<ChildResponse>(TestWebApplicationFactory<Program>.JsonOptions);

        // Family B tries to add deed to Family A's child
        var clientB = await TestAuthHelper.RegisterWithFamily(factory);
        var response = await clientB.PostAsJsonAsync("/api/deeds", new CreateDeedRequest(childA!.Id, "Good deed", "⭐"));
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_WithoutFamily_Returns400()
    {
        var client = await TestAuthHelper.RegisterOnly(factory);
        var response = await client.PostAsJsonAsync("/api/deeds", new CreateDeedRequest(Guid.NewGuid(), "Good deed", "⭐"));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListByChild_ReturnsDeeds()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);
        var childResp = await client.PostAsJsonAsync("/api/children", new CreateChildRequest("Liam", 6, Gender.Boy, "🐯"));
        var child = await childResp.Content.ReadFromJsonAsync<ChildResponse>(TestWebApplicationFactory<Program>.JsonOptions);

        await client.PostAsJsonAsync("/api/deeds", new CreateDeedRequest(child!.Id, "Deed 1", "⭐"));
        await client.PostAsJsonAsync("/api/deeds", new CreateDeedRequest(child.Id, "Deed 2", "🌟"));

        var response = await client.GetAsync($"/api/deeds?childId={child.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var deeds = await response.Content.ReadFromJsonAsync<List<DeedResponse>>(TestWebApplicationFactory<Program>.JsonOptions);
        deeds!.Count.Should().Be(2);
        // Ordered descending by CreatedAt, so last created first
        deeds[0].Description.Should().Be("Deed 2");
        deeds[1].Description.Should().Be("Deed 1");
    }

    [Fact]
    public async Task ListByChild_WhenChildNotInFamily_Returns404()
    {
        // Family A creates a child
        var clientA = await TestAuthHelper.RegisterWithFamily(factory);
        var childResp = await clientA.PostAsJsonAsync("/api/children", new CreateChildRequest("ChildA", 5, Gender.Boy, "🦁"));
        var childA = await childResp.Content.ReadFromJsonAsync<ChildResponse>(TestWebApplicationFactory<Program>.JsonOptions);

        // Family B tries to list deeds for Family A's child
        var clientB = await TestAuthHelper.RegisterWithFamily(factory);
        var response = await clientB.GetAsync($"/api/deeds?childId={childA!.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Stats_ReturnsCountsForAllChildren()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);

        var child1Resp = await client.PostAsJsonAsync("/api/children", new CreateChildRequest("Alice", 7, Gender.Girl, "🦄"));
        var child1 = await child1Resp.Content.ReadFromJsonAsync<ChildResponse>(TestWebApplicationFactory<Program>.JsonOptions);

        var child2Resp = await client.PostAsJsonAsync("/api/children", new CreateChildRequest("Bob", 5, Gender.Boy, "🦁"));
        var child2 = await child2Resp.Content.ReadFromJsonAsync<ChildResponse>(TestWebApplicationFactory<Program>.JsonOptions);

        // Add 2 deeds to child1, none to child2
        await client.PostAsJsonAsync("/api/deeds", new CreateDeedRequest(child1!.Id, "Deed A", "⭐"));
        await client.PostAsJsonAsync("/api/deeds", new CreateDeedRequest(child1.Id, "Deed B", "🌟"));

        var response = await client.GetAsync("/api/deeds/stats");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var stats = await response.Content.ReadFromJsonAsync<List<ChildStatsResponse>>(TestWebApplicationFactory<Program>.JsonOptions);
        stats!.Count.Should().Be(2);

        var child1Stats = stats.First(s => s.ChildId == child1.Id);
        child1Stats.TotalCount.Should().Be(2);

        var child2Stats = stats.First(s => s.ChildId == child2!.Id);
        child2Stats.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Stats_WeeklyCountMatchesTotal()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);
        var childResp = await client.PostAsJsonAsync("/api/children", new CreateChildRequest("Charlie", 8, Gender.Boy, "🐻"));
        var child = await childResp.Content.ReadFromJsonAsync<ChildResponse>(TestWebApplicationFactory<Program>.JsonOptions);

        await client.PostAsJsonAsync("/api/deeds", new CreateDeedRequest(child!.Id, "Today deed", "⭐"));

        var response = await client.GetAsync("/api/deeds/stats");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var stats = await response.Content.ReadFromJsonAsync<List<ChildStatsResponse>>(TestWebApplicationFactory<Program>.JsonOptions);
        var childStats = stats!.First(s => s.ChildId == child.Id);
        // Deed added today should be in this week's count
        childStats.WeeklyCount.Should().Be(1);
        childStats.WeeklyCount.Should().Be(childStats.TotalCount);
    }

    [Fact]
    public async Task CreateDeed_WithLibraryImage_Succeeds()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);
        var childResp = await client.PostAsJsonAsync("/api/children", new CreateChildRequest("Hero", 7, Gender.Boy, "🦸"));
        childResp.EnsureSuccessStatusCode();

        var childrenResp = await client.GetAsync("/api/children");
        var children = await childrenResp.Content.ReadFromJsonAsync<List<ChildResponse>>(TestWebApplicationFactory<Program>.JsonOptions);
        var childId = children![0].Id;

        var resp = await client.PostAsJsonAsync("/api/deeds", new CreateDeedRequest(childId, "Did homework", "📚", "library"));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var deed = await resp.Content.ReadFromJsonAsync<DeedResponse>(TestWebApplicationFactory<Program>.JsonOptions);
        deed!.ImageType.Should().Be("library");
        deed.ImageValue.Should().Be("📚");
    }

    [Fact]
    public async Task CreateDeed_WithAiImage_StoresDataUrl()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);
        var childResp = await client.PostAsJsonAsync("/api/children", new CreateChildRequest("Hero", 7, Gender.Boy, "🦸"));
        childResp.EnsureSuccessStatusCode();

        var childrenResp = await client.GetAsync("/api/children");
        var children = await childrenResp.Content.ReadFromJsonAsync<List<ChildResponse>>(TestWebApplicationFactory<Program>.JsonOptions);
        var childId = children![0].Id;

        var resp = await client.PostAsJsonAsync("/api/deeds", new CreateDeedRequest(childId, "Drew a picture", "data:image/jpeg;base64,FAKE", "ai"));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var deed = await resp.Content.ReadFromJsonAsync<DeedResponse>(TestWebApplicationFactory<Program>.JsonOptions);
        deed!.ImageType.Should().Be("ai");
    }

    [Fact]
    public async Task GenerateImage_WithValidPrompt_ReturnsDataUrl()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);
        var resp = await client.PostAsJsonAsync("/api/deeds/generate-image", new GenerateImageRequest("A child helping with dishes"));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await resp.Content.ReadFromJsonAsync<GenerateImageResponse>(TestWebApplicationFactory<Program>.JsonOptions);
        result!.DataUrl.Should().StartWith("data:image/");
    }

    [Fact]
    public async Task GenerateImage_WithEmptyPrompt_Returns400()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);
        var resp = await client.PostAsJsonAsync("/api/deeds/generate-image", new GenerateImageRequest(""));
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
