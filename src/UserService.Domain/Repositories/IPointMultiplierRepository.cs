using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

public interface IPointMultiplierRepository
{
    Task<PointMultiplier?> GetByIdAsync(Guid id);
    Task<IEnumerable<PointMultiplier>> GetAllAsync();
    Task<IEnumerable<PointMultiplier>> GetActiveAsync();
    Task<IEnumerable<PointMultiplier>> GetCurrentlyActiveAsync();
    Task<PointMultiplier?> GetHighestActiveMultiplierAsync(string actionType);
    Task AddAsync(PointMultiplier multiplier);
    Task UpdateAsync(PointMultiplier multiplier);
    Task DeleteAsync(Guid id);
}
