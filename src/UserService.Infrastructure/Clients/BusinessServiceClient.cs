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

    /// <summary>
    /// Updates the business email in the Business Service by old email.
    /// </summary>
    public async Task<bool> UpdateBusinessEmailAsync(string oldEmail, string newEmail)
    {
        try
        {
            var payload = new { oldEmail, newEmail };
            var response = await httpClient.PatchAsJsonAsync("/api/business/email", payload);

            if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent)
            {
                logger.LogInformation("Business email updated from {OldEmail} to {NewEmail}", oldEmail, newEmail);
                return true;
            }

            var error = await response.Content.ReadAsStringAsync();
            logger.LogWarning("Failed to update business email: {StatusCode} | {Error}", response.StatusCode, error);
            return false;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Network error updating business email from {OldEmail}", oldEmail);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error updating business email from {OldEmail}", oldEmail);
            return false;
        }
    }

    /// <summary>
    /// Sets the user_id field on the business record in the Business Service.
    /// </summary>
    public async Task UpdateBusinessUserIdAsync(Guid businessId, Guid userId)
    {
        try
        {
            var payload = new { userId };
            var response = await httpClient.PatchAsJsonAsync($"/api/business/{businessId}/user-id", payload);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                logger.LogError(
                    "Failed to set user_id on business {BusinessId}: {StatusCode} | {Error}",
                    businessId, response.StatusCode, error);
                throw new BusinessUserCreationFailedException(
                    $"Failed to link user to business {businessId}.");
            }

            logger.LogInformation(
                "Successfully set user_id {UserId} on business {BusinessId}", userId, businessId);
        }
        catch (BusinessUserCreationFailedException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Network error setting user_id on business {BusinessId}", businessId);
            throw new BusinessUserCreationFailedException(
                $"Network error linking user to business {businessId}.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error setting user_id on business {BusinessId}", businessId);
            throw new BusinessUserCreationFailedException(
                $"Unexpected error linking user to business {businessId}.");
        }
    }

    private sealed class BusinessFetchResponse
    {
        public Guid Id { get; set; }
    }
}
