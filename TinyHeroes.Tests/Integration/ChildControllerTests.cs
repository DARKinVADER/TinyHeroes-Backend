using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using TinyHeroes.Application.DTOs.Auth;
using TinyHeroes.Application.DTOs.Child;
using TinyHeroes.Application.DTOs.Family;
using TinyHeroes.Domain.Enums;

namespace TinyHeroes.Tests.Integration;

public class ChildControllerTests(TestWebApplicationFactory<Program> factory)
    : IClassFixture<TestWebApplicationFactory<Program>>
{
    private async Task<HttpClient> CreateAuthenticatedClientWithFamily()
    {
        var client = factory.CreateClient();
        var email = $"user_{Guid.NewGuid()}@test.com";
        var regResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("Parent", email, "Password123!"));
        var auth = await regResponse.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        await client.PostAsJsonAsync("/api/families", new CreateFamilyRequest("Test Family", DayOfWeek.Monday));
        return client;
    }

    private async Task<HttpClient> CreateAuthenticatedClientWithoutFamily()
    {
        var client = factory.CreateClient();
        var email = $"user_{Guid.NewGuid()}@test.com";
        var regResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("Parent", email, "Password123!"));
        var auth = await regResponse.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    [Fact]
    public async Task Create_WithValidData_Returns200()
    {
        var client = await CreateAuthenticatedClientWithFamily();
        var response = await client.PostAsJsonAsync("/api/children", new CreateChildRequest("Emma", 7, Gender.Girl, "🦄"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var child = await response.Content.ReadFromJsonAsync<ChildResponse>();
        child!.Name.Should().Be("Emma");
        child.Age.Should().Be(7);
        child.Gender.Should().Be(Gender.Girl);
        child.AvatarEmoji.Should().Be("🦄");
    }

    [Fact]
    public async Task Create_WithoutFamily_Returns400()
    {
        var client = await CreateAuthenticatedClientWithoutFamily();
        var response = await client.PostAsJsonAsync("/api/children", new CreateChildRequest("Emma", 7, Gender.Girl, "🦄"));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task List_ReturnsAllFamilyChildren()
    {
        var client = await CreateAuthenticatedClientWithFamily();
        await client.PostAsJsonAsync("/api/children", new CreateChildRequest("Child1", 5, Gender.Boy, "🦁"));
        await client.PostAsJsonAsync("/api/children", new CreateChildRequest("Child2", 8, Gender.Girl, "🦊"));

        var response = await client.GetAsync("/api/children");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var children = await response.Content.ReadFromJsonAsync<List<ChildResponse>>();
        children!.Count.Should().Be(2);
    }

    [Fact]
    public async Task Get_ReturnsChild()
    {
        var client = await CreateAuthenticatedClientWithFamily();
        var createResp = await client.PostAsJsonAsync("/api/children", new CreateChildRequest("Solo", 6, Gender.Boy, "🐯"));
        var created = await createResp.Content.ReadFromJsonAsync<ChildResponse>();

        var response = await client.GetAsync($"/api/children/{created!.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var child = await response.Content.ReadFromJsonAsync<ChildResponse>();
        child!.Name.Should().Be("Solo");
    }

    [Fact]
    public async Task Update_ModifiesChild()
    {
        var client = await CreateAuthenticatedClientWithFamily();
        var createResp = await client.PostAsJsonAsync("/api/children", new CreateChildRequest("OldName", 5, Gender.Boy, "🦁"));
        var created = await createResp.Content.ReadFromJsonAsync<ChildResponse>();

        var response = await client.PutAsJsonAsync($"/api/children/{created!.Id}", new UpdateChildRequest("NewName", 6, Gender.Boy, "🐯"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<ChildResponse>();
        updated!.Name.Should().Be("NewName");
        updated.Age.Should().Be(6);
    }

    [Fact]
    public async Task Delete_AsAdmin_Succeeds()
    {
        var client = await CreateAuthenticatedClientWithFamily();
        var createResp = await client.PostAsJsonAsync("/api/children", new CreateChildRequest("ToDelete", 4, Gender.Girl, "🐼"));
        var created = await createResp.Content.ReadFromJsonAsync<ChildResponse>();

        var response = await client.DeleteAsync($"/api/children/{created!.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync($"/api/children/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_Unauthorized_Returns401()
    {
        var client = factory.CreateClient();
        var response = await client.DeleteAsync($"/api/children/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
