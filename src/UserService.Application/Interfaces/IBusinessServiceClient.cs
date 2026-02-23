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

    /// <summary>
    /// Create a new business
    /// </summary>
    Task<Guid?> CreateBusinessAsync(BusinessUserDto business);

    /// <summary>
    /// Updates the business email by the old email address.
    /// </summary>
    /// <param name="oldEmail">The current business email</param>
    /// <param name="newEmail">The new business email</param>
    /// <returns>True if update was successful, otherwise false</returns>
    Task<bool> UpdateBusinessEmailAsync(string oldEmail, string newEmail);

    /// <summary>
    /// Sets the user_id field on the business record, linking the owning user account.
    /// </summary>
    /// <param name="businessId">The business to update</param>
    /// <param name="userId">The user ID to assign</param>
    Task UpdateBusinessUserIdAsync(Guid businessId, Guid userId);
}