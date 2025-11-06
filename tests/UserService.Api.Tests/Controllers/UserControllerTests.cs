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

    // --------------------------
    // ✅ Create Sub Business User
    // --------------------------
    [Test]
    public async Task CreateSubBusinessUser_ShouldReturnCreated_WhenSuccessful()
    {
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

        var expected = new SubBusinessUserResponseDto(
            UserId: Guid.NewGuid(),
            BusinessRepId: Guid.NewGuid(),
            BusinessId: businessId,
            Username: "john_rep",
            Email: "john@business.com",
            Phone: "1234567890",
            Address: "123 Business St",
            BranchName: "Main Branch",
            BranchAddress: "456 Branch Ave",
            Auth0UserId:"test",
            CreatedAt: DateTime.UtcNow
        );

        _mockUserService
            .Setup(s => s.CreateSubBusinessUserAsync(dto))
            .ReturnsAsync(expected);

        _controller.Url = new Mock<IUrlHelper>().Object;

        var result = await _controller.CreateSubBusinessUser(dto);
        var created = result as CreatedResult;
        Assert.That(created, Is.Not.Null);

        dynamic response = created!.Value!;
        Assert.That((Guid)response.UserId, Is.EqualTo(expected.UserId));
        Assert.That((string)response.Username, Is.EqualTo("john_rep"));
        Assert.That(response.Auth0UserId, Is.Null); // DTO has no Auth0Id yet (backend assigns)
    }

    [Test]
    public async Task CreateSubBusinessUser_ShouldReturnNotFound_WhenBusinessDoesNotExist()
    {
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

        var result = await _controller.CreateSubBusinessUser(dto);
        var notFound = result as NotFoundObjectResult;

        Assert.That(notFound, Is.Not.Null);
        Assert.That(notFound!.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task CreateSubBusinessUser_ShouldReturnInternalServerError_WhenUnexpectedErrorOccurs()
    {
        var dto = new CreateSubBusinessUserDto(
            BusinessId: Guid.NewGuid(), Username: "x", Email: "x", Phone: "1", Address: null, BranchName: null, BranchAddress: null
        );

        _mockUserService
            .Setup(s => s.CreateSubBusinessUserAsync(dto))
            .ThrowsAsync(new Exception("Unexpected"));

        var result = await _controller.CreateSubBusinessUser(dto);
        var error = result as ObjectResult;

        Assert.That(error, Is.Not.Null);
        Assert.That(error!.StatusCode, Is.EqualTo(500));
    }

    // ------------------------
    // ✅ Update Sub Business User
    // ------------------------
    [Test]
    public async Task UpdateSubBusinessUser_ShouldReturnOk_WhenSuccessful()
    {
        var id = Guid.NewGuid();
        var businessId = Guid.NewGuid();

        var dto = new UpdateSubBusinessUserDto(
            Email: "updated@business.com", Phone: "9876543210", Address: null, BranchName: null, BranchAddress: null
        );

        var expected = new SubBusinessUserResponseDto(
            UserId: id,
            BusinessRepId: Guid.NewGuid(),
            BusinessId: businessId,
            Username: "john_rep",
            Email: "updated@business.com",
            Phone: "9876543210",
            Address: "old",
            BranchName: "Main",
            BranchAddress: "Old addr",
            Auth0UserId:"test",
            CreatedAt: DateTime.UtcNow
        );

        _mockUserService
            .Setup(s => s.UpdateSubBusinessUserAsync(id, dto))
            .ReturnsAsync(expected);

        var result = await _controller.UpdateSubBusinessUser(id, dto);
        var ok = result as OkObjectResult;

        Assert.That(ok, Is.Not.Null);
        dynamic response = ok!.Value!;
        Assert.That((string)response.Email, Is.EqualTo("updated@business.com"));
    }

    // ------------------------
    // ✅ Support User Creation
    // ------------------------
    [Test]
    public async Task CreateSupportUser_ShouldReturnCreated_WhenSuccessful()
    {
        var dto = new CreateSupportUserDto("support", "admin@x.com", "111", "street");

        var expected = new SupportUserResponseDto(
            UserId: Guid.NewGuid(), SupportUserProfileId: Guid.NewGuid(),
            Username: "support", Email: "admin@x.com", Phone: "111",
            Address: "street",Auth0UserId:"Test", CreatedAt: DateTime.UtcNow
        );

        _mockUserService
            .Setup(s => s.CreateSupportUserAsync(dto))
            .ReturnsAsync(expected);

        _controller.Url = new Mock<IUrlHelper>().Object;
        var result = await _controller.CreateSupportUser(dto);

        var created = result as CreatedResult;
        Assert.That(created, Is.Not.Null);
        dynamic response = created!.Value!;
        Assert.That((string)response.Email, Is.EqualTo("admin@x.com"));
    }

    // ------------------------
    // ✅ End User Creation
    // ------------------------
    [Test]
    public async Task CreateEndUser_ShouldReturnCreated_WhenSuccessful()
    {
        var dto = new CreateEndUserDto("jane", "jane@x.com", "123", "address", "social");

        var expected = new EndUserResponseDto(
            UserId: Guid.NewGuid(), EndUserProfileId: Guid.NewGuid(),
            Username: "jane", Email: "jane@x.com", Phone: "123",
            Address: "address", SocialMedia: "social",Auth0UserId:"Test", CreatedAt: DateTime.UtcNow
        );

        _mockUserService
            .Setup(s => s.CreateEndUserAsync(dto))
            .ReturnsAsync(expected);

        _controller.Url = new Mock<IUrlHelper>().Object;
        var result = await _controller.CreateEndUser(dto);

        var created = result as CreatedResult;
        Assert.That(created, Is.Not.Null);
        dynamic response = created!.Value!;
        Assert.That((string)response.Username, Is.EqualTo("jane"));
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
    
    
    // DELETE USER
        
    [Test]
    public async Task DeleteUser_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var type = "support_user";
    
        _mockUserService
            .Setup(s => s.DeleteUserAsync(userId, type))
            .Returns(Task.CompletedTask);
    
        // Act
        var result = await _controller.DeleteUser(userId, type);
            
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<OkResult>());
            
        _mockUserService.Verify(s => s.DeleteUserAsync(userId, type), Times.Once);
    }
    
    [Test]
    public async Task DeleteUser_ShouldReturn500_WhenUserNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var type = "end_user";

        _mockUserService
            .Setup(s => s.DeleteUserAsync(userId, type))
            .ThrowsAsync(new UserNotFoundException(userId));

        // Act
        var result = await _controller.DeleteUser(userId, type) as ObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(500));
    }
    
    [Test]
    public async Task DeleteUser_ShouldReturn500_WhenUserTypeInvalid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var type = "invalid_type";

        _mockUserService
            .Setup(s => s.DeleteUserAsync(userId, type))
            .ThrowsAsync(new UserTypeNotFoundException(type));

        // Act
        var result = await _controller.DeleteUser(userId, type) as ObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(500));
    }
    
    // UPDATE BUSINESS
    
    [Test]
    public async Task UpdateBusinessUser_ShouldReturnNoContent_WhenSuccessful()
    {
        // Arrange
        var dto = new UpdateBusinessUserDto(
            Id: Guid.NewGuid(),
            Name: "Test Business",
            Email: "test@example.com",
            Phone: "1234567890",
            Address: "123 Main St",
            BranchName: "HQ",
            BranchAddress: "456 Elm St",
            Website: "https://example.com",
            CategoryIds: new List<string> { "cat1", "cat2" }
        );

        _mockUserService
            .Setup(s => s.UpdateBusinessAccount(dto))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateBusinessUser(dto);

        // Assert
        Assert.That(result, Is.TypeOf<NoContentResult>());
        _mockUserService.Verify(s => s.UpdateBusinessAccount(dto), Times.Once);
    }
    
    [Test]
    public async Task UpdateBusinessUser_ShouldReturn500_WhenBusinessNotFound()
    {
        // Arrange
        var dto = new UpdateBusinessUserDto(
            Id: Guid.NewGuid(),
            Name: "Test Business",
            Email: "test@example.com",
            Phone: "1234567890",
            Address: null,
            BranchName: null,
            BranchAddress: null,
            Website: null,
            CategoryIds: new List<string>()
        );

        _mockUserService
            .Setup(s => s.UpdateBusinessAccount(dto))
            .ThrowsAsync(new BusinessNotFoundException(dto.Id));

        // Act
        var result = await _controller.UpdateBusinessUser(dto) as ObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(500));
    }

    [Test]
    public async Task UpdateBusinessUser_ShouldReturn500_WhenUserNotFound()
    {
        // Arrange
        var dto = new UpdateBusinessUserDto(
            Id: Guid.NewGuid(),
            Name: "Test Business",
            Email: "test@example.com",
            Phone: "1234567890",
            Address: null,
            BranchName: null,
            BranchAddress: null,
            Website: null,
            CategoryIds: new List<string>()
        );

        _mockUserService
            .Setup(s => s.UpdateBusinessAccount(dto))
            .ThrowsAsync(new UserNotFoundException(dto.Id));

        // Act
        var result = await _controller.UpdateBusinessUser(dto) as ObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(500));
    }
}

