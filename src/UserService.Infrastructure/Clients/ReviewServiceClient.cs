using System.Text.Json;
using Microsoft.Extensions.Logging;
using UserService.Application.Interfaces;
using System.Text.Json.Serialization;

namespace UserService.Infrastructure.Clients;

public class ReviewServiceClient : IReviewServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ReviewServiceClient> _logger;

    public ReviewServiceClient(HttpClient httpClient, ILogger<ReviewServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<int> GetTotalHelpfulVotesForUserAsync(Guid userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/review/user/{userId}/helpful-votes/total");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<HelpfulVotesResponse>(content);
                return result?.TotalHelpfulVotes ?? 0;
            }

            _logger.LogWarning("Failed to get helpful votes for user {UserId}. Status: {StatusCode}", 
                userId, response.StatusCode);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting helpful votes for user {UserId}", userId);
            return 0;
        }
    }

    public async Task<int> GetApprovedReviewCountAsync(Guid userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/review/user/{userId}/approved/count");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ReviewCountResponse>(content);
                return result?.Count ?? 0;
            }

            _logger.LogWarning("Failed to get review count for user {UserId}. Status: {StatusCode}", 
                userId, response.StatusCode);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting review count for user {UserId}", userId);
            return 0;
        }
    }
}

internal class HelpfulVotesResponse
{
    [JsonPropertyName("totalHelpfulVotes")]
    public int TotalHelpfulVotes { get; set; }
}

internal class ReviewCountResponse
{
    [JsonPropertyName("count")]
    public int Count { get; set; }
}