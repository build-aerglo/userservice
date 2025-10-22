using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

/// <summary>
/// Repository interface for managing Business Representative persistence.
/// Defines data access operations for business_reps table.
/// </summary>
public interface IBusinessRepRepository
{
    /// <summary>
    /// Retrieves a business representative by its unique ID.
    /// </summary>
    Task<BusinessRep?> GetByIdAsync(Guid id);

    /// <summary>
    /// Retrieves the business representative record for a given user.
    /// </summary>
    Task<BusinessRep?> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// Retrieves all business representatives linked to a specific business.
    /// </summary>
    Task<IEnumerable<BusinessRep>> GetByBusinessIdAsync(Guid businessId);

    /// <summary>
    /// Inserts a new business representative record.
    /// </summary>
    Task AddAsync(BusinessRep businessRep);

    /// <summary>
    /// Updates an existing business representative record.
    /// </summary>
    Task UpdateAsync(BusinessRep businessRep);

    /// <summary>
    /// Deletes a business representative record by ID.
    /// </summary>
    Task DeleteAsync(Guid id);
}