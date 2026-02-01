using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using UserService.Application.Interfaces;

namespace UserService.Infrastructure.Clients;

/// <summary>
/// Client for communicating with the Notification Service API.
/// </summary>
public class NotificationServiceClient(HttpClient httpClient, ILogger<NotificationServiceClient> logger)
    : INotificationServiceClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Creates an OTP via the Notification Service.
    /// </summary>
    public async Task<bool> CreateOtpAsync(string id, string type, string purpose)
    {
        try
        {
            var payload = new OtpCreateRequest(id, type, purpose);
            var json = JsonSerializer.Serialize(payload, JsonOptions);

            logger.LogInformation("Sending OTP request: {Json}", json);

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("/api/otp/create", content);

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

    private record OtpCreateRequest(string Id, string Type, string Purpose);
}
