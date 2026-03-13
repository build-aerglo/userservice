using Microsoft.Extensions.Logging;
using UserService.Application.DTOs.Verification;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Domain.Repositories;

namespace UserService.Application.Services;

public class RegistrationVerificationService(
    IRegistrationVerificationRepository registrationVerificationRepository,
    IUserRepository userRepository,
    IEncryptionService encryptionService,
    INotificationServiceClient notificationClient,
    IBusinessServiceClient businessServiceClient,
    ILogger<RegistrationVerificationService> logger
) : IRegistrationVerificationService
{
    private const string VerificationBaseUrl = "https://www.clereview.com/auth/verify-email";

    /// <summary>
    /// Creates a registration_verification entry and sends the verification email.
    /// Any existing pending entry for the email is replaced.
    /// </summary>
    public async Task SendVerificationEmailAsync(string email, string username, string userType)
    {
        // Encrypt the email to create the verification token
        var token = encryptionService.Encrypt(email);

        // Remove any previous pending entry for this email
        await registrationVerificationRepository.DeleteByEmailAsync(email);

        // Persist the new verification record
        var verification = new RegistrationVerification(email, username, token, userType);
        await registrationVerificationRepository.AddAsync(verification);

        // Build the verification URL
        var encodedToken = Uri.EscapeDataString(token);
        var encodedEmail = Uri.EscapeDataString(email);
        var url = $"{VerificationBaseUrl}?token={encodedToken}&e={encodedEmail}";

        // Send the notification — failure is logged but does not throw so that
        // the registration itself is not rolled back.
        var sent = await notificationClient.SendNotificationAsync(
            template: "registeration",
            recipient: email,
            channel: "email",
            payload: new { username, url }
        );

        if (!sent)
            logger.LogWarning("Verification email could not be delivered to {Email}", email);
    }

    /// <summary>
    /// Validates the registration email token, marks the user as verified,
    /// updates the business_verification record (for business users) and
    /// removes the pending registration_verification entry.
    /// </summary>
    public async Task<VerifyRegistrationEmailResultDto> VerifyEmailAsync(string email, string token)
    {
        // a) Decrypt token and confirm the email matches
        string decryptedEmail;
        try
        {
            decryptedEmail = encryptionService.Decrypt(token);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to decrypt registration token for email {Email}", email);
            throw new InvalidVerificationTokenException("Invalid token.");
        }

        if (!string.Equals(decryptedEmail, email, StringComparison.OrdinalIgnoreCase))
            throw new InvalidVerificationTokenException("Token does not match the provided email.");

        // b) Confirm the user exists in the users table
        var user = await userRepository.GetByEmailAsync(email);
        if (user is null)
            throw new EndUserNotFoundException(Guid.Empty);

        // Confirm a pending registration_verification entry exists
        var verification = await registrationVerificationRepository.GetByEmailAsync(email);
        if (verification is null)
            throw new InvalidVerificationTokenException("No pending verification found for this email.");

        // Check expiry
        if (verification.IsExpired)
            throw new VerificationTokenExpiredException();

        // c) For business_user: update business_verification via business service
        if (string.Equals(user.UserType, "business_user", StringComparison.OrdinalIgnoreCase))
        {
            var businessId = await businessServiceClient.GetBusinessIdByEmailAsync(email);
            if (businessId.HasValue)
            {
                var updated = await businessServiceClient.MarkBusinessEmailVerifiedAsync(businessId.Value, email);
                if (!updated)
                    logger.LogWarning("Could not update business_verification for business {BusinessId}", businessId.Value);
            }
            else
            {
                logger.LogWarning("No business found for email {Email} during email verification", email);
            }
        }

        // For both user types: mark is_email_verified on users table
        await userRepository.UpdateEmailVerifiedAsync(user.Id);

        // d) Clean up the pending registration_verification entry
        await registrationVerificationRepository.DeleteByEmailAsync(email);

        logger.LogInformation("Email verified successfully for {Email} (userType={UserType})", email, user.UserType);

        return new VerifyRegistrationEmailResultDto(
            Success: true,
            Message: "Email verified successfully."
        );
    }

    /// <summary>
    /// Re-sends the verification email for an existing user who has not yet
    /// verified their registration email.
    /// </summary>
    public async Task<ReverifyEmailResultDto> ReverifyEmailAsync(string email)
    {
        // Fetch user to get username and userType
        var user = await userRepository.GetByEmailAsync(email);
        if (user is null)
            throw new EndUserNotFoundException(Guid.Empty);

        await SendVerificationEmailAsync(email, user.Username, user.UserType);

        // Return the expiry so the caller knows when the new link expires
        var expiry = DateTime.UtcNow.AddHours(24);

        return new ReverifyEmailResultDto(
            Success: true,
            Message: "Verification email resent successfully.",
            ExpiresAt: expiry
        );
    }
}
