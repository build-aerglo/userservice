using Microsoft.Extensions.Logging;
using UserService.Application.DTOs.PasswordReset;
using UserService.Application.Interfaces;
using UserService.Application.Services.Auth0;
using UserService.Domain.Repositories;

namespace UserService.Application.Services;

public class PasswordResetService(
    IUserRepository userRepository,
    IPasswordResetRequestRepository passwordResetRequestRepository,
    IAuth0ManagementService auth0ManagementService,
    IAuth0UserLoginService auth0UserLoginService,
    IBusinessServiceClient businessServiceClient,
    INotificationServiceClient notificationServiceClient,
    IEncryptionService encryptionService,
    ILogger<PasswordResetService> logger
) : IPasswordResetService
{
    public async Task<(bool Success, string Message)> ResetEmailAsync(ResetEmailRequest request)
    {
        var user = await userRepository.GetByEmailAsync(request.CurrentEmail);
        if (user is null)
            return (false, "Email does not exist");

        if (string.IsNullOrEmpty(user.Auth0UserId))
            return (false, "User account is not linked to Auth0");

        var auth0Updated = await auth0ManagementService.UpdateEmailAsync(user.Auth0UserId, request.NewEmail);
        if (!auth0Updated)
            return (false, "Failed to update email on Auth0");

        await userRepository.UpdateEmailAsync(user.Id, request.NewEmail);

        if (user.UserType == "business_user")
        {
            var businessUpdated = await businessServiceClient.UpdateBusinessEmailAsync(
                request.CurrentEmail,
                request.NewEmail
            );

            if (!businessUpdated)
                logger.LogWarning("Failed to propagate email update to BusinessService for {Email}", request.CurrentEmail);
        }

        return (true, "Email updated successfully");
    }

    public async Task<(bool Success, string Message)> RequestPasswordResetAsync(RequestPasswordResetRequest request)
    {
        if (request.Type != "email" && request.Type != "sms")
            return (false, "Type must be 'email' or 'sms'");

        var user = request.Type == "email"
            ? await userRepository.GetByEmailAsync(request.Id)
            : await userRepository.GetByPhoneAsync(request.Id);

        if (user is null)
            return (false, "Id does not exist");

        var otpCreated = await notificationServiceClient.CreateOtpAsync(
            request.Id,
            request.Type,
            "resetpassword"
        );

        if (!otpCreated)
            return (false, "Failed to send OTP");

        return (true, "OTP sent successfully");
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var resetRequest = await passwordResetRequestRepository.GetByIdAsync(request.Id);

        if (resetRequest is null)
            return (false, "No password reset request found");

        if (resetRequest.IsExpired())
            return (false, "Password reset request has expired");

        var user = await userRepository.GetByEmailOrPhoneAsync(request.Id);

        if (user is null)
            return (false, "User not found");

        if (string.IsNullOrEmpty(user.Auth0UserId))
            return (false, "User account is not linked to Auth0");

        string decryptedPassword;
        try
        {
            decryptedPassword = encryptionService.Decrypt(request.Password);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Password decryption failed during reset for user {Id}", request.Id);
            return (false, "Invalid password format");
        }

        var passwordUpdated = await auth0ManagementService.UpdatePasswordAsync(user.Auth0UserId, decryptedPassword);

        if (!passwordUpdated)
            return (false, "Failed to update password");

        await passwordResetRequestRepository.DeleteByIdAsync(request.Id);

        return (true, "Password updated");
    }

    public async Task<(bool Success, string Message)> UpdatePasswordAsync(UpdatePasswordRequest request)
    {
        var user = await userRepository.GetByEmailAsync(request.Email);

        if (user is null)
            return (false, "User not found");

        if (string.IsNullOrEmpty(user.Auth0UserId))
            return (false, "User account is not linked to Auth0");

        string decryptedOldPassword;
        string decryptedNewPassword;

        try
        {
            decryptedOldPassword = encryptionService.Decrypt(request.OldPassword);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Old password decryption failed for {Email}", request.Email);
            return (false, "Invalid old password format");
        }

        try
        {
            decryptedNewPassword = encryptionService.Decrypt(request.NewPassword);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "New password decryption failed for {Email}", request.Email);
            return (false, "Invalid new password format");
        }

        try
        {
            await auth0UserLoginService.LoginAsync(request.Email, decryptedOldPassword);
        }
        catch (Exception)
        {
            return (false, "Current password is incorrect");
        }

        var passwordUpdated = await auth0ManagementService.UpdatePasswordAsync(user.Auth0UserId, decryptedNewPassword);

        if (!passwordUpdated)
            return (false, "Failed to update password");

        return (true, "Password updated successfully");
    }
}
