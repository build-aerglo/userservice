using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace UserService.Application.Services.Auth0;

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
            audience = "https://dev-jx8cz5q0wcoddune.us.auth0.com/api/v2/",
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
        // 1) CREATE USER
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
        string auth0UserId;

        if (!createResp.IsSuccessStatusCode)
        {
            // Handle 409 Conflict - user already exists
            if (createResp.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                // User already exists in Auth0, get their user_id
                var existingUserId = await GetUserByEmailAsync(email);
                if (existingUserId == null)
                {
                    throw new Exception($"User with email {email} exists in Auth0 but could not be retrieved.");
                }

                auth0UserId = existingUserId;

                // Ensure role is assigned (might already be assigned, but this is idempotent)
                try
                {
                    await AssignRoleAsync(auth0UserId, roleId);
                }
                catch
                {
                    // Role might already be assigned, continue
                }

                return auth0UserId;
            }
            else
            {
                throw new Exception($"Create user failed: {createResp.StatusCode} - {respContent}");
            }
        }

        var userJson = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync());
        auth0UserId = userJson.RootElement.GetProperty("user_id").GetString()!;

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

    // =============================================================================================
    // GET USER BY EMAIL
    // =============================================================================================
    public async Task<string?> GetUserByEmailAsync(string email)
    {
        await UseMgmtAuthAsync();

        var domain = config["Auth0:Domain"]!;
        var encodedEmail = Uri.EscapeDataString(email);

        var resp = await http.GetAsync(
            $"https://{domain}/api/v2/users-by-email?email={encodedEmail}"
        );

        if (!resp.IsSuccessStatusCode)
            return null;

        var content = await resp.Content.ReadAsStringAsync();
        var users = JsonDocument.Parse(content);

        // Auth0 returns an array of users matching the email
        if (users.RootElement.GetArrayLength() > 0)
        {
            var firstUser = users.RootElement[0];
            return firstUser.GetProperty("user_id").GetString();
        }

        return null;
    }
}
