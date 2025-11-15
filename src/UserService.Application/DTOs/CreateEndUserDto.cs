namespace UserService.Application.DTOs;

public record CreateEndUserDto(
    string Username,
    string Email,
    string Password,
    string Phone,
    string? Address,
    string? SocialMedia
);