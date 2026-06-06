namespace TinyHeroes.Api.Services;

public class BypassCaptchaService : ICaptchaService
{
    public Task<bool> ValidateAsync(string token, CancellationToken ct = default)
        => Task.FromResult(true);
}
