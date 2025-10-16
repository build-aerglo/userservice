using UserService.Domain.Entities;
using UserService.Application.DTOs;

namespace UserService.Application.Services
{
    public interface IUserService
    {
        Task<IEnumerable<User>> GetAllAsync();
        Task<User?> GetByIdAsync(Guid id);
        Task<User> CreateAsync(string username, string email, string phone, string userType, string? address);
        Task UpdateAsync(Guid id, string? email, string? phone, string? address);
        Task DeleteAsync(Guid id);
		Task<SubBusinessUserResponseDto> CreateSubBusinessUserAsync(CreateSubBusinessUserDto dto);
    }
}