using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;


public interface IBusinessRepRepository
{ 
    Task<IEnumerable<BusinessRep>> GetByBusinessIdAsync(Guid businessId);
    Task<BusinessRep?> GetByIdAsync(Guid id);
    Task<BusinessRep?> GetByUserIdAsync(Guid userId);
    Task AddAsync(BusinessRep businessRep);
    Task <bool>CheckBusinessExistsInDatabase(Guid businessId);
    
   
}