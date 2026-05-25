using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TinyHeroes.Application.Interfaces;
using TinyHeroes.Infrastructure;
using TinyHeroes.Infrastructure.Services;

namespace TinyHeroes.Tests.Unit;

public class AzureBlobStorageServiceTests
{
    [Fact]
    public void ParseConnectionString_ExtractsAccountName()
    {
        var cs = "azureblob://AccountName=testaccount;AccountKey=dGVzdA==;ContainerName=uploads";
        var parsed = AzureBlobStorageService.ParseConnectionString(cs);
        parsed.AccountName.Should().Be("testaccount");
    }

    [Fact]
    public void ParseConnectionString_ExtractsAccountKey()
    {
        var cs = "azureblob://AccountName=testaccount;AccountKey=dGVzdA==;ContainerName=uploads";
        var parsed = AzureBlobStorageService.ParseConnectionString(cs);
        parsed.AccountKey.Should().Be("dGVzdA==");
    }

    [Fact]
    public void ParseConnectionString_ExtractsContainerName()
    {
        var cs = "azureblob://AccountName=testaccount;AccountKey=dGVzdA==;ContainerName=uploads";
        var parsed = AzureBlobStorageService.ParseConnectionString(cs);
        parsed.ContainerName.Should().Be("uploads");
    }

    [Fact]
    public void ParseConnectionString_ThrowsOnMissingAccountName()
    {
        var cs = "azureblob://AccountKey=dGVzdA==;ContainerName=uploads";
        var act = () => AzureBlobStorageService.ParseConnectionString(cs);
        act.Should().Throw<InvalidOperationException>().WithMessage("*AccountName*");
    }

    [Theory]
    [InlineData("azureblob://AccountName=a;AccountKey=b;ContainerName=uploads", typeof(AzureBlobStorageService))]
    [InlineData("disk://path=/app/uploads", typeof(LocalFileStorageService))]
    public void DependencyInjection_RegistersCorrectImplementation(string connectionString, Type expectedType)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:ConnectionString"] = connectionString,
                ["ConnectionStrings:Default"] = "Host=localhost;Database=test",
                ["Jwt:Secret"] = "test-secret-that-is-long-enough-32chars",
                ["Jwt:Issuer"] = "test",
                ["Jwt:Audience"] = "test",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient();
        services.AddSingleton<IConfiguration>(config);
        services.AddInfrastructure(config);

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IFileStorageService>();
        service.Should().BeOfType(expectedType);
    }
}
