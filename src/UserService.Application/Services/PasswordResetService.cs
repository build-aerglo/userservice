using UserService.Application.DTOs.PasswordReset;
using UserService.Application.Interfaces;
using UserService.Application.Services.Auth0;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Application.Services;

public class PasswordResetService(
    IUserRepository userRepository,
    IPasswordResetRequestRepository passwordResetRequestRepository,
    IAuth0ManagementService auth0ManagementService,
    IBusinessServiceClient businessServiceClient,
    INotificationServiceClient notificationServiceClient,
    IEncryptionService encryptionService
) : IPasswordResetService
{
    public async Task<(bool Success, string Message)> ResetEmailAsync(ResetEmailRequest request)
    {
        var user = await userRepository.GetByEmailAsync(request.CurrentEmail);
        if (user is null)
        {
            return (false, "Email does not exist");
        }

        var auth0Updated = await auth0ManagementService.UpdateEmailAsync(user.Auth0UserId, request.NewEmail);
        if (!auth0Updated)
        {
            return (false, "Failed to update email on Auth0");
        }

        await userRepository.UpdateEmailAsync(user.Id, request.NewEmail);

        if (user.UserType == "business_user")
        {
            var businessUpdated = await businessServiceClient.UpdateBusinessEmailAsync(
                request.CurrentEmail,
                request.NewEmail
            );

            if (!businessUpdated)
            {
                Console.WriteLine($"[ResetEmailAsync] Warning: Failed to update business email for {request.CurrentEmail}");
            }
        }

        return (true, "Email updated successfully");
    }

    public async Task<(bool Success, string Message)> RequestPasswordResetAsync(RequestPasswordResetRequest request)
    {
        if (request.Type != "email" && request.Type != "sms")
        {
            return (false, "Type must be 'email' or 'sms'");
        }

        var user = request.Type == "email"
            ? await userRepository.GetByEmailAsync(request.Id)
            : await userRepository.GetByPhoneAsync(request.Id);

        if (user is null)
        {
            return (false, "Id does not exist");
        }

        var otpCreated = await notificationServiceClient.CreateOtpAsync(
            request.Id,
            request.Type,
            "resetpassword"
        );

        if (!otpCreated)
        {
            return (false, "Failed to send OTP");
        }

        var resetRequest = new PasswordResetRequest(
            user.Id,
            request.Id,
            request.Type
        );

        await passwordResetRequestRepository.AddAsync(resetRequest);

        return (true, "OTP sent successfully");
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var resetRequest = await passwordResetRequestRepository.GetByIdentifierAsync(request.Id);

        if (resetRequest is null)
        {
            return (false, "No verified password reset request found for this identifier");
        }

        if (resetRequest.IsExpired())
        {
            return (false, "Password reset request has expired");
        }

        var user = resetRequest.IdentifierType == "email"
            ? await userRepository.GetByEmailAsync(request.Id)
            : await userRepository.GetByPhoneAsync(request.Id);

        if (user is null)
        {
            return (false, "User not found");
        }

        string decryptedPassword;
        try
        {
            decryptedPassword = encryptionService.Decrypt(request.Password);
        }
        catch (Exception)
        {
            return (false, "Invalid password format");
        }

        var passwordUpdated = await auth0ManagementService.UpdatePasswordAsync(user.Auth0UserId, decryptedPassword);

        if (!passwordUpdated)
        {
            return (false, "Failed to update password");
        }

        return (true, "Password updated");
    }
}
