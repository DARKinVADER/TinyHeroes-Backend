using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TinyHeroes.Application.DTOs.Family;
using TinyHeroes.Application.DTOs.Invite;
using TinyHeroes.Tests.Integration.Helpers;

namespace TinyHeroes.Tests.Integration;

public class FamilyControllerTests(TestWebApplicationFactory<Program> factory)
    : IClassFixture<TestWebApplicationFactory<Program>>
{
    // -------------------------------------------------------------------------
    // GET /api/families/mine
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetMine_Authenticated_WithFamily_Returns200WithDetails()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory, "Smith Family");

        var response = await client.GetAsync("/api/families/mine");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await response.Content.ReadFromJsonAsync<FamilyDetailResponse>(
            TestWebApplicationFactory<Program>.JsonOptions);
        detail!.Name.Should().Be("Smith Family");
        detail.Members.Should().HaveCount(1);
        detail.JoinCode.Should().NotBeNullOrEmpty(); // admin sees join code
    }

    [Fact]
    public async Task GetMine_AsCoParent_JoinCodeIsNull()
    {
        var adminClient = await TestAuthHelper.RegisterWithFamily(factory);
        var inviteResponse = await adminClient.PostAsJsonAsync("/api/invites", new CreateInviteRequest(null));
        inviteResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var invite = await inviteResponse.Content.ReadFromJsonAsync<InviteResponse>(TestWebApplicationFactory<Program>.JsonOptions);

        var coParentClient = await TestAuthHelper.RegisterOnly(factory);
        var acceptResponse = await coParentClient.PostAsync($"/api/invites/{invite!.Token}/accept", null);
        acceptResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await coParentClient.GetAsync("/api/families/mine");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await response.Content.ReadFromJsonAsync<FamilyDetailResponse>(
            TestWebApplicationFactory<Program>.JsonOptions);
        detail!.JoinCode.Should().BeNull(); // co-parent cannot see the join code
    }

    [Fact]
    public async Task GetMine_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/families/mine");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMine_AuthenticatedWithoutFamily_Returns404()
    {
        var client = await TestAuthHelper.RegisterOnly(factory);

        var response = await client.GetAsync("/api/families/mine");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // POST /api/families  (Create)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Create_WithValidData_Returns201()
    {
        // Use a plain registered-only client so we can create a new family
        var client = await TestAuthHelper.RegisterOnly(factory);

        var response = await client.PostAsJsonAsync("/api/families",
            new CreateFamilyRequest("New Family", DayOfWeek.Monday));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var family = await response.Content.ReadFromJsonAsync<FamilyResponse>(
            TestWebApplicationFactory<Program>.JsonOptions);
        family!.Name.Should().Be("New Family");
        family.WeekStartDay.Should().Be(DayOfWeek.Monday);
        family.JoinCode.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Create_WhenUserAlreadyHasFamily_Returns409()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);

        var response = await client.PostAsJsonAsync("/api/families",
            new CreateFamilyRequest("Second Family", DayOfWeek.Monday));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/families",
            new CreateFamilyRequest("Anon Family", DayOfWeek.Monday));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // PATCH /api/families/mine  (UpdateMine)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateMine_AsAdmin_Returns200WithUpdatedName()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory, "Old Name");

        var response = await client.PatchAsJsonAsync("/api/families/mine",
            new UpdateFamilyRequest("New Name", DayOfWeek.Sunday));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var family = await response.Content.ReadFromJsonAsync<FamilyResponse>(
            TestWebApplicationFactory<Program>.JsonOptions);
        family!.Name.Should().Be("New Name");
        family.WeekStartDay.Should().Be(DayOfWeek.Sunday);
    }

    [Fact]
    public async Task UpdateMine_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.PatchAsJsonAsync("/api/families/mine",
            new UpdateFamilyRequest("X", DayOfWeek.Monday));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateMine_WithoutFamily_Returns400()
    {
        var client = await TestAuthHelper.RegisterOnly(factory);

        var response = await client.PatchAsJsonAsync("/api/families/mine",
            new UpdateFamilyRequest("X", DayOfWeek.Monday));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // -------------------------------------------------------------------------
    // PATCH /api/families/mine/prize-rules  (UpdatePrizeRules)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdatePrizeRules_AsAdmin_Returns200WithUpdatedRules()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);

        var response = await client.PatchAsJsonAsync("/api/families/mine/prize-rules",
            new SetPrizeRulesRequest(5, 20));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var family = await response.Content.ReadFromJsonAsync<FamilyResponse>(
            TestWebApplicationFactory<Program>.JsonOptions);
        family!.WeeklyMinDeeds.Should().Be(5);
        family.MonthlyMinDeeds.Should().Be(20);
    }

    [Fact]
    public async Task UpdatePrizeRules_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.PatchAsJsonAsync("/api/families/mine/prize-rules",
            new SetPrizeRulesRequest(1, 2));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdatePrizeRules_WithoutFamily_Returns400()
    {
        var client = await TestAuthHelper.RegisterOnly(factory);

        var response = await client.PatchAsJsonAsync("/api/families/mine/prize-rules",
            new SetPrizeRulesRequest(5, 20));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // -------------------------------------------------------------------------
    // DELETE /api/families/mine  (DeleteFamily)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteFamily_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.DeleteAsync("/api/families/mine");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteFamily_WithoutFamily_Returns400()
    {
        var client = await TestAuthHelper.RegisterOnly(factory);

        var response = await client.DeleteAsync("/api/families/mine");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // -------------------------------------------------------------------------
    // DELETE /api/families/mine/members/{memberId}  (RemoveMember)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RemoveMember_AsCoParent_Returns403()
    {
        var adminClient = await TestAuthHelper.RegisterWithFamily(factory);
        var inviteResponse = await adminClient.PostAsJsonAsync("/api/invites", new CreateInviteRequest(null));
        inviteResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var invite = await inviteResponse.Content.ReadFromJsonAsync<InviteResponse>(TestWebApplicationFactory<Program>.JsonOptions);

        var coParentClient = await TestAuthHelper.RegisterOnly(factory);
        var acceptResponse = await coParentClient.PostAsync($"/api/invites/{invite!.Token}/accept", null);
        acceptResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Co-parent tries to remove a member — should be forbidden (role-based check)
        var response = await coParentClient.DeleteAsync($"/api/families/mine/members/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RemoveMember_NonexistentMember_Returns404()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);

        var response = await client.DeleteAsync($"/api/families/mine/members/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemoveMember_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.DeleteAsync($"/api/families/mine/members/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // GET /api/families/join-requests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetJoinRequests_AsAdmin_Returns200EmptyList()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);

        var response = await client.GetAsync("/api/families/join-requests");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<List<object>>(
            TestWebApplicationFactory<Program>.JsonOptions);
        list.Should().BeEmpty();
    }

    [Fact]
    public async Task GetJoinRequests_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/families/join-requests");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetJoinRequests_WithoutFamily_Returns404()
    {
        var client = await TestAuthHelper.RegisterOnly(factory);

        var response = await client.GetAsync("/api/families/join-requests");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
