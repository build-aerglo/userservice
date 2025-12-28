namespace UserService.Application.DTOs.Auth;

public class SocialAuthUrlResponse
{
    public string AuthorizationUrl { get; set; } = default!;
    public string State { get; set; } = default!;
}
