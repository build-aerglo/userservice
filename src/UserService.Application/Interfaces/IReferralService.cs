using UserService.Application.DTOs.Referral;

namespace UserService.Application.Interfaces;

public interface IReferralService
{
    // Referral codes
    Task<UserReferralCodeDto> GetOrCreateReferralCodeAsync(Guid userId);
    Task<UserReferralCodeDto?> GetReferralCodeByUserIdAsync(Guid userId);
    Task<UserReferralCodeDto?> GetReferralCodeByCodeAsync(string code);
    Task<UserReferralCodeDto> SetCustomCodeAsync(Guid userId, string customCode);
    Task<bool> ValidateReferralCodeAsync(string code, Guid referredUserId);

    // Referrals
    Task<UseReferralCodeResultDto> UseReferralCodeAsync(UseReferralCodeDto dto);
    Task<ReferralDto?> GetReferralByIdAsync(Guid referralId);
    Task<ReferralDto?> GetReferralByReferredUserIdAsync(Guid referredUserId);
    Task<IEnumerable<ReferralDto>> GetReferralsByReferrerAsync(Guid referrerUserId);
    Task<ReferralDto> CompleteReferralAsync(Guid referralId);
    Task<int> GetSuccessfulReferralCountAsync(Guid userId);

    // Summary
    Task<ReferralSummaryDto> GetReferralSummaryAsync(Guid userId);
    Task<IEnumerable<ReferralLeaderboardEntryDto>> GetReferralLeaderboardAsync(int count = 10);

    // Reward tiers
    Task<IEnumerable<ReferralRewardTierDto>> GetRewardTiersAsync();
    Task<ReferralRewardTierDto?> GetCurrentTierAsync(Guid userId);

    // Campaigns
    Task<ReferralCampaignDto?> GetActiveCampaignAsync();
    Task<IEnumerable<ReferralCampaignDto>> GetAllCampaignsAsync();
    Task<ReferralCampaignDto> CreateCampaignAsync(CreateReferralCampaignDto dto);

    // Invites
    Task<ReferralDto> SendReferralInviteAsync(SendReferralInviteDto dto);

    // Expiry processing
    Task ProcessExpiredReferralsAsync();
}
