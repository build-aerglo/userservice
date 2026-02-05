namespace UserService.Application.DTOs.PasswordReset;

public record ResetPasswordRequest(
    string Id,
    string Password
);
