using Moq;
using UserService.Application.DTOs.Verification;
using UserService.Application.Services;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Domain.Repositories;

namespace UserService.Application.Tests.Services;

[TestFixture]
public class VerificationServiceTests
{
    private Mock<IUserVerificationRepository> _mockVerificationRepository = null!;
    private Mock<IVerificationTokenRepository> _mockTokenRepository = null!;
    private Mock<IUserRepository> _mockUserRepository = null!;
    private VerificationService _service = null!;

    [SetUp]
    public void Setup()
    {
        _mockVerificationRepository = new Mock<IUserVerificationRepository>();
        _mockTokenRepository = new Mock<IVerificationTokenRepository>();
        _mockUserRepository = new Mock<IUserRepository>();

        _service = new VerificationService(
            _mockVerificationRepository.Object,
            _mockTokenRepository.Object,
            _mockUserRepository.Object
        );
    }

    [Test]
    public async Task GetVerificationStatusAsync_ShouldReturnStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var verification = new UserVerification(userId);
        verification.VerifyPhone();

        _mockVerificationRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(verification);

        // Act
        var result = await _service.GetVerificationStatusAsync(userId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.UserId, Is.EqualTo(userId));
        Assert.That(result.PhoneVerified, Is.True);
        Assert.That(result.EmailVerified, Is.False);
        Assert.That(result.VerificationLevel, Is.EqualTo(VerificationLevels.Partial));
    }

    [Test]
    public async Task SendPhoneOtpAsync_ShouldSendOtp_WhenValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User("john_doe", "john@example.com", "1234567890", "password", "end_user", "123 Main St", "auth0|123");
        var dto = new SendPhoneOtpDto(userId, "+2348012345678");

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockVerificationRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((UserVerification?)null);
        _mockTokenRepository.Setup(r => r.InvalidatePreviousTokensAsync(userId, VerificationTypes.Phone)).Returns(Task.CompletedTask);
        _mockTokenRepository.Setup(r => r.AddAsync(It.IsAny<VerificationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.SendPhoneOtpAsync(dto);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Success, Is.True);
        Assert.That(result.RemainingAttempts, Is.EqualTo(3));
    }

    [Test]
    public void SendPhoneOtpAsync_ShouldThrow_WhenUserNotFound()
    {
        // Arrange
        var dto = new SendPhoneOtpDto(Guid.NewGuid(), "+2348012345678");
        _mockUserRepository.Setup(r => r.GetByIdAsync(dto.UserId)).ReturnsAsync((User?)null);

        // Act & Assert
        Assert.ThrowsAsync<EndUserNotFoundException>(() => _service.SendPhoneOtpAsync(dto));
    }

    [Test]
    public void SendPhoneOtpAsync_ShouldThrow_WhenInvalidPhoneNumber()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User("john_doe", "john@example.com", "1234567890", "password", "end_user", "123 Main St", "auth0|123");
        var dto = new SendPhoneOtpDto(userId, "invalid_phone");

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act & Assert
        Assert.ThrowsAsync<InvalidPhoneNumberException>(() => _service.SendPhoneOtpAsync(dto));
    }

    [Test]
    public void SendPhoneOtpAsync_ShouldThrow_WhenAlreadyVerified()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User("john_doe", "john@example.com", "1234567890", "password", "end_user", "123 Main St", "auth0|123");
        var verification = new UserVerification(userId);
        verification.VerifyPhone();
        var dto = new SendPhoneOtpDto(userId, "+2348012345678");

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockVerificationRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(verification);

        // Act & Assert
        Assert.ThrowsAsync<PhoneAlreadyVerifiedException>(() => _service.SendPhoneOtpAsync(dto));
    }

    [Test]
    public async Task VerifyPhoneOtpAsync_ShouldVerify_WhenValidOtp()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otp = "123456";
        var token = new VerificationToken(userId, VerificationTypes.Phone, otp, "+2348012345678", 10);
        var verification = new UserVerification(userId);
        var dto = new VerifyPhoneOtpDto(userId, otp);

        _mockTokenRepository.Setup(r => r.GetLatestByUserIdAndTypeAsync(userId, VerificationTypes.Phone))
            .ReturnsAsync(token);
        _mockTokenRepository.Setup(r => r.UpdateAsync(token)).Returns(Task.CompletedTask);
        _mockVerificationRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(verification);
        _mockVerificationRepository.Setup(r => r.UpdateAsync(verification)).Returns(Task.CompletedTask);

        // Act
        var result = await _service.VerifyPhoneOtpAsync(dto);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Success, Is.True);
        Assert.That(result.PhoneVerified, Is.True);
    }

    [Test]
    public void VerifyPhoneOtpAsync_ShouldThrow_WhenNoTokenFound()
    {
        // Arrange
        var dto = new VerifyPhoneOtpDto(Guid.NewGuid(), "123456");
        _mockTokenRepository.Setup(r => r.GetLatestByUserIdAndTypeAsync(dto.UserId, VerificationTypes.Phone))
            .ReturnsAsync((VerificationToken?)null);

        // Act & Assert
        Assert.ThrowsAsync<InvalidVerificationTokenException>(() => _service.VerifyPhoneOtpAsync(dto));
    }

    [Test]
    public async Task IsUserVerifiedAsync_ShouldReturnTrue_WhenVerified()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockVerificationRepository.Setup(r => r.IsUserVerifiedAsync(userId)).ReturnsAsync(true);

        // Act
        var result = await _service.IsUserVerifiedAsync(userId);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task GetPointsMultiplierAsync_ShouldReturn1_5_WhenVerified()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockVerificationRepository.Setup(r => r.IsUserVerifiedAsync(userId)).ReturnsAsync(true);

        // Act
        var multiplier = await _service.GetPointsMultiplierAsync(userId);

        // Assert
        Assert.That(multiplier, Is.EqualTo(1.5m));
    }

    [Test]
    public async Task GetPointsMultiplierAsync_ShouldReturn1_WhenNotVerified()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockVerificationRepository.Setup(r => r.IsUserVerifiedAsync(userId)).ReturnsAsync(false);

        // Act
        var multiplier = await _service.GetPointsMultiplierAsync(userId);

        // Assert
        Assert.That(multiplier, Is.EqualTo(1.0m));
    }

    [TestCase("+2348012345678", true)]
    [TestCase("08012345678", true)]
    [TestCase("+2349012345678", true)]
    [TestCase("2347012345678", true)]
    [TestCase("invalid", false)]
    [TestCase("+1234567890", false)]
    [TestCase("", false)]
    public void ValidateNigerianPhoneNumber_ShouldValidateCorrectly(string phone, bool expected)
    {
        // Act
        var result = _service.ValidateNigerianPhoneNumber(phone);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }
}
