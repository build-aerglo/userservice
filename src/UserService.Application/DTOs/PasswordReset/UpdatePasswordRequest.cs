namespace UserService.Application.DTOs.PasswordReset;

public record UpdatePasswordRequest(
    string Email,
    string OldPassword,
    string NewPassword
);
