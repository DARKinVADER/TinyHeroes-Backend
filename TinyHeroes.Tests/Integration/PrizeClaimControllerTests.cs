// backend/TinyHeroes.Tests/Integration/PrizeClaimControllerTests.cs
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TinyHeroes.Application.DTOs.Invite;
using TinyHeroes.Application.DTOs.Prize;
using TinyHeroes.Tests.Integration.Helpers;

namespace TinyHeroes.Tests.Integration;

public class PrizeClaimControllerTests(TestWebApplicationFactory<Program> factory)
    : IClassFixture<TestWebApplicationFactory<Program>>
{
    [Fact]
    public async Task GetByWeekSummary_ReturnsEmptyList_WhenNoClaimsExist()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);
        var summaryId = Guid.NewGuid();

        var response = await client.GetAsync($"/api/prize-claims?weekSummaryId={summaryId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var claims = await response.Content.ReadFromJsonAsync<List<PrizeClaimDto>>(TestWebApplicationFactory<Program>.JsonOptions);
        claims.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByMonthSummary_ReturnsEmptyList_WhenNoClaimsExist()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);
        var summaryId = Guid.NewGuid();

        var response = await client.GetAsync($"/api/prize-claims?monthSummaryId={summaryId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var claims = await response.Content.ReadFromJsonAsync<List<PrizeClaimDto>>(TestWebApplicationFactory<Program>.JsonOptions);
        claims.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateClaim_ReturnsClaim_WithEmptyComments()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);
        var summaryId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        var req = new CreatePrizeClaimRequest("weekly", summaryId, null, 1, childId, "Alice", "🍕", "Pizza night");
        var response = await client.PostAsJsonAsync("/api/prize-claims", req);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var claim = await response.Content.ReadFromJsonAsync<PrizeClaimDto>(TestWebApplicationFactory<Program>.JsonOptions);
        claim.Should().NotBeNull();
        claim!.PrizeLabel.Should().Be("Pizza night");
        claim.IsUsed.Should().BeFalse();
        claim.Comments.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateClaim_Idempotent_ReturnsSameClaimOnDuplicate()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);
        var summaryId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var req = new CreatePrizeClaimRequest("weekly", summaryId, null, 1, childId, "Alice", "🍕", "Pizza night");

        var first = await client.PostAsJsonAsync("/api/prize-claims", req);
        var second = await client.PostAsJsonAsync("/api/prize-claims", req);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var c1 = await first.Content.ReadFromJsonAsync<PrizeClaimDto>(TestWebApplicationFactory<Program>.JsonOptions);
        var c2 = await second.Content.ReadFromJsonAsync<PrizeClaimDto>(TestWebApplicationFactory<Program>.JsonOptions);
        c1!.Id.Should().Be(c2!.Id);
    }

    [Fact]
    public async Task MarkUsed_SetsIsUsedAndUsedAt()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);
        var summaryId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var createReq = new CreatePrizeClaimRequest("weekly", summaryId, null, 1, childId, "Alice", "🍕", "Pizza night");
        var createResponse = await client.PostAsJsonAsync("/api/prize-claims", createReq);
        var claim = await createResponse.Content.ReadFromJsonAsync<PrizeClaimDto>(TestWebApplicationFactory<Program>.JsonOptions);

        var response = await client.PatchAsJsonAsync($"/api/prize-claims/{claim!.Id}/used", new UpdateUsedRequest(true));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<PrizeClaimDto>(TestWebApplicationFactory<Program>.JsonOptions);
        updated!.IsUsed.Should().BeTrue();
        updated.UsedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkUnused_ClearsIsUsedAndUsedAt()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);
        var summaryId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var createReq = new CreatePrizeClaimRequest("weekly", summaryId, null, 1, childId, "Alice", "🍕", "Pizza night");
        var createResponse = await client.PostAsJsonAsync("/api/prize-claims", createReq);
        var claim = await createResponse.Content.ReadFromJsonAsync<PrizeClaimDto>(TestWebApplicationFactory<Program>.JsonOptions);
        await client.PatchAsJsonAsync($"/api/prize-claims/{claim!.Id}/used", new UpdateUsedRequest(true));

        var response = await client.PatchAsJsonAsync($"/api/prize-claims/{claim.Id}/used", new UpdateUsedRequest(false));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<PrizeClaimDto>(TestWebApplicationFactory<Program>.JsonOptions);
        updated!.IsUsed.Should().BeFalse();
        updated.UsedAt.Should().BeNull();
    }

    [Fact]
    public async Task AddComment_AppearsInSubsequentGet()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);
        var summaryId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var createReq = new CreatePrizeClaimRequest("weekly", summaryId, null, 1, childId, "Alice", "🍕", "Pizza night");
        var createResponse = await client.PostAsJsonAsync("/api/prize-claims", createReq);
        var claim = await createResponse.Content.ReadFromJsonAsync<PrizeClaimDto>(TestWebApplicationFactory<Program>.JsonOptions);

        var commentResponse = await client.PostAsJsonAsync($"/api/prize-claims/{claim!.Id}/comments", new AddCommentRequest("Given out Friday!"));

        commentResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var getResponse = await client.GetAsync($"/api/prize-claims?weekSummaryId={summaryId}");
        var claims = await getResponse.Content.ReadFromJsonAsync<List<PrizeClaimDto>>(TestWebApplicationFactory<Program>.JsonOptions);
        claims!.Single().Comments.Should().ContainSingle(c => c.Text == "Given out Friday!");
    }

    [Fact]
    public async Task DeleteComment_RemovesItFromGet()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);
        var summaryId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var createReq = new CreatePrizeClaimRequest("weekly", summaryId, null, 1, childId, "Alice", "🍕", "Pizza night");
        var createResponse = await client.PostAsJsonAsync("/api/prize-claims", createReq);
        var claim = await createResponse.Content.ReadFromJsonAsync<PrizeClaimDto>(TestWebApplicationFactory<Program>.JsonOptions);
        var commentResponse = await client.PostAsJsonAsync($"/api/prize-claims/{claim!.Id}/comments", new AddCommentRequest("Given out Friday!"));
        var comment = await commentResponse.Content.ReadFromJsonAsync<PrizeCommentDto>(TestWebApplicationFactory<Program>.JsonOptions);

        var deleteResponse = await client.DeleteAsync($"/api/prize-claims/{claim.Id}/comments/{comment!.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var getResponse = await client.GetAsync($"/api/prize-claims?weekSummaryId={summaryId}");
        var claims = await getResponse.Content.ReadFromJsonAsync<List<PrizeClaimDto>>(TestWebApplicationFactory<Program>.JsonOptions);
        claims!.Single().Comments.Should().BeEmpty();
    }

    [Fact]
    public async Task AllWriteEndpoints_Return401_WhenUnauthenticated()
    {
        var client = factory.CreateClient();
        var id = Guid.NewGuid();

        (await client.GetAsync($"/api/prize-claims?weekSummaryId={id}")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.PostAsJsonAsync("/api/prize-claims", new { })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.PatchAsJsonAsync($"/api/prize-claims/{id}/used", new { })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.PostAsJsonAsync($"/api/prize-claims/{id}/comments", new { })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.DeleteAsync($"/api/prize-claims/{id}/comments/{id}")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CoParent_CanMarkUsedAndComment()
    {
        var adminClient = await TestAuthHelper.RegisterWithFamily(factory);
        var inviteResponse = await adminClient.PostAsJsonAsync("/api/invites", new CreateInviteRequest(null));
        var invite = await inviteResponse.Content.ReadFromJsonAsync<InviteResponse>(TestWebApplicationFactory<Program>.JsonOptions);
        var coParentClient = await TestAuthHelper.RegisterOnly(factory);
        var acceptResponse = await coParentClient.PostAsync($"/api/invites/{invite!.Token}/accept", null);
        acceptResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var summaryId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var createReq = new CreatePrizeClaimRequest("weekly", summaryId, null, 1, childId, "Alice", "🍕", "Pizza night");

        var createResponse = await coParentClient.PostAsJsonAsync("/api/prize-claims", createReq);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var claim = await createResponse.Content.ReadFromJsonAsync<PrizeClaimDto>(TestWebApplicationFactory<Program>.JsonOptions);

        var usedResponse = await coParentClient.PatchAsJsonAsync($"/api/prize-claims/{claim!.Id}/used", new UpdateUsedRequest(true));
        usedResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var commentResponse = await coParentClient.PostAsJsonAsync($"/api/prize-claims/{claim.Id}/comments", new AddCommentRequest("Co-parent note"));
        commentResponse.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task OtherFamily_CannotAccessClaims()
    {
        var client1 = await TestAuthHelper.RegisterWithFamily(factory, "Family 1");
        var client2 = await TestAuthHelper.RegisterWithFamily(factory, "Family 2");

        var summaryId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var req = new CreatePrizeClaimRequest("weekly", summaryId, null, 1, childId, "Alice", "🍕", "Pizza night");
        var createResponse = await client1.PostAsJsonAsync("/api/prize-claims", req);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var claim = await createResponse.Content.ReadFromJsonAsync<PrizeClaimDto>(TestWebApplicationFactory<Program>.JsonOptions);

        var response = await client2.PatchAsJsonAsync($"/api/prize-claims/{claim!.Id}/used", new UpdateUsedRequest(true));
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task OtherFamily_CannotAddComment()
    {
        var client1 = await TestAuthHelper.RegisterWithFamily(factory, "Family 1");
        var client2 = await TestAuthHelper.RegisterWithFamily(factory, "Family 2");

        var summaryId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var createResponse = await client1.PostAsJsonAsync("/api/prize-claims",
            new CreatePrizeClaimRequest("weekly", summaryId, null, 1, childId, "Alice", "🍕", "Pizza night"));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var claim = await createResponse.Content.ReadFromJsonAsync<PrizeClaimDto>(TestWebApplicationFactory<Program>.JsonOptions);

        var response = await client2.PostAsJsonAsync($"/api/prize-claims/{claim!.Id}/comments", new AddCommentRequest("Sneaky note"));
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task OtherFamily_CannotDeleteComment()
    {
        var client1 = await TestAuthHelper.RegisterWithFamily(factory, "Family 1");
        var client2 = await TestAuthHelper.RegisterWithFamily(factory, "Family 2");

        var summaryId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var createResponse = await client1.PostAsJsonAsync("/api/prize-claims",
            new CreatePrizeClaimRequest("weekly", summaryId, null, 1, childId, "Alice", "🍕", "Pizza night"));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var claim = await createResponse.Content.ReadFromJsonAsync<PrizeClaimDto>(TestWebApplicationFactory<Program>.JsonOptions);

        var commentResponse = await client1.PostAsJsonAsync($"/api/prize-claims/{claim!.Id}/comments", new AddCommentRequest("Owner note"));
        commentResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var comment = await commentResponse.Content.ReadFromJsonAsync<PrizeCommentDto>(TestWebApplicationFactory<Program>.JsonOptions);

        var response = await client2.DeleteAsync($"/api/prize-claims/{claim.Id}/comments/{comment!.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetList_DoesNotReturnOtherFamilyClaims()
    {
        var client1 = await TestAuthHelper.RegisterWithFamily(factory, "Family 1");
        var client2 = await TestAuthHelper.RegisterWithFamily(factory, "Family 2");

        var summaryId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var createResponse = await client1.PostAsJsonAsync("/api/prize-claims",
            new CreatePrizeClaimRequest("weekly", summaryId, null, 1, childId, "Alice", "🍕", "Pizza night"));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // client2 queries with client1's summaryId — should see nothing
        var response = await client2.GetAsync($"/api/prize-claims?weekSummaryId={summaryId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var claims = await response.Content.ReadFromJsonAsync<List<PrizeClaimDto>>(TestWebApplicationFactory<Program>.JsonOptions);
        claims.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateMonthlyClaim_ReturnsClaim_AndIdempotentOnDuplicate()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);
        var monthSummaryId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var req = new CreatePrizeClaimRequest("monthly", null, monthSummaryId, null, childId, "Alice", "🎡", "Amusement park");

        var first = await client.PostAsJsonAsync("/api/prize-claims", req);
        var second = await client.PostAsJsonAsync("/api/prize-claims", req);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var c1 = await first.Content.ReadFromJsonAsync<PrizeClaimDto>(TestWebApplicationFactory<Program>.JsonOptions);
        var c2 = await second.Content.ReadFromJsonAsync<PrizeClaimDto>(TestWebApplicationFactory<Program>.JsonOptions);
        c1!.Id.Should().Be(c2!.Id);
        c1.Scope.Should().Be("monthly");
        c1.Rank.Should().BeNull();
        c1.MonthSummaryId.Should().Be(monthSummaryId);
    }

    [Fact]
    public async Task GetList_WithoutQueryParam_Returns400()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);

        var response = await client.GetAsync("/api/prize-claims");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
