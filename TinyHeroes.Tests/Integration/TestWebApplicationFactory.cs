using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TinyHeroes.Application.Interfaces;
using TinyHeroes.Infrastructure.Data;

namespace TinyHeroes.Tests.Integration;

public class TestWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    private readonly string _dbName = "TestDb_" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:ConnectionString"] = $"disk://path={Path.Combine(Path.GetTempPath(), "tinyheroes-test-uploads")}"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace the DbContextOptions to use InMemory instead of Npgsql.
            // We replace the options singleton - the AppDbContext service itself remains.
            services.Replace(ServiceDescriptor.Singleton<DbContextOptions<AppDbContext>>(sp =>
            {
                var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
                optionsBuilder.UseInMemoryDatabase(_dbName);
                return optionsBuilder.Options;
            }));

            // Replace the real AI image service with a fake for tests.
            var aiDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAiImageService));
            if (aiDescriptor != null) services.Remove(aiDescriptor);
            services.AddScoped<IAiImageService>(_ => new FakeAiImageService());

            // Replace the real file storage service with a fake for tests.
            var fsDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IFileStorageService));
            if (fsDescriptor != null) services.Remove(fsDescriptor);
            services.AddScoped<IFileStorageService>(_ => new FakeFileStorageService());
        });
    }

    private class FakeAiImageService : IAiImageService
    {
        public Task<string> GenerateDataUrlAsync(string prompt, CancellationToken ct = default)
            => Task.FromResult("data:image/jpeg;base64,/9j/FAKEDATA==");
    }

    private class FakeFileStorageService : IFileStorageService
    {
        public Task<string> SaveAsync(Stream content, string subPath, string fileName, CancellationToken ct = default)
            => Task.FromResult($"/uploads/{subPath}/{fileName}");
        public void Delete(string subPath, string fileName) { }
    }
}
