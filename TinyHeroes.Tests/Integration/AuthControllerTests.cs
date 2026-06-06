using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using TinyHeroes.Api.Services;
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

    [Fact]
    public async Task Login_WithFailingCaptcha_Returns400WithCaptchaFailed()
    {
        await using var failingFactory = new FailingCaptchaFactory<Program>();
        var client = failingFactory.CreateClient();
        var email = $"captcha_{Guid.NewGuid()}@test.com";

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(email, "Password123!", "bad-token"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("captcha_failed");
    }

    [Fact]
    public async Task Register_WithFailingCaptcha_Returns400WithCaptchaFailed()
    {
        await using var failingFactory = new FailingCaptchaFactory<Program>();
        var client = failingFactory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("Bot User", $"bot_{Guid.NewGuid()}@test.com", "Password123!", "bad-token"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("captcha_failed");
    }
}

// Factory variant that registers a captcha service which always returns false.
file class FailingCaptchaFactory<TProgram> : TestWebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICaptchaService));
            if (descriptor != null) services.Remove(descriptor);
            services.AddSingleton<ICaptchaService, AlwaysFailCaptchaService>();
        });
    }

    private class AlwaysFailCaptchaService : ICaptchaService
    {
        public Task<bool> ValidateAsync(string token, CancellationToken ct = default)
            => Task.FromResult(false);
    }
}
