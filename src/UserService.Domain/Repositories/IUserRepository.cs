using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

public interface IUserRepository
{
    Task<bool> EmailExistsAsync(string email);
    Task<bool> PhoneExistsAsync(string phone);
    Task<IEnumerable<User>> GetAllAsync();
    Task<User?> GetByIdAsync(Guid id);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task<Guid?> GetUserOrBusinessIdByEmailAsync(string email);
    Task UpdateLastLoginAsync(Guid userId, DateTime loginTime);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByPhoneAsync(string phone);
    Task<User?> GetByEmailOrPhoneAsync(string identifier);
    Task UpdateEmailAsync(Guid userId, string newEmail);
}