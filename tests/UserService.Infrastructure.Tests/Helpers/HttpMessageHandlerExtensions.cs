using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using Moq;
using Moq.Protected;

namespace UserService.Infrastructure.Tests.Helpers;

internal static class HttpMessageHandlerExtensions
{
    /// <summary>
    /// Prepares a mocked HttpMessageHandler to intercept requests to a specific path and method,
    /// and return a JSON response.
    /// </summary>
    public static Mock<HttpMessageHandler> SetupRequest(
        this Mock<HttpMessageHandler> mockHandler,
        HttpMethod method,
        string path)
    {
        // Return the mock so methods can be chained
        return mockHandler;
    }

    /// <summary>
    /// Configures the mock handler to return a plain text HTTP response for a specific request.
    /// Should be called after SetupRequest to configure the response for that specific path.
    /// </summary>
    public static void ReturnsResponse(
        this Mock<HttpMessageHandler> mockHandler,
        HttpStatusCode statusCode,
        string content = "")
    {
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content)
            });
    }

    /// <summary>
    /// Configures the mock handler to return a JSON HTTP response for the most recently set up request.
    /// This creates a new setup that matches ANY request - use SetupRequestWithJsonResponse for specific URLs.
    /// </summary>
    public static void ReturnsJsonResponse<T>(
        this Mock<HttpMessageHandler> mockHandler,
        T content,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = JsonContent.Create(content)
            });
    }

    /// <summary>
    /// Sets up a mock handler to return a JSON response for a specific HTTP method and path.
    /// This is the recommended method for setting up specific URL responses.
    /// </summary>
    public static void SetupRequestWithJsonResponse<T>(
        this Mock<HttpMessageHandler> mockHandler,
        HttpMethod method,
        string path,
        T content,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == method &&
                    req.RequestUri != null &&
                    req.RequestUri.PathAndQuery.EndsWith(path, StringComparison.OrdinalIgnoreCase)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = JsonContent.Create(content)
            });
    }

    /// <summary>
    /// Sets up a mock handler to return a plain response for a specific HTTP method and path.
    /// </summary>
    public static void SetupRequestWithResponse(
        this Mock<HttpMessageHandler> mockHandler,
        HttpMethod method,
        string path,
        HttpStatusCode statusCode,
        string content = "")
    {
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == method &&
                    req.RequestUri != null &&
                    req.RequestUri.PathAndQuery.EndsWith(path, StringComparison.OrdinalIgnoreCase)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content)
            });
    }

    /// <summary>
    /// Configures the mock handler to throw an exception (e.g., network error).
    /// </summary>
    public static void Throws<TException>(
        this Mock<HttpMessageHandler> mockHandler)
        where TException : Exception, new()
    {
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TException());
    }

    /// <summary>
    /// Sets up a mock handler to throw an exception for a specific HTTP method and path.
    /// </summary>
    public static void SetupRequestWithException<TException>(
        this Mock<HttpMessageHandler> mockHandler,
        HttpMethod method,
        string path)
        where TException : Exception, new()
    {
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == method &&
                    req.RequestUri != null &&
                    req.RequestUri.PathAndQuery.EndsWith(path, StringComparison.OrdinalIgnoreCase)),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TException());
    }
    
    /// <summary>
    /// Verifies that a request was made to the specified path and method
    /// </summary>
    public static void VerifyRequest(
        this Mock<HttpMessageHandler> mockHandler,
        HttpMethod method,
        string path,
        Times times)
    {
        mockHandler.Protected().Verify(
            "SendAsync",
            times,
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == method &&
                req.RequestUri != null &&
                req.RequestUri.PathAndQuery.EndsWith(path, StringComparison.OrdinalIgnoreCase)),
            ItExpr.IsAny<CancellationToken>()
        );
    }
}