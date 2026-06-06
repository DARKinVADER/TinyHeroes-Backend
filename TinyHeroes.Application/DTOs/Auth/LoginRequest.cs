namespace TinyHeroes.Application.DTOs.Auth;

public record LoginRequest(string Email, string Password, string CaptchaToken = "");
