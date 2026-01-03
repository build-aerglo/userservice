using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

public interface IUserVerificationStatusRepository
{
    Task<UserVerificationStatus?> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<UserVerificationStatus>> GetByVerificationLevelAsync(string level);
    Task AddAsync(UserVerificationStatus status);
    Task UpdateAsync(UserVerificationStatus status);
    Task UpsertAsync(UserVerificationStatus status);
}
