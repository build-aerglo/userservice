namespace UserService.Application.DTOs.PasswordReset;

public record RequestPasswordResetRequest(
    string Id,
    string Type
);
