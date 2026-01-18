using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

public interface IPointRedemptionRepository
{
    Task<PointRedemption?> GetByIdAsync(Guid id);
    Task<IEnumerable<PointRedemption>> GetByUserIdAsync(Guid userId, int limit = 50, int offset = 0);
    Task<IEnumerable<PointRedemption>> GetPendingRedemptionsAsync();
    Task AddAsync(PointRedemption redemption);
    Task UpdateAsync(PointRedemption redemption);
}