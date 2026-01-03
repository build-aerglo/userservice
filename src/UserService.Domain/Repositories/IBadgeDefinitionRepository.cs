using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

public interface IBadgeDefinitionRepository
{
    Task<BadgeDefinition?> GetByIdAsync(Guid id);
    Task<BadgeDefinition?> GetByNameAsync(string name);
    Task<IEnumerable<BadgeDefinition>> GetAllAsync();
    Task<IEnumerable<BadgeDefinition>> GetActiveAsync();
    Task<IEnumerable<BadgeDefinition>> GetByCategoryAsync(string category);
    Task<IEnumerable<BadgeDefinition>> GetByTierAsync(int tier);
    Task<IEnumerable<BadgeDefinition>> GetByPointsRequiredAsync(int maxPoints);
    Task AddAsync(BadgeDefinition badge);
    Task UpdateAsync(BadgeDefinition badge);
    Task DeleteAsync(Guid id);
}
