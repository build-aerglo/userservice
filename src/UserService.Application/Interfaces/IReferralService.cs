using UserService.Application.DTOs.Referral;

namespace UserService.Application.Interfaces;

public interface IReferralService
{
    // ========================================================================
    // REFERRAL CODE MANAGEMENT
    // ========================================================================
    
    /// <summary>
    /// Get user's referral code
    /// </summary>
    Task<UserReferralCodeDto?> GetUserReferralCodeAsync(Guid userId);

    /// <summary>
    /// Generate a new referral code for a user
    /// </summary>
    Task<UserReferralCodeDto> GenerateReferralCodeAsync(GenerateReferralCodeDto dto);

    /// <summary>
    /// Get referral code details by code
    /// </summary>
    Task<ReferralCodeDetailsDto?> GetReferralCodeDetailsAsync(string code);

    /// <summary>
    /// Set a custom referral code for a user
    /// </summary>
    Task<UserReferralCodeDto> SetCustomReferralCodeAsync(SetCustomReferralCodeDto dto);

    /// <summary>
    /// Validate a referral code
    /// </summary>
    Task<bool> ValidateReferralCodeAsync(string code);

    /// <summary>
    /// Generate a unique referral code based on username
    /// </summary>
    Task<string> GenerateUniqueCodeAsync(string username);

    // ========================================================================
    // REFERRAL USAGE
    // ========================================================================

    /// <summary>
    /// Apply a referral code when user signs up
    /// </summary>
    Task<ApplyReferralCodeResponseDto> ApplyReferralCodeAsync(ApplyReferralCodeDto dto);

    // ========================================================================
    // REFERRAL TRACKING
    // ========================================================================

    /// <summary>
    /// Get all referrals made by a user
    /// </summary>
    Task<ReferralListResponseDto> GetUserReferralsAsync(Guid userId);

    /// <summary>
    /// Get referral statistics for a user
    /// </summary>
    Task<ReferralStatsDto> GetReferralStatsAsync(Guid userId);

    /// <summary>
    /// Get who referred a specific user
    /// </summary>
    Task<ReferredByDto?> GetReferredByAsync(Guid userId);

    /// <summary>
    /// Check if user was referred
    /// </summary>
    Task<bool> WasUserReferredAsync(Guid userId);

    /// <summary>
    /// Process a review approval for a referred user
    /// Called when a review is approved to track referral progress
    /// </summary>
    Task ProcessReferralReviewAsync(ProcessReferralReviewDto dto);

    /// <summary>
    /// Complete a qualified referral and award points
    /// </summary>
    Task CompleteReferralAsync(Guid referralId);

    /// <summary>
    /// Process all qualified referrals (background job)
    /// </summary>
    Task ProcessQualifiedReferralsAsync();

    // ========================================================================
    // LEADERBOARD & REWARDS
    // ========================================================================

    /// <summary>
    /// Get top referrers leaderboard
    /// </summary>
    Task<IEnumerable<TopReferrerDto>> GetTopReferrersAsync(int limit = 10);

    /// <summary>
    /// Get reward tiers information
    /// </summary>
    Task<RewardTiersResponseDto> GetRewardTiersAsync();

    /// <summary>
    /// Get user's current reward tier
    /// </summary>
    Task<UserTierDto> GetUserTierAsync(Guid userId);

    // ========================================================================
    // CAMPAIGNS
    // ========================================================================

    /// <summary>
    /// Get active referral campaign
    /// </summary>
    Task<ReferralCampaignDto?> GetActiveCampaignAsync();

    /// <summary>
    /// Get all referral campaigns
    /// </summary>
    Task<IEnumerable<ReferralCampaignDto>> GetAllCampaignsAsync();

    /// <summary>
    /// Create a new referral campaign
    /// </summary>
    Task<ReferralCampaignDto> CreateCampaignAsync(CreateCampaignDto dto);

    // ========================================================================
    // INVITES
    // ========================================================================

    /// <summary>
    /// Send referral invite via email
    /// </summary>
    Task<SendReferralInviteResponseDto> SendReferralInviteAsync(SendReferralInviteDto dto);
}