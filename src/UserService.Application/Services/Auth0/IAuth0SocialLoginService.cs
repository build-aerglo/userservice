using UserService.Application.DTOs.Auth;

namespace UserService.Application.Services.Auth0;

public interface IAuth0SocialLoginService
{
    SocialAuthUrlResponse GetAuthorizationUrl(SocialAuthUrlRequest request);
    Task<SocialLoginResponse> AuthenticateAsync(SocialLoginRequest request);
    Task<LinkedSocialAccountDto> LinkAccountAsync(Guid userId, LinkSocialAccountRequest request);
    Task UnlinkAccountAsync(Guid userId, string provider);
    Task<IEnumerable<LinkedSocialAccountDto>> GetLinkedAccountsAsync(Guid userId);
}
