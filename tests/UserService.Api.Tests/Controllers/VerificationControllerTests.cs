using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using UserService.Api.Controllers;
using UserService.Application.DTOs.Verification;
using UserService.Application.Interfaces;
using UserService.Domain.Exceptions;

namespace UserService.Api.Tests.Controllers;

[TestFixture]
public class VerificationControllerTests
{
    private Mock<IVerificationService> _mockVerificationService = null!;
    private Mock<IRegistrationVerificationService> _mockRegVerificationService = null!;
    private Mock<ILogger<VerificationController>> _mockLogger = null!;
    private VerificationController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _mockVerificationService = new Mock<IVerificationService>();
        _mockRegVerificationService = new Mock<IRegistrationVerificationService>();
        _mockLogger = new Mock<ILogger<VerificationController>>();

        _controller = new VerificationController(
            _mockVerificationService.Object,
            _mockRegVerificationService.Object,
            _mockLogger.Object
        );
    }

    // =========================================================================
    // VerifyRegistrationEmail (GET /api/verification/verify-email)
    // =========================================================================

    [Test]
    public async Task VerifyRegistrationEmail_ShouldReturn200_WhenSuccessful()
    {
        // Arrange
        const string email = "user@example.com";
        const string token = "encrypted-token";
        var expectedResult = new VerifyRegistrationEmailResultDto(true, "Email verified successfully.");

        _mockRegVerificationService
            .Setup(s => s.VerifyEmailAsync(email, token))
            .ReturnsAsync(expectedResult);

        // Act
        var actionResult = await _controller.VerifyRegistrationEmail(email, token);

        // Assert
        var okResult = actionResult as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.StatusCode, Is.EqualTo(200));
        Assert.That(okResult.Value, Is.EqualTo(expectedResult));
    }

    [Test]
    public async Task VerifyRegistrationEmail_ShouldReturn400_WhenEmailMissing()
    {
        // Act
        var actionResult = await _controller.VerifyRegistrationEmail("", "some-token");

        // Assert
        var badRequest = actionResult as BadRequestObjectResult;
        Assert.That(badRequest, Is.Not.Null);
        Assert.That(badRequest!.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public async Task VerifyRegistrationEmail_ShouldReturn400_WhenTokenMissing()
    {
        // Act
        var actionResult = await _controller.VerifyRegistrationEmail("user@example.com", "");

        // Assert
        var badRequest = actionResult as BadRequestObjectResult;
        Assert.That(badRequest, Is.Not.Null);
        Assert.That(badRequest!.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public async Task VerifyRegistrationEmail_ShouldReturn400_OnInvalidVerificationTokenException()
    {
        // Arrange
        _mockRegVerificationService
            .Setup(s => s.VerifyEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidVerificationTokenException("Token does not match the provided email."));

        // Act
        var actionResult = await _controller.VerifyRegistrationEmail("user@example.com", "bad-token");

        // Assert
        var badRequest = actionResult as BadRequestObjectResult;
        Assert.That(badRequest, Is.Not.Null);
        Assert.That(badRequest!.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public async Task VerifyRegistrationEmail_ShouldReturn400_OnVerificationTokenExpiredException()
    {
        // Arrange
        _mockRegVerificationService
            .Setup(s => s.VerifyEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new VerificationTokenExpiredException());

        // Act
        var actionResult = await _controller.VerifyRegistrationEmail("user@example.com", "expired-token");

        // Assert
        var badRequest = actionResult as BadRequestObjectResult;
        Assert.That(badRequest, Is.Not.Null);
        Assert.That(badRequest!.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public async Task VerifyRegistrationEmail_ShouldReturn404_OnEndUserNotFoundException()
    {
        // Arrange
        _mockRegVerificationService
            .Setup(s => s.VerifyEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new EndUserNotFoundException(Guid.Empty));

        // Act
        var actionResult = await _controller.VerifyRegistrationEmail("notfound@example.com", "token");

        // Assert
        var notFound = actionResult as NotFoundObjectResult;
        Assert.That(notFound, Is.Not.Null);
        Assert.That(notFound!.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task VerifyRegistrationEmail_ShouldReturn500_OnUnexpectedException()
    {
        // Arrange
        _mockRegVerificationService
            .Setup(s => s.VerifyEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Something went wrong"));

        // Act
        var actionResult = await _controller.VerifyRegistrationEmail("user@example.com", "token");

        // Assert
        var serverError = actionResult as ObjectResult;
        Assert.That(serverError, Is.Not.Null);
        Assert.That(serverError!.StatusCode, Is.EqualTo(500));
    }

    // =========================================================================
    // ReverifyEmail (POST /api/verification/reverify-email)
    // =========================================================================

    [Test]
    public async Task ReverifyEmail_ShouldReturn200_WhenSuccessful()
    {
        // Arrange
        var request = new ReverifyEmailRequest("user@example.com");
        var expectedResult = new ReverifyEmailResultDto(true, "Verification email resent successfully.", DateTime.UtcNow.AddHours(24));

        _mockRegVerificationService
            .Setup(s => s.ReverifyEmailAsync(request.Email))
            .ReturnsAsync(expectedResult);

        // Act
        var actionResult = await _controller.ReverifyEmail(request);

        // Assert
        var okResult = actionResult as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.StatusCode, Is.EqualTo(200));
        Assert.That(okResult.Value, Is.EqualTo(expectedResult));
    }

    [Test]
    public async Task ReverifyEmail_ShouldReturn400_WhenEmailMissing()
    {
        // Act
        var actionResult = await _controller.ReverifyEmail(new ReverifyEmailRequest(""));

        // Assert
        var badRequest = actionResult as BadRequestObjectResult;
        Assert.That(badRequest, Is.Not.Null);
        Assert.That(badRequest!.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public async Task ReverifyEmail_ShouldReturn400_WhenRequestBodyIsNull()
    {
        // Act
        var actionResult = await _controller.ReverifyEmail(null!);

        // Assert
        var badRequest = actionResult as BadRequestObjectResult;
        Assert.That(badRequest, Is.Not.Null);
        Assert.That(badRequest!.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public async Task ReverifyEmail_ShouldReturn404_OnEndUserNotFoundException()
    {
        // Arrange
        _mockRegVerificationService
            .Setup(s => s.ReverifyEmailAsync(It.IsAny<string>()))
            .ThrowsAsync(new EndUserNotFoundException(Guid.Empty));

        // Act
        var actionResult = await _controller.ReverifyEmail(new ReverifyEmailRequest("notfound@example.com"));

        // Assert
        var notFound = actionResult as NotFoundObjectResult;
        Assert.That(notFound, Is.Not.Null);
        Assert.That(notFound!.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task ReverifyEmail_ShouldReturn500_OnUnexpectedException()
    {
        // Arrange
        _mockRegVerificationService
            .Setup(s => s.ReverifyEmailAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Unexpected failure"));

        // Act
        var actionResult = await _controller.ReverifyEmail(new ReverifyEmailRequest("user@example.com"));

        // Assert
        var serverError = actionResult as ObjectResult;
        Assert.That(serverError, Is.Not.Null);
        Assert.That(serverError!.StatusCode, Is.EqualTo(500));
    }
}
