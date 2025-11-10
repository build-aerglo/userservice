using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using Moq;
using Moq.Protected;

namespace UserService.Infrastructure.Tests.Helpers;

internal static class HttpMessageHandlerExtensions
{
    /// <summary>
    /// Prepares a mocked HttpMessageHandler to intercept requests to a specific path and method.
    /// </summary>
    public static Mock<HttpMessageHandler> SetupRequest(
        this Mock<HttpMessageHandler> mockHandler,
        HttpMethod method,
        string path)
    {
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == method &&
                    req.RequestUri != null &&
                    req.RequestUri.PathAndQuery.EndsWith(path, StringComparison.OrdinalIgnoreCase)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)); // default
        return mockHandler;
    }

    /// <summary>
    /// Configures the mock handler to return a plain text HTTP response.
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
    /// Configures the mock handler to return a JSON HTTP response.
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
}
