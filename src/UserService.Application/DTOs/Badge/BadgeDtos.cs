namespace UserService.Application.DTOs.Badge;

// Response DTOs
public record UserBadgeDto(
    Guid Id,
    Guid UserId,
    string BadgeType,
    string? Location,
    string? Category,
    DateTime EarnedAt,
    bool IsActive,
    string DisplayName,
    string? Description
);

public record UserBadgesResponseDto(
    Guid UserId,
    IEnumerable<UserBadgeDto> Badges,
    int TotalBadges,
    string CurrentTier
);

// Request DTOs
public record AssignBadgeDto(
    Guid UserId,
    string BadgeType,
    string? Location = null,
    string? Category = null
);

public record RevokeBadgeDto(
    Guid UserId,
    string BadgeType,
    string? Location = null,
    string? Category = null
);

// Badge calculation result DTO (internal use)
public record BadgeCalculationResultDto(
    bool ShouldHaveBadge,
    string BadgeType,
    string? Location = null,
    string? Category = null,
    string? Reason = null
);
