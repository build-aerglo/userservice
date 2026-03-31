namespace UserService.Application.DTOs;

public record ContactDto(
    string? Name,
    string Email,
    string? Subject,
    string Message
);
