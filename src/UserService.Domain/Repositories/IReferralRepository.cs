using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

public interface IUserReferralCodeRepository
{
    Task<UserReferralCode?> GetByIdAsync(Guid id);
    Task<UserReferralCode?> GetByUserIdAsync(Guid userId);
    Task<UserReferralCode?> GetByCodeAsync(string code);
    Task AddAsync(UserReferralCode referralCode);
    Task UpdateAsync(UserReferralCode referralCode);
    Task<bool> CodeExistsAsync(string code);
    Task<IEnumerable<UserReferralCode>> GetTopReferrersAsync(int limit = 10);
}

public interface IReferralRepository
{
    Task<Referral?> GetByIdAsync(Guid id);
    Task<Referral?> GetByReferredUserIdAsync(Guid referredUserId);
    Task<IEnumerable<Referral>> GetByReferrerIdAsync(Guid referrerId);
    Task AddAsync(Referral referral);
    Task UpdateAsync(Referral referral);
    Task<IEnumerable<Referral>> GetByStatusAsync(string status);
    Task<IEnumerable<Referral>> GetQualifiedButNotCompletedAsync();
    Task<int> GetSuccessfulReferralCountAsync(Guid referrerId);
    Task<IEnumerable<Referral>> GetByReferralCodeAsync(string code);
}
