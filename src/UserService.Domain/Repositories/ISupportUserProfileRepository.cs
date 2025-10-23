using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;
public interface ISupportUserProfileRepository
{
    Task<SupportUserProfile?> GetByIdAsync(Guid id);Task<SupportUserProfile?> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<SupportUserProfile>> GetAllAsync();
    Task AddAsync(SupportUserProfile supportUserProfile);Task UpdateAsync(SupportUserProfile supportUserProfile);
    Task DeleteAsync(Guid id);
}