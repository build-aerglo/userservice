using UserService.Application.DTOs.Badge;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Domain.Repositories;

namespace UserService.Application.Services;

public class BadgeService(
    IUserBadgeRepository badgeRepository,
    IUserRepository userRepository
) : IBadgeService
{
    // Platform launch date for Pioneer badge calculation
    private static readonly DateTime PlatformLaunchDate = new(2026, 1, 21, 0, 0, 0, DateTimeKind.Utc);
    private const int PioneerWindowDays = 100;

    public async Task<UserBadgesResponseDto> GetUserBadgesAsync(Guid userId)
    {   
        var user = await userRepository.GetByIdAsync(userId);
        if (user is null)
            throw new EndUserNotFoundException(userId);

        var badges = await badgeRepository.GetActiveByUserIdAsync(userId);

        var currentTier = GetCurrentTier(badges);
        
        var badgeDtos = badges
            .Where(b => !IsTierBadge(b.BadgeType))
            .Select(MapToDto)
            .ToList();

        return new UserBadgesResponseDto(
            UserId: userId,
            Badges: badgeDtos,
            TotalBadges: badgeDtos.Count,
            CurrentTier: currentTier
        );
    }

    public async Task<UserBadgeDto?> GetBadgeByIdAsync(Guid badgeId)
    {
        var badge = await badgeRepository.GetByIdAsync(badgeId);
        return badge is null ? null : MapToDto(badge);
    }

    public async Task<UserBadgeDto> AssignBadgeAsync(AssignBadgeDto dto)
    {
        // Validate user exists
        var user = await userRepository.GetByIdAsync(dto.UserId);
        if (user is null)
            throw new EndUserNotFoundException(dto.UserId);

        // Validate badge type
        if (!IsValidBadgeType(dto.BadgeType))
            throw new InvalidBadgeTypeException(dto.BadgeType);

        // Check if user already has this badge
        var existingBadge = await badgeRepository.GetByUserIdAndTypeAsync(
            dto.UserId, dto.BadgeType, dto.Location, dto.Category);

        if (existingBadge is not null && existingBadge.IsActive)
            throw new BadgeAlreadyExistsException(dto.UserId, dto.BadgeType);

        // If badge exists but is inactive, reactivate it
        if (existingBadge is not null)
        {
            existingBadge.Reactivate();
            await badgeRepository.UpdateAsync(existingBadge);
            return MapToDto(existingBadge);
        }

        // For tier badges, deactivate existing tier badges first
        if (IsTierBadge(dto.BadgeType))
        {
            await badgeRepository.DeactivateAllTierBadgesAsync(dto.UserId);
        }

        // Create new badge
        var badge = new UserBadge(
            userId: dto.UserId,
            badgeType: dto.BadgeType,
            location: dto.Location,
            category: dto.Category
        );

        await badgeRepository.AddAsync(badge);

        var savedBadge = await badgeRepository.GetByIdAsync(badge.Id);
        if (savedBadge is null)
            throw new BadgeAssignmentFailedException("Failed to save badge.");

        return MapToDto(savedBadge);
    }

    public async Task<bool> RevokeBadgeAsync(RevokeBadgeDto dto)
    {
        var badge = await badgeRepository.GetByUserIdAndTypeAsync(
            dto.UserId, dto.BadgeType, dto.Location, dto.Category);

        if (badge is null)
            return false;

        badge.Deactivate();
        await badgeRepository.UpdateAsync(badge);
        return true;
    }

    public async Task<UserBadgeDto?> CalculateTierBadgeAsync(Guid userId, int reviewCount, int daysSinceJoin)
    {
        // Determine appropriate tier
        string tierBadge;

        if (daysSinceJoin >= 250 || reviewCount >= 50)
        {
            tierBadge = BadgeTypes.Pro;
        }
        else if (daysSinceJoin >= 100 || reviewCount >= 25)
        {
            tierBadge = BadgeTypes.Expert;
        }
        else
        {
            tierBadge = BadgeTypes.Newbie;
        }

        // Check if user already has this tier badge
        var existingBadge = await badgeRepository.GetByUserIdAndTypeAsync(userId, tierBadge);
        if (existingBadge is not null && existingBadge.IsActive)
        {
            return MapToDto(existingBadge);
        }

        // Assign new tier badge (this will deactivate existing tier badges)
        return await AssignBadgeAsync(new AssignBadgeDto(userId, tierBadge));
    }

    public async Task<bool> CheckAndAssignPioneerBadgeAsync(Guid userId, DateTime joinDate)
    {
        var daysSinceLaunch = (joinDate - PlatformLaunchDate).Days;

        // User must have joined after launch and within the pioneer window
        if (daysSinceLaunch < 0 || daysSinceLaunch > PioneerWindowDays)
            return false;

        // Check if already has Pioneer badge
        var existingBadge = await badgeRepository.GetByUserIdAndTypeAsync(userId, BadgeTypes.Pioneer);
        if (existingBadge is not null)
            return existingBadge.IsActive;

        await AssignBadgeAsync(new AssignBadgeDto(userId, BadgeTypes.Pioneer));
        return true;
    }

    public async Task<bool> CheckAndAssignTopContributorBadgeAsync(Guid userId, string location, int userRankInLocation, int disputeCount)
    {
        // Top 5 in location with lowest disputes
        if (userRankInLocation > 5 || disputeCount > 0)
        {
            // Revoke if they no longer qualify
            await RevokeBadgeAsync(new RevokeBadgeDto(userId, BadgeTypes.TopContributor, location));
            return false;
        }

        // Check if already has this badge for this location
        var existingBadge = await badgeRepository.GetByUserIdAndTypeAsync(
            userId, BadgeTypes.TopContributor, location);

        if (existingBadge is not null && existingBadge.IsActive)
            return true;

        await AssignBadgeAsync(new AssignBadgeDto(userId, BadgeTypes.TopContributor, location));
        return true;
    }

    public async Task<bool> CheckAndAssignCategoryExpertBadgeAsync(Guid userId, string category, int reviewsInCategory, int helpfulVotes)
    {
        // 10+ reviews in category + high helpful votes
        if (reviewsInCategory < 10 || helpfulVotes < 5)
        {
            // Revoke if they no longer qualify
            await RevokeBadgeAsync(new RevokeBadgeDto(userId, BadgeTypes.ExpertCategory, null, category));
            return false;
        }

        var existingBadge = await badgeRepository.GetByUserIdAndTypeAsync(
            userId, BadgeTypes.ExpertCategory, null, category);

        if (existingBadge is not null && existingBadge.IsActive)
            return true;

        await AssignBadgeAsync(new AssignBadgeDto(userId, BadgeTypes.ExpertCategory, null, category));
        return true;
    }

    public async Task<bool> CheckAndAssignMostHelpfulBadgeAsync(Guid userId, int helpfulVoteRankPercentile)
    {
        // Top 10% by helpful votes
        if (helpfulVoteRankPercentile > 10)
        {
            // Revoke if they no longer qualify
            await RevokeBadgeAsync(new RevokeBadgeDto(userId, BadgeTypes.MostHelpful));
            return false;
        }

        var existingBadge = await badgeRepository.GetByUserIdAndTypeAsync(userId, BadgeTypes.MostHelpful);
        if (existingBadge is not null && existingBadge.IsActive)
            return true;

        await AssignBadgeAsync(new AssignBadgeDto(userId, BadgeTypes.MostHelpful));
        return true;
    }

    public async Task RecalculateAllBadgesAsync(Guid userId)
    {
        var user = await userRepository.GetByIdAsync(userId);
        if (user is null)
            return;

        // Calculate days since join and review count (for now, assume 0 reviews)
        var daysSinceJoin = (DateTime.UtcNow - user.JoinDate).Days;
        var reviewCount = 0; 

        // Calculate and assign tier badge
        await CalculateTierBadgeAsync(userId, reviewCount, daysSinceJoin);

        // Check for Pioneer badge
        await CheckAndAssignPioneerBadgeAsync(userId, user.JoinDate);

        // Other badges (Top Contributor, Category Expert, Most Helpful)
        // require data from other services and should be called with that data
    }

    public (string DisplayName, string Description, string Icon) GetBadgeInfo(string badgeType, string? location = null, string? category = null)
    {
        return badgeType switch
        {
            BadgeTypes.Pioneer => ("Pioneer", "Joined in the first 100 days of platform launch", "🏅"),
            BadgeTypes.TopContributor => ($"Top Contributor in {location ?? "Location"}", $"Top 5 reviewer in {location ?? "location"} with excellent track record", "🏆"),
            BadgeTypes.ExpertCategory => ($"Expert in {category ?? "Category"}", $"10+ quality reviews in {category ?? "category"}", "⭐"),
            BadgeTypes.MostHelpful => ("Most Helpful", "Top 10% by helpful votes", "👍"),
            BadgeTypes.Newbie => ("Newbie", "New member of the community", "🌱"),
            BadgeTypes.Expert => ("Expert", "Established community member", "📚"),
            BadgeTypes.Pro => ("Pro", "Veteran community member", "💎"),
            _ => ("Unknown Badge", "Unknown badge type", "❓")
        };
    }

    private UserBadgeDto MapToDto(UserBadge badge)
    {
        var (displayName, description, _) = GetBadgeInfo(badge.BadgeType, badge.Location, badge.Category);

        return new UserBadgeDto(
            Id: badge.Id,
            UserId: badge.UserId,
            BadgeType: badge.BadgeType,
            Location: badge.Location,
            Category: badge.Category,
            EarnedAt: badge.EarnedAt,
            IsActive: badge.IsActive,
            DisplayName: displayName,
            Description: description
        );
    }

    public string GetCurrentTier(IEnumerable<UserBadge> badges)
    {
        var tierBadge = badges.FirstOrDefault(b =>
            b.BadgeType == BadgeTypes.Pro ||
            b.BadgeType == BadgeTypes.Expert ||
            b.BadgeType == BadgeTypes.Newbie);

        return tierBadge?.BadgeType ?? BadgeTypes.Newbie;
    }

    private static bool IsValidBadgeType(string badgeType)
    {
        return badgeType is BadgeTypes.Pioneer or BadgeTypes.TopContributor
            or BadgeTypes.ExpertCategory or BadgeTypes.MostHelpful
            or BadgeTypes.Newbie or BadgeTypes.Expert or BadgeTypes.Pro;
    }

    public bool IsTierBadge(string badgeType)
    {
        return badgeType is BadgeTypes.Newbie or BadgeTypes.Expert or BadgeTypes.Pro;
    }
}