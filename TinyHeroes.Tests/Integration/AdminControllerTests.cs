using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TinyHeroes.Tests.Integration.Helpers;

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

        var initialResponse = await client.GetAsync("/api/admin/log-level");
        var initialBody = JsonSerializer.Deserialize<JsonElement>(
            await initialResponse.Content.ReadAsStringAsync(), JsonOpts);
        var initialLevel = initialBody.GetProperty("level").GetString()!;

        var setResponse = await client.PostAsJsonAsync("/api/admin/log-level", new { level = "Debug" });
        Assert.Equal(HttpStatusCode.OK, setResponse.StatusCode);

        var getResponse = await client.GetAsync("/api/admin/log-level");
        var body = JsonSerializer.Deserialize<JsonElement>(
            await getResponse.Content.ReadAsStringAsync(), JsonOpts);
        Assert.Equal("Debug", body.GetProperty("level").GetString());

        await client.PostAsJsonAsync("/api/admin/log-level", new { level = initialLevel });
    }

    [Fact]
    public async Task SetLogLevel_NonAdmin_Returns403()
    {
        var client = await TestAuthHelper.RegisterOnly(factory);
        var response = await client.PostAsJsonAsync("/api/admin/log-level", new { level = "Debug" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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
