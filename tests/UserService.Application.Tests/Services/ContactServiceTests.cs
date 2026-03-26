using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using UserService.Application.DTOs;
using UserService.Application.Interfaces;
using UserService.Application.Services;

namespace UserService.Application.Tests.Services;

[TestFixture]
public class ContactServiceTests
{
    private Mock<INotificationServiceClient> _mockNotificationClient = null!;
    private Mock<IConfiguration> _mockConfig = null!;
    private Mock<ILogger<ContactService>> _mockLogger = null!;
    private ContactService _service = null!;

    [SetUp]
    public void Setup()
    {
        _mockNotificationClient = new Mock<INotificationServiceClient>();
        _mockConfig = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<ContactService>>();

        _mockConfig.Setup(c => c["Services:ContactEmail"]).Returns("contact@clereview.com");

        _service = new ContactService(
            _mockNotificationClient.Object,
            _mockConfig.Object,
            _mockLogger.Object
        );
    }

    [Test]
    public async Task SendContactMessageAsync_ShouldReturnTrue_WhenNotificationSucceeds()
    {
        // Arrange
        var dto = new ContactDto("Alice", "alice@example.com", "Support needed", "Hello!");
        _mockNotificationClient
            .Setup(n => n.SendNotificationAsync("contact-us", "contact@clereview.com", "email", It.IsAny<object>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.SendContactMessageAsync(dto);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task SendContactMessageAsync_ShouldReturnFalse_WhenNotificationFails()
    {
        // Arrange
        var dto = new ContactDto(null, "alice@example.com", null, "Hello!");
        _mockNotificationClient
            .Setup(n => n.SendNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.SendContactMessageAsync(dto);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task SendContactMessageAsync_ShouldUseContactEmailFromConfig()
    {
        // Arrange
        string? capturedRecipient = null;
        var dto = new ContactDto("Bob", "bob@example.com", "Question", "Hi there!");
        _mockNotificationClient
            .Setup(n => n.SendNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
            .Callback<string, string, string, object>((_, recipient, _, _) => capturedRecipient = recipient)
            .ReturnsAsync(true);

        // Act
        await _service.SendContactMessageAsync(dto);

        // Assert
        Assert.That(capturedRecipient, Is.EqualTo("contact@clereview.com"));
    }

    [Test]
    public async Task SendContactMessageAsync_ShouldFallBackToDefaultEmail_WhenConfigMissing()
    {
        // Arrange
        string? capturedRecipient = null;
        _mockConfig.Setup(c => c["Services:ContactEmail"]).Returns((string?)null);
        var service = new ContactService(_mockNotificationClient.Object, _mockConfig.Object, _mockLogger.Object);
        var dto = new ContactDto(null, "user@example.com", null, "Message");
        _mockNotificationClient
            .Setup(n => n.SendNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
            .Callback<string, string, string, object>((_, recipient, _, _) => capturedRecipient = recipient)
            .ReturnsAsync(true);

        // Act
        await service.SendContactMessageAsync(dto);

        // Assert
        Assert.That(capturedRecipient, Is.EqualTo("contact@clereview.com"));
    }

    [Test]
    public async Task SendContactMessageAsync_ShouldSendCorrectTemplate()
    {
        // Arrange
        string? capturedTemplate = null;
        var dto = new ContactDto("Alice", "alice@example.com", "Help", "Message body");
        _mockNotificationClient
            .Setup(n => n.SendNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
            .Callback<string, string, string, object>((template, _, _, _) => capturedTemplate = template)
            .ReturnsAsync(true);

        // Act
        await _service.SendContactMessageAsync(dto);

        // Assert
        Assert.That(capturedTemplate, Is.EqualTo("contact-us"));
    }

    [Test]
    public async Task SendContactMessageAsync_ShouldSendViaEmailChannel()
    {
        // Arrange
        string? capturedChannel = null;
        var dto = new ContactDto(null, "user@example.com", null, "Hi");
        _mockNotificationClient
            .Setup(n => n.SendNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
            .Callback<string, string, string, object>((_, _, channel, _) => capturedChannel = channel)
            .ReturnsAsync(true);

        // Act
        await _service.SendContactMessageAsync(dto);

        // Assert
        Assert.That(capturedChannel, Is.EqualTo("email"));
    }
}
