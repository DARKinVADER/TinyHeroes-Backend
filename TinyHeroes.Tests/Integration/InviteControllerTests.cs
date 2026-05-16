using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TinyHeroes.Application.DTOs.Family;
using TinyHeroes.Application.DTOs.Invite;
using TinyHeroes.Domain.Enums;
using TinyHeroes.Tests.Integration.Helpers;

namespace TinyHeroes.Tests.Integration;

public class InviteControllerTests(TestWebApplicationFactory<Program> factory)
    : IClassFixture<TestWebApplicationFactory<Program>>
{
    [Fact]
    public async Task GetMine_ReturnsFamily_WithMembers()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);

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
        var client = await TestAuthHelper.RegisterOnly(factory);

        var response = await client.GetAsync("/api/families/mine");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateInvite_WithEmail_ReturnsToken()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);

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
        var adminClient = await TestAuthHelper.RegisterWithFamily(factory);
        var inviteResponse = await adminClient.PostAsJsonAsync("/api/invites", new CreateInviteRequest(null));
        var invite = await inviteResponse.Content.ReadFromJsonAsync<InviteResponse>();

        // User B accepts the invite (becomes CoParent)
        var coParentClient = await TestAuthHelper.RegisterOnly(factory);
        await coParentClient.PostAsync($"/api/invites/{invite!.Token}/accept", null);

        // User B tries to create an invite — should be forbidden
        var response = await coParentClient.PostAsJsonAsync("/api/invites", new CreateInviteRequest("other@test.com"));
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AcceptInvite_JoinsFamily()
    {
        // User A creates family and invite
        var adminClient = await TestAuthHelper.RegisterWithFamily(factory);
        var inviteResponse = await adminClient.PostAsJsonAsync("/api/invites", new CreateInviteRequest(null));
        var invite = await inviteResponse.Content.ReadFromJsonAsync<InviteResponse>();

        // User B accepts
        var userBClient = await TestAuthHelper.RegisterOnly(factory);
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
        var client = await TestAuthHelper.RegisterOnly(factory);

        var response = await client.PostAsync("/api/invites/nonexistenttoken123/accept", null);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AcceptInvite_WhenAlreadyInFamily_Returns409()
    {
        // User A creates family and invite
        var adminClient = await TestAuthHelper.RegisterWithFamily(factory);
        var inviteResponse = await adminClient.PostAsJsonAsync("/api/invites", new CreateInviteRequest(null));
        var invite = await inviteResponse.Content.ReadFromJsonAsync<InviteResponse>();

        // User B already has a family
        var userBClient = await TestAuthHelper.RegisterWithFamily(factory);

        // User B tries to accept the invite — should conflict
        var response = await userBClient.PostAsync($"/api/invites/{invite!.Token}/accept", null);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
