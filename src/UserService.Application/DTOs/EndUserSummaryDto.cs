using UserService.Domain.Entities;

public class PaginatedReviews
{
    public IEnumerable<ReviewResponseDto> Items { get; set; } = new List<ReviewResponseDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class EndUserSummaryDto
{
    public Guid? UserId { get; set; }
    public string? Email { get; set; }
    public EndUserProfileDetailDto? Profile { get; set; }
    public PaginatedReviews Reviews { get; set; } = new();
    public IEnumerable<TopCityStat> TopCities { get; set; } = new List<TopCityStat>();
    public IEnumerable<TopCategoryStat> TopCategories { get; set; } = new List<TopCategoryStat>();
    public UserBadge? TierBadge { get; set; }
    public IEnumerable<UserBadge> AchievementBadges { get; set; } = new List<UserBadge>();
    
    // Points
    public int Points { get; set; }
    public int Rank { get; set; }
    public int Streak { get; set; }
    public int LifetimePoints { get; set; }
    public string PointTier { get; set; } = "Bronze";        // NEW
    public int LongestStreak { get; set; }                   // NEW
    
    
    public decimal ReviewPoints { get; set; }          // Earned from reviews
    public decimal ReferralPoints { get; set; }        // Earned from referrals
    public decimal StreakPoints { get; set; }          // Earned from login streaks
    public decimal BonusPoints { get; set; }           // Other bonuses (milestones, loyalty, etc.)
    public decimal OtherPoints { get; set; }           // Anything not categorized above
    public IEnumerable<PointActivityDto> RecentActivity { get; set; } = new List<PointActivityDto>();
    
    // Redemptions                                            // NEW BLOCK
    public int TotalPointsRedeemed { get; set; }
    public int RemainingRedeemablePoints { get; set; }
    public IEnumerable<RedemptionSummaryDto> RecentRedemptions { get; set; } = new List<RedemptionSummaryDto>();
    
    // Referral                                              // NEW BLOCK
    public UserReferralSummaryDto? Referral { get; set; }
}

// New supporting DTOs
public class RedemptionSummaryDto
{
    public int PointsRedeemed { get; set; }
    public decimal AmountInNaira { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class UserReferralSummaryDto
{
    public string? Code { get; set; }
    public int TotalReferrals { get; set; }
    public int SuccessfulReferrals { get; set; }
    public int PendingReferrals { get; set; }
    public decimal TotalPointsEarned { get; set; }
    public bool WasReferred { get; set; }
    public string? ReferredByUsername { get; set; }
}