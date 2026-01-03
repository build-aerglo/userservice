using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

public interface IUserSavedLocationRepository
{
    Task<UserSavedLocation?> GetByIdAsync(Guid id);
    Task<UserSavedLocation?> GetByUserIdAndNameAsync(Guid userId, string name);
    Task<IEnumerable<UserSavedLocation>> GetByUserIdAsync(Guid userId);
    Task<UserSavedLocation?> GetDefaultByUserIdAsync(Guid userId);
    Task<IEnumerable<UserSavedLocation>> GetActiveByUserIdAsync(Guid userId);
    Task AddAsync(UserSavedLocation location);
    Task UpdateAsync(UserSavedLocation location);
    Task DeleteAsync(Guid id);
    Task ClearDefaultForUserAsync(Guid userId);
}
