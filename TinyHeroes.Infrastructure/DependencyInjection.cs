using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TinyHeroes.Application.Interfaces;
using TinyHeroes.Application.Settings;
using TinyHeroes.Infrastructure.Data;
using TinyHeroes.Infrastructure.Services;

namespace TinyHeroes.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseNpgsql(config.GetConnectionString("Default")));

        services.AddHttpClient();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<ISummaryService, SummaryService>();
        services.AddScoped<IAiImageService, HuggingFaceImageService>();
        services.AddScoped<IEmailService, EmailService>();
        services.Configure<EmailSettings>(config.GetSection("Email"));
        var storageConn = config["Storage:ConnectionString"] ?? "";
        if (storageConn.StartsWith("azureblob://", StringComparison.OrdinalIgnoreCase))
            services.AddScoped<IFileStorageService, AzureBlobStorageService>();
        else
            services.AddScoped<IFileStorageService, LocalFileStorageService>();

        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>("database");

        return services;
    }
}
