using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TinyHeroes.Application.DTOs.User;
using TinyHeroes.Tests.Integration.Helpers;

namespace TinyHeroes.Tests.Integration;

public class UserControllerTests(TestWebApplicationFactory<Program> factory)
    : IClassFixture<TestWebApplicationFactory<Program>>
{
    [Fact]
    public async Task GetProfile_ReturnsCurrentUser()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);

        var response = await client.GetAsync("/api/users/me");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponse>(TestWebApplicationFactory<Program>.JsonOptions);
        profile.Should().NotBeNull();
        profile!.DisplayName.Should().NotBeNullOrEmpty();
        profile.Email.Should().NotBeNullOrEmpty();
        profile.PreferredLanguage.Should().Be("en");
    }

    [Fact]
    public async Task UpdateProfile_ChangesDisplayName()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);

        var response = await client.PatchAsJsonAsync("/api/users/me",
            new UpdateUserProfileRequest("New Name", null, null, null, null));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponse>(TestWebApplicationFactory<Program>.JsonOptions);
        profile!.DisplayName.Should().Be("New Name");
    }

    [Fact]
    public async Task UpdateProfile_ChangesPreferences()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);

        var response = await client.PatchAsJsonAsync("/api/users/me",
            new UpdateUserProfileRequest(null, "hu", false, true, null));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponse>(TestWebApplicationFactory<Program>.JsonOptions);
        profile!.PreferredLanguage.Should().Be("hu");
        profile.PushNotificationsEnabled.Should().BeFalse();
        profile.WeeklyEmailEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateProfile_ChangesPreferredTheme()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);

        var response = await client.PatchAsJsonAsync("/api/users/me",
            new UpdateUserProfileRequest(null, null, null, null, "ocean"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponse>(TestWebApplicationFactory<Program>.JsonOptions);
        profile!.PreferredTheme.Should().Be("ocean");
    }

    [Fact]
    public async Task GetProfile_ReturnsDefaultTheme()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);

        var response = await client.GetAsync("/api/users/me");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponse>(TestWebApplicationFactory<Program>.JsonOptions);
        profile!.PreferredTheme.Should().Be("sunny");
    }
}
