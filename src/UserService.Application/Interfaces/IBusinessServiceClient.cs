using UserService.Application.DTOs;

namespace UserService.Application.Interfaces;

/// <summary>
/// Contract for communication with the external Business Service.
/// Used to verify that a business exists before creating a sub-business user.
/// </summary>
public interface IBusinessServiceClient
{
    /// <summary>
    /// Checks if a business exists by its ID via Business Service API.
    /// </summary>
    /// <param name="businessId">Business ID (GUID)</param>
    /// <returns>True if business exists, otherwise false</returns>
    Task<bool> BusinessExistsAsync(Guid businessId);
    
    // <summary>
    // Create a new business
    //
    Task<Guid?> CreateBusinessAsync(BusinessUserDto business);
    
    /// <summary>
    /// Updates a business
    /// </summary>
    Task<bool> UpdateBusinessAsync(BusinessUpdateRequest business);
    
    
}