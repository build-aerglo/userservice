namespace UserService.Application.DTOs;

public record EndUserDto(
    Guid UserId,
    string? Preferences,
    string? Bio,
    string? SocialLinks,
    string Username,
    string Email,
    string Phone,
    string UserType,
    string? Address
    );