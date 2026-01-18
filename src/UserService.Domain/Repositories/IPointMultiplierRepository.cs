using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

public interface IPointMultiplierRepository
{
    Task<PointMultiplier?> GetByIdAsync(Guid id);
    Task<IEnumerable<PointMultiplier>> GetActiveMultipliersAsync();
    Task<IEnumerable<PointMultiplier>> GetAllAsync();
    Task AddAsync(PointMultiplier multiplier);
    Task UpdateAsync(PointMultiplier multiplier);
    Task DeleteAsync(Guid id);
}