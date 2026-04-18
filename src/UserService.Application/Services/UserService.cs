using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using UserService.Application.DTOs;
using UserService.Application.DTOs.Badge;
using UserService.Application.DTOs.Referral;
using UserService.Application.Interfaces;
using UserService.Application.Services.Auth0;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Domain.Repositories;

namespace UserService.Application.Services;

public class UserService(
    IUserRepository userRepository,
    IBusinessRepRepository businessRepRepository,
    IBusinessServiceClient businessServiceClient,
    IBusinessClaimRepository businessClaimRepository,
    IBusinessRepository businessRepository,
    ISupportUserProfileRepository supportUserProfileRepository,
    IEndUserProfileRepository endUserProfileRepository,
    IUserSettingsRepository userSettingsRepository,
    IBadgeService badgeService,
    IPointsService pointsService,
    IReferralService referralService,
    IAuth0ManagementService _auth0,
    IConfiguration _config,
    IMemoryCache cache,
    IRegistrationVerificationService registrationVerificationService
) : IUserService
{
    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        return await userRepository.GetByIdAsync(userId);
    }

    // Sub business user services

    public async Task<SubBusinessUserResponseDto> CreateSubBusinessUserAsync(CreateSubBusinessUserDto dto)
    {
        var businessExists = await businessServiceClient.BusinessExistsAsync(dto.BusinessId);
        if (!businessExists)
            throw new BusinessNotFoundException(dto.BusinessId);

        if (await userRepository.EmailExistsAsync(dto.Email))
            throw new DuplicateUserEmailException($"Email '{dto.Email}' already exists.");

        var auth0UserId = await _auth0.CreateUserAndAssignRoleAsync(dto.Email, dto.Username, dto.Password, _config["Auth0:Roles:BusinessUser"]);

        var user = new User(
            username: dto.Username,
            email: dto.Email,
            phone: dto.Phone,
            password: dto.Password,
            userType: "business_user",
            address: dto.Address,
            auth0UserId
        );
        await userRepository.AddAsync(user);

        var savedUser = await userRepository.GetByIdAsync(user.Id);
        if (savedUser is null)
            throw new UserCreationFailedException("Failed to create user record.");

        var businessRep = new BusinessRep(
            businessId: dto.BusinessId,
            userId: user.Id,
            branchName: dto.BranchName,
            branchAddress: dto.BranchAddress
        );
        await businessRepRepository.AddAsync(businessRep);

        var savedBusinessRep = await businessRepRepository.GetByIdAsync(businessRep.Id);
        if (savedBusinessRep is null)
            throw new UserCreationFailedException("Failed to create business representative relationship.");

        return new SubBusinessUserResponseDto(
            UserId: user.Id,
            BusinessRepId: businessRep.Id,
            BusinessId: businessRep.BusinessId,
            Username: user.Username,
            Email: user.Email,
            Phone: user.Phone,
            Address: user.Address,
            BranchName: businessRep.BranchName,
            BranchAddress: businessRep.BranchAddress,
            Auth0UserId: auth0UserId,
            CreatedAt: user.CreatedAt
        );
    }

    public async Task<SubBusinessUserResponseDto> UpdateSubBusinessUserAsync(Guid userId, UpdateSubBusinessUserDto dto)
    {
        var user = await userRepository.GetByIdAsync(userId);
        if (user is null)
            throw new SubBusinessUserNotFoundException(userId);

        var businessRep = await businessRepRepository.GetByUserIdAsync(userId);
        if (businessRep is null)
            throw new SubBusinessUserNotFoundException(userId);

        user.Update(dto.Email, dto.Phone, dto.Address);
        await userRepository.UpdateAsync(user);

        var updatedUser = await userRepository.GetByIdAsync(userId);
        if (updatedUser is null)
            throw new SubBusinessUserUpdateFailedException("Failed to update user record.");

        businessRep.UpdateBranch(dto.BranchName, dto.BranchAddress);
        await businessRepRepository.UpdateAsync(businessRep);

        var updatedBusinessRep = await businessRepRepository.GetByIdAsync(businessRep.Id);
        if (updatedBusinessRep is null)
            throw new SubBusinessUserUpdateFailedException("Failed to update business representative record.");

        return new SubBusinessUserResponseDto(
            UserId: updatedUser.Id,
            BusinessRepId: updatedBusinessRep.Id,
            BusinessId: updatedBusinessRep.BusinessId,
            Username: updatedUser.Username,
            Email: updatedUser.Email,
            Phone: updatedUser.Phone,
            Address: updatedUser.Address,
            BranchName: updatedBusinessRep.BranchName,
            BranchAddress: updatedBusinessRep.BranchAddress,
            Auth0UserId: string.Empty,
            CreatedAt: updatedUser.CreatedAt
        );
    }

    // Support user services

    public async Task<SupportUserResponseDto> CreateSupportUserAsync(CreateSupportUserDto dto)
    {
        if (await userRepository.EmailExistsAsync(dto.Email))
            throw new DuplicateUserEmailException($"Email '{dto.Email}' already exists.");

        var auth0UserId = await _auth0.CreateUserAndAssignRoleAsync(dto.Email, dto.Username, dto.Password, _config["Auth0:Roles:SupportUser"]);

        var user = new User(
            username: dto.Username,
            email: dto.Email,
            phone: dto.Phone,
            password: dto.Password,
            userType: "support_user",
            address: dto.Address,
            auth0UserId: auth0UserId
        );
        await userRepository.AddAsync(user);

        var savedUser = await userRepository.GetByIdAsync(user.Id);
        if (savedUser is null)
            throw new UserCreationFailedException("Failed to create user record.");

        var supportUserProfile = new SupportUserProfile(userId: user.Id);
        await supportUserProfileRepository.AddAsync(supportUserProfile);

        var savedSupportProfile = await supportUserProfileRepository.GetByIdAsync(supportUserProfile.Id);
        if (savedSupportProfile is null)
            throw new UserCreationFailedException("Failed to create support user profile.");

        return new SupportUserResponseDto(
            UserId: user.Id,
            SupportUserProfileId: supportUserProfile.Id,
            Username: user.Username,
            Email: user.Email,
            Phone: user.Phone,
            Address: user.Address,
            Auth0UserId: auth0UserId,
            CreatedAt: user.CreatedAt
        );
    }

    public async Task<SupportUserResponseDto> UpdateSupportUserAsync(Guid userId, UpdateSupportUserDto dto)
    {
        var user = await userRepository.GetByIdAsync(userId);
        if (user is null)
            throw new SupportUserNotFoundException(userId);

        if (user.UserType != "support_user")
            throw new SupportUserUpdateFailedException($"User with ID {userId} is not a support user.");

        var supportProfile = await supportUserProfileRepository.GetByUserIdAsync(userId);
        if (supportProfile is null)
            throw new SupportUserNotFoundException(userId);

        user.Update(dto.Email, dto.Phone, dto.Address);
        await userRepository.UpdateAsync(user);

        supportProfile.UpdateTimestamp();
        await supportUserProfileRepository.UpdateAsync(supportProfile);

        var updatedUser = await userRepository.GetByIdAsync(userId);
        if (updatedUser is null)
            throw new SupportUserUpdateFailedException("Failed to update user record.");

        return new SupportUserResponseDto(
            UserId: updatedUser.Id,
            SupportUserProfileId: supportProfile.Id,
            Username: updatedUser.Username,
            Email: updatedUser.Email,
            Phone: updatedUser.Phone,
            Address: updatedUser.Address,
            Auth0UserId: updatedUser.Auth0UserId,
            CreatedAt: updatedUser.CreatedAt
        );
    }

    // Business user services

    public async Task<(User, Guid businessId, BusinessRep)> RegisterBusinessAccountAsync(BusinessUserDto userPayload)
    {
        if (await userRepository.EmailExistsAsync(userPayload.Email))
            throw new DuplicateUserEmailException($"Email '{userPayload.Email}' already exists.");

        var businessId = await businessServiceClient.CreateBusinessAsync(userPayload);
        if (businessId == null || businessId == Guid.Empty)
            throw new BusinessUserCreationFailedException("Business creation failed: BusinessId is missing from services.");

        var auth0UserId = await _auth0.CreateUserAndAssignRoleAsync(userPayload.Email, userPayload.Name, userPayload.Password, _config["Auth0:Roles:BusinessUser"]);

        var user = new User(userPayload.Name, userPayload.Email, userPayload.Phone, userPayload.Password, userPayload.UserType, userPayload.Address, auth0UserId);
        await userRepository.AddAsync(user);

        var savedUser = await userRepository.GetByIdAsync(user.Id);
        if (savedUser == null)
            throw new UserCreationFailedException("Failed to create user record.");

        await userRepository.SetUserIdAsync(savedUser.Id, businessId.Value);

        var businessRep = new BusinessRep(businessId.Value, savedUser.Id, userPayload.BranchName, userPayload.BranchAddress);
        await businessRepRepository.AddAsync(businessRep);

        var savedBusiness = await GetBusinessRepByIdAsync(businessRep.Id);
        if (savedBusiness == null)
            throw new BusinessUserCreationFailedException("Failed to create business record.");

        // Send registration verification email (non-blocking)
        await registrationVerificationService.SendVerificationEmailAsync(user.Email, user.Username, "business_user");

        return (user, businessId.Value, businessRep);
    }

    public async Task<BusinessRep?> GetBusinessRepByIdAsync(Guid id)
        => await businessRepRepository.GetByIdAsync(id);

    public async Task<RegisterBusinessResultDto> RegisterBusinessAfterClaimAsync(RegisterBusinessDto dto)
    {
        // 1. Validate business claim
        var claim = await businessClaimRepository.GetByBusinessIdAsync(dto.BusinessId);
        if (claim is null)
            throw new BusinessNotFoundException(dto.BusinessId);

        if (claim.Status != 7)
            throw new BusinessClaimNotApprovedException(dto.BusinessId);

        if (DateTime.UtcNow > claim.ExpiresAt)
            throw new BusinessClaimExpiredException(dto.BusinessId);

        // 2. Fetch business name from business table
        var businessName = await businessRepository.GetNameByIdAsync(dto.BusinessId);
        if (businessName is null)
            throw new BusinessNotFoundException(dto.BusinessId);

        // 2b. Ensure email is not already registered
        if (await userRepository.EmailExistsAsync(dto.Email))
            throw new DuplicateUserEmailException($"Email '{dto.Email}' already exists.");

        // 3. Create Auth0 user with business_user role
        var auth0UserId = await _auth0.CreateUserAndAssignRoleAsync(
            dto.Email, businessName, dto.Password, _config["Auth0:Roles:BusinessUser"]);

        // 4. Create local user record (username = business name)
        var user = new User(
            username: businessName,
            email: dto.Email,
            phone: dto.PhoneNumber ?? "",
            password: dto.Password,
            userType: "business_user",
            address: null,
            auth0UserId: auth0UserId);

        await userRepository.AddAsync(user);

        var savedUser = await userRepository.GetByIdAsync(user.Id);
        if (savedUser is null)
            throw new UserCreationFailedException("Failed to create user record.");

        // 5. Link user to the claimed business and update owner details + status
        await userRepository.SetUserIdAsync(savedUser.Id, dto.BusinessId);
        await businessRepository.UpdateOwnerAsync(dto.BusinessId, savedUser.Id, dto.Email, dto.PhoneNumber);
        await businessRepository.UpdateStatusAsync(dto.BusinessId, "claimed");

        // 6. Create BusinessRep record
        var businessRep = new BusinessRep(dto.BusinessId, savedUser.Id);
        await businessRepRepository.AddAsync(businessRep);

        // 7. Send registration verification email
        await registrationVerificationService.SendVerificationEmailAsync(savedUser.Email, businessName, "business_user");

        return new RegisterBusinessResultDto(
            UserId: savedUser.Id,
            BusinessId: dto.BusinessId,
            Username: businessName,
            Email: savedUser.Email,
            Phone: dto.PhoneNumber,
            Auth0UserId: auth0UserId,
            CreatedAt: savedUser.CreatedAt);
    }


    public async Task<EndUserResponseDto> CreateEndUserAsync(CreateEndUserDto dto)
    {
        if (await userRepository.EmailExistsAsync(dto.Email))
            throw new DuplicateUserEmailException($"Email '{dto.Email}' already exists.");

        var auth0UserId = await _auth0.CreateUserAndAssignRoleAsync(dto.Email, dto.Username, dto.Password, _config["Auth0:Roles:EndUser"]);

        var user = new User(
            username: dto.Username,
            email: dto.Email,
            phone: dto.Phone,
            password: dto.Password,
            userType: "end_user",
            address: dto.Address,
            auth0UserId
        );
        await userRepository.AddAsync(user);

        var savedUser = await userRepository.GetByIdAsync(user.Id);
        if (savedUser is null)
            throw new UserCreationFailedException("Failed to create user record.");

        await badgeService.CheckAndAssignPioneerBadgeAsync(user.Id, user.JoinDate);

        var endUserProfile = new EndUserProfile(
            userId: user.Id,
            socialMedia: dto.SocialMedia
        );
        await endUserProfileRepository.AddAsync(endUserProfile);

        var savedProfile = await endUserProfileRepository.GetByIdAsync(endUserProfile.Id);
        if (savedProfile is null)
            throw new UserCreationFailedException("Failed to create end user profile.");

        // ✅ 8. Send registration verification email (non-blocking)
        await registrationVerificationService.SendVerificationEmailAsync(user.Email, user.Username, "end_user");

        // ✅ 7. Map to response DTO
        return new EndUserResponseDto(
            UserId: user.Id,
            EndUserProfileId: endUserProfile.Id,
            Username: user.Username,
            Email: user.Email,
            Phone: user.Phone,
            Address: user.Address,
            SocialMedia: endUserProfile.SocialMedia,
            Auth0UserId: auth0UserId,
            CreatedAt: user.CreatedAt
        );
    }

    public async Task<EndUserProfileDetailDto> GetEndUserProfileDetailAsync(Guid userId)
    {
        var user = await userRepository.GetByIdAsync(userId);
        if (user is null)
            throw new EndUserNotFoundException(userId);

        if (user.UserType != "end_user")
            throw new EndUserNotFoundException(userId);

        var profile = await endUserProfileRepository.GetByUserIdAsync(userId);
        if (profile is null)
            throw new EndUserNotFoundException(userId);

        var settings = await userSettingsRepository.GetByUserIdAsync(userId);
        if (settings is null)
        {
            settings = new UserSettings(userId);
            await userSettingsRepository.AddAsync(settings);
        }

        var notificationPrefs = settings.GetNotificationPreferences();

        return new EndUserProfileDetailDto(
            UserId: user.Id,
            Username: user.Username,
            Email: user.Email,
            IsEmailVerified: user.IsEmailVerified,
            Phone: user.Phone,
            Address: user.Address,
            JoinDate: user.JoinDate,
            EndUserProfileId: profile.Id,
            SocialMedia: profile.SocialMedia,
            NotificationPreferences: new NotificationPreferencesDto(
                EmailNotifications: notificationPrefs.EmailNotifications,
                SmsNotifications: notificationPrefs.SmsNotifications,
                PushNotifications: notificationPrefs.PushNotifications,
                MarketingEmails: notificationPrefs.MarketingEmails
            ),
            DarkMode: settings.DarkMode,
            CreatedAt: user.CreatedAt,
            UpdatedAt: settings.UpdatedAt
        );
    }

    public async Task<EndUserProfileDetailDto> UpdateEndUserProfileAsync(Guid userId, UpdateEndUserProfileDto dto)
    {
        var user = await userRepository.GetByIdAsync(userId);
        if (user is null)
            throw new EndUserNotFoundException(userId);

        if (user.UserType != "end_user")
            throw new EndUserNotFoundException(userId);

        var profile = await endUserProfileRepository.GetByUserIdAsync(userId);
        if (profile is null)
            throw new EndUserNotFoundException(userId);

        var settings = await userSettingsRepository.GetByUserIdAsync(userId);
        if (settings is null)
        {
            settings = new UserSettings(userId);
            await userSettingsRepository.AddAsync(settings);
        }

        if (!string.IsNullOrWhiteSpace(dto.Username) ||
            !string.IsNullOrWhiteSpace(dto.Phone) ||
            dto.Address != null)
        {
            user.Update(email: null, phone: dto.Phone, address: dto.Address);
            await userRepository.UpdateAsync(user);
        }

        if (dto.SocialMedia != null)
        {
            profile.UpdateSocialMedia(dto.SocialMedia);
            await endUserProfileRepository.UpdateAsync(profile);
        }

        if (dto.NotificationPreferences != null || dto.DarkMode.HasValue)
        {
            NotificationPreferencesModel? notifPrefs = null;
            if (dto.NotificationPreferences != null)
            {
                notifPrefs = new NotificationPreferencesModel
                {
                    EmailNotifications = dto.NotificationPreferences.EmailNotifications,
                    SmsNotifications = dto.NotificationPreferences.SmsNotifications,
                    PushNotifications = dto.NotificationPreferences.PushNotifications,
                    MarketingEmails = dto.NotificationPreferences.MarketingEmails
                };
            }
            settings.UpdateSettings(darkMode: dto.DarkMode, notificationPrefs: notifPrefs);
            await userSettingsRepository.UpdateAsync(settings);
        }

        return await GetEndUserProfileDetailAsync(userId);
    }

    public async Task<EndUserSummaryDto> GetEndUserSummaryAsync(Guid userId, int page = 1, int pageSize = 5, bool recalculate = true)
    {
        return await BuildEndUserSummaryInternalAsync(userId, page, pageSize, recalculate);
    }

    public async Task<EndUserSummaryDto> BuildEndUserSummaryInternalAsync(Guid userId, int page, int pageSize, bool recalculate = true)
    {
        // Step 1: fetch user first — needed for email (used by GetUserDataAsync)
        // and for the type guard. This is the only mandatory sequential step.
        var user = await userRepository.GetByIdAsync(userId);
        if (user is null)
            throw new EndUserNotFoundException(userId);

        // Step 2: fire everything that can run concurrently in one batch.
        //
        // Previously this was three sequential phases:
        //   Phase1 (profileDetail + getUserData) → Phase2 (points x3) → Phase3 (referral x3)
        //
        // Phases 2 and 3 only need userId, which is already known, so there is no
        // reason to wait for Phase 1 before starting them. Collapsing all into one
        // WhenAll cuts the critical path from (Phase1 + Phase2 + Phase3) to
        // max(all tasks) — roughly a 2-3x reduction in wall-clock time.
        var profileDetailTask        = GetEndUserProfileDetailAsync(userId);
        var entityTask               = endUserProfileRepository.GetUserDataAsync(userId, user.Email, page, pageSize);
        var pointsTask               = pointsService.GetUserPointsAsync(userId);
        var redemptionHistoryTask    = pointsService.GetRedemptionHistoryAsync(userId, limit: 3, offset: 0);
        var pointsBreakdownTask      = pointsService.GetPointsBreakdownAsync(userId);
        var referralStatsTask        = referralService.GetReferralStatsAsync(userId);
        var referralCodeTask         = referralService.GetUserReferralCodeAsync(userId);
        var referredByTask           = referralService.GetReferredByAsync(userId);

        await Task.WhenAll(
            profileDetailTask,
            entityTask,
            pointsTask,
            redemptionHistoryTask,
            pointsBreakdownTask,
            referralStatsTask,
            referralCodeTask,
            referredByTask
        );

        var profileDetail     = await profileDetailTask;
        var entity            = await entityTask;
        var pointsData        = await pointsTask;
        var redemptionHistory = await redemptionHistoryTask;
        var pointsBreakdown   = await pointsBreakdownTask;
        var referralStats     = await referralStatsTask;
        var referralCode      = await referralCodeTask;
        var referredBy        = await referredByTask;

        // Step 3: badge recalc — depends on entity.TotalReviewCount from step 2.
        // Kept sequential here because it writes to the DB and the enrichment
        // below reads the updated badges immediately after.
        if (recalculate)
        {
            await badgeService.RecalculateAllBadgesAsync(userId, entity.TotalReviewCount);
        }

        // Step 4: referral code generation — rare path, only runs when user has none.
        if (referralCode is null)
        {
            referralCode = await referralService.GenerateReferralCodeAsync(new GenerateReferralCodeDto(userId));
        }

        // Step 5: badge enrichment (sync, no DB calls).
        var tierBadge = entity.Badges.FirstOrDefault(b => badgeService.IsTierBadge(b.BadgeType));
        var achievementBadges = entity.Badges
            .Where(b => !badgeService.IsTierBadge(b.BadgeType))
            .ToList();

        foreach (var badge in achievementBadges)
        {
            var info = badgeService.GetBadgeInfo(badge.BadgeType, badge.Location, badge.Category);
            badge.Icon = info.Icon;
            badge.Description = info.Description;
        }

        if (tierBadge != null)
        {
            var info = badgeService.GetBadgeInfo(tierBadge.BadgeType);
            tierBadge.Icon = info.Icon;
            tierBadge.Description = info.Description;
        }

        // Step 6: build response.
        var recentRedemptions = redemptionHistory.Redemptions
            .Select(r => new RedemptionSummaryDto
            {
                PointsRedeemed = (int)r.PointsRedeemed,
                AmountInNaira  = r.AmountInNaira,
                PhoneNumber    = r.PhoneNumber,
                Status         = r.Status,
                CreatedAt      = r.CreatedAt
            })
            .ToList();

        var totalPointsRedeemed = redemptionHistory.Redemptions
            .Where(r => r.Status == "Completed")
            .Sum(r => (int)r.PointsRedeemed);

        return new EndUserSummaryDto
        {
            UserId = entity.UserId,
            Email  = entity.Email,
            Profile = profileDetail,
            Reviews = new PaginatedReviews
            {
                Items      = entity.Reviews,
                TotalCount = entity.TotalReviewCount,
                Page       = page,
                PageSize   = pageSize
            },
            TopCities      = entity.TopCities,
            TopCategories  = entity.TopCategories,
            TierBadge      = tierBadge,
            AchievementBadges = achievementBadges,

            Points        = entity.Points,
            Rank          = entity.Rank,
            Streak        = entity.Streak,
            LifetimePoints = entity.LifetimePoints,
            PointTier     = pointsData.Tier,
            LongestStreak = pointsData.LongestStreak,

            ReviewPoints   = pointsBreakdown.ReviewPoints,
            ReferralPoints = pointsBreakdown.ReferralPoints,
            StreakPoints   = pointsBreakdown.StreakPoints,
            BonusPoints    = pointsBreakdown.BonusPoints,
            OtherPoints    = pointsBreakdown.OtherPoints,

            RecentActivity = entity.RecentActivity,

            TotalPointsRedeemed        = totalPointsRedeemed,
            RemainingRedeemablePoints  = entity.Points,
            RecentRedemptions          = recentRedemptions,

            Referral = new UserReferralSummaryDto
            {
                Code                = referralCode?.Code,
                TotalReferrals      = referralStats.TotalReferrals,
                SuccessfulReferrals = referralStats.SuccessfulReferrals,
                PendingReferrals    = referralStats.PendingReferrals,
                TotalPointsEarned   = referralStats.TotalPointsEarned,
                WasReferred         = referredBy is not null,
                ReferredByUsername  = referredBy?.ReferrerUsername
            }
        };
    }
}