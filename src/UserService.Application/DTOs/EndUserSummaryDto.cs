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
    
    // Replace flat Reviews with paginated wrapper
    public PaginatedReviews Reviews { get; set; } = new();
    
    public IEnumerable<TopCityStat> TopCities { get; set; } = new List<TopCityStat>();
    public IEnumerable<TopCategoryStat> TopCategories { get; set; } = new List<TopCategoryStat>();
    public UserBadge? TierBadge { get; set; }
    public IEnumerable<UserBadge> AchievementBadges { get; set; } = new List<UserBadge>();
    public int Points { get; set; }
    public int Rank { get; set; }
    public int Streak { get; set; }
    public int LifetimePoints { get; set; }
    public IEnumerable<PointActivityDto> RecentActivity { get; set; } = new List<PointActivityDto>();
}