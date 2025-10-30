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
    IEndUserProfileRepository endUserProfileRepository
) : IUserService
{

	//Sub business user services
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
            CreatedAt: updatedUser.CreatedAt
        );
    }


	//Support User Services

    public async Task<SupportUserResponseDto> CreateSupportUserAsync(CreateSupportUserDto dto)
    {
        // ✅ 1. Create the user entity with support_user type
        var user = new User(
            username: dto.Username,
            email: dto.Email,
            phone: dto.Phone,
            userType: "support_user",
            address: dto.Address
        );
        
        // validate email address
        if (await userRepository.EmailExistsAsync(dto.Email))
            throw new DuplicateUserEmailException($"Email '{dto.Email}' already exists.");

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

        // save user
        var user = new User(userPayload.Name, userPayload.Email, userPayload.Phone, userPayload.UserType, userPayload.Address);
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

        // ✅ 2. Create user entity
        var user = new User(
            username: dto.Username,
            email: dto.Email,
            phone: dto.Phone,
            userType: "end_user",
            address: dto.Address
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
            CreatedAt: user.CreatedAt
        );
    }

    public async Task UpdateBusinessAccount(UpdateBusinessUserDto dto)
    {
        // confirm business guid exists
        var savedBusiness = await businessRepRepository.GetByIdAsync(dto.Id);
        if (savedBusiness == null)
            throw new BusinessNotFoundException(dto.Id);
        
        // get user
        var user = await userRepository.GetByIdAsync(savedBusiness.UserId);
        if(user is null)
            throw new UserNotFoundException(savedBusiness.UserId);
        
        // update business user on business service with business guid
        var isUpdated = await businessServiceClient.UpdateBusinessAsync(dto);
        if (!isUpdated) 
            throw new BusinessNotUpdatedException($"Business with id {dto.Id} could not be updated..");
        
        // update user with user id
        user.Update(dto.Email, dto.Phone, dto.Address);
        await userRepository.UpdateAsync(user);
        
        // update business user on user service
        savedBusiness.UpdateBusiness(dto.BranchName, dto.BranchAddress);
        await businessRepRepository.UpdateAsync(savedBusiness);

    }
    
    // public async Task DeleteEndUserAsync(Guid id)
    // {
    //     var result = await endUserProfileRepository.GetByIdAsync(id);
    //     if(result is null)
    //         throw new UserNotFoundException(id);
    //
    //     await endUserProfileRepository.DeleteAsync(id);
    // }
    //
    // public async Task DeleteSupportUserAsync(Guid id)
    // {
    //     var result = await supportUserProfileRepository.GetByIdAsync(id);
    //     if(result is null)
    //         throw new UserNotFoundException(id);
    //     
    //     await supportUserProfileRepository.DeleteAsync(id);
    // }
    
    public async Task DeleteUserAsync(Guid id, string type)
    {
        Guid userId = Guid.Empty;

        switch (type.ToLowerInvariant())
        {
            case "end_user":
            {
                var result = await endUserProfileRepository.GetByIdAsync(id);
                if (result is null)
                    throw new UserNotFoundException(id);

                await endUserProfileRepository.DeleteAsync(id);
                userId = result.UserId;
                break;
            }

            case "support_user":
            {
                var result = await supportUserProfileRepository.GetByIdAsync(id);
                if (result is null)
                    throw new UserNotFoundException(id);

                await supportUserProfileRepository.DeleteAsync(id);
                userId = result.UserId;
                break;
            }
             case "business_user":
            {
                var result = await businessRepRepository.GetByIdAsync(id);
                if (result is null)
                    throw new UserNotFoundException(id);

                await businessRepRepository.DeleteAsync(id);
                userId = result.UserId;
                break;
            }
            

            default:
                throw new UserTypeNotFoundException($"User Type {type} must be a valid user type");
        }
        
        if (userId != Guid.Empty)
        {
            await userRepository.DeleteAsync(userId);
        }
    }
}