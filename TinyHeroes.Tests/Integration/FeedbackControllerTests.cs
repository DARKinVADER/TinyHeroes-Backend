using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace TinyHeroes.Tests.Integration;

public class FeedbackControllerTests(TestWebApplicationFactory<Program> factory)
    : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Submit_WithValidBugReport_Returns204()
    {
        var response = await _client.PostAsJsonAsync("/api/feedback", new
        {
            category = "bug",
            message = "Something broke on the dashboard."
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Submit_WithValidIdeaAndEmail_Returns204()
    {
        var response = await _client.PostAsJsonAsync("/api/feedback", new
        {
            category = "idea",
            message = "It would be great to have a dark mode.",
            email = "user@example.com"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Submit_MissingMessage_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/feedback", new
        {
            category = "bug",
            message = ""
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Submit_MessageTooLong_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/feedback", new
        {
            category = "general",
            message = new string('x', 2001)
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Submit_InvalidCategory_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/feedback", new
        {
            category = "complaint",
            message = "This is not a valid category."
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Submit_InvalidEmailFormat_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/feedback", new
        {
            category = "bug",
            message = "Something is broken.",
            email = "not-an-email"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
