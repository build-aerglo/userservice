using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

public interface IReferralRewardTierRepository
{
    Task<ReferralRewardTier?> GetByIdAsync(Guid id);
    Task<IEnumerable<ReferralRewardTier>> GetAllAsync();
    Task<IEnumerable<ReferralRewardTier>> GetActiveAsync();
    Task<ReferralRewardTier?> GetTierForReferralCountAsync(int referralCount);
    Task AddAsync(ReferralRewardTier tier);
    Task UpdateAsync(ReferralRewardTier tier);
    Task DeleteAsync(Guid id);
}
