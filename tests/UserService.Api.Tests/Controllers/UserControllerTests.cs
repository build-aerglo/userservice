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
}
