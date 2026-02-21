using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

public interface IEndUserProfileRepository
{
    Task AddAsync(EndUserProfile profile);
    Task<EndUserProfile?> GetByIdAsync(Guid id);
    Task<EndUserProfile?> GetByUserIdAsync(Guid userId);
    Task UpdateAsync(EndUserProfile profile);
    Task DeleteAsync(Guid id);
    Task<EndUserSummary> GetUserDataAsync(Guid? userId, string? email, int page = 1, int pageSize = 5);
}
