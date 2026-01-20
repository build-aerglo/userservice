
using UserService.Application.DTOs.Points;

namespace UserService.Application.Interfaces;

public interface IPointsService
{
    // ========================================================================
    // CORE POINTS OPERATIONS
    // ========================================================================
    
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
    /// Initialize points record for new user
    /// </summary>
    Task InitializeUserPointsAsync(Guid userId);

    // ========================================================================
    // REVIEW POINTS
    // ========================================================================
    
    /// <summary>
    /// Calculate points for a review based on content
    /// </summary>
    Task<ReviewPointsResultDto> CalculateReviewPointsAsync(CalculateReviewPointsDto dto);

    /// <summary>
    /// Award points for a review and record the transaction
    /// </summary>
    Task<PointTransactionDto> AwardReviewPointsAsync(CalculateReviewPointsDto dto);

    // ========================================================================
    // STREAK MANAGEMENT
    // ========================================================================
    
    /// <summary>
    /// Update user's login streak based on login date
    /// </summary>
    Task UpdateLoginStreakAsync(Guid userId, DateTime loginDate);

    /// <summary>
    /// Check and award streak milestone points (100-day streak bonus)
    /// </summary>
    Task<PointTransactionDto?> CheckAndAwardStreakMilestoneAsync(Guid userId);

    // ========================================================================
    // MILESTONES
    // ========================================================================
    
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

    // ========================================================================
    // LEADERBOARDS & TIERS
    // ========================================================================
    
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

    // ========================================================================
    // POINT REDEMPTION
    // ========================================================================
    
    /// <summary>
    /// Redeem points for airtime
    /// </summary>
    Task<RedemptionResponseDto> RedeemPointsAsync(RedeemPointsDto dto);

    /// <summary>
    /// Get redemption history for a user
    /// </summary>
    Task<RedemptionHistoryDto> GetRedemptionHistoryAsync(Guid userId, int limit = 50, int offset = 0);

    // ========================================================================
    // POINT RULES MANAGEMENT
    // ========================================================================
    
    /// <summary>
    /// Get all point rules
    /// </summary>
    Task<IEnumerable<PointRuleDto>> GetAllPointRulesAsync();

    /// <summary>
    /// Get point rule by action type
    /// </summary>
    Task<PointRuleDto> GetPointRuleByActionTypeAsync(string actionType);

    /// <summary>
    /// Create a new point rule (admin only)
    /// </summary>
    Task<PointRuleDto> CreatePointRuleAsync(CreatePointRuleDto dto, Guid? createdBy);

    /// <summary>
    /// Update an existing point rule (admin only)
    /// </summary>
    Task<PointRuleDto> UpdatePointRuleAsync(Guid id, UpdatePointRuleDto dto, Guid? updatedBy);

    // ========================================================================
    // POINT MULTIPLIERS MANAGEMENT
    // ========================================================================
    
    /// <summary>
    /// Get all active point multipliers
    /// </summary>
    Task<IEnumerable<PointMultiplierDto>> GetActivePointMultipliersAsync();

    /// <summary>
    /// Get all point multipliers (admin only)
    /// </summary>
    Task<IEnumerable<PointMultiplierDto>> GetAllPointMultipliersAsync();

    /// <summary>
    /// Create a new point multiplier (admin only)
    /// </summary>
    Task<PointMultiplierDto> CreatePointMultiplierAsync(CreatePointMultiplierDto dto, Guid? createdBy);

    /// <summary>
    /// Update an existing point multiplier (admin only)
    /// </summary>
    Task<PointMultiplierDto> UpdatePointMultiplierAsync(Guid id, UpdatePointMultiplierDto dto, Guid? updatedBy);

    // ========================================================================
    // SUMMARY & QUERIES
    // ========================================================================
    
    /// <summary>
    /// Get comprehensive user points summary with recent transactions
    /// </summary>
    Task<UserPointsSummaryDto> GetUserPointsSummaryAsync(Guid userId, int transactionLimit = 10);

    /// <summary>
    /// Get transactions by transaction type
    /// </summary>
    Task<PointTransactionsByTypeDto> GetTransactionsByTypeAsync(Guid userId, string transactionType);

    /// <summary>
    /// Get transactions by date range
    /// </summary>
    Task<PointTransactionsByDateRangeDto> GetTransactionsByDateRangeAsync(
        Guid userId, 
        DateTime startDate, 
        DateTime endDate);
}