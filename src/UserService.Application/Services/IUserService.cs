using UserService.Domain.Entities;
using UserService.Application.DTOs;

namespace UserService.Application.Services
{
    public interface IUserService
    {
		// Business Rep Services interfaces
		Task<SubBusinessUserResponseDto> CreateSubBusinessUserAsync(CreateSubBusinessUserDto dto);
		
		//Support user Services  interfaces
		Task<SupportUserResponseDto> CreateSupportUserAsync(CreateSupportUserDto dto);
		
		// Register business
		Task <(User, Guid businessId, BusinessRep)> RegisterBusinessAccountAsync(BusinessUserDto userPayload);
		
		Task<BusinessRep?> GetBusinessRepByIdAsync(Guid id);

		Task<EndUserResponseDto> CreateEndUserAsync(CreateEndUserDto dto);

    }
}