using FluentAssertions;
using Microsoft.AspNetCore.Hosting;

namespace TinyHeroes.Tests.Integration;

public class OpenApiTests
{
    [Fact]
    public async Task OpenApiJson_Returns200_InDevelopment()
    {
        await using var factory = new DevFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task ScalarUi_ReturnsSuccessOrRedirect_InDevelopment()
    {
        await using var factory = new DevFactory();
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/scalar/v1");

        ((int)response.StatusCode).Should().BeOneOf(200, 301, 302);
    }

    // Inherits fake DB, AI, and file services from TestWebApplicationFactory
    // then overrides the environment to Development so MapOpenApi/MapScalarApiReference are registered.
    private class DevFactory : TestWebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseEnvironment("Development");
        }
    }
}
