using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

/// <summary>
/// Repository interface for BusinessRep operations
/// This is a CONTRACT - it defines WHAT operations are available
/// The actual implementation (HOW) will be in Infrastructure layer
/// </summary>
public interface IBusinessRepRepository
{
    /// <summary>
    /// Get all business reps for a specific business
    /// </summary>
    Task<IEnumerable<BusinessRep>> GetByBusinessIdAsync(Guid businessId);
    
    /// <summary>
    /// Get a specific business rep by ID
    /// </summary>
    Task<BusinessRep?> GetByIdAsync(Guid id);
    
    /// <summary>
    /// Get business rep by user ID (one user can only be rep for one business)
    /// </summary>
    Task<BusinessRep?> GetByUserIdAsync(Guid userId);
    
    /// <summary>
    /// Add a new business rep relationship
    /// </summary>
    Task AddAsync(BusinessRep businessRep);
    
   
}