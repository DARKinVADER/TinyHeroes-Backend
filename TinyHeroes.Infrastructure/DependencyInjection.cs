using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TinyHeroes.Application.Interfaces;
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

        return services;
    }
}
