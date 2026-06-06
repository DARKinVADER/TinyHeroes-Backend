namespace TinyHeroes.Api.Services;

public interface ICaptchaService
{
    Task<bool> ValidateAsync(string token, CancellationToken ct = default);
}
