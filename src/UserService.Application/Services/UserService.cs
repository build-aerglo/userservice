using UserService.Application.DTOs;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Domain.Repositories;

namespace UserService.Application.Services;

public class UserService(
    IUserRepository userRepository,
    IEndUserRepository endUserRepository,
    IBusinessRepRepository businessRepRepository,
    IBusinessServiceClient businessServiceClient
) : IUserService
{
    public async Task<EndUser> CreateEndUser(EndUserDto dto)
    {
        var user = new User(
            username: dto.Username,
            email: dto.Email,
            phone: dto.Phone,
            userType: "end_user",
            address: dto.Address
        );
        await userRepository.AddAsync(user);

        // Confirm save
        var savedUser = await userRepository.GetByIdAsync(user.Id);
        if (savedUser is null)
            throw new UserCreationFailedException("Failed to create user record.");

        var endUser = new EndUser(
            savedUser.Id,
            preferences: dto.Preferences,
            bio: dto.Bio,
            socialLinks: dto.SocialLinks
        );
        await endUserRepository.AddAsync(endUser);

        var savedEndUser = await endUserRepository.GetByIdAsync(endUser.Id);
        if (savedEndUser is null)
            throw new UserCreationFailedException("Failed to create business representative relationship.");
        
        
        return endUser;
    }

    public async Task<SubBusinessUserResponseDto> CreateSubBusinessUserAsync(CreateSubBusinessUserDto dto)
    {
        // ✅ 1. Check if the target business exists via BusinessService API
        var businessExists = await businessServiceClient.BusinessExistsAsync(dto.BusinessId);
        if (!businessExists)
            throw new BusinessNotFoundException(dto.BusinessId);

        // ✅ 2. Create the user entity
        var user = new User(
            username: dto.Username,
            email: dto.Email,
            phone: dto.Phone,
            userType: "business_user",
            address: dto.Address
        );
        // ✅ 3. Save user
        await userRepository.AddAsync(user);

        // ✅ 4. Confirm save
        var savedUser = await userRepository.GetByIdAsync(user.Id);
        if (savedUser is null)
            throw new UserCreationFailedException("Failed to create user record.");

        // ✅ 5. Create business representative link
        var businessRep = new BusinessRep(
            businessId: dto.BusinessId,
            userId: user.Id,
            branchName: dto.BranchName,
            branchAddress: dto.BranchAddress
        );

        await businessRepRepository.AddAsync(businessRep);
       
        // ✅ 6. Confirm save
        var savedBusinessRep = await businessRepRepository.GetByIdAsync(businessRep.Id);
        if (savedBusinessRep is null)
            throw new UserCreationFailedException("Failed to create business representative relationship.");

        // ✅ 7. Map to response DTO
        return new SubBusinessUserResponseDto(
            UserId: user.Id,
            BusinessRepId: businessRep.Id,
            BusinessId: businessRep.BusinessId,
            Username: user.Username,
            Email: user.Email,
            Phone: user.Phone,
            Address: user.Address,
            BranchName: businessRep.BranchName,
            BranchAddress: businessRep.BranchAddress,
            CreatedAt: user.CreatedAt
        );
    }
}
