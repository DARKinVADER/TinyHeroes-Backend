namespace TinyHeroes.Application.DTOs.Auth;

public record AuthResponse(string AccessToken, string UserId, string DisplayName, string Email, bool HasFamily);
