using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

public interface IEmailVerificationRepository
{
    Task<EmailVerification?> GetByIdAsync(Guid id);
    Task<EmailVerification?> GetByUserIdAsync(Guid userId);
    Task<EmailVerification?> GetByTokenAsync(Guid token);
    Task<EmailVerification?> GetLatestByUserIdAsync(Guid userId);
    Task<EmailVerification?> GetActiveByUserIdAsync(Guid userId);
    Task AddAsync(EmailVerification verification);
    Task UpdateAsync(EmailVerification verification);
    Task DeleteAsync(Guid id);
    Task DeleteExpiredAsync();
}
