using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using UserService.Application.Services.Auth0;

namespace UserService.Application.Services;

public class Auth0ManagementService(HttpClient http, IConfiguration config) : IAuth0ManagementService
{
    // =============================================================================================
    // GET MANAGEMENT API TOKEN
    // =============================================================================================
    private async Task<string> GetMgmtTokenAsync()
    {
        var domain = config["Auth0:Domain"]!;
        var body = new
        {
            client_id = config["Auth0:ClientId"],
            client_secret = config["Auth0:ClientSecret"],
            audience = config["Auth0:ManagementAudience"],
            grant_type = "client_credentials"
        };

        var res = await http.PostAsync(
            $"https://{domain}/oauth/token",
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        );

        res.EnsureSuccessStatusCode();

        var json = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("access_token").GetString()!;
    }

    // Attach mgmt token to header
    private async Task UseMgmtAuthAsync()
    {
        var token = await GetMgmtTokenAsync();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    // =============================================================================================
    // CREATE USER + ASSIGN ROLE + SEND PASSWORD RESET LINK
    // =============================================================================================
    public async Task<string> CreateUserAndAssignRoleAsync(string email, string username,string password, string roleId)
    {
        await UseMgmtAuthAsync();

        var domain = config["Auth0:Domain"]!;
        var connection = config["Auth0:DbConnection"] ?? "Username-Password-Authentication";

        // ----------------------------------------------------------------------
        // 1) CREATE USER (no password — via email reset link)
        // ----------------------------------------------------------------------
        var createBody = new
        {
            email,
            password,
            connection,
            email_verified = false
        };

        var createResp = await http.PostAsync(
            $"https://{domain}/api/v2/users",
            new StringContent(JsonSerializer.Serialize(createBody), Encoding.UTF8, "application/json")
        );
        
        var respContent = await createResp.Content.ReadAsStringAsync();

        if (!createResp.IsSuccessStatusCode)
        {
            throw new Exception($"Create user failed: {createResp.StatusCode} - {respContent}");
        }

        createResp.EnsureSuccessStatusCode();
        var userJson = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync());
        var auth0UserId = userJson.RootElement.GetProperty("user_id").GetString()!;

        // ----------------------------------------------------------------------
        // 2) ASSIGN ROLE
        // ----------------------------------------------------------------------
        await AssignRoleAsync(auth0UserId, roleId);

        // ----------------------------------------------------------------------
        // 3) SEND "SET YOUR PASSWORD" EMAIL
        // ----------------------------------------------------------------------
        await SendPasswordSetupEmailAsync(email);

        return auth0UserId;
    }

    // =============================================================================================
    // ASSIGN ROLE
    // =============================================================================================
    public async Task AssignRoleAsync(string auth0UserId, string roleId)
    {
        await UseMgmtAuthAsync();

        var domain = config["Auth0:Domain"]!;

        var body = new { roles = new[] { roleId } };

        var resp = await http.PostAsync(
            $"https://{domain}/api/v2/users/{Uri.EscapeDataString(auth0UserId)}/roles",
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        );

        resp.EnsureSuccessStatusCode();
    }

    // =============================================================================================
    // SEND PASSWORD RESET EMAIL
    // =============================================================================================
    public async Task SendPasswordSetupEmailAsync(string email)
    {
        var domain = config["Auth0:Domain"]!;
        var connection = config["Auth0:DbConnection"] ?? "Username-Password-Authentication";

        var payload = new
        {
            client_id = config["Auth0:ClientId"], // frontend client id OK for password reset
            email,
            connection
        };

        var resp = await http.PostAsync(
            $"https://{domain}/dbconnections/change_password",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        );

        resp.EnsureSuccessStatusCode();
    }
}
