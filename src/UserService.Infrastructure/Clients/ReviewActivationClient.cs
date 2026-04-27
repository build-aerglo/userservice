using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UserService.Application.Interfaces;

namespace UserService.Infrastructure.Clients;

/// <summary>
/// RS-DeferredAuth: HTTP client for triggering review activation in ReviewService.
/// Fire-and-forget — never throws, only logs.
/// </summary>
public class ReviewActivationClient : IReviewActivationClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ReviewActivationClient> _logger;
    private readonly string _internalApiKey;

    public ReviewActivationClient(
        HttpClient httpClient,
        IConfiguration config,
        ILogger<ReviewActivationClient> logger)
    {
        _httpClient     = httpClient;
        _logger         = logger;
        _internalApiKey = config["Services:InternalApiKey"] ?? string.Empty;
    }

    public async Task ActivateReviewsForUserAsync(Guid userId)
    {
        try
        {
            _logger.LogInformation(
                "Triggering review activation for verified user {UserId}", userId);

            _httpClient.DefaultRequestHeaders.Remove("X-Internal-Api-Key");
            _httpClient.DefaultRequestHeaders.Add("X-Internal-Api-Key", _internalApiKey);

            var response = await _httpClient.PostAsJsonAsync(
                "/api/review/internal/activate-verified-reviews",
                new { UserId = userId });

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Review activation succeeded for user {UserId}", userId);
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning(
                    "Review activation returned {Status} for user {UserId}: {Body}",
                    response.StatusCode, userId, body);
            }
        }
        catch (Exception ex)
        {
            // Fire-and-forget — verification must not fail because of this
            _logger.LogError(ex,
                "Review activation call failed for user {UserId}. " +
                "User is still verified. Reviews will remain pending.", userId);
        }
    }
}
