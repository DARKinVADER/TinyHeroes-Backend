using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using FluentAssertions;
using Xunit;

namespace TinyHeroes.Tests.Integration;

public class PasswordHasherTests : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;

    public PasswordHasherTests(TestWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public void PasswordHasher_ShouldBeConfiguredWith600000Iterations()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<PasswordHasherOptions>>();

        // Assert
        options.Value.IterationCount.Should().Be(600000);
    }
}
