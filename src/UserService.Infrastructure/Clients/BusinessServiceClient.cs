using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using UserService.Application.DTOs;
using UserService.Application.Interfaces;
using UserService.Domain.Exceptions;

namespace UserService.Infrastructure.Clients;

public class BusinessServiceClient(HttpClient httpClient, ILogger<BusinessServiceClient> logger)
    : IBusinessServiceClient
{
    public async Task<bool> BusinessExistsAsync(Guid businessId)
    {
        try
        {
            var response = await httpClient.GetAsync($"/api/businesses/{businessId}");

            if (response.StatusCode == HttpStatusCode.OK)
                return true;

            if (response.StatusCode == HttpStatusCode.NotFound)
                return false;

            logger.LogWarning("Unexpected response checking business {BusinessId}: {StatusCode}", businessId, response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if business {BusinessId} exists.", businessId);
            return false;
        }
    }

    public async Task<Guid?> CreateBusinessAsync(BusinessUserDto business)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("/api/business", business);

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                // BusinessService returned 409 — duplicate name, email, or phone.
                // Read the error body and propagate it so the controller can return a proper 409.
                var errorBody = await response.Content.ReadAsStringAsync();
                string errorMessage;
                try
                {
                    var doc = System.Text.Json.JsonDocument.Parse(errorBody);
                    errorMessage = doc.RootElement.TryGetProperty("error", out var errProp)
                        ? errProp.GetString() ?? "A business with these details already exists."
                        : doc.RootElement.TryGetProperty("message", out var msgProp)
                            ? msgProp.GetString() ?? "A business with these details already exists."
                            : "A business with these details already exists.";
                }
                catch
                {
                    errorMessage = "A business with these details already exists.";
                }
                throw new DuplicateBusinessException(errorMessage);
            }

            if (response.StatusCode != HttpStatusCode.Created)
            {
                var error = await response.Content.ReadAsStringAsync();
                logger.LogError("Business creation failed: {StatusCode} | {Error}", response.StatusCode, error);
                throw new BusinessUserCreationFailedException("Business creation failed in Business Service.");
            }

            var result = await response.Content.ReadFromJsonAsync<BusinessFetchResponse>();
            if (result == null || result.Id == Guid.Empty)
                throw new BusinessUserCreationFailedException("Invalid business creation response. ID missing.");

            return result.Id;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Network error creating business at {Url}", httpClient.BaseAddress);
            return null;
        }
        catch (BusinessUserCreationFailedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error creating business for user: {Username}", business.Name);
            return null;
        }
    }

    public async Task<bool> UpdateBusinessEmailAsync(string oldEmail, string newEmail)
    {
        try
        {
            var payload = new { oldEmail, newEmail };
            var response = await httpClient.PatchAsJsonAsync("/api/business/email", payload);

            if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent)
                return true;

            var error = await response.Content.ReadAsStringAsync();
            logger.LogWarning("Failed to update business email: {StatusCode} | {Error}", response.StatusCode, error);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating business email from {OldEmail}", oldEmail);
            return false;
        }
    }

    private sealed class BusinessFetchResponse
    {
        public Guid Id { get; set; }
    }
}
