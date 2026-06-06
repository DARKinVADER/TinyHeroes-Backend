using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TinyHeroes.Api.Services;

public class TurnstileCaptchaService(HttpClient http, IConfiguration config, ILogger<TurnstileCaptchaService> logger)
    : ICaptchaService
{
    private const string SiteverifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<bool> ValidateAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;

        try
        {
            var secret = config["Turnstile:SecretKey"] ?? "";
            var response = await http.PostAsJsonAsync(SiteverifyUrl, new { secret, response = token }, ct);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<TurnstileResult>(JsonOptions, ct);
            return result?.Success ?? false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Turnstile siteverify call failed — failing closed");
            return false;
        }
    }

    private record TurnstileResult(bool Success);
}
