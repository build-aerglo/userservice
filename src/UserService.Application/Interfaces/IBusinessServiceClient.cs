using UserService.Application.DTOs;

namespace UserService.Application.Interfaces;

public interface IBusinessServiceClient
{
    Task<bool> BusinessExistsAsync(Guid businessId);
    Task<Guid?> CreateBusinessAsync(BusinessUserDto business);
    Task<bool> UpdateBusinessEmailAsync(string oldEmail, string newEmail);
}
