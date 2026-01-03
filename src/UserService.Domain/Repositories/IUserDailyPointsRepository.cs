using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

public interface IUserDailyPointsRepository
{
    Task<UserDailyPoints?> GetByUserActionDateAsync(Guid userId, string actionType, DateTime date);
    Task<IEnumerable<UserDailyPoints>> GetByUserIdAsync(Guid userId, DateTime date);
    Task AddAsync(UserDailyPoints dailyPoints);
    Task UpdateAsync(UserDailyPoints dailyPoints);
    Task UpsertAsync(UserDailyPoints dailyPoints);
    Task CleanupOldRecordsAsync(int daysToKeep = 7);
}
