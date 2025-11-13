using Microsoft.Extensions.Configuration;
using UserService.Application.DTOs;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Domain.Repositories;

namespace UserService.Application.Services;

public class UserService(
    IUserRepository userRepository,
    IBusinessRepRepository businessRepRepository,
    IBusinessServiceClient businessServiceClient,
    ISupportUserProfileRepository supportUserProfileRepository,
    IEndUserProfileRepository endUserProfileRepository,
    IAuth0ManagementService _auth0,
    IConfiguration _config
) : IUserService
{

public async Task<User?> GetUserByIdAsync(Guid userId)
{
    return await userRepository.GetByIdAsync(userId);
}
	//Sub business user services
    public async Task<SubBusinessUserResponseDto> CreateSubBusinessUserAsync(CreateSubBusinessUserDto dto)
    {
        // ✅ 1. Check if the target business exists via BusinessService API
       // var businessExists = await businessServiceClient.BusinessExistsAsync(dto.BusinessId);
      //  if (!businessExists)
         //   throw new BusinessNotFoundException(dto.BusinessId);
        
        var auth0UserId = await _auth0.CreateUserAndAssignRoleAsync(dto.Email, dto.Username, dto.Password,_config["Auth0:Roles:BusinessUser"]);

        // ✅ 2. Create the user entity
        var user = new User(
            username: dto.Username,
            email: dto.Email,
            phone: dto.Phone,
            password: dto.Password,
            userType: "business_user",
            address: dto.Address,
            auth0UserId
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
            Auth0UserId: auth0UserId,
            CreatedAt: user.CreatedAt
        );
    }


    public async Task<SubBusinessUserResponseDto> UpdateSubBusinessUserAsync(Guid userId, UpdateSubBusinessUserDto dto)
    {
        //Get existing user
        var user = await userRepository.GetByIdAsync(userId);
        if (user is null)
            throw new SubBusinessUserNotFoundException(userId);
        
        //Get existing business rep record
        var businessRep = await businessRepRepository.GetByUserIdAsync(userId);
        if (businessRep is null)
            throw new SubBusinessUserNotFoundException(userId);

        //Update user details
        user.Update(dto.Email, dto.Phone, dto.Address);
        await userRepository.UpdateAsync(user);

        //Verify user update
        var updatedUser = await userRepository.GetByIdAsync(userId);
        if (updatedUser is null)
            throw new SubBusinessUserUpdateFailedException("Failed to update user record.");

        //Update business rep branch details
        businessRep.UpdateBranch(dto.BranchName, dto.BranchAddress);
        await businessRepRepository.UpdateAsync(businessRep);

        //Verify business rep update
        var updatedBusinessRep = await businessRepRepository.GetByIdAsync(businessRep.Id);
        if (updatedBusinessRep is null)
            throw new SubBusinessUserUpdateFailedException("Failed to update business representative record.");

        //Map to response DTO
        return new SubBusinessUserResponseDto(
            UserId: updatedUser.Id,
            BusinessRepId: updatedBusinessRep.Id,
            BusinessId: updatedBusinessRep.BusinessId,
            Username: updatedUser.Username,
            Email: updatedUser.Email,
            Phone: updatedUser.Phone,
            Address: updatedUser.Address,
            BranchName: updatedBusinessRep.BranchName,
            BranchAddress: updatedBusinessRep.BranchAddress,
            Auth0UserId: string.Empty,
            CreatedAt: updatedUser.CreatedAt
        );
    }


	//Support User Services

    public async Task<SupportUserResponseDto> CreateSupportUserAsync(CreateSupportUserDto dto)
    {
        // validate email address
        if (await userRepository.EmailExistsAsync(dto.Email))
            throw new DuplicateUserEmailException($"Email '{dto.Email}' already exists.");
        
        var auth0UserId = await _auth0.CreateUserAndAssignRoleAsync(dto.Email, dto.Username, dto.Password,_config["Auth0:Roles:SupportUser"]);
        
        // ✅ 1. Create the user entity with support_user type
        var user = new User(
            username: dto.Username,
            email: dto.Email,
            phone: dto.Phone,
            password:dto.Password,
            userType: "support_user",
            address: dto.Address,
            auth0UserId:auth0UserId
        );

        // ✅ 2. Save user
        await userRepository.AddAsync(user);
        
        // ✅ 3. Confirm user was saved
        var savedUser = await userRepository.GetByIdAsync(user.Id);
        if (savedUser is null)
            throw new UserCreationFailedException("Failed to create user record.");

        // ✅ 4. Create support user profile link
        var supportUserProfile = new SupportUserProfile(userId: user.Id);

        await supportUserProfileRepository.AddAsync(supportUserProfile);
        
        // ✅ 5. Confirm support profile was saved
        var savedSupportProfile = await supportUserProfileRepository.GetByIdAsync(supportUserProfile.Id);
        if (savedSupportProfile is null)
            throw new UserCreationFailedException("Failed to create support user profile.");

        // ✅ 6. Map to response DTO
        return new SupportUserResponseDto(
            UserId: user.Id,
            SupportUserProfileId: supportUserProfile.Id,
            Username: user.Username,
            Email: user.Email,
            Phone: user.Phone,
            Address: user.Address,
            Auth0UserId: auth0UserId,
            CreatedAt: user.CreatedAt
        );
    }


	//Business User Services
    
    public async Task<(User, Guid businessId, BusinessRep)> RegisterBusinessAccountAsync(BusinessUserDto userPayload)
    {   
        // fetch business
        var businessId = await businessServiceClient.CreateBusinessAsync(userPayload);
        if (businessId == null || businessId == Guid.Empty)
            throw new BusinessUserCreationFailedException("Business creation failed: BusinessId is missing from services.");
        
        var auth0UserId = await _auth0.CreateUserAndAssignRoleAsync(userPayload.Email, userPayload.Name, userPayload.Password,_config["Auth0:Roles:BusinessUser"]);

        // save user
        var user = new User(userPayload.Name, userPayload.Email, userPayload.Phone, userPayload.Password, userPayload.UserType, userPayload.Address,auth0UserId);
        await userRepository.AddAsync(user);

        // confirm save
        var savedUser = await userRepository.GetByIdAsync(user.Id);
        if (savedUser == null)
            throw new UserCreationFailedException("Failed to create user record.");


        // save business
        var businessRep = new BusinessRep(businessId.Value, savedUser.Id, userPayload.BranchName, userPayload.BranchAddress);
        await businessRepRepository.AddAsync(businessRep);

        // confirm save
        var savedBusiness = await GetBusinessRepByIdAsync(businessRep.Id);
        if (savedBusiness == null)
            throw new BusinessUserCreationFailedException("Failed to create business record.");

        return (user, businessId.Value, businessRep);
    }
    
    public async Task<BusinessRep?> GetBusinessRepByIdAsync(Guid id)
        => await businessRepRepository.GetByIdAsync(id);
    
    public async Task<EndUserResponseDto> CreateEndUserAsync(CreateEndUserDto dto)
    {
        // ✅ 1. Validate email uniqueness
        if (await userRepository.EmailExistsAsync(dto.Email))
            throw new DuplicateUserEmailException($"Email '{dto.Email}' already exists.");
        
        var auth0UserId = await _auth0.CreateUserAndAssignRoleAsync(dto.Email, dto.Username, dto.Password,_config["Auth0:Roles:EndUser"]);

        // ✅ 2. Create user entity
        var user = new User(
            username: dto.Username,
            email: dto.Email,
            phone: dto.Phone,
            password:dto.Password,
            userType: "end_user",
            address: dto.Address,
            auth0UserId
        );

        // ✅ 3. Save user
        await userRepository.AddAsync(user);

        // ✅ 4. Confirm save
        var savedUser = await userRepository.GetByIdAsync(user.Id);
        if (savedUser is null)
            throw new UserCreationFailedException("Failed to create user record.");

        // ✅ 5. Create end user profile
        var endUserProfile = new EndUserProfile(
            userId: user.Id,
            socialMedia: dto.SocialMedia
        );

        await endUserProfileRepository.AddAsync(endUserProfile);

        // ✅ 6. Confirm profile saved
        var savedProfile = await endUserProfileRepository.GetByIdAsync(endUserProfile.Id);
        if (savedProfile is null)
            throw new UserCreationFailedException("Failed to create end user profile.");

        // ✅ 7. Map to response DTO
        return new EndUserResponseDto(
            UserId: user.Id,
            EndUserProfileId: endUserProfile.Id,
            Username: user.Username,
            Email: user.Email,
            Phone: user.Phone,
            Address: user.Address,
            SocialMedia: endUserProfile.SocialMedia,
            Auth0UserId:auth0UserId,
            CreatedAt: user.CreatedAt
        );
    }


}