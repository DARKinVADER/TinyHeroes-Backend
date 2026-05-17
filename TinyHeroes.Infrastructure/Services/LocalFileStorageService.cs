using Microsoft.Extensions.Configuration;
using TinyHeroes.Application.Interfaces;

namespace TinyHeroes.Infrastructure.Services;

public class LocalFileStorageService(IConfiguration config) : IFileStorageService
{
    private string BasePath => ParseBasePath(config["Storage:ConnectionString"]);

    public async Task<string> SaveAsync(Stream content, string subPath, string fileName, CancellationToken ct = default)
    {
        var fullPath = ResolveSafe(subPath, fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        await content.CopyToAsync(fs, ct);
        return $"/uploads/{subPath}/{fileName}";
    }

    public void Delete(string subPath, string fileName)
    {
        var fullPath = ResolveSafe(subPath, fileName);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }

    private string ResolveSafe(string subPath, string fileName)
    {
        var root = Path.GetFullPath(BasePath);
        var resolved = Path.GetFullPath(Path.Combine(root, subPath, fileName));
        if (!resolved.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Path is outside the storage root.");
        return resolved;
    }

    private static string ParseBasePath(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return "/app/uploads";
        var idx = connectionString.IndexOf("path=", StringComparison.OrdinalIgnoreCase);
        return idx >= 0 ? connectionString[(idx + 5)..] : "/app/uploads";
    }
}
