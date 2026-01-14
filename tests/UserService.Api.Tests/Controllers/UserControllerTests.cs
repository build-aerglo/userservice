using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using UserService.Api.Controllers;
using UserService.Application.DTOs;
using UserService.Application.Services;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Domain.Repositories;
using System.Text.Json;

namespace UserService.Api.Tests.Controllers;

[TestFixture]
public class UserControllerTests
{
    private Mock<IUserService> _mockUserService = null!;
    private Mock<IBusinessRepRepository> _mockBusinessRepRepository = null!;
    private Mock<ILogger<UserController>> _mockLogger = null!;
    private UserController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _mockUserService = new Mock<IUserService>();
        _mockBusinessRepRepository = new Mock<IBusinessRepRepository>();
        _mockLogger = new Mock<ILogger<UserController>>();
        _controller = new UserController(
            _mockUserService.Object,
            _mockBusinessRepRepository.Object,
            _mockLogger.Object
        );
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
            Password:"123456",
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
            Auth0UserId: "test",
            CreatedAt: DateTime.UtcNow
        );

        _mockUserService
            .Setup(s => s.CreateSubBusinessUserAsync(dto))
            .ReturnsAsync(expected);

        _controller.Url = new Mock<IUrlHelper>().Object;

        var result = await _controller.CreateSubBusinessUser(dto);
        var created = result as CreatedResult;
        Assert.That(created, Is.Not.Null);

        // ✅ Serialize and deserialize to get the actual DTO
        var json = JsonSerializer.Serialize(created!.Value);
        var response = JsonSerializer.Deserialize<SubBusinessUserResponseDto>(json);
        
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.UserId, Is.EqualTo(expected.UserId));
        Assert.That(response.Username, Is.EqualTo("john_rep"));
    }

    [Test]
    public async Task CreateSubBusinessUser_ShouldReturnNotFound_WhenBusinessDoesNotExist()
    {
        var dto = new CreateSubBusinessUserDto(
            BusinessId: Guid.NewGuid(),
            Username: "john_rep",
            Email: "john@business.com",
            Phone: "1234567890",
            Password:"123456",
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

    // ------------------------
    // ✅ Update Sub Business User
    // ------------------------
    [Test]
    public async Task UpdateSubBusinessUser_ShouldReturnOk_WhenSuccessful()
    {
        var id = Guid.NewGuid();
        var businessId = Guid.NewGuid();

        var dto = new UpdateSubBusinessUserDto(
            Email: "updated@business.com", 
            Phone: "9876543210", 
            Address: null, 
            BranchName: null, 
            BranchAddress: null
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
            Auth0UserId: "test",
            CreatedAt: DateTime.UtcNow
        );

        _mockUserService
            .Setup(s => s.UpdateSubBusinessUserAsync(id, dto))
            .ReturnsAsync(expected);

        var result = await _controller.UpdateSubBusinessUser(id, dto);
        var ok = result as OkObjectResult;

        Assert.That(ok, Is.Not.Null);
        
        // ✅ Serialize and deserialize to get the actual DTO
        var json = JsonSerializer.Serialize(ok!.Value);
        var response = JsonSerializer.Deserialize<SubBusinessUserResponseDto>(json);
        
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Email, Is.EqualTo("updated@business.com"));
    }

    // ------------------------
    // ✅ Support User Creation
    // ------------------------
    [Test]
    public async Task CreateSupportUser_ShouldReturnCreated_WhenSuccessful()
    {
        var dto = new CreateSupportUserDto("support", "admin@x.com", "test","111", "street");

        var expected = new SupportUserResponseDto(
            UserId: Guid.NewGuid(), 
            SupportUserProfileId: Guid.NewGuid(),
            Username: "support", 
            Email: "admin@x.com", 
            Phone: "111",
            Address: "street",
            Auth0UserId: "Test", 
            CreatedAt: DateTime.UtcNow
        );

        _mockUserService
            .Setup(s => s.CreateSupportUserAsync(dto))
            .ReturnsAsync(expected);

        _controller.Url = new Mock<IUrlHelper>().Object;
        var result = await _controller.CreateSupportUser(dto);

        var created = result as CreatedResult;
        Assert.That(created, Is.Not.Null);
        
        // ✅ Serialize and deserialize to get the actual DTO
        var json = JsonSerializer.Serialize(created!.Value);
        var response = JsonSerializer.Deserialize<SupportUserResponseDto>(json);
        
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Email, Is.EqualTo("admin@x.com"));
    }

    // ------------------------
    // ✅ End User Creation
    // ------------------------
    [Test]
    public async Task CreateEndUser_ShouldReturnCreated_WhenSuccessful()
    {
        var dto = new CreateEndUserDto("jane", "jane@x.com", "123456","123", "address", "social");

        var expected = new EndUserResponseDto(
            UserId: Guid.NewGuid(), 
            EndUserProfileId: Guid.NewGuid(),
            Username: "jane", 
            Email: "jane@x.com", 
            Phone: "123",
            Address: "address", 
            SocialMedia: "social",
            Auth0UserId: "Test", 
            CreatedAt: DateTime.UtcNow
        );

        _mockUserService
            .Setup(s => s.CreateEndUserAsync(dto))
            .ReturnsAsync(expected);

        _controller.Url = new Mock<IUrlHelper>().Object;
        var result = await _controller.CreateEndUser(dto);

        var created = result as CreatedResult;
        Assert.That(created, Is.Not.Null);
        
        // ✅ Serialize and deserialize to get the actual DTO
        var json = JsonSerializer.Serialize(created!.Value);
        var response = JsonSerializer.Deserialize<EndUserResponseDto>(json);
        
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Username, Is.EqualTo("jane"));
    }

    // ------------------------
    // GetBusinessRep Tests
    // ------------------------
    [Test]
    public async Task GetBusinessRep_ShouldReturnOk_WhenBusinessRepExists()
    {
        // Arrange
        var businessRepId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var businessRep = new BusinessRep(businessId, userId, "Main Branch", "123 Main St");
        typeof(BusinessRep).GetProperty("Id")!.SetValue(businessRep, businessRepId);

        _mockBusinessRepRepository
            .Setup(r => r.GetByIdAsync(businessRepId))
            .ReturnsAsync(businessRep);

        // Act
        var result = await _controller.GetBusinessRep(businessRepId);

        // Assert
        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.StatusCode, Is.EqualTo(200));

        // ✅ Use reflection to access anonymous type properties
        var response = ok.Value;
        Assert.That(response, Is.Not.Null);
        
        var responseType = response!.GetType();
        var idProp = responseType.GetProperty("Id")!.GetValue(response);
        var businessIdProp = responseType.GetProperty("BusinessId")!.GetValue(response);
        var userIdProp = responseType.GetProperty("UserId")!.GetValue(response);
        var branchNameProp = responseType.GetProperty("BranchName")!.GetValue(response);
        
        Assert.That(idProp, Is.EqualTo(businessRepId));
        Assert.That(businessIdProp, Is.EqualTo(businessId));
        Assert.That(userIdProp, Is.EqualTo(userId));
        Assert.That(branchNameProp, Is.EqualTo("Main Branch"));
    }

    [Test]
    public async Task GetBusinessRep_ShouldReturnNotFound_WhenBusinessRepDoesNotExist()
    {
        // Arrange
        var businessRepId = Guid.NewGuid();

        _mockBusinessRepRepository
            .Setup(r => r.GetByIdAsync(businessRepId))
            .ReturnsAsync((BusinessRep?)null);

        // Act
        var result = await _controller.GetBusinessRep(businessRepId);

        // Assert
        var notFound = result as NotFoundObjectResult;
        Assert.That(notFound, Is.Not.Null);
        Assert.That(notFound!.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task GetBusinessRep_ShouldReturnInternalServerError_OnException()
    {
        // Arrange
        var businessRepId = Guid.NewGuid();

        _mockBusinessRepRepository
            .Setup(r => r.GetByIdAsync(businessRepId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetBusinessRep(businessRepId);

        // Assert
        var error = result as ObjectResult;
        Assert.That(error, Is.Not.Null);
        Assert.That(error!.StatusCode, Is.EqualTo(500));
    }

    // ------------------------
    //  GetParentRepByBusinessId Tests
    // ------------------------
    [Test]
    public async Task GetParentRepByBusinessId_ShouldReturnOk_WhenParentRepExists()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var businessRepId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var parentRep = new BusinessRep(businessId, userId, "Parent Branch", "456 Parent Ave");
        typeof(BusinessRep).GetProperty("Id")!.SetValue(parentRep, businessRepId);

        _mockBusinessRepRepository
            .Setup(r => r.GetParentRepByBusinessIdAsync(businessId))
            .ReturnsAsync(parentRep);

        // Act
        var result = await _controller.GetParentRepByBusinessId(businessId);

        // Assert
        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.StatusCode, Is.EqualTo(200));

        // ✅ Use reflection to access anonymous type properties
        var response = ok.Value;
        Assert.That(response, Is.Not.Null);
        
        var responseType = response!.GetType();
        var idProp = responseType.GetProperty("Id")!.GetValue(response);
        var businessIdProp = responseType.GetProperty("BusinessId")!.GetValue(response);
        var userIdProp = responseType.GetProperty("UserId")!.GetValue(response);
        var branchNameProp = responseType.GetProperty("BranchName")!.GetValue(response);
        
        Assert.That(idProp, Is.EqualTo(businessRepId));
        Assert.That(businessIdProp, Is.EqualTo(businessId));
        Assert.That(userIdProp, Is.EqualTo(userId));
        Assert.That(branchNameProp, Is.EqualTo("Parent Branch"));
    }

    [Test]
    public async Task GetParentRepByBusinessId_ShouldReturnNotFound_WhenParentRepDoesNotExist()
    {
        // Arrange
        var businessId = Guid.NewGuid();

        _mockBusinessRepRepository
            .Setup(r => r.GetParentRepByBusinessIdAsync(businessId))
            .ReturnsAsync((BusinessRep?)null);

        // Act
        var result = await _controller.GetParentRepByBusinessId(businessId);

        // Assert
        var notFound = result as NotFoundObjectResult;
        Assert.That(notFound, Is.Not.Null);
        Assert.That(notFound!.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task GetParentRepByBusinessId_ShouldReturnInternalServerError_OnException()
    {
        // Arrange
        var businessId = Guid.NewGuid();

        _mockBusinessRepRepository
            .Setup(r => r.GetParentRepByBusinessIdAsync(businessId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetParentRepByBusinessId(businessId);

        // Assert
        var error = result as ObjectResult;
        Assert.That(error, Is.Not.Null);
        Assert.That(error!.StatusCode, Is.EqualTo(500));
    }
    
    public async Task GetEndUserProfileDetail_ShouldReturnOk_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedDto = new EndUserProfileDetailDto(
            UserId: userId,
            Username: "john_doe",
            Email: "john@example.com",
            Phone: "1234567890",
            Address: "123 Main St",
            JoinDate: DateTime.UtcNow.AddDays(-30),
            EndUserProfileId: Guid.NewGuid(),
            SocialMedia: "instagram.com/johndoe",
            NotificationPreferences: new NotificationPreferencesDto(
                EmailNotifications: true,
                SmsNotifications: false,
                PushNotifications: true,
                MarketingEmails: false
            ),
            DarkMode: false,
            CreatedAt: DateTime.UtcNow.AddDays(-30),
            UpdatedAt: DateTime.UtcNow
        );

        _mockUserService
            .Setup(s => s.GetEndUserProfileDetailAsync(userId))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _controller.GetEndUserProfileDetail(userId);

        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.StatusCode, Is.EqualTo(200));

        var json = JsonSerializer.Serialize(okResult.Value);
        var response = JsonSerializer.Deserialize<EndUserProfileDetailDto>(json);

        Assert.That(response, Is.Not.Null);
        Assert.That(response!.UserId, Is.EqualTo(userId));
        Assert.That(response.Username, Is.EqualTo("john_doe"));
        Assert.That(response.Email, Is.EqualTo("john@example.com"));
        Assert.That(response.Phone, Is.EqualTo("1234567890"));
        Assert.That(response.SocialMedia, Is.EqualTo("instagram.com/johndoe"));
        Assert.That(response.NotificationPreferences.EmailNotifications, Is.True);
        Assert.That(response.NotificationPreferences.PushNotifications, Is.True);
        Assert.That(response.DarkMode, Is.False);
    }

    [Test]
    public async Task GetEndUserProfileDetail_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockUserService
            .Setup(s => s.GetEndUserProfileDetailAsync(userId))
            .ThrowsAsync(new EndUserNotFoundException(userId));

        // Act
        var result = await _controller.GetEndUserProfileDetail(userId);

        // Assert
        var notFoundResult = result as NotFoundObjectResult;
        Assert.That(notFoundResult, Is.Not.Null);
        Assert.That(notFoundResult!.StatusCode, Is.EqualTo(404));

        // Verify error message
        var json = JsonSerializer.Serialize(notFoundResult.Value);
        var response = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.ContainsKey("error"), Is.True);
        Assert.That(response["error"], Does.Contain("not found"));
    }

    [Test]
    public async Task GetEndUserProfileDetail_ShouldReturnNotFound_WhenUserIsNotEndUser()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockUserService
            .Setup(s => s.GetEndUserProfileDetailAsync(userId))
            .ThrowsAsync(new EndUserNotFoundException(userId));

        // Act
        var result = await _controller.GetEndUserProfileDetail(userId);

        // Assert
        var notFoundResult = result as NotFoundObjectResult;
        Assert.That(notFoundResult, Is.Not.Null);
        Assert.That(notFoundResult!.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task GetEndUserProfileDetail_ShouldReturnInternalServerError_OnUnexpectedException()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockUserService
            .Setup(s => s.GetEndUserProfileDetailAsync(userId))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await _controller.GetEndUserProfileDetail(userId);

        // Assert
        var errorResult = result as ObjectResult;
        Assert.That(errorResult, Is.Not.Null);
        Assert.That(errorResult!.StatusCode, Is.EqualTo(500));

        var json = JsonSerializer.Serialize(errorResult.Value);
        var response = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.ContainsKey("error"), Is.True);
        Assert.That(response["error"], Is.EqualTo("Internal server error"));
    }

    [Test]
    public async Task GetEndUserProfileDetail_ShouldCallServiceOnce()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedDto = new EndUserProfileDetailDto(
            UserId: userId,
            Username: "test_user",
            Email: "test@example.com",
            Phone: "1234567890",
            Address: "Test Address",
            JoinDate: DateTime.UtcNow,
            EndUserProfileId: Guid.NewGuid(),
            SocialMedia: null,
            NotificationPreferences: new NotificationPreferencesDto(true, false, true, false),
            DarkMode: false,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow
        );

        _mockUserService
            .Setup(s => s.GetEndUserProfileDetailAsync(userId))
            .ReturnsAsync(expectedDto);

        // Act
        await _controller.GetEndUserProfileDetail(userId);

        // Assert
        _mockUserService.Verify(s => s.GetEndUserProfileDetailAsync(userId), Times.Once);
    }

    // ========================================================================
    // PUT ENDPOINT TESTS
    // ========================================================================

    [Test]
    public async Task UpdateEndUserProfileDetail_ShouldReturnOk_WhenUpdateSuccessful()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateDto = new UpdateEndUserProfileDto(
            Username: "Lizzy",
            Phone: "9876543210",
            Address: "456 New Street",
            SocialMedia: "twitter.com/johndoe",
            NotificationPreferences: new NotificationPreferencesDto(
                EmailNotifications: true,
                SmsNotifications: true,
                PushNotifications: false,
                MarketingEmails: true
            ),
            DarkMode: true
        );

        var expectedResponse = new EndUserProfileDetailDto(
            UserId: userId,
            Username: "john_doe",
            Email: "john@example.com",
            Phone: "9876543210",
            Address: "456 New Street",
            JoinDate: DateTime.UtcNow.AddDays(-30),
            EndUserProfileId: Guid.NewGuid(),
            SocialMedia: "twitter.com/johndoe",
            NotificationPreferences: new NotificationPreferencesDto(
                EmailNotifications: true,
                SmsNotifications: true,
                PushNotifications: false,
                MarketingEmails: true
            ),
            DarkMode: true,
            CreatedAt: DateTime.UtcNow.AddDays(-30),
            UpdatedAt: DateTime.UtcNow
        );

        _mockUserService
            .Setup(s => s.UpdateEndUserProfileAsync(userId, updateDto))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.UpdateEndUserProfileDetail(userId, updateDto);

        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.StatusCode, Is.EqualTo(200));

        var json = JsonSerializer.Serialize(okResult.Value);
        var response = JsonSerializer.Deserialize<EndUserProfileDetailDto>(json);

        Assert.That(response, Is.Not.Null);
        Assert.That(response!.UserId, Is.EqualTo(userId));
        Assert.That(response.Phone, Is.EqualTo("9876543210"));
        Assert.That(response.Address, Is.EqualTo("456 New Street"));
        Assert.That(response.SocialMedia, Is.EqualTo("twitter.com/johndoe"));
        Assert.That(response.NotificationPreferences.SmsNotifications, Is.True);
        Assert.That(response.DarkMode, Is.True);
    }

    [Test]
    public async Task UpdateEndUserProfileDetail_ShouldReturnOk_WhenPartialUpdate()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateDto = new UpdateEndUserProfileDto(
            Username: null,
            Phone: null,
            Address: null,
            SocialMedia: null,
            NotificationPreferences: null,
            DarkMode: true  // Only updating dark mode
        );

        var expectedResponse = new EndUserProfileDetailDto(
            UserId: userId,
            Username: "john_doe",
            Email: "john@example.com",
            Phone: "1234567890",
            Address: "123 Main St",
            JoinDate: DateTime.UtcNow.AddDays(-30),
            EndUserProfileId: Guid.NewGuid(),
            SocialMedia: "instagram.com/johndoe",
            NotificationPreferences: new NotificationPreferencesDto(true, false, true, false),
            DarkMode: true,
            CreatedAt: DateTime.UtcNow.AddDays(-30),
            UpdatedAt: DateTime.UtcNow
        );

        _mockUserService
            .Setup(s => s.UpdateEndUserProfileAsync(userId, updateDto))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.UpdateEndUserProfileDetail(userId, updateDto);

        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);

        var json = JsonSerializer.Serialize(okResult.Value);
        var response = JsonSerializer.Deserialize<EndUserProfileDetailDto>(json);

        Assert.That(response, Is.Not.Null);
        Assert.That(response!.DarkMode, Is.True);
        Assert.That(response.Phone, Is.EqualTo("1234567890")); // Unchanged
    }

    [Test]
    public async Task UpdateEndUserProfileDetail_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateDto = new UpdateEndUserProfileDto(
            Username: "Lizzy",
            Phone: "9876543210",
            Address: null,
            SocialMedia: null,
            NotificationPreferences: null,
            DarkMode: null
        );

        _mockUserService
            .Setup(s => s.UpdateEndUserProfileAsync(userId, updateDto))
            .ThrowsAsync(new EndUserNotFoundException(userId));

        // Act
        var result = await _controller.UpdateEndUserProfileDetail(userId, updateDto);

        // Assert
        var notFoundResult = result as NotFoundObjectResult;
        Assert.That(notFoundResult, Is.Not.Null);
        Assert.That(notFoundResult!.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task UpdateEndUserProfileDetail_ShouldReturnBadRequest_WhenModelStateInvalid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateDto = new UpdateEndUserProfileDto(
            Username: null,
            Phone: null,
            Address: null,
            SocialMedia: null,
            NotificationPreferences: null,
            DarkMode: null
        );

        _controller.ModelState.AddModelError("Phone", "Invalid phone format");

        // Act
        var result = await _controller.UpdateEndUserProfileDetail(userId, updateDto);

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null);
        Assert.That(badRequestResult!.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public async Task UpdateEndUserProfileDetail_ShouldReturnInternalServerError_OnUnexpectedException()
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
        // Arrange
        var userId = Guid.NewGuid();
        var updateDto = new UpdateEndUserProfileDto(
            Username: "Lizzy",
            Phone: "9876543210",
            Address: null,
            SocialMedia: null,
            NotificationPreferences: null,
            DarkMode: null
        );

        _mockUserService
            .Setup(s => s.UpdateEndUserProfileAsync(userId, updateDto))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await _controller.UpdateEndUserProfileDetail(userId, updateDto);

        // Assert
        var errorResult = result as ObjectResult;
        Assert.That(errorResult, Is.Not.Null);
        Assert.That(errorResult!.StatusCode, Is.EqualTo(500));
    }

    [Test]
    public async Task UpdateEndUserProfileDetail_ShouldCallServiceOnce()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateDto = new UpdateEndUserProfileDto(
            Username: "Lizzy",
            Phone: "9876543210",
            Address: "New Address",
            SocialMedia: "twitter.com/test",
            NotificationPreferences: new NotificationPreferencesDto(true, true, false, false),
            DarkMode: true
        );

        var expectedResponse = new EndUserProfileDetailDto(
            UserId: userId,
            Username: "test",
            Email: "test@example.com",
            Phone: "9876543210",
            Address: "New Address",
            JoinDate: DateTime.UtcNow,
            EndUserProfileId: Guid.NewGuid(),
            SocialMedia: "twitter.com/test",
            NotificationPreferences: new NotificationPreferencesDto(true, true, false, false),
            DarkMode: true,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow
        );

        _mockUserService
            .Setup(s => s.UpdateEndUserProfileAsync(userId, updateDto))
            .ReturnsAsync(expectedResponse);

        // Act
        await _controller.UpdateEndUserProfileDetail(userId, updateDto);

        // Assert
        _mockUserService.Verify(s => s.UpdateEndUserProfileAsync(userId, updateDto), Times.Once);
    }

    [Test]
    public async Task UpdateEndUserProfileDetail_ShouldUpdateOnlyNotificationPreferences()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateDto = new UpdateEndUserProfileDto(
            Username: null,
            Phone: null,
            Address: null,
            SocialMedia: null,
            NotificationPreferences: new NotificationPreferencesDto(
                EmailNotifications: false,
                SmsNotifications: true,
                PushNotifications: false,
                MarketingEmails: true
            ),
            DarkMode: null
        );

        var expectedResponse = new EndUserProfileDetailDto(
            UserId: userId,
            Username: "john_doe",
            Email: "john@example.com",
            Phone: "1234567890",
            Address: "123 Main St",
            JoinDate: DateTime.UtcNow.AddDays(-30),
            EndUserProfileId: Guid.NewGuid(),
            SocialMedia: "instagram.com/johndoe",
            NotificationPreferences: new NotificationPreferencesDto(
                EmailNotifications: false,
                SmsNotifications: true,
                PushNotifications: false,
                MarketingEmails: true
            ),
            DarkMode: false,
            CreatedAt: DateTime.UtcNow.AddDays(-30),
            UpdatedAt: DateTime.UtcNow
        );

        _mockUserService
            .Setup(s => s.UpdateEndUserProfileAsync(userId, updateDto))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.UpdateEndUserProfileDetail(userId, updateDto);

        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);

        var json = JsonSerializer.Serialize(okResult.Value);
        var response = JsonSerializer.Deserialize<EndUserProfileDetailDto>(json);

        Assert.That(response, Is.Not.Null);
        Assert.That(response!.NotificationPreferences.EmailNotifications, Is.False);
        Assert.That(response.NotificationPreferences.SmsNotifications, Is.True);
        Assert.That(response.NotificationPreferences.MarketingEmails, Is.True);
        Assert.That(response.Phone, Is.EqualTo("1234567890")); // Unchanged
        Assert.That(response.DarkMode, Is.False); // Unchanged
    }





}