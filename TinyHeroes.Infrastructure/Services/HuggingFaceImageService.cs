using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
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

        var url  = $"https://api-inference.huggingface.co/models/{model}";
        var json = JsonSerializer.Serialize(new { inputs = prompt });

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", apiKey) },
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var client = httpFactory.CreateClient();
        var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
        return $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";
    }
}
