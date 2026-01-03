using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

public interface IPointRuleRepository
{
    Task<PointRule?> GetByIdAsync(Guid id);
    Task<PointRule?> GetByActionTypeAsync(string actionType);
    Task<IEnumerable<PointRule>> GetAllAsync();
    Task<IEnumerable<PointRule>> GetActiveAsync();
    Task AddAsync(PointRule rule);
    Task UpdateAsync(PointRule rule);
    Task DeleteAsync(Guid id);
}
