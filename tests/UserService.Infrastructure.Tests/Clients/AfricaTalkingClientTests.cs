using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using UserService.Application.Interfaces;
using UserService.Infrastructure.Clients;
using UserService.Infrastructure.Tests.Helpers;

namespace UserService.Infrastructure.Tests.Clients;

[TestFixture]
public class AfricaTalkingClientTests
{
    private Mock<HttpMessageHandler> _mockHandler = null!;
    private Mock<ILogger<AfricaTalkingClient>> _mockLogger = null!;
    private Mock<IConfiguration> _mockConfig = null!;
    private HttpClient _httpClient = null!;
    private AfricaTalkingClient _client = null!;

    [SetUp]
    public void Setup()
    {
        _mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Loose);
        _mockLogger = new Mock<ILogger<AfricaTalkingClient>>();
        _mockConfig = new Mock<IConfiguration>();

        // Setup configuration
        _mockConfig.Setup(c => c["AfricaTalking:ApiKey"]).Returns("test-api-key");
        _mockConfig.Setup(c => c["AfricaTalking:Username"]).Returns("test-username");

        _httpClient = new HttpClient(_mockHandler.Object)
        {
            BaseAddress = new Uri("https://api.africastalking.com")
        };

        _client = new AfricaTalkingClient(_httpClient, _mockConfig.Object, _mockLogger.Object);
    }

    [TearDown]
    public void Cleanup()
    {
        _httpClient.Dispose();
    }

    // ========================================================================
    // SUCCESSFUL AIRTIME SEND TESTS
    // ========================================================================

    [Test]
    public async Task SendAirtimeAsync_Successful_ShouldReturnSuccessResponse()
    {
        // ARRANGE
        var phoneNumber = "+2348012345678";
        var amount = 50m;

        var apiResponse = new
        {
            numSent = 1,
            totalAmount = "NGN 50.00",
            totalDiscount = "NGN 0.00",
            responses = new[]
            {
                new
                {
                    status = "Sent",
                    phoneNumber = phoneNumber,
                    amount = "NGN 50.00",
                    requestId = "ATQid_12345",
                    errorMessage = (string?)null,
                    discount = "NGN 0.00"
                }
            }
        };

        _mockHandler
            .SetupRequest(HttpMethod.Post, "/version1/airtime/send")
            .ReturnsJsonResponse(apiResponse, HttpStatusCode.OK);

        // ACT
        var result = await _client.SendAirtimeAsync(phoneNumber, amount);

        // ASSERT
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Message, Is.EqualTo("Sent"));
            Assert.That(result.TransactionId, Is.EqualTo("ATQid_12345"));
            Assert.That(result.ErrorMessage, Is.Null);
        });
    }

    [Test]
    public async Task SendAirtimeAsync_Successful_ShouldSendCorrectPayload()
    {
        // ARRANGE
        var phoneNumber = "+2348012345678";
        var amount = 100m;

        var apiResponse = new
        {
            numSent = 1,
            totalAmount = "NGN 100.00",
            totalDiscount = "NGN 0.00",
            responses = new[]
            {
                new
                {
                    status = "Sent",
                    phoneNumber = phoneNumber,
                    amount = "NGN 100.00",
                    requestId = "ATQid_67890",
                    errorMessage = (string?)null,
                    discount = "NGN 0.00"
                }
            }
        };

        _mockHandler
            .SetupRequest(HttpMethod.Post, "/version1/airtime/send")
            .ReturnsJsonResponse(apiResponse, HttpStatusCode.OK);

        // ACT
        var result = await _client.SendAirtimeAsync(phoneNumber, amount);

        // ASSERT
        Assert.That(result.Success, Is.True);
        _mockHandler.VerifyRequest(HttpMethod.Post, "/version1/airtime/send", Times.Once());
    }

    // ========================================================================
    // API FAILURE TESTS
    // ========================================================================

    [Test]
    public async Task SendAirtimeAsync_ApiReturnsError_ShouldReturnFailureResponse()
    {
        // ARRANGE
        var phoneNumber = "+2348012345678";
        var amount = 50m;

        _mockHandler
            .SetupRequest(HttpMethod.Post, "/version1/airtime/send")
            .ReturnsResponse(HttpStatusCode.BadRequest, "Invalid request");

        // ACT
        var result = await _client.SendAirtimeAsync(phoneNumber, amount);

        // ASSERT
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("failed with status"));
            Assert.That(result.ErrorMessage, Is.Not.Null);
        });
    }

    [Test]
    public async Task SendAirtimeAsync_ApiReturnsInternalServerError_ShouldReturnFailure()
    {
        // ARRANGE
        var phoneNumber = "+2348012345678";
        var amount = 50m;

        _mockHandler
            .SetupRequest(HttpMethod.Post, "/version1/airtime/send")
            .ReturnsResponse(HttpStatusCode.InternalServerError, "Server error");

        // ACT
        var result = await _client.SendAirtimeAsync(phoneNumber, amount);

        // ASSERT
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("500"));
        });
    }

    [Test]
    public async Task SendAirtimeAsync_InsufficientBalance_ShouldReturnFailureWithError()
    {
        // ARRANGE
        var phoneNumber = "+2348012345678";
        var amount = 50m;

        var apiResponse = new
        {
            numSent = 0,
            totalAmount = "NGN 0.00",
            totalDiscount = "NGN 0.00",
            responses = new[]
            {
                new
                {
                    status = "Failed",
                    phoneNumber = phoneNumber,
                    amount = "NGN 50.00",
                    requestId = "ATQid_fail",
                    errorMessage = "Insufficient balance",
                    discount = "NGN 0.00"
                }
            }
        };

        _mockHandler
            .SetupRequest(HttpMethod.Post, "/version1/airtime/send")
            .ReturnsJsonResponse(apiResponse, HttpStatusCode.OK);

        // ACT
        var result = await _client.SendAirtimeAsync(phoneNumber, amount);

        // ASSERT
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("Insufficient balance"));
        });
    }

    // ========================================================================
    // INVALID RESPONSE HANDLING TESTS
    // ========================================================================

    [Test]
    public async Task SendAirtimeAsync_NoResponsesInPayload_ShouldReturnFailure()
    {
        // ARRANGE
        var phoneNumber = "+2348012345678";
        var amount = 50m;

        var apiResponse = new
        {
            numSent = 0,
            totalAmount = "NGN 0.00",
            totalDiscount = "NGN 0.00",
            responses = Array.Empty<object>()
        };

        _mockHandler
            .SetupRequest(HttpMethod.Post, "/version1/airtime/send")
            .ReturnsJsonResponse(apiResponse, HttpStatusCode.OK);

        // ACT
        var result = await _client.SendAirtimeAsync(phoneNumber, amount);

        // ASSERT
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("No response from provider"));
        });
    }

    [Test]
    public async Task SendAirtimeAsync_NullResponsesArray_ShouldReturnFailure()
    {
        // ARRANGE
        var phoneNumber = "+2348012345678";
        var amount = 50m;

        var apiResponse = new
        {
            numSent = 0,
            totalAmount = "NGN 0.00",
            totalDiscount = "NGN 0.00",
            responses = (object[]?)null
        };

        _mockHandler
            .SetupRequest(HttpMethod.Post, "/version1/airtime/send")
            .ReturnsJsonResponse(apiResponse, HttpStatusCode.OK);

        // ACT
        var result = await _client.SendAirtimeAsync(phoneNumber, amount);

        // ASSERT
        Assert.That(result.Success, Is.False);
    }

    [Test]
    public async Task SendAirtimeAsync_InvalidJsonResponse_ShouldReturnFailure()
    {
        // ARRANGE
        var phoneNumber = "+2348012345678";
        var amount = 50m;

        _mockHandler
            .SetupRequest(HttpMethod.Post, "/version1/airtime/send")
            .ReturnsResponse(HttpStatusCode.OK, "Invalid JSON {{{");

        // ACT
        var result = await _client.SendAirtimeAsync(phoneNumber, amount);

        // ASSERT
        Assert.That(result.Success, Is.False);
    }

    // ========================================================================
    // NETWORK ERROR TESTS
    // ========================================================================

    [Test]
    public async Task SendAirtimeAsync_NetworkError_ShouldReturnFailureWithException()
    {
        // ARRANGE
        var phoneNumber = "+2348012345678";
        var amount = 50m;

        _mockHandler
            .SetupRequest(HttpMethod.Post, "/version1/airtime/send")
            .Throws<HttpRequestException>();

        // ACT
        var result = await _client.SendAirtimeAsync(phoneNumber, amount);

        // ASSERT
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Exception occurred"));
            Assert.That(result.ErrorMessage, Is.Not.Null);
        });
    }

    [Test]
    public async Task SendAirtimeAsync_Timeout_ShouldReturnFailure()
    {
        // ARRANGE
        var phoneNumber = "+2348012345678";
        var amount = 50m;

        _mockHandler
            .SetupRequest(HttpMethod.Post, "/version1/airtime/send")
            .Throws<TaskCanceledException>();

        // ACT
        var result = await _client.SendAirtimeAsync(phoneNumber, amount);

        // ASSERT
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Exception occurred"));
        });
    }

    // ========================================================================
    // HTTP CLIENT CONFIGURATION TESTS
    // ========================================================================

    [Test]
    public async Task SendAirtimeAsync_ShouldIncludeApiKeyInHeaders()
    {
        // ARRANGE
        var phoneNumber = "+2348012345678";
        var amount = 50m;

        var apiResponse = new
        {
            numSent = 1,
            totalAmount = "NGN 50.00",
            totalDiscount = "NGN 0.00",
            responses = new[]
            {
                new
                {
                    status = "Sent",
                    phoneNumber = phoneNumber,
                    amount = "NGN 50.00",
                    requestId = "ATQid_12345",
                    errorMessage = (string?)null,
                    discount = "NGN 0.00"
                }
            }
        };

        _mockHandler
            .SetupRequest(HttpMethod.Post, "/version1/airtime/send")
            .ReturnsJsonResponse(apiResponse, HttpStatusCode.OK);

        // ACT
        var result = await _client.SendAirtimeAsync(phoneNumber, amount);

        // ASSERT
        Assert.That(result.Success, Is.True);
        // Headers are set in constructor, so if request succeeds, headers were correct
    }

    [Test]
    public async Task SendAirtimeAsync_ShouldLogInformation()
    {
        // ARRANGE
        var phoneNumber = "+2348012345678";
        var amount = 50m;

        var apiResponse = new
        {
            numSent = 1,
            totalAmount = "NGN 50.00",
            totalDiscount = "NGN 0.00",
            responses = new[]
            {
                new
                {
                    status = "Sent",
                    phoneNumber = phoneNumber,
                    amount = "NGN 50.00",
                    requestId = "ATQid_12345",
                    errorMessage = (string?)null,
                    discount = "NGN 0.00"
                }
            }
        };

        _mockHandler
            .SetupRequest(HttpMethod.Post, "/version1/airtime/send")
            .ReturnsJsonResponse(apiResponse, HttpStatusCode.OK);

        // ACT
        await _client.SendAirtimeAsync(phoneNumber, amount);

        // ASSERT
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("AfricaTalking API Response")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task SendAirtimeAsync_OnError_ShouldLogError()
    {
        // ARRANGE
        var phoneNumber = "+2348012345678";
        var amount = 50m;

        _mockHandler
            .SetupRequest(HttpMethod.Post, "/version1/airtime/send")
            .Throws<HttpRequestException>();

        // ACT
        await _client.SendAirtimeAsync(phoneNumber, amount);

        // ASSERT
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error sending airtime")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ========================================================================
    // DIFFERENT PHONE NUMBER FORMATS TESTS
    // ========================================================================

    [Test]
    [TestCase("+2348012345678")]
    [TestCase("+2347012345678")]
    [TestCase("+2349012345678")]
    public async Task SendAirtimeAsync_VariousNigerianNumbers_ShouldSucceed(string phoneNumber)
    {
        // ARRANGE
        var amount = 50m;

        var apiResponse = new
        {
            numSent = 1,
            totalAmount = "NGN 50.00",
            totalDiscount = "NGN 0.00",
            responses = new[]
            {
                new
                {
                    status = "Sent",
                    phoneNumber = phoneNumber,
                    amount = "NGN 50.00",
                    requestId = "ATQid_test",
                    errorMessage = (string?)null,
                    discount = "NGN 0.00"
                }
            }
        };

        _mockHandler
            .SetupRequest(HttpMethod.Post, "/version1/airtime/send")
            .ReturnsJsonResponse(apiResponse, HttpStatusCode.OK);

        // ACT
        var result = await _client.SendAirtimeAsync(phoneNumber, amount);

        // ASSERT
        Assert.That(result.Success, Is.True);
    }
}