namespace UserService.Application.DTOs.Referral;

// ========================================================================
// RESPONSE DTOs
// ========================================================================

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

public record TopReferrerDto(
    int Rank,
    Guid UserId,
    string Username,
    string Code,
    int SuccessfulReferrals,
    decimal TotalPointsEarned
);

//     Referral code details
public record ReferralCodeDetailsDto(
    string Code,
    Guid UserId,
    string Username,
    int TotalReferrals,
    int SuccessfulReferrals,
    bool IsActive
);

//     Referred by info
public record ReferredByDto(
    Guid ReferrerId,
    string ReferrerUsername,
    string ReferralCode,
    string Status,
    int ApprovedReviewCount,
    DateTime ReferredAt
);

//     Reward tiers
public record RewardTierDto(
    string Name,
    int NonVerifiedPoints,
    int VerifiedPoints,
    string Description
);

public record RewardTiersResponseDto(
    IEnumerable<RewardTierDto> Tiers,
    string CurrentTier
);

//     User tier
public record UserTierDto(
    Guid UserId,
    string Tier,
    int TotalReferrals,
    int SuccessfulReferrals,
    decimal TotalPointsEarned,
    int NextTierReferralsNeeded
);

//     Campaigns
public record ReferralCampaignDto(
    Guid Id,
    string Name,
    string Description,
    int NonVerifiedBonus,
    int VerifiedBonus,
    DateTime StartDate,
    DateTime EndDate,
    bool IsActive
);

// ========================================================================
// REQUEST DTOs
// ========================================================================

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

//     Set custom referral code
public record SetCustomReferralCodeDto(
    Guid UserId,
    string CustomCode
);

//     Create campaign
public record CreateCampaignDto(
    string Name,
    string Description,
    int NonVerifiedBonus,
    int VerifiedBonus,
    DateTime StartDate,
    DateTime EndDate
);

//     Send referral invite
public record SendReferralInviteDto(
    Guid UserId,
    string RecipientEmail,
    string? PersonalMessage = null
);

public record SendReferralInviteResponseDto(
    bool Success,
    string Message
);