using UserService.Domain.Entities;
using UserService.Application.DTOs;

namespace UserService.Application.Services
{
    public interface IUserService
    {
		// Business Rep Services interfaces
		Task<SubBusinessUserResponseDto> CreateSubBusinessUserAsync(CreateSubBusinessUserDto dto);
		Task<SubBusinessUserResponseDto> UpdateSubBusinessUserAsync(Guid userId, UpdateSubBusinessUserDto dto);
		//Support user Services  interfaces
		Task<SupportUserResponseDto> CreateSupportUserAsync(CreateSupportUserDto dto);
		
		// Register business
		Task <(User, Guid businessId, BusinessRep)> RegisterBusinessAccountAsync(BusinessUserDto userPayload);
			
		// Get Business User By Guid
		Task<BusinessRep?> GetBusinessRepByIdAsync(Guid id);
	
		// Creates End User
		Task<EndUserResponseDto> CreateEndUserAsync(CreateEndUserDto dto);
	
		// Updates business
		Task UpdateBusinessAccount(UpdateBusinessUserDto dto);
		
		// // delete end user
		// Task DeleteEndUserAsync(Guid id);
		//
		// // delete support user
		// Task DeleteSupportUserAsync(Guid id);
	
		// delete user
		Task DeleteUserAsync(Guid id, string type);
    }
}