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
                It.IsAny<string>(), // password
                It.IsAny<string>(), // username
                It.IsAny<string>()  // roleId
            ))
            .ReturnsAsync("auth0|dummy-id");

        _service = new Application.Services.UserService(
            _mockUserRepository.Object,
            _mockBusinessRepRepository.Object,
            _mockBusinessServiceClient.Object,
            _mockSupportUserProfileRepository.Object,
            _mockEndUserProfileRepository.Object,
            _mockUserSettingsRepository.Object,
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

        var fakeUser = new User("john_rep", "john@business.com", "1234567890", "123456","business_user", "123 Business St", "auth0|dummy-id");

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
        var dto = new CreateSubBusinessUserDto(Guid.NewGuid(), "x", "x", "P","x", null, null, null);
        
        _mockBusinessServiceClient.Setup(c => c.BusinessExistsAsync(dto.BusinessId))
            .ReturnsAsync(false);

        Assert.ThrowsAsync<BusinessNotFoundException>(() => _service.CreateSubBusinessUserAsync(dto));
    }

    [Test]
    public void CreateSubBusinessUser_ShouldThrow_WhenUserSaveFails()
    {
        var dto = new CreateSubBusinessUserDto(Guid.NewGuid(), "u", "e", "p", "22",null, null, null);

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

        var fakeUser = new User("support", "support@test.com", "111", "23456", "support_user","addr", "auth0|dummy-id");

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

        var user = new User("jane", "jane@test.com", "123", "123456","end_user", "addr", "auth0|dummy-id");
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

    // ========================================================================
    // UPDATE ENDPOINT SERVICE TESTS
    // ========================================================================

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
            DarkMode: true  // Only updating dark mode
        );

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockEndUserProfileRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(profile);
        _mockUserSettingsRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(settings);
        _mockUserSettingsRepository.Setup(r => r.UpdateAsync(It.IsAny<UserSettings>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateEndUserProfileAsync(userId, updateDto);

        // Assert
        Assert.That(result, Is.Not.Null);
        _mockUserRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never); // Should not update user
        _mockEndUserProfileRepository.Verify(r => r.UpdateAsync(It.IsAny<EndUserProfile>()), Times.Never); // Should not update profile
        _mockUserSettingsRepository.Verify(r => r.UpdateAsync(It.IsAny<UserSettings>()), Times.Once); // Should only update settings
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
        _mockUserSettingsRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((UserSettings?)null);
        _mockUserSettingsRepository.Setup(r => r.AddAsync(It.IsAny<UserSettings>())).Returns(Task.CompletedTask);
        _mockUserRepository.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockUserSettingsRepository.Setup(r => r.UpdateAsync(It.IsAny<UserSettings>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateEndUserProfileAsync(userId, updateDto);

        // Assert
        Assert.That(result, Is.Not.Null);
        _mockUserSettingsRepository.Verify(r => r.AddAsync(It.Is<UserSettings>(s => s.UserId == userId)), Times.Once);
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
