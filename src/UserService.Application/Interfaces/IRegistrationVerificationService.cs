using UserService.Application.DTOs.Verification;

namespace UserService.Application.Interfaces;

public interface IRegistrationVerificationService
{
    /// <summary>
    /// Creates a registration_verification entry and sends a verification email to the user.
    /// Called after end_user or business_user sign-up.
    /// </summary>
    Task SendVerificationEmailAsync(string email, string username, string userType);

    /// <summary>
    /// Verifies the registration email using the token from the email link.
    /// Decrypts the token, validates it, updates user/business records and cleans up.
    /// </summary>
    Task<VerifyRegistrationEmailResultDto> VerifyEmailAsync(string email, string token);

    /// <summary>
    /// Re-sends the registration verification email for an existing user.
    /// Fetches the user to get their username, then runs the same send flow.
    /// </summary>
    Task<ReverifyEmailResultDto> ReverifyEmailAsync(string email);
}
