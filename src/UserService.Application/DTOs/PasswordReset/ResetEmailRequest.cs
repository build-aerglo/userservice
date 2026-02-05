namespace UserService.Application.DTOs.PasswordReset;

public record ResetEmailRequest(
    string CurrentEmail,
    string NewEmail
);
