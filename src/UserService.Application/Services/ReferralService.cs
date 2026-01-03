using UserService.Application.DTOs.Points;
using UserService.Application.DTOs.Referral;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Domain.Repositories;

namespace UserService.Application.Services;

public class ReferralService(
    IUserReferralCodeRepository referralCodeRepository,
    IReferralRepository referralRepository,
    IReferralRewardTierRepository rewardTierRepository,
    IReferralCampaignRepository campaignRepository,
    IUserRepository userRepository,
    IPointsService pointsService
) : IReferralService
{
    public async Task<UserReferralCodeDto> GetOrCreateReferralCodeAsync(Guid userId)
    {
        var code = await referralCodeRepository.GetByUserIdAsync(userId);
        if (code == null)
        {
            code = new UserReferralCode(userId);
            await referralCodeRepository.AddAsync(code);
        }
        return MapToCodeDto(code);
    }

    public async Task<UserReferralCodeDto?> GetReferralCodeByUserIdAsync(Guid userId)
    {
        var code = await referralCodeRepository.GetByUserIdAsync(userId);
        return code != null ? MapToCodeDto(code) : null;
    }

    public async Task<UserReferralCodeDto?> GetReferralCodeByCodeAsync(string code)
    {
        var referralCode = await referralCodeRepository.GetByCodeAsync(code);
        return referralCode != null ? MapToCodeDto(referralCode) : null;
    }

    public async Task<UserReferralCodeDto> SetCustomCodeAsync(Guid userId, string customCode)
    {
        var code = await referralCodeRepository.GetByUserIdAsync(userId);
        if (code == null)
            throw new UserReferralCodeNotFoundException(userId);

        // Check if custom code already exists
        var existing = await referralCodeRepository.GetByCodeAsync(customCode);
        if (existing != null && existing.UserId != userId)
            throw new ReferralCodeAlreadyExistsException(customCode);

        code.SetCustomCode(customCode);
        await referralCodeRepository.UpdateAsync(code);
        return MapToCodeDto(code);
    }

    public async Task<bool> ValidateReferralCodeAsync(string code, Guid referredUserId)
    {
        var referralCode = await referralCodeRepository.GetByCodeAsync(code);
        if (referralCode == null || !referralCode.IsActive)
            return false;

        // Can't use own code
        if (referralCode.UserId == referredUserId)
            return false;

        // Check if user already referred
        var existingReferral = await referralRepository.GetByReferredUserIdAsync(referredUserId);
        if (existingReferral != null)
            return false;

        return true;
    }

    public async Task<UseReferralCodeResultDto> UseReferralCodeAsync(UseReferralCodeDto dto)
    {
        var referralCode = await referralCodeRepository.GetByCodeAsync(dto.ReferralCode);
        if (referralCode == null || !referralCode.IsActive)
            return new UseReferralCodeResultDto(false, "Invalid or inactive referral code", null, null);

        // Can't use own code
        if (referralCode.UserId == dto.ReferredUserId)
            return new UseReferralCodeResultDto(false, "Cannot use your own referral code", null, null);

        // Check if user already referred
        var existingReferral = await referralRepository.GetByReferredUserIdAsync(dto.ReferredUserId);
        if (existingReferral != null)
            return new UseReferralCodeResultDto(false, "User has already been referred", null, null);

        // Create referral
        var referral = new Referral(
            referralCode.UserId,
            referralCode.Id,
            referralCode.ActiveCode
        );
        referral.MarkAsRegistered(dto.ReferredUserId);
        await referralRepository.AddAsync(referral);

        // Update referral code stats
        referralCode.RecordReferral();
        await referralCodeRepository.UpdateAsync(referralCode);

        // Award signup points to referred user
        try
        {
            await pointsService.EarnPointsAsync(new EarnPointsDto(
                dto.ReferredUserId,
                "referral_signup",
                "referral",
                referral.Id,
                "Signed up using referral code"
            ));
        }
        catch { /* Points awarding is optional */ }

        return new UseReferralCodeResultDto(true, "Referral code applied successfully", referral.Id, referralCode.UserId);
    }

    public async Task<ReferralDto?> GetReferralByIdAsync(Guid referralId)
    {
        var referral = await referralRepository.GetByIdAsync(referralId);
        return referral != null ? MapToReferralDto(referral) : null;
    }

    public async Task<ReferralDto?> GetReferralByReferredUserIdAsync(Guid referredUserId)
    {
        var referral = await referralRepository.GetByReferredUserIdAsync(referredUserId);
        return referral != null ? MapToReferralDto(referral) : null;
    }

    public async Task<IEnumerable<ReferralDto>> GetReferralsByReferrerAsync(Guid referrerUserId)
    {
        var referrals = await referralRepository.GetByReferrerUserIdAsync(referrerUserId);
        return referrals.Select(MapToReferralDto);
    }

    public async Task<ReferralDto> CompleteReferralAsync(Guid referralId)
    {
        var referral = await referralRepository.GetByIdAsync(referralId);
        if (referral == null)
            throw new ReferralNotFoundException(referralId);

        if (referral.Status == "completed")
            throw new ReferralAlreadyCompletedException(referralId);

        if (referral.IsExpired())
            throw new ReferralExpiredException(referralId);

        // Get reward tier
        var referrerCode = await referralCodeRepository.GetByUserIdAsync(referral.ReferrerUserId);
        var successfulCount = referrerCode?.SuccessfulReferrals ?? 0;
        var tier = await rewardTierRepository.GetTierForReferralCountAsync(successfulCount);

        // Get campaign bonus
        var campaign = await campaignRepository.GetCurrentlyActiveAsync();

        var referrerPoints = (tier?.ReferrerPoints ?? 100) + (campaign?.BonusReferrerPoints ?? 0);
        var referredPoints = (tier?.ReferredPoints ?? 50) + (campaign?.BonusReferredPoints ?? 0);

        referral.MarkAsCompleted(referrerPoints, referredPoints);
        await referralRepository.UpdateAsync(referral);

        // Update referral code stats
        if (referrerCode != null)
        {
            referrerCode.CompleteReferral(referrerPoints);
            await referralCodeRepository.UpdateAsync(referrerCode);
        }

        // Award points to referrer
        try
        {
            await pointsService.EarnPointsAsync(new EarnPointsDto(
                referral.ReferrerUserId,
                "referral_completed",
                "referral",
                referralId,
                "Referral completed"
            ));
            referral.MarkReferrerRewarded();
        }
        catch { }

        // Award bonus points to referred user
        if (referral.ReferredUserId.HasValue)
        {
            try
            {
                await pointsService.EarnPointsAsync(new EarnPointsDto(
                    referral.ReferredUserId.Value,
                    "referral_completed",
                    "referral",
                    referralId,
                    "Completed referral action"
                ));
                referral.MarkReferredRewarded();
            }
            catch { }
        }

        await referralRepository.UpdateAsync(referral);

        return MapToReferralDto(referral);
    }

    public async Task<int> GetSuccessfulReferralCountAsync(Guid userId)
    {
        return await referralRepository.GetSuccessfulReferralCountAsync(userId);
    }

    public async Task<ReferralSummaryDto> GetReferralSummaryAsync(Guid userId)
    {
        var code = await GetOrCreateReferralCodeAsync(userId);
        var referrals = await GetReferralsByReferrerAsync(userId);
        var currentTier = await GetCurrentTierAsync(userId);
        var tiers = await GetRewardTiersAsync();
        var campaign = await GetActiveCampaignAsync();

        ReferralRewardTierDto? nextTier = null;
        var referralsToNext = 0;

        if (currentTier != null)
        {
            var nextTierEntity = tiers.FirstOrDefault(t => t.MinReferrals > currentTier.MinReferrals);
            if (nextTierEntity != null)
            {
                nextTier = nextTierEntity;
                referralsToNext = nextTierEntity.MinReferrals - code.SuccessfulReferrals;
            }
        }

        return new ReferralSummaryDto(
            code,
            currentTier ?? tiers.First(),
            nextTier,
            Math.Max(0, referralsToNext),
            referrals.Take(10),
            campaign
        );
    }

    public async Task<IEnumerable<ReferralLeaderboardEntryDto>> GetReferralLeaderboardAsync(int count = 10)
    {
        var topReferrers = await referralCodeRepository.GetTopReferrersAsync(count);
        var result = new List<ReferralLeaderboardEntryDto>();
        var rank = 1;

        foreach (var rc in topReferrers)
        {
            var user = await userRepository.GetByIdAsync(rc.UserId);
            result.Add(new ReferralLeaderboardEntryDto(
                rank++,
                rc.UserId,
                user?.Username,
                rc.SuccessfulReferrals,
                rc.TotalPointsEarned
            ));
        }

        return result;
    }

    public async Task<IEnumerable<ReferralRewardTierDto>> GetRewardTiersAsync()
    {
        var tiers = await rewardTierRepository.GetActiveAsync();
        return tiers.Select(MapToTierDto);
    }

    public async Task<ReferralRewardTierDto?> GetCurrentTierAsync(Guid userId)
    {
        var code = await referralCodeRepository.GetByUserIdAsync(userId);
        var count = code?.SuccessfulReferrals ?? 0;
        var tier = await rewardTierRepository.GetTierForReferralCountAsync(count);
        return tier != null ? MapToTierDto(tier) : null;
    }

    public async Task<ReferralCampaignDto?> GetActiveCampaignAsync()
    {
        var campaign = await campaignRepository.GetCurrentlyActiveAsync();
        return campaign != null ? MapToCampaignDto(campaign) : null;
    }

    public async Task<IEnumerable<ReferralCampaignDto>> GetAllCampaignsAsync()
    {
        var campaigns = await campaignRepository.GetAllAsync();
        return campaigns.Select(MapToCampaignDto);
    }

    public async Task<ReferralCampaignDto> CreateCampaignAsync(CreateReferralCampaignDto dto)
    {
        var campaign = new ReferralCampaign(
            dto.Name,
            dto.StartsAt,
            dto.EndsAt,
            dto.Description,
            dto.BonusReferrerPoints,
            dto.BonusReferredPoints,
            dto.Multiplier,
            dto.MaxReferralsPerUser
        );
        await campaignRepository.AddAsync(campaign);
        return MapToCampaignDto(campaign);
    }

    public async Task<ReferralDto> SendReferralInviteAsync(SendReferralInviteDto dto)
    {
        var code = await GetOrCreateReferralCodeAsync(dto.ReferrerUserId);
        var codeEntity = await referralCodeRepository.GetByUserIdAsync(dto.ReferrerUserId);

        var referral = new Referral(
            dto.ReferrerUserId,
            codeEntity!.Id,
            code.ActiveCode,
            dto.Email,
            dto.Phone
        );

        await referralRepository.AddAsync(referral);

        // TODO: Send actual invite via email/SMS
        // In production, integrate with email/SMS service

        return MapToReferralDto(referral);
    }

    public async Task ProcessExpiredReferralsAsync()
    {
        var expired = await referralRepository.GetExpiredPendingAsync();
        foreach (var referral in expired)
        {
            referral.MarkAsExpired();
            await referralRepository.UpdateAsync(referral);

            // Cancel pending referral in code stats
            var code = await referralCodeRepository.GetByIdAsync(referral.ReferralCodeId);
            if (code != null)
            {
                code.CancelReferral();
                await referralCodeRepository.UpdateAsync(code);
            }
        }
    }

    private static UserReferralCodeDto MapToCodeDto(UserReferralCode c) => new(
        c.Id,
        c.UserId,
        c.ReferralCode,
        c.CustomCode,
        c.ActiveCode,
        c.IsActive,
        c.TotalReferrals,
        c.SuccessfulReferrals,
        c.PendingReferrals,
        c.TotalPointsEarned,
        c.CreatedAt
    );

    private static ReferralDto MapToReferralDto(Referral r) => new(
        r.Id,
        r.ReferrerUserId,
        r.ReferredUserId,
        r.ReferralCode,
        r.Status,
        r.ReferredEmail,
        r.ReferredPhone,
        r.ReferrerRewardPoints,
        r.ReferredRewardPoints,
        r.ReferrerRewarded,
        r.ReferredRewarded,
        r.ExpiresAt,
        r.CompletedAt,
        r.CreatedAt
    );

    private static ReferralRewardTierDto MapToTierDto(ReferralRewardTier t) => new(
        t.Id,
        t.TierName,
        t.MinReferrals,
        t.MaxReferrals,
        t.ReferrerPoints,
        t.ReferredPoints,
        t.BonusMultiplier,
        t.IsActive
    );

    private static ReferralCampaignDto MapToCampaignDto(ReferralCampaign c) => new(
        c.Id,
        c.Name,
        c.Description,
        c.BonusReferrerPoints,
        c.BonusReferredPoints,
        c.Multiplier,
        c.StartsAt,
        c.EndsAt,
        c.MaxReferralsPerUser,
        c.IsActive,
        c.IsCurrentlyActive()
    );
}
