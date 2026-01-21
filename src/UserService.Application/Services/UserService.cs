using Microsoft.Extensions.Configuration;
using UserService.Application.DTOs;
using UserService.Application.Interfaces;
using UserService.Application.Services.Auth0;
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
    IUserSettingsRepository userSettingsRepository,
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
        // 1. Check if the target business exists via BusinessService API
        var businessExists = await businessServiceClient.BusinessExistsAsync(dto.BusinessId);
       if (!businessExists) 
            throw new BusinessNotFoundException(dto.BusinessId);
        
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

    public async Task<SupportUserResponseDto> UpdateSupportUserAsync(Guid userId, UpdateSupportUserDto dto)
    {
        // Get the existing user
        var user = await userRepository.GetByIdAsync(userId);
        if (user is null)
            throw new SupportUserNotFoundException(userId);

        // Verify user is a support user
        if (user.UserType != "support_user")
            throw new SupportUserUpdateFailedException($"User with ID {userId} is not a support user.");

        // Get the support user profile
        var supportProfile = await supportUserProfileRepository.GetByUserIdAsync(userId);
        if (supportProfile is null)
            throw new SupportUserNotFoundException(userId);

        // Update user details
        user.Update(dto.Email, dto.Phone, dto.Address);

        // Save updated user
        await userRepository.UpdateAsync(user);

        // Update support profile timestamp
        supportProfile.UpdateTimestamp();
        await supportUserProfileRepository.UpdateAsync(supportProfile);

        // Verify update
        var updatedUser = await userRepository.GetByIdAsync(userId);
        if (updatedUser is null)
            throw new SupportUserUpdateFailedException("Failed to update user record.");

        // Map to response DTO
        return new SupportUserResponseDto(
            UserId: updatedUser.Id,
            SupportUserProfileId: supportProfile.Id,
            Username: updatedUser.Username,
            Email: updatedUser.Email,
            Phone: updatedUser.Phone,
            Address: updatedUser.Address,
            Auth0UserId: updatedUser.Auth0UserId,
            CreatedAt: updatedUser.CreatedAt
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
    
    public async Task<EndUserProfileDetailDto> GetEndUserProfileDetailAsync(Guid userId)
    {
        // 1. Get the user
        var user = await userRepository.GetByIdAsync(userId);
        if (user is null)
            throw new EndUserNotFoundException(userId);

        // 2. Verify this is an end user
        if (user.UserType != "end_user")
            throw new EndUserNotFoundException(userId);

        // 3. Get the end user profile
        var profile = await endUserProfileRepository.GetByUserIdAsync(userId);
        if (profile is null)
            throw new EndUserNotFoundException(userId);

        // 4. Get user settings (create default if doesn't exist)
        var settings = await userSettingsRepository.GetByUserIdAsync(userId);
        if (settings is null)
        {
            // Create default settings for this user
            settings = new UserSettings(userId);
            await userSettingsRepository.AddAsync(settings);
        }

        // 5. Parse notification preferences from JSONB
        var notificationPrefs = settings.GetNotificationPreferences();

        // 6. Map to DTO
        return new EndUserProfileDetailDto(
            UserId: user.Id,
            Username: user.Username,
            Email: user.Email,
            Phone: user.Phone,
            Address: user.Address,
            JoinDate: user.JoinDate,
        
            EndUserProfileId: profile.Id,
            SocialMedia: profile.SocialMedia,
        
            NotificationPreferences: new NotificationPreferencesDto(
                EmailNotifications: notificationPrefs.EmailNotifications,
                SmsNotifications: notificationPrefs.SmsNotifications,
                PushNotifications: notificationPrefs.PushNotifications,
                MarketingEmails: notificationPrefs.MarketingEmails
            ),
            DarkMode: settings.DarkMode,
        
            CreatedAt: user.CreatedAt,
            UpdatedAt: settings.UpdatedAt
        );
    }

    public async Task<EndUserProfileDetailDto> UpdateEndUserProfileAsync(Guid userId, UpdateEndUserProfileDto dto)
    {
        // 1. Get the user
        var user = await userRepository.GetByIdAsync(userId);
        if (user is null)
            throw new EndUserNotFoundException(userId);

        // 2. Verify this is an end user
        if (user.UserType != "end_user")
            throw new EndUserNotFoundException(userId);

        // 3. Get the end user profile
        var profile = await endUserProfileRepository.GetByUserIdAsync(userId);
        if (profile is null)
            throw new EndUserNotFoundException(userId);

        // 4. Get user settings (create if doesn't exist)
        var settings = await userSettingsRepository.GetByUserIdAsync(userId);
        if (settings is null)
        {
            settings = new UserSettings(userId);
            await userSettingsRepository.AddAsync(settings);
        }

        // 5. Update user basic info (if provided)
        if (!string.IsNullOrWhiteSpace(dto.Username) || 
            !string.IsNullOrWhiteSpace(dto.Phone) || 
            dto.Address != null)
        {
            user.Update(
                email: null, // Email cannot be updated via this endpoint
                phone: dto.Phone,
                address: dto.Address
            );
            
            // Update username separately if needed (User entity might not have this in Update method)
            // If username update is needed, you may need to add it to the User.Update method
            await userRepository.UpdateAsync(user);
        }

        // 6. Update end user profile (if provided)
        if (dto.SocialMedia != null)
        {
            profile.UpdateSocialMedia(dto.SocialMedia);
            await endUserProfileRepository.UpdateAsync(profile);
        }

        // 7. Update user settings (if provided)
        if (dto.NotificationPreferences != null || dto.DarkMode.HasValue)
        {
            NotificationPreferencesModel? notifPrefs = null;
            
            if (dto.NotificationPreferences != null)
            {
                notifPrefs = new NotificationPreferencesModel
                {
                    EmailNotifications = dto.NotificationPreferences.EmailNotifications,
                    SmsNotifications = dto.NotificationPreferences.SmsNotifications,
                    PushNotifications = dto.NotificationPreferences.PushNotifications,
                    MarketingEmails = dto.NotificationPreferences.MarketingEmails
                };
            }
            
            settings.UpdateSettings(
                darkMode: dto.DarkMode,
                notificationPrefs: notifPrefs
            );
            await userSettingsRepository.UpdateAsync(settings);
        }

        // 8. Fetch and return updated profile
        return await GetEndUserProfileDetailAsync(userId);
    }



}