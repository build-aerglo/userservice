namespace UserService.Application.Interfaces;

public interface IAfricaTalkingClient
{
    Task<AirtimeResponse> SendAirtimeAsync(string phoneNumber, decimal amount);
}

public class AirtimeResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? TransactionId { get; set; }
    public string? ErrorMessage { get; set; }
}