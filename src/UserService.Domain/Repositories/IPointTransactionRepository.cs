using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

public interface IPointTransactionRepository
{
    Task<PointTransaction?> GetByIdAsync(Guid id);
    Task<IEnumerable<PointTransaction>> GetByUserIdAsync(Guid userId, int limit = 50, int offset = 0);
    Task<IEnumerable<PointTransaction>> GetByUserIdAndTypeAsync(Guid userId, string transactionType);
    Task<IEnumerable<PointTransaction>> GetByUserIdAndDateRangeAsync(Guid userId, DateTime startDate, DateTime endDate);
    Task<int> GetTotalPointsEarnedByUserAsync(Guid userId);
    Task<IEnumerable<PointTransaction>> GetExpiringPointsAsync(DateTime beforeDate);
    Task AddAsync(PointTransaction transaction);
}
