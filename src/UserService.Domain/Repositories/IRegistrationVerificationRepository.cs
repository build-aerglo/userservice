using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

public interface IRegistrationVerificationRepository
{
    Task AddAsync(RegistrationVerification verification);
    Task<RegistrationVerification?> GetByEmailAsync(string email);
    Task DeleteByEmailAsync(string email);
}
