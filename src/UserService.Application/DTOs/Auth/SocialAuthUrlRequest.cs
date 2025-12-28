namespace UserService.Application.DTOs.Auth;

public class SocialAuthUrlRequest
{
    public string Provider { get; set; } = default!;
    public string RedirectUri { get; set; } = default!;
    public string? State { get; set; }
}
