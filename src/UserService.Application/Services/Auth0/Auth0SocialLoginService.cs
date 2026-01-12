using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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

        // Exchange authorization code for tokens
        var tokenResponse = await ExchangeCodeForTokenAsync(request.Code, request.RedirectUri ?? "");

        // Get user info from Auth0
        var userInfo = await GetUserInfoAsync(tokenResponse.AccessToken);

        // Check if this social identity already exists in our database
        var existingIdentity = await _socialIdentityRepo.GetByProviderUserIdAsync(provider, userInfo.Sub);

        bool isNewUser = false;
        Guid userId;
        User? user;
        EndUserProfile? endUserProfile = null;

        if (existingIdentity != null)
        {
            // Existing social login user - update tokens and get user data
            userId = existingIdentity.UserId;
            user = await _userRepo.GetByIdAsync(userId);

            existingIdentity.UpdateTokens(
                tokenResponse.AccessToken,
                tokenResponse.RefreshToken,
                tokenResponse.ExpiresAt);
            existingIdentity.UpdateProfile(userInfo.Email, userInfo.Name);

            await _socialIdentityRepo.UpdateAsync(existingIdentity);

            // Fetch end user profile
            endUserProfile = await _endUserProfileRepo.GetByUserIdAsync(userId);
        }
        else
        {
            // New social login - check if email already exists in our system
            User? existingUserByEmail = null;
            if (!string.IsNullOrEmpty(userInfo.Email))
            {
                var existingUserId = await _userRepo.GetUserOrBusinessIdByEmailAsync(userInfo.Email);
                if (existingUserId.HasValue)
                {
                    existingUserByEmail = await _userRepo.GetByIdAsync(existingUserId.Value);
                }
            }

            if (existingUserByEmail != null)
            {
                // Link social identity to existing user
                userId = existingUserByEmail.Id;
                user = existingUserByEmail;
                endUserProfile = await _endUserProfileRepo.GetByUserIdAsync(userId);
            }
            else
            {
                // Create new local user for social login
                (user, endUserProfile) = await CreateEndUserFromSocialAsync(userInfo, provider);
                userId = user.Id;
                isNewUser = true;
            }

            // Create social identity link
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

        // Extract roles from token (or use default for new users)
        var roles = ExtractRolesFromToken(tokenResponse.IdToken);
        if (roles.Count == 0 && user?.UserType == "end_user")
        {
            roles = new List<string> { "end_user" };
        }

        return new SocialLoginResponse
        {
            AccessToken = tokenResponse.AccessToken,
            IdToken = tokenResponse.IdToken,
            ExpiresIn = tokenResponse.ExpiresIn,
            Roles = roles,
            UserId = userId,
            EndUserProfileId = endUserProfile?.Id,
            IsNewUser = isNewUser,
            Provider = provider,
            Email = userInfo.Email ?? user?.Email,
            Name = userInfo.Name ?? user?.Username,
            Picture = userInfo.Picture,
            Phone = user?.Phone,
            Address = user?.Address
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

    /// <summary>
    /// Creates a new end user in our local database from social login.
    /// Note: The user already exists in Auth0 via social login, so we only:
    /// 1. Assign the end_user role to the Auth0 social user
    /// 2. Create the local user record with the social Auth0 user ID
    /// </summary>
    private async Task<(User user, EndUserProfile profile)> CreateEndUserFromSocialAsync(Auth0UserInfo userInfo, string provider)
    {
        var email = userInfo.Email ?? $"{userInfo.Sub.Replace("|", "_")}@social.local";
        var username = userInfo.Name ?? userInfo.Nickname ?? email.Split('@')[0];

        // The Auth0 user already exists from social login (e.g., "google-oauth2|101712854607093347603")
        // We just need to assign the end_user role to this existing Auth0 user
        var auth0UserId = userInfo.Sub;

        try
        {
            var endUserRoleId = _config["Auth0:Roles:EndUser"]!;
            await _auth0Management.AssignRoleAsync(auth0UserId, endUserRoleId);
        }
        catch (Exception ex)
        {
            // Log but don't fail - role assignment is not critical for login
            Console.WriteLine($"Warning: Failed to assign role to social user {auth0UserId}: {ex.Message}");
        }

        // Create local user record with the social Auth0 user ID
        var user = new User(
            username: username,
            email: email,
            phone: "",
            password: "",
            userType: "end_user",
            address: null,
            auth0UserId: auth0UserId);

        await _userRepo.AddAsync(user);

        // Create end user profile
        var endUserProfile = new EndUserProfile(
            user.Id,
            $"Registered via {SocialProvider.GetDisplayName(provider)}");
        await _endUserProfileRepo.AddAsync(endUserProfile);

        // Create default user settings
        var userSettings = new UserSettings(user.Id);
        await _userSettingsRepo.AddAsync(userSettings);

        return (user, endUserProfile);
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
        public string Sub { get; set; } = default!;
        public string? Email { get; set; }
        public bool? Email_Verified { get; set; }
        public string? Name { get; set; }
        public string? Nickname { get; set; }
        public string? Picture { get; set; }
    }
}
