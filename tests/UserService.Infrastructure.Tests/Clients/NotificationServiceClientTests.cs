using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using UserService.Infrastructure.Clients;
using UserService.Infrastructure.Tests.Helpers;

namespace UserService.Infrastructure.Tests.Clients;

[TestFixture]
public class NotificationServiceClientTests
{
    private Mock<HttpMessageHandler> _mockHandler = null!;
    private Mock<ILogger<NotificationServiceClient>> _mockLogger = null!;
    private HttpClient _httpClient = null!;
    private NotificationServiceClient _client = null!;

    [SetUp]
    public void Setup()
    {
        _mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Loose);
        _mockLogger = new Mock<ILogger<NotificationServiceClient>>();

        _httpClient = new HttpClient(_mockHandler.Object)
        {
            BaseAddress = new Uri("https://fake-notification-service.com")
        };

        _client = new NotificationServiceClient(_httpClient, _mockLogger.Object);
    }

    [TearDown]
    public void Cleanup()
    {
        _httpClient.Dispose();
    }

    [Test]
    public async Task CreateOtpAsync_ShouldReturnTrue_WhenStatusOk()
    {
        // Arrange
        _mockHandler
            .SetupRequest(HttpMethod.Post, "/otp/create")
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _client.CreateOtpAsync("test@example.com", "email", "resetpassword");

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task CreateOtpAsync_ShouldReturnTrue_WhenStatusCreated()
    {
        // Arrange
        _mockHandler
            .SetupRequest(HttpMethod.Post, "/otp/create")
            .ReturnsResponse(HttpStatusCode.Created);

        // Act
        var result = await _client.CreateOtpAsync("+2341234567890", "sms", "resetpassword");

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task CreateOtpAsync_ShouldReturnFalse_WhenStatusBadRequest()
    {
        // Arrange
        _mockHandler
            .SetupRequest(HttpMethod.Post, "/otp/create")
            .ReturnsResponse(HttpStatusCode.BadRequest, "Invalid request");

        // Act
        var result = await _client.CreateOtpAsync("test@example.com", "email", "resetpassword");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task CreateOtpAsync_ShouldReturnFalse_OnHttpRequestException()
    {
        // Arrange
        _mockHandler
            .SetupRequest(HttpMethod.Post, "/otp/create")
            .Throws<HttpRequestException>();

        // Act
        var result = await _client.CreateOtpAsync("test@example.com", "email", "resetpassword");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task CreateOtpAsync_ShouldReturnFalse_OnUnexpectedException()
    {
        // Arrange
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Unexpected error"));

        // Act
        var result = await _client.CreateOtpAsync("test@example.com", "email", "resetpassword");

        // Assert
        Assert.That(result, Is.False);
    }
}
