using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TinyHeroes.Infrastructure.Data;

namespace TinyHeroes.Tests.Integration;

public class TestWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    private readonly string _dbName = "TestDb_" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

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
        });
    }
}
