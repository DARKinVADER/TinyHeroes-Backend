namespace TinyHeroes.Application.DTOs.User;

public record UserProfileResponse(
    string UserId,
    string DisplayName,
    string Email,
    string PreferredLanguage,
    bool PushNotificationsEnabled,
    bool WeeklyEmailEnabled);

public record UpdateUserProfileRequest(
    string? DisplayName,
    string? PreferredLanguage,
    bool? PushNotificationsEnabled,
    bool? WeeklyEmailEnabled);
