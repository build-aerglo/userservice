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
    private Mock<IAuth0UserLoginService> _mockAuth0UserLogin = null!;
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
        _mockAuth0UserLogin = new Mock<IAuth0UserLoginService>();
        _mockBusinessClient = new Mock<IBusinessServiceClient>();
        _mockNotificationClient = new Mock<INotificationServiceClient>();
        _mockEncryption = new Mock<IEncryptionService>();

        _service = new PasswordResetService(
            _mockUserRepo.Object,
            _mockPasswordResetRepo.Object,
            _mockAuth0.Object,
            _mockAuth0UserLogin.Object,
            _mockBusinessClient.Object,
            _mockNotificationClient.Object,
            _mockEncryption.Object
        );
    }

    // ========== ResetEmailAsync Tests ==========

    [Test]
    public async Task ResetEmailAsync_ShouldReturnFalse_WhenEmailDoesNotExist()
    {
        _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var request = new ResetEmailRequest("old@example.com", "new@example.com");

        var (success, message) = await _service.ResetEmailAsync(request);

        Assert.That(success, Is.False);
        Assert.That(message, Is.EqualTo("Email does not exist"));
    }

    [Test]
    public async Task ResetEmailAsync_ShouldReturnFalse_WhenAuth0UpdateFails()
    {
        var user = CreateTestUser("end_user");
        _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        _mockAuth0.Setup(a => a.UpdateEmailAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);

        var request = new ResetEmailRequest("old@example.com", "new@example.com");

        var (success, message) = await _service.ResetEmailAsync(request);

        Assert.That(success, Is.False);
        Assert.That(message, Is.EqualTo("Failed to update email on Auth0"));
    }

    [Test]
    public async Task ResetEmailAsync_ShouldSucceed_ForEndUser()
    {
        var user = CreateTestUser("end_user");
        _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        _mockAuth0.Setup(a => a.UpdateEmailAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

        var request = new ResetEmailRequest("old@example.com", "new@example.com");

        var (success, message) = await _service.ResetEmailAsync(request);

        Assert.That(success, Is.True);
        Assert.That(message, Is.EqualTo("Email updated successfully"));
        _mockUserRepo.Verify(r => r.UpdateEmailAsync(user.Id, "new@example.com"), Times.Once);
        _mockBusinessClient.Verify(b => b.UpdateBusinessEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task ResetEmailAsync_ShouldUpdateBusinessEmail_ForBusinessUser()
    {
        var user = CreateTestUser("business_user");
        _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        _mockAuth0.Setup(a => a.UpdateEmailAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
        _mockBusinessClient.Setup(b => b.UpdateBusinessEmailAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

        var request = new ResetEmailRequest("old@example.com", "new@example.com");

        var (success, message) = await _service.ResetEmailAsync(request);

        Assert.That(success, Is.True);
        _mockBusinessClient.Verify(b => b.UpdateBusinessEmailAsync("old@example.com", "new@example.com"), Times.Once);
    }

    // ========== RequestPasswordResetAsync Tests ==========

    [Test]
    public async Task RequestPasswordResetAsync_ShouldReturnFalse_WhenInvalidType()
    {
        var request = new RequestPasswordResetRequest("test@example.com", "invalid");

        var (success, message) = await _service.RequestPasswordResetAsync(request);

        Assert.That(success, Is.False);
        Assert.That(message, Is.EqualTo("Type must be 'email' or 'sms'"));
    }

    [Test]
    public async Task RequestPasswordResetAsync_ShouldReturnFalse_WhenEmailNotFound()
    {
        _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var request = new RequestPasswordResetRequest("notfound@example.com", "email");

        var (success, message) = await _service.RequestPasswordResetAsync(request);

        Assert.That(success, Is.False);
        Assert.That(message, Is.EqualTo("Id does not exist"));
    }

    [Test]
    public async Task RequestPasswordResetAsync_ShouldReturnFalse_WhenOtpFails()
    {
        var user = CreateTestUser("end_user");
        _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        _mockNotificationClient.Setup(n => n.CreateOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var request = new RequestPasswordResetRequest("test@example.com", "email");

        var (success, message) = await _service.RequestPasswordResetAsync(request);

        Assert.That(success, Is.False);
        Assert.That(message, Is.EqualTo("Failed to send OTP"));
    }

    [Test]
    public async Task RequestPasswordResetAsync_ShouldSucceed_WhenOtpSent()
    {
        var user = CreateTestUser("end_user");
        _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        _mockNotificationClient.Setup(n => n.CreateOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var request = new RequestPasswordResetRequest("test@example.com", "email");

        var (success, message) = await _service.RequestPasswordResetAsync(request);

        Assert.That(success, Is.True);
        Assert.That(message, Is.EqualTo("OTP sent successfully"));
        _mockNotificationClient.Verify(n => n.CreateOtpAsync("test@example.com", "email", "resetpassword"), Times.Once);
    }

    // ========== ResetPasswordAsync Tests ==========

    [Test]
    public async Task ResetPasswordAsync_ShouldReturnFalse_WhenNoResetRequest()
    {
        _mockPasswordResetRepo.Setup(r => r.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((PasswordResetRequest?)null);

        var request = new ResetPasswordRequest("test@example.com", "encryptedPassword");

        var (success, message) = await _service.ResetPasswordAsync(request);

        Assert.That(success, Is.False);
        Assert.That(message, Is.EqualTo("No password reset request found"));
    }

    [Test]
    public async Task ResetPasswordAsync_ShouldReturnFalse_WhenUserNotFound()
    {
        var resetRequest = new PasswordResetRequest("test@example.com");

        _mockPasswordResetRepo.Setup(r => r.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(resetRequest);
        _mockUserRepo.Setup(r => r.GetByEmailOrPhoneAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var request = new ResetPasswordRequest("test@example.com", "encryptedPassword");

        var (success, message) = await _service.ResetPasswordAsync(request);

        Assert.That(success, Is.False);
        Assert.That(message, Is.EqualTo("User not found"));
    }

    [Test]
    public async Task ResetPasswordAsync_ShouldReturnFalse_WhenDecryptionFails()
    {
        var user = CreateTestUser("end_user");
        var resetRequest = new PasswordResetRequest("test@example.com");

        _mockPasswordResetRepo.Setup(r => r.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(resetRequest);
        _mockUserRepo.Setup(r => r.GetByEmailOrPhoneAsync(It.IsAny<string>())).ReturnsAsync(user);
        _mockEncryption.Setup(e => e.Decrypt(It.IsAny<string>())).Throws(new FormatException("Invalid encryption"));

        var request = new ResetPasswordRequest("test@example.com", "invalidEncryptedPassword");

        var (success, message) = await _service.ResetPasswordAsync(request);

        Assert.That(success, Is.False);
        Assert.That(message, Does.StartWith("Invalid password format:"));
    }

    [Test]
    public async Task ResetPasswordAsync_ShouldReturnFalse_WhenAuth0UpdateFails()
    {
        var user = CreateTestUser("end_user");
        var resetRequest = new PasswordResetRequest("test@example.com");

        _mockPasswordResetRepo.Setup(r => r.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(resetRequest);
        _mockUserRepo.Setup(r => r.GetByEmailOrPhoneAsync(It.IsAny<string>())).ReturnsAsync(user);
        _mockEncryption.Setup(e => e.Decrypt(It.IsAny<string>())).Returns("decryptedPassword");
        _mockAuth0.Setup(a => a.UpdatePasswordAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);

        var request = new ResetPasswordRequest("test@example.com", "encryptedPassword");

        var (success, message) = await _service.ResetPasswordAsync(request);

        Assert.That(success, Is.False);
        Assert.That(message, Is.EqualTo("Failed to update password"));
    }

    [Test]
    public async Task ResetPasswordAsync_ShouldSucceed_WhenValidRequest()
    {
        var user = CreateTestUser("end_user");
        var resetRequest = new PasswordResetRequest("test@example.com");

        _mockPasswordResetRepo.Setup(r => r.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(resetRequest);
        _mockUserRepo.Setup(r => r.GetByEmailOrPhoneAsync(It.IsAny<string>())).ReturnsAsync(user);
        _mockEncryption.Setup(e => e.Decrypt(It.IsAny<string>())).Returns("decryptedPassword");
        _mockAuth0.Setup(a => a.UpdatePasswordAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

        var request = new ResetPasswordRequest("test@example.com", "encryptedPassword");

        var (success, message) = await _service.ResetPasswordAsync(request);

        Assert.That(success, Is.True);
        Assert.That(message, Is.EqualTo("Password updated"));
        _mockAuth0.Verify(a => a.UpdatePasswordAsync(user.Auth0UserId, "decryptedPassword"), Times.Once);
        _mockPasswordResetRepo.Verify(r => r.DeleteByIdAsync("test@example.com"), Times.Once);
    }

    // ========== UpdatePasswordAsync Tests ==========

    [Test]
    public async Task UpdatePasswordAsync_ShouldReturnFalse_WhenUserNotFound()
    {
        _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var request = new UpdatePasswordRequest("notfound@example.com", "encryptedOld", "encryptedNew");

        var (success, message) = await _service.UpdatePasswordAsync(request);

        Assert.That(success, Is.False);
        Assert.That(message, Is.EqualTo("User not found"));
    }

    [Test]
    public async Task UpdatePasswordAsync_ShouldReturnFalse_WhenUserNotLinkedToAuth0()
    {
        var user = new User(
            username: "testuser",
            email: "test@example.com",
            phone: "+2341234567890",
            password: "password",
            userType: "end_user",
            address: null,
            auth0UserId: null,
            loginType: "email-password"
        );
        _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);

        var request = new UpdatePasswordRequest("test@example.com", "encryptedOld", "encryptedNew");

        var (success, message) = await _service.UpdatePasswordAsync(request);

        Assert.That(success, Is.False);
        Assert.That(message, Is.EqualTo("User account is not linked to Auth0"));
    }

    [Test]
    public async Task UpdatePasswordAsync_ShouldReturnFalse_WhenOldPasswordDecryptionFails()
    {
        var user = CreateTestUser("end_user");
        _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        _mockEncryption.Setup(e => e.Decrypt("invalidOldPassword")).Throws(new FormatException("Invalid encryption"));

        var request = new UpdatePasswordRequest("test@example.com", "invalidOldPassword", "encryptedNew");

        var (success, message) = await _service.UpdatePasswordAsync(request);

        Assert.That(success, Is.False);
        Assert.That(message, Is.EqualTo("Invalid old password format"));
    }

    [Test]
    public async Task UpdatePasswordAsync_ShouldReturnFalse_WhenNewPasswordDecryptionFails()
    {
        var user = CreateTestUser("end_user");
        _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        _mockEncryption.Setup(e => e.Decrypt("encryptedOld")).Returns("decryptedOld");
        _mockEncryption.Setup(e => e.Decrypt("invalidNewPassword")).Throws(new FormatException("Invalid encryption"));

        var request = new UpdatePasswordRequest("test@example.com", "encryptedOld", "invalidNewPassword");

        var (success, message) = await _service.UpdatePasswordAsync(request);

        Assert.That(success, Is.False);
        Assert.That(message, Is.EqualTo("Invalid new password format"));
    }

    [Test]
    public async Task UpdatePasswordAsync_ShouldReturnFalse_WhenCurrentPasswordIncorrect()
    {
        var user = CreateTestUser("end_user");
        _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        _mockEncryption.Setup(e => e.Decrypt("encryptedOld")).Returns("wrongPassword");
        _mockEncryption.Setup(e => e.Decrypt("encryptedNew")).Returns("newPassword");
        _mockAuth0UserLogin.Setup(a => a.LoginAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Invalid credentials"));

        var request = new UpdatePasswordRequest("test@example.com", "encryptedOld", "encryptedNew");

        var (success, message) = await _service.UpdatePasswordAsync(request);

        Assert.That(success, Is.False);
        Assert.That(message, Is.EqualTo("Current password is incorrect"));
    }

    [Test]
    public async Task UpdatePasswordAsync_ShouldReturnFalse_WhenAuth0UpdateFails()
    {
        var user = CreateTestUser("end_user");
        _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        _mockEncryption.Setup(e => e.Decrypt("encryptedOld")).Returns("oldPassword");
        _mockEncryption.Setup(e => e.Decrypt("encryptedNew")).Returns("newPassword");
        _mockAuth0UserLogin.Setup(a => a.LoginAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new UserService.Application.DTOs.TokenResponse());
        _mockAuth0.Setup(a => a.UpdatePasswordAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);

        var request = new UpdatePasswordRequest("test@example.com", "encryptedOld", "encryptedNew");

        var (success, message) = await _service.UpdatePasswordAsync(request);

        Assert.That(success, Is.False);
        Assert.That(message, Is.EqualTo("Failed to update password"));
    }

    [Test]
    public async Task UpdatePasswordAsync_ShouldSucceed_WhenValidRequest()
    {
        var user = CreateTestUser("end_user");
        _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        _mockEncryption.Setup(e => e.Decrypt("encryptedOld")).Returns("oldPassword");
        _mockEncryption.Setup(e => e.Decrypt("encryptedNew")).Returns("newPassword");
        _mockAuth0UserLogin.Setup(a => a.LoginAsync("test@example.com", "oldPassword"))
            .ReturnsAsync(new UserService.Application.DTOs.TokenResponse());
        _mockAuth0.Setup(a => a.UpdatePasswordAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

        var request = new UpdatePasswordRequest("test@example.com", "encryptedOld", "encryptedNew");

        var (success, message) = await _service.UpdatePasswordAsync(request);

        Assert.That(success, Is.True);
        Assert.That(message, Is.EqualTo("Password updated successfully"));
        _mockAuth0UserLogin.Verify(a => a.LoginAsync("test@example.com", "oldPassword"), Times.Once);
        _mockAuth0.Verify(a => a.UpdatePasswordAsync(user.Auth0UserId, "newPassword"), Times.Once);
    }

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
