namespace UserService.Application.Services;

public interface IAuth0ManagementService
{
    Task<string> CreateUserAndAssignRoleAsync(string email, string username,string password, string roleId);
    Task AssignRoleAsync(string auth0UserId, string roleId);
    Task SendPasswordSetupEmailAsync(string email);
}