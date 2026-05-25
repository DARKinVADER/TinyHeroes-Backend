using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using TinyHeroes.Application.Interfaces;

namespace TinyHeroes.Infrastructure.Services;

public class AzureBlobStorageService : IFileStorageService
{
    public record BlobConnectionInfo(string AccountName, string AccountKey, string ContainerName)
    {
        public override string ToString() =>
            $"BlobConnectionInfo {{ AccountName = {AccountName}, AccountKey = [REDACTED], ContainerName = {ContainerName} }}";
    }

    private readonly BlobContainerClient _container;
    private readonly string _accountName;
    private readonly string _containerName;

    public AzureBlobStorageService(IConfiguration config)
    {
        var info = ParseConnectionString(config["Storage:ConnectionString"]);
        _accountName = info.AccountName;
        _containerName = info.ContainerName;
        _container = CreateContainerClient(info);
    }

    public async Task<string> SaveAsync(Stream content, string subPath, string fileName, CancellationToken ct = default)
    {
        var blobName = $"{subPath}/{fileName}";
        var blob = _container.GetBlobClient(blobName);

        var contentType = Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };

        await blob.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        }, ct);

        return $"https://{_accountName}.blob.core.windows.net/{_containerName}/{blobName}";
    }

    public void Delete(string subPath, string fileName)
    {
        _container.GetBlobClient($"{subPath}/{fileName}").DeleteIfExists();
    }

    public static BlobConnectionInfo ParseConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Storage:ConnectionString is not configured.");

        var body = connectionString.StartsWith("azureblob://", StringComparison.OrdinalIgnoreCase)
            ? connectionString["azureblob://".Length..]
            : connectionString;

        var parts = body.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split('=', 2))
            .Where(p => p.Length == 2)
            .ToDictionary(p => p[0].Trim(), p => p[1].Trim(), StringComparer.OrdinalIgnoreCase);

        if (!parts.TryGetValue("AccountName", out var accountName) || string.IsNullOrEmpty(accountName))
            throw new InvalidOperationException("Storage connection string is missing AccountName.");
        if (!parts.TryGetValue("AccountKey", out var accountKey) || string.IsNullOrEmpty(accountKey))
            throw new InvalidOperationException("Storage connection string is missing AccountKey.");
        if (!parts.TryGetValue("ContainerName", out var containerName) || string.IsNullOrEmpty(containerName))
            throw new InvalidOperationException("Storage connection string is missing ContainerName.");

        return new BlobConnectionInfo(accountName, accountKey, containerName);
    }

    private static BlobContainerClient CreateContainerClient(BlobConnectionInfo info)
    {
        var serviceUri = new Uri($"https://{info.AccountName}.blob.core.windows.net");
        var credential = new Azure.Storage.StorageSharedKeyCredential(info.AccountName, info.AccountKey);
        return new BlobServiceClient(serviceUri, credential).GetBlobContainerClient(info.ContainerName);
    }
}
