using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

public interface IUserRepository
{
    Task<bool> EmailExistsAsync(string email);
    Task<IEnumerable<User>> GetAllAsync();
    Task<User?> GetByIdAsync(Guid id);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task<Guid?> GetUserOrBusinessIdByEmailAsync(string email);
    Task UpdateLastLoginAsync(Guid userId, DateTime loginTime);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByAuth0UserIdAsync(string auth0UserId);

}