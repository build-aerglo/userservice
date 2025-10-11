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
);