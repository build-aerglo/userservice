using UserService.Application.DTOs.Verification;

namespace UserService.Application.Interfaces;

public interface IVerificationService
{
    // Status
    Task<VerificationStatusDto> GetVerificationStatusAsync(Guid userId);
    Task<VerificationStatusDto> GetOrCreateVerificationStatusAsync(Guid userId);

    // Email verification
    Task<EmailVerificationDto> SendEmailVerificationAsync(SendEmailVerificationDto dto);
    Task<VerificationResultDto> VerifyEmailAsync(VerifyEmailDto dto);
    Task<VerificationResultDto> VerifyEmailByTokenAsync(VerifyEmailByTokenDto dto);
    Task<EmailVerificationDto> ResendEmailVerificationAsync(Guid userId);
    Task<EmailVerificationDto?> GetActiveEmailVerificationAsync(Guid userId);

    // Phone verification
    Task<PhoneVerificationDto> SendPhoneVerificationAsync(SendPhoneVerificationDto dto);
    Task<VerificationResultDto> VerifyPhoneAsync(VerifyPhoneDto dto);
    Task<PhoneVerificationDto> ResendPhoneVerificationAsync(Guid userId);
    Task<PhoneVerificationDto?> GetActivePhoneVerificationAsync(Guid userId);

    // Admin operations
    Task CleanupExpiredVerificationsAsync();
}
