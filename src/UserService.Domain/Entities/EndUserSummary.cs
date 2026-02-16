namespace UserService.Domain.Entities

{
    public class EndUserSummary
    {
        public Guid? UserId { get; set; }
        public string? Email { get; set; }
        public EndUserProfileDetailDto? Profile { get; set; }
        public IEnumerable<ReviewResponseDto> Reviews { get; set; } = new List<ReviewResponseDto>();
        public IEnumerable<TopCityStat> TopCities { get; set; } = new List<TopCityStat>();
        public IEnumerable<TopCategoryStat> TopCategories { get; set; } = new List<TopCategoryStat>();
        
        // Badges - now with full display information
        public IEnumerable<UserBadge> Badges { get; set; } = new List<UserBadge>();
        public string CurrentTier { get; set; } = string.Empty;
        
        // Points
        public int Points { get; set; }
        public int Rank { get; set; }
        public int Streak { get; set; }
        public int LifetimePoints { get; set; }
        public IEnumerable<PointActivityDto> RecentActivity { get; set; } = new List<PointActivityDto>();
    }

    public class PointActivityDto
    {
        public int Points { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}

public record ReviewResponseDto(
    Guid Id,
    Guid BusinessId,
    Guid? LocationId,
    Guid? ReviewerId,
    string? Email,
    decimal StarRating,
    string ReviewBody,
    string[]? PhotoUrls,
    bool ReviewAsAnon,
    bool IsGuestReview,
    DateTime CreatedAt,
    string Status,
    DateTime? ValidatedAt,
    string Name,
    string? Logo,
    bool IsVerified,
    string BusinessAddress
);

public record TopCityStat(
    string City,
    string? State,
    int ReviewCount,
    int BusinessCount,
    decimal AverageRating
);

public record TopCategoryStat(
    Guid CategoryId,
    string CategoryName,
    int ReviewCount,
    int BusinessCount,
    decimal AverageRating
);

public class UserBadgeWithDetails
{
    // From user_badges table
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid BadgeId { get; set; }
    public DateTime EarnedAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime? ExpiresAt { get; set; }
    
    // From badges table (joined)
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string BadgeType { get; set; } = string.Empty; // "Tier" or "Achievement"
    public int? TierLevel { get; set; } // For tier badges, indicates level (1,2,3 etc)
    public string? Category { get; set; } // For achievement badges
    public int? PointsValue { get; set; } // Optional points awarded for badge
    
    // Helper properties for display
    public string DisplayName => !string.IsNullOrEmpty(Name) ? Name : $"{BadgeType} Badge";
    public string DisplayIcon => !string.IsNullOrEmpty(Icon) ? Icon : "default-badge-icon.png";
    
    // For tier badges, you might want to format the name
    public string FormattedTierName => BadgeType == "Tier" && TierLevel.HasValue 
        ? $"Tier {TierLevel} - {Name}" 
        : Name;
}

// Alternative using record for immutability if preferred
public record UserBadgeWithDetailsRecord(
    Guid Id,
    Guid UserId,
    Guid BadgeId,
    DateTime EarnedAt,
    bool IsActive,
    DateTime? ExpiresAt,
    string Name,
    string? Description,
    string? Icon,
    string BadgeType,
    int? TierLevel,
    string? Category,
    int? PointsValue
)
{
    public string DisplayName => !string.IsNullOrEmpty(Name) ? Name : $"{BadgeType} Badge";
    public string DisplayIcon => !string.IsNullOrEmpty(Icon) ? Icon : "default-badge-icon.png";
    public string FormattedTierName => BadgeType == "Tier" && TierLevel.HasValue 
        ? $"Tier {TierLevel} - {Name}" 
        : Name;
}

public class EndUserProfileDetail
{
    public Guid UserId { get; init; }
    public string Username { get; init; }
    public string Email { get; init; }
    public string Phone { get; init; }
    public string? Address { get; init; }
    public DateTime JoinDate { get; init; }
    public Guid EndUserProfileId { get; init; }
    public string? SocialMedia { get; init; }
    public NotificationPreferencesDto NotificationPreferences { get; init; }
    public bool DarkMode { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}


public record EndUserProfileDetailDto(
    Guid UserId,
    string Username,
    string Email,
    string Phone,
    string? Address,
    DateTime JoinDate,
    Guid EndUserProfileId,
    string? SocialMedia,
    NotificationPreferencesDto NotificationPreferences,
    bool DarkMode,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
public record NotificationPreferencesDto(
    bool EmailNotifications,
    bool SmsNotifications,
    bool PushNotifications,
    bool MarketingEmails
);