using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

public interface IUserPointsRepository
{
    Task<UserPoints?> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<UserPoints>> GetTopByTotalPointsAsync(int count);
    Task<IEnumerable<UserPoints>> GetTopByLifetimePointsAsync(int count);
    Task AddAsync(UserPoints points);
    Task UpdateAsync(UserPoints points);
    Task UpsertAsync(UserPoints points);
}
