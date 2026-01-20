namespace UserService.Application.DTOs.Points;

// Response DTOs
public record UserPointsDto(
    Guid UserId,
    decimal TotalPoints,
    string Tier,
    int CurrentStreak,
    int LongestStreak,
    DateTime? LastActivityDate,
    int Rank
);

public record PointTransactionDto(
    Guid Id,
    Guid UserId,
    decimal Points,
    string TransactionType,
    string Description,
    Guid? ReferenceId,
    string? ReferenceType,
    DateTime CreatedAt
);

public record PointsHistoryResponseDto(
    Guid UserId,
    decimal TotalPoints,
    IEnumerable<PointTransactionDto> Transactions,
    int TotalCount
);

public record LeaderboardEntryDto(
    int Rank,
    Guid UserId,
    string Username,
    decimal TotalPoints,
    string Tier,
    int BadgeCount
);

public record LeaderboardResponseDto(
    IEnumerable<LeaderboardEntryDto> Entries,
    string? Location,
    int TotalUsers
);

// Request DTOs
public record AwardPointsDto(
    Guid UserId,
    decimal Points,
    string TransactionType,
    string Description,
    Guid? ReferenceId = null,
    string? ReferenceType = null
);

public record DeductPointsDto(
    Guid UserId,
    decimal Points,
    string Reason
);

// Review points calculation request
public record CalculateReviewPointsDto(
    Guid UserId,
    Guid ReviewId,
    bool HasStars,
    bool HasHeader,
    int BodyLength,
    int ImageCount,
    bool IsVerifiedUser
);

// Review points calculation result
public record ReviewPointsResultDto(
    decimal TotalPoints,
    decimal StarPoints,
    decimal HeaderPoints,
    decimal BodyPoints,
    decimal ImagePoints,
    bool VerifiedBonus,
    string Breakdown
);


// Redemption DTOs
public record RedeemPointsDto(
    Guid UserId,
    decimal Points,
    string PhoneNumber
);

public record RedemptionResponseDto(
    Guid RedemptionId,
    decimal PointsRedeemed,
    decimal AmountInNaira,
    string PhoneNumber,
    string Status,
    string? TransactionReference,
    DateTime CreatedAt
);

public record RedemptionHistoryDto(
    Guid UserId,
    IEnumerable<RedemptionResponseDto> Redemptions,
    int TotalCount
);

// Point Rules DTOs
public record PointRuleDto(
    Guid Id,
    string ActionType,
    string Description,
    decimal BasePointsNonVerified,
    decimal BasePointsVerified,
    string? Conditions,
    bool IsActive
);

public record CreatePointRuleDto(
    string ActionType,
    string Description,
    decimal BasePointsNonVerified,
    decimal BasePointsVerified,
    string? Conditions = null
);

public record UpdatePointRuleDto(
    string? Description = null,
    decimal? BasePointsNonVerified = null,
    decimal? BasePointsVerified = null,
    string? Conditions = null,
    bool? IsActive = null
);

// Point Multipliers DTOs
public record PointMultiplierDto(
    Guid Id,
    string Name,
    string Description,
    decimal Multiplier,
    string[]? ActionTypes,
    DateTime StartDate,
    DateTime EndDate,
    bool IsActive,
    bool IsCurrentlyActive
);

public record CreatePointMultiplierDto(
    string Name,
    string Description,
    decimal Multiplier,
    DateTime StartDate,
    DateTime EndDate,
    string[]? ActionTypes = null
);

public record UpdatePointMultiplierDto(
    string? Name = null,
    string? Description = null,
    decimal? Multiplier = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    string[]? ActionTypes = null,
    bool? IsActive = null
);

// Summary DTOs
public record UserPointsSummaryDto(
    Guid UserId,
    decimal TotalPoints,
    string Tier,
    int CurrentStreak,
    int LongestStreak,
    DateTime? LastLoginDate,
    int Rank,
    IEnumerable<PointTransactionDto> RecentTransactions
);

public record PointTransactionsByTypeDto(
    Guid UserId,
    string TransactionType,
    IEnumerable<PointTransactionDto> Transactions,
    decimal TotalPoints,
    int Count
);

public record PointTransactionsByDateRangeDto(
    Guid UserId,
    DateTime StartDate,
    DateTime EndDate,
    IEnumerable<PointTransactionDto> Transactions,
    decimal TotalPointsEarned,
    decimal TotalPointsDeducted,
    int Count
);
