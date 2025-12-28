namespace UserService.Application.DTOs.Auth;

public class SocialLoginResponse
{
    public string AccessToken { get; set; } = default!;
    public string? IdToken { get; set; }
    public int ExpiresIn { get; set; }
    public List<string>? Roles { get; set; }
    public Guid? UserId { get; set; }
    public bool IsNewUser { get; set; }
    public string Provider { get; set; } = default!;
    public string? Email { get; set; }
    public string? Name { get; set; }
}
