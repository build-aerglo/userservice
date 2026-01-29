using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

public interface IPasswordResetRequestRepository
{
    Task AddAsync(PasswordResetRequest request);
    Task<PasswordResetRequest?> GetByIdentifierAsync(string identifier);
    Task<PasswordResetRequest?> GetLatestByIdentifierAsync(string identifier);
    Task<PasswordResetRequest?> GetByIdAsync(Guid id);
    Task UpdateAsync(PasswordResetRequest request);
    Task DeleteExpiredAsync();
}
