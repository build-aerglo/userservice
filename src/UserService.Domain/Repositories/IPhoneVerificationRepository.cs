using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

public interface IPhoneVerificationRepository
{
    Task<PhoneVerification?> GetByIdAsync(Guid id);
    Task<PhoneVerification?> GetByUserIdAsync(Guid userId);
    Task<PhoneVerification?> GetLatestByUserIdAsync(Guid userId);
    Task<PhoneVerification?> GetActiveByUserIdAsync(Guid userId);
    Task AddAsync(PhoneVerification verification);
    Task UpdateAsync(PhoneVerification verification);
    Task DeleteAsync(Guid id);
    Task DeleteExpiredAsync();
}
