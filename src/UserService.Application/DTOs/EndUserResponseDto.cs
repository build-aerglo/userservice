namespace UserService.Application.DTOs;

public record EndUserResponseDto(
    Guid UserId,
    Guid EndUserProfileId,
    string Username,
    string Email,
    string Phone,
    string? Address,
    string? SocialMedia,
    DateTime CreatedAt
);