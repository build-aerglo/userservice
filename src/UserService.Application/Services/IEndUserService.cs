using UserService.Application.DTOs;
using UserService.Domain.Entities;

namespace UserService.Application.Services;

public interface IEndUserService
{
    Task<IEnumerable<EndUser>> GetAllAsync();
    Task<EndUser?> GetByIdAsync(Guid id);
    Task<EndUser?> GetByUserIdAsync(Guid userId);
    Task<EndUser> CreateAsync(EndUserDto endUser);
    Task UpdateAsync(Guid id, string? preferences, string? bio, string? socialLinks);
    Task DeleteAsync(Guid id);
}