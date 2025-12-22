namespace UserService.Application.DTOs;

public record UpdateEndUserProfileDto(
    // User basic info (optional updates)
    string? Username,
    string? Phone,
    string? Address,
    
    // End user profile info
    string? SocialMedia,
    
    // User settings
    NotificationPreferencesDto? NotificationPreferences,
    bool? DarkMode
);