using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using UserService.Api.Controllers;
using UserService.Application.DTOs.PasswordReset;
using UserService.Application.Interfaces;

namespace UserService.Api.Tests.Controllers;

[TestFixture]
public class PasswordResetControllerTests
{
    private Mock<IPasswordResetService> _mockService = null!;
    private Mock<ILogger<PasswordResetController>> _mockLogger = null!;
    private PasswordResetController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _mockService = new Mock<IPasswordResetService>();
        _mockLogger = new Mock<ILogger<PasswordResetController>>();

        _controller = new PasswordResetController(_mockService.Object, _mockLogger.Object);
    }

    // ========== ResetEmail Tests ==========

    [Test]
    public async Task ResetEmail_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        var request = new ResetEmailRequest("old@example.com", "new@example.com");
        _mockService.Setup(s => s.ResetEmailAsync(request)).ReturnsAsync((true, "Email updated successfully"));

        // Act
        var result = await _controller.ResetEmail(request);

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        Assert.That(okResult!.Value, Is.Not.Null);
    }

    [Test]
    public async Task ResetEmail_ShouldReturnBadRequest_WhenFailed()
    {
        // Arrange
        var request = new ResetEmailRequest("old@example.com", "new@example.com");
        _mockService.Setup(s => s.ResetEmailAsync(request)).ReturnsAsync((false, "Email does not exist"));

        // Act
        var result = await _controller.ResetEmail(request);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task ResetEmail_ShouldReturn500_OnException()
    {
        // Arrange
        var request = new ResetEmailRequest("old@example.com", "new@example.com");
        _mockService.Setup(s => s.ResetEmailAsync(request)).ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.ResetEmail(request);

        // Assert
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var objectResult = result as ObjectResult;
        Assert.That(objectResult!.StatusCode, Is.EqualTo(500));
    }

    // ========== RequestPasswordReset Tests ==========

    [Test]
    public async Task RequestPasswordReset_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        var request = new RequestPasswordResetRequest("test@example.com", "email");
        _mockService.Setup(s => s.RequestPasswordResetAsync(request)).ReturnsAsync((true, "OTP sent successfully"));

        // Act
        var result = await _controller.RequestPasswordReset(request);

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task RequestPasswordReset_ShouldReturnBadRequest_WhenInvalidType()
    {
        // Arrange
        var request = new RequestPasswordResetRequest("test@example.com", "invalid");
        _mockService.Setup(s => s.RequestPasswordResetAsync(request)).ReturnsAsync((false, "Type must be 'email' or 'sms'"));

        // Act
        var result = await _controller.RequestPasswordReset(request);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task RequestPasswordReset_ShouldReturnBadRequest_WhenIdNotFound()
    {
        // Arrange
        var request = new RequestPasswordResetRequest("notfound@example.com", "email");
        _mockService.Setup(s => s.RequestPasswordResetAsync(request)).ReturnsAsync((false, "Id does not exist"));

        // Act
        var result = await _controller.RequestPasswordReset(request);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task RequestPasswordReset_ShouldReturn500_OnException()
    {
        // Arrange
        var request = new RequestPasswordResetRequest("test@example.com", "email");
        _mockService.Setup(s => s.RequestPasswordResetAsync(request)).ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.RequestPasswordReset(request);

        // Assert
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var objectResult = result as ObjectResult;
        Assert.That(objectResult!.StatusCode, Is.EqualTo(500));
    }

    // ========== ResetPassword Tests ==========

    [Test]
    public async Task ResetPassword_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        var request = new ResetPasswordRequest("test@example.com", "encryptedPassword");
        _mockService.Setup(s => s.ResetPasswordAsync(request)).ReturnsAsync((true, "Password updated"));

        // Act
        var result = await _controller.ResetPassword(request);

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task ResetPassword_ShouldReturnBadRequest_WhenNoVerifiedRequest()
    {
        // Arrange
        var request = new ResetPasswordRequest("test@example.com", "encryptedPassword");
        _mockService.Setup(s => s.ResetPasswordAsync(request))
            .ReturnsAsync((false, "No verified password reset request found for this identifier"));

        // Act
        var result = await _controller.ResetPassword(request);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task ResetPassword_ShouldReturnBadRequest_WhenInvalidPassword()
    {
        // Arrange
        var request = new ResetPasswordRequest("test@example.com", "invalidEncrypted");
        _mockService.Setup(s => s.ResetPasswordAsync(request)).ReturnsAsync((false, "Invalid password format"));

        // Act
        var result = await _controller.ResetPassword(request);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task ResetPassword_ShouldReturn500_OnException()
    {
        // Arrange
        var request = new ResetPasswordRequest("test@example.com", "encryptedPassword");
        _mockService.Setup(s => s.ResetPasswordAsync(request)).ThrowsAsync(new Exception("Auth0 error"));

        // Act
        var result = await _controller.ResetPassword(request);

        // Assert
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var objectResult = result as ObjectResult;
        Assert.That(objectResult!.StatusCode, Is.EqualTo(500));
    }
}
