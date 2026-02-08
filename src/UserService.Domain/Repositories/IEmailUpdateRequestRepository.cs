using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

public interface IEmailUpdateRequestRepository
{
    Task AddAsync(EmailUpdateRequest request);
    Task DeleteByBusinessIdAsync(Guid businessId);
}
