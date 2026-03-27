using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using UserService.Application.DTOs;
using UserService.Domain.Exceptions;
using UserService.Infrastructure.Clients;
using UserService.Infrastructure.Tests.Helpers;

namespace UserService.Infrastructure.Tests.Clients;

[TestFixture]
public class BusinessServiceClientTests
{
    private Mock<HttpMessageHandler> _mockHandler = null!;
    private Mock<ILogger<BusinessServiceClient>> _mockLogger = null!;
    private HttpClient _httpClient = null!;
    private BusinessServiceClient _client = null!;

    [SetUp]
    public void Setup()
    {
        _mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Loose);
        _mockLogger = new Mock<ILogger<BusinessServiceClient>>();

        _httpClient = new HttpClient(_mockHandler.Object)
        {
            BaseAddress = new Uri("https://fake-business-service.com")
        };

        _client = new BusinessServiceClient(_httpClient, _mockLogger.Object);
    }

    [TearDown]
    public void Cleanup()
    {
        _httpClient.Dispose();
    }

    // --- BUSINESS EXISTS TESTS ---

    [Test]
    public async Task BusinessExistsAsync_ShouldReturnTrue_WhenStatusOk()
    {
        // ARRANGE
        var businessId = Guid.NewGuid();
        _mockHandler
            .SetupRequest(HttpMethod.Get, $"/api/businesses/{businessId}")
            .ReturnsResponse(HttpStatusCode.OK);

        // ACT
        var result = await _client.BusinessExistsAsync(businessId);

        // ASSERT
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task BusinessExistsAsync_ShouldReturnFalse_WhenStatusNotFound()
    {
        var businessId = Guid.NewGuid();
        _mockHandler
            .SetupRequest(HttpMethod.Get, $"/api/businesses/{businessId}")
            .ReturnsResponse(HttpStatusCode.NotFound);

        var result = await _client.BusinessExistsAsync(businessId);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task BusinessExistsAsync_ShouldReturnFalse_OnUnexpectedStatus()
    {
        var businessId = Guid.NewGuid();
        _mockHandler
            .SetupRequest(HttpMethod.Get, $"/api/businesses/{businessId}")
            .ReturnsResponse(HttpStatusCode.InternalServerError);

        var result = await _client.BusinessExistsAsync(businessId);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task BusinessExistsAsync_ShouldHandleHttpRequestException()
    {
        var businessId = Guid.NewGuid();
        _mockHandler
            .SetupRequest(HttpMethod.Get, $"/api/businesses/{businessId}")
            .Throws<HttpRequestException>();

        var result = await _client.BusinessExistsAsync(businessId);

        Assert.That(result, Is.False);
    }

    // --- CREATE BUSINESS TESTS ---

    [Test]
    public async Task CreateBusinessAsync_ShouldReturnGuid_WhenSuccessful()
    {
        var dto = new BusinessUserDto(
            Name: "Test Biz",
            Email: "test@biz.com",
            Phone: "1234567890",
            Password:"123456",
            UserType: "business_user",
            Address: "123 Main St",
            BranchName: "HQ",
            BranchAddress: "123 Main St",
            Website: "https://biz.com",
            CategoryIds: new List<string>() { "tech" }
        );

        var expectedId = Guid.NewGuid();

        var response = new
        {
            Id = expectedId
        };

        _mockHandler
            .SetupRequest(HttpMethod.Post, "/api/business")
            .ReturnsJsonResponse(response, HttpStatusCode.Created);

        var result = await _client.CreateBusinessAsync(dto);

        Assert.That(result, Is.EqualTo(expectedId));
    }

    [Test]
    public void CreateBusinessAsync_ShouldThrow_WhenStatusIsNotCreated()
    {
        var dto = new BusinessUserDto(
            Name: "Fail Biz",
            Email: "fail@biz.com",
            Phone: "0000000000",
            Password:"123456",
            UserType: "business_user",
            Address: "Nowhere",
            BranchName: "Branch",
            BranchAddress: "Addr",
            Website: "https://fail.com",
            CategoryIds: new List<string>() { "none" }
        );

        _mockHandler
            .SetupRequest(HttpMethod.Post, "/api/business")
            .ReturnsResponse(HttpStatusCode.BadRequest, "Error!");

        Assert.ThrowsAsync<BusinessUserCreationFailedException>(
            async () => await _client.CreateBusinessAsync(dto)
        );
    }

    [Test]
    public void CreateBusinessAsync_ShouldThrow_WhenResponseMissingId()
    {
        var dto = new BusinessUserDto(
            Name: "NoIdBiz",
            Email: "noid@biz.com",
            Phone: "1111111111",
            Password:"123456",
            UserType: "business_user",
            Address: "Address",
            BranchName: "HQ",
            BranchAddress: "Addr",
            Website: "https://noid.com",
            CategoryIds: new List<string>() { "empty" }
        );

        var response = new { Id = Guid.Empty };

        _mockHandler
            .SetupRequest(HttpMethod.Post, "/api/business")
            .ReturnsJsonResponse(response, HttpStatusCode.Created);

        Assert.ThrowsAsync<BusinessUserCreationFailedException>(
            async () => await _client.CreateBusinessAsync(dto)
        );
    }

    [Test]
    public async Task CreateBusinessAsync_ShouldReturnNull_OnHttpRequestException()
    {
        var dto = new BusinessUserDto(
            Name: "NetworkFail",
            Email: "net@fail.com",
            Phone: "9999999999",
            Password:"123456",
            UserType: "business_user",
            Address: "Somewhere",
            BranchName: "Main",
            BranchAddress: "Addr",
            Website: "https://fail.com",
            CategoryIds: new List<string>() { "none" }
        );

        _mockHandler
            .SetupRequest(HttpMethod.Post, "/api/business")
            .Throws<HttpRequestException>();

        var result = await _client.CreateBusinessAsync(dto);

        Assert.That(result, Is.Null);
    }

    // =========================================================================
    // GetBusinessNameAsync
    // =========================================================================

    [Test]
    public async Task GetBusinessNameAsync_ShouldReturnName_WhenFound()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        _mockHandler.SetupRequestWithJsonResponse(
            HttpMethod.Get,
            $"/api/businesses/{businessId}",
            new { Id = businessId, Name = "Acme Corp" });

        // Act
        var result = await _client.GetBusinessNameAsync(businessId);

        // Assert
        Assert.That(result, Is.EqualTo("Acme Corp"));
    }

    [Test]
    public async Task GetBusinessNameAsync_ShouldReturnNull_WhenNotFound()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        _mockHandler.SetupRequestWithResponse(HttpMethod.Get, $"/api/businesses/{businessId}", HttpStatusCode.NotFound);

        // Act
        var result = await _client.GetBusinessNameAsync(businessId);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetBusinessNameAsync_ShouldReturnNull_OnException()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        _mockHandler.SetupRequestWithException<HttpRequestException>(HttpMethod.Get, $"/api/businesses/{businessId}");

        // Act
        var result = await _client.GetBusinessNameAsync(businessId);

        // Assert
        Assert.That(result, Is.Null);
    }

    // =========================================================================
    // UpdateBusinessOwnerAsync
    // =========================================================================

    [Test]
    public async Task UpdateBusinessOwnerAsync_ShouldReturnTrue_WhenSuccessful()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _mockHandler.SetupRequestWithResponse(HttpMethod.Patch, $"/api/business/{businessId}/owner", HttpStatusCode.OK);

        // Act
        var result = await _client.UpdateBusinessOwnerAsync(businessId, userId, "owner@biz.com", "+2348012345678");

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task UpdateBusinessOwnerAsync_ShouldReturnFalse_WhenServerError()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        _mockHandler.SetupRequestWithResponse(HttpMethod.Patch, $"/api/business/{businessId}/owner", HttpStatusCode.InternalServerError);

        // Act
        var result = await _client.UpdateBusinessOwnerAsync(businessId, Guid.NewGuid(), "owner@biz.com", null);

        // Assert
        Assert.That(result, Is.False);
    }

    // =========================================================================
    // InitializeBusinessSubscriptionAsync
    // =========================================================================

    [Test]
    public async Task InitializeBusinessSubscriptionAsync_ShouldReturnTrue_WhenCreated()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        _mockHandler.SetupRequestWithResponse(HttpMethod.Post, $"/api/business/{businessId}/subscription", HttpStatusCode.Created);

        // Act
        var result = await _client.InitializeBusinessSubscriptionAsync(businessId);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task InitializeBusinessSubscriptionAsync_ShouldReturnFalse_OnException()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        _mockHandler.SetupRequestWithException<HttpRequestException>(HttpMethod.Post, $"/api/business/{businessId}/subscription");

        // Act
        var result = await _client.InitializeBusinessSubscriptionAsync(businessId);

        // Assert
        Assert.That(result, Is.False);
    }

    // =========================================================================
    // InitializeBusinessSettingsAsync
    // =========================================================================

    [Test]
    public async Task InitializeBusinessSettingsAsync_ShouldReturnTrue_WhenOk()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        _mockHandler.SetupRequestWithResponse(HttpMethod.Post, $"/api/business/{businessId}/settings", HttpStatusCode.OK);

        // Act
        var result = await _client.InitializeBusinessSettingsAsync(businessId);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task InitializeBusinessSettingsAsync_ShouldReturnFalse_OnException()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        _mockHandler.SetupRequestWithException<HttpRequestException>(HttpMethod.Post, $"/api/business/{businessId}/settings");

        // Act
        var result = await _client.InitializeBusinessSettingsAsync(businessId);

        // Assert
        Assert.That(result, Is.False);
    }

    // =========================================================================
    // UpdateBusinessStatusAsync
    // =========================================================================

    [Test]
    public async Task UpdateBusinessStatusAsync_ShouldReturnTrue_WhenOk()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        _mockHandler.SetupRequestWithResponse(HttpMethod.Patch, $"/api/business/{businessId}/status", HttpStatusCode.OK);

        // Act
        var result = await _client.UpdateBusinessStatusAsync(businessId, "claimed");

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task UpdateBusinessStatusAsync_ShouldReturnTrue_WhenNoContent()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        _mockHandler.SetupRequestWithResponse(HttpMethod.Patch, $"/api/business/{businessId}/status", HttpStatusCode.NoContent);

        // Act
        var result = await _client.UpdateBusinessStatusAsync(businessId, "claimed");

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task UpdateBusinessStatusAsync_ShouldReturnFalse_WhenNotFound()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        _mockHandler.SetupRequestWithResponse(HttpMethod.Patch, $"/api/business/{businessId}/status", HttpStatusCode.NotFound);

        // Act
        var result = await _client.UpdateBusinessStatusAsync(businessId, "claimed");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task UpdateBusinessStatusAsync_ShouldReturnFalse_OnException()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        _mockHandler.SetupRequestWithException<HttpRequestException>(HttpMethod.Patch, $"/api/business/{businessId}/status");

        // Act
        var result = await _client.UpdateBusinessStatusAsync(businessId, "claimed");

        // Assert
        Assert.That(result, Is.False);
    }
}
