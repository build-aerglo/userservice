using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using UserService.Api.Controllers;
using UserService.Application.DTOs;
using UserService.Application.Services;
using UserService.Domain.Exceptions;

namespace UserService.Api.Tests.Controllers;

[TestFixture]
public class UserControllerTests
{
    private Mock<IUserService> _mockUserService = null!;
    private Mock<ILogger<UserController>> _mockLogger = null!;
    private UserController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _mockUserService = new Mock<IUserService>();
        _mockLogger = new Mock<ILogger<UserController>>();

        _controller = new UserController(_mockUserService.Object, _mockLogger.Object);
    }

    [Test]
    public async Task CreateSubBusinessUser_ShouldReturnCreated_WhenSuccessful()
    {
        // ARRANGE
        var businessId = Guid.NewGuid();

        var dto = new CreateSubBusinessUserDto(
            BusinessId: businessId,
            Username: "john_rep",
            Email: "john@business.com",
            Phone: "1234567890",
            Address: "123 Business St",
            BranchName: "Main Branch",
            BranchAddress: "456 Branch Ave"
        );

        var response = new SubBusinessUserResponseDto(
            UserId: Guid.NewGuid(),
            BusinessRepId: Guid.NewGuid(),
            BusinessId: businessId,
            Username: "john_rep",
            Email: "john@business.com",
            Phone: "1234567890",
            Address: "123 Business St",
            BranchName: "Main Branch",
            BranchAddress: "456 Branch Ave",
            CreatedAt: DateTime.UtcNow
        );

        _mockUserService
            .Setup(s => s.CreateSubBusinessUserAsync(dto))
            .ReturnsAsync(response);

        // Mock Url.Action to avoid null references in tests
        var mockUrlHelper = new Mock<IUrlHelper>();
        mockUrlHelper
            .Setup(u => u.Action(It.IsAny<UrlActionContext>()))
            .Returns("/api/user/" + response.UserId);

        _controller.Url = mockUrlHelper.Object;

        // ACT
        var result = await _controller.CreateSubBusinessUser(dto);

        // ASSERT
        var createdResult = result as CreatedResult;
        Assert.That(createdResult, Is.Not.Null, "Expected a CreatedResult but got null (Url.Action may be null).");
        Assert.That(createdResult!.StatusCode, Is.EqualTo(201));

        var returnedValue = createdResult.Value as SubBusinessUserResponseDto;
        Assert.That(returnedValue, Is.Not.Null);
        Assert.That(returnedValue!.Username, Is.EqualTo("john_rep"));
        Assert.That(returnedValue.BusinessId, Is.EqualTo(businessId));

        _mockUserService.Verify(s => s.CreateSubBusinessUserAsync(dto), Times.Once);
    }


    [Test]
    public async Task CreateSubBusinessUser_ShouldReturnNotFound_WhenBusinessDoesNotExist()
    {
        // ARRANGE
        var dto = new CreateSubBusinessUserDto(
            BusinessId: Guid.NewGuid(),
            Username: "john_rep",
            Email: "john@business.com",
            Phone: "1234567890",
            Address: null,
            BranchName: null,
            BranchAddress: null
        );

        _mockUserService
            .Setup(s => s.CreateSubBusinessUserAsync(dto))
            .ThrowsAsync(new BusinessNotFoundException(dto.BusinessId));

        // ACT
        var result = await _controller.CreateSubBusinessUser(dto);

        // ASSERT
        var notFoundResult = result as NotFoundObjectResult;
        Assert.That(notFoundResult, Is.Not.Null);
        Assert.That(notFoundResult!.StatusCode, Is.EqualTo(404));
        Assert.That(notFoundResult.Value?.ToString(), Does.Contain(dto.BusinessId.ToString()));
    }

    [Test]
    public async Task CreateSubBusinessUser_ShouldReturnInternalServerError_WhenUserCreationFails()
    {
        // ARRANGE
        var dto = new CreateSubBusinessUserDto(
            BusinessId: Guid.NewGuid(),
            Username: "failed_user",
            Email: "failed@business.com",
            Phone: "1234567890",
            Address: null,
            BranchName: null,
            BranchAddress: null
        );

        _mockUserService
            .Setup(s => s.CreateSubBusinessUserAsync(dto))
            .ThrowsAsync(new UserCreationFailedException("Failed to create user record."));

        // ACT
        var result = await _controller.CreateSubBusinessUser(dto);

        // ASSERT
        var errorResult = result as ObjectResult;
        Assert.That(errorResult, Is.Not.Null);
        Assert.That(errorResult!.StatusCode, Is.EqualTo(500));

        var value = errorResult.Value?.ToString();
        Assert.That(value, Does.Contain("Failed to create user record."));
    }

    [Test]
    public async Task CreateSubBusinessUser_ShouldReturnInternalServerError_WhenUnexpectedErrorOccurs()
    {
        // ARRANGE
        var dto = new CreateSubBusinessUserDto(
            BusinessId: Guid.NewGuid(),
            Username: "unexpected_user",
            Email: "unexpected@business.com",
            Phone: "9999999999",
            Address: null,
            BranchName: null,
            BranchAddress: null
        );

        _mockUserService
            .Setup(s => s.CreateSubBusinessUserAsync(dto))
            .ThrowsAsync(new Exception("Unexpected failure"));

        // ACT
        var result = await _controller.CreateSubBusinessUser(dto);

        // ASSERT
        var errorResult = result as ObjectResult;
        Assert.That(errorResult, Is.Not.Null);
        Assert.That(errorResult!.StatusCode, Is.EqualTo(500));

        var errorValue = errorResult.Value?.ToString();
        Assert.That(errorValue, Does.Contain("Internal server error occurred."));
    }
    
    
    // Support User tests
    [Test]
    public async Task CreateSupportUser_ShouldReturnCreated_WhenSuccessful()
    {
        // ARRANGE
        var dto = new CreateSupportUserDto(
            Username: "support_admin",
            Email: "admin@support.com",
            Phone: "1234567890",
            Address: "123 Support St"
        );

        var response = new SupportUserResponseDto(
            UserId: Guid.NewGuid(),
            SupportUserProfileId: Guid.NewGuid(),
            Username: "support_admin",
            Email: "admin@support.com",
            Phone: "1234567890",
            Address: "123 Support St",
            CreatedAt: DateTime.UtcNow
        );

        _mockUserService
            .Setup(s => s.CreateSupportUserAsync(dto))
            .ReturnsAsync(response);

        // Mock Url.Action to avoid null references in tests
        var mockUrlHelper = new Mock<IUrlHelper>();
        mockUrlHelper
            .Setup(u => u.Action(It.IsAny<UrlActionContext>()))
            .Returns("/api/user/" + response.UserId);

        _controller.Url = mockUrlHelper.Object;

        // ACT
        var result = await _controller.CreateSupportUser(dto);

        // ASSERT
        var createdResult = result as CreatedResult;
        Assert.That(createdResult, Is.Not.Null, "Expected a CreatedResult");
        Assert.That(createdResult!.StatusCode, Is.EqualTo(201));

        var returnedValue = createdResult.Value as SupportUserResponseDto;
        Assert.That(returnedValue, Is.Not.Null);
        Assert.That(returnedValue!.Username, Is.EqualTo("support_admin"));
        Assert.That(returnedValue.Email, Is.EqualTo("admin@support.com"));

        _mockUserService.Verify(s => s.CreateSupportUserAsync(dto), Times.Once);
    }

    [Test]
    public async Task CreateSupportUser_ShouldReturnInternalServerError_WhenUserCreationFails()
    {
        // ARRANGE
        var dto = new CreateSupportUserDto(
            Username: "failed_support",
            Email: "failed@support.com",
            Phone: "9999999999",
            Address: null
        );

        _mockUserService
            .Setup(s => s.CreateSupportUserAsync(dto))
            .ThrowsAsync(new UserCreationFailedException("Failed to create user record."));

        // ACT
        var result = await _controller.CreateSupportUser(dto);

        // ASSERT
        var errorResult = result as ObjectResult;
        Assert.That(errorResult, Is.Not.Null);
        Assert.That(errorResult!.StatusCode, Is.EqualTo(500));

        var value = errorResult.Value?.ToString();
        Assert.That(value, Does.Contain("Failed to create user record."));
    }

    [Test]
    public async Task CreateSupportUser_ShouldReturnInternalServerError_WhenUnexpectedErrorOccurs()
    {
        // ARRANGE
        var dto = new CreateSupportUserDto(
            Username: "unexpected_support",
            Email: "unexpected@support.com",
            Phone: "8888888888",
            Address: "Unexpected St"
        );

        _mockUserService
            .Setup(s => s.CreateSupportUserAsync(dto))
            .ThrowsAsync(new Exception("Unexpected failure"));

        // ACT
        var result = await _controller.CreateSupportUser(dto);

        // ASSERT
        var errorResult = result as ObjectResult;
        Assert.That(errorResult, Is.Not.Null);
        Assert.That(errorResult!.StatusCode, Is.EqualTo(500));

        var errorValue = errorResult.Value?.ToString();
        Assert.That(errorValue, Does.Contain("Internal server error occurred."));
    }

    [Test]
    public async Task CreateSupportUser_ShouldReturnBadRequest_WhenModelStateIsInvalid()
    {
        // ARRANGE
        var dto = new CreateSupportUserDto(
            Username: "",  // Invalid - empty username
            Email: "invalid@support.com",
            Phone: "7777777777",
            Address: null
        );

        _controller.ModelState.AddModelError("Username", "Username is required");

        // ACT
        var result = await _controller.CreateSupportUser(dto);

        // ASSERT
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null);
        Assert.That(badRequestResult!.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public async Task CreateSupportUser_WithNullAddress_ShouldSucceed()
    {
        // ARRANGE
        var dto = new CreateSupportUserDto(
            Username: "no_address_support",
            Email: "noaddr@support.com",
            Phone: "5555555555",
            Address: null
        );

        var response = new SupportUserResponseDto(
            UserId: Guid.NewGuid(),
            SupportUserProfileId: Guid.NewGuid(),
            Username: "no_address_support",
            Email: "noaddr@support.com",
            Phone: "5555555555",
            Address: null,
            CreatedAt: DateTime.UtcNow
        );

        _mockUserService
            .Setup(s => s.CreateSupportUserAsync(dto))
            .ReturnsAsync(response);

        var mockUrlHelper = new Mock<IUrlHelper>();
        mockUrlHelper
            .Setup(u => u.Action(It.IsAny<UrlActionContext>()))
            .Returns("/api/user/" + response.UserId);

        _controller.Url = mockUrlHelper.Object;

        // ACT
        var result = await _controller.CreateSupportUser(dto);

        // ASSERT
        var createdResult = result as CreatedResult;
        Assert.That(createdResult, Is.Not.Null);

        var returnedValue = createdResult!.Value as SupportUserResponseDto;
        Assert.That(returnedValue!.Address, Is.Null);
    }
}
