using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

public interface ISocialIdentityRepository
{
    Task<SocialIdentity?> GetByIdAsync(Guid id);
    Task<SocialIdentity?> GetByProviderUserIdAsync(string provider, string providerUserId);
    Task<SocialIdentity?> GetByUserAndProviderAsync(Guid userId, string provider);
    Task<IEnumerable<SocialIdentity>> GetByUserIdAsync(Guid userId);
    Task AddAsync(SocialIdentity socialIdentity);
    Task UpdateAsync(SocialIdentity socialIdentity);
    Task DeleteAsync(Guid id);
    Task DeleteByUserAndProviderAsync(Guid userId, string provider);
}
