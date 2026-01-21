using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using UserService.Application.Interfaces;
using UserService.Infrastructure.Clients;
using UserService.Infrastructure.Tests.Helpers;

namespace UserService.Infrastructure.Tests.Clients;

[TestFixture]
public class ReviewServiceClientTests
{
    private Mock<HttpMessageHandler> _mockHandler = null!;
    private Mock<ILogger<ReviewServiceClient>> _mockLogger = null!;
    private HttpClient _httpClient = null!;
    private ReviewServiceClient _client = null!;

    [SetUp]
    public void Setup()
    {
        _mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Loose);
        _mockLogger = new Mock<ILogger<ReviewServiceClient>>();

        _httpClient = new HttpClient(_mockHandler.Object)
        {
            BaseAddress = new Uri("https://fake-review-service.com")
        };

        _client = new ReviewServiceClient(_httpClient, _mockLogger.Object);
    }

    [TearDown]
    public void Cleanup()
    {
        _httpClient.Dispose();
    }

    // ========================================================================
    // GET TOTAL HELPFUL VOTES TESTS
    // ========================================================================

    [Test]
    public async Task GetTotalHelpfulVotesForUserAsync_Successful_ShouldReturnCount()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var responseObject = new { TotalHelpfulVotes = 42 };

        _mockHandler
            .SetupRequest(HttpMethod.Get, $"/api/review/user/{userId}/helpful-votes/total")
            .ReturnsJsonResponse(responseObject, HttpStatusCode.OK);

        // ACT
        var result = await _client.GetTotalHelpfulVotesForUserAsync(userId);

        // ASSERT
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public async Task GetTotalHelpfulVotesForUserAsync_NoVotes_ShouldReturnZero()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var responseObject = new { TotalHelpfulVotes = 0 };

        _mockHandler
            .SetupRequest(HttpMethod.Get, $"/api/review/user/{userId}/helpful-votes/total")
            .ReturnsJsonResponse(responseObject, HttpStatusCode.OK);

        // ACT
        var result = await _client.GetTotalHelpfulVotesForUserAsync(userId);

        // ASSERT
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public async Task GetTotalHelpfulVotesForUserAsync_ServiceReturnsError_ShouldReturnZero()
    {
        // ARRANGE
        var userId = Guid.NewGuid();

        _mockHandler
            .SetupRequest(HttpMethod.Get, $"/api/review/user/{userId}/helpful-votes/total")
            .ReturnsResponse(HttpStatusCode.InternalServerError);

        // ACT
        var result = await _client.GetTotalHelpfulVotesForUserAsync(userId);

        // ASSERT
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public async Task GetTotalHelpfulVotesForUserAsync_ServiceReturnsNotFound_ShouldReturnZero()
    {
        // ARRANGE
        var userId = Guid.NewGuid();

        _mockHandler
            .SetupRequest(HttpMethod.Get, $"/api/review/user/{userId}/helpful-votes/total")
            .ReturnsResponse(HttpStatusCode.NotFound);

        // ACT
        var result = await _client.GetTotalHelpfulVotesForUserAsync(userId);

        // ASSERT
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public async Task GetTotalHelpfulVotesForUserAsync_NetworkError_ShouldReturnZero()
    {
        // ARRANGE
        var userId = Guid.NewGuid();

        _mockHandler
            .SetupRequest(HttpMethod.Get, $"/api/review/user/{userId}/helpful-votes/total")
            .Throws<HttpRequestException>();

        // ACT
        var result = await _client.GetTotalHelpfulVotesForUserAsync(userId);

        // ASSERT
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public async Task GetTotalHelpfulVotesForUserAsync_InvalidJson_ShouldReturnZero()
    {
        // ARRANGE
        var userId = Guid.NewGuid();

        _mockHandler
            .SetupRequest(HttpMethod.Get, $"/api/review/user/{userId}/helpful-votes/total")
            .ReturnsResponse(HttpStatusCode.OK, "Invalid JSON {{{");

        // ACT
        var result = await _client.GetTotalHelpfulVotesForUserAsync(userId);

        // ASSERT
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public async Task GetTotalHelpfulVotesForUserAsync_NullResponse_ShouldReturnZero()
    {
        // ARRANGE
        var userId = Guid.NewGuid();

        _mockHandler
            .SetupRequest(HttpMethod.Get, $"/api/review/user/{userId}/helpful-votes/total")
            .ReturnsJsonResponse(new { }, HttpStatusCode.OK);

        // ACT
        var result = await _client.GetTotalHelpfulVotesForUserAsync(userId);

        // ASSERT
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public async Task GetTotalHelpfulVotesForUserAsync_ShouldLogWarning_OnFailure()
    {
        // ARRANGE
        var userId = Guid.NewGuid();

        _mockHandler
            .SetupRequest(HttpMethod.Get, $"/api/review/user/{userId}/helpful-votes/total")
            .ReturnsResponse(HttpStatusCode.InternalServerError);

        // ACT
        await _client.GetTotalHelpfulVotesForUserAsync(userId);

        // ASSERT
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to get helpful votes")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ========================================================================
    // GET APPROVED REVIEW COUNT TESTS
    // ========================================================================

    [Test]
    public async Task GetApprovedReviewCountAsync_Successful_ShouldReturnCount()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var responseObject = new { Count = 15 };

        _mockHandler
            .SetupRequest(HttpMethod.Get, $"/api/review/user/{userId}/approved/count")
            .ReturnsJsonResponse(responseObject, HttpStatusCode.OK);

        // ACT
        var result = await _client.GetApprovedReviewCountAsync(userId);

        // ASSERT
        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public async Task GetApprovedReviewCountAsync_NoReviews_ShouldReturnZero()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var responseObject = new { Count = 0 };

        _mockHandler
            .SetupRequest(HttpMethod.Get, $"/api/review/user/{userId}/approved/count")
            .ReturnsJsonResponse(responseObject, HttpStatusCode.OK);

        // ACT
        var result = await _client.GetApprovedReviewCountAsync(userId);

        // ASSERT
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public async Task GetApprovedReviewCountAsync_ServiceReturnsError_ShouldReturnZero()
    {
        // ARRANGE
        var userId = Guid.NewGuid();

        _mockHandler
            .SetupRequest(HttpMethod.Get, $"/api/review/user/{userId}/approved/count")
            .ReturnsResponse(HttpStatusCode.InternalServerError);

        // ACT
        var result = await _client.GetApprovedReviewCountAsync(userId);

        // ASSERT
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public async Task GetApprovedReviewCountAsync_NetworkError_ShouldReturnZero()
    {
        // ARRANGE
        var userId = Guid.NewGuid();

        _mockHandler
            .SetupRequest(HttpMethod.Get, $"/api/review/user/{userId}/approved/count")
            .Throws<HttpRequestException>();

        // ACT
        var result = await _client.GetApprovedReviewCountAsync(userId);

        // ASSERT
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public async Task GetApprovedReviewCountAsync_InvalidJson_ShouldReturnZero()
    {
        // ARRANGE
        var userId = Guid.NewGuid();

        _mockHandler
            .SetupRequest(HttpMethod.Get, $"/api/review/user/{userId}/approved/count")
            .ReturnsResponse(HttpStatusCode.OK, "Invalid JSON");

        // ACT
        var result = await _client.GetApprovedReviewCountAsync(userId);

        // ASSERT
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public async Task GetApprovedReviewCountAsync_ShouldLogWarning_OnFailure()
    {
        // ARRANGE
        var userId = Guid.NewGuid();

        _mockHandler
            .SetupRequest(HttpMethod.Get, $"/api/review/user/{userId}/approved/count")
            .ReturnsResponse(HttpStatusCode.BadRequest);

        // ACT
        await _client.GetApprovedReviewCountAsync(userId);

        // ASSERT
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to get review count")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetApprovedReviewCountAsync_ShouldLogError_OnException()
    {
        // ARRANGE
        var userId = Guid.NewGuid();

        _mockHandler
            .SetupRequest(HttpMethod.Get, $"/api/review/user/{userId}/approved/count")
            .Throws<HttpRequestException>();

        // ACT
        await _client.GetApprovedReviewCountAsync(userId);

        // ASSERT
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error getting review count")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ========================================================================
    // HTTP FAILURE HANDLING TESTS
    // ========================================================================

    [Test]
    [TestCase(HttpStatusCode.BadRequest)]
    [TestCase(HttpStatusCode.Unauthorized)]
    [TestCase(HttpStatusCode.Forbidden)]
    [TestCase(HttpStatusCode.NotFound)]
    [TestCase(HttpStatusCode.InternalServerError)]
    [TestCase(HttpStatusCode.ServiceUnavailable)]
    public async Task GetTotalHelpfulVotesForUserAsync_VariousErrors_ShouldReturnZero(HttpStatusCode statusCode)
    {
        // ARRANGE
        var userId = Guid.NewGuid();

        _mockHandler
            .SetupRequest(HttpMethod.Get, $"/api/review/user/{userId}/helpful-votes/total")
            .ReturnsResponse(statusCode);

        // ACT
        var result = await _client.GetTotalHelpfulVotesForUserAsync(userId);

        // ASSERT
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    [TestCase(HttpStatusCode.BadRequest)]
    [TestCase(HttpStatusCode.Unauthorized)]
    [TestCase(HttpStatusCode.Forbidden)]
    [TestCase(HttpStatusCode.NotFound)]
    [TestCase(HttpStatusCode.InternalServerError)]
    [TestCase(HttpStatusCode.ServiceUnavailable)]
    public async Task GetApprovedReviewCountAsync_VariousErrors_ShouldReturnZero(HttpStatusCode statusCode)
    {
        // ARRANGE
        var userId = Guid.NewGuid();

        _mockHandler
            .SetupRequest(HttpMethod.Get, $"/api/review/user/{userId}/approved/count")
            .ReturnsResponse(statusCode);

        // ACT
        var result = await _client.GetApprovedReviewCountAsync(userId);

        // ASSERT
        Assert.That(result, Is.EqualTo(0));
    }

    // ========================================================================
    // CONCURRENT REQUESTS TESTS
    // ========================================================================

    [Test]
    public async Task GetTotalHelpfulVotesForUserAsync_ConcurrentRequests_ShouldHandleCorrectly()
    {
        // ARRANGE
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        // Use the new combined method that properly matches specific URLs
        _mockHandler.SetupRequestWithJsonResponse(
            HttpMethod.Get, 
            $"/api/review/user/{userId1}/helpful-votes/total",
            new { totalHelpfulVotes = 10 }, 
            HttpStatusCode.OK);

        _mockHandler.SetupRequestWithJsonResponse(
            HttpMethod.Get, 
            $"/api/review/user/{userId2}/helpful-votes/total",
            new { totalHelpfulVotes = 20 }, 
            HttpStatusCode.OK);

        // ACT
        var task1 = _client.GetTotalHelpfulVotesForUserAsync(userId1);
        var task2 = _client.GetTotalHelpfulVotesForUserAsync(userId2);

        await Task.WhenAll(task1, task2);

        // ASSERT
        Assert.That(task1.Result, Is.EqualTo(10));
        Assert.That(task2.Result, Is.EqualTo(20));
    }
}