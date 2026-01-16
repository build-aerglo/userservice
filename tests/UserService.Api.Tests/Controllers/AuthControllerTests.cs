using Microsoft.AspNetCore.Mvc;
using Moq;
using UserService.Api.Controllers;
using UserService.Application.DTOs.Auth;
using UserService.Application.Services;
using UserService.Application.Services.Auth0;
using UserService.Domain.Exceptions;

namespace UserService.Api.Tests.Controllers;

[TestFixture]
public class AuthControllerTests
{
    private Mock<IAuth0UserLoginService> _mockAuth0Login = null!;
    private Mock<IAuth0SocialLoginService> _mockSocialLogin = null!;
    private Mock<IRefreshTokenCookieService> _mockRefreshCookie = null!;
    private AuthController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _mockAuth0Login = new Mock<IAuth0UserLoginService>();
        _mockSocialLogin = new Mock<IAuth0SocialLoginService>();
        _mockRefreshCookie = new Mock<IRefreshTokenCookieService>();
        _controller = new AuthController(
            _mockAuth0Login.Object,
            _mockSocialLogin.Object,
            _mockRefreshCookie.Object
        );
    }

    // -----------------------------------------------
    // SOCIAL LOGIN CALLBACK TESTS
    // -----------------------------------------------

    [Test]
    public async Task SocialLoginCallback_ShouldReturnOk_WhenAuthenticationSucceeds()
    {
        // Arrange
        var request = new SocialLoginRequest
        {
            Provider = "google",
            Code = "test-code",
            RedirectUri = "https://example.com/callback"
        };

        var response = new SocialLoginResponse
        {
            AccessToken = "test-access-token",
            IdToken = "test-id-token",
            ExpiresIn = 3600,
            Roles = new List<string> { "end_user" },
            UserId = Guid.NewGuid(),
            IsNewUser = true,
            Provider = "google-oauth2",
            Email = "test@example.com",
            Name = "Test User"
        };

        _mockSocialLogin
            .Setup(s => s.AuthenticateAsync(request))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.SocialLoginCallback(request);

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
    }

    [Test]
    public async Task SocialLoginCallback_ShouldReturnConflict_WhenEmailAlreadyRegisteredWithPassword()
    {
        // Arrange
        var request = new SocialLoginRequest
        {
            Provider = "google",
            Code = "test-code",
            RedirectUri = "https://example.com/callback"
        };

        _mockSocialLogin
            .Setup(s => s.AuthenticateAsync(request))
            .ThrowsAsync(new EmailAlreadyRegisteredWithPasswordException("test@example.com"));

        // Act
        var result = await _controller.SocialLoginCallback(request);

        // Assert
        Assert.That(result, Is.InstanceOf<ConflictObjectResult>());
        var conflictResult = result as ConflictObjectResult;
        Assert.That(conflictResult, Is.Not.Null);

        dynamic? value = conflictResult!.Value;
        Assert.That(value, Is.Not.Null);

        // Use reflection to get the error property
        var errorProperty = value!.GetType().GetProperty("error");
        var messageProperty = value.GetType().GetProperty("message");
        var emailProperty = value.GetType().GetProperty("email");

        Assert.That(errorProperty?.GetValue(value), Is.EqualTo("email_already_registered_with_password"));
        Assert.That(messageProperty?.GetValue(value), Does.Contain("test@example.com"));
        Assert.That(emailProperty?.GetValue(value), Is.EqualTo("test@example.com"));
    }

    [Test]
    public async Task SocialLoginCallback_ShouldReturnBadRequest_WhenInvalidProvider()
    {
        // Arrange
        var request = new SocialLoginRequest
        {
            Provider = "invalid-provider",
            Code = "test-code",
            RedirectUri = "https://example.com/callback"
        };

        _mockSocialLogin
            .Setup(s => s.AuthenticateAsync(request))
            .ThrowsAsync(new InvalidSocialProviderException("invalid-provider"));

        // Act
        var result = await _controller.SocialLoginCallback(request);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null);

        dynamic? value = badRequestResult!.Value;
        Assert.That(value, Is.Not.Null);

        var errorProperty = value!.GetType().GetProperty("error");
        Assert.That(errorProperty?.GetValue(value), Is.EqualTo("invalid_provider"));
    }

    [Test]
    public async Task SocialLoginCallback_ShouldReturnUnauthorized_WhenSocialLoginFails()
    {
        // Arrange
        var request = new SocialLoginRequest
        {
            Provider = "google",
            Code = "invalid-code",
            RedirectUri = "https://example.com/callback"
        };

        _mockSocialLogin
            .Setup(s => s.AuthenticateAsync(request))
            .ThrowsAsync(new SocialLoginException("google-oauth2", "invalid_grant", "Authorization code is invalid"));

        // Act
        var result = await _controller.SocialLoginCallback(request);

        // Assert
        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
        var unauthorizedResult = result as UnauthorizedObjectResult;
        Assert.That(unauthorizedResult, Is.Not.Null);

        dynamic? value = unauthorizedResult!.Value;
        Assert.That(value, Is.Not.Null);

        var errorProperty = value!.GetType().GetProperty("error");
        Assert.That(errorProperty?.GetValue(value), Is.EqualTo("invalid_grant"));
    }

    [Test]
    public async Task SocialLoginCallback_ShouldReturnInternalServerError_WhenUnexpectedErrorOccurs()
    {
        // Arrange
        var request = new SocialLoginRequest
        {
            Provider = "google",
            Code = "test-code",
            RedirectUri = "https://example.com/callback"
        };

        _mockSocialLogin
            .Setup(s => s.AuthenticateAsync(request))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _controller.SocialLoginCallback(request);

        // Assert
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var objectResult = result as ObjectResult;
        Assert.That(objectResult, Is.Not.Null);
        Assert.That(objectResult!.StatusCode, Is.EqualTo(500));

        dynamic? value = objectResult.Value;
        Assert.That(value, Is.Not.Null);

        var errorProperty = value!.GetType().GetProperty("error");
        Assert.That(errorProperty?.GetValue(value), Is.EqualTo("server_error"));
    }

    // -----------------------------------------------
    // GET AUTHORIZATION URL TESTS
    // -----------------------------------------------

    [Test]
    public void GetAuthorizationUrl_ShouldReturnOk_WhenRequestIsValid()
    {
        // Arrange
        var request = new SocialAuthUrlRequest
        {
            Provider = "google",
            RedirectUri = "https://example.com/callback",
            State = "test-state"
        };

        var response = new SocialAuthUrlResponse
        {
            AuthorizationUrl = "https://auth0.com/authorize?...",
            State = "test-state"
        };

        _mockSocialLogin
            .Setup(s => s.GetAuthorizationUrl(request))
            .Returns(response);

        // Act
        var result = _controller.GetAuthorizationUrl(request);

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.Value, Is.EqualTo(response));
    }

    [Test]
    public void GetAuthorizationUrl_ShouldReturnBadRequest_WhenInvalidProvider()
    {
        // Arrange
        var request = new SocialAuthUrlRequest
        {
            Provider = "invalid-provider",
            RedirectUri = "https://example.com/callback"
        };

        _mockSocialLogin
            .Setup(s => s.GetAuthorizationUrl(request))
            .Throws(new InvalidSocialProviderException("invalid-provider"));

        // Act
        var result = _controller.GetAuthorizationUrl(request);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null);

        dynamic? value = badRequestResult!.Value;
        Assert.That(value, Is.Not.Null);

        var errorProperty = value!.GetType().GetProperty("error");
        Assert.That(errorProperty?.GetValue(value), Is.EqualTo("invalid_provider"));
    }

    // -----------------------------------------------
    // GET SOCIAL PROVIDERS TESTS
    // -----------------------------------------------

    [Test]
    public void GetSocialProviders_ShouldReturnOk_WithProviderList()
    {
        // Act
        var result = _controller.GetSocialProviders();

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.Value, Is.Not.Null);
    }
}
