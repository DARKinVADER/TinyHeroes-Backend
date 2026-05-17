using Microsoft.Extensions.Configuration;
using TinyHeroes.Application.Interfaces;

namespace TinyHeroes.Infrastructure.Services;

public class LocalFileStorageService(IConfiguration config) : IFileStorageService
{
    private string BasePath => ParseBasePath(config["Storage:ConnectionString"]);

    public async Task<string> SaveAsync(Stream content, string subPath, string fileName, CancellationToken ct = default)
    {
        var dir = Path.Combine(BasePath, subPath);
        Directory.CreateDirectory(dir);
        var fullPath = Path.Combine(dir, fileName);
        await using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        await content.CopyToAsync(fs, ct);
        return $"/uploads/{subPath}/{fileName}";
    }

    public void Delete(string subPath, string fileName)
    {
        var fullPath = Path.Combine(BasePath, subPath, fileName);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }

    private static string ParseBasePath(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return "/app/uploads";
        var idx = connectionString.IndexOf("path=", StringComparison.OrdinalIgnoreCase);
        return idx >= 0 ? connectionString[(idx + 5)..] : "/app/uploads";
    }
}
