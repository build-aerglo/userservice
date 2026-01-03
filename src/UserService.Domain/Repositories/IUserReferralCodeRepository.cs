using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

public interface IUserReferralCodeRepository
{
    Task<UserReferralCode?> GetByIdAsync(Guid id);
    Task<UserReferralCode?> GetByUserIdAsync(Guid userId);
    Task<UserReferralCode?> GetByCodeAsync(string code);
    Task<bool> CodeExistsAsync(string code);
    Task<IEnumerable<UserReferralCode>> GetTopReferrersAsync(int count);
    Task AddAsync(UserReferralCode referralCode);
    Task UpdateAsync(UserReferralCode referralCode);
}
