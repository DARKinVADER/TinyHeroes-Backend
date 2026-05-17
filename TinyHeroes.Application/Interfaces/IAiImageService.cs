namespace TinyHeroes.Application.Interfaces;

public interface IAiImageService
{
    /// <summary>
    /// Generates an image from a text prompt.
    /// Returns the image as a base64-encoded data URL (e.g., "data:image/jpeg;base64,...").
    /// Throws InvalidOperationException if the provider is not configured.
    /// </summary>
    Task<string> GenerateDataUrlAsync(string prompt, CancellationToken ct = default);
}
