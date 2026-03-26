namespace UserService.Application.DTOs;

public record RegisterBusinessDto(
    Guid BusinessId,
    string Email,
    string Password,
    string? PhoneNumber
);

public record RegisterBusinessResultDto(
    Guid UserId,
    Guid BusinessId,
    string Username,
    string Email,
    string? Phone,
    string Auth0UserId,
    DateTime CreatedAt
);
