using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using UserService.Api.Controllers;
using UserService.Application.DTOs.Auth;
using UserService.Application.Interfaces;
using UserService.Application.Services.Auth0;
using UserService.Domain.Entities;
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
    }

    // ========================================================================
    // REQUEST EMAIL UPDATE TESTS
    // ========================================================================

    [Test]
    public async Task RequestEmailUpdate_Success_ShouldDeleteExistingAndInsertNew()
    {
        // ARRANGE
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

        // ACT
        var result = await _controller.RequestEmailUpdate(dto);

        // ASSERT
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        _mockEmailUpdateRequestRepository.Verify(r => r.DeleteByBusinessIdAsync(businessId), Times.Once);
        _mockEmailUpdateRequestRepository.Verify(r => r.AddAsync(It.IsAny<EmailUpdateRequest>()), Times.Once);
    }

    [Test]
    public async Task RequestEmailUpdate_Success_ShouldDeleteBeforeInsert()
    {
        // ARRANGE
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

        // ACT
        await _controller.RequestEmailUpdate(dto);

        // ASSERT
        Assert.That(callOrder, Has.Count.EqualTo(2));
        Assert.That(callOrder[0], Is.EqualTo("delete"));
        Assert.That(callOrder[1], Is.EqualTo("add"));
    }

    [Test]
    public async Task RequestEmailUpdate_WithoutReason_ShouldSucceed()
    {
        // ARRANGE
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

        // ACT
        var result = await _controller.RequestEmailUpdate(dto);

        // ASSERT
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        _mockEmailUpdateRequestRepository.Verify(r => r.AddAsync(It.IsAny<EmailUpdateRequest>()), Times.Once);
    }

    [Test]
    public async Task RequestEmailUpdate_RepositoryThrows_ShouldReturn500()
    {
        // ARRANGE
        var businessId = Guid.NewGuid();
        var dto = new RequestEmailUpdateDto
        {
            BusinessId = businessId,
            EmailAddress = "newemail@example.com"
        };

        _mockEmailUpdateRequestRepository
            .Setup(r => r.DeleteByBusinessIdAsync(businessId))
            .ThrowsAsync(new Exception("Database error"));

        // ACT
        var result = await _controller.RequestEmailUpdate(dto);

        // ASSERT
        var statusResult = result as ObjectResult;
        Assert.That(statusResult, Is.Not.Null);
        Assert.That(statusResult!.StatusCode, Is.EqualTo(500));
    }
}
