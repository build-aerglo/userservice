namespace UserService.Application.DTOs;

public class TokenResponse
{
    public string? Access_Token { get; set; }
    public string? Id_Token { get; set; }
    public string? Refresh_Token { get; set; }
    public int Expires_In { get; set; }
    public List<string>? Roles { get; set; }
}