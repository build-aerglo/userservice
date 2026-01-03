using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

public interface IUserLocationPreferencesRepository
{
    Task<UserLocationPreferences?> GetByUserIdAsync(Guid userId);
    Task AddAsync(UserLocationPreferences preferences);
    Task UpdateAsync(UserLocationPreferences preferences);
    Task UpsertAsync(UserLocationPreferences preferences);
}
