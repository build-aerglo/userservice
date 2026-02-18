using UserService.Application.DTOs.Badge;
using UserService.Domain.Entities;

namespace UserService.Application.Interfaces;

public interface IBadgeService
{
    /// <summary>
    /// Get all badges for a user
    /// </summary>
    Task<UserBadgesResponseDto> GetUserBadgesAsync(Guid userId);

    /// <summary>
    /// Get a specific badge by ID
    /// </summary>
    Task<UserBadgeDto?> GetBadgeByIdAsync(Guid badgeId);

    /// <summary>
    /// Assign a badge to a user
    /// </summary>
    Task<UserBadgeDto> AssignBadgeAsync(AssignBadgeDto dto);

    /// <summary>
    /// Revoke a badge from a user
    /// </summary>
    Task<bool> RevokeBadgeAsync(RevokeBadgeDto dto);

    /// <summary>
    /// Calculate and update tier badge (Newbie/Expert/Pro) for a user
    /// </summary>
    Task<UserBadgeDto?> CalculateTierBadgeAsync(Guid userId, int reviewCount, int daysSinceJoin);

    /// <summary>
    /// Check if user qualifies for Pioneer badge (joined first 100 days of launch)
    /// </summary>
    Task<bool> CheckAndAssignPioneerBadgeAsync(Guid userId, DateTime joinDate);

    /// <summary>
    /// Check if user qualifies for Top Contributor badge in a location
    /// </summary>
    Task<bool> CheckAndAssignTopContributorBadgeAsync(Guid userId, string location, int userRankInLocation, int disputeCount);

    /// <summary>
    /// Check if user qualifies for Expert in Category badge
    /// </summary>
    Task<bool> CheckAndAssignCategoryExpertBadgeAsync(Guid userId, string category, int reviewsInCategory, int helpfulVotes);

    /// <summary>
    /// Check if user qualifies for Most Helpful badge
    /// </summary>
    Task<bool> CheckAndAssignMostHelpfulBadgeAsync(Guid userId, int helpfulVoteRankPercentile);

    /// <summary>
    /// Recalculate all badges for a user (used by background job)
    /// </summary>
    Task RecalculateAllBadgesAsync(Guid userId);

    /// <summary>
    /// Get badge display information (name, description, icon)
    /// </summary>
    (string DisplayName, string Description, string Icon) GetBadgeInfo(string badgeType, string? location = null, string? category = null);

    string GetCurrentTier(IEnumerable<UserBadge> badges);
    bool IsTierBadge(string badgeType);
}
