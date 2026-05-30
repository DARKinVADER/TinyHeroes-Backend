using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TinyHeroes.Application.DTOs.Family;
using TinyHeroes.Application.DTOs.Invite;
using TinyHeroes.Tests.Integration.Helpers;

namespace TinyHeroes.Tests.Integration;

public class FamilySettingsControllerTests(TestWebApplicationFactory<Program> factory)
    : IClassFixture<TestWebApplicationFactory<Program>>
{
    [Fact]
    public async Task UpdateFamily_AsAdmin_Succeeds()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);

        var response = await client.PatchAsJsonAsync("/api/families/mine",
            new UpdateFamilyRequest("Updated Family", DayOfWeek.Sunday));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var family = await response.Content.ReadFromJsonAsync<FamilyResponse>(TestWebApplicationFactory<Program>.JsonOptions);
        family!.Name.Should().Be("Updated Family");
        family.WeekStartDay.Should().Be(DayOfWeek.Sunday);
    }

    [Fact]
    public async Task UpdateFamily_AsCoParent_Returns403()
    {
        var adminClient = await TestAuthHelper.RegisterWithFamily(factory);
        var inviteResponse = await adminClient.PostAsJsonAsync("/api/invites", new CreateInviteRequest(null));
        var invite = await inviteResponse.Content.ReadFromJsonAsync<InviteResponse>(TestWebApplicationFactory<Program>.JsonOptions);

        var coParentClient = await TestAuthHelper.RegisterOnly(factory);
        var acceptResponse = await coParentClient.PostAsync($"/api/invites/{invite!.Token}/accept", null);
        acceptResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await coParentClient.PatchAsJsonAsync("/api/families/mine",
            new UpdateFamilyRequest("Hacked Name", DayOfWeek.Friday));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Verify the mutation did not persist
        var familyResponse = await adminClient.GetAsync("/api/families/mine");
        var family = await familyResponse.Content.ReadFromJsonAsync<FamilyDetailResponse>(TestWebApplicationFactory<Program>.JsonOptions);
        family!.Name.Should().Be("Test Family");
    }

    [Fact]
    public async Task RemoveMember_AsAdmin_Succeeds()
    {
        var adminClient = await TestAuthHelper.RegisterWithFamily(factory);
        var inviteResponse = await adminClient.PostAsJsonAsync("/api/invites", new CreateInviteRequest(null));
        var invite = await inviteResponse.Content.ReadFromJsonAsync<InviteResponse>(TestWebApplicationFactory<Program>.JsonOptions);

        var coParentClient = await TestAuthHelper.RegisterOnly(factory);
        var acceptResponse = await coParentClient.PostAsync($"/api/invites/{invite!.Token}/accept", null);
        acceptResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Get the co-parent's userId from the family details
        var familyResponse = await adminClient.GetAsync("/api/families/mine");
        var family = await familyResponse.Content.ReadFromJsonAsync<FamilyDetailResponse>(TestWebApplicationFactory<Program>.JsonOptions);
        var coParentMember = family!.Members.First(m => m.Role == "CoParent");

        var removeResponse = await adminClient.DeleteAsync($"/api/families/mine/members/{coParentMember.UserId}");
        removeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify member is no longer in the family
        var updatedFamilyResponse = await adminClient.GetAsync("/api/families/mine");
        var updatedFamily = await updatedFamilyResponse.Content.ReadFromJsonAsync<FamilyDetailResponse>(TestWebApplicationFactory<Program>.JsonOptions);
        updatedFamily!.Members.Should().HaveCount(1);
        updatedFamily.Members.Should().NotContain(m => m.Role == "CoParent");
    }

    [Fact]
    public async Task RemoveMember_Self_Returns400()
    {
        var adminClient = await TestAuthHelper.RegisterWithFamily(factory);

        // Get own userId
        var familyResponse = await adminClient.GetAsync("/api/families/mine");
        var family = await familyResponse.Content.ReadFromJsonAsync<FamilyDetailResponse>(TestWebApplicationFactory<Program>.JsonOptions);
        var adminMember = family!.Members.First(m => m.Role == "Admin");

        var response = await adminClient.DeleteAsync($"/api/families/mine/members/{adminMember.UserId}");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteFamily_AsAdmin_Succeeds()
    {
        var adminClient = await TestAuthHelper.RegisterWithFamily(factory);
        var inviteResponse = await adminClient.PostAsJsonAsync("/api/invites", new CreateInviteRequest(null));
        var invite = await inviteResponse.Content.ReadFromJsonAsync<InviteResponse>(TestWebApplicationFactory<Program>.JsonOptions);
        var coParentClient = await TestAuthHelper.RegisterOnly(factory);
        var acceptResponse = await coParentClient.PostAsync($"/api/invites/{invite!.Token}/accept", null);
        acceptResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleteResponse = await adminClient.DeleteAsync("/api/families/mine");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Admin can no longer see the family
        var adminGetResponse = await adminClient.GetAsync("/api/families/mine");
        adminGetResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Co-parent also loses access — confirming the Family entity was deleted, not just the admin's membership
        var coParentGetResponse = await coParentClient.GetAsync("/api/families/mine");
        coParentGetResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteFamily_AsCoParent_Returns403()
    {
        var adminClient = await TestAuthHelper.RegisterWithFamily(factory);
        var inviteResponse = await adminClient.PostAsJsonAsync("/api/invites", new CreateInviteRequest(null));
        var invite = await inviteResponse.Content.ReadFromJsonAsync<InviteResponse>(TestWebApplicationFactory<Program>.JsonOptions);

        var coParentClient = await TestAuthHelper.RegisterOnly(factory);
        var acceptResponse = await coParentClient.PostAsync($"/api/invites/{invite!.Token}/accept", null);
        acceptResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await coParentClient.DeleteAsync("/api/families/mine");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
