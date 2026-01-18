using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UserService.Application.Interfaces;

namespace UserService.Infrastructure.Clients;

public class AfricaTalkingClient : IAfricaTalkingClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<AfricaTalkingClient> _logger;

    public AfricaTalkingClient(HttpClient httpClient, IConfiguration config, ILogger<AfricaTalkingClient> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;

        var apiKey = _config["AfricaTalking:ApiKey"];
        _httpClient.DefaultRequestHeaders.Add("apiKey", apiKey);
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public async Task<AirtimeResponse> SendAirtimeAsync(string phoneNumber, decimal amount)
    {
        try
        {
            var username = _config["AfricaTalking:Username"];
            
            var request = new
            {
                username = username,
                recipients = new[]
                {
                    new
                    {
                        phoneNumber = phoneNumber,
                        currencyCode = "NGN",
                        amount = amount.ToString("F2")
                    }
                }
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/version1/airtime/send", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("AfricaTalking API Response: {Response}", responseContent);

            if (!response.IsSuccessStatusCode)
            {
                return new AirtimeResponse
                {
                    Success = false,
                    Message = $"API request failed with status {response.StatusCode} ({(int)response.StatusCode})",
                    ErrorMessage = responseContent
                };
            }

            var result = JsonSerializer.Deserialize<AfricaTalkingApiResponse>(responseContent);
            
            if (result?.Responses != null && result.Responses.Length > 0)
            {
                var firstResponse = result.Responses[0];
                
                return new AirtimeResponse
                {
                    Success = firstResponse.Status == "Sent",
                    Message = firstResponse.Status,
                    TransactionId = firstResponse.RequestId,
                    ErrorMessage = firstResponse.ErrorMessage
                };
            }

            return new AirtimeResponse
            {
                Success = false,
                Message = "No response from provider",
                ErrorMessage = responseContent
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending airtime via AfricaTalking");
            return new AirtimeResponse
            {
                Success = false,
                Message = "Exception occurred",
                ErrorMessage = ex.Message
            };
        }
    }
}

// Internal models for API response
internal class AfricaTalkingApiResponse
{
    [JsonPropertyName("numSent")]
    public int NumSent { get; set; }
    
    [JsonPropertyName("totalAmount")]
    public string TotalAmount { get; set; } = default!;
    
    [JsonPropertyName("totalDiscount")]
    public string TotalDiscount { get; set; } = default!;
    
    [JsonPropertyName("responses")]
    public AirtimeResponseItem[]? Responses { get; set; }
}

internal class AirtimeResponseItem
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = default!;
    
    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = default!;
    
    [JsonPropertyName("amount")]
    public string Amount { get; set; } = default!;
    
    [JsonPropertyName("requestId")]
    public string RequestId { get; set; } = default!;
    
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }
    
    [JsonPropertyName("discount")]
    public string Discount { get; set; } = default!;
}