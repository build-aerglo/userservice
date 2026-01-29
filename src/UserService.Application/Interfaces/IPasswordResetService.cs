using UserService.Application.DTOs.PasswordReset;

namespace UserService.Application.Interfaces;

public interface IPasswordResetService
{
    Task<(bool Success, string Message)> ResetEmailAsync(ResetEmailRequest request);
    Task<(bool Success, string Message)> RequestPasswordResetAsync(RequestPasswordResetRequest request);
    Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordRequest request);
}
