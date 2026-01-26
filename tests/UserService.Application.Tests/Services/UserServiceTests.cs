using Moq;
using UserService.Application.DTOs;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using UserService.Application.Services;
using UserService.Application.Services.Auth0;

namespace UserService.Application.Tests.Services;

[TestFixture]
public class UserServiceTests
{
    private Mock<IUserRepository> _mockUserRepository = null!;
    private Mock<IBusinessRepRepository> _mockBusinessRepRepository = null!;
    private Mock<IBusinessServiceClient> _mockBusinessServiceClient = null!;
    private Mock<ISupportUserProfileRepository> _mockSupportUserProfileRepository = null!;
    private Mock<IEndUserProfileRepository> _mockEndUserProfileRepository = null!;
    private Mock<IUserSettingsRepository> _mockUserSettingsRepository = null!;
    private Mock<IAuth0ManagementService> _mockAuth0 = null!;
    private Mock<IBadgeService> _mockBadgeService = null!;
    private Mock<IConfiguration> _mockConfig = null!;
    private Application.Services.UserService _service = null!;

    [SetUp]
    public void Setup()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockBusinessRepRepository = new Mock<IBusinessRepRepository>();
        _mockBusinessServiceClient = new Mock<IBusinessServiceClient>();
        _mockSupportUserProfileRepository = new Mock<ISupportUserProfileRepository>();
        _mockEndUserProfileRepository = new Mock<IEndUserProfileRepository>();
        _mockUserSettingsRepository = new Mock<IUserSettingsRepository>();
        _mockBadgeService = new Mock<IBadgeService>();
        _mockAuth0 = new Mock<IAuth0ManagementService>();
        _mockConfig = new Mock<IConfiguration>();

        // Auth0 role mappings
        _mockConfig.Setup(c => c["Auth0:Roles:BusinessUser"]).Returns("auth0_business_role");
        _mockConfig.Setup(c => c["Auth0:Roles:SupportUser"]).Returns("auth0_support_role");
        _mockConfig.Setup(c => c["Auth0:Roles:EndUser"]).Returns("auth0_end_role");

        // ******** CORRECT PARAMETER ORDER ********
        _mockAuth0.Setup(a =>
            a.CreateUserAndAssignRoleAsync(
                It.IsAny<string>(), // email
                It.IsAny<string>(), // username
                It.IsAny<string>(), // password
                It.IsAny<string>()  // roleId
            ))
            .ReturnsAsync("auth0|dummy-id");

        // Mock badge service methods
        _mockBadgeService.Setup(b => 
            b.CheckAndAssignPioneerBadgeAsync(It.IsAny<Guid>(), It.IsAny<DateTime>()))
            .ReturnsAsync(true);

        _service = new Application.Services.UserService(
            _mockUserRepository.Object,
            _mockBusinessRepRepository.Object,
            _mockBusinessServiceClient.Object,
            _mockSupportUserProfileRepository.Object,
            _mockEndUserProfileRepository.Object,
            _mockUserSettingsRepository.Object,
            _mockBadgeService.Object,
            _mockAuth0.Object,
            _mockConfig.Object
        );
    }

    // -----------------------------------------------
    // BUSINESS USER TESTS
    // -----------------------------------------------

    [Test]
    public async Task CreateSubBusinessUser_ShouldReturnResponse_WhenSuccessful()
    {
        var businessId = Guid.NewGuid();

        var dto = new CreateSubBusinessUserDto(
            businessId,
            "john_rep",
            "john@business.com",
            "1234567890",
            "1234567890",
            "123 Business St",
            "Main Branch",
            "456 Branch Ave"
        );

        _mockBusinessServiceClient.Setup(c => c.BusinessExistsAsync(businessId)).ReturnsAsync(true);
        
        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        var fakeUser = new User("john_rep", "john@business.com", "1234567890", "123456", "business_user", "123 Business St", "auth0|dummy-id");

        _mockUserRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(fakeUser);

        _mockBusinessRepRepository.Setup(r => r.AddAsync(It.IsAny<BusinessRep>()))
            .Returns(Task.CompletedTask);

        _mockBusinessRepRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new BusinessRep(businessId, fakeUser.Id, "Main Branch", "456 Branch Ave"));

        var result = await _service.CreateSubBusinessUserAsync(dto);

        Assert.Multiple(() =>
        {
            Assert.That(result.Username, Is.EqualTo("john_rep"));
            Assert.That(result.Email, Is.EqualTo("john@business.com"));
            Assert.That(result.BusinessId, Is.EqualTo(businessId));
        });
    }

    [Test]
    public void CreateSubBusinessUser_ShouldThrow_WhenBusinessDoesNotExist()
    {
        var dto = new CreateSubBusinessUserDto(Guid.NewGuid(), "x", "x", "P", "x", "123 St", null, null);
        
        _mockBusinessServiceClient.Setup(c => c.BusinessExistsAsync(dto.BusinessId))
            .ReturnsAsync(false);

        Assert.ThrowsAsync<BusinessNotFoundException>(() => _service.CreateSubBusinessUserAsync(dto));
    }

    [Test]
    public void CreateSubBusinessUser_ShouldThrow_WhenUserSaveFails()
    {
        var dto = new CreateSubBusinessUserDto(Guid.NewGuid(), "u", "e", "p", "22", "123 St", null, null);

        _mockBusinessServiceClient.Setup(c => c.BusinessExistsAsync(dto.BusinessId))
            .ReturnsAsync(true);

        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        _mockUserRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((User?)null);

        Assert.ThrowsAsync<UserCreationFailedException>(() => _service.CreateSubBusinessUserAsync(dto));
    }

    // -----------------------------------------------
    // SUPPORT USER TESTS
    // -----------------------------------------------

    [Test]
    public async Task CreateSupportUser_ShouldReturnResponse_WhenSuccessful()
    {
        var dto = new CreateSupportUserDto(
            "support",
            "support@test.com",
            "111",
            "123456",
            "addr"
        );

        var fakeUser = new User("support", "support@test.com", "111", "23456", "support_user", "addr", "auth0|dummy-id");

        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockUserRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(fakeUser);

        _mockSupportUserProfileRepository.Setup(r => r.AddAsync(It.IsAny<SupportUserProfile>())).Returns(Task.CompletedTask);
        _mockSupportUserProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new SupportUserProfile(fakeUser.Id));

        var result = await _service.CreateSupportUserAsync(dto);

        Assert.That(result.Email, Is.EqualTo("support@test.com"));
    }

    // -----------------------------------------------
    // END USER TESTS
    // -----------------------------------------------

    [Test]
    public async Task CreateEndUser_ShouldReturnResponse_WhenSuccessful()
    {
        var dto = new CreateEndUserDto(
            "jane",
            "jane@test.com",
            "123",
            "123456",
            "addr",
            "instagram.com/jane"
        );

        var user = new User("jane", "jane@test.com", "123", "123456", "end_user", "addr", "auth0|dummy-id");
        var profile = new EndUserProfile(user.Id, "instagram.com/jane");

        _mockUserRepository.Setup(r => r.EmailExistsAsync(dto.Email)).ReturnsAsync(false);

        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockUserRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(user);

        _mockEndUserProfileRepository.Setup(r => r.AddAsync(It.IsAny<EndUserProfile>())).Returns(Task.CompletedTask);
        _mockEndUserProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(profile);

        var result = await _service.CreateEndUserAsync(dto);

        Assert.That(result.Email, Is.EqualTo("jane@test.com"));
    }
    
    [Test]
    public async Task GetEndUserProfileDetailAsync_ShouldReturnCompleteProfile_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User("john_doe", "john@example.com", "1234567890", "password", "end_user", "123 Main St", "auth0|123");
        var profile = new EndUserProfile(userId, "instagram.com/johndoe");
        var settings = new UserSettings(userId);

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockEndUserProfileRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(profile);
        _mockUserSettingsRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(settings);

        // Act
        var result = await _service.GetEndUserProfileDetailAsync(userId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.UserId, Is.EqualTo(user.Id));
        Assert.That(result.Username, Is.EqualTo("john_doe"));
        Assert.That(result.Email, Is.EqualTo("john@example.com"));
        Assert.That(result.Phone, Is.EqualTo("1234567890"));
        Assert.That(result.SocialMedia, Is.EqualTo("instagram.com/johndoe"));
        Assert.That(result.DarkMode, Is.False);
        Assert.That(result.NotificationPreferences.EmailNotifications, Is.True);
    }

    // ---------------------- UPDATE SUPPORT USER TESTS ----------------------
    [Test]
    public async Task UpdateSupportUser_ShouldReturnResponse_WhenSuccessful()
    {
        // ARRANGE
        var existingUser = new User("support_user", "old@support.com", "1234567890", "password", "support_user", "123 Old St", "auth0|test");
        var userId = existingUser.Id;

        var dto = new UpdateSupportUserDto(
            Email: "updated@support.com",
            Phone: "9876543210",
            Address: "456 Updated St"
        );

        var supportProfile = new SupportUserProfile(userId);

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(existingUser);
        _mockSupportUserProfileRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(supportProfile);
        _mockUserRepository.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockSupportUserProfileRepository.Setup(r => r.UpdateAsync(It.IsAny<SupportUserProfile>())).Returns(Task.CompletedTask);

        // After update, return the updated user
        _mockUserRepository.SetupSequence(r => r.GetByIdAsync(userId))
            .ReturnsAsync(existingUser)
            .ReturnsAsync(() =>
            {
                existingUser.Update(
                    email: "updated@support.com",
                    phone: "9876543210",
                    address: "456 Updated St"
                );
                return existingUser;
            });

        // ACT
        var result = await _service.UpdateSupportUserAsync(userId, dto);

        // ASSERT
        Assert.Multiple(() =>
        {
            Assert.That(result.Email, Is.EqualTo("updated@support.com"));
            Assert.That(result.Phone, Is.EqualTo("9876543210"));
            Assert.That(result.Address, Is.EqualTo("456 Updated St"));
            Assert.That(result.UserId, Is.EqualTo(userId));
        });

        _mockUserRepository.Verify(r => r.GetByIdAsync(userId), Times.AtLeastOnce);
        _mockUserRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once);
        _mockSupportUserProfileRepository.Verify(r => r.UpdateAsync(It.IsAny<SupportUserProfile>()), Times.Once);
    }

    [Test]
    public void UpdateSupportUser_ShouldThrow_WhenUserNotFound()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var dto = new UpdateSupportUserDto(
            Email: "test@support.com",
            Phone: "1234567890",
            Address: null
        );

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // ACT & ASSERT
        var ex = Assert.ThrowsAsync<SupportUserNotFoundException>(
            async () => await _service.UpdateSupportUserAsync(userId, dto)
        );

        Assert.That(ex!.Message, Does.Contain(userId.ToString()));
        _mockUserRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Test]
    public void UpdateSupportUser_ShouldThrow_WhenUserIsNotSupportUser()
    {
        // ARRANGE
        var endUser = new User("end_user", "end@user.com", "1234567890", "password", "end_user", "123 St", "auth0|test");
        var userId = endUser.Id;

        var dto = new UpdateSupportUserDto(
            Email: "test@support.com",
            Phone: "1234567890",
            Address: null
        );

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(endUser);

        // ACT & ASSERT
        var ex = Assert.ThrowsAsync<SupportUserUpdateFailedException>(
            async () => await _service.UpdateSupportUserAsync(userId, dto)
        );

        Assert.That(ex!.Message, Does.Contain("is not a support user"));
        _mockUserRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Test]
    public void UpdateSupportUser_ShouldThrow_WhenSupportProfileNotFound()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var dto = new UpdateSupportUserDto(
            Email: "test@support.com",
            Phone: "1234567890",
            Address: null
        );

        var supportUser = new User("support_user", "support@user.com", "1234567890", "password", "support_user", "123 St", "auth0|test");
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(supportUser);
        _mockSupportUserProfileRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((SupportUserProfile?)null);

        // ACT & ASSERT
        var ex = Assert.ThrowsAsync<SupportUserNotFoundException>(
            async () => await _service.UpdateSupportUserAsync(userId, dto)
        );

        Assert.That(ex!.Message, Does.Contain(userId.ToString()));
    }

    [Test]
    public void UpdateSupportUser_ShouldThrow_WhenUpdateVerificationFails()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var dto = new UpdateSupportUserDto(
            Email: "updated@support.com",
            Phone: "9876543210",
            Address: "456 Updated St"
        );

        var existingUser = new User("support_user", "old@support.com", "1234567890", "password", "support_user", "123 Old St", "auth0|test");
        var supportProfile = new SupportUserProfile(userId);

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(existingUser);
        _mockSupportUserProfileRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(supportProfile);
        _mockUserRepository.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockSupportUserProfileRepository.Setup(r => r.UpdateAsync(It.IsAny<SupportUserProfile>())).Returns(Task.CompletedTask);

        _mockUserRepository.SetupSequence(r => r.GetByIdAsync(userId))
            .ReturnsAsync(existingUser)
            .ReturnsAsync((User?)null);

        // ACT & ASSERT
        var ex = Assert.ThrowsAsync<SupportUserUpdateFailedException>(
            async () => await _service.UpdateSupportUserAsync(userId, dto)
        );

        Assert.That(ex!.Message, Does.Contain("Failed to update user record"));
    }

    [Test]
    public async Task UpdateSupportUser_WithPartialUpdate_ShouldOnlyUpdateProvidedFields()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var dto = new UpdateSupportUserDto(
            Email: "newemail@support.com",
            Phone: null,
            Address: null
        );

        var existingUser = new User("support_user", "old@support.com", "1234567890", "password", "support_user", "123 Old St", "auth0|test");
        var supportProfile = new SupportUserProfile(userId);

        User? capturedUser = null;

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(existingUser);
        _mockSupportUserProfileRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(supportProfile);

        _mockUserRepository.Setup(r => r.UpdateAsync(It.IsAny<User>()))
            .Callback<User>(u => capturedUser = u)
            .Returns(Task.CompletedTask);

        _mockSupportUserProfileRepository.Setup(r => r.UpdateAsync(It.IsAny<SupportUserProfile>())).Returns(Task.CompletedTask);

        _mockUserRepository.SetupSequence(r => r.GetByIdAsync(userId))
            .ReturnsAsync(existingUser)
            .ReturnsAsync(() => capturedUser ?? existingUser);

        // ACT
        var result = await _service.UpdateSupportUserAsync(userId, dto);

        // ASSERT
        Assert.That(capturedUser, Is.Not.Null);
        Assert.That(capturedUser!.Email, Is.EqualTo("newemail@support.com"));
        Assert.That(result.Email, Is.EqualTo("newemail@support.com"));
    }

    [Test]
    public async Task UpdateSupportUser_ShouldTouchSupportProfileTimestamp()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var dto = new UpdateSupportUserDto(
            Email: "updated@support.com",
            Phone: "9876543210",
            Address: "456 Updated St"
        );

        var existingUser = new User("support_user", "old@support.com", "1234567890", "password", "support_user", "123 Old St", "auth0|test");
        var supportProfile = new SupportUserProfile(userId);

        SupportUserProfile? capturedProfile = null;

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(existingUser);
        _mockSupportUserProfileRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(supportProfile);
        _mockUserRepository.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        _mockSupportUserProfileRepository.Setup(r => r.UpdateAsync(It.IsAny<SupportUserProfile>()))
            .Callback<SupportUserProfile>(sp => capturedProfile = sp)
            .Returns(Task.CompletedTask);

        _mockUserRepository.SetupSequence(r => r.GetByIdAsync(userId))
            .ReturnsAsync(existingUser)
            .ReturnsAsync(new User("support_user", "updated@support.com", "9876543210", "password", "support_user", "456 Updated St", "auth0|test"));

        // ACT
        await _service.UpdateSupportUserAsync(userId, dto);

        // ASSERT
        Assert.That(capturedProfile, Is.Not.Null);
        _mockSupportUserProfileRepository.Verify(r => r.UpdateAsync(It.IsAny<SupportUserProfile>()), Times.Once);
    }

    [Test]
    public async Task UpdateSupportUser_WithAllNullFields_ShouldStillUpdateTimestamps()
    {
        // ARRANGE
        var existingUser = new User("support_user", "old@support.com", "1234567890", "password", "support_user", "123 Old St", "auth0|test");
        var userId = existingUser.Id;

        var dto = new UpdateSupportUserDto(
            Email: null,
            Phone: null,
            Address: null
        );

        var supportProfile = new SupportUserProfile(userId);

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(existingUser);
        _mockSupportUserProfileRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(supportProfile);
        _mockUserRepository.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockSupportUserProfileRepository.Setup(r => r.UpdateAsync(It.IsAny<SupportUserProfile>())).Returns(Task.CompletedTask);

        _mockUserRepository.SetupSequence(r => r.GetByIdAsync(userId))
            .ReturnsAsync(existingUser)
            .ReturnsAsync(existingUser);

        // ACT
        var result = await _service.UpdateSupportUserAsync(userId, dto);

        // ASSERT
        Assert.Multiple(() =>
        {
            Assert.That(result.Email, Is.EqualTo("old@support.com"));
            Assert.That(result.Phone, Is.EqualTo("1234567890"));
            Assert.That(result.Address, Is.EqualTo("123 Old St"));
            Assert.That(result.UserId, Is.EqualTo(userId));
        });

        _mockUserRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once);
        _mockSupportUserProfileRepository.Verify(r => r.UpdateAsync(It.IsAny<SupportUserProfile>()), Times.Once);
    }

    [Test]
    public async Task RegisterBusinessAccountAsync_ShouldReturnTuple_WhenSuccessful()
    {
        // ARRANGE
        var dto = new BusinessUserDto
        (
            Name: "TechNova",
            Email: "info@technova.com",
            Password: "SecurePass123",
            Phone: "5551234567",
            UserType: "business_user",
            Address: "123 Innovation Blvd",
            BranchName: "HQ",
            BranchAddress: "123 Innovation Blvd",
            Website: "https://technova.com",
            CategoryIds: new List<string>() { "software", "cloud" }
        );

        var businessId = Guid.NewGuid();
        var user = new User(dto.Name, dto.Email, dto.Phone, "password123", dto.UserType, dto.Address, "auth0|dummy-id");
        var businessRep = new BusinessRep(businessId, user.Id, dto.BranchName, dto.BranchAddress);

        _mockBusinessServiceClient
            .Setup(c => c.CreateBusinessAsync(dto))
            .ReturnsAsync(businessId);

        _mockUserRepository
            .Setup(r => r.AddAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(user);

        _mockBusinessRepRepository
            .Setup(r => r.AddAsync(It.IsAny<BusinessRep>()))
            .Returns(Task.CompletedTask);

        _mockBusinessRepRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(businessRep);

        // ACT
        var result = await _service.RegisterBusinessAccountAsync(dto);

        // ASSERT
        Assert.Multiple(() =>
        {
            Assert.That(result.Item1, Is.Not.Null);
            Assert.That(result.Item1.Email, Is.EqualTo("info@technova.com"));
            Assert.That(result.Item2, Is.EqualTo(businessId));
            Assert.That(result.Item3, Is.Not.Null);
            Assert.That(result.Item3.BusinessId, Is.EqualTo(businessId));
        });

        _mockBusinessServiceClient.Verify(c => c.CreateBusinessAsync(dto), Times.Once);
        _mockUserRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
        _mockBusinessRepRepository.Verify(r => r.AddAsync(It.IsAny<BusinessRep>()), Times.Once);
    }

    [Test]
    public async Task GetEndUserProfileDetailAsync_ShouldAutoCreateSettings_WhenSettingsMissing()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User("john_doe", "john@example.com", "1234567890", "password", "end_user", "123 Main St", "auth0|123");
        var profile = new EndUserProfile(userId, "instagram.com/johndoe");

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockEndUserProfileRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(profile);
        _mockUserSettingsRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((UserSettings?)null);
        _mockUserSettingsRepository.Setup(r => r.AddAsync(It.IsAny<UserSettings>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.GetEndUserProfileDetailAsync(userId);

        // Assert
        Assert.That(result, Is.Not.Null);
        _mockUserSettingsRepository.Verify(r => r.AddAsync(It.Is<UserSettings>(s => s.UserId == userId)), Times.Once);
        Assert.That(result.NotificationPreferences.EmailNotifications, Is.True);
        Assert.That(result.NotificationPreferences.PushNotifications, Is.True);
        Assert.That(result.DarkMode, Is.False);
    }

    [Test]
    public void GetEndUserProfileDetailAsync_ShouldThrowException_WhenUserNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // Act & Assert
        Assert.ThrowsAsync<EndUserNotFoundException>(() => _service.GetEndUserProfileDetailAsync(userId));
    }

    [Test]
    public void GetEndUserProfileDetailAsync_ShouldThrowException_WhenUserIsNotEndUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User("business_user", "business@example.com", "1234567890", "password", "business_user", "123 Main St", "auth0|123");

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act & Assert
        Assert.ThrowsAsync<EndUserNotFoundException>(() => _service.GetEndUserProfileDetailAsync(userId));
    }

    [Test]
    public void GetEndUserProfileDetailAsync_ShouldThrowException_WhenProfileNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User("john_doe", "john@example.com", "1234567890", "password", "end_user", "123 Main St", "auth0|123");

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockEndUserProfileRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((EndUserProfile?)null);

        // Act & Assert
        Assert.ThrowsAsync<EndUserNotFoundException>(() => _service.GetEndUserProfileDetailAsync(userId));
    }

    [Test]
    public async Task UpdateEndUserProfileAsync_ShouldUpdateAllFields_WhenAllProvided()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User("john_doe", "john@example.com", "1234567890", "password", "end_user", "123 Main St", "auth0|123");
        var profile = new EndUserProfile(userId, "instagram.com/johndoe");
        var settings = new UserSettings(userId);

        var updateDto = new UpdateEndUserProfileDto(
            Username: "Lizzy",
            Phone: "9876543210",
            Address: "456 New Street",
            SocialMedia: "twitter.com/johndoe",
            NotificationPreferences: new NotificationPreferencesDto(
                EmailNotifications: false,
                SmsNotifications: true,
                PushNotifications: false,
                MarketingEmails: true
            ),
            DarkMode: true
        );

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockEndUserProfileRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(profile);
        _mockUserSettingsRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(settings);
        _mockUserRepository.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockEndUserProfileRepository.Setup(r => r.UpdateAsync(It.IsAny<EndUserProfile>())).Returns(Task.CompletedTask);
        _mockUserSettingsRepository.Setup(r => r.UpdateAsync(It.IsAny<UserSettings>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateEndUserProfileAsync(userId, updateDto);

        // Assert
        Assert.That(result, Is.Not.Null);
        _mockUserRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once);
        _mockEndUserProfileRepository.Verify(r => r.UpdateAsync(It.IsAny<EndUserProfile>()), Times.Once);
        _mockUserSettingsRepository.Verify(r => r.UpdateAsync(It.IsAny<UserSettings>()), Times.Once);
    }

    [Test]
    public async Task UpdateEndUserProfileAsync_ShouldUpdateOnlyProvidedFields()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User("john_doe", "john@example.com", "1234567890", "password", "end_user", "123 Main St", "auth0|123");
        var profile = new EndUserProfile(userId, "instagram.com/johndoe");
        var settings = new UserSettings(userId);

        var updateDto = new UpdateEndUserProfileDto(
            Username: null,
            Phone: null,
            Address: null,
            SocialMedia: null,
            NotificationPreferences: null,
            DarkMode: true
        );

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockEndUserProfileRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(profile);
        _mockUserSettingsRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(settings);
        _mockUserSettingsRepository.Setup(r => r.UpdateAsync(It.IsAny<UserSettings>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateEndUserProfileAsync(userId, updateDto);

        // Assert
        Assert.That(result, Is.Not.Null);
        _mockUserRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
        _mockEndUserProfileRepository.Verify(r => r.UpdateAsync(It.IsAny<EndUserProfile>()), Times.Never);
        _mockUserSettingsRepository.Verify(r => r.UpdateAsync(It.IsAny<UserSettings>()), Times.Once);
    }

    [Test]
    public async Task UpdateEndUserProfileAsync_ShouldAutoCreateSettings_WhenMissing()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User("john_doe", "john@example.com", "1234567890", "password", "end_user", "123 Main St", "auth0|123");
        var profile = new EndUserProfile(userId, "instagram.com/johndoe");

        var updateDto = new UpdateEndUserProfileDto(
            Username: "Lizzy",
            Phone: "9876543210",
            Address: null,
            SocialMedia: null,
            NotificationPreferences: null,
            DarkMode: true
        );

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockEndUserProfileRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(profile);
    
        
        UserSettings? createdSettings = null;
        _mockUserSettingsRepository
            .SetupSequence(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync((UserSettings?)null)  // First call - doesn't exist
            .ReturnsAsync(() => createdSettings ?? new UserSettings(userId)); // Second call - returns created
    
        _mockUserSettingsRepository
            .Setup(r => r.AddAsync(It.IsAny<UserSettings>()))
            .Callback<UserSettings>(s => createdSettings = s)
            .Returns(Task.CompletedTask);
    
        _mockUserRepository.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockUserSettingsRepository.Setup(r => r.UpdateAsync(It.IsAny<UserSettings>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateEndUserProfileAsync(userId, updateDto);

        // Assert
        Assert.That(result, Is.Not.Null);
    
        // ✅ FIX: Should be called exactly once during UpdateEndUserProfileAsync
        _mockUserSettingsRepository.Verify(
            r => r.AddAsync(It.Is<UserSettings>(s => s.UserId == userId)), 
            Times.Once
        );
    }

    [Test]
    public void UpdateEndUserProfileAsync_ShouldThrowException_WhenUserNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateDto = new UpdateEndUserProfileDto(
            Username: "Lizzy",
            Phone: "9876543210",
            Address: null,
            SocialMedia: null,
            NotificationPreferences: null,
            DarkMode: null
        );

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // Act & Assert
        Assert.ThrowsAsync<EndUserNotFoundException>(() => _service.UpdateEndUserProfileAsync(userId, updateDto));
    }

    [Test]
    public void UpdateEndUserProfileAsync_ShouldThrowException_WhenUserIsNotEndUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User("support_user", "support@example.com", "1234567890", "password", "support_user", "123 Main St", "auth0|123");
        var updateDto = new UpdateEndUserProfileDto(
            Username: "Lizzy",
            Phone: "9876543210",
            Address: null,
            SocialMedia: null,
            NotificationPreferences: null,
            DarkMode: null
        );

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act & Assert
        Assert.ThrowsAsync<EndUserNotFoundException>(() => _service.UpdateEndUserProfileAsync(userId, updateDto));
    }

    [Test]
    public void UpdateEndUserProfileAsync_ShouldThrowException_WhenProfileNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User("john_doe", "john@example.com", "1234567890", "password", "end_user", "123 Main St", "auth0|123");
        var updateDto = new UpdateEndUserProfileDto(
            Username: "Lizzy",
            Phone: "9876543210",
            Address: null,
            SocialMedia: null,
            NotificationPreferences: null,
            DarkMode: null
        );

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockEndUserProfileRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((EndUserProfile?)null);

        // Act & Assert
        Assert.ThrowsAsync<EndUserNotFoundException>(() => _service.UpdateEndUserProfileAsync(userId, updateDto));
    }

    [Test]
    public async Task UpdateEndUserProfileAsync_ShouldNotUpdateUser_WhenNoUserFieldsProvided()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User("john_doe", "john@example.com", "1234567890", "password", "end_user", "123 Main St", "auth0|123");
        var profile = new EndUserProfile(userId, "instagram.com/johndoe");
        var settings = new UserSettings(userId);

        var updateDto = new UpdateEndUserProfileDto(
            Username: null,
            Phone: null,
            Address: null,
            SocialMedia: "twitter.com/johndoe",
            NotificationPreferences: null,
            DarkMode: true
        );

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockEndUserProfileRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(profile);
        _mockUserSettingsRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(settings);
        _mockEndUserProfileRepository.Setup(r => r.UpdateAsync(It.IsAny<EndUserProfile>())).Returns(Task.CompletedTask);
        _mockUserSettingsRepository.Setup(r => r.UpdateAsync(It.IsAny<UserSettings>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateEndUserProfileAsync(userId, updateDto);

        // Assert
        Assert.That(result, Is.Not.Null);
        _mockUserRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
        _mockEndUserProfileRepository.Verify(r => r.UpdateAsync(It.IsAny<EndUserProfile>()), Times.Once);
        _mockUserSettingsRepository.Verify(r => r.UpdateAsync(It.IsAny<UserSettings>()), Times.Once);
    }
}