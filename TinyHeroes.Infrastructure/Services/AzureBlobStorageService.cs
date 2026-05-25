using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using TinyHeroes.Application.Interfaces;

namespace TinyHeroes.Infrastructure.Services;

public class AzureBlobStorageService(IConfiguration config) : IFileStorageService
{
    public record BlobConnectionInfo(string AccountName, string AccountKey, string ContainerName);

    public async Task<string> SaveAsync(Stream content, string subPath, string fileName, CancellationToken ct = default)
    {
        var info = ParseConnectionString(config["Storage:ConnectionString"]);
        var container = CreateContainerClient(info);
        var blobName = $"{subPath}/{fileName}";
        var blob = container.GetBlobClient(blobName);
        await blob.UploadAsync(content, new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders() }, ct);
        return $"https://{info.AccountName}.blob.core.windows.net/{info.ContainerName}/{blobName}";
    }

    public void Delete(string subPath, string fileName)
    {
        var info = ParseConnectionString(config["Storage:ConnectionString"]);
        var container = CreateContainerClient(info);
        container.GetBlobClient($"{subPath}/{fileName}").DeleteIfExists();
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
