namespace UserService.Application.DTOs;

public record UpdateEndUserProfileDto(
    string? Username,
    string? Phone,
    string? Address,
    string? SocialMedia,
    NotificationPreferencesDto? NotificationPreferences,
    bool? DarkMode
);