using Microsoft.Extensions.Logging;
using Moq;
using UserService.Application.Interfaces;
using UserService.Application.Services;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Domain.Repositories;

namespace UserService.Application.Tests.Services;

[TestFixture]
public class RegistrationVerificationServiceTests
{
    private Mock<IRegistrationVerificationRepository> _mockRegVerificationRepo = null!;
    private Mock<IUserRepository> _mockUserRepo = null!;
    private Mock<IEncryptionService> _mockEncryption = null!;
    private Mock<INotificationServiceClient> _mockNotificationClient = null!;
    private Mock<IBusinessServiceClient> _mockBusinessClient = null!;
    private Mock<ILogger<RegistrationVerificationService>> _mockLogger = null!;
    private RegistrationVerificationService _service = null!;

    [SetUp]
    public void Setup()
    {
        _mockRegVerificationRepo = new Mock<IRegistrationVerificationRepository>();
        _mockUserRepo = new Mock<IUserRepository>();
        _mockEncryption = new Mock<IEncryptionService>();
        _mockNotificationClient = new Mock<INotificationServiceClient>();
        _mockBusinessClient = new Mock<IBusinessServiceClient>();
        _mockLogger = new Mock<ILogger<RegistrationVerificationService>>();

        _service = new RegistrationVerificationService(
            _mockRegVerificationRepo.Object,
            _mockUserRepo.Object,
            _mockEncryption.Object,
            _mockNotificationClient.Object,
            _mockBusinessClient.Object,
            _mockLogger.Object
        );
    }

    // =========================================================================
    // SendVerificationEmailAsync
    // =========================================================================

    [Test]
    public async Task SendVerificationEmailAsync_ShouldDeleteOldEntry_And_PersistNew_And_SendNotification()
    {
        // Arrange
        const string email = "user@example.com";
        const string username = "alice";
        const string userType = "end_user";
        const string encryptedToken = "encrypted-token-abc";

        _mockEncryption.Setup(e => e.Encrypt(email)).Returns(encryptedToken);
        _mockRegVerificationRepo.Setup(r => r.DeleteByEmailAsync(email)).Returns(Task.CompletedTask);
        _mockRegVerificationRepo.Setup(r => r.AddAsync(It.IsAny<RegistrationVerification>())).Returns(Task.CompletedTask);
        _mockNotificationClient
            .Setup(n => n.SendNotificationAsync(
                "registeration", email, "email",
                It.IsAny<object>()))
            .ReturnsAsync(true);

        // Act
        await _service.SendVerificationEmailAsync(email, username, userType);

        // Assert
        _mockRegVerificationRepo.Verify(r => r.DeleteByEmailAsync(email), Times.Once);
        _mockRegVerificationRepo.Verify(r => r.AddAsync(It.Is<RegistrationVerification>(v =>
            v.Email == email && v.Username == username && v.Token == encryptedToken && v.UserType == userType
        )), Times.Once);
        _mockNotificationClient.Verify(n => n.SendNotificationAsync(
            "registeration", email, "email", It.IsAny<object>()), Times.Once);
    }

    [Test]
    public async Task SendVerificationEmailAsync_ShouldIncludeTypeUser_InUrlForEndUser()
    {
        // Arrange
        const string email = "user@example.com";
        const string encryptedToken = "tok";
        object? capturedPayload = null;

        _mockEncryption.Setup(e => e.Encrypt(email)).Returns(encryptedToken);
        _mockRegVerificationRepo.Setup(r => r.DeleteByEmailAsync(email)).Returns(Task.CompletedTask);
        _mockRegVerificationRepo.Setup(r => r.AddAsync(It.IsAny<RegistrationVerification>())).Returns(Task.CompletedTask);
        _mockNotificationClient
            .Setup(n => n.SendNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
            .Callback<string, string, string, object>((_, _, _, payload) => capturedPayload = payload)
            .ReturnsAsync(true);

        // Act
        await _service.SendVerificationEmailAsync(email, "alice", "end_user");

        // Assert
        Assert.That(capturedPayload, Is.Not.Null);
        var url = capturedPayload!.GetType().GetProperty("url")!.GetValue(capturedPayload)!.ToString();
        Assert.That(url, Does.Contain("&type=user"));
    }

    [Test]
    public async Task SendVerificationEmailAsync_ShouldIncludeTypeBusiness_InUrlForBusinessUser()
    {
        // Arrange
        const string email = "biz@example.com";
        const string encryptedToken = "tok";
        object? capturedPayload = null;

        _mockEncryption.Setup(e => e.Encrypt(email)).Returns(encryptedToken);
        _mockRegVerificationRepo.Setup(r => r.DeleteByEmailAsync(email)).Returns(Task.CompletedTask);
        _mockRegVerificationRepo.Setup(r => r.AddAsync(It.IsAny<RegistrationVerification>())).Returns(Task.CompletedTask);
        _mockNotificationClient
            .Setup(n => n.SendNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
            .Callback<string, string, string, object>((_, _, _, payload) => capturedPayload = payload)
            .ReturnsAsync(true);

        // Act
        await _service.SendVerificationEmailAsync(email, "bizcorp", "business_user");

        // Assert
        Assert.That(capturedPayload, Is.Not.Null);
        var url = capturedPayload!.GetType().GetProperty("url")!.GetValue(capturedPayload)!.ToString();
        Assert.That(url, Does.Contain("&type=business"));
    }

    [Test]
    public async Task SendVerificationEmailAsync_ShouldNotThrow_WhenNotificationFails()
    {
        // Arrange
        const string email = "user@example.com";
        _mockEncryption.Setup(e => e.Encrypt(email)).Returns("token");
        _mockRegVerificationRepo.Setup(r => r.DeleteByEmailAsync(email)).Returns(Task.CompletedTask);
        _mockRegVerificationRepo.Setup(r => r.AddAsync(It.IsAny<RegistrationVerification>())).Returns(Task.CompletedTask);
        _mockNotificationClient
            .Setup(n => n.SendNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(false);

        // Act & Assert — should not throw even when notification delivery fails
        Assert.DoesNotThrowAsync(() => _service.SendVerificationEmailAsync(email, "alice", "end_user"));
    }

    // =========================================================================
    // VerifyEmailAsync
    // =========================================================================

    [Test]
    public async Task VerifyEmailAsync_ShouldSucceed_ForEndUser()
    {
        // Arrange
        const string email = "user@example.com";
        const string token = "encrypted-token";
        var userId = Guid.NewGuid();
        var user = new User("alice", email, "08012345678", "pw", "end_user", null, "auth0|1");
        var verification = new RegistrationVerification(email, "alice", token, "end_user");

        _mockEncryption.Setup(e => e.Decrypt(token)).Returns(email);
        _mockUserRepo.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync(user);
        _mockRegVerificationRepo.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync(verification);
        _mockUserRepo.Setup(r => r.UpdateEmailVerifiedAsync(user.Id)).Returns(Task.CompletedTask);
        _mockRegVerificationRepo.Setup(r => r.DeleteByEmailAsync(email)).Returns(Task.CompletedTask);

        // Act
        var result = await _service.VerifyEmailAsync(email, token);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Message, Does.Contain("verified"));
        _mockUserRepo.Verify(r => r.UpdateEmailVerifiedAsync(user.Id), Times.Once);
        _mockRegVerificationRepo.Verify(r => r.DeleteByEmailAsync(email), Times.Once);
        _mockBusinessClient.Verify(b => b.GetBusinessIdByEmailAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task VerifyEmailAsync_ShouldCallBusinessService_ForBusinessUser()
    {
        // Arrange
        const string email = "biz@example.com";
        const string token = "encrypted-token";
        var businessId = Guid.NewGuid();
        var user = new User("bizcorp", email, "08012345678", "pw", "business_user", null, "auth0|2");
        var verification = new RegistrationVerification(email, "bizcorp", token, "business_user");

        _mockEncryption.Setup(e => e.Decrypt(token)).Returns(email);
        _mockUserRepo.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync(user);
        _mockRegVerificationRepo.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync(verification);
        _mockBusinessClient.Setup(b => b.GetBusinessIdByEmailAsync(email)).ReturnsAsync(businessId);
        _mockBusinessClient.Setup(b => b.MarkBusinessEmailVerifiedAsync(businessId, email)).ReturnsAsync(true);
        _mockUserRepo.Setup(r => r.UpdateEmailVerifiedAsync(user.Id)).Returns(Task.CompletedTask);
        _mockRegVerificationRepo.Setup(r => r.DeleteByEmailAsync(email)).Returns(Task.CompletedTask);

        // Act
        var result = await _service.VerifyEmailAsync(email, token);

        // Assert
        Assert.That(result.Success, Is.True);
        _mockBusinessClient.Verify(b => b.GetBusinessIdByEmailAsync(email), Times.Once);
        _mockBusinessClient.Verify(b => b.MarkBusinessEmailVerifiedAsync(businessId, email), Times.Once);
    }

    [Test]
    public void VerifyEmailAsync_ShouldThrowInvalidVerificationToken_WhenDecryptFails()
    {
        // Arrange
        const string email = "user@example.com";
        const string token = "bad-token";

        _mockEncryption.Setup(e => e.Decrypt(token)).Throws(new Exception("Decryption failed"));

        // Act & Assert
        Assert.ThrowsAsync<InvalidVerificationTokenException>(() => _service.VerifyEmailAsync(email, token));
    }

    [Test]
    public void VerifyEmailAsync_ShouldThrowInvalidVerificationToken_WhenEmailMismatch()
    {
        // Arrange
        const string email = "user@example.com";
        const string token = "token";

        _mockEncryption.Setup(e => e.Decrypt(token)).Returns("other@example.com");

        // Act & Assert
        Assert.ThrowsAsync<InvalidVerificationTokenException>(() => _service.VerifyEmailAsync(email, token));
    }

    [Test]
    public void VerifyEmailAsync_ShouldThrowEndUserNotFoundException_WhenUserNotFound()
    {
        // Arrange
        const string email = "user@example.com";
        const string token = "token";

        _mockEncryption.Setup(e => e.Decrypt(token)).Returns(email);
        _mockUserRepo.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync((User?)null);

        // Act & Assert
        Assert.ThrowsAsync<EndUserNotFoundException>(() => _service.VerifyEmailAsync(email, token));
    }

    [Test]
    public void VerifyEmailAsync_ShouldThrowInvalidVerificationToken_WhenNoPendingRecord()
    {
        // Arrange
        const string email = "user@example.com";
        const string token = "token";
        var user = new User("alice", email, "08012345678", "pw", "end_user", null, "auth0|1");

        _mockEncryption.Setup(e => e.Decrypt(token)).Returns(email);
        _mockUserRepo.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync(user);
        _mockRegVerificationRepo.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync((RegistrationVerification?)null);

        // Act & Assert
        Assert.ThrowsAsync<InvalidVerificationTokenException>(() => _service.VerifyEmailAsync(email, token));
    }

    [Test]
    public void VerifyEmailAsync_ShouldThrowVerificationTokenExpired_WhenRecordExpired()
    {
        // Arrange
        const string email = "user@example.com";
        const string token = "token";
        var user = new User("alice", email, "08012345678", "pw", "end_user", null, "auth0|1");

        // Create a RegistrationVerification then backdate its Expiry via reflection so IsExpired == true
        var expiredVerification = new RegistrationVerification(email, "alice", token, "end_user");
        var expiryProp = typeof(RegistrationVerification).GetProperty("Expiry")!;
        expiryProp.SetValue(expiredVerification, DateTime.UtcNow.AddHours(-1));

        _mockEncryption.Setup(e => e.Decrypt(token)).Returns(email);
        _mockUserRepo.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync(user);
        _mockRegVerificationRepo.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync(expiredVerification);

        // Act & Assert
        Assert.ThrowsAsync<VerificationTokenExpiredException>(() => _service.VerifyEmailAsync(email, token));
    }

    [Test]
    public async Task VerifyEmailAsync_ShouldReturnAlreadyVerified_WhenEmailAlreadyVerified()
    {
        // Arrange
        const string email = "user@example.com";
        const string token = "token";
        var user = new User("alice", email, "08012345678", "pw", "end_user", null, "auth0|1");
        user.MarkEmailVerified();

        _mockEncryption.Setup(e => e.Decrypt(token)).Returns(email);
        _mockUserRepo.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync(user);

        // Act
        var result = await _service.VerifyEmailAsync(email, token);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Message, Does.Contain("already verified"));
        _mockUserRepo.Verify(r => r.UpdateEmailVerifiedAsync(It.IsAny<Guid>()), Times.Never);
        _mockRegVerificationRepo.Verify(r => r.DeleteByEmailAsync(It.IsAny<string>()), Times.Never);
    }

    // =========================================================================
    // ReverifyEmailAsync
    // =========================================================================

    [Test]
    public async Task ReverifyEmailAsync_ShouldResendEmail_AndReturnSuccess()
    {
        // Arrange
        const string email = "user@example.com";
        const string encryptedToken = "encrypted-token";
        var user = new User("alice", email, "08012345678", "pw", "end_user", null, "auth0|1");

        _mockUserRepo.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync(user);
        _mockEncryption.Setup(e => e.Encrypt(email)).Returns(encryptedToken);
        _mockRegVerificationRepo.Setup(r => r.DeleteByEmailAsync(email)).Returns(Task.CompletedTask);
        _mockRegVerificationRepo.Setup(r => r.AddAsync(It.IsAny<RegistrationVerification>())).Returns(Task.CompletedTask);
        _mockNotificationClient
            .Setup(n => n.SendNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.ReverifyEmailAsync(email);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.ExpiresAt, Is.GreaterThan(DateTime.UtcNow));
        _mockNotificationClient.Verify(n => n.SendNotificationAsync(
            "registeration", email, "email", It.IsAny<object>()), Times.Once);
    }

    [Test]
    public void ReverifyEmailAsync_ShouldThrowEndUserNotFoundException_WhenUserNotFound()
    {
        // Arrange
        const string email = "notfound@example.com";
        _mockUserRepo.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync((User?)null);

        // Act & Assert
        Assert.ThrowsAsync<EndUserNotFoundException>(() => _service.ReverifyEmailAsync(email));
    }

    [Test]
    public async Task ReverifyEmailAsync_ShouldReturnAlreadyVerified_WhenEmailAlreadyVerified()
    {
        // Arrange
        const string email = "user@example.com";
        var user = new User("alice", email, "08012345678", "pw", "end_user", null, "auth0|1");
        user.MarkEmailVerified();

        _mockUserRepo.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync(user);

        // Act
        var result = await _service.ReverifyEmailAsync(email);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Does.Contain("already verified"));
        _mockNotificationClient.Verify(n => n.SendNotificationAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

}
