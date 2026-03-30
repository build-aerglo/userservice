using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using UserService.Application.DTOs;
using UserService.Application.Interfaces;
using UserService.Application.Services;
using UserService.Application.Services.Auth0;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Domain.Repositories;

namespace UserService.Application.Tests.Services;

[TestFixture]
public class RegisterBusinessAfterClaimTests
{
    private Mock<IUserRepository> _mockUserRepo = null!;
    private Mock<IBusinessRepRepository> _mockBusinessRepRepo = null!;
    private Mock<IBusinessServiceClient> _mockBusinessClient = null!;
    private Mock<IBusinessClaimRepository> _mockBusinessClaimRepo = null!;
    private Mock<IBusinessRepository> _mockBusinessRepo = null!;
    private Mock<IAuth0ManagementService> _mockAuth0 = null!;
    private Mock<IConfiguration> _mockConfig = null!;
    private Mock<IRegistrationVerificationService> _mockRegVerification = null!;
    private Application.Services.UserService _service = null!;

    private static readonly Guid BusinessId = Guid.NewGuid();
    private const string BusinessName = "Acme Corp";
    private const string Auth0Id = "auth0|biz-123";

    [SetUp]
    public void Setup()
    {
        _mockUserRepo = new Mock<IUserRepository>();
        _mockBusinessRepRepo = new Mock<IBusinessRepRepository>();
        _mockBusinessClient = new Mock<IBusinessServiceClient>();
        _mockBusinessClaimRepo = new Mock<IBusinessClaimRepository>();
        _mockBusinessRepo = new Mock<IBusinessRepository>();
        _mockAuth0 = new Mock<IAuth0ManagementService>();
        _mockConfig = new Mock<IConfiguration>();
        _mockRegVerification = new Mock<IRegistrationVerificationService>();

        _mockConfig.Setup(c => c["Auth0:Roles:BusinessUser"]).Returns("auth0_biz_role");

        _mockAuth0
            .Setup(a => a.CreateUserAndAssignRoleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Auth0Id);

        _mockBusinessClaimRepo.Setup(r => r.GetByBusinessIdAsync(BusinessId))
            .ReturnsAsync(new BusinessClaim { BusinessId = BusinessId, BusinessName = BusinessName, Status = 7, ExpiresAt = DateTime.UtcNow.AddHours(12) });
        _mockBusinessRepo.Setup(r => r.UpdateOwnerAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>())).Returns(Task.CompletedTask);
        _mockBusinessRepo.Setup(r => r.UpdateStatusAsync(It.IsAny<Guid>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        _mockUserRepo.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockUserRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => new User(BusinessName, "owner@biz.com", "+2348012345678", "pw", "business_user", null, Auth0Id));
        _mockUserRepo.Setup(r => r.SetUserIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(Task.CompletedTask);

        _mockBusinessRepRepo.Setup(r => r.AddAsync(It.IsAny<BusinessRep>())).Returns(Task.CompletedTask);

        _mockRegVerification
            .Setup(s => s.SendVerificationEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var mockCache = new Mock<IMemoryCache>();
        object? cacheValue = null;
        mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out cacheValue)).Returns(false);
        mockCache.Setup(c => c.CreateEntry(It.IsAny<object>())).Returns(Mock.Of<ICacheEntry>());

        _service = new Application.Services.UserService(
            _mockUserRepo.Object,
            _mockBusinessRepRepo.Object,
            _mockBusinessClient.Object,
            _mockBusinessClaimRepo.Object,
            _mockBusinessRepo.Object,
            new Mock<ISupportUserProfileRepository>().Object,
            new Mock<IEndUserProfileRepository>().Object,
            new Mock<IUserSettingsRepository>().Object,
            new Mock<IBadgeService>().Object,
            new Mock<IPointsService>().Object,
            new Mock<IReferralService>().Object,
            _mockAuth0.Object,
            _mockConfig.Object,
            mockCache.Object,
            _mockRegVerification.Object
        );
    }

    // =========================================================================
    // Happy path
    // =========================================================================

    [Test]
    public async Task RegisterBusinessAfterClaimAsync_ShouldReturnResult_WhenSuccessful()
    {
        // Arrange
        var dto = new RegisterBusinessDto(BusinessId, "owner@biz.com", "Password1!", "+2348012345678");

        // Act
        var result = await _service.RegisterBusinessAfterClaimAsync(dto);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.BusinessId, Is.EqualTo(BusinessId));
        Assert.That(result.Username, Is.EqualTo(BusinessName));
        Assert.That(result.Email, Is.EqualTo("owner@biz.com"));
        Assert.That(result.Auth0UserId, Is.EqualTo(Auth0Id));
    }

    [Test]
    public async Task RegisterBusinessAfterClaimAsync_ShouldCreateUserWithBusinessName()
    {
        // Arrange
        var dto = new RegisterBusinessDto(BusinessId, "owner@biz.com", "Password1!", null);
        User? createdUser = null;
        _mockUserRepo.Setup(r => r.AddAsync(It.IsAny<User>()))
            .Callback<User>(u => createdUser = u)
            .Returns(Task.CompletedTask);

        // Act
        await _service.RegisterBusinessAfterClaimAsync(dto);

        // Assert
        Assert.That(createdUser, Is.Not.Null);
        Assert.That(createdUser!.Username, Is.EqualTo(BusinessName));
        Assert.That(createdUser.UserType, Is.EqualTo("business_user"));
    }

    [Test]
    public async Task RegisterBusinessAfterClaimAsync_ShouldLinkUserToBusiness()
    {
        // Arrange
        var dto = new RegisterBusinessDto(BusinessId, "owner@biz.com", "Password1!", null);

        // Act
        await _service.RegisterBusinessAfterClaimAsync(dto);

        // Assert — SetUserIdAsync called with the new user's id and the businessId
        _mockUserRepo.Verify(r => r.SetUserIdAsync(It.IsAny<Guid>(), BusinessId), Times.Once);
    }

    [Test]
    public async Task RegisterBusinessAfterClaimAsync_ShouldUpdateBusinessOwner()
    {
        // Arrange
        var dto = new RegisterBusinessDto(BusinessId, "owner@biz.com", "Password1!", "+2348012345678");

        // Act
        await _service.RegisterBusinessAfterClaimAsync(dto);

        // Assert
        _mockBusinessRepo.Verify(r => r.UpdateOwnerAsync(
            BusinessId, It.IsAny<Guid>(), "owner@biz.com", "+2348012345678"), Times.Once);
    }

    [Test]
    public async Task RegisterBusinessAfterClaimAsync_ShouldCreateBusinessRep()
    {
        // Arrange
        var dto = new RegisterBusinessDto(BusinessId, "owner@biz.com", "Password1!", null);

        // Act
        await _service.RegisterBusinessAfterClaimAsync(dto);

        // Assert
        _mockBusinessRepRepo.Verify(r => r.AddAsync(It.Is<BusinessRep>(b => b.BusinessId == BusinessId)), Times.Once);
    }

    [Test]
    public async Task RegisterBusinessAfterClaimAsync_ShouldSendVerificationEmail()
    {
        // Arrange
        var dto = new RegisterBusinessDto(BusinessId, "owner@biz.com", "Password1!", null);

        // Act
        await _service.RegisterBusinessAfterClaimAsync(dto);

        // Assert
        _mockRegVerification.Verify(s =>
            s.SendVerificationEmailAsync("owner@biz.com", BusinessName, "business_user"), Times.Once);
    }

    // =========================================================================
    // Error paths
    // =========================================================================

    [Test]
    public void RegisterBusinessAfterClaimAsync_ShouldThrowBusinessNotFoundException_WhenClaimNotFound()
    {
        // Arrange — no claim record at all
        _mockBusinessClaimRepo.Setup(r => r.GetByBusinessIdAsync(BusinessId)).ReturnsAsync((BusinessClaim?)null);
        var dto = new RegisterBusinessDto(BusinessId, "owner@biz.com", "Password1!", null);

        // Act & Assert
        Assert.ThrowsAsync<BusinessNotFoundException>(() => _service.RegisterBusinessAfterClaimAsync(dto));
    }

    [Test]
    public void RegisterBusinessAfterClaimAsync_ShouldThrowBusinessClaimNotApprovedException_WhenStatusIsNot7()
    {
        // Arrange — claim exists but status is not 7 (e.g. 5 = pending)
        _mockBusinessClaimRepo.Setup(r => r.GetByBusinessIdAsync(BusinessId))
            .ReturnsAsync(new BusinessClaim { BusinessId = BusinessId, Status = 5, ExpiresAt = DateTime.UtcNow.AddHours(12) });
        var dto = new RegisterBusinessDto(BusinessId, "owner@biz.com", "Password1!", null);

        // Act & Assert
        Assert.ThrowsAsync<BusinessClaimNotApprovedException>(() => _service.RegisterBusinessAfterClaimAsync(dto));
    }

    [Test]
    public void RegisterBusinessAfterClaimAsync_ShouldThrowBusinessClaimExpiredException_WhenClaimExpired()
    {
        // Arrange — claim status is approved but expires_at is in the past
        _mockBusinessClaimRepo.Setup(r => r.GetByBusinessIdAsync(BusinessId))
            .ReturnsAsync(new BusinessClaim { BusinessId = BusinessId, Status = 7, ExpiresAt = DateTime.UtcNow.AddHours(-1) });
        var dto = new RegisterBusinessDto(BusinessId, "owner@biz.com", "Password1!", null);

        // Act & Assert
        Assert.ThrowsAsync<BusinessClaimExpiredException>(() => _service.RegisterBusinessAfterClaimAsync(dto));
    }

    [Test]
    public void RegisterBusinessAfterClaimAsync_ShouldThrowUserCreationFailed_WhenUserNotSaved()
    {
        // Arrange
        _mockUserRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);
        var dto = new RegisterBusinessDto(BusinessId, "owner@biz.com", "Password1!", null);

        // Act & Assert
        Assert.ThrowsAsync<UserCreationFailedException>(() => _service.RegisterBusinessAfterClaimAsync(dto));
    }

    [Test]
    public async Task RegisterBusinessAfterClaimAsync_ShouldSetStatusToClaimed()
    {
        // Arrange
        var dto = new RegisterBusinessDto(BusinessId, "owner@biz.com", "Password1!", null);

        // Act
        await _service.RegisterBusinessAfterClaimAsync(dto);

        // Assert
        _mockBusinessRepo.Verify(r => r.UpdateStatusAsync(BusinessId, "claimed"), Times.Once);
    }
}
