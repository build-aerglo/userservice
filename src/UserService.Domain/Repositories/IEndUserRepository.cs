using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

public interface IEndUserRepository
{
    Task<EndUser?> GetByIdAsync(Guid id);
    Task AddAsync(EndUser user);
}