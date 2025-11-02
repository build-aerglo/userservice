using Microsoft.AspNetCore.Http;

namespace UserService.Application.Services;

public interface IRefreshTokenCookieService
{
    void SetRefreshToken(HttpResponse response, string token);
    string? GetRefreshToken(HttpRequest request);
    void ClearRefreshToken(HttpResponse response);
}