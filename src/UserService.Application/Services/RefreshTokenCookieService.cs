using Microsoft.AspNetCore.Http;

namespace UserService.Application.Services;

public class RefreshTokenCookieService : IRefreshTokenCookieService
{
    private const string CookieName = "refresh_token";

    public void SetRefreshToken(HttpResponse response, string token)
    {
        response.Cookies.Append(CookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth/refresh",
            MaxAge = TimeSpan.FromDays(30)
        });
    }

    public string? GetRefreshToken(HttpRequest request)
        => request.Cookies.TryGetValue(CookieName, out var token) ? token : null;

    public void ClearRefreshToken(HttpResponse response)
    {
        response.Cookies.Delete(CookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth/refresh"
        });
    }
}
