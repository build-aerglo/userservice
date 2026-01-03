using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

public interface IReferralCampaignRepository
{
    Task<ReferralCampaign?> GetByIdAsync(Guid id);
    Task<IEnumerable<ReferralCampaign>> GetAllAsync();
    Task<IEnumerable<ReferralCampaign>> GetActiveAsync();
    Task<ReferralCampaign?> GetCurrentlyActiveAsync();
    Task AddAsync(ReferralCampaign campaign);
    Task UpdateAsync(ReferralCampaign campaign);
    Task DeleteAsync(Guid id);
}
