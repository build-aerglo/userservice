using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

public interface IBusinessRepRepository
{
    Task AddAsync(BusinessRep rep);
    Task<IEnumerable<BusinessRep>> GetByBusinessIdAsync(Guid businessId);
}