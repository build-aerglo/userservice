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
        var dto = new CreateSupportUserDto("support", "admin@x.com", "111", "street");

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
        var dto = new CreateEndUserDto("jane", "jane@x.com", "123", "address", "social");

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
}