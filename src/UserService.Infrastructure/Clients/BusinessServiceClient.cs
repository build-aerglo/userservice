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
    /// Retrieves the business ID by email address.
    /// </summary>
    public async Task<Guid?> GetBusinessIdByEmailAsync(string email)
    {
        try
        {
            var response = await httpClient.GetAsync($"/api/business/by-email?email={Uri.EscapeDataString(email)}");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var result = await response.Content.ReadFromJsonAsync<BusinessFetchResponse>();
                if (result != null && result.Id != Guid.Empty)
                {
                    logger.LogInformation("Found business ID {BusinessId} for email {Email}", result.Id, email);
                    return result.Id;
                }
            }

            logger.LogWarning("Business not found for email {Email}: {StatusCode}", email, response.StatusCode);
            return null;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Network error fetching business by email {Email}", email);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error fetching business by email {Email}", email);
            return null;
        }
    }

    /// <summary>
    /// Marks a business's email as verified in the Business Service.
    /// </summary>
    public async Task<bool> MarkBusinessEmailVerifiedAsync(Guid businessId, string email)
    {
        try
        {
            var payload = new { email };
            var response = await httpClient.PatchAsJsonAsync($"/api/business/{businessId}/verify-email", payload);

            if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent)
            {
                logger.LogInformation("Business email verified for business {BusinessId}", businessId);
                return true;
            }

            var error = await response.Content.ReadAsStringAsync();
            logger.LogWarning("Failed to mark business email verified: {StatusCode} | {Error}", response.StatusCode, error);
            return false;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Network error marking business email verified for {BusinessId}", businessId);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error marking business email verified for {BusinessId}", businessId);
            return false;
        }
    }

    /// <summary>
    /// Gets the name of a business by its ID.
    /// </summary>
    public async Task<string?> GetBusinessNameAsync(Guid businessId)
    {
        try
        {
            var response = await httpClient.GetAsync($"/api/businesses/{businessId}");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var result = await response.Content.ReadFromJsonAsync<BusinessFetchResponse>();
                if (result?.Name is not null)
                {
                    logger.LogInformation("Retrieved business name '{Name}' for {BusinessId}", result.Name, businessId);
                    return result.Name;
                }
            }

            logger.LogWarning("Business name not found for {BusinessId}: {StatusCode}", businessId, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching business name for {BusinessId}", businessId);
            return null;
        }
    }

    /// <summary>
    /// Updates the status field on a business.
    /// </summary>
    public async Task<bool> UpdateBusinessStatusAsync(Guid businessId, string status)
    {
        try
        {
            var payload = new { status };
            var response = await httpClient.PatchAsJsonAsync($"/api/business/{businessId}/status", payload);

            if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent)
            {
                logger.LogInformation("Business {BusinessId} status updated to '{Status}'", businessId, status);
                return true;
            }

            var error = await response.Content.ReadAsStringAsync();
            logger.LogWarning("Failed to update business status: {StatusCode} | {Error}", response.StatusCode, error);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating status for business {BusinessId}", businessId);
            return false;
        }
    }

    /// <summary>
    /// Sets owner details on the business after a claim registration.
    /// </summary>
    public async Task<bool> UpdateBusinessOwnerAsync(Guid businessId, Guid userId, string email, string? phoneNumber)
    {
        try
        {
            var payload = new { userId, email, phoneNumber };
            var response = await httpClient.PatchAsJsonAsync($"/api/business/{businessId}/owner", payload);

            if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent)
            {
                logger.LogInformation("Updated owner for business {BusinessId} to user {UserId}", businessId, userId);
                return true;
            }

            var error = await response.Content.ReadAsStringAsync();
            logger.LogWarning("Failed to update business owner: {StatusCode} | {Error}", response.StatusCode, error);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating owner for business {BusinessId}", businessId);
            return false;
        }
    }

    /// <summary>
    /// Initialises a default subscription for a business.
    /// </summary>
    public async Task<bool> InitializeBusinessSubscriptionAsync(Guid businessId)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"/api/business/{businessId}/subscription", new { });

            if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created
                || response.StatusCode == HttpStatusCode.NoContent)
            {
                logger.LogInformation("Subscription initialized for business {BusinessId}", businessId);
                return true;
            }

            var error = await response.Content.ReadAsStringAsync();
            logger.LogWarning("Failed to initialize subscription for {BusinessId}: {StatusCode} | {Error}", businessId, response.StatusCode, error);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error initializing subscription for business {BusinessId}", businessId);
            return false;
        }
    }

    /// <summary>
    /// Initialises default settings for a business.
    /// </summary>
    public async Task<bool> InitializeBusinessSettingsAsync(Guid businessId)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"/api/business/{businessId}/settings", new { });

            if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created
                || response.StatusCode == HttpStatusCode.NoContent)
            {
                logger.LogInformation("Settings initialized for business {BusinessId}", businessId);
                return true;
            }

            var error = await response.Content.ReadAsStringAsync();
            logger.LogWarning("Failed to initialize settings for {BusinessId}: {StatusCode} | {Error}", businessId, response.StatusCode, error);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error initializing settings for business {BusinessId}", businessId);
            return false;
        }
    }

    private sealed class BusinessFetchResponse
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
    }


}
