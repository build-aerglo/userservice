using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using Microsoft.Extensions.Configuration;
using UserService.Application.DTOs.Auth;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Domain.Repositories;

namespace UserService.Application.Services.Auth0;

public class Auth0SocialLoginService : IAuth0SocialLoginService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ISocialIdentityRepository _socialIdentityRepo;
    private readonly IUserRepository _userRepo;
    private readonly IEndUserProfileRepository _endUserProfileRepo;
    private readonly IUserSettingsRepository _userSettingsRepo;
    private readonly IAuth0ManagementService _auth0Management;

    private readonly string _domain;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _audience;

    public Auth0SocialLoginService(
        HttpClient httpClient,
        IConfiguration config,
        ISocialIdentityRepository socialIdentityRepo,
        IUserRepository userRepo,
        IEndUserProfileRepository endUserProfileRepo,
        IUserSettingsRepository userSettingsRepo,
        IAuth0ManagementService auth0Management)
    {
        _httpClient = httpClient;
        _config = config;
        _socialIdentityRepo = socialIdentityRepo;
        _userRepo = userRepo;
        _endUserProfileRepo = endUserProfileRepo;
        _userSettingsRepo = userSettingsRepo;
        _auth0Management = auth0Management;

        _domain = config["Auth0:Domain"]!;
        _clientId = config["Auth0:ClientId"]!;
        _clientSecret = config["Auth0:ClientSecret"]!;
        _audience = config["Auth0:Audience"]!;
    }

    public SocialAuthUrlResponse GetAuthorizationUrl(SocialAuthUrlRequest request)
    {
        var provider = SocialProvider.GetAuth0Connection(request.Provider);

        if (!SocialProvider.IsValid(provider))
            throw new InvalidSocialProviderException(request.Provider);

        var state = request.State ?? GenerateState();
        var encodedRedirectUri = HttpUtility.UrlEncode(request.RedirectUri);
        var encodedState = HttpUtility.UrlEncode(state);

        var authUrl = $"https://{_domain}/authorize?" +
                      $"response_type=code&" +
                      $"client_id={_clientId}&" +
                      $"connection={provider}&" +
                      $"redirect_uri={encodedRedirectUri}&" +
                      $"scope=openid%20profile%20email%20offline_access&" +
                      $"audience={HttpUtility.UrlEncode(_audience)}&" +
                      $"state={encodedState}";

        return new SocialAuthUrlResponse
        {
            AuthorizationUrl = authUrl,
            State = state
        };
    }

    public async Task<SocialLoginResponse> AuthenticateAsync(SocialLoginRequest request)
    {
        var provider = SocialProvider.GetAuth0Connection(request.Provider);

        if (!SocialProvider.IsValid(provider))
            throw new InvalidSocialProviderException(request.Provider);

        var tokenResponse = await ExchangeCodeForTokenAsync(request.Code, request.RedirectUri ?? "");

        var userInfo = await GetUserInfoAsync(tokenResponse.AccessToken);

        var existingIdentity = await _socialIdentityRepo.GetByProviderUserIdAsync(
            provider, userInfo.Sub);

        bool isNewUser = false;
        Guid userId;
        User? user;

        if (existingIdentity != null)
        {
            userId = existingIdentity.UserId;
            user = await _userRepo.GetByIdAsync(userId);

            existingIdentity.UpdateTokens(
                tokenResponse.AccessToken,
                tokenResponse.RefreshToken,
                tokenResponse.ExpiresAt);
            existingIdentity.UpdateProfile(userInfo.Email, userInfo.Name);

            await _socialIdentityRepo.UpdateAsync(existingIdentity);
        }
        else
        {
            var existingUserByEmail = userInfo.Email != null
                ? await _userRepo.GetUserOrBusinessIdByEmailAsync(userInfo.Email)
                : null;

            if (existingUserByEmail.HasValue)
            {
                userId = existingUserByEmail.Value;
                user = await _userRepo.GetByIdAsync(userId);
            }
            else
            {
                var newUser = await CreateEndUserFromSocialAsync(userInfo, provider);
                userId = newUser.Id;
                user = newUser;
                isNewUser = true;
            }

            var socialIdentity = new SocialIdentity(
                userId,
                provider,
                userInfo.Sub,
                userInfo.Email,
                userInfo.Name,
                tokenResponse.AccessToken,
                tokenResponse.RefreshToken,
                tokenResponse.ExpiresAt);

            await _socialIdentityRepo.AddAsync(socialIdentity);
        }

        var roles = ExtractRolesFromToken(tokenResponse.IdToken);

        return new SocialLoginResponse
        {
            AccessToken = tokenResponse.AccessToken,
            IdToken = tokenResponse.IdToken,
            ExpiresIn = tokenResponse.ExpiresIn,
            Roles = roles,
            UserId = userId,
            IsNewUser = isNewUser,
            Provider = provider,
            Email = userInfo.Email,
            Name = userInfo.Name
        };
    }

    public async Task<LinkedSocialAccountDto> LinkAccountAsync(Guid userId, LinkSocialAccountRequest request)
    {
        var provider = SocialProvider.GetAuth0Connection(request.Provider);

        if (!SocialProvider.IsValid(provider))
            throw new InvalidSocialProviderException(request.Provider);

        var existingLink = await _socialIdentityRepo.GetByUserAndProviderAsync(userId, provider);
        if (existingLink != null)
            throw new SocialAccountAlreadyLinkedException(provider);

        var tokenResponse = await ExchangeCodeForTokenAsync(request.Code, request.RedirectUri ?? "");
        var userInfo = await GetUserInfoAsync(tokenResponse.AccessToken);

        var existingIdentity = await _socialIdentityRepo.GetByProviderUserIdAsync(
            provider, userInfo.Sub);

        if (existingIdentity != null)
            throw new SocialAccountAlreadyLinkedException(provider);

        var socialIdentity = new SocialIdentity(
            userId,
            provider,
            userInfo.Sub,
            userInfo.Email,
            userInfo.Name,
            tokenResponse.AccessToken,
            tokenResponse.RefreshToken,
            tokenResponse.ExpiresAt);

        await _socialIdentityRepo.AddAsync(socialIdentity);

        return new LinkedSocialAccountDto
        {
            Id = socialIdentity.Id,
            Provider = provider,
            ProviderUserId = userInfo.Sub,
            Email = userInfo.Email,
            Name = userInfo.Name,
            LinkedAt = socialIdentity.CreatedAt
        };
    }

    public async Task UnlinkAccountAsync(Guid userId, string provider)
    {
        var normalizedProvider = SocialProvider.GetAuth0Connection(provider);

        var existingLink = await _socialIdentityRepo.GetByUserAndProviderAsync(userId, normalizedProvider);
        if (existingLink == null)
            throw new SocialAccountNotLinkedException(normalizedProvider);

        await _socialIdentityRepo.DeleteByUserAndProviderAsync(userId, normalizedProvider);
    }

    public async Task<IEnumerable<LinkedSocialAccountDto>> GetLinkedAccountsAsync(Guid userId)
    {
        var identities = await _socialIdentityRepo.GetByUserIdAsync(userId);

        return identities.Select(i => new LinkedSocialAccountDto
        {
            Id = i.Id,
            Provider = i.Provider,
            ProviderUserId = i.ProviderUserId,
            Email = i.Email,
            Name = i.Name,
            LinkedAt = i.CreatedAt
        });
    }

    private async Task<Auth0TokenResponse> ExchangeCodeForTokenAsync(string code, string redirectUri)
    {
        var body = new
        {
            grant_type = "authorization_code",
            client_id = _clientId,
            client_secret = _clientSecret,
            code,
            redirect_uri = redirectUri
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"https://{_domain}/oauth/token", body);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<Auth0ErrorResponse>();
            throw new SocialLoginException(
                "auth0",
                error?.Error ?? "token_exchange_failed",
                error?.Error_Description ?? "Failed to exchange authorization code for token");
        }

        var tokenData = await response.Content.ReadFromJsonAsync<Auth0TokenResponseRaw>()
                        ?? throw new SocialLoginException("auth0", "invalid_response", "Invalid token response");

        return new Auth0TokenResponse
        {
            AccessToken = tokenData.access_token!,
            IdToken = tokenData.id_token,
            RefreshToken = tokenData.refresh_token,
            ExpiresIn = tokenData.expires_in,
            ExpiresAt = DateTime.UtcNow.AddSeconds(tokenData.expires_in)
        };
    }

    private async Task<Auth0UserInfo> GetUserInfoAsync(string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://{_domain}/userinfo");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            throw new SocialLoginException("auth0", "userinfo_failed", "Failed to get user info");

        return await response.Content.ReadFromJsonAsync<Auth0UserInfo>()
               ?? throw new SocialLoginException("auth0", "invalid_userinfo", "Invalid user info response");
    }

    private async Task<User> CreateEndUserFromSocialAsync(Auth0UserInfo userInfo, string provider)
    {
        var email = userInfo.Email ?? $"{userInfo.Sub}@{provider}.social";
        var username = userInfo.Name ?? userInfo.Nickname ?? email.Split('@')[0];
        var password = GenerateRandomPassword();

        var endUserRoleId = _config["Auth0:Roles:EndUser"]!;
        var auth0UserId = await _auth0Management.CreateUserAndAssignRoleAsync(
            email, username, password, endUserRoleId);

        var user = new User(
            username: username,
            email: email,
            phone: "",
            password: "",
            userType: "end_user",
            address: null,
            auth0UserId: auth0UserId);

        await _userRepo.AddAsync(user);

        var endUserProfile = new EndUserProfile(user.Id, $"Registered via {SocialProvider.GetDisplayName(provider)}");
        await _endUserProfileRepo.AddAsync(endUserProfile);

        var userSettings = new UserSettings(user.Id);
        await _userSettingsRepo.AddAsync(userSettings);

        return user;
    }

    private List<string> ExtractRolesFromToken(string? idToken)
    {
        if (string.IsNullOrEmpty(idToken))
            return new List<string>();

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(idToken);
            var rolesClaimKey = $"{_audience}/roles";

            if (jwt.Payload.TryGetValue(rolesClaimKey, out var rolesObj) &&
                rolesObj is JsonElement json && json.ValueKind == JsonValueKind.Array)
            {
                return json.EnumerateArray().Select(r => r.GetString()!).ToList();
            }
        }
        catch
        {
        }

        return new List<string>();
    }

    private static string GenerateState()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    private static string GenerateRandomPassword()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        var bytes = new byte[24];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);

        var result = new StringBuilder(24);
        foreach (var b in bytes)
        {
            result.Append(chars[b % chars.Length]);
        }
        return result.ToString();
    }

    private class Auth0TokenResponseRaw
    {
        public string? access_token { get; set; }
        public string? id_token { get; set; }
        public string? refresh_token { get; set; }
        public int expires_in { get; set; }
        public string? token_type { get; set; }
    }

    private class Auth0TokenResponse
    {
        public string AccessToken { get; set; } = default!;
        public string? IdToken { get; set; }
        public string? RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    private class Auth0UserInfo
    {
        [JsonPropertyName("sub")]
        public string Sub { get; set; } = default!;

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("email_verified")]
        public bool? EmailVerified { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("nickname")]
        public string? Nickname { get; set; }

        [JsonPropertyName("picture")]
        public string? Picture { get; set; }
    }
}
