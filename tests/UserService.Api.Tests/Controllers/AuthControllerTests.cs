using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using UserService.Api.Controllers;
using UserService.Application.DTOs;
using UserService.Application.DTOs.Auth;
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

        // Setup HttpContext for cookie operations
        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    // ========================================================================
    // LOGIN ENDPOINT TESTS
    // ========================================================================

    [Test]
    public async Task Login_ShouldReturnOk_WhenCredentialsValid()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            Password = "password123"
        };

        var tokenResponse = new TokenResponse
        {
            Access_Token = "access_token_123",
            Id_Token = "id_token_456",
            Refresh_Token = "refresh_token_789",
            Expires_In = 3600,
            Roles = new List<string> { "user" },
            Id = Guid.NewGuid()
        };

        _mockAuth0Login
            .Setup(s => s.LoginAsync(loginRequest.Email, loginRequest.Password))
            .ReturnsAsync(tokenResponse);

        // Act
        var result = await _controller.Login(loginRequest);

        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.StatusCode, Is.EqualTo(200));

        _mockRefreshCookie.Verify(
            r => r.SetRefreshToken(It.IsAny<HttpResponse>(), tokenResponse.Refresh_Token!),
            Times.Once
        );

        var response = okResult.Value;
        Assert.That(response, Is.Not.Null);

        var responseType = response!.GetType();
        var accessToken = responseType.GetProperty("access_token")!.GetValue(response) as string;
        var idToken = responseType.GetProperty("id_token")!.GetValue(response) as string;
        var expiresIn = (int)responseType.GetProperty("expires_in")!.GetValue(response)!;
        var id = (Guid?)responseType.GetProperty("id")!.GetValue(response);

        Assert.That(accessToken, Is.EqualTo("access_token_123"));
        Assert.That(idToken, Is.EqualTo("id_token_456"));
        Assert.That(expiresIn, Is.EqualTo(3600));
        Assert.That(id, Is.EqualTo(tokenResponse.Id));
    }

    [Test]
    public async Task Login_ShouldReturnUnauthorized_WhenRefreshTokenMissing()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            Password = "password123"
        };

        var tokenResponse = new TokenResponse
        {
            Access_Token = "access_token_123",
            Id_Token = "id_token_456",
            Refresh_Token = null, // Missing refresh token
            Expires_In = 3600,
            Roles = new List<string> { "user" },
            Id = Guid.NewGuid()
        };

        _mockAuth0Login
            .Setup(s => s.LoginAsync(loginRequest.Email, loginRequest.Password))
            .ReturnsAsync(tokenResponse);

        // Act
        var result = await _controller.Login(loginRequest);

        // Assert
        var unauthorizedResult = result as UnauthorizedObjectResult;
        Assert.That(unauthorizedResult, Is.Not.Null);
        Assert.That(unauthorizedResult!.StatusCode, Is.EqualTo(401));

        var response = unauthorizedResult.Value;
        var responseType = response!.GetType();
        var error = responseType.GetProperty("error")!.GetValue(response) as string;
        Assert.That(error, Does.Contain("Refresh token not returned"));
    }

    [Test]
    public async Task Login_ShouldReturnUnauthorized_WhenCredentialsInvalid()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            Password = "wrongpassword"
        };

        _mockAuth0Login
            .Setup(s => s.LoginAsync(loginRequest.Email, loginRequest.Password))
            .ThrowsAsync(new AuthLoginFailedException("Invalid credentials"));

        // Act
        var result = await _controller.Login(loginRequest);

        // Assert
        var unauthorizedResult = result as UnauthorizedObjectResult;
        Assert.That(unauthorizedResult, Is.Not.Null);
        Assert.That(unauthorizedResult!.StatusCode, Is.EqualTo(401));

        var response = unauthorizedResult.Value;
        var responseType = response!.GetType();
        var error = responseType.GetProperty("error")!.GetValue(response) as string;
        var message = responseType.GetProperty("message")!.GetValue(response) as string;

        Assert.That(error, Is.EqualTo("invalid_credentials"));
        Assert.That(message, Is.EqualTo("Invalid credentials"));
    }

    [Test]
    public async Task Login_ShouldReturnInternalServerError_OnUnexpectedException()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            Password = "password123"
        };

        _mockAuth0Login
            .Setup(s => s.LoginAsync(loginRequest.Email, loginRequest.Password))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _controller.Login(loginRequest);

        // Assert
        var errorResult = result as ObjectResult;
        Assert.That(errorResult, Is.Not.Null);
        Assert.That(errorResult!.StatusCode, Is.EqualTo(500));

        var response = errorResult.Value;
        var responseType = response!.GetType();
        var error = responseType.GetProperty("error")!.GetValue(response) as string;
        Assert.That(error, Is.EqualTo("server_error"));
    }

    // ========================================================================
    // REFRESH ENDPOINT TESTS
    // ========================================================================

    [Test]
    public async Task Refresh_ShouldReturnOk_WhenRefreshTokenValid()
    {
        // Arrange
        var refreshToken = "valid_refresh_token";
        var tokenResponse = new TokenResponse
        {
            Access_Token = "new_access_token",
            Refresh_Token = "new_refresh_token",
            Expires_In = 3600
        };

        _mockRefreshCookie
            .Setup(r => r.GetRefreshToken(It.IsAny<HttpRequest>()))
            .Returns(refreshToken);

        _mockAuth0Login
            .Setup(s => s.RefreshAsync(refreshToken))
            .ReturnsAsync(tokenResponse);

        // Act
        var result = await _controller.Refresh();

        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.StatusCode, Is.EqualTo(200));

        _mockRefreshCookie.Verify(
            r => r.SetRefreshToken(It.IsAny<HttpResponse>(), "new_refresh_token"),
            Times.Once
        );

        var response = okResult.Value;
        var responseType = response!.GetType();
        var accessToken = responseType.GetProperty("access_token")!.GetValue(response) as string;
        var expiresIn = (int)responseType.GetProperty("expires_in")!.GetValue(response)!;

        Assert.That(accessToken, Is.EqualTo("new_access_token"));
        Assert.That(expiresIn, Is.EqualTo(3600));
    }

    [Test]
    public async Task Refresh_ShouldReturnOk_WhenNoNewRefreshToken()
    {
        // Arrange
        var refreshToken = "valid_refresh_token";
        var tokenResponse = new TokenResponse
        {
            Access_Token = "new_access_token",
            Refresh_Token = null, // No new refresh token
            Expires_In = 3600
        };

        _mockRefreshCookie
            .Setup(r => r.GetRefreshToken(It.IsAny<HttpRequest>()))
            .Returns(refreshToken);

        _mockAuth0Login
            .Setup(s => s.RefreshAsync(refreshToken))
            .ReturnsAsync(tokenResponse);

        // Act
        var result = await _controller.Refresh();

        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);

        // Should not set refresh token if none provided
        _mockRefreshCookie.Verify(
            r => r.SetRefreshToken(It.IsAny<HttpResponse>(), It.IsAny<string>()),
            Times.Never
        );
    }

    [Test]
    public async Task Refresh_ShouldReturnUnauthorized_WhenRefreshTokenMissing()
    {
        // Arrange
        _mockRefreshCookie
            .Setup(r => r.GetRefreshToken(It.IsAny<HttpRequest>()))
            .Returns((string?)null);

        // Act
        var result = await _controller.Refresh();

        // Assert
        var unauthorizedResult = result as UnauthorizedObjectResult;
        Assert.That(unauthorizedResult, Is.Not.Null);
        Assert.That(unauthorizedResult!.StatusCode, Is.EqualTo(401));
        Assert.That(unauthorizedResult.Value, Is.EqualTo("Missing refresh token cookie"));

        _mockAuth0Login.Verify(
            s => s.RefreshAsync(It.IsAny<string>()),
            Times.Never
        );
    }

    // ========================================================================
    // LOGOUT ENDPOINT TESTS
    // ========================================================================

    [Test]
    public void Logout_ShouldReturnNoContent_AndClearCookie()
    {
        // Act
        var result = _controller.Logout();

        // Assert
        var noContentResult = result as NoContentResult;
        Assert.That(noContentResult, Is.Not.Null);
        Assert.That(noContentResult!.StatusCode, Is.EqualTo(204));

        _mockRefreshCookie.Verify(
            r => r.ClearRefreshToken(It.IsAny<HttpResponse>()),
            Times.Once
        );
    }

    // ========================================================================
    // GET SOCIAL PROVIDERS ENDPOINT TESTS
    // ========================================================================

    [Test]
    public void GetSocialProviders_ShouldReturnListOfProviders()
    {
        // Act
        var result = _controller.GetSocialProviders();

        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.StatusCode, Is.EqualTo(200));

        var providers = okResult.Value as object[];
        Assert.That(providers, Is.Not.Null);
        Assert.That(providers!.Length, Is.EqualTo(6));

        // Verify first provider structure
        var firstProvider = providers[0];
        var providerType = firstProvider.GetType();
        var id = providerType.GetProperty("id")!.GetValue(firstProvider) as string;
        var name = providerType.GetProperty("name")!.GetValue(firstProvider) as string;
        var icon = providerType.GetProperty("icon")!.GetValue(firstProvider) as string;

        Assert.That(id, Is.EqualTo("google-oauth2"));
        Assert.That(name, Is.EqualTo("Google"));
        Assert.That(icon, Is.EqualTo("google"));
    }

    // ========================================================================
    // GET AUTHORIZATION URL ENDPOINT TESTS
    // ========================================================================

    [Test]
    public void GetAuthorizationUrl_ShouldReturnOk_WhenProviderValid()
    {
        // Arrange
        var request = new SocialAuthUrlRequest
        {
            Provider = "google-oauth2",
            RedirectUri = "https://example.com/callback"
        };

        var response = new SocialAuthUrlResponse
        {
            AuthorizationUrl = "https://auth0.com/authorize?...",
            State = "random_state_123"
        };

        _mockSocialLogin
            .Setup(s => s.GetAuthorizationUrl(request))
            .Returns(response);

        // Act
        var result = _controller.GetAuthorizationUrl(request);

        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.Value, Is.EqualTo(response));
    }

    [Test]
    public void GetAuthorizationUrl_ShouldReturnBadRequest_WhenProviderInvalid()
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
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null);
        Assert.That(badRequestResult!.StatusCode, Is.EqualTo(400));

        var responseValue = badRequestResult.Value;
        var responseType = responseValue!.GetType();
        var error = responseType.GetProperty("error")!.GetValue(responseValue) as string;
        Assert.That(error, Is.EqualTo("invalid_provider"));
    }

    // ========================================================================
    // SOCIAL LOGIN CALLBACK ENDPOINT TESTS
    // ========================================================================

    [Test]
    public async Task SocialLoginCallback_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        var request = new SocialLoginRequest
        {
            Provider = "google-oauth2",
            Code = "auth_code_123",
            State = "state_123",
            RedirectUri = "https://example.com/callback"
        };

        var response = new SocialLoginResponse
        {
            AccessToken = "access_token",
            IdToken = "id_token",
            ExpiresIn = 3600,
            Roles = new List<string> { "user" },
            UserId = Guid.NewGuid(),
            IsNewUser = false,
            Provider = "google-oauth2",
            Email = "user@example.com",
            Name = "John Doe"
        };

        _mockSocialLogin
            .Setup(s => s.AuthenticateAsync(request))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.SocialLoginCallback(request);

        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.StatusCode, Is.EqualTo(200));

        var responseValue = okResult.Value;
        var responseType = responseValue!.GetType();
        var email = responseType.GetProperty("email")!.GetValue(responseValue) as string;
        var isNewUser = (bool)responseType.GetProperty("is_new_user")!.GetValue(responseValue)!;

        Assert.That(email, Is.EqualTo("user@example.com"));
        Assert.That(isNewUser, Is.False);
    }

    [Test]
    public async Task SocialLoginCallback_ShouldReturnBadRequest_WhenProviderInvalid()
    {
        // Arrange
        var request = new SocialLoginRequest
        {
            Provider = "invalid-provider",
            Code = "auth_code_123",
            State = "state_123",
            RedirectUri = "https://example.com/callback"
        };

        _mockSocialLogin
            .Setup(s => s.AuthenticateAsync(request))
            .ThrowsAsync(new InvalidSocialProviderException("invalid-provider"));

        // Act
        var result = await _controller.SocialLoginCallback(request);

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null);
        Assert.That(badRequestResult!.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public async Task SocialLoginCallback_ShouldReturnUnauthorized_WhenSocialLoginFails()
    {
        // Arrange
        var request = new SocialLoginRequest
        {
            Provider = "google-oauth2",
            Code = "invalid_code",
            State = "state_123",
            RedirectUri = "https://example.com/callback"
        };

        _mockSocialLogin
            .Setup(s => s.AuthenticateAsync(request))
            .ThrowsAsync(new SocialLoginException("google-oauth2", "access_denied", "User denied access"));

        // Act
        var result = await _controller.SocialLoginCallback(request);

        // Assert
        var unauthorizedResult = result as UnauthorizedObjectResult;
        Assert.That(unauthorizedResult, Is.Not.Null);
        Assert.That(unauthorizedResult!.StatusCode, Is.EqualTo(401));

        var responseValue = unauthorizedResult.Value;
        var responseType = responseValue!.GetType();
        var error = responseType.GetProperty("error")!.GetValue(responseValue) as string;
        var provider = responseType.GetProperty("provider")!.GetValue(responseValue) as string;

        Assert.That(error, Is.EqualTo("access_denied"));
        Assert.That(provider, Is.EqualTo("google-oauth2"));
    }

    [Test]
    public async Task SocialLoginCallback_ShouldReturnInternalServerError_OnUnexpectedException()
    {
        // Arrange
        var request = new SocialLoginRequest
        {
            Provider = "google-oauth2",
            Code = "auth_code_123",
            State = "state_123",
            RedirectUri = "https://example.com/callback"
        };

        _mockSocialLogin
            .Setup(s => s.AuthenticateAsync(request))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _controller.SocialLoginCallback(request);

        // Assert
        var errorResult = result as ObjectResult;
        Assert.That(errorResult, Is.Not.Null);
        Assert.That(errorResult!.StatusCode, Is.EqualTo(500));

        var responseValue = errorResult.Value;
        var responseType = responseValue!.GetType();
        var error = responseType.GetProperty("error")!.GetValue(responseValue) as string;
        Assert.That(error, Is.EqualTo("server_error"));
    }

    // ========================================================================
    // LINK SOCIAL ACCOUNT ENDPOINT TESTS
    // ========================================================================

    [Test]
    public async Task LinkSocialAccount_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var request = new LinkSocialAccountRequest
        {
            Provider = "google-oauth2",
            AccessToken = "provider_access_token"
        };

        var linkedAccount = new LinkedSocialAccountDto
        {
            Id = Guid.NewGuid(),
            Provider = "google-oauth2",
            ProviderUserId = "google_user_123",
            Email = "user@example.com",
            Name = "John Doe",
            LinkedAt = DateTime.UtcNow
        };

        _mockSocialLogin
            .Setup(s => s.LinkAccountAsync(userId, request))
            .ReturnsAsync(linkedAccount);

        // Act
        var result = await _controller.LinkSocialAccount(request);

        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.Value, Is.EqualTo(linkedAccount));
    }

    [Test]
    public async Task LinkSocialAccount_ShouldReturnUnauthorized_WhenUserIdMissing()
    {
        // Arrange - No user claims set
        var request = new LinkSocialAccountRequest
        {
            Provider = "google-oauth2",
            AccessToken = "provider_access_token"
        };

        // Act
        var result = await _controller.LinkSocialAccount(request);

        // Assert
        var unauthorizedResult = result as UnauthorizedObjectResult;
        Assert.That(unauthorizedResult, Is.Not.Null);
        Assert.That(unauthorizedResult!.StatusCode, Is.EqualTo(401));

        var responseValue = unauthorizedResult.Value;
        var responseType = responseValue!.GetType();
        var error = responseType.GetProperty("error")!.GetValue(responseValue) as string;
        Assert.That(error, Is.EqualTo("invalid_token"));
    }

    [Test]
    public async Task LinkSocialAccount_ShouldReturnBadRequest_WhenProviderInvalid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var request = new LinkSocialAccountRequest
        {
            Provider = "invalid-provider",
            AccessToken = "provider_access_token"
        };

        _mockSocialLogin
            .Setup(s => s.LinkAccountAsync(userId, request))
            .ThrowsAsync(new InvalidSocialProviderException("invalid-provider"));

        // Act
        var result = await _controller.LinkSocialAccount(request);

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null);
        Assert.That(badRequestResult!.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public async Task LinkSocialAccount_ShouldReturnConflict_WhenAccountAlreadyLinked()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var request = new LinkSocialAccountRequest
        {
            Provider = "google-oauth2",
            AccessToken = "provider_access_token"
        };

        _mockSocialLogin
            .Setup(s => s.LinkAccountAsync(userId, request))
            .ThrowsAsync(new SocialAccountAlreadyLinkedException("google-oauth2"));

        // Act
        var result = await _controller.LinkSocialAccount(request);

        // Assert
        var conflictResult = result as ConflictObjectResult;
        Assert.That(conflictResult, Is.Not.Null);
        Assert.That(conflictResult!.StatusCode, Is.EqualTo(409));

        var responseValue = conflictResult.Value;
        var responseType = responseValue!.GetType();
        var error = responseType.GetProperty("error")!.GetValue(responseValue) as string;
        var provider = responseType.GetProperty("provider")!.GetValue(responseValue) as string;

        Assert.That(error, Is.EqualTo("already_linked"));
        Assert.That(provider, Is.EqualTo("google-oauth2"));
    }

    [Test]
    public async Task LinkSocialAccount_ShouldReturnBadRequest_OnSocialLoginException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var request = new LinkSocialAccountRequest
        {
            Provider = "google-oauth2",
            AccessToken = "invalid_token"
        };

        _mockSocialLogin
            .Setup(s => s.LinkAccountAsync(userId, request))
            .ThrowsAsync(new SocialLoginException("google-oauth2", "invalid_token", "Token is invalid"));

        // Act
        var result = await _controller.LinkSocialAccount(request);

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null);
        Assert.That(badRequestResult!.StatusCode, Is.EqualTo(400));
    }

    // ========================================================================
    // UNLINK SOCIAL ACCOUNT ENDPOINT TESTS
    // ========================================================================

    [Test]
    public async Task UnlinkSocialAccount_ShouldReturnNoContent_WhenSuccessful()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var provider = "google-oauth2";

        _mockSocialLogin
            .Setup(s => s.UnlinkAccountAsync(userId, provider))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UnlinkSocialAccount(provider);

        // Assert
        var noContentResult = result as NoContentResult;
        Assert.That(noContentResult, Is.Not.Null);
        Assert.That(noContentResult!.StatusCode, Is.EqualTo(204));

        _mockSocialLogin.Verify(s => s.UnlinkAccountAsync(userId, provider), Times.Once);
    }

    [Test]
    public async Task UnlinkSocialAccount_ShouldReturnUnauthorized_WhenUserIdMissing()
    {
        // Arrange - No user claims set
        var provider = "google-oauth2";

        // Act
        var result = await _controller.UnlinkSocialAccount(provider);

        // Assert
        var unauthorizedResult = result as UnauthorizedObjectResult;
        Assert.That(unauthorizedResult, Is.Not.Null);
        Assert.That(unauthorizedResult!.StatusCode, Is.EqualTo(401));
    }

    [Test]
    public async Task UnlinkSocialAccount_ShouldReturnBadRequest_WhenProviderInvalid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var provider = "invalid-provider";

        _mockSocialLogin
            .Setup(s => s.UnlinkAccountAsync(userId, provider))
            .ThrowsAsync(new InvalidSocialProviderException(provider));

        // Act
        var result = await _controller.UnlinkSocialAccount(provider);

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null);
        Assert.That(badRequestResult!.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public async Task UnlinkSocialAccount_ShouldReturnNotFound_WhenAccountNotLinked()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var provider = "google-oauth2";

        _mockSocialLogin
            .Setup(s => s.UnlinkAccountAsync(userId, provider))
            .ThrowsAsync(new SocialAccountNotLinkedException(provider));

        // Act
        var result = await _controller.UnlinkSocialAccount(provider);

        // Assert
        var notFoundResult = result as NotFoundObjectResult;
        Assert.That(notFoundResult, Is.Not.Null);
        Assert.That(notFoundResult!.StatusCode, Is.EqualTo(404));

        var responseValue = notFoundResult.Value;
        var responseType = responseValue!.GetType();
        var error = responseType.GetProperty("error")!.GetValue(responseValue) as string;
        var providerValue = responseType.GetProperty("provider")!.GetValue(responseValue) as string;

        Assert.That(error, Is.EqualTo("not_linked"));
        Assert.That(providerValue, Is.EqualTo(provider));
    }

    // ========================================================================
    // GET LINKED ACCOUNTS ENDPOINT TESTS
    // ========================================================================

    [Test]
    public async Task GetLinkedAccounts_ShouldReturnOk_WithAccountsList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var linkedAccounts = new List<LinkedSocialAccountDto>
        {
            new LinkedSocialAccountDto
            {
                Id = Guid.NewGuid(),
                Provider = "google-oauth2",
                ProviderUserId = "google_123",
                Email = "user@gmail.com",
                Name = "John Doe",
                LinkedAt = DateTime.UtcNow.AddDays(-30)
            },
            new LinkedSocialAccountDto
            {
                Id = Guid.NewGuid(),
                Provider = "facebook",
                ProviderUserId = "fb_456",
                Email = "user@facebook.com",
                Name = "John Doe",
                LinkedAt = DateTime.UtcNow.AddDays(-10)
            }
        };

        _mockSocialLogin
            .Setup(s => s.GetLinkedAccountsAsync(userId))
            .ReturnsAsync(linkedAccounts);

        // Act
        var result = await _controller.GetLinkedAccounts();

        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.StatusCode, Is.EqualTo(200));

        var accounts = okResult.Value as List<LinkedSocialAccountDto>;
        Assert.That(accounts, Is.Not.Null);
        Assert.That(accounts!.Count, Is.EqualTo(2));
        Assert.That(accounts[0].Provider, Is.EqualTo("google-oauth2"));
        Assert.That(accounts[1].Provider, Is.EqualTo("facebook"));
    }

    [Test]
    public async Task GetLinkedAccounts_ShouldReturnOk_WithEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var linkedAccounts = new List<LinkedSocialAccountDto>();

        _mockSocialLogin
            .Setup(s => s.GetLinkedAccountsAsync(userId))
            .ReturnsAsync(linkedAccounts);

        // Act
        var result = await _controller.GetLinkedAccounts();

        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);

        var accounts = okResult.Value as List<LinkedSocialAccountDto>;
        Assert.That(accounts, Is.Not.Null);
        Assert.That(accounts!.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task GetLinkedAccounts_ShouldReturnUnauthorized_WhenUserIdMissing()
    {
        // Arrange - No user claims set

        // Act
        var result = await _controller.GetLinkedAccounts();

        // Assert
        var unauthorizedResult = result as UnauthorizedObjectResult;
        Assert.That(unauthorizedResult, Is.Not.Null);
        Assert.That(unauthorizedResult!.StatusCode, Is.EqualTo(401));
    }

    // ========================================================================
    // HELPER METHODS
    // ========================================================================

    private void SetupAuthenticatedUser(Guid userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("sub", userId.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext.HttpContext.User = claimsPrincipal;
    }
}
