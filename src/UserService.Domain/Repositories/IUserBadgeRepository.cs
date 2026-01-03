using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

public interface IUserBadgeRepository
{
    Task<UserBadge?> GetByIdAsync(Guid id);
    Task<IEnumerable<UserBadge>> GetByUserIdAsync(Guid userId);
    Task<UserBadge?> GetByUserAndBadgeAsync(Guid userId, Guid badgeId);
    Task<bool> HasBadgeAsync(Guid userId, Guid badgeId);
    Task<int> GetBadgeCountByUserAsync(Guid userId);
    Task AddAsync(UserBadge userBadge);
    Task DeleteAsync(Guid id);
    Task DeleteByUserIdAsync(Guid userId);
}
