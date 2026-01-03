using UserService.Application.DTOs.Badge;

namespace UserService.Application.Interfaces;

public interface IBadgeService
{
    // Badge definitions
    Task<IEnumerable<BadgeDefinitionDto>> GetAllBadgesAsync();
    Task<IEnumerable<BadgeDefinitionDto>> GetActiveBadgesAsync();
    Task<BadgeDefinitionDto?> GetBadgeByIdAsync(Guid id);
    Task<BadgeDefinitionDto?> GetBadgeByNameAsync(string name);
    Task<IEnumerable<BadgeDefinitionDto>> GetBadgesByCategoryAsync(string category);

    // User badges
    Task<IEnumerable<UserBadgeDto>> GetUserBadgesAsync(Guid userId);
    Task<UserBadgeLevelDto> GetUserBadgeLevelAsync(Guid userId);
    Task<UserBadgeSummaryDto> GetUserBadgeSummaryAsync(Guid userId);
    Task<bool> HasBadgeAsync(Guid userId, string badgeName);

    // Award badges
    Task<UserBadgeDto> AwardBadgeAsync(Guid userId, string badgeName, string? source = null);
    Task<IEnumerable<UserBadgeDto>> CheckAndAwardEligibleBadgesAsync(Guid userId);

    // Admin operations
    Task<BadgeDefinitionDto> CreateBadgeAsync(CreateBadgeDefinitionDto dto);
    Task<BadgeDefinitionDto> UpdateBadgeAsync(Guid id, UpdateBadgeDefinitionDto dto);
    Task ActivateBadgeAsync(Guid id);
    Task DeactivateBadgeAsync(Guid id);
}
