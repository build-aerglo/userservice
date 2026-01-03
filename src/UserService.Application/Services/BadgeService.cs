using UserService.Application.DTOs.Badge;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Domain.Repositories;

namespace UserService.Application.Services;

public class BadgeService(
    IBadgeDefinitionRepository badgeDefinitionRepository,
    IUserBadgeRepository userBadgeRepository,
    IUserBadgeLevelRepository userBadgeLevelRepository,
    IUserPointsRepository userPointsRepository
) : IBadgeService
{
    public async Task<IEnumerable<BadgeDefinitionDto>> GetAllBadgesAsync()
    {
        var badges = await badgeDefinitionRepository.GetAllAsync();
        return badges.Select(MapToDto);
    }

    public async Task<IEnumerable<BadgeDefinitionDto>> GetActiveBadgesAsync()
    {
        var badges = await badgeDefinitionRepository.GetActiveAsync();
        return badges.Select(MapToDto);
    }

    public async Task<BadgeDefinitionDto?> GetBadgeByIdAsync(Guid id)
    {
        var badge = await badgeDefinitionRepository.GetByIdAsync(id);
        return badge != null ? MapToDto(badge) : null;
    }

    public async Task<BadgeDefinitionDto?> GetBadgeByNameAsync(string name)
    {
        var badge = await badgeDefinitionRepository.GetByNameAsync(name);
        return badge != null ? MapToDto(badge) : null;
    }

    public async Task<IEnumerable<BadgeDefinitionDto>> GetBadgesByCategoryAsync(string category)
    {
        var badges = await badgeDefinitionRepository.GetByCategoryAsync(category);
        return badges.Select(MapToDto);
    }

    public async Task<IEnumerable<UserBadgeDto>> GetUserBadgesAsync(Guid userId)
    {
        var userBadges = await userBadgeRepository.GetByUserIdAsync(userId);
        var result = new List<UserBadgeDto>();

        foreach (var ub in userBadges)
        {
            var badge = await badgeDefinitionRepository.GetByIdAsync(ub.BadgeId);
            if (badge != null)
            {
                result.Add(MapToUserBadgeDto(ub, badge));
            }
        }

        return result;
    }

    public async Task<UserBadgeLevelDto> GetUserBadgeLevelAsync(Guid userId)
    {
        var level = await userBadgeLevelRepository.GetByUserIdAsync(userId);
        if (level == null)
        {
            level = new UserBadgeLevel(userId);
            await userBadgeLevelRepository.AddAsync(level);
        }

        return MapToLevelDto(level);
    }

    public async Task<UserBadgeSummaryDto> GetUserBadgeSummaryAsync(Guid userId)
    {
        var level = await GetUserBadgeLevelAsync(userId);
        var badges = await GetUserBadgesAsync(userId);
        var earnedBadgeIds = badges.Select(b => b.BadgeId).ToHashSet();

        var availableBadges = await badgeDefinitionRepository.GetActiveAsync();
        var unearnedBadges = availableBadges.Where(b => !earnedBadgeIds.Contains(b.Id)).Select(MapToDto);

        return new UserBadgeSummaryDto(
            UserId: userId,
            CurrentLevel: level.CurrentLevel,
            LevelProgress: level.LevelProgress,
            TotalBadgesEarned: level.TotalBadgesEarned,
            RecentBadges: badges.Take(5),
            AvailableBadges: unearnedBadges
        );
    }

    public async Task<bool> HasBadgeAsync(Guid userId, string badgeName)
    {
        var badge = await badgeDefinitionRepository.GetByNameAsync(badgeName);
        if (badge == null) return false;
        return await userBadgeRepository.HasBadgeAsync(userId, badge.Id);
    }

    public async Task<UserBadgeDto> AwardBadgeAsync(Guid userId, string badgeName, string? source = null)
    {
        var badge = await badgeDefinitionRepository.GetByNameAsync(badgeName);
        if (badge == null)
            throw new BadgeNotFoundException(badgeName);

        var hasBadge = await userBadgeRepository.HasBadgeAsync(userId, badge.Id);
        if (hasBadge)
            throw new BadgeAlreadyEarnedException(userId, badgeName);

        var userBadge = new UserBadge(userId, badge.Id, source);
        await userBadgeRepository.AddAsync(userBadge);

        // Update badge level
        var level = await userBadgeLevelRepository.GetByUserIdAsync(userId);
        if (level == null)
        {
            level = new UserBadgeLevel(userId);
            await userBadgeLevelRepository.AddAsync(level);
        }
        level.IncrementBadgeCount();
        await userBadgeLevelRepository.UpdateAsync(level);

        return MapToUserBadgeDto(userBadge, badge);
    }

    public async Task<IEnumerable<UserBadgeDto>> CheckAndAwardEligibleBadgesAsync(Guid userId)
    {
        var awarded = new List<UserBadgeDto>();
        var userPoints = await userPointsRepository.GetByUserIdAsync(userId);
        var totalPoints = userPoints?.LifetimePoints ?? 0;

        // Get badges user is eligible for based on points
        var eligibleBadges = await badgeDefinitionRepository.GetByPointsRequiredAsync(totalPoints);

        foreach (var badge in eligibleBadges)
        {
            var hasBadge = await userBadgeRepository.HasBadgeAsync(userId, badge.Id);
            if (!hasBadge)
            {
                try
                {
                    var dto = await AwardBadgeAsync(userId, badge.Name, "auto_points");
                    awarded.Add(dto);
                }
                catch (BadgeAlreadyEarnedException)
                {
                    // Already has badge, skip
                }
            }
        }

        return awarded;
    }

    public async Task<BadgeDefinitionDto> CreateBadgeAsync(CreateBadgeDefinitionDto dto)
    {
        var badge = new BadgeDefinition(
            dto.Name,
            dto.DisplayName,
            dto.Description,
            dto.IconUrl,
            dto.Tier,
            dto.PointsRequired,
            dto.Category
        );

        await badgeDefinitionRepository.AddAsync(badge);
        return MapToDto(badge);
    }

    public async Task<BadgeDefinitionDto> UpdateBadgeAsync(Guid id, UpdateBadgeDefinitionDto dto)
    {
        var badge = await badgeDefinitionRepository.GetByIdAsync(id);
        if (badge == null)
            throw new BadgeNotFoundException(id);

        badge.Update(dto.DisplayName, dto.Description, dto.IconUrl, dto.Tier, dto.PointsRequired);
        await badgeDefinitionRepository.UpdateAsync(badge);
        return MapToDto(badge);
    }

    public async Task ActivateBadgeAsync(Guid id)
    {
        var badge = await badgeDefinitionRepository.GetByIdAsync(id);
        if (badge == null)
            throw new BadgeNotFoundException(id);

        badge.Activate();
        await badgeDefinitionRepository.UpdateAsync(badge);
    }

    public async Task DeactivateBadgeAsync(Guid id)
    {
        var badge = await badgeDefinitionRepository.GetByIdAsync(id);
        if (badge == null)
            throw new BadgeNotFoundException(id);

        badge.Deactivate();
        await badgeDefinitionRepository.UpdateAsync(badge);
    }

    private static BadgeDefinitionDto MapToDto(BadgeDefinition badge) => new(
        badge.Id,
        badge.Name,
        badge.DisplayName,
        badge.Description,
        badge.IconUrl,
        badge.Tier,
        badge.PointsRequired,
        badge.Category,
        badge.IsActive
    );

    private static UserBadgeDto MapToUserBadgeDto(UserBadge ub, BadgeDefinition badge) => new(
        ub.Id,
        ub.UserId,
        ub.BadgeId,
        badge.Name,
        badge.DisplayName,
        badge.Description,
        badge.IconUrl,
        badge.Tier,
        badge.Category,
        ub.EarnedAt,
        ub.Source
    );

    private static UserBadgeLevelDto MapToLevelDto(UserBadgeLevel level)
    {
        var (nextLevel, badgesToNext) = level.CurrentLevel switch
        {
            "Pioneer" => ("Explorer", 1 - level.TotalBadgesEarned),
            "Explorer" => ("Expert", 5 - level.TotalBadgesEarned),
            "Expert" => ("Pro", 15 - level.TotalBadgesEarned),
            "Pro" => ("Master", 30 - level.TotalBadgesEarned),
            "Master" => ("Legend", 50 - level.TotalBadgesEarned),
            _ => ("Max", 0)
        };

        return new UserBadgeLevelDto(
            level.UserId,
            level.CurrentLevel,
            level.LevelProgress,
            level.TotalBadgesEarned,
            nextLevel,
            Math.Max(0, badgesToNext)
        );
    }
}
