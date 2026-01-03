using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

public interface IReferralRepository
{
    Task<Referral?> GetByIdAsync(Guid id);
    Task<Referral?> GetByReferredUserIdAsync(Guid referredUserId);
    Task<IEnumerable<Referral>> GetByReferrerUserIdAsync(Guid referrerUserId);
    Task<IEnumerable<Referral>> GetByReferralCodeAsync(string code);
    Task<IEnumerable<Referral>> GetByStatusAsync(string status);
    Task<IEnumerable<Referral>> GetPendingByReferrerAsync(Guid referrerUserId);
    Task<IEnumerable<Referral>> GetCompletedByReferrerAsync(Guid referrerUserId);
    Task<int> GetSuccessfulReferralCountAsync(Guid referrerUserId);
    Task<IEnumerable<Referral>> GetExpiredPendingAsync();
    Task AddAsync(Referral referral);
    Task UpdateAsync(Referral referral);
}
