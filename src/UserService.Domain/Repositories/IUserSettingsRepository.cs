using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

public interface IUserSettingsRepository
{
    Task<UserSettings?> GetByUserIdAsync(Guid userId);
    Task AddAsync(UserSettings userSettings);
    Task UpdateAsync(UserSettings userSettings);
}