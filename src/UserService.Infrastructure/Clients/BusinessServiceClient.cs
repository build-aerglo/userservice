using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UserService.Application.DTOs;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;

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
    
    /// <summary>
    /// Creates a new business.
    /// </summary>
    public async Task<Guid?> CreateBusinessAsync(BusinessUserDto business)
    {
        try
        {
            // var options = new JsonSerializerOptions
            // {
            //     PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            //     PropertyNameCaseInsensitive = true
            // };
            
            var response = await _httpClient.PostAsJsonAsync($"/api/Business/", business);
            Console.WriteLine("response: " + response);
            // if (response.StatusCode != HttpStatusCode.Created)
            //     throw new BusinessUserCreationFailedException("Business creation failed: Business Endpoint Error.");
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<BusinessFetchResponseClass>();
            Console.WriteLine("result: " + result);
            if (result == null || result.Id == Guid.Empty)
                throw new BusinessUserCreationFailedException("Business creation failed: BusinessId is missing.");
            return result.Id;

        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error connecting to Business Service API.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while checking business existence.");
            return null;
        }
    }
    
    public class BusinessFetchResponseClass
    {
        public Guid Id { get; set; }
    }

}
