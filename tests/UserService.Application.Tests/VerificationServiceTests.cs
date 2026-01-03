using Moq;
using NUnit.Framework;
using UserService.Application.DTOs.Points;
using UserService.Application.DTOs.Verification;
using UserService.Application.Interfaces;
using UserService.Application.Services;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Domain.Repositories;

namespace UserService.Application.Tests;

[TestFixture]
public class VerificationServiceTests
{
    private Mock<IEmailVerificationRepository> _emailVerifRepoMock;
    private Mock<IPhoneVerificationRepository> _phoneVerifRepoMock;
    private Mock<IUserVerificationStatusRepository> _statusRepoMock;
    private Mock<IUserRepository> _userRepoMock;
    private Mock<IPointsService> _pointsServiceMock;
    private IVerificationService _verificationService;

    [SetUp]
    public void Setup()
    {
        _emailVerifRepoMock = new Mock<IEmailVerificationRepository>();
        _phoneVerifRepoMock = new Mock<IPhoneVerificationRepository>();
        _statusRepoMock = new Mock<IUserVerificationStatusRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _pointsServiceMock = new Mock<IPointsService>();

        _verificationService = new VerificationService(
            _emailVerifRepoMock.Object,
            _phoneVerifRepoMock.Object,
            _statusRepoMock.Object,
            _userRepoMock.Object,
            _pointsServiceMock.Object
        );
    }

    [Test]
    public async Task GetOrCreateVerificationStatusAsync_NewUser_CreatesStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _statusRepoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((UserVerificationStatus?)null);

        // Act
        var result = await _verificationService.GetOrCreateVerificationStatusAsync(userId);

        // Assert
        Assert.That(result.UserId, Is.EqualTo(userId));
        Assert.That(result.EmailVerified, Is.False);
        Assert.That(result.PhoneVerified, Is.False);
        Assert.That(result.VerificationLevel, Is.EqualTo("none"));
        _statusRepoMock.Verify(r => r.AddAsync(It.IsAny<UserVerificationStatus>()), Times.Once);
    }

    [Test]
    public async Task SendEmailVerificationAsync_NewVerification_CreatesVerification()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _statusRepoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((UserVerificationStatus?)null);
        _emailVerifRepoMock.Setup(r => r.GetActiveByUserIdAsync(userId)).ReturnsAsync((EmailVerification?)null);

        var dto = new SendEmailVerificationDto(userId, "test@example.com");

        // Act
        var result = await _verificationService.SendEmailVerificationAsync(dto);

        // Assert
        Assert.That(result.UserId, Is.EqualTo(userId));
        Assert.That(result.Email, Is.EqualTo("test@example.com"));
        Assert.That(result.IsVerified, Is.False);
        _emailVerifRepoMock.Verify(r => r.AddAsync(It.IsAny<EmailVerification>()), Times.Once);
    }

    [Test]
    public void SendEmailVerificationAsync_AlreadyVerified_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var status = new UserVerificationStatus(userId);
        status.MarkEmailVerified();
        _statusRepoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(status);

        var dto = new SendEmailVerificationDto(userId, "test@example.com");

        // Act & Assert
        Assert.ThrowsAsync<AlreadyVerifiedException>(async () =>
            await _verificationService.SendEmailVerificationAsync(dto));
    }

    [Test]
    public async Task VerifyEmailAsync_ValidCode_VerifiesEmail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var verification = new EmailVerification(userId, "test@example.com");
        var status = new UserVerificationStatus(userId);

        _emailVerifRepoMock.Setup(r => r.GetActiveByUserIdAsync(userId)).ReturnsAsync(verification);
        _statusRepoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(status);
        _pointsServiceMock.Setup(p => p.EarnPointsAsync(It.IsAny<EarnPointsDto>()))
            .ReturnsAsync(new EarnPointsResultDto(true, 25, 25, null, 1.0m));

        // Get the verification code through reflection for testing
        var codeProperty = typeof(EmailVerification).GetProperty("VerificationCode");
        var code = codeProperty?.GetValue(verification)?.ToString() ?? "123456";

        var dto = new VerifyEmailDto(userId, code);

        // Act
        var result = await _verificationService.VerifyEmailAsync(dto);

        // Assert
        Assert.That(result.Success, Is.True);
        _statusRepoMock.Verify(r => r.UpdateAsync(It.IsAny<UserVerificationStatus>()), Times.Once);
    }

    [Test]
    public void VerifyEmailAsync_NoActiveVerification_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _emailVerifRepoMock.Setup(r => r.GetActiveByUserIdAsync(userId)).ReturnsAsync((EmailVerification?)null);

        var dto = new VerifyEmailDto(userId, "123456");

        // Act & Assert
        Assert.ThrowsAsync<VerificationNotFoundException>(async () =>
            await _verificationService.VerifyEmailAsync(dto));
    }

    [Test]
    public async Task SendPhoneVerificationAsync_ValidInput_CreatesVerification()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _statusRepoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((UserVerificationStatus?)null);
        _phoneVerifRepoMock.Setup(r => r.GetActiveByUserIdAsync(userId)).ReturnsAsync((PhoneVerification?)null);

        var dto = new SendPhoneVerificationDto(userId, "5551234567", "+1", "sms");

        // Act
        var result = await _verificationService.SendPhoneVerificationAsync(dto);

        // Assert
        Assert.That(result.UserId, Is.EqualTo(userId));
        Assert.That(result.PhoneNumber, Is.EqualTo("5551234567"));
        Assert.That(result.VerificationMethod, Is.EqualTo("sms"));
        _phoneVerifRepoMock.Verify(r => r.AddAsync(It.IsAny<PhoneVerification>()), Times.Once);
    }

    [Test]
    public void SendPhoneVerificationAsync_AlreadyVerified_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var status = new UserVerificationStatus(userId);
        status.MarkPhoneVerified();
        _statusRepoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(status);

        var dto = new SendPhoneVerificationDto(userId, "5551234567");

        // Act & Assert
        Assert.ThrowsAsync<AlreadyVerifiedException>(async () =>
            await _verificationService.SendPhoneVerificationAsync(dto));
    }

    [Test]
    public async Task VerifyEmailByTokenAsync_ValidToken_VerifiesEmail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var verification = new EmailVerification(userId, "test@example.com");
        var tokenProperty = typeof(EmailVerification).GetProperty("VerificationToken");
        var token = (Guid)(tokenProperty?.GetValue(verification) ?? Guid.NewGuid());

        var status = new UserVerificationStatus(userId);

        _emailVerifRepoMock.Setup(r => r.GetByTokenAsync(token)).ReturnsAsync(verification);
        _statusRepoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(status);

        var dto = new VerifyEmailByTokenDto(token);

        // Act
        var result = await _verificationService.VerifyEmailByTokenAsync(dto);

        // Assert
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public void VerifyEmailByTokenAsync_InvalidToken_ThrowsException()
    {
        // Arrange
        var invalidToken = Guid.NewGuid();
        _emailVerifRepoMock.Setup(r => r.GetByTokenAsync(invalidToken)).ReturnsAsync((EmailVerification?)null);

        var dto = new VerifyEmailByTokenDto(invalidToken);

        // Act & Assert
        Assert.ThrowsAsync<VerificationTokenNotFoundException>(async () =>
            await _verificationService.VerifyEmailByTokenAsync(dto));
    }
}
