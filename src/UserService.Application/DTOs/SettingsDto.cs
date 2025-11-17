using UserService.Domain.Entities;

namespace UserService.Application.DTOs;

public record SettingsDto(
    Guid UserId,
    List<string> NotificationPreferences,
    bool DarkMode
    );