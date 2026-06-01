using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TinyHeroes.Application.DTOs.Family;
using TinyHeroes.Application.DTOs.JoinRequest;
using TinyHeroes.Tests.Integration.Helpers;

namespace TinyHeroes.Tests.Integration;

public class JoinRequestControllerTests(TestWebApplicationFactory<Program> factory)
    : IClassFixture<TestWebApplicationFactory<Program>>
{
    // ── Family creation seeds a JoinCode ────────────────────────────────────

    [Fact]
    public async Task CreateFamily_SetsJoinCode()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);
        var response = await client.GetAsync("/api/families/mine");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var family = await response.Content.ReadFromJsonAsync<FamilyDetailResponse>(TestWebApplicationFactory<Program>.JsonOptions);
        family!.JoinCode.Should().NotBeNullOrEmpty().And.HaveLength(8);
    }

    // ── Submit a join request ────────────────────────────────────────────────

    [Fact]
    public async Task SubmitJoinRequest_WithValidCode_Returns201()
    {
        var adminClient = await TestAuthHelper.RegisterWithFamily(factory);
        var familyRes = await adminClient.GetAsync("/api/families/mine");
        var family = await familyRes.Content.ReadFromJsonAsync<FamilyDetailResponse>(TestWebApplicationFactory<Program>.JsonOptions);

        var requesterClient = await TestAuthHelper.RegisterOnly(factory);
        var response = await requesterClient.PostAsJsonAsync("/api/join-requests", new SubmitJoinRequestRequest(family!.JoinCode!));
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var req = await response.Content.ReadFromJsonAsync<JoinRequestResponse>(TestWebApplicationFactory<Program>.JsonOptions);
        req!.Status.Should().Be("Pending");
        req.FamilyName.Should().Be("Test Family");
    }

    [Fact]
    public async Task SubmitJoinRequest_InvalidCode_Returns404()
    {
        var client = await TestAuthHelper.RegisterOnly(factory);
        var response = await client.PostAsJsonAsync("/api/join-requests", new SubmitJoinRequestRequest("BADCODE1"));
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SubmitJoinRequest_WhenAlreadyInFamily_Returns400()
    {
        var adminClient = await TestAuthHelper.RegisterWithFamily(factory);
        var familyRes = await adminClient.GetAsync("/api/families/mine");
        var family = await familyRes.Content.ReadFromJsonAsync<FamilyDetailResponse>(TestWebApplicationFactory<Program>.JsonOptions);

        var response = await adminClient.PostAsJsonAsync("/api/join-requests", new SubmitJoinRequestRequest(family!.JoinCode!));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SubmitJoinRequest_WhenAlreadyPending_Returns400()
    {
        var adminClient = await TestAuthHelper.RegisterWithFamily(factory);
        var familyRes = await adminClient.GetAsync("/api/families/mine");
        var family = await familyRes.Content.ReadFromJsonAsync<FamilyDetailResponse>(TestWebApplicationFactory<Program>.JsonOptions);

        var requesterClient = await TestAuthHelper.RegisterOnly(factory);
        await requesterClient.PostAsJsonAsync("/api/join-requests", new SubmitJoinRequestRequest(family!.JoinCode!));

        var response = await requesterClient.PostAsJsonAsync("/api/join-requests", new SubmitJoinRequestRequest(family.JoinCode!));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SubmitJoinRequest_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/join-requests", new SubmitJoinRequestRequest("ANYCODE1"));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Get / cancel own request ─────────────────────────────────────────────

    [Fact]
    public async Task GetJoinRequest_WithPending_ReturnsRequest()
    {
        var adminClient = await TestAuthHelper.RegisterWithFamily(factory);
        var familyRes = await adminClient.GetAsync("/api/families/mine");
        var family = await familyRes.Content.ReadFromJsonAsync<FamilyDetailResponse>(TestWebApplicationFactory<Program>.JsonOptions);

        var requesterClient = await TestAuthHelper.RegisterOnly(factory);
        await requesterClient.PostAsJsonAsync("/api/join-requests", new SubmitJoinRequestRequest(family!.JoinCode!));

        var response = await requesterClient.GetAsync("/api/join-requests");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var req = await response.Content.ReadFromJsonAsync<JoinRequestResponse>(TestWebApplicationFactory<Program>.JsonOptions);
        req!.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task GetJoinRequest_WithNone_Returns404()
    {
        var client = await TestAuthHelper.RegisterOnly(factory);
        var response = await client.GetAsync("/api/join-requests");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CancelJoinRequest_DeletesRequest()
    {
        var adminClient = await TestAuthHelper.RegisterWithFamily(factory);
        var familyRes = await adminClient.GetAsync("/api/families/mine");
        var family = await familyRes.Content.ReadFromJsonAsync<FamilyDetailResponse>(TestWebApplicationFactory<Program>.JsonOptions);

        var requesterClient = await TestAuthHelper.RegisterOnly(factory);
        await requesterClient.PostAsJsonAsync("/api/join-requests", new SubmitJoinRequestRequest(family!.JoinCode!));

        var deleteResponse = await requesterClient.DeleteAsync("/api/join-requests");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await requesterClient.GetAsync("/api/join-requests");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Admin: list pending requests ─────────────────────────────────────────

    [Fact]
    public async Task GetFamilyJoinRequests_AsAdmin_ReturnsPendingList()
    {
        var adminClient = await TestAuthHelper.RegisterWithFamily(factory);
        var familyRes = await adminClient.GetAsync("/api/families/mine");
        var family = await familyRes.Content.ReadFromJsonAsync<FamilyDetailResponse>(TestWebApplicationFactory<Program>.JsonOptions);

        var requesterClient = await TestAuthHelper.RegisterOnly(factory);
        await requesterClient.PostAsJsonAsync("/api/join-requests", new SubmitJoinRequestRequest(family!.JoinCode!));

        var response = await adminClient.GetAsync("/api/families/join-requests");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<List<JoinRequestResponse>>(TestWebApplicationFactory<Program>.JsonOptions);
        list.Should().HaveCount(1);
        list![0].Status.Should().Be("Pending");
    }

    [Fact]
    public async Task GetFamilyJoinRequests_AsCoParent_Returns403()
    {
        var adminClient = await TestAuthHelper.RegisterWithFamily(factory);
        var inviteRes = await adminClient.PostAsJsonAsync("/api/invites", new { email = (string?)null });
        var invite = await inviteRes.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(TestWebApplicationFactory<Program>.JsonOptions);
        var token = invite.GetProperty("token").GetString();

        var coParentClient = await TestAuthHelper.RegisterOnly(factory);
        await coParentClient.PostAsync($"/api/invites/{token}/accept", null);

        var response = await coParentClient.GetAsync("/api/families/join-requests");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetFamilyJoinRequests_WithoutFamily_Returns404()
    {
        var client = await TestAuthHelper.RegisterOnly(factory);
        var response = await client.GetAsync("/api/families/join-requests");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Admin: approve ───────────────────────────────────────────────────────

    [Fact]
    public async Task ResolveJoinRequest_Approve_CreatesFamilyMember()
    {
        var adminClient = await TestAuthHelper.RegisterWithFamily(factory);
        var familyRes = await adminClient.GetAsync("/api/families/mine");
        var family = await familyRes.Content.ReadFromJsonAsync<FamilyDetailResponse>(TestWebApplicationFactory<Program>.JsonOptions);

        var requesterClient = await TestAuthHelper.RegisterOnly(factory);
        var submitRes = await requesterClient.PostAsJsonAsync("/api/join-requests", new SubmitJoinRequestRequest(family!.JoinCode!));
        var joinReq = await submitRes.Content.ReadFromJsonAsync<JoinRequestResponse>(TestWebApplicationFactory<Program>.JsonOptions);

        var resolveRes = await adminClient.PostAsJsonAsync(
            $"/api/families/join-requests/{joinReq!.Id}/resolve",
            new ResolveJoinRequestRequest(true));
        resolveRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Requester can now access their family
        var familyCheckRes = await requesterClient.GetAsync("/api/families/mine");
        familyCheckRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var approvedFamily = await familyCheckRes.Content.ReadFromJsonAsync<FamilyDetailResponse>(TestWebApplicationFactory<Program>.JsonOptions);
        approvedFamily!.Members.Should().HaveCount(2);
        approvedFamily.Members.Should().Contain(m => m.Role == "CoParent");
    }

    [Fact]
    public async Task ResolveJoinRequest_Reject_DoesNotCreateFamilyMember()
    {
        var adminClient = await TestAuthHelper.RegisterWithFamily(factory);
        var familyRes = await adminClient.GetAsync("/api/families/mine");
        var family = await familyRes.Content.ReadFromJsonAsync<FamilyDetailResponse>(TestWebApplicationFactory<Program>.JsonOptions);

        var requesterClient = await TestAuthHelper.RegisterOnly(factory);
        var submitRes = await requesterClient.PostAsJsonAsync("/api/join-requests", new SubmitJoinRequestRequest(family!.JoinCode!));
        var joinReq = await submitRes.Content.ReadFromJsonAsync<JoinRequestResponse>(TestWebApplicationFactory<Program>.JsonOptions);

        var resolveRes = await adminClient.PostAsJsonAsync(
            $"/api/families/join-requests/{joinReq!.Id}/resolve",
            new ResolveJoinRequestRequest(false));
        resolveRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var familyCheckRes = await requesterClient.GetAsync("/api/families/mine");
        familyCheckRes.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Requester's join request now has Rejected status — GET pending returns 404
        var reqRes = await requesterClient.GetAsync("/api/join-requests");
        reqRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ResolveJoinRequest_ApproveTwice_SecondReturns404()
    {
        var adminClient = await TestAuthHelper.RegisterWithFamily(factory);
        var familyRes = await adminClient.GetAsync("/api/families/mine");
        var family = await familyRes.Content.ReadFromJsonAsync<FamilyDetailResponse>(TestWebApplicationFactory<Program>.JsonOptions);

        var requesterClient = await TestAuthHelper.RegisterOnly(factory);
        var submitRes = await requesterClient.PostAsJsonAsync("/api/join-requests", new SubmitJoinRequestRequest(family!.JoinCode!));
        var joinReq = await submitRes.Content.ReadFromJsonAsync<JoinRequestResponse>(TestWebApplicationFactory<Program>.JsonOptions);

        await adminClient.PostAsJsonAsync($"/api/families/join-requests/{joinReq!.Id}/resolve", new ResolveJoinRequestRequest(true));
        var secondRes = await adminClient.PostAsJsonAsync($"/api/families/join-requests/{joinReq.Id}/resolve", new ResolveJoinRequestRequest(true));
        secondRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
