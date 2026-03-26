using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using UserService.Api.Controllers;
using UserService.Application.DTOs;
using UserService.Application.Interfaces;

namespace UserService.Api.Tests.Controllers;

[TestFixture]
public class ContactControllerTests
{
    private Mock<IContactService> _mockContactService = null!;
    private Mock<ILogger<ContactController>> _mockLogger = null!;
    private ContactController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _mockContactService = new Mock<IContactService>();
        _mockLogger = new Mock<ILogger<ContactController>>();
        _controller = new ContactController(_mockContactService.Object, _mockLogger.Object);
    }

    [Test]
    public async Task ContactUs_ShouldReturn200_WhenMessageSent()
    {
        // Arrange
        var dto = new ContactDto("Alice", "alice@example.com", "Help", "Hello!");
        _mockContactService.Setup(s => s.SendContactMessageAsync(dto)).ReturnsAsync(true);

        // Act
        var result = await _controller.ContactUs(dto);

        // Assert
        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.StatusCode, Is.EqualTo(200));
    }

    [Test]
    public async Task ContactUs_ShouldReturn400_WhenEmailMissing()
    {
        // Act
        var result = await _controller.ContactUs(new ContactDto(null, "", null, "Hello!"));

        // Assert
        var bad = result as BadRequestObjectResult;
        Assert.That(bad, Is.Not.Null);
        Assert.That(bad!.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public async Task ContactUs_ShouldReturn400_WhenMessageMissing()
    {
        // Act
        var result = await _controller.ContactUs(new ContactDto(null, "alice@example.com", null, ""));

        // Assert
        var bad = result as BadRequestObjectResult;
        Assert.That(bad, Is.Not.Null);
        Assert.That(bad!.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public async Task ContactUs_ShouldReturn500_WhenNotificationFails()
    {
        // Arrange
        var dto = new ContactDto(null, "alice@example.com", null, "Hello!");
        _mockContactService.Setup(s => s.SendContactMessageAsync(dto)).ReturnsAsync(false);

        // Act
        var result = await _controller.ContactUs(dto);

        // Assert
        var server = result as ObjectResult;
        Assert.That(server, Is.Not.Null);
        Assert.That(server!.StatusCode, Is.EqualTo(500));
    }

    [Test]
    public async Task ContactUs_ShouldReturn500_OnUnexpectedException()
    {
        // Arrange
        var dto = new ContactDto(null, "alice@example.com", null, "Hello!");
        _mockContactService.Setup(s => s.SendContactMessageAsync(dto)).ThrowsAsync(new Exception("Boom"));

        // Act
        var result = await _controller.ContactUs(dto);

        // Assert
        var server = result as ObjectResult;
        Assert.That(server, Is.Not.Null);
        Assert.That(server!.StatusCode, Is.EqualTo(500));
    }

    [Test]
    public async Task ContactUs_ShouldWorkWithOptionalFields_WhenNameAndSubjectOmitted()
    {
        // Arrange
        var dto = new ContactDto(null, "alice@example.com", null, "Just a message");
        _mockContactService.Setup(s => s.SendContactMessageAsync(dto)).ReturnsAsync(true);

        // Act
        var result = await _controller.ContactUs(dto);

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }
}
