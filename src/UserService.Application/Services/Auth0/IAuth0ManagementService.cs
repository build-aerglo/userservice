namespace UserService.Application.Services.Auth0;

public interface IAuth0ManagementService
{
    Task<string> CreateUserAndAssignRoleAsync(string email, string username, string password, string roleId);
    Task AssignRoleAsync(string auth0UserId, string roleId);
    Task SendPasswordSetupEmailAsync(string email);
    Task<bool> UpdateEmailAsync(string auth0UserId, string newEmail);
    Task<bool> UpdatePasswordAsync(string auth0UserId, string newPassword);
}