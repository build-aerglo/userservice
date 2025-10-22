using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UserService.Application.Interfaces;

namespace UserService.Infrastructure.Clients;

/// <summary>
/// Concrete implementation of IBusinessServiceClient that communicates
/// with the Business Service via HTTP.
/// </summary>
public class BusinessServiceClient : IBusinessServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BusinessServiceClient> _logger;

    public BusinessServiceClient(HttpClient httpClient, ILogger<BusinessServiceClient> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;

        // Set base URL from appsettings
        var baseUrl = configuration["Services:BusinessServiceBaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            _logger.LogWarning("BusinessServiceBaseUrl not configured. Using default localhost URL.");
            baseUrl = "https://localhost:7001";
        }

        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    /// <summary>
    /// Calls the Business Service API to check if a business exists.
    /// </summary>
    public async Task<bool> BusinessExistsAsync(Guid businessId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/businesses/{businessId}");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                _logger.LogInformation("✅ Business {BusinessId} found in Business Service.", businessId);
                return true;
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("❌ Business {BusinessId} not found in Business Service.", businessId);
                return false;
            }

            _logger.LogError("⚠️ Unexpected response from Business Service: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error connecting to Business Service API.");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while checking business existence.");
            return false;
        }
    }
}
