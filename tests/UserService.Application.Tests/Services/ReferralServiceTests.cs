using Moq;
using UserService.Application.DTOs.Referral;
using UserService.Application.Interfaces;
using UserService.Application.Services;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Domain.Repositories;

namespace UserService.Application.Tests.Services;

[TestFixture]
public class ReferralServiceTests
{
    private Mock<IUserReferralCodeRepository> _mockReferralCodeRepository = null!;
    private Mock<IReferralRepository> _mockReferralRepository = null!;
    private Mock<IUserRepository> _mockUserRepository = null!;
    private Mock<IPointsService> _mockPointsService = null!;
    private Mock<IVerificationService> _mockVerificationService = null!;
    private ReferralService _service = null!;

    [SetUp]
    public void Setup()
    {
        _mockReferralCodeRepository = new Mock<IUserReferralCodeRepository>();
        _mockReferralRepository = new Mock<IReferralRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPointsService = new Mock<IPointsService>();
        _mockVerificationService = new Mock<IVerificationService>();

        _service = new ReferralService(
            _mockReferralCodeRepository.Object,
            _mockReferralRepository.Object,
            _mockUserRepository.Object,
            _mockPointsService.Object,
            _mockVerificationService.Object
        );
    }

    [Test]
    public async Task GenerateReferralCodeAsync_ShouldGenerateCode_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User("john_doe", "john@example.com", "1234567890", "password", "end_user", "123 Main St", "auth0|123");
        var dto = new GenerateReferralCodeDto(userId);

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockReferralCodeRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((UserReferralCode?)null);
        _mockReferralCodeRepository.Setup(r => r.CodeExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        _mockReferralCodeRepository.Setup(r => r.AddAsync(It.IsAny<UserReferralCode>())).Returns(Task.CompletedTask);
        _mockReferralCodeRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new UserReferralCode(userId, "JOHNDOE2024"));

        // Act
        var result = await _service.GenerateReferralCodeAsync(dto);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Code, Is.Not.Empty);
        _mockReferralCodeRepository.Verify(r => r.AddAsync(It.IsAny<UserReferralCode>()), Times.Once);
    }

    [Test]
    public void GenerateReferralCodeAsync_ShouldThrow_WhenUserNotFound()
    {
        // Arrange
        var dto = new GenerateReferralCodeDto(Guid.NewGuid());
        _mockUserRepository.Setup(r => r.GetByIdAsync(dto.UserId)).ReturnsAsync((User?)null);

        // Act & Assert
        Assert.ThrowsAsync<EndUserNotFoundException>(() => _service.GenerateReferralCodeAsync(dto));
    }

    [Test]
    public void GenerateReferralCodeAsync_ShouldThrow_WhenUserAlreadyHasCode()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User("john_doe", "john@example.com", "1234567890", "password", "end_user", "123 Main St", "auth0|123");
        var existingCode = new UserReferralCode(userId, "EXISTING2024");
        var dto = new GenerateReferralCodeDto(userId);

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockReferralCodeRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(existingCode);

        // Act & Assert
        Assert.ThrowsAsync<ReferralCodeAlreadyExistsException>(() => _service.GenerateReferralCodeAsync(dto));
    }

    [Test]
    public async Task ApplyReferralCodeAsync_ShouldApplyCode_WhenValid()
    {
        // Arrange
        var referrerId = Guid.NewGuid();
        var referredUserId = Guid.NewGuid();
        var referralCode = new UserReferralCode(referrerId, "JOHN2024");
        var user = new User("jane_doe", "jane@example.com", "1234567890", "password", "end_user", "123 Main St", "auth0|456");
        var dto = new ApplyReferralCodeDto(referredUserId, "JOHN2024");

        _mockUserRepository.Setup(r => r.GetByIdAsync(referredUserId)).ReturnsAsync(user);
        _mockReferralRepository.Setup(r => r.GetByReferredUserIdAsync(referredUserId)).ReturnsAsync((Referral?)null);
        _mockReferralCodeRepository.Setup(r => r.GetByCodeAsync("JOHN2024")).ReturnsAsync(referralCode);
        _mockReferralRepository.Setup(r => r.AddAsync(It.IsAny<Referral>())).Returns(Task.CompletedTask);
        _mockReferralCodeRepository.Setup(r => r.UpdateAsync(referralCode)).Returns(Task.CompletedTask);

        // Act
        var result = await _service.ApplyReferralCodeAsync(dto);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Success, Is.True);
        Assert.That(result.ReferrerId, Is.EqualTo(referrerId));
    }

    [Test]
    public void ApplyReferralCodeAsync_ShouldThrow_WhenUserAlreadyReferred()
    {
        // Arrange
        var referredUserId = Guid.NewGuid();
        var existingReferral = new Referral(Guid.NewGuid(), referredUserId, "EXISTING");
        var dto = new ApplyReferralCodeDto(referredUserId, "JOHN2024");

        _mockUserRepository.Setup(r => r.GetByIdAsync(referredUserId))
            .ReturnsAsync(new User("jane_doe", "jane@example.com", "1234567890", "password", "end_user", "123 Main St", "auth0|456"));
        _mockReferralRepository.Setup(r => r.GetByReferredUserIdAsync(referredUserId)).ReturnsAsync(existingReferral);

        // Act & Assert
        Assert.ThrowsAsync<UserAlreadyReferredException>(() => _service.ApplyReferralCodeAsync(dto));
    }

    [Test]
    public void ApplyReferralCodeAsync_ShouldThrow_WhenCodeNotFound()
    {
        // Arrange
        var referredUserId = Guid.NewGuid();
        var dto = new ApplyReferralCodeDto(referredUserId, "INVALID");

        _mockUserRepository.Setup(r => r.GetByIdAsync(referredUserId))
            .ReturnsAsync(new User("jane_doe", "jane@example.com", "1234567890", "password", "end_user", "123 Main St", "auth0|456"));
        _mockReferralRepository.Setup(r => r.GetByReferredUserIdAsync(referredUserId)).ReturnsAsync((Referral?)null);
        _mockReferralCodeRepository.Setup(r => r.GetByCodeAsync("INVALID")).ReturnsAsync((UserReferralCode?)null);

        // Act & Assert
        Assert.ThrowsAsync<ReferralCodeNotFoundException>(() => _service.ApplyReferralCodeAsync(dto));
    }

    [Test]
    public void ApplyReferralCodeAsync_ShouldThrow_WhenSelfReferral()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var referralCode = new UserReferralCode(userId, "SELF2024");
        var dto = new ApplyReferralCodeDto(userId, "SELF2024");

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(new User("john_doe", "john@example.com", "1234567890", "password", "end_user", "123 Main St", "auth0|123"));
        _mockReferralRepository.Setup(r => r.GetByReferredUserIdAsync(userId)).ReturnsAsync((Referral?)null);
        _mockReferralCodeRepository.Setup(r => r.GetByCodeAsync("SELF2024")).ReturnsAsync(referralCode);

        // Act & Assert
        Assert.ThrowsAsync<SelfReferralException>(() => _service.ApplyReferralCodeAsync(dto));
    }

    [Test]
    public async Task ProcessReferralReviewAsync_ShouldUpdateReferral_WhenApproved()
    {
        // Arrange
        var referredUserId = Guid.NewGuid();
        var referral = new Referral(Guid.NewGuid(), referredUserId, "CODE2024");
        var dto = new ProcessReferralReviewDto(referredUserId, Guid.NewGuid(), true);

        _mockReferralRepository.Setup(r => r.GetByReferredUserIdAsync(referredUserId)).ReturnsAsync(referral);
        _mockReferralRepository.Setup(r => r.UpdateAsync(referral)).Returns(Task.CompletedTask);

        // Act
        await _service.ProcessReferralReviewAsync(dto);

        // Assert
        _mockReferralRepository.Verify(r => r.UpdateAsync(It.IsAny<Referral>()), Times.Once);
    }

    [Test]
    public async Task ValidateReferralCodeAsync_ShouldReturnTrue_WhenCodeValid()
    {
        // Arrange
        var code = "VALID2024";
        var referralCode = new UserReferralCode(Guid.NewGuid(), code);

        _mockReferralCodeRepository.Setup(r => r.GetByCodeAsync(code)).ReturnsAsync(referralCode);

        // Act
        var result = await _service.ValidateReferralCodeAsync(code);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task ValidateReferralCodeAsync_ShouldReturnFalse_WhenCodeNotFound()
    {
        // Arrange
        var code = "INVALID";
        _mockReferralCodeRepository.Setup(r => r.GetByCodeAsync(code)).ReturnsAsync((UserReferralCode?)null);

        // Act
        var result = await _service.ValidateReferralCodeAsync(code);

        // Assert
        Assert.That(result, Is.False);
    }
}
