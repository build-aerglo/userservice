using UserService.Domain.Entities;
using UserService.Application.DTOs;

namespace UserService.Application.Services
{
    public interface IUserService
    {
		Task<SubBusinessUserResponseDto> CreateSubBusinessUserAsync(CreateSubBusinessUserDto dto);
    }
}