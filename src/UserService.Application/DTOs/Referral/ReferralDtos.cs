namespace UserService.Application.DTOs.Referral;

// Response DTOs
public record UserReferralCodeDto(
    Guid UserId,
    string Code,
    int TotalReferrals,
    int SuccessfulReferrals,
    bool IsActive,
    DateTime CreatedAt
);

public record ReferralDto(
    Guid Id,
    Guid ReferrerId,
    Guid ReferredUserId,
    string ReferralCode,
    string Status,
    int ApprovedReviewCount,
    bool PointsAwarded,
    DateTime? QualifiedAt,
    DateTime? CompletedAt,
    DateTime CreatedAt
);

public record ReferralStatsDto(
    Guid UserId,
    string Code,
    int TotalReferrals,
    int PendingReferrals,
    int SuccessfulReferrals,
    decimal TotalPointsEarned
);

public record ReferralListResponseDto(
    Guid UserId,
    IEnumerable<ReferralDto> Referrals,
    int TotalCount,
    ReferralStatsDto Stats
);

public record ApplyReferralCodeResponseDto(
    bool Success,
    string Message,
    Guid? ReferrerId
);

// Request DTOs
public record GenerateReferralCodeDto(
    Guid UserId
);

public record ApplyReferralCodeDto(
    Guid UserId,
    string Code
);

public record ProcessReferralReviewDto(
    Guid ReferredUserId,
    Guid ReviewId,
    bool IsApproved
);

public record TopReferrerDto(
    int Rank,
    Guid UserId,
    string Username,
    string Code,
    int SuccessfulReferrals,
    decimal TotalPointsEarned
);
