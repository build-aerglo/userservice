using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

public interface IPasswordResetRequestRepository
{
    Task AddAsync(PasswordResetRequest request);
    Task<PasswordResetRequest?> GetByIdAsync(string identifier);
    Task<PasswordResetRequest?> GetByResetIdAsync(Guid resetId);
    Task DeleteExpiredAsync();
    Task DeleteByIdAsync(string identifier);
}
