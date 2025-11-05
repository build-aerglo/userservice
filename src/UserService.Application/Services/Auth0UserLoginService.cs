using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using UserService.Application.DTOs;

namespace UserService.Application.Services;

public class Auth0UserLoginService(HttpClient httpClient, IConfiguration config) : IAuth0UserLoginService
{
    public async Task<TokenResponse> LoginAsync(string email, string password)
    {
        var body = new
        {
            grant_type = "password",
            username = email,
            password = password,
            audience = config["Auth0:Audience"],
            client_id = config["Auth0:ClientId"],
            client_secret = config["Auth0:ClientSecret"],
            realm = config["Auth0:DbConnection"], 
            scope = "openid profile email offline_access"
        };
        
        var response = await httpClient.PostAsJsonAsync(
         //   $"https://dev-jx8cz5q0wcoddune.us.auth0.com/oauth/token", body);
        $"https://{config["Auth0:Domain"]}/oauth/token", body);


        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<TokenResponse>()
                    ?? throw new Exception("Auth0 login failed");

        //  Decode the ID token to extract roles
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token.Id_Token);

        var rolesClaimKey = $"{config["Auth0:Audience"]}/roles"; // custom namespace

        if (jwt.Payload.TryGetValue(rolesClaimKey, out var rolesObj) &&
            rolesObj is JsonElement json && json.ValueKind == JsonValueKind.Array)
        {
            token.Roles = json.EnumerateArray()
                .Select(r => r.GetString()!)
                .ToList();
        }
        else
        {
            token.Roles = new List<string>();
        }

        return token;
    }


    public async Task<TokenResponse> RefreshAsync(string refreshToken)
    {
        var body = new
        {
            grant_type = "refresh_token",
            refresh_token = refreshToken,
            client_id = config["Auth0:ClientId"],
            client_secret = config["Auth0:ClientSecret"]
        };

        var response = await httpClient.PostAsJsonAsync(
            $"https://{config["Auth0:Domain"]}/oauth/token", body);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<TokenResponse>()
               ?? throw new Exception("Failed to refresh token");
    }
}