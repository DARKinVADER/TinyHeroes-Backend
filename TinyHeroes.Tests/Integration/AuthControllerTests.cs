using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TinyHeroes.Application.DTOs.Auth;

namespace TinyHeroes.Tests.Integration;

public class AuthControllerTests(TestWebApplicationFactory<Program> factory)
    : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_WithValidData_Returns200WithToken()
    {
        var request = new RegisterRequest("Test User", $"test_{Guid.NewGuid()}@test.com", "Password123!");

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>(TestWebApplicationFactory<Program>.JsonOptions);
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
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>(TestWebApplicationFactory<Program>.JsonOptions);
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
