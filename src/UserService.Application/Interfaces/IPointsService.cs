using UserService.Application.DTOs.Points;

namespace UserService.Application.Interfaces;

public interface IPointsService
{
    /// <summary>
    /// Get user's points and statistics
    /// </summary>
    Task<UserPointsDto> GetUserPointsAsync(Guid userId);

    /// <summary>
    /// Get user's point transaction history
    /// </summary>
    Task<PointsHistoryResponseDto> GetPointsHistoryAsync(Guid userId, int limit = 50, int offset = 0);

    /// <summary>
    /// Award points to a user
    /// </summary>
    Task<PointTransactionDto> AwardPointsAsync(AwardPointsDto dto);

    /// <summary>
    /// Deduct points from a user
    /// </summary>
    Task<PointTransactionDto> DeductPointsAsync(DeductPointsDto dto);

    /// <summary>
    /// Calculate points for a review based on content
    /// </summary>
    Task<ReviewPointsResultDto> CalculateReviewPointsAsync(CalculateReviewPointsDto dto);

    /// <summary>
    /// Award points for a review and record the transaction
    /// </summary>
    Task<PointTransactionDto> AwardReviewPointsAsync(CalculateReviewPointsDto dto);

    /// <summary>
    /// Update user's streak based on activity
    /// </summary>
    Task UpdateStreakAsync(Guid userId, DateTime activityDate);

    /// <summary>
    /// Check and award streak milestone points (100-day streak bonus)
    /// </summary>
    Task<PointTransactionDto?> CheckAndAwardStreakMilestoneAsync(Guid userId);

    /// <summary>
    /// Check and award review milestone points (25 reviews, etc.)
    /// </summary>
    Task<PointTransactionDto?> CheckAndAwardReviewMilestoneAsync(Guid userId, int totalReviews);

    /// <summary>
    /// Check and award helpful vote milestone points
    /// </summary>
    Task<PointTransactionDto?> CheckAndAwardHelpfulVoteMilestoneAsync(Guid userId, int totalHelpfulVotes);

    /// <summary>
    /// Award referral bonus points
    /// </summary>
    Task<PointTransactionDto> AwardReferralBonusAsync(Guid userId, Guid referralId, bool isVerifiedUser);

    /// <summary>
    /// Get global leaderboard
    /// </summary>
    Task<LeaderboardResponseDto> GetLeaderboardAsync(int limit = 10);

    /// <summary>
    /// Get location-based leaderboard
    /// </summary>
    Task<LeaderboardResponseDto> GetLocationLeaderboardAsync(string state, int limit = 10);

    /// <summary>
    /// Get user's tier based on total points
    /// </summary>
    Task<string> GetUserTierAsync(Guid userId);

    /// <summary>
    /// Initialize points record for new user
    /// </summary>
    Task InitializeUserPointsAsync(Guid userId);
}
