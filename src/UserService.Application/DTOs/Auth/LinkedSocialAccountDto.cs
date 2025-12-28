namespace UserService.Application.DTOs.Auth;

public class LinkedSocialAccountDto
{
    public Guid Id { get; set; }
    public string Provider { get; set; } = default!;
    public string ProviderUserId { get; set; } = default!;
    public string? Email { get; set; }
    public string? Name { get; set; }
    public DateTime LinkedAt { get; set; }
}
