
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace UserService.Application.Services;

public class Auth0ManagementService(HttpClient http, IConfiguration config) : IAuth0ManagementService
{
    private async Task<string> GetMgmtTokenAsync()
    {
        var domain = config["Auth0:Domain"]!;
        var body = new
        {
            client_id = config["Auth0:ClientId"],
            client_secret = config["Auth0:ClientSecret"],
            audience = config["Auth0:Audience"],
            grant_type = "client_credentials"
        };


        var res = await http.PostAsync($"https://{domain}/oauth/token",
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
        res.EnsureSuccessStatusCode();
        var json = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("access_token").GetString()!;
    }


    private async Task UseMgmtAuthAsync()
    {
        var token = await GetMgmtTokenAsync();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
    
    public async Task<string> CreateUserAndAssignRoleAsync(string email, string username, string roleId)
    {
        await UseMgmtAuthAsync();
        var domain = config["Auth0:Domain"]!;
        var connection = config["Auth0:DbConnection"] ?? "Username-Password-Authentication";


        // 1) Create user WITHOUT password (they will set it via email link)
        var createBody = new
        {
            email,
            username,
            connection,
            email_verified = false
        };


        var createResp = await http.PostAsync($"https://{domain}/api/v2/users",
            new StringContent(JsonSerializer.Serialize(createBody), Encoding.UTF8, "application/json"));
        createResp.EnsureSuccessStatusCode();
        var userJson = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync());
        var auth0UserId = userJson.RootElement.GetProperty("user_id").GetString()!;


        // 2) Assign role
        await AssignRoleAsync(auth0UserId, roleId);


        // 3) Send password setup email
        await SendPasswordSetupEmailAsync(email);


        return auth0UserId;
    }

    public async Task AssignRoleAsync(string auth0UserId, string roleId)
    {
        await UseMgmtAuthAsync();
        var domain = config["Auth0:Domain"]!;
        var body = new { roles = new[] { roleId } };
        var resp = await http.PostAsync($"https://{domain}/api/v2/users/{Uri.EscapeDataString(auth0UserId)}/roles",
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
        resp.EnsureSuccessStatusCode();
    }

    public async Task SendPasswordSetupEmailAsync(string email)
    {
        // Uses Auth0's Change Password endpoint to send the email
        var domain = config["Auth0:Domain"]!;
        var payload = new
        {
            client_id = config["Auth0:ClientId"],
            email,
            connection = config["Auth0:DbConnection"] ?? "Username-Password-Authentication"
        };
        var resp = await http.PostAsync($"https://{domain}/dbconnections/change_password",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
        resp.EnsureSuccessStatusCode();
    }
}
