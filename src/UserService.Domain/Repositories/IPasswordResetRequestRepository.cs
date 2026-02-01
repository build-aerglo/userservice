using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

public interface IPasswordResetRequestRepository
{
    Task AddAsync(PasswordResetRequest request);
    Task<PasswordResetRequest?> GetByUserIdAsync(Guid userId);
    Task<PasswordResetRequest?> GetByResetIdAsync(Guid resetId);
    Task DeleteExpiredAsync();
    Task DeleteByUserIdAsync(Guid userId);
}
