using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

public interface IUserBadgeRepository
{
    Task<UserBadge?> GetByIdAsync(Guid id);
    Task<IEnumerable<UserBadge>> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<UserBadge>> GetActiveByUserIdAsync(Guid userId);
    Task<UserBadge?> GetByUserIdAndTypeAsync(Guid userId, string badgeType, string? location = null, string? category = null);
    Task AddAsync(UserBadge badge);
    Task UpdateAsync(UserBadge badge);
    Task DeleteAsync(Guid id);
    Task<IEnumerable<UserBadge>> GetByTypeAsync(string badgeType);
    Task<int> GetBadgeCountByUserIdAsync(Guid userId);
    Task DeactivateAllTierBadgesAsync(Guid userId);
}
