using Moq;
using Microsoft.AspNetCore.Mvc;
using UserService.Api.Controllers;
using UserService.Application.DTOs;
using UserService.Application.DTOs.Points; 
using UserService.Application.Interfaces;
using UserService.Application.Services.Auth0;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;
using UserService.Domain.Exceptions; 
using Microsoft.Extensions.Logging;  

namespace UserService.Api.Tests.Controllers;

[TestFixture]
public class AuthControllerTests
{
    private Mock<IAuth0UserLoginService> _mockAuth0Login = null!;
    private Mock<IAuth0SocialLoginService> _mockSocialLogin = null!;
    private Mock<IRefreshTokenCookieService> _mockRefreshCookie = null!;
    private Mock<IUserRepository> _mockUserRepository = null!;
    private Mock<IPointsService> _mockPointsService = null!;
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
        _mockLogger = new Mock<ILogger<AuthController>>(); 

        _controller = new AuthController(
            _mockAuth0Login.Object,
            _mockSocialLogin.Object,
            _mockRefreshCookie.Object,
            _mockUserRepository.Object,
            _mockPointsService.Object,
            _mockLogger.Object
        );
    }
    // ========================================================================
    // LOGIN WITH STREAK TRACKING TESTS
    // ========================================================================

    [Test]
    public async Task Login_Successful_ShouldUpdateLastLogin()
    {
        // ARRANGE
        var email = "test@example.com";
        var password = "password123";
        var userId = Guid.NewGuid();

        var loginRequest = new LoginRequest { Email = email, Password = password };
        var tokenResponse = new TokenResponse
        {
            Access_Token = "access_token",
            Id_Token = "id_token",
            Refresh_Token = "refresh_token",
            Expires_In = 3600,
            Roles = new List<string> { "end_user" },
            Id = userId
        };

        var user = new User("testuser", email, "1234567890", "pass", "end_user", "addr", "auth0|test");

        _mockAuth0Login.Setup(s => s.LoginAsync(email, password))
            .ReturnsAsync(tokenResponse);
        _mockUserRepository.Setup(r => r.GetByEmailAsync(email))
            .ReturnsAsync(user);
        _mockUserRepository.Setup(r => r.UpdateLastLoginAsync(user.Id, It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);
        _mockPointsService.Setup(s => s.UpdateLoginStreakAsync(user.Id, It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);
        _mockPointsService.Setup(s => s.CheckAndAwardStreakMilestoneAsync(user.Id))
            .ReturnsAsync((PointTransactionDto?)null);

        // ACT
        var result = await _controller.Login(loginRequest);

        // ASSERT
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        _mockUserRepository.Verify(r => r.UpdateLastLoginAsync(user.Id, It.IsAny<DateTime>()), Times.Once);
    }

    [Test]
    public async Task Login_Successful_ShouldUpdateLoginStreak()
    {
        // ARRANGE
        var email = "test@example.com";
        var password = "password123";
        var userId = Guid.NewGuid();

        var loginRequest = new LoginRequest { Email = email, Password = password };
        var tokenResponse = new TokenResponse
        {
            Access_Token = "access_token",
            Id_Token = "id_token",
            Refresh_Token = "refresh_token",
            Expires_In = 3600,
          Roles = new List<string> { "end_user" },
        };

        var user = new User("testuser", email, "1234567890", "pass", "end_user", "addr", "auth0|test");

        _mockAuth0Login.Setup(s => s.LoginAsync(email, password))
            .ReturnsAsync(tokenResponse);
        _mockUserRepository.Setup(r => r.GetByEmailAsync(email))
            .ReturnsAsync(user);
        _mockUserRepository.Setup(r => r.UpdateLastLoginAsync(user.Id, It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);
        _mockPointsService.Setup(s => s.UpdateLoginStreakAsync(user.Id, It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);
        _mockPointsService.Setup(s => s.CheckAndAwardStreakMilestoneAsync(user.Id))
            .ReturnsAsync((PointTransactionDto?)null);

        // ACT
        var result = await _controller.Login(loginRequest);

        // ASSERT
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        _mockPointsService.Verify(s => s.UpdateLoginStreakAsync(user.Id, It.IsAny<DateTime>()), Times.Once);
    }

    [Test]
    public async Task Login_Successful_ShouldCheckStreakMilestone()
    {
        // ARRANGE
        var email = "test@example.com";
        var password = "password123";
        var userId = Guid.NewGuid();

        var loginRequest = new LoginRequest { Email = email, Password = password };
        var tokenResponse = new TokenResponse
        {
            Access_Token = "access_token",
            Id_Token = "id_token",
            Refresh_Token = "refresh_token",
            Expires_In = 3600,
          Id = userId
        };

        var user = new User("testuser", email, "1234567890", "pass", "end_user", "addr", "auth0|test");

        _mockAuth0Login.Setup(s => s.LoginAsync(email, password))
            .ReturnsAsync(tokenResponse);
        _mockUserRepository.Setup(r => r.GetByEmailAsync(email))
            .ReturnsAsync(user);
        _mockUserRepository.Setup(r => r.UpdateLastLoginAsync(user.Id, It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);
        _mockPointsService.Setup(s => s.UpdateLoginStreakAsync(user.Id, It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);
        _mockPointsService.Setup(s => s.CheckAndAwardStreakMilestoneAsync(user.Id))
            .ReturnsAsync((PointTransactionDto?)null);

        // ACT
        var result = await _controller.Login(loginRequest);

        // ASSERT
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        _mockPointsService.Verify(s => s.CheckAndAwardStreakMilestoneAsync(user.Id), Times.Once);
    }

    [Test]
    public async Task Login_At100DayStreak_ShouldAwardMilestonePoints()
    {
        // ARRANGE
        var email = "test@example.com";
        var password = "password123";
        var userId = Guid.NewGuid();

        var loginRequest = new LoginRequest { Email = email, Password = password };
        var tokenResponse = new TokenResponse
        {
            Access_Token = "access_token",
            Id_Token = "id_token",
            Refresh_Token = "refresh_token",
            Expires_In = 3600,
            Roles = new List<string> { "end_user" },
            Id = userId
        };

        var user = new User("testuser", email, "1234567890", "pass", "end_user", "addr", "auth0|test");

        var milestoneTransaction = new PointTransactionDto(
            Id: Guid.NewGuid(),
            UserId: userId,
            Points: 100m,
            TransactionType: "MILESTONE",
            Description: "100-day login streak milestone bonus",
            ReferenceId: null,
            ReferenceType: null,
            CreatedAt: DateTime.UtcNow
        );

        _mockAuth0Login.Setup(s => s.LoginAsync(email, password))
            .ReturnsAsync(tokenResponse);
        _mockUserRepository.Setup(r => r.GetByEmailAsync(email))
            .ReturnsAsync(user);
        _mockUserRepository.Setup(r => r.UpdateLastLoginAsync(user.Id, It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);
        _mockPointsService.Setup(s => s.UpdateLoginStreakAsync(user.Id, It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);
        _mockPointsService.Setup(s => s.CheckAndAwardStreakMilestoneAsync(user.Id))
            .ReturnsAsync(milestoneTransaction);

        // ACT
        var result = await _controller.Login(loginRequest);

        // ASSERT
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        _mockPointsService.Verify(s => s.CheckAndAwardStreakMilestoneAsync(user.Id), Times.Once);
    }

    [Test]
    public async Task Login_UserNotFound_ShouldNotUpdateStreak()
    {
        // ARRANGE
        var email = "nonexistent@example.com";
        var password = "password123";

        var loginRequest = new LoginRequest { Email = email, Password = password };
        var tokenResponse = new TokenResponse
        {
            Access_Token = "access_token",
            Id_Token = "id_token",
            Refresh_Token = "refresh_token",
            Expires_In = 3600,
            Roles = new List<string> { "end_user" },
            Id = Guid.NewGuid()
        };

        _mockAuth0Login.Setup(s => s.LoginAsync(email, password))
            .ReturnsAsync(tokenResponse);
        _mockUserRepository.Setup(r => r.GetByEmailAsync(email))
            .ReturnsAsync((User?)null);

        // ACT
        var result = await _controller.Login(loginRequest);

        // ASSERT
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        _mockPointsService.Verify(s => s.UpdateLoginStreakAsync(It.IsAny<Guid>(), It.IsAny<DateTime>()), Times.Never);
        _mockPointsService.Verify(s => s.CheckAndAwardStreakMilestoneAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task Login_StreakUpdateFails_ShouldStillReturnToken()
    {
        // ARRANGE
        var email = "test@example.com";
        var password = "password123";
        var userId = Guid.NewGuid();

        var loginRequest = new LoginRequest { Email = email, Password = password };
        var tokenResponse = new TokenResponse
        {
            Access_Token = "access_token",
            Id_Token = "id_token",
            Refresh_Token = "refresh_token",
            Expires_In = 3600,
          
        };

        var user = new User("testuser", email, "1234567890", "pass", "end_user", "addr", "auth0|test");

        _mockAuth0Login.Setup(s => s.LoginAsync(email, password))
            .ReturnsAsync(tokenResponse);
        _mockUserRepository.Setup(r => r.GetByEmailAsync(email))
            .ReturnsAsync(user);
        _mockUserRepository.Setup(r => r.UpdateLastLoginAsync(user.Id, It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);
        _mockPointsService.Setup(s => s.UpdateLoginStreakAsync(user.Id, It.IsAny<DateTime>()))
            .ThrowsAsync(new Exception("Database error"));

        // ACT
        var result = await _controller.Login(loginRequest);

        // ASSERT - Login should still succeed even if streak update fails
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        var value = okResult.Value;
        Assert.That(value, Is.Not.Null);
    }

    [Test]
    public async Task Login_InvalidCredentials_ShouldNotUpdateStreak()
    {
        // ARRANGE
        var email = "test@example.com";
        var password = "wrongpassword";

        var loginRequest = new LoginRequest { Email = email, Password = password };

        _mockAuth0Login.Setup(s => s.LoginAsync(email, password))
            .ThrowsAsync(new AuthLoginFailedException("Invalid credentials", "Invalid  credentials"));

        // ACT
        var result = await _controller.Login(loginRequest);

        // ASSERT
        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
        _mockPointsService.Verify(s => s.UpdateLoginStreakAsync(It.IsAny<Guid>(), It.IsAny<DateTime>()), Times.Never);
    }
}