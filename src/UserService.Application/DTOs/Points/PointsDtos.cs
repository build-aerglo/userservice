namespace UserService.Application.DTOs.Points;

// Response DTOs
public record UserPointsDto(
    Guid UserId,
    int TotalPoints,
    int AvailablePoints,
    int LifetimePoints,
    int RedeemedPoints,
    int PendingPoints,
    DateTime? LastEarnedAt
);

public record PointTransactionDto(
    Guid Id,
    Guid UserId,
    string TransactionType,
    int Points,
    int BalanceAfter,
    string? Description,
    string? ReferenceType,
    Guid? ReferenceId,
    decimal Multiplier,
    DateTime? ExpiresAt,
    DateTime CreatedAt
);

public record PointRuleDto(
    Guid Id,
    string ActionType,
    int PointsValue,
    string? Description,
    int? MaxDailyOccurrences,
    int? MaxTotalOccurrences,
    int? CooldownMinutes,
    bool IsActive,
    bool MultiplierEligible
);

public record PointMultiplierDto(
    Guid Id,
    string Name,
    string? Description,
    decimal Multiplier,
    string[]? ActionTypes,
    DateTime StartsAt,
    DateTime EndsAt,
    bool IsActive,
    bool IsCurrentlyActive
);

public record PointsSummaryDto(
    UserPointsDto Points,
    string CurrentBadgeLevel,
    IEnumerable<PointTransactionDto> RecentTransactions,
    IEnumerable<PointMultiplierDto> ActiveMultipliers
);

public record LeaderboardEntryDto(
    int Rank,
    Guid UserId,
    string? Username,
    int TotalPoints,
    int LifetimePoints,
    string BadgeLevel
);

// Request DTOs
public record EarnPointsDto(
    Guid UserId,
    string ActionType,
    string? ReferenceType = null,
    Guid? ReferenceId = null,
    string? Description = null
);

public record RedeemPointsDto(
    Guid UserId,
    int Points,
    string? Description,
    string? ReferenceType = null,
    Guid? ReferenceId = null
);

public record AdjustPointsDto(
    Guid UserId,
    int Adjustment,
    string Reason
);

public record CreatePointRuleDto(
    string ActionType,
    int PointsValue,
    string? Description,
    int? MaxDailyOccurrences,
    int? MaxTotalOccurrences,
    int? CooldownMinutes,
    bool MultiplierEligible = true
);

public record UpdatePointRuleDto(
    int? PointsValue,
    string? Description,
    int? MaxDailyOccurrences,
    int? CooldownMinutes
);

public record CreatePointMultiplierDto(
    string Name,
    string? Description,
    decimal Multiplier,
    string[]? ActionTypes,
    DateTime StartsAt,
    DateTime EndsAt
);

public record EarnPointsResultDto(
    bool Success,
    int PointsEarned,
    int NewBalance,
    string? Message,
    decimal MultiplierApplied
);
