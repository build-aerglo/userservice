namespace UserService.Application.DTOs.Auth;

public class LinkSocialAccountRequest
{
    public string Provider { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string RedirectUri { get; set; } = default!;
}
