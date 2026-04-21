namespace UserService.Domain.Repositories;

public interface IBusinessRepository
{
    Task<string?> GetNameByIdAsync(Guid businessId);
    Task<Guid?> GetIdByEmailAsync(string email);
    Task MarkEmailVerifiedAsync(Guid businessId);
    Task MarkEmailVerifiedOnVerificationTableAsync(Guid businessId);
    Task UpdateOwnerAsync(Guid businessId, Guid userId, string email, string? phoneNumber);
    Task UpdateStatusAsync(Guid businessId, string status);
}
