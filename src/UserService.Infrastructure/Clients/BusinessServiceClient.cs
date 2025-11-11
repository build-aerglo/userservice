using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using UserService.Application.DTOs;
using UserService.Application.Interfaces;
using UserService.Domain.Exceptions;

namespace UserService.Infrastructure.Clients;

/// <summary>
/// Client for communicating with the Business Service API.
/// </summary>
public class BusinessServiceClient(HttpClient httpClient, ILogger<BusinessServiceClient> logger)
    : IBusinessServiceClient
{
    /// <summary>
    /// Checks if a business exists in the Business Service by ID.
    /// </summary>
    public async Task<bool> BusinessExistsAsync(Guid businessId)
    {
        try
        {
            var response = await httpClient.GetAsync($"/api/businesses/{businessId}");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                logger.LogInformation("✅ Business {BusinessId} exists.", businessId);
                return true;
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogWarning("❌ Business {BusinessId} not found.", businessId);
                return false;
            }

            logger.LogWarning("⚠️ Unexpected response from Business Service: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Network error checking if business {BusinessId} exists.", businessId);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error checking business existence for {BusinessId}.", businessId);
            return false;
        }
    }

    /// <summary>
    /// Creates a new business in the Business Service.
    /// </summary>
    public async Task<Guid?> CreateBusinessAsync(BusinessUserDto business)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("/api/business", business);

            if (response.StatusCode != HttpStatusCode.Created)
            {
                var error = await response.Content.ReadAsStringAsync();
                logger.LogError("❌ Business creation failed: {StatusCode} | {Error}", response.StatusCode, error);
                throw new BusinessUserCreationFailedException("Business creation failed in Business Service.");
            }

            var result = await response.Content.ReadFromJsonAsync<BusinessFetchResponse>();
            if (result == null || result.Id == Guid.Empty)
                throw new BusinessUserCreationFailedException("Invalid business creation response. ID missing.");

            logger.LogInformation("✅ Business successfully created with ID: {BusinessId}", result.Id);
            return result.Id;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Network error creating business at {Url}", httpClient.BaseAddress);
            return null;
        }
        catch (BusinessUserCreationFailedException)
        {
            throw; // allow upper layers to handle domain exception
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error creating business for user: {Username}", business.Name);
            return null;
        }
    }

    private sealed class BusinessFetchResponse
    {
        public Guid Id { get; set; }
    }
}
