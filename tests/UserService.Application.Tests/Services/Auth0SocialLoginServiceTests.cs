using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using UserService.Application.DTOs.Auth;
using UserService.Application.Services.Auth0;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Domain.Repositories;

namespace UserService.Application.Tests.Services;

[TestFixture]
public class Auth0SocialLoginServiceTests
{
    private Mock<HttpMessageHandler> _mockHttpHandler = null!;
    private HttpClient _httpClient = null!;
    private Mock<IConfiguration> _mockConfig = null!;
    private Mock<ISocialIdentityRepository> _mockSocialIdentityRepo = null!;
    private Mock<IUserRepository> _mockUserRepo = null!;
    private Mock<IEndUserProfileRepository> _mockEndUserProfileRepo = null!;
    private Mock<IUserSettingsRepository> _mockUserSettingsRepo = null!;
    private Mock<IAuth0ManagementService> _mockAuth0Management = null!;
    private Auth0SocialLoginService _service = null!;

    [SetUp]
    public void Setup()
    {
        _mockHttpHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpHandler.Object);
        _mockConfig = new Mock<IConfiguration>();
        _mockSocialIdentityRepo = new Mock<ISocialIdentityRepository>();
        _mockUserRepo = new Mock<IUserRepository>();
        _mockEndUserProfileRepo = new Mock<IEndUserProfileRepository>();
        _mockUserSettingsRepo = new Mock<IUserSettingsRepository>();
        _mockAuth0Management = new Mock<IAuth0ManagementService>();

        // Auth0 configuration
        _mockConfig.Setup(c => c["Auth0:Domain"]).Returns("test.auth0.com");
        _mockConfig.Setup(c => c["Auth0:ClientId"]).Returns("test-client-id");
        _mockConfig.Setup(c => c["Auth0:ClientSecret"]).Returns("test-client-secret");
        _mockConfig.Setup(c => c["Auth0:Audience"]).Returns("test-audience");
        _mockConfig.Setup(c => c["Auth0:Roles:EndUser"]).Returns("auth0_end_role");

        _service = new Auth0SocialLoginService(
            _httpClient,
            _mockConfig.Object,
            _mockSocialIdentityRepo.Object,
            _mockUserRepo.Object,
            _mockEndUserProfileRepo.Object,
            _mockUserSettingsRepo.Object,
            _mockAuth0Management.Object
        );
    }

    // -----------------------------------------------
    // AUTHORIZATION URL TESTS
    // -----------------------------------------------

    [Test]
    public void GetAuthorizationUrl_ShouldReturnValidUrl_ForGoogleProvider()
    {
        // Arrange
        var request = new SocialAuthUrlRequest
        {
            Provider = "google",
            RedirectUri = "https://example.com/callback",
            State = "test-state"
        };

        // Act
        var result = _service.GetAuthorizationUrl(request);

        // Assert
        Assert.That(result.AuthorizationUrl, Does.Contain("https://test.auth0.com/authorize"));
        Assert.That(result.AuthorizationUrl, Does.Contain("connection=google-oauth2"));
        Assert.That(result.AuthorizationUrl, Does.Contain("client_id=test-client-id"));
        Assert.That(result.State, Is.EqualTo("test-state"));
    }

    [Test]
    public void GetAuthorizationUrl_ShouldThrow_ForInvalidProvider()
    {
        // Arrange
        var request = new SocialAuthUrlRequest
        {
            Provider = "invalid-provider",
            RedirectUri = "https://example.com/callback"
        };

        // Act & Assert
        Assert.Throws<InvalidSocialProviderException>(() => _service.GetAuthorizationUrl(request));
    }

    // -----------------------------------------------
    // AUTHENTICATION TESTS - EXISTING USER WITH PASSWORD
    // -----------------------------------------------

    [Test]
    public void AuthenticateAsync_ShouldThrowEmailAlreadyRegisteredWithPasswordException_WhenUserHasPassword()
    {
        // Arrange
        var request = new SocialLoginRequest
        {
            Provider = "google",
            Code = "test-code",
            RedirectUri = "https://example.com/callback"
        };

        var userId = Guid.NewGuid();
        var existingUser = new User(
            username: "testuser",
            email: "test@example.com",
            phone: "1234567890",
            password: "hashedpassword123", // User has a password
            userType: "end_user",
            address: "123 Main St",
            auth0UserId: "auth0|existing"
        );

        // Mock token exchange
        var tokenResponseContent = JsonSerializer.Serialize(new
        {
            access_token = "test-access-token",
            id_token = "test-id-token",
            refresh_token = "test-refresh-token",
            expires_in = 3600
        });

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.AbsolutePath.Contains("/oauth/token")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(tokenResponseContent)
            });

        // Mock userinfo
        var userInfoContent = JsonSerializer.Serialize(new
        {
            sub = "google-oauth2|12345",
            email = "test@example.com",
            name = "Test User"
        });

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.AbsolutePath.Contains("/userinfo")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(userInfoContent)
            });

        // No existing social identity
        _mockSocialIdentityRepo
            .Setup(r => r.GetByProviderUserIdAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((SocialIdentity?)null);

        // Existing user by email with password
        _mockUserRepo
            .Setup(r => r.GetUserOrBusinessIdByEmailAsync("test@example.com"))
            .ReturnsAsync(userId);

        _mockUserRepo
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(existingUser);

        // Act & Assert
        var ex = Assert.ThrowsAsync<EmailAlreadyRegisteredWithPasswordException>(
            () => _service.AuthenticateAsync(request)
        );
        Assert.That(ex!.Email, Is.EqualTo("test@example.com"));
    }

    // -----------------------------------------------
    // AUTHENTICATION TESTS - EXISTING USER WITHOUT PASSWORD (SOCIAL ONLY)
    // -----------------------------------------------

    [Test]
    public async Task AuthenticateAsync_ShouldLinkAccount_WhenUserExistsWithoutPassword()
    {
        // Arrange
        var request = new SocialLoginRequest
        {
            Provider = "google",
            Code = "test-code",
            RedirectUri = "https://example.com/callback"
        };

        var userId = Guid.NewGuid();
        var existingUser = new User(
            username: "testuser",
            email: "test@example.com",
            phone: "1234567890",
            password: "", // User has no password (registered via social)
            userType: "end_user",
            address: "123 Main St",
            auth0UserId: "auth0|existing"
        );

        // Mock token exchange
        var tokenResponseContent = JsonSerializer.Serialize(new
        {
            access_token = "test-access-token",
            id_token = "test-id-token",
            refresh_token = "test-refresh-token",
            expires_in = 3600
        });

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.AbsolutePath.Contains("/oauth/token")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(tokenResponseContent)
            });

        // Mock userinfo
        var userInfoContent = JsonSerializer.Serialize(new
        {
            sub = "google-oauth2|12345",
            email = "test@example.com",
            name = "Test User"
        });

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.AbsolutePath.Contains("/userinfo")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(userInfoContent)
            });

        // No existing social identity
        _mockSocialIdentityRepo
            .Setup(r => r.GetByProviderUserIdAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((SocialIdentity?)null);

        // Existing user by email without password
        _mockUserRepo
            .Setup(r => r.GetUserOrBusinessIdByEmailAsync("test@example.com"))
            .ReturnsAsync(userId);

        _mockUserRepo
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(existingUser);

        _mockSocialIdentityRepo
            .Setup(r => r.AddAsync(It.IsAny<SocialIdentity>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.AuthenticateAsync(request);

        // Assert
        Assert.That(result.UserId, Is.EqualTo(userId));
        Assert.That(result.Email, Is.EqualTo("test@example.com"));
        Assert.That(result.IsNewUser, Is.False);
        _mockSocialIdentityRepo.Verify(r => r.AddAsync(It.IsAny<SocialIdentity>()), Times.Once);
    }

    // -----------------------------------------------
    // AUTHENTICATION TESTS - NEW USER
    // -----------------------------------------------

    [Test]
    public async Task AuthenticateAsync_ShouldCreateNewUser_WhenEmailDoesNotExist()
    {
        // Arrange
        var request = new SocialLoginRequest
        {
            Provider = "google",
            Code = "test-code",
            RedirectUri = "https://example.com/callback"
        };

        var newUserId = Guid.NewGuid();
        var newUser = new User(
            username: "Test User",
            email: "newuser@example.com",
            phone: "",
            password: "",
            userType: "end_user",
            address: null,
            auth0UserId: "auth0|newuser"
        );

        // Mock token exchange
        var tokenResponseContent = JsonSerializer.Serialize(new
        {
            access_token = "test-access-token",
            id_token = "test-id-token",
            refresh_token = "test-refresh-token",
            expires_in = 3600
        });

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.AbsolutePath.Contains("/oauth/token")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(tokenResponseContent)
            });

        // Mock userinfo
        var userInfoContent = JsonSerializer.Serialize(new
        {
            sub = "google-oauth2|12345",
            email = "newuser@example.com",
            name = "Test User"
        });

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.AbsolutePath.Contains("/userinfo")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(userInfoContent)
            });

        // No existing social identity
        _mockSocialIdentityRepo
            .Setup(r => r.GetByProviderUserIdAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((SocialIdentity?)null);

        // No existing user by email
        _mockUserRepo
            .Setup(r => r.GetUserOrBusinessIdByEmailAsync("newuser@example.com"))
            .ReturnsAsync((Guid?)null);

        // Mock user creation
        _mockAuth0Management
            .Setup(m => m.CreateUserAndAssignRoleAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync("auth0|newuser");

        _mockUserRepo
            .Setup(r => r.AddAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        _mockEndUserProfileRepo
            .Setup(r => r.AddAsync(It.IsAny<EndUserProfile>()))
            .Returns(Task.CompletedTask);

        _mockUserSettingsRepo
            .Setup(r => r.AddAsync(It.IsAny<UserSettings>()))
            .Returns(Task.CompletedTask);

        _mockSocialIdentityRepo
            .Setup(r => r.AddAsync(It.IsAny<SocialIdentity>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.AuthenticateAsync(request);

        // Assert
        Assert.That(result.Email, Is.EqualTo("newuser@example.com"));
        Assert.That(result.IsNewUser, Is.True);
        _mockUserRepo.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
        _mockEndUserProfileRepo.Verify(r => r.AddAsync(It.IsAny<EndUserProfile>()), Times.Once);
        _mockUserSettingsRepo.Verify(r => r.AddAsync(It.IsAny<UserSettings>()), Times.Once);
        _mockSocialIdentityRepo.Verify(r => r.AddAsync(It.IsAny<SocialIdentity>()), Times.Once);
    }

    // -----------------------------------------------
    // AUTHENTICATION TESTS - EXISTING SOCIAL IDENTITY
    // -----------------------------------------------

    [Test]
    public async Task AuthenticateAsync_ShouldUpdateTokens_WhenSocialIdentityExists()
    {
        // Arrange
        var request = new SocialLoginRequest
        {
            Provider = "google",
            Code = "test-code",
            RedirectUri = "https://example.com/callback"
        };

        var userId = Guid.NewGuid();
        var existingUser = new User(
            username: "testuser",
            email: "test@example.com",
            phone: "1234567890",
            password: "",
            userType: "end_user",
            address: "123 Main St",
            auth0UserId: "auth0|existing"
        );

        var existingSocialIdentity = new SocialIdentity(
            userId: userId,
            provider: "google-oauth2",
            providerUserId: "google-oauth2|12345",
            email: "test@example.com",
            name: "Test User",
            accessToken: "old-token",
            refreshToken: "old-refresh",
            expiresAt: DateTime.UtcNow.AddHours(-1)
        );

        // Mock token exchange
        var tokenResponseContent = JsonSerializer.Serialize(new
        {
            access_token = "new-access-token",
            id_token = "new-id-token",
            refresh_token = "new-refresh-token",
            expires_in = 3600
        });

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.AbsolutePath.Contains("/oauth/token")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(tokenResponseContent)
            });

        // Mock userinfo
        var userInfoContent = JsonSerializer.Serialize(new
        {
            sub = "google-oauth2|12345",
            email = "test@example.com",
            name = "Test User"
        });

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.AbsolutePath.Contains("/userinfo")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(userInfoContent)
            });

        // Existing social identity
        _mockSocialIdentityRepo
            .Setup(r => r.GetByProviderUserIdAsync("google-oauth2", "google-oauth2|12345"))
            .ReturnsAsync(existingSocialIdentity);

        _mockUserRepo
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(existingUser);

        _mockSocialIdentityRepo
            .Setup(r => r.UpdateAsync(It.IsAny<SocialIdentity>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.AuthenticateAsync(request);

        // Assert
        Assert.That(result.UserId, Is.EqualTo(userId));
        Assert.That(result.Email, Is.EqualTo("test@example.com"));
        Assert.That(result.IsNewUser, Is.False);
        _mockSocialIdentityRepo.Verify(r => r.UpdateAsync(It.IsAny<SocialIdentity>()), Times.Once);
    }
}
