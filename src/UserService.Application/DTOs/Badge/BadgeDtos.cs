namespace UserService.Application.DTOs.Badge;

// Response DTOs
public record BadgeDefinitionDto(
    Guid Id,
    string Name,
    string DisplayName,
    string? Description,
    string? IconUrl,
    int Tier,
    int PointsRequired,
    string Category,
    bool IsActive
);

public record UserBadgeDto(
    Guid Id,
    Guid UserId,
    Guid BadgeId,
    string BadgeName,
    string BadgeDisplayName,
    string? BadgeDescription,
    string? BadgeIconUrl,
    int BadgeTier,
    string BadgeCategory,
    DateTime EarnedAt,
    string? Source
);

public record UserBadgeLevelDto(
    Guid UserId,
    string CurrentLevel,
    int LevelProgress,
    int TotalBadgesEarned,
    string NextLevel,
    int BadgesToNextLevel
);

public record UserBadgeSummaryDto(
    Guid UserId,
    string CurrentLevel,
    int LevelProgress,
    int TotalBadgesEarned,
    IEnumerable<UserBadgeDto> RecentBadges,
    IEnumerable<BadgeDefinitionDto> AvailableBadges
);

// Request DTOs
public record CreateBadgeDefinitionDto(
    string Name,
    string DisplayName,
    string? Description,
    string? IconUrl,
    int Tier,
    int PointsRequired,
    string Category
);

public record UpdateBadgeDefinitionDto(
    string? DisplayName,
    string? Description,
    string? IconUrl,
    int? Tier,
    int? PointsRequired
);

public record AwardBadgeDto(
    Guid UserId,
    string BadgeName,
    string? Source
);
