namespace UserService.Application.DTOs.Auth;

public class RequestEmailUpdateDto
{
    public Guid BusinessId { get; set; }
    public string EmailAddress { get; set; } = default!;
    public string? Reason { get; set; }
}
