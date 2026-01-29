using Moq;
using UserService.Application.DTOs.PasswordReset;
using UserService.Application.Interfaces;
using UserService.Application.Services;
using UserService.Application.Services.Auth0;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Application.Tests.Services;

[TestFixture]
public class PasswordResetServiceTests
{
    private Mock<IUserRepository> _mockUserRepo = null!;
    private Mock<IPasswordResetRequestRepository> _mockPasswordResetRepo = null!;
    private Mock<IAuth0ManagementService> _mockAuth0 = null!;
    private Mock<IBusinessServiceClient> _mockBusinessClient = null!;
    private Mock<INotificationServiceClient> _mockNotificationClient = null!;
    private Mock<IEncryptionService> _mockEncryption = null!;
    private PasswordResetService _service = null!;

    [SetUp]
    public void Setup()
    {
        _mockUserRepo = new Mock<IUserRepository>();
        _mockPasswordResetRepo = new Mock<IPasswordResetRequestRepository>();
        _mockAuth0 = new Mock<IAuth0ManagementService>();
        _mockBusinessClient = new Mock<IBusinessServiceClient>();
        _mockNotificationClient = new Mock<INotificationServiceClient>();
        _mockEncryption = new Mock<IEncryptionService>();

        _service = new PasswordResetService(
            _mockUserRepo.Object,
            _mockPasswordResetRepo.Object,
            _mockAuth0.Object,
            _mockBusinessClient.Object,
            _mockNotificationClient.Object,
            _mockEncryption.Object
        );
    }

    // ========== ResetEmailAsync Tests ==========

    [Test]
    public async Task ResetEmailAsync_ShouldReturnFalse_WhenEmailDoesNotExist()
    {
        // Arrange
        _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var request = new ResetEmailRequest("old@example.com", "new@example.com");

        // Act
        var (success, message) = await _service.ResetEmailAsync(request);

        // Assert
        Assert.That(success, Is.False);
        Assert.That(message, Is.EqualTo("Email does not exist"));
    }

    [Test]
    public async Task ResetEmailAsync_ShouldReturnFalse_WhenAuth0UpdateFails()
    {
        // Arrange
        var user = CreateTestUser("end_user");
        _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        _mockAuth0.Setup(a => a.UpdateEmailAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);

        var request = new ResetEmailRequest("old@example.com", "new@example.com");

        // Act
        var (success, message) = await _service.ResetEmailAsync(request);

        // Assert
        Assert.That(success, Is.False);
        Assert.That(message, Is.EqualTo("Failed to update email on Auth0"));
    }

    [Test]
    public async Task ResetEmailAsync_ShouldSucceed_ForEndUser()
    {
        // Arrange
        var user = CreateTestUser("end_user");
        _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        _mockAuth0.Setup(a => a.UpdateEmailAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

        var request = new ResetEmailRequest("old@example.com", "new@example.com");

        // Act
        var (success, message) = await _service.ResetEmailAsync(request);

        // Assert
        Assert.That(success, Is.True);
        Assert.That(message, Is.EqualTo("Email updated successfully"));
        _mockUserRepo.Verify(r => r.UpdateEmailAsync(user.Id, "new@example.com"), Times.Once);
        _mockBusinessClient.Verify(b => b.UpdateBusinessEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task ResetEmailAsync_ShouldUpdateBusinessEmail_ForBusinessUser()
    {
        // Arrange
        var user = CreateTestUser("business_user");
        _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        _mockAuth0.Setup(a => a.UpdateEmailAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
        _mockBusinessClient.Setup(b => b.UpdateBusinessEmailAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

        var request = new ResetEmailRequest("old@example.com", "new@example.com");

        // Act
        var (success, message) = await _service.ResetEmailAsync(request);

        // Assert
        Assert.That(success, Is.True);
        _mockBusinessClient.Verify(b => b.UpdateBusinessEmailAsync("old@example.com", "new@example.com"), Times.Once);
    }

    // ========== RequestPasswordResetAsync Tests ==========

    [Test]
    public async Task RequestPasswordResetAsync_ShouldReturnFalse_WhenInvalidType()
    {
        // Arrange
        var request = new RequestPasswordResetRequest("test@example.com", "invalid");

        // Act
        var (success, message) = await _service.RequestPasswordResetAsync(request);

        // Assert
        Assert.That(success, Is.False);
        Assert.That(message, Is.EqualTo("Type must be 'email' or 'sms'"));
    }

    [Test]
    public async Task RequestPasswordResetAsync_ShouldReturnFalse_WhenEmailNotFound()
    {
        // Arrange
        _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var request = new RequestPasswordResetRequest("notfound@example.com", "email");

        // Act
        var (success, message) = await _service.RequestPasswordResetAsync(request);

        // Assert
        Assert.That(success, Is.False);
        Assert.That(message, Is.EqualTo("Id does not exist"));
    }

    [Test]
    public async Task RequestPasswordResetAsync_ShouldReturnFalse_WhenPhoneNotFound()
    {
        // Arrange
        _mockUserRepo.Setup(r => r.GetByPhoneAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var request = new RequestPasswordResetRequest("+2341234567890", "sms");

        // Act
        var (success, message) = await _service.RequestPasswordResetAsync(request);

        // Assert
        Assert.That(success, Is.False);
        Assert.That(message, Is.EqualTo("Id does not exist"));
    }

    [Test]
    public async Task RequestPasswordResetAsync_ShouldReturnFalse_WhenOtpFails()
    {
        // Arrange
        var user = CreateTestUser("end_user");
        _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        _mockNotificationClient.Setup(n => n.CreateOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var request = new RequestPasswordResetRequest("test@example.com", "email");

        // Act
        var (success, message) = await _service.RequestPasswordResetAsync(request);

        // Assert
        Assert.That(success, Is.False);
        Assert.That(message, Is.EqualTo("Failed to send OTP"));
    }

    [Test]
    public async Task RequestPasswordResetAsync_ShouldSucceed_WhenOtpSent()
    {
        // Arrange
        var user = CreateTestUser("end_user");
        _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        _mockNotificationClient.Setup(n => n.CreateOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var request = new RequestPasswordResetRequest("test@example.com", "email");

        // Act
        var (success, message) = await _service.RequestPasswordResetAsync(request);

        // Assert
        Assert.That(success, Is.True);
        Assert.That(message, Is.EqualTo("OTP sent successfully"));
        _mockNotificationClient.Verify(n => n.CreateOtpAsync("test@example.com", "email", "resetpassword"), Times.Once);
        _mockPasswordResetRepo.Verify(r => r.AddAsync(It.IsAny<PasswordResetRequest>()), Times.Once);
    }

    // ========== ResetPasswordAsync Tests ==========

    [Test]
    public async Task ResetPasswordAsync_ShouldReturnFalse_WhenNoVerifiedRequest()
    {
        // Arrange
        _mockPasswordResetRepo.Setup(r => r.GetByIdentifierAsync(It.IsAny<string>())).ReturnsAsync((PasswordResetRequest?)null);

        var request = new ResetPasswordRequest("test@example.com", "encryptedPassword");

        // Act
        var (success, message) = await _service.ResetPasswordAsync(request);

        // Assert
        Assert.That(success, Is.False);
        Assert.That(message, Is.EqualTo("No verified password reset request found for this identifier"));
    }

    [Test]
    public async Task ResetPasswordAsync_ShouldReturnFalse_WhenDecryptionFails()
    {
        // Arrange
        var resetRequest = new PasswordResetRequest(Guid.NewGuid(), "test@example.com", "email");
        // Use reflection to set IsVerified to true for testing
        typeof(PasswordResetRequest).GetProperty("IsVerified")!.SetValue(resetRequest, true);

        var user = CreateTestUser("end_user");
        _mockPasswordResetRepo.Setup(r => r.GetByIdentifierAsync(It.IsAny<string>())).ReturnsAsync(resetRequest);
        _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        _mockEncryption.Setup(e => e.Decrypt(It.IsAny<string>())).Throws(new FormatException("Invalid encryption"));

        var request = new ResetPasswordRequest("test@example.com", "invalidEncryptedPassword");

        // Act
        var (success, message) = await _service.ResetPasswordAsync(request);

        // Assert
        Assert.That(success, Is.False);
        Assert.That(message, Is.EqualTo("Invalid password format"));
    }

    [Test]
    public async Task ResetPasswordAsync_ShouldReturnFalse_WhenAuth0UpdateFails()
    {
        // Arrange
        var resetRequest = new PasswordResetRequest(Guid.NewGuid(), "test@example.com", "email");
        typeof(PasswordResetRequest).GetProperty("IsVerified")!.SetValue(resetRequest, true);

        var user = CreateTestUser("end_user");
        _mockPasswordResetRepo.Setup(r => r.GetByIdentifierAsync(It.IsAny<string>())).ReturnsAsync(resetRequest);
        _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        _mockEncryption.Setup(e => e.Decrypt(It.IsAny<string>())).Returns("decryptedPassword");
        _mockAuth0.Setup(a => a.UpdatePasswordAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);

        var request = new ResetPasswordRequest("test@example.com", "encryptedPassword");

        // Act
        var (success, message) = await _service.ResetPasswordAsync(request);

        // Assert
        Assert.That(success, Is.False);
        Assert.That(message, Is.EqualTo("Failed to update password"));
    }

    [Test]
    public async Task ResetPasswordAsync_ShouldSucceed_WhenValidRequest()
    {
        // Arrange
        var resetRequest = new PasswordResetRequest(Guid.NewGuid(), "test@example.com", "email");
        typeof(PasswordResetRequest).GetProperty("IsVerified")!.SetValue(resetRequest, true);

        var user = CreateTestUser("end_user");
        _mockPasswordResetRepo.Setup(r => r.GetByIdentifierAsync(It.IsAny<string>())).ReturnsAsync(resetRequest);
        _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        _mockEncryption.Setup(e => e.Decrypt(It.IsAny<string>())).Returns("decryptedPassword");
        _mockAuth0.Setup(a => a.UpdatePasswordAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

        var request = new ResetPasswordRequest("test@example.com", "encryptedPassword");

        // Act
        var (success, message) = await _service.ResetPasswordAsync(request);

        // Assert
        Assert.That(success, Is.True);
        Assert.That(message, Is.EqualTo("Password updated"));
        _mockAuth0.Verify(a => a.UpdatePasswordAsync(user.Auth0UserId, "decryptedPassword"), Times.Once);
    }

    // Helper method to create test users
    private User CreateTestUser(string userType)
    {
        return new User(
            username: "testuser",
            email: "test@example.com",
            phone: "+2341234567890",
            password: "password",
            userType: userType,
            address: null,
            auth0UserId: "auth0|123456",
            loginType: "email-password"
        );
    }
}
