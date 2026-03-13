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

    /// <summary>
    /// Sends a templated notification via the Notification Service.
    /// </summary>
    /// <param name="template">The notification template name (e.g., "registeration")</param>
    /// <param name="recipient">The recipient identifier (e.g., email address)</param>
    /// <param name="channel">The delivery channel (e.g., "email", "sms")</param>
    /// <param name="payload">The template payload data</param>
    /// <returns>True if notification was sent successfully, otherwise false</returns>
    Task<bool> SendNotificationAsync(string template, string recipient, string channel, object payload);
}
