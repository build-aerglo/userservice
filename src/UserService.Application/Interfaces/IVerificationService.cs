using UserService.Application.DTOs.Verification;

namespace UserService.Application.Interfaces;

public interface IVerificationService
{
    /// <summary>
    /// Get user's verification status
    /// </summary>
    Task<UserVerificationStatusDto> GetVerificationStatusAsync(Guid userId);

    /// <summary>
    /// Send OTP to user's phone number
    /// </summary>
    Task<SendOtpResponseDto> SendPhoneOtpAsync(SendPhoneOtpDto dto);

    /// <summary>
    /// Verify phone OTP
    /// </summary>
    Task<VerifyOtpResponseDto> VerifyPhoneOtpAsync(VerifyPhoneOtpDto dto);

    /// <summary>
    /// Send email verification link
    /// </summary>
    Task<SendEmailVerificationResponseDto> SendEmailVerificationAsync(SendEmailVerificationDto dto);

    /// <summary>
    /// Verify email token
    /// </summary>
    Task<VerifyEmailResponseDto> VerifyEmailAsync(VerifyEmailDto dto);

    /// <summary>
    /// Check if user is verified (at least partially)
    /// </summary>
    Task<bool> IsUserVerifiedAsync(Guid userId);

    /// <summary>
    /// Check if user is fully verified (both phone and email)
    /// </summary>
    Task<bool> IsUserFullyVerifiedAsync(Guid userId);

    /// <summary>
    /// Get verification level string (unverified, partial, fully_verified)
    /// </summary>
    Task<string> GetVerificationLevelAsync(Guid userId);

    /// <summary>
    /// Get points multiplier based on verification status (1.0 or 1.5)
    /// </summary>
    Task<decimal> GetPointsMultiplierAsync(Guid userId);

    /// <summary>
    /// Initialize verification record for new user
    /// </summary>
    Task InitializeUserVerificationAsync(Guid userId);

    /// <summary>
    /// Validate Nigerian phone number format
    /// </summary>
    bool ValidateNigerianPhoneNumber(string phoneNumber);

    /// <summary>
    /// Clean up expired verification tokens
    /// </summary>
    Task CleanupExpiredTokensAsync();
}
