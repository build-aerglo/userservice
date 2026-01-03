namespace UserService.Application.DTOs.Referral;

// Response DTOs
public record UserReferralCodeDto(
    Guid Id,
    Guid UserId,
    string ReferralCode,
    string? CustomCode,
    string ActiveCode,
    bool IsActive,
    int TotalReferrals,
    int SuccessfulReferrals,
    int PendingReferrals,
    int TotalPointsEarned,
    DateTime CreatedAt
);

public record ReferralDto(
    Guid Id,
    Guid ReferrerUserId,
    Guid? ReferredUserId,
    string ReferralCode,
    string Status,
    string? ReferredEmail,
    string? ReferredPhone,
    int ReferrerRewardPoints,
    int ReferredRewardPoints,
    bool ReferrerRewarded,
    bool ReferredRewarded,
    DateTime? ExpiresAt,
    DateTime? CompletedAt,
    DateTime CreatedAt
);

public record ReferralRewardTierDto(
    Guid Id,
    string TierName,
    int MinReferrals,
    int? MaxReferrals,
    int ReferrerPoints,
    int ReferredPoints,
    decimal BonusMultiplier,
    bool IsActive
);

public record ReferralCampaignDto(
    Guid Id,
    string Name,
    string? Description,
    int BonusReferrerPoints,
    int BonusReferredPoints,
    decimal Multiplier,
    DateTime StartsAt,
    DateTime EndsAt,
    int? MaxReferralsPerUser,
    bool IsActive,
    bool IsCurrentlyActive
);

public record ReferralSummaryDto(
    UserReferralCodeDto ReferralCode,
    ReferralRewardTierDto CurrentTier,
    ReferralRewardTierDto? NextTier,
    int ReferralsToNextTier,
    IEnumerable<ReferralDto> RecentReferrals,
    ReferralCampaignDto? ActiveCampaign
);

public record ReferralLeaderboardEntryDto(
    int Rank,
    Guid UserId,
    string? Username,
    int SuccessfulReferrals,
    int TotalPointsEarned
);

public record UseReferralCodeResultDto(
    bool Success,
    string Message,
    Guid? ReferralId,
    Guid? ReferrerUserId
);

// Request DTOs
public record CreateReferralCodeDto(
    Guid UserId,
    string? CustomCode = null
);

public record SetCustomCodeDto(
    string CustomCode
);

public record UseReferralCodeDto(
    Guid ReferredUserId,
    string ReferralCode
);

public record SendReferralInviteDto(
    Guid ReferrerUserId,
    string? Email,
    string? Phone
);

public record CreateReferralCampaignDto(
    string Name,
    string? Description,
    int BonusReferrerPoints,
    int BonusReferredPoints,
    decimal Multiplier,
    DateTime StartsAt,
    DateTime EndsAt,
    int? MaxReferralsPerUser
);

public record CompleteReferralDto(
    Guid ReferralId,
    string CompletedAction
);
