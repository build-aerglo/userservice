using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

public interface IUserVerificationRepository
{
    Task<UserVerification?> GetByIdAsync(Guid id);
    Task<UserVerification?> GetByUserIdAsync(Guid userId);
    Task AddAsync(UserVerification verification);
    Task UpdateAsync(UserVerification verification);
    Task<bool> IsUserVerifiedAsync(Guid userId);
    Task<bool> IsUserFullyVerifiedAsync(Guid userId);
    Task<IEnumerable<UserVerification>> GetVerifiedUsersAsync(int limit = 100, int offset = 0);
}

public interface IVerificationTokenRepository
{
    Task<VerificationToken?> GetByIdAsync(Guid id);
    Task<VerificationToken?> GetLatestByUserIdAndTypeAsync(Guid userId, string verificationType);
    Task<VerificationToken?> GetByTokenAsync(string token);
    Task AddAsync(VerificationToken token);
    Task UpdateAsync(VerificationToken token);
    Task DeleteExpiredTokensAsync();
    Task InvalidatePreviousTokensAsync(Guid userId, string verificationType);
}
