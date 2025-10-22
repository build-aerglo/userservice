using UserService.Domain.Entities;
using UserService.Application.DTOs;

namespace UserService.Application.Services
{
    public interface IUserService
    {
		Task<SubBusinessUserResponseDto> CreateSubBusinessUserAsync(CreateSubBusinessUserDto dto);
        // Task<EndUser?> GetByIdAsync(Guid id);
        Task<EndUser> CreateEndUser(EndUserDto dto);
    }
}