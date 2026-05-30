using System.Net.Http.Headers;
using System.Net.Http.Json;
using TinyHeroes.Application.DTOs.Auth;
using TinyHeroes.Application.DTOs.Family;
using TinyHeroes.Domain.Enums;

namespace TinyHeroes.Tests.Integration.Helpers;

public static class TestAuthHelper
{
    public static async Task<HttpClient> RegisterWithFamily(TestWebApplicationFactory<Program> factory, string familyName = "Test Family", DayOfWeek weekStartDay = DayOfWeek.Monday)
    {
        var client = factory.CreateClient();
        var email = $"user_{Guid.NewGuid()}@test.com";
        var regResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("Parent", email, "Password123!"));
        var auth = await regResponse.Content.ReadFromJsonAsync<AuthResponse>(TestWebApplicationFactory<Program>.JsonOptions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        await client.PostAsJsonAsync("/api/families", new CreateFamilyRequest(familyName, weekStartDay));
        return client;
    }

    public static async Task<HttpClient> RegisterOnly(TestWebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        var email = $"user_{Guid.NewGuid()}@test.com";
        var regResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("Parent", email, "Password123!"));
        var auth = await regResponse.Content.ReadFromJsonAsync<AuthResponse>(TestWebApplicationFactory<Program>.JsonOptions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }
}
