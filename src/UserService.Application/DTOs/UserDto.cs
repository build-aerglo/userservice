namespace UserService.Application.DTOs;

public record UserDto(
    string Username,
    string Email,
    string Phone,
    string UserType,
    string? Address
);

public record UpdateUserDto(
    string? Email,
    string? Phone,
    string? Address
);public record EndUserDto(
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