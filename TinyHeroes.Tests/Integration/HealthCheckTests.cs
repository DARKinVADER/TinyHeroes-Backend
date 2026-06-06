// backend/TinyHeroes.Tests/Integration/HealthCheckTests.cs
using System.Net;
using FluentAssertions;

namespace TinyHeroes.Tests.Integration;

public class HealthCheckTests(TestWebApplicationFactory<Program> factory)
    : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetReady_ReturnsOk()
    {
        var response = await _client.GetAsync("/ready");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
