using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TinyHeroes.Application.DTOs.Child;
using TinyHeroes.Domain.Enums;
using TinyHeroes.Tests.Integration.Helpers;

namespace TinyHeroes.Tests.Integration;

public class ChildControllerTests(TestWebApplicationFactory<Program> factory)
    : IClassFixture<TestWebApplicationFactory<Program>>
{
    [Fact]
    public async Task Create_WithValidData_Returns200()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);
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
        var client = await TestAuthHelper.RegisterOnly(factory);
        var response = await client.PostAsJsonAsync("/api/children", new CreateChildRequest("Emma", 7, Gender.Girl, "🦄"));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task List_ReturnsAllFamilyChildren()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);
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
        var client = await TestAuthHelper.RegisterWithFamily(factory);
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
        var client = await TestAuthHelper.RegisterWithFamily(factory);
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
        var client = await TestAuthHelper.RegisterWithFamily(factory);
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

    [Fact]
    public async Task UploadAvatar_WithValidJpeg_SetsAvatarUrl()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);
        var childResp = await client.PostAsJsonAsync("/api/children", new CreateChildRequest("Hero", 7, Gender.Boy, "🦸"));
        var child = await childResp.Content.ReadFromJsonAsync<ChildResponse>();

        using var content = new MultipartFormDataContent();
        var imageBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        content.Add(new ByteArrayContent(imageBytes) { Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg") } }, "file", "avatar.jpg");

        var resp = await client.PostAsync($"/api/children/{child!.Id}/avatar", content);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await resp.Content.ReadFromJsonAsync<ChildResponse>();
        updated!.AvatarUrl.Should().Contain("/uploads/avatars/");
    }

    [Fact]
    public async Task UploadAvatar_WithInvalidExtension_Returns400()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);
        var childResp = await client.PostAsJsonAsync("/api/children", new CreateChildRequest("Hero", 7, Gender.Boy, "🦸"));
        var child = await childResp.Content.ReadFromJsonAsync<ChildResponse>();

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent([0x00]) { Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream") } }, "file", "avatar.gif");

        var resp = await client.PostAsync($"/api/children/{child!.Id}/avatar", content);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadAvatar_ForOtherFamilyChild_Returns404()
    {
        var client1 = await TestAuthHelper.RegisterWithFamily(factory);
        var client2 = await TestAuthHelper.RegisterWithFamily(factory);

        var childResp = await client1.PostAsJsonAsync("/api/children", new CreateChildRequest("Hero", 7, Gender.Boy, "🦸"));
        var child = await childResp.Content.ReadFromJsonAsync<ChildResponse>();

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent([0xFF, 0xD8]) { Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg") } }, "file", "avatar.jpg");

        var resp = await client2.PostAsync($"/api/children/{child!.Id}/avatar", content);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
