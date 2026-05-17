using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using TinyHeroes.Application.Interfaces;

namespace TinyHeroes.Infrastructure.Services;

public class HuggingFaceImageService(IHttpClientFactory httpFactory, IConfiguration config) : IAiImageService
{
    public async Task<string> GenerateDataUrlAsync(string prompt, CancellationToken ct = default)
    {
        var apiKey = config["AiImage:HuggingFace:ApiKey"];
        var model  = config["AiImage:HuggingFace:Model"] ?? "black-forest-labs/FLUX.1-schnell";

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Hugging Face API key is not configured.");

        var client = httpFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var url  = $"https://api-inference.huggingface.co/models/{model}";
        var body = new { inputs = prompt };

        var response = await client.PostAsJsonAsync(url, body, ct);
        response.EnsureSuccessStatusCode();

        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
        return $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";
    }
}
