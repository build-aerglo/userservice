using Microsoft.Extensions.Configuration;
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
    IBusinessRepository businessRepository,
    ILogger<RegistrationVerificationService> logger,
    IConfiguration config,
    IReviewActivationClient reviewActivationClient  // RS-DeferredAuth
) : IRegistrationVerificationService
{

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
        var accountType = userType.Equals("business_user", StringComparison.OrdinalIgnoreCase) ? "business" : "user";
        var frontendUrl = config["FrontendUrl"]?.TrimEnd('/');
        var url = $"{frontendUrl}/auth/verify-email?token={encodedToken}&e={encodedEmail}&type={accountType}";
        
        // Resolve user type from DB to pick the correct template
        var template = userType.Equals("business_user", StringComparison.OrdinalIgnoreCase)
            ? "registeration-business"
            : "registeration";

        // Send the notification — failure is logged but does not throw so that
        // the registration itself is not rolled back.
        var sent = await notificationClient.SendNotificationAsync(
            template: template,
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

        if (user.IsEmailVerified)
            return new VerifyRegistrationEmailResultDto(true, "Email is already verified.");

        // Confirm a pending registration_verification entry exists
        var verification = await registrationVerificationRepository.GetByEmailAsync(email);
        if (verification is null)
            throw new InvalidVerificationTokenException("No pending verification found for this email.");

        // Check expiry
        if (verification.IsExpired)
            throw new VerificationTokenExpiredException();

        // c) For business_user: mark is_verified on the business table
        if (string.Equals(user.UserType, "business_user", StringComparison.OrdinalIgnoreCase))
        {
            var businessId = await businessRepository.GetIdByEmailAsync(email);
            if (businessId.HasValue)
            {
                await businessRepository.MarkEmailVerifiedAsync(businessId.Value);
                await businessRepository.MarkEmailVerifiedOnVerificationTableAsync(businessId.Value);
            }
            
            else
                logger.LogWarning("No business found for email {Email} during email verification", email);
        }

        // For both user types: mark is_email_verified on users table
        await userRepository.UpdateEmailVerifiedAsync(user.Id);

        // d) Clean up the pending registration_verification entry
        await registrationVerificationRepository.DeleteByEmailAsync(email);

        // RS-DeferredAuth: Activate any pending-verification reviews for this user.
        // Fire-and-forget — do not let a ReviewService failure break the verification response.
        _ = reviewActivationClient.ActivateReviewsForUserAsync(user.Id);

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

        if (user.IsEmailVerified)
            return new ReverifyEmailResultDto(false, "Email is already verified.", DateTime.UtcNow);

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
