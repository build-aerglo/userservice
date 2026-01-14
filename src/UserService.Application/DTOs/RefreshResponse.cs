namespace UserService.Application.DTOs;

public class RefreshResponse
{
    public string AccessToken { get; set; } = default!;
    public string? IdToken { get; set; }
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; } = "Bearer";
}