using UserService.Application.DTOs;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Domain.Repositories;

namespace UserService.Application.Services;

public class UserService(
    IUserRepository userRepository,
    IBusinessRepRepository businessRepRepository,
    IBusinessServiceClient businessServiceClient
) : IUserService
{
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

    public async Task<SettingsDto> GetSettingsAsync(Guid userId)
    {
        var settings = await userRepository.GetSettingsByUserIdAsync(userId);

        if (settings == null)
        {
            var defaultSettings = new Settings(
                userId: userId,
                notificationPreferences: new List<string>(), // or maybe ["email"]
                darkMode: false
            );

            // Save the new default settings
            settings = await userRepository.UpdateSettingsAsync(defaultSettings);
        }

        

        return new SettingsDto(
            settings.UserId,
            settings.NotificationPreferences,
            settings.DarkMode
        );
    }
    public async Task<SettingsDto> SetSettingsAsync(SettingsDto dto)
    {
        var savedUser = await userRepository.GetByIdAsync(dto.UserId);
        if (savedUser is null)
            throw new Exception("No user found!!!");
        var settings = new Settings
        (
            userId: savedUser.Id,
            notificationPreferences: dto.NotificationPreferences,
            darkMode: dto.DarkMode
        );

        var result = await userRepository.UpdateSettingsAsync(settings);

        return new SettingsDto(
            result.UserId,
            result.NotificationPreferences,
            result.DarkMode
        );
    }
}
