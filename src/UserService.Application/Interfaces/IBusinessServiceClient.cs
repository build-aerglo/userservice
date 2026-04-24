using UserService.Application.DTOs;

namespace UserService.Application.Interfaces;

public interface IBusinessServiceClient
{
    Task<bool> BusinessExistsAsync(Guid businessId);
    Task<Guid?> CreateBusinessAsync(BusinessUserDto business);
    Task<bool> UpdateBusinessEmailAsync(string oldEmail, string newEmail);

    /// <summary>
    /// Deletes an orphaned business record created by CreateBusinessAsync in cases
    /// where subsequent registration steps (password decrypt, Auth0 user creation,
    /// DB user record) fail. Used as a compensating action to prevent orphaned rows.
    /// Returns true if deleted or already gone, false on unexpected errors.
    /// </summary>
    Task<bool> DeleteBusinessAsync(Guid businessId);
}