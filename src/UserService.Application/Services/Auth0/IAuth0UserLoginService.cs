using UserService.Application.DTOs;

namespace UserService.Application.Services.Auth0;

public interface IAuth0UserLoginService
{
    Task<TokenResponse> LoginAsync(string email, string password);
    Task<TokenResponse> RefreshAsync(string refreshToken);
}