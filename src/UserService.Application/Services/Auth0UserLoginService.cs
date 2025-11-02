using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using UserService.Application.DTOs;

namespace UserService.Application.Services;

public class Auth0UserLoginService : IAuth0UserLoginService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public Auth0UserLoginService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task<TokenResponse> LoginAsync(string email, string password)
    {
        var body = new
        {
            grant_type = "password",
            username = email,
            password = password,
            audience = "https://user-service.aerglotechnology.com",
            client_id = "ivgMr8BzNOCtZDiXiiRt20S4ss6sVsAG",
            client_secret = "hA6hZ_8NbliSzlmymwXCTCJZOAfEpoxcw_eTDSv2OcA8SOk2dyYHgZsJ4FyBg20c",
            realm = _config["Auth0:DbConnection"],  // ✅ REQUIRED FOR EMAIL/PASSWORD
            scope = "openid profile email offline_access"
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"https://dev-jx8cz5q0wcoddune.us.auth0.com/oauth/token", body);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<TokenResponse>()
               ?? throw new Exception("Auth0 login failed");
    }


    public async Task<TokenResponse> RefreshAsync(string refreshToken)
    {
        var body = new
        {
            grant_type = "refresh_token",
            refresh_token = refreshToken,
            client_id = _config["Auth0:ClientId"],
            client_secret = _config["Auth0:ClientSecret"]
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"https://{_config["Auth0:Domain"]}/oauth/token", body);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<TokenResponse>()
               ?? throw new Exception("Failed to refresh token");
    }
}