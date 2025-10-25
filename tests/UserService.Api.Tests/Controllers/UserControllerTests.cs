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



    // UPDATE SUPPORT USER TESTS
    [Test]
    public async Task UpdateSupportUser_ShouldReturnOk_WhenSuccessful()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var dto = new UpdateSupportUserDto(
            Email: "updated@support.com",
            Phone: "9876543210",
            Address: "456 Updated St"
        );

        var response = new SupportUserResponseDto(
            UserId: userId,
            SupportUserProfileId: Guid.NewGuid(),
            Username: "support_admin",
            Email: "updated@support.com",
            Phone: "9876543210",
            Address: "456 Updated St",
            CreatedAt: DateTime.UtcNow
        );

        _mockUserService
            .Setup(s => s.UpdateSupportUserAsync(userId, dto))
            .ReturnsAsync(response);

        // ACT
        var result = await _controller.UpdateSupportUser(userId, dto);

        // ASSERT
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.StatusCode, Is.EqualTo(200));

        var returnedValue = okResult.Value as SupportUserResponseDto;
        Assert.That(returnedValue, Is.Not.Null);
        Assert.That(returnedValue!.Email, Is.EqualTo("updated@support.com"));
        Assert.That(returnedValue.Phone, Is.EqualTo("9876543210"));
        Assert.That(returnedValue.Address, Is.EqualTo("456 Updated St"));

        _mockUserService.Verify(s => s.UpdateSupportUserAsync(userId, dto), Times.Once);
    }

    [Test]
    public async Task UpdateSupportUser_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var dto = new UpdateSupportUserDto(
            Email: "notfound@support.com",
            Phone: "1234567890",
            Address: null
        );

        _mockUserService
            .Setup(s => s.UpdateSupportUserAsync(userId, dto))
            .ThrowsAsync(new SupportUserNotFoundException(userId));

        // ACT
        var result = await _controller.UpdateSupportUser(userId, dto);

        // ASSERT
        var notFoundResult = result as NotFoundObjectResult;
        Assert.That(notFoundResult, Is.Not.Null);
        Assert.That(notFoundResult!.StatusCode, Is.EqualTo(404));
        Assert.That(notFoundResult.Value?.ToString(), Does.Contain(userId.ToString()));
    }

    [Test]
    public async Task UpdateSupportUser_ShouldReturnInternalServerError_WhenUpdateFails()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var dto = new UpdateSupportUserDto(
            Email: "fail@support.com",
            Phone: "1234567890",
            Address: "Fail St"
        );

        _mockUserService
            .Setup(s => s.UpdateSupportUserAsync(userId, dto))
            .ThrowsAsync(new SupportUserUpdateFailedException("Failed to update user record."));

        // ACT
        var result = await _controller.UpdateSupportUser(userId, dto);

        // ASSERT
        var errorResult = result as ObjectResult;
        Assert.That(errorResult, Is.Not.Null);
        Assert.That(errorResult!.StatusCode, Is.EqualTo(500));
        Assert.That(errorResult.Value?.ToString(), Does.Contain("Failed to update user record."));
    }

    [Test]
    public async Task UpdateSupportUser_ShouldReturnInternalServerError_WhenUnexpectedErrorOccurs()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var dto = new UpdateSupportUserDto(
            Email: "unexpected@support.com",
            Phone: "9999999999",
            Address: "Unexpected St"
        );

        _mockUserService
            .Setup(s => s.UpdateSupportUserAsync(userId, dto))
            .ThrowsAsync(new Exception("Unexpected failure"));

        // ACT
        var result = await _controller.UpdateSupportUser(userId, dto);

        // ASSERT
        var errorResult = result as ObjectResult;
        Assert.That(errorResult, Is.Not.Null);
        Assert.That(errorResult!.StatusCode, Is.EqualTo(500));
        Assert.That(errorResult.Value?.ToString(), Does.Contain("Internal server error occurred."));
    }

    [Test]
    public async Task UpdateSupportUser_ShouldReturnBadRequest_WhenModelStateIsInvalid()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var dto = new UpdateSupportUserDto(
            Email: "",  // Invalid - empty email
            Phone: "1234567890",
            Address: null
        );

        _controller.ModelState.AddModelError("Email", "Email cannot be empty");

        // ACT
        var result = await _controller.UpdateSupportUser(userId, dto);

        // ASSERT
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null);
        Assert.That(badRequestResult!.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public async Task UpdateSupportUser_WithPartialUpdate_ShouldSucceed()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var dto = new UpdateSupportUserDto(
            Email: "partial@support.com",
            Phone: null,  // Not updating phone
            Address: null  // Not updating address
        );

        var response = new SupportUserResponseDto(
            UserId: userId,
            SupportUserProfileId: Guid.NewGuid(),
            Username: "support_admin",
            Email: "partial@support.com",
            Phone: "1234567890",  // Original phone retained
            Address: "123 Original St",  // Original address retained
            CreatedAt: DateTime.UtcNow
        );

        _mockUserService
            .Setup(s => s.UpdateSupportUserAsync(userId, dto))
            .ReturnsAsync(response);

        // ACT
        var result = await _controller.UpdateSupportUser(userId, dto);

        // ASSERT
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);

        var returnedValue = okResult!.Value as SupportUserResponseDto;
        Assert.That(returnedValue!.Email, Is.EqualTo("partial@support.com"));
        Assert.That(returnedValue.Phone, Is.EqualTo("1234567890"));
    }

    [Test]
    public async Task UpdateSupportUser_WithAllNullFields_ShouldStillSucceed()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var dto = new UpdateSupportUserDto(
            Email: null,
            Phone: null,
            Address: null
        );

        var response = new SupportUserResponseDto(
            UserId: userId,
            SupportUserProfileId: Guid.NewGuid(),
            Username: "support_admin",
            Email: "original@support.com",
            Phone: "1234567890",
            Address: "123 Original St",
            CreatedAt: DateTime.UtcNow
        );

        _mockUserService
            .Setup(s => s.UpdateSupportUserAsync(userId, dto))
            .ReturnsAsync(response);

        // ACT
        var result = await _controller.UpdateSupportUser(userId, dto);

        // ASSERT
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.StatusCode, Is.EqualTo(200));
    }

    [Test]
    public async Task UpdateSupportUser_ShouldReturnInternalServerError_WhenUserIsNotSupportUser()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var dto = new UpdateSupportUserDto(
            Email: "test@support.com",
            Phone: "1234567890",
            Address: null
        );

        _mockUserService
            .Setup(s => s.UpdateSupportUserAsync(userId, dto))
            .ThrowsAsync(new SupportUserUpdateFailedException($"User with ID {userId} is not a support user."));

        // ACT
        var result = await _controller.UpdateSupportUser(userId, dto);

        // ASSERT
        var errorResult = result as ObjectResult;
        Assert.That(errorResult, Is.Not.Null);
        Assert.That(errorResult!.StatusCode, Is.EqualTo(500));
        Assert.That(errorResult.Value?.ToString(), Does.Contain("is not a support user"));
    }
    
    // ---------------------- END USER TESTS ----------------------
[Test]
public async Task CreateEndUser_ShouldReturnCreated_WhenSuccessful()
{
    // ARRANGE
    var dto = new CreateEndUserDto(
        Username: "jane_doe",
        Email: "jane@example.com",
        Phone: "1234567890",
        Address: "123 Main St",
        SocialMedia: "https://twitter.com/jane_doe"
    );

    var response = new EndUserResponseDto(
        UserId: Guid.NewGuid(),
        EndUserProfileId: Guid.NewGuid(),
        Username: "jane_doe",
        Email: "jane@example.com",
        Phone: "1234567890",
        Address: "123 Main St",
        SocialMedia: "https://twitter.com/jane_doe",
        CreatedAt: DateTime.UtcNow
    );

    _mockUserService
        .Setup(s => s.CreateEndUserAsync(dto))
        .ReturnsAsync(response);

    var mockUrlHelper = new Mock<IUrlHelper>();
    mockUrlHelper
        .Setup(u => u.Action(It.IsAny<UrlActionContext>()))
        .Returns("/api/user/" + response.UserId);

    _controller.Url = mockUrlHelper.Object;

    // ACT
    var result = await _controller.CreateEndUser(dto);

    // ASSERT
    var createdResult = result as CreatedResult;
    Assert.That(createdResult, Is.Not.Null);
    Assert.That(createdResult!.StatusCode, Is.EqualTo(201));

    var returnedValue = createdResult.Value as EndUserResponseDto;
    Assert.That(returnedValue, Is.Not.Null);
    Assert.That(returnedValue!.Username, Is.EqualTo("jane_doe"));
    Assert.That(returnedValue.Email, Is.EqualTo("jane@example.com"));
    Assert.That(returnedValue.SocialMedia, Is.EqualTo("https://twitter.com/jane_doe"));

    _mockUserService.Verify(s => s.CreateEndUserAsync(dto), Times.Once);
}

[Test]
public async Task CreateEndUser_ShouldReturnConflict_WhenEmailAlreadyExists()
{
    // ARRANGE
    var dto = new CreateEndUserDto(
        Username: "duplicate_user",
        Email: "duplicate@example.com",
        Phone: "9999999999",
        Address: "Duplicate St",
        SocialMedia: null
    );

    _mockUserService
        .Setup(s => s.CreateEndUserAsync(dto))
        .ThrowsAsync(new DuplicateUserEmailException($"Email '{dto.Email}' already exists."));

    // ACT
    var result = await _controller.CreateEndUser(dto);

    // ASSERT
    var conflictResult = result as ObjectResult;
    Assert.That(conflictResult, Is.Not.Null);
    Assert.That(conflictResult!.StatusCode, Is.EqualTo(409));

    var errorValue = conflictResult.Value?.ToString();
    Assert.That(errorValue, Does.Contain("already exists"));
}

[Test]
public async Task CreateEndUser_ShouldReturnInternalServerError_WhenUserCreationFails()
{
    // ARRANGE
    var dto = new CreateEndUserDto(
        Username: "failed_end_user",
        Email: "fail@enduser.com",
        Phone: "0000000000",
        Address: null,
        SocialMedia: null
    );

    _mockUserService
        .Setup(s => s.CreateEndUserAsync(dto))
        .ThrowsAsync(new UserCreationFailedException("Failed to create user record."));

    // ACT
    var result = await _controller.CreateEndUser(dto);

    // ASSERT
    var errorResult = result as ObjectResult;
    Assert.That(errorResult, Is.Not.Null);
    Assert.That(errorResult!.StatusCode, Is.EqualTo(500));
    Assert.That(errorResult.Value?.ToString(), Does.Contain("Failed to create user record."));
}

[Test]
public async Task CreateEndUser_ShouldReturnInternalServerError_WhenUnexpectedErrorOccurs()
{
    // ARRANGE
    var dto = new CreateEndUserDto(
        Username: "unexpected_user",
        Email: "unexpected@enduser.com",
        Phone: "4444444444",
        Address: "Unexpected St",
        SocialMedia: null
    );

    _mockUserService
        .Setup(s => s.CreateEndUserAsync(dto))
        .ThrowsAsync(new Exception("Unexpected failure"));

    // ACT
    var result = await _controller.CreateEndUser(dto);

    // ASSERT
    var errorResult = result as ObjectResult;
    Assert.That(errorResult, Is.Not.Null);
    Assert.That(errorResult!.StatusCode, Is.EqualTo(500));
    Assert.That(errorResult.Value?.ToString(), Does.Contain("Internal server error occurred."));
}

[Test]
public async Task CreateEndUser_ShouldReturnBadRequest_WhenModelStateIsInvalid()
{
    // ARRANGE
    var dto = new CreateEndUserDto(
        Username: "", // invalid
        Email: "invalid@enduser.com",
        Phone: "5555555555",
        Address: "Bad Street",
        SocialMedia: null
    );

    _controller.ModelState.AddModelError("Username", "Username is required");

    // ACT
    var result = await _controller.CreateEndUser(dto);

    // ASSERT
    var badRequestResult = result as BadRequestObjectResult;
    Assert.That(badRequestResult, Is.Not.Null);
    Assert.That(badRequestResult!.StatusCode, Is.EqualTo(400));
}

[Test]
public async Task CreateEndUser_WithNullSocialMedia_ShouldSucceed()
{
    // ARRANGE
    var dto = new CreateEndUserDto(
        Username: "no_social",
        Email: "nosocial@enduser.com",
        Phone: "1234567890",
        Address: "123 No Social St",
        SocialMedia: null
    );

    var response = new EndUserResponseDto(
        UserId: Guid.NewGuid(),
        EndUserProfileId: Guid.NewGuid(),
        Username: "no_social",
        Email: "nosocial@enduser.com",
        Phone: "1234567890",
        Address: "123 No Social St",
        SocialMedia: null,
        CreatedAt: DateTime.UtcNow
    );

    _mockUserService
        .Setup(s => s.CreateEndUserAsync(dto))
        .ReturnsAsync(response);

    var mockUrlHelper = new Mock<IUrlHelper>();
    mockUrlHelper
        .Setup(u => u.Action(It.IsAny<UrlActionContext>()))
        .Returns("/api/user/" + response.UserId);

    _controller.Url = mockUrlHelper.Object;

    // ACT
    var result = await _controller.CreateEndUser(dto);

    // ASSERT
    var createdResult = result as CreatedResult;
    Assert.That(createdResult, Is.Not.Null);
    Assert.That(createdResult!.StatusCode, Is.EqualTo(201));

    var returnedValue = createdResult.Value as EndUserResponseDto;
    Assert.That(returnedValue!.SocialMedia, Is.Null);
}

}
