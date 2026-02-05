namespace UserService.Application.Interfaces;

/// <summary>
/// Contract for communication with the external Notification Service.
/// Used to send OTPs and notifications.
/// </summary>
public interface INotificationServiceClient
{
    /// <summary>
    /// Creates an OTP request via the Notification Service.
    /// </summary>
    /// <param name="id">The identifier (email or phone)</param>
    /// <param name="type">The type of notification (sms or email)</param>
    /// <param name="purpose">The purpose of the OTP (e.g., resetpassword)</param>
    /// <returns>True if OTP was created successfully, otherwise false</returns>
    Task<bool> CreateOtpAsync(string id, string type, string purpose);
}
