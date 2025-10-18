using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

public interface IEndUserRepository
{
    Task<IEnumerable<EndUser>> GetAllAsync();
    Task<EndUser?> GetByIdAsync(Guid id);
    Task<EndUser?> GetByUserIdAsync(Guid userId);
    Task AddAsync(EndUser endUser);
    Task UpdateAsync(EndUser endUser);
    Task DeleteAsync(Guid id);
}