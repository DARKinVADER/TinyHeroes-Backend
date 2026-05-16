using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using TinyHeroes.Application.DTOs.Auth;
using TinyHeroes.Application.DTOs.Family;
using TinyHeroes.Application.DTOs.Invite;
using TinyHeroes.Domain.Enums;

namespace TinyHeroes.Tests.Integration;

public class InviteControllerTests(TestWebApplicationFactory<Program> factory)
    : IClassFixture<TestWebApplicationFactory<Program>>
{
    private async Task<HttpClient> CreateAuthenticatedClient()
    {
        var client = factory.CreateClient();
        var email = $"user_{Guid.NewGuid()}@test.com";
        var regResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("Parent", email, "Password123!"));
        var auth = await regResponse.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    private async Task<HttpClient> CreateAuthenticatedClientWithFamily()
    {
        var client = await CreateAuthenticatedClient();
        await client.PostAsJsonAsync("/api/families", new CreateFamilyRequest("Test Family", DayOfWeek.Monday));
        return client;
    }

    [Fact]
    public async Task GetMine_ReturnsFamily_WithMembers()
    {
        var client = await CreateAuthenticatedClientWithFamily();

        var response = await client.GetAsync("/api/families/mine");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var family = await response.Content.ReadFromJsonAsync<FamilyDetailResponse>();
        family!.Name.Should().Be("Test Family");
        family.WeekStartDay.Should().Be(DayOfWeek.Monday);
        family.Members.Should().HaveCount(1);
        family.Members[0].Role.Should().Be(FamilyRole.Admin);
    }

    [Fact]
    public async Task GetMine_WhenNoFamily_Returns404()
    {
        var client = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/families/mine");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateInvite_WithEmail_ReturnsToken()
    {
        var client = await CreateAuthenticatedClientWithFamily();

        var response = await client.PostAsJsonAsync("/api/invites", new CreateInviteRequest("friend@test.com"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var invite = await response.Content.ReadFromJsonAsync<InviteResponse>();
        invite!.Token.Should().NotBeNullOrEmpty();
        invite.Email.Should().Be("friend@test.com");
        invite.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task CreateInvite_AsNonAdmin_Returns403()
    {
        // User A creates family and invite
        var adminClient = await CreateAuthenticatedClientWithFamily();
        var inviteResponse = await adminClient.PostAsJsonAsync("/api/invites", new CreateInviteRequest(null));
        var invite = await inviteResponse.Content.ReadFromJsonAsync<InviteResponse>();

        // User B accepts the invite (becomes CoParent)
        var coParentClient = await CreateAuthenticatedClient();
        await coParentClient.PostAsync($"/api/invites/{invite!.Token}/accept", null);

        // User B tries to create an invite — should be forbidden
        var response = await coParentClient.PostAsJsonAsync("/api/invites", new CreateInviteRequest("other@test.com"));
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AcceptInvite_JoinsFamily()
    {
        // User A creates family and invite
        var adminClient = await CreateAuthenticatedClientWithFamily();
        var inviteResponse = await adminClient.PostAsJsonAsync("/api/invites", new CreateInviteRequest(null));
        var invite = await inviteResponse.Content.ReadFromJsonAsync<InviteResponse>();

        // User B accepts
        var userBClient = await CreateAuthenticatedClient();
        var acceptResponse = await userBClient.PostAsync($"/api/invites/{invite!.Token}/accept", null);
        acceptResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify user B is now in the family
        var familyResponse = await userBClient.GetAsync("/api/families/mine");
        familyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var family = await familyResponse.Content.ReadFromJsonAsync<FamilyDetailResponse>();
        family!.Members.Should().HaveCount(2);
        family.Members.Should().Contain(m => m.Role == FamilyRole.CoParent);
    }

    [Fact]
    public async Task AcceptInvite_InvalidToken_Returns404()
    {
        var client = await CreateAuthenticatedClient();

        var response = await client.PostAsync("/api/invites/nonexistenttoken123/accept", null);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AcceptInvite_WhenAlreadyInFamily_Returns409()
    {
        // User A creates family and invite
        var adminClient = await CreateAuthenticatedClientWithFamily();
        var inviteResponse = await adminClient.PostAsJsonAsync("/api/invites", new CreateInviteRequest(null));
        var invite = await inviteResponse.Content.ReadFromJsonAsync<InviteResponse>();

        // User B already has a family
        var userBClient = await CreateAuthenticatedClientWithFamily();

        // User B tries to accept the invite — should conflict
        var response = await userBClient.PostAsync($"/api/invites/{invite!.Token}/accept", null);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
