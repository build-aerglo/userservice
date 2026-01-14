using UserService.Application.DTOs.Referral;

namespace UserService.Application.Interfaces;

public interface IReferralService
{
    /// <summary>
    /// Get user's referral code
    /// </summary>
    Task<UserReferralCodeDto?> GetUserReferralCodeAsync(Guid userId);

    /// <summary>
    /// Generate a new referral code for a user
    /// </summary>
    Task<UserReferralCodeDto> GenerateReferralCodeAsync(GenerateReferralCodeDto dto);

    /// <summary>
    /// Apply a referral code when user signs up
    /// </summary>
    Task<ApplyReferralCodeResponseDto> ApplyReferralCodeAsync(ApplyReferralCodeDto dto);

    /// <summary>
    /// Get all referrals made by a user
    /// </summary>
    Task<ReferralListResponseDto> GetUserReferralsAsync(Guid userId);

    /// <summary>
    /// Get referral statistics for a user
    /// </summary>
    Task<ReferralStatsDto> GetReferralStatsAsync(Guid userId);

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

    /// <summary>
    /// Get top referrers leaderboard
    /// </summary>
    Task<IEnumerable<TopReferrerDto>> GetTopReferrersAsync(int limit = 10);

    /// <summary>
    /// Validate a referral code
    /// </summary>
    Task<bool> ValidateReferralCodeAsync(string code);

    /// <summary>
    /// Check if user was referred
    /// </summary>
    Task<bool> WasUserReferredAsync(Guid userId);

    /// <summary>
    /// Generate a unique referral code based on username
    /// </summary>
    Task<string> GenerateUniqueCodeAsync(string username);
}
