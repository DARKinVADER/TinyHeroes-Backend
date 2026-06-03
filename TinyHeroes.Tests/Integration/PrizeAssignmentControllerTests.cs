using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using TinyHeroes.Application.DTOs.Auth;
using TinyHeroes.Application.DTOs.Invite;
using TinyHeroes.Application.DTOs.Prize;
using TinyHeroes.Tests.Integration.Helpers;

namespace TinyHeroes.Tests.Integration;

public class PrizeAssignmentControllerTests(TestWebApplicationFactory<Program> factory)
    : IClassFixture<TestWebApplicationFactory<Program>>
{
    [Fact]
    public async Task List_ReturnsAssignments()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);

        // Create an assignment via PATCH
        var putResponse = await client.PatchAsJsonAsync("/api/prize-assignments", new SetPrizeRequest("weekly", 1, "🏆", "Gold trophy"));
        putResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // GET should include it
        var response = await client.GetAsync("/api/prize-assignments");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var assignments = await response.Content.ReadFromJsonAsync<List<PrizeAssignmentResponse>>(TestWebApplicationFactory<Program>.JsonOptions);
        assignments.Should().NotBeNull();
        assignments!.Should().ContainSingle(a => a.Scope == "weekly" && a.Rank == 1 && a.Label == "Gold trophy");
    }

    [Fact]
    public async Task Set_AsAdmin_CreatesAssignment()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);

        var response = await client.PatchAsJsonAsync("/api/prize-assignments", new SetPrizeRequest("weekly", 1, "🥇", "First place medal"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var assignment = await response.Content.ReadFromJsonAsync<PrizeAssignmentResponse>(TestWebApplicationFactory<Program>.JsonOptions);
        assignment.Should().NotBeNull();
        assignment!.Scope.Should().Be("weekly");
        assignment.Rank.Should().Be(1);
        assignment.Emoji.Should().Be("🥇");
        assignment.Label.Should().Be("First place medal");
    }

    [Fact]
    public async Task Set_AsAdmin_UpdatesExisting()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);

        // Create initial assignment
        var createResponse = await client.PatchAsJsonAsync("/api/prize-assignments", new SetPrizeRequest("weekly", 2, "🥈", "Silver medal"));
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = await createResponse.Content.ReadFromJsonAsync<PrizeAssignmentResponse>(TestWebApplicationFactory<Program>.JsonOptions);

        // Update with same scope+rank but different label/emoji
        var updateResponse = await client.PatchAsJsonAsync("/api/prize-assignments", new SetPrizeRequest("weekly", 2, "🎉", "Party time"));
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await updateResponse.Content.ReadFromJsonAsync<PrizeAssignmentResponse>(TestWebApplicationFactory<Program>.JsonOptions);
        updated.Should().NotBeNull();
        updated!.Id.Should().Be(created!.Id); // same record was updated
        updated.Emoji.Should().Be("🎉");
        updated.Label.Should().Be("Party time");
    }

    [Fact]
    public async Task Set_AsCoParent_Returns403()
    {
        // User A: register with family (admin)
        var adminClient = await TestAuthHelper.RegisterWithFamily(factory);

        // Create invite for co-parent
        var inviteResponse = await adminClient.PostAsJsonAsync("/api/invites", new CreateInviteRequest(null));
        inviteResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var invite = await inviteResponse.Content.ReadFromJsonAsync<InviteResponse>(TestWebApplicationFactory<Program>.JsonOptions);

        // User B: register without family
        var coParentClient = await TestAuthHelper.RegisterOnly(factory);

        // Accept invite (becomes CoParent in User A's family)
        var acceptResponse = await coParentClient.PostAsync($"/api/invites/{invite!.Token}/accept", null);
        acceptResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // CoParent tries to PATCH prize assignment -> 403
        var response = await coParentClient.PatchAsJsonAsync("/api/prize-assignments", new SetPrizeRequest("weekly", 1, "🏆", "Not allowed"));
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
