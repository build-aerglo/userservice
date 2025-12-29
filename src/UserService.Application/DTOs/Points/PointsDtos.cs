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
