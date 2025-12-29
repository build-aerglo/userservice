using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

public interface IUserPointsRepository
{
    Task<UserPoints?> GetByIdAsync(Guid id);
    Task<UserPoints?> GetByUserIdAsync(Guid userId);
    Task AddAsync(UserPoints userPoints);
    Task UpdateAsync(UserPoints userPoints);
    Task<IEnumerable<UserPoints>> GetTopUsersByPointsAsync(int limit = 10);
    Task<IEnumerable<UserPoints>> GetTopUsersByPointsInLocationAsync(string state, int limit = 10);
    Task<int> GetUserRankAsync(Guid userId);
    Task<int> GetUserRankInLocationAsync(Guid userId, string state);
}

public interface IPointTransactionRepository
{
    Task<PointTransaction?> GetByIdAsync(Guid id);
    Task<IEnumerable<PointTransaction>> GetByUserIdAsync(Guid userId, int limit = 50, int offset = 0);
    Task AddAsync(PointTransaction transaction);
    Task<IEnumerable<PointTransaction>> GetByUserIdAndTypeAsync(Guid userId, string transactionType);
    Task<IEnumerable<PointTransaction>> GetByReferenceAsync(Guid referenceId, string referenceType);
    Task<decimal> GetTotalPointsByUserIdAsync(Guid userId);
    Task<decimal> GetTotalPointsByUserIdAndDateRangeAsync(Guid userId, DateTime startDate, DateTime endDate);
}
