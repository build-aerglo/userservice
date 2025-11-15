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
}
