using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

public interface IUserBadgeLevelRepository
{
    Task<UserBadgeLevel?> GetByUserIdAsync(Guid userId);
    Task AddAsync(UserBadgeLevel level);
    Task UpdateAsync(UserBadgeLevel level);
    Task UpsertAsync(UserBadgeLevel level);
}
