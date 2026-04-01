using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

public interface IBusinessClaimRepository
{
    Task<BusinessClaim?> GetByBusinessIdAsync(Guid businessId);
}
