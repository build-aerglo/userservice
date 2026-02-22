using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using UserService.Api.Controllers;
using UserService.Application.DTOs;
using UserService.Application.DTOs.Auth;
using UserService.Application.Interfaces;
using UserService.Application.Services.Auth0;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Domain.Repositories;

namespace UserService.Api.Tests.Controllers;

[TestFixture]
public class AuthControllerTests
{
    private Mock<IAuth0UserLoginService> _mockAuth0Login = null!;
    private Mock<IAuth0SocialLoginService> _mockSocialLogin = null!;
    private Mock<IRefreshTokenCookieService> _mockRefreshCookie = null!;
    private Mock<IUserRepository> _mockUserRepository = null!;
    private Mock<IPointsService> _mockPointsService = null!;
    private Mock<IEmailUpdateRequestRepository> _mockEmailUpdateRequestRepository = null!;
    private Mock<ILogger<AuthController>> _mockLogger = null!;
    private AuthController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _mockAuth0Login = new Mock<IAuth0UserLoginService>();
        _mockSocialLogin = new Mock<IAuth0SocialLoginService>();
        _mockRefreshCookie = new Mock<IRefreshTokenCookieService>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPointsService = new Mock<IPointsService>();
        _mockEmailUpdateRequestRepository = new Mock<IEmailUpdateRequestRepository>();
        _mockLogger = new Mock<ILogger<AuthController>>();

        _controller = new AuthController(
            _mockAuth0Login.Object,
            _mockSocialLogin.Object,
            _mockRefreshCookie.Object,
            _mockUserRepository.Object,
            _mockPointsService.Object,
            _mockEmailUpdateRequestRepository.Object,
            _mockLogger.Object);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    private void SetupUserClaims(Guid userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("sub", userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    // ========================================================================
    // LOGIN TESTS
    // ========================================================================

    [Test]
    public async Task Login_NullDto_ShouldReturnBadRequest()
    {
        var result = await _controller.Login(null!, CancellationToken.None);
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Login_EmptyEmail_ShouldReturnBadRequest()
    {
        var dto = new LoginRequest { Email = "", Password = "password123" };
        var result = await _controller.Login(dto, CancellationToken.None);
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Login_InvalidEmailFormat_ShouldReturnBadRequest()
    {
        var dto = new LoginRequest { Email = "notanemail", Password = "password123" };
        var result = await _controller.Login(dto, CancellationToken.None);
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Login_EmptyPassword_ShouldReturnBadRequest()
    {
        var dto = new LoginRequest { Email = "user@example.com", Password = "" };
        var result = await _controller.Login(dto, CancellationToken.None);
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Login_PasswordTooShort_ShouldReturnBadRequest()
    {
        var dto = new LoginRequest { Email = "user@example.com", Password = "abc" };
        var result = await _controller.Login(dto, CancellationToken.None);
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Login_Success_ShouldReturnOkAndSetRefreshCookie()
    {
        var dto = new LoginRequest { Email = "user@example.com", Password = "password123" };
        var token = new TokenResponse
        {
            Access_Token = "access",
            Id_Token = "id",
            Refresh_Token = "refresh",
            Expires_In = 3600
        };
        _mockAuth0Login.Setup(s => s.LoginAsync(dto.Email, dto.Password)).ReturnsAsync(token);

        var result = await _controller.Login(dto, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        _mockRefreshCookie.Verify(c => c.SetRefreshToken(It.IsAny<HttpResponse>(), "refresh"), Times.Once);
    }

    [Test]
    public async Task Login_ServiceReturnsNull_ShouldReturn502()
    {
        var dto = new LoginRequest { Email = "user@example.com", Password = "password123" };
        _mockAuth0Login.Setup(s => s.LoginAsync(dto.Email, dto.Password))
            .ReturnsAsync((TokenResponse?)null);

        var result = await _controller.Login(dto, CancellationToken.None);

        var statusResult = result as ObjectResult;
        Assert.That(statusResult!.StatusCode, Is.EqualTo(502));
    }

    [Test]
    public async Task Login_NoRefreshToken_ShouldReturnUnauthorized()
    {
        var dto = new LoginRequest { Email = "user@example.com", Password = "password123" };
        var token = new TokenResponse { Access_Token = "access", Refresh_Token = null };
        _mockAuth0Login.Setup(s => s.LoginAsync(dto.Email, dto.Password)).ReturnsAsync(token);

        var result = await _controller.Login(dto, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task Login_AuthLoginFailed_ShouldReturnUnauthorized()
    {
        var dto = new LoginRequest { Email = "user@example.com", Password = "wrongpass" };
        _mockAuth0Login.Setup(s => s.LoginAsync(dto.Email, dto.Password))
            .ThrowsAsync(new AuthLoginFailedException("invalid_grant", "Wrong credentials"));

        var result = await _controller.Login(dto, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task Login_AccountBlocked_ShouldReturn403()
    {
        var dto = new LoginRequest { Email = "user@example.com", Password = "password123" };
        _mockAuth0Login.Setup(s => s.LoginAsync(dto.Email, dto.Password))
            .ThrowsAsync(new AccountBlockedException("Account locked"));

        var result = await _controller.Login(dto, CancellationToken.None);

        var statusResult = result as ObjectResult;
        Assert.That(statusResult!.StatusCode, Is.EqualTo(403));
    }

    [Test]
    public async Task Login_EmailNotVerified_ShouldReturn403()
    {
        var dto = new LoginRequest { Email = "user@example.com", Password = "password123" };
        _mockAuth0Login.Setup(s => s.LoginAsync(dto.Email, dto.Password))
            .ThrowsAsync(new EmailNotVerifiedException("Email not verified"));

        var result = await _controller.Login(dto, CancellationToken.None);

        var statusResult = result as ObjectResult;
        Assert.That(statusResult!.StatusCode, Is.EqualTo(403));
    }

    [Test]
    public async Task Login_RateLimitExceeded_ShouldReturn429()
    {
        var dto = new LoginRequest { Email = "user@example.com", Password = "password123" };
        _mockAuth0Login.Setup(s => s.LoginAsync(dto.Email, dto.Password))
            .ThrowsAsync(new RateLimitExceededException("Too many requests", 60));

        var result = await _controller.Login(dto, CancellationToken.None);

        var statusResult = result as ObjectResult;
        Assert.That(statusResult!.StatusCode, Is.EqualTo(429));
    }

    [Test]
    public async Task Login_HttpRequestException_ShouldReturn502()
    {
        var dto = new LoginRequest { Email = "user@example.com", Password = "password123" };
        _mockAuth0Login.Setup(s => s.LoginAsync(dto.Email, dto.Password))
            .ThrowsAsync(new HttpRequestException("Network error"));

        var result = await _controller.Login(dto, CancellationToken.None);

        var statusResult = result as ObjectResult;
        Assert.That(statusResult!.StatusCode, Is.EqualTo(502));
    }

    [Test]
    public async Task Login_UnexpectedException_ShouldReturn500()
    {
        var dto = new LoginRequest { Email = "user@example.com", Password = "password123" };
        _mockAuth0Login.Setup(s => s.LoginAsync(dto.Email, dto.Password))
            .ThrowsAsync(new Exception("Unexpected"));

        var result = await _controller.Login(dto, CancellationToken.None);

        var statusResult = result as ObjectResult;
        Assert.That(statusResult!.StatusCode, Is.EqualTo(500));
    }

    // ========================================================================
    // REFRESH TESTS
    // ========================================================================

    [Test]
    public async Task Refresh_NoRefreshTokenCookie_ShouldReturnUnauthorized()
    {
        _mockRefreshCookie.Setup(c => c.GetRefreshToken(It.IsAny<HttpRequest>()))
            .Returns((string?)null);

        var result = await _controller.Refresh(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task Refresh_Success_ShouldReturnOkAndRotateCookie()
    {
        var token = new TokenResponse { Access_Token = "new_access", Refresh_Token = "new_refresh", Expires_In = 3600 };
        _mockRefreshCookie.Setup(c => c.GetRefreshToken(It.IsAny<HttpRequest>())).Returns("old_refresh");
        _mockAuth0Login.Setup(s => s.RefreshAsync("old_refresh")).ReturnsAsync(token);

        var result = await _controller.Refresh(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        _mockRefreshCookie.Verify(c => c.SetRefreshToken(It.IsAny<HttpResponse>(), "new_refresh"), Times.Once);
    }

    [Test]
    public async Task Refresh_ServiceReturnsNull_ShouldReturn502()
    {
        _mockRefreshCookie.Setup(c => c.GetRefreshToken(It.IsAny<HttpRequest>())).Returns("old_refresh");
        _mockAuth0Login.Setup(s => s.RefreshAsync("old_refresh"))
            .ReturnsAsync((TokenResponse?)null);

        var result = await _controller.Refresh(CancellationToken.None);

        var statusResult = result as ObjectResult;
        Assert.That(statusResult!.StatusCode, Is.EqualTo(502));
    }

    [Test]
    public async Task Refresh_AuthLoginFailed_ShouldReturnUnauthorizedAndClearCookie()
    {
        _mockRefreshCookie.Setup(c => c.GetRefreshToken(It.IsAny<HttpRequest>())).Returns("old_refresh");
        _mockAuth0Login.Setup(s => s.RefreshAsync("old_refresh"))
            .ThrowsAsync(new AuthLoginFailedException("token_expired", "Refresh token expired"));

        var result = await _controller.Refresh(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
        _mockRefreshCookie.Verify(c => c.ClearRefreshToken(It.IsAny<HttpResponse>()), Times.Once);
    }

    [Test]
    public async Task Refresh_RateLimitExceeded_ShouldReturn429()
    {
        _mockRefreshCookie.Setup(c => c.GetRefreshToken(It.IsAny<HttpRequest>())).Returns("old_refresh");
        _mockAuth0Login.Setup(s => s.RefreshAsync("old_refresh"))
            .ThrowsAsync(new RateLimitExceededException("Too many requests", 30));

        var result = await _controller.Refresh(CancellationToken.None);

        var statusResult = result as ObjectResult;
        Assert.That(statusResult!.StatusCode, Is.EqualTo(429));
    }

    [Test]
    public async Task Refresh_HttpRequestException_ShouldReturn502()
    {
        _mockRefreshCookie.Setup(c => c.GetRefreshToken(It.IsAny<HttpRequest>())).Returns("old_refresh");
        _mockAuth0Login.Setup(s => s.RefreshAsync("old_refresh"))
            .ThrowsAsync(new HttpRequestException("Network error"));

        var result = await _controller.Refresh(CancellationToken.None);

        var statusResult = result as ObjectResult;
        Assert.That(statusResult!.StatusCode, Is.EqualTo(502));
    }

    [Test]
    public async Task Refresh_UnexpectedException_ShouldReturn500()
    {
        _mockRefreshCookie.Setup(c => c.GetRefreshToken(It.IsAny<HttpRequest>())).Returns("old_refresh");
        _mockAuth0Login.Setup(s => s.RefreshAsync("old_refresh"))
            .ThrowsAsync(new Exception("Unexpected"));

        var result = await _controller.Refresh(CancellationToken.None);

        var statusResult = result as ObjectResult;
        Assert.That(statusResult!.StatusCode, Is.EqualTo(500));
    }

    // ========================================================================
    // LOGOUT TESTS
    // ========================================================================

    [Test]
    public void Logout_ShouldClearCookieAndReturnNoContent()
    {
        var result = _controller.Logout();

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        _mockRefreshCookie.Verify(c => c.ClearRefreshToken(It.IsAny<HttpResponse>()), Times.Once);
    }

    [Test]
    public void Logout_WhenExceptionThrown_ShouldStillReturnNoContent()
    {
        _mockRefreshCookie.Setup(c => c.ClearRefreshToken(It.IsAny<HttpResponse>()))
            .Throws(new Exception("Cookie error"));

        var result = _controller.Logout();

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    // ========================================================================
    // GET SOCIAL PROVIDERS TESTS
    // ========================================================================

    [Test]
    public void GetSocialProviders_ShouldReturnOkWithProviders()
    {
        var result = _controller.GetSocialProviders();

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.Value, Is.Not.Null);
    }

    // ========================================================================
    // GET AUTHORIZATION URL TESTS
    // ========================================================================

    [Test]
    public void GetAuthorizationUrl_NullRequest_ShouldReturnBadRequest()
    {
        var result = _controller.GetAuthorizationUrl(null!);
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public void GetAuthorizationUrl_EmptyProvider_ShouldReturnBadRequest()
    {
        var request = new SocialAuthUrlRequest { Provider = "", RedirectUri = "https://example.com/callback" };
        var result = _controller.GetAuthorizationUrl(request);
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public void GetAuthorizationUrl_Success_ShouldReturnOk()
    {
        var request = new SocialAuthUrlRequest { Provider = "google-oauth2", RedirectUri = "https://example.com/callback" };
        var response = new SocialAuthUrlResponse { AuthorizationUrl = "https://auth.example.com/authorize", State = "some-state" };
        _mockSocialLogin.Setup(s => s.GetAuthorizationUrl(request)).Returns(response);

        var result = _controller.GetAuthorizationUrl(request);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.Value, Is.EqualTo(response));
    }

    [Test]
    public void GetAuthorizationUrl_InvalidProvider_ShouldReturnBadRequest()
    {
        var request = new SocialAuthUrlRequest { Provider = "unsupported", RedirectUri = "https://example.com/callback" };
        _mockSocialLogin.Setup(s => s.GetAuthorizationUrl(request))
            .Throws(new InvalidSocialProviderException("unsupported"));

        var result = _controller.GetAuthorizationUrl(request);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public void GetAuthorizationUrl_UnexpectedException_ShouldReturn500()
    {
        var request = new SocialAuthUrlRequest { Provider = "google-oauth2", RedirectUri = "https://example.com/callback" };
        _mockSocialLogin.Setup(s => s.GetAuthorizationUrl(request))
            .Throws(new Exception("Unexpected error"));

        var result = _controller.GetAuthorizationUrl(request);

        var statusResult = result as ObjectResult;
        Assert.That(statusResult!.StatusCode, Is.EqualTo(500));
    }

    // ========================================================================
    // SOCIAL LOGIN CALLBACK TESTS
    // ========================================================================

    [Test]
    public async Task SocialLoginCallback_NullRequest_ShouldReturnBadRequest()
    {
        var result = await _controller.SocialLoginCallback(null!, CancellationToken.None);
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task SocialLoginCallback_EmptyProvider_ShouldReturnBadRequest()
    {
        var request = new SocialLoginRequest { Provider = "", Code = "auth_code" };
        var result = await _controller.SocialLoginCallback(request, CancellationToken.None);
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task SocialLoginCallback_EmptyCode_ShouldReturnBadRequest()
    {
        var request = new SocialLoginRequest { Provider = "google-oauth2", Code = "" };
        var result = await _controller.SocialLoginCallback(request, CancellationToken.None);
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task SocialLoginCallback_Success_ShouldReturnOk()
    {
        var request = new SocialLoginRequest { Provider = "google-oauth2", Code = "auth_code" };
        var response = new SocialLoginResponse
        {
            AccessToken = "access",
            IdToken = "id",
            ExpiresIn = 3600,
            Provider = "google-oauth2",
            UserId = Guid.NewGuid(),
            IsNewUser = false,
            Email = "user@example.com",
            Name = "Test User"
        };
        _mockSocialLogin.Setup(s => s.AuthenticateAsync(request)).ReturnsAsync(response);

        var result = await _controller.SocialLoginCallback(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task SocialLoginCallback_ServiceReturnsNull_ShouldReturn502()
    {
        var request = new SocialLoginRequest { Provider = "google-oauth2", Code = "auth_code" };
        _mockSocialLogin.Setup(s => s.AuthenticateAsync(request))
            .ReturnsAsync((SocialLoginResponse?)null);

        var result = await _controller.SocialLoginCallback(request, CancellationToken.None);

        var statusResult = result as ObjectResult;
        Assert.That(statusResult!.StatusCode, Is.EqualTo(502));
    }

    [Test]
    public async Task SocialLoginCallback_InvalidProvider_ShouldReturnBadRequest()
    {
        var request = new SocialLoginRequest { Provider = "unsupported", Code = "auth_code" };
        _mockSocialLogin.Setup(s => s.AuthenticateAsync(request))
            .ThrowsAsync(new InvalidSocialProviderException("unsupported"));

        var result = await _controller.SocialLoginCallback(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task SocialLoginCallback_EmailAlreadyRegistered_ShouldReturnConflict()
    {
        var request = new SocialLoginRequest { Provider = "google-oauth2", Code = "auth_code" };
        _mockSocialLogin.Setup(s => s.AuthenticateAsync(request))
            .ThrowsAsync(new EmailAlreadyRegisteredException("user@example.com"));

        var result = await _controller.SocialLoginCallback(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ConflictObjectResult>());
    }

    [Test]
    public async Task SocialLoginCallback_SocialLoginException_ShouldReturnUnauthorized()
    {
        var request = new SocialLoginRequest { Provider = "google-oauth2", Code = "auth_code" };
        _mockSocialLogin.Setup(s => s.AuthenticateAsync(request))
            .ThrowsAsync(new SocialLoginException("google-oauth2", "access_denied", "User denied access"));

        var result = await _controller.SocialLoginCallback(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task SocialLoginCallback_AccountBlocked_ShouldReturn403()
    {
        var request = new SocialLoginRequest { Provider = "google-oauth2", Code = "auth_code" };
        _mockSocialLogin.Setup(s => s.AuthenticateAsync(request))
            .ThrowsAsync(new AccountBlockedException("Account suspended"));

        var result = await _controller.SocialLoginCallback(request, CancellationToken.None);

        var statusResult = result as ObjectResult;
        Assert.That(statusResult!.StatusCode, Is.EqualTo(403));
    }

    [Test]
    public async Task SocialLoginCallback_RateLimitExceeded_ShouldReturn429()
    {
        var request = new SocialLoginRequest { Provider = "google-oauth2", Code = "auth_code" };
        _mockSocialLogin.Setup(s => s.AuthenticateAsync(request))
            .ThrowsAsync(new RateLimitExceededException("Too many requests", 60));

        var result = await _controller.SocialLoginCallback(request, CancellationToken.None);

        var statusResult = result as ObjectResult;
        Assert.That(statusResult!.StatusCode, Is.EqualTo(429));
    }

    [Test]
    public async Task SocialLoginCallback_HttpRequestException_ShouldReturn502()
    {
        var request = new SocialLoginRequest { Provider = "google-oauth2", Code = "auth_code" };
        _mockSocialLogin.Setup(s => s.AuthenticateAsync(request))
            .ThrowsAsync(new HttpRequestException("Network error"));

        var result = await _controller.SocialLoginCallback(request, CancellationToken.None);

        var statusResult = result as ObjectResult;
        Assert.That(statusResult!.StatusCode, Is.EqualTo(502));
    }

    [Test]
    public async Task SocialLoginCallback_UnexpectedException_ShouldReturn500()
    {
        var request = new SocialLoginRequest { Provider = "google-oauth2", Code = "auth_code" };
        _mockSocialLogin.Setup(s => s.AuthenticateAsync(request))
            .ThrowsAsync(new Exception("Unexpected"));

        var result = await _controller.SocialLoginCallback(request, CancellationToken.None);

        var statusResult = result as ObjectResult;
        Assert.That(statusResult!.StatusCode, Is.EqualTo(500));
    }

    // ========================================================================
    // LINK SOCIAL ACCOUNT TESTS
    // ========================================================================

    [Test]
    public async Task LinkSocialAccount_NullRequest_ShouldReturnBadRequest()
    {
        var result = await _controller.LinkSocialAccount(null!, CancellationToken.None);
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task LinkSocialAccount_EmptyProvider_ShouldReturnBadRequest()
    {
        var request = new LinkSocialAccountRequest { Provider = "", Code = "auth_code" };
        var result = await _controller.LinkSocialAccount(request, CancellationToken.None);
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task LinkSocialAccount_NoUserClaim_ShouldReturnUnauthorized()
    {
        var request = new LinkSocialAccountRequest { Provider = "google-oauth2", Code = "auth_code" };
        var result = await _controller.LinkSocialAccount(request, CancellationToken.None);
        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task LinkSocialAccount_Success_ShouldReturnOk()
    {
        var userId = Guid.NewGuid();
        SetupUserClaims(userId);

        var request = new LinkSocialAccountRequest { Provider = "google-oauth2", Code = "auth_code" };
        var linked = new LinkedSocialAccountDto
        {
            Id = Guid.NewGuid(),
            Provider = "google-oauth2",
            ProviderUserId = "google-user-123",
            Email = "user@example.com",
            LinkedAt = DateTime.UtcNow
        };
        _mockSocialLogin.Setup(s => s.LinkAccountAsync(userId, request)).ReturnsAsync(linked);

        var result = await _controller.LinkSocialAccount(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.Value, Is.EqualTo(linked));
    }

    [Test]
    public async Task LinkSocialAccount_ServiceReturnsNull_ShouldReturn502()
    {
        var userId = Guid.NewGuid();
        SetupUserClaims(userId);

        var request = new LinkSocialAccountRequest { Provider = "google-oauth2", Code = "auth_code" };
        _mockSocialLogin.Setup(s => s.LinkAccountAsync(userId, request))
            .ReturnsAsync((LinkedSocialAccountDto?)null);

        var result = await _controller.LinkSocialAccount(request, CancellationToken.None);

        var statusResult = result as ObjectResult;
        Assert.That(statusResult!.StatusCode, Is.EqualTo(502));
    }

    [Test]
    public async Task LinkSocialAccount_InvalidProvider_ShouldReturnBadRequest()
    {
        var userId = Guid.NewGuid();
        SetupUserClaims(userId);

        var request = new LinkSocialAccountRequest { Provider = "unsupported", Code = "auth_code" };
        _mockSocialLogin.Setup(s => s.LinkAccountAsync(userId, request))
            .ThrowsAsync(new InvalidSocialProviderException("unsupported"));

        var result = await _controller.LinkSocialAccount(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task LinkSocialAccount_AlreadyLinked_ShouldReturnConflict()
    {
        var userId = Guid.NewGuid();
        SetupUserClaims(userId);

        var request = new LinkSocialAccountRequest { Provider = "google-oauth2", Code = "auth_code" };
        _mockSocialLogin.Setup(s => s.LinkAccountAsync(userId, request))
            .ThrowsAsync(new SocialAccountAlreadyLinkedException("google-oauth2"));

        var result = await _controller.LinkSocialAccount(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ConflictObjectResult>());
    }

    [Test]
    public async Task LinkSocialAccount_EmailConflict_ShouldReturnConflict()
    {
        var userId = Guid.NewGuid();
        SetupUserClaims(userId);

        var request = new LinkSocialAccountRequest { Provider = "google-oauth2", Code = "auth_code" };
        _mockSocialLogin.Setup(s => s.LinkAccountAsync(userId, request))
            .ThrowsAsync(new EmailAlreadyRegisteredException("other@example.com"));

        var result = await _controller.LinkSocialAccount(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ConflictObjectResult>());
    }

    [Test]
    public async Task LinkSocialAccount_SocialLoginException_ShouldReturnBadRequest()
    {
        var userId = Guid.NewGuid();
        SetupUserClaims(userId);

        var request = new LinkSocialAccountRequest { Provider = "google-oauth2", Code = "auth_code" };
        _mockSocialLogin.Setup(s => s.LinkAccountAsync(userId, request))
            .ThrowsAsync(new SocialLoginException("google-oauth2", "token_exchange_failed", "Could not exchange token"));

        var result = await _controller.LinkSocialAccount(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task LinkSocialAccount_RateLimitExceeded_ShouldReturn429()
    {
        var userId = Guid.NewGuid();
        SetupUserClaims(userId);

        var request = new LinkSocialAccountRequest { Provider = "google-oauth2", Code = "auth_code" };
        _mockSocialLogin.Setup(s => s.LinkAccountAsync(userId, request))
            .ThrowsAsync(new RateLimitExceededException("Too many requests", 30));

        var result = await _controller.LinkSocialAccount(request, CancellationToken.None);

        var statusResult = result as ObjectResult;
        Assert.That(statusResult!.StatusCode, Is.EqualTo(429));
    }

    [Test]
    public async Task LinkSocialAccount_UnexpectedException_ShouldReturn500()
    {
        var userId = Guid.NewGuid();
        SetupUserClaims(userId);

        var request = new LinkSocialAccountRequest { Provider = "google-oauth2", Code = "auth_code" };
        _mockSocialLogin.Setup(s => s.LinkAccountAsync(userId, request))
            .ThrowsAsync(new Exception("Unexpected"));

        var result = await _controller.LinkSocialAccount(request, CancellationToken.None);

        var statusResult = result as ObjectResult;
        Assert.That(statusResult!.StatusCode, Is.EqualTo(500));
    }

    // ========================================================================
    // UNLINK SOCIAL ACCOUNT TESTS
    // ========================================================================

    [Test]
    public async Task UnlinkSocialAccount_EmptyProvider_ShouldReturnBadRequest()
    {
        var result = await _controller.UnlinkSocialAccount("", CancellationToken.None);
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task UnlinkSocialAccount_NoUserClaim_ShouldReturnUnauthorized()
    {
        var result = await _controller.UnlinkSocialAccount("google-oauth2", CancellationToken.None);
        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task UnlinkSocialAccount_Success_ShouldReturnNoContent()
    {
        var userId = Guid.NewGuid();
        SetupUserClaims(userId);
        _mockSocialLogin.Setup(s => s.UnlinkAccountAsync(userId, "google-oauth2"))
            .Returns(Task.CompletedTask);

        var result = await _controller.UnlinkSocialAccount("google-oauth2", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        _mockSocialLogin.Verify(s => s.UnlinkAccountAsync(userId, "google-oauth2"), Times.Once);
    }

    [Test]
    public async Task UnlinkSocialAccount_InvalidProvider_ShouldReturnBadRequest()
    {
        var userId = Guid.NewGuid();
        SetupUserClaims(userId);
        _mockSocialLogin.Setup(s => s.UnlinkAccountAsync(userId, "unsupported"))
            .ThrowsAsync(new InvalidSocialProviderException("unsupported"));

        var result = await _controller.UnlinkSocialAccount("unsupported", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task UnlinkSocialAccount_NotLinked_ShouldReturnNotFound()
    {
        var userId = Guid.NewGuid();
        SetupUserClaims(userId);
        _mockSocialLogin.Setup(s => s.UnlinkAccountAsync(userId, "google-oauth2"))
            .ThrowsAsync(new SocialAccountNotLinkedException("google-oauth2"));

        var result = await _controller.UnlinkSocialAccount("google-oauth2", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task UnlinkSocialAccount_CannotUnlinkLastMethod_ShouldReturnBadRequest()
    {
        var userId = Guid.NewGuid();
        SetupUserClaims(userId);
        _mockSocialLogin.Setup(s => s.UnlinkAccountAsync(userId, "google-oauth2"))
            .ThrowsAsync(new CannotUnlinkLastAuthMethodException("Cannot remove last auth method"));

        var result = await _controller.UnlinkSocialAccount("google-oauth2", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task UnlinkSocialAccount_RateLimitExceeded_ShouldReturn429()
    {
        var userId = Guid.NewGuid();
        SetupUserClaims(userId);
        _mockSocialLogin.Setup(s => s.UnlinkAccountAsync(userId, "google-oauth2"))
            .ThrowsAsync(new RateLimitExceededException("Too many requests", 60));

        var result = await _controller.UnlinkSocialAccount("google-oauth2", CancellationToken.None);

        var statusResult = result as ObjectResult;
        Assert.That(statusResult!.StatusCode, Is.EqualTo(429));
    }

    [Test]
    public async Task UnlinkSocialAccount_UnexpectedException_ShouldReturn500()
    {
        var userId = Guid.NewGuid();
        SetupUserClaims(userId);
        _mockSocialLogin.Setup(s => s.UnlinkAccountAsync(userId, "google-oauth2"))
            .ThrowsAsync(new Exception("Unexpected"));

        var result = await _controller.UnlinkSocialAccount("google-oauth2", CancellationToken.None);

        var statusResult = result as ObjectResult;
        Assert.That(statusResult!.StatusCode, Is.EqualTo(500));
    }

    // ========================================================================
    // GET LINKED ACCOUNTS TESTS
    // ========================================================================

    [Test]
    public async Task GetLinkedAccounts_NoUserClaim_ShouldReturnUnauthorized()
    {
        var result = await _controller.GetLinkedAccounts(CancellationToken.None);
        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task GetLinkedAccounts_Success_ShouldReturnOkWithAccounts()
    {
        var userId = Guid.NewGuid();
        SetupUserClaims(userId);

        var accounts = new List<LinkedSocialAccountDto>
        {
            new LinkedSocialAccountDto
            {
                Id = Guid.NewGuid(),
                Provider = "google-oauth2",
                ProviderUserId = "google-user-123",
                Email = "user@example.com",
                LinkedAt = DateTime.UtcNow
            }
        };
        _mockSocialLogin.Setup(s => s.GetLinkedAccountsAsync(userId)).ReturnsAsync(accounts);

        var result = await _controller.GetLinkedAccounts(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetLinkedAccounts_ServiceReturnsNull_ShouldReturnOkWithEmptyList()
    {
        var userId = Guid.NewGuid();
        SetupUserClaims(userId);
        _mockSocialLogin.Setup(s => s.GetLinkedAccountsAsync(userId))
            .ReturnsAsync((IEnumerable<LinkedSocialAccountDto>?)null);

        var result = await _controller.GetLinkedAccounts(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetLinkedAccounts_RateLimitExceeded_ShouldReturn429()
    {
        var userId = Guid.NewGuid();
        SetupUserClaims(userId);
        _mockSocialLogin.Setup(s => s.GetLinkedAccountsAsync(userId))
            .ThrowsAsync(new RateLimitExceededException("Too many requests", 30));

        var result = await _controller.GetLinkedAccounts(CancellationToken.None);

        var statusResult = result as ObjectResult;
        Assert.That(statusResult!.StatusCode, Is.EqualTo(429));
    }

    [Test]
    public async Task GetLinkedAccounts_HttpRequestException_ShouldReturn502()
    {
        var userId = Guid.NewGuid();
        SetupUserClaims(userId);
        _mockSocialLogin.Setup(s => s.GetLinkedAccountsAsync(userId))
            .ThrowsAsync(new HttpRequestException("Network error"));

        var result = await _controller.GetLinkedAccounts(CancellationToken.None);

        var statusResult = result as ObjectResult;
        Assert.That(statusResult!.StatusCode, Is.EqualTo(502));
    }

    [Test]
    public async Task GetLinkedAccounts_UnexpectedException_ShouldReturn500()
    {
        var userId = Guid.NewGuid();
        SetupUserClaims(userId);
        _mockSocialLogin.Setup(s => s.GetLinkedAccountsAsync(userId))
            .ThrowsAsync(new Exception("Unexpected"));

        var result = await _controller.GetLinkedAccounts(CancellationToken.None);

        var statusResult = result as ObjectResult;
        Assert.That(statusResult!.StatusCode, Is.EqualTo(500));
    }

    // ========================================================================
    // REQUEST EMAIL UPDATE TESTS
    // ========================================================================

    [Test]
    public async Task RequestEmailUpdate_Success_ShouldDeleteExistingAndInsertNew()
    {
        var businessId = Guid.NewGuid();
        var dto = new RequestEmailUpdateDto
        {
            BusinessId = businessId,
            EmailAddress = "newemail@example.com",
            Reason = "Changing business email"
        };

        _mockEmailUpdateRequestRepository
            .Setup(r => r.DeleteByBusinessIdAsync(businessId))
            .Returns(Task.CompletedTask);
        _mockEmailUpdateRequestRepository
            .Setup(r => r.AddAsync(It.IsAny<EmailUpdateRequest>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.RequestEmailUpdate(dto);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        _mockEmailUpdateRequestRepository.Verify(r => r.DeleteByBusinessIdAsync(businessId), Times.Once);
        _mockEmailUpdateRequestRepository.Verify(r => r.AddAsync(It.IsAny<EmailUpdateRequest>()), Times.Once);
    }

    [Test]
    public async Task RequestEmailUpdate_Success_ShouldDeleteBeforeInsert()
    {
        var callOrder = new List<string>();
        var businessId = Guid.NewGuid();
        var dto = new RequestEmailUpdateDto
        {
            BusinessId = businessId,
            EmailAddress = "newemail@example.com"
        };

        _mockEmailUpdateRequestRepository
            .Setup(r => r.DeleteByBusinessIdAsync(businessId))
            .Callback(() => callOrder.Add("delete"))
            .Returns(Task.CompletedTask);
        _mockEmailUpdateRequestRepository
            .Setup(r => r.AddAsync(It.IsAny<EmailUpdateRequest>()))
            .Callback(() => callOrder.Add("add"))
            .Returns(Task.CompletedTask);

        await _controller.RequestEmailUpdate(dto);

        Assert.That(callOrder, Has.Count.EqualTo(2));
        Assert.That(callOrder[0], Is.EqualTo("delete"));
        Assert.That(callOrder[1], Is.EqualTo("add"));
    }

    [Test]
    public async Task RequestEmailUpdate_WithoutReason_ShouldSucceed()
    {
        var businessId = Guid.NewGuid();
        var dto = new RequestEmailUpdateDto
        {
            BusinessId = businessId,
            EmailAddress = "newemail@example.com",
            Reason = null
        };

        _mockEmailUpdateRequestRepository
            .Setup(r => r.DeleteByBusinessIdAsync(businessId))
            .Returns(Task.CompletedTask);
        _mockEmailUpdateRequestRepository
            .Setup(r => r.AddAsync(It.IsAny<EmailUpdateRequest>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.RequestEmailUpdate(dto);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        _mockEmailUpdateRequestRepository.Verify(r => r.AddAsync(It.IsAny<EmailUpdateRequest>()), Times.Once);
    }

    [Test]
    public async Task RequestEmailUpdate_RepositoryThrows_ShouldReturn500()
    {
        var businessId = Guid.NewGuid();
        var dto = new RequestEmailUpdateDto
        {
            BusinessId = businessId,
            EmailAddress = "newemail@example.com"
        };

        _mockEmailUpdateRequestRepository
            .Setup(r => r.DeleteByBusinessIdAsync(businessId))
            .ThrowsAsync(new Exception("Database error"));

        var result = await _controller.RequestEmailUpdate(dto);

        var statusResult = result as ObjectResult;
        Assert.That(statusResult, Is.Not.Null);
        Assert.That(statusResult!.StatusCode, Is.EqualTo(500));
    }
}
