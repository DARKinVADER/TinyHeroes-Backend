namespace TinyHeroes.Application.DTOs.User;

public record UserProfileResponse(
    string UserId,
    string DisplayName,
    string Email,
    string PreferredLanguage,
    bool PushNotificationsEnabled,
    bool WeeklyEmailEnabled,
    string PreferredTheme);

public record UpdateUserProfileRequest(
    string? DisplayName,
    string? PreferredLanguage,
    bool? PushNotificationsEnabled,
    bool? WeeklyEmailEnabled,
    string? PreferredTheme);
