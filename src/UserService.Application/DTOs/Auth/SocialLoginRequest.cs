namespace UserService.Application.DTOs.Auth;

public class SocialLoginRequest
{
    public string Provider { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string? RedirectUri { get; set; }
    public string? State { get; set; }
}
