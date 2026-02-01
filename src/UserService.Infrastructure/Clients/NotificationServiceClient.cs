using System.Net;
using Microsoft.Extensions.Logging;
using UserService.Application.Interfaces;

namespace UserService.Infrastructure.Clients;

/// <summary>
/// Client for communicating with the Notification Service API.
/// </summary>
public class NotificationServiceClient(HttpClient httpClient, ILogger<NotificationServiceClient> logger)
    : INotificationServiceClient
{
    /// <summary>
    /// Creates an OTP via the Notification Service.
    /// </summary>
    public async Task<bool> CreateOtpAsync(string id, string type, string purpose)
    {
        try
        {
            var url = $"/api/otp/create?id={Uri.EscapeDataString(id)}&type={Uri.EscapeDataString(type)}&purpose={Uri.EscapeDataString(purpose)}";

            logger.LogInformation("Sending OTP request: {Url}", url);

            var response = await httpClient.PostAsync(url, null);

            if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
            {
                logger.LogInformation("OTP created successfully for {Id} via {Type}", id, type);
                return true;
            }

            var error = await response.Content.ReadAsStringAsync();
            logger.LogWarning("OTP creation failed: {StatusCode} | {Error}", response.StatusCode, error);
            return false;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Network error creating OTP for {Id}", id);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error creating OTP for {Id}", id);
            return false;
        }
    }
}
