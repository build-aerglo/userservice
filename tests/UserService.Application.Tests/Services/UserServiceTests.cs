using Moq;
using UserService.Application.DTOs;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using UserService.Application.Services;

namespace UserService.Application.Tests.Services;

[TestFixture]
public class UserServiceTests
{
    private Mock<IUserRepository> _mockUserRepository = null!;
    private Mock<IBusinessRepRepository> _mockBusinessRepRepository = null!;
    private Mock<IBusinessServiceClient> _mockBusinessServiceClient = null!;
    private Mock<ISupportUserProfileRepository> _mockSupportUserProfileRepository = null!;
    private Mock<IEndUserProfileRepository> _mockEndUserProfileRepository = null!;
    private Mock<IAuth0ManagementService> _mockAuth0 = null!;
    private Mock<IConfiguration> _mockConfig = null!;
    private Application.Services.UserService _service = null!;

    [SetUp]
    public void Setup()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockBusinessRepRepository = new Mock<IBusinessRepRepository>();
        _mockBusinessServiceClient = new Mock<IBusinessServiceClient>();
        _mockSupportUserProfileRepository = new Mock<ISupportUserProfileRepository>();
        _mockEndUserProfileRepository = new Mock<IEndUserProfileRepository>();
        _mockAuth0 = new Mock<IAuth0ManagementService>();
        _mockConfig = new Mock<IConfiguration>();

        // Auth0 role mocks
        _mockConfig.Setup(c => c["Auth0:Roles:BusinessUser"]).Returns("auth0_business_role");
        _mockConfig.Setup(c => c["Auth0:Roles:SupportUser"]).Returns("auth0_support_role");
        _mockConfig.Setup(c => c["Auth0:Roles:EndUser"]).Returns("auth0_end_role");

        // Mock Auth0 create+assign role
        _mockAuth0.Setup(a =>
                a.CreateUserAndAssignRoleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("auth0|dummy-id");

        _service = new Application.Services.UserService(
            _mockUserRepository.Object,
            _mockBusinessRepRepository.Object,
            _mockBusinessServiceClient.Object,
            _mockSupportUserProfileRepository.Object,
            _mockEndUserProfileRepository.Object,
            _mockAuth0.Object,
            _mockConfig.Object
        );
    }

    // ---------------- BUSINESS USER TESTS ----------------

    [Test]
    public async Task CreateSubBusinessUser_ShouldReturnResponse_WhenSuccessful()
    {
        var businessId = Guid.NewGuid();
        var dto = new CreateSubBusinessUserDto(businessId, "john_rep", "john@business.com", "1234567890", "123 Business St", "Main Branch", "456 Branch Ave");

        _mockBusinessServiceClient.Setup(c => c.BusinessExistsAsync(businessId)).ReturnsAsync(true);
        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockUserRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new User("john_rep", "john@business.com", "1234567890", "business_user", "123 Business St", "auth0|dummy-id"));

        _mockBusinessRepRepository.Setup(r => r.AddAsync(It.IsAny<BusinessRep>())).Returns(Task.CompletedTask);
        _mockBusinessRepRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new BusinessRep(businessId, Guid.NewGuid(), "Main Branch", "456 Branch Ave"));

        var result = await _service.CreateSubBusinessUserAsync(dto);

        Assert.Multiple(() =>
        {
            Assert.That(result.Username, Is.EqualTo("john_rep"));
            Assert.That(result.Email, Is.EqualTo("john@business.com"));
            Assert.That(result.BusinessId, Is.EqualTo(businessId));
        });
    }

    [Test]
    public void CreateSubBusinessUser_ShouldThrow_WhenBusinessDoesNotExist()
    {
        var dto = new CreateSubBusinessUserDto(Guid.NewGuid(), "x", "x", "x", null, null, null);
        _mockBusinessServiceClient.Setup(c => c.BusinessExistsAsync(dto.BusinessId)).ReturnsAsync(false);

        Assert.ThrowsAsync<BusinessNotFoundException>(() => _service.CreateSubBusinessUserAsync(dto));
    }

    [Test]
    public void CreateSubBusinessUser_ShouldThrow_WhenUserSaveFails()
    {
        var dto = new CreateSubBusinessUserDto(Guid.NewGuid(), "u", "e", "p", null, null, null);
        _mockBusinessServiceClient.Setup(c => c.BusinessExistsAsync(dto.BusinessId)).ReturnsAsync(true);
        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockUserRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        Assert.ThrowsAsync<UserCreationFailedException>(() => _service.CreateSubBusinessUserAsync(dto));
    }

    [Test]
    public void CreateSubBusinessUser_ShouldThrow_WhenBusinessRepSaveFails()
    {
        var businessId = Guid.NewGuid();
        var dto = new CreateSubBusinessUserDto(businessId, "u", "e", "p", null, null, null);

        _mockBusinessServiceClient.Setup(c => c.BusinessExistsAsync(businessId)).ReturnsAsync(true);
        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockUserRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new User("u", "e", "p", "business_user", null, "auth0|dummy-id"));

        _mockBusinessRepRepository.Setup(r => r.AddAsync(It.IsAny<BusinessRep>())).Returns(Task.CompletedTask);
        _mockBusinessRepRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((BusinessRep?)null);

        Assert.ThrowsAsync<UserCreationFailedException>(() => _service.CreateSubBusinessUserAsync(dto));
    }

    // ---------------- SUPPORT USER TESTS ----------------

    [Test]
    public async Task CreateSupportUser_ShouldReturnResponse_WhenSuccessful()
    {
        var dto = new CreateSupportUserDto("support", "support@test.com", "111", "addr");

        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockUserRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new User("support", "support@test.com", "111", "support_user", "addr", "auth0|dummy-id"));

        _mockSupportUserProfileRepository.Setup(r => r.AddAsync(It.IsAny<SupportUserProfile>())).Returns(Task.CompletedTask);
        _mockSupportUserProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new SupportUserProfile(Guid.NewGuid()));

        var result = await _service.CreateSupportUserAsync(dto);

        Assert.That(result.Email, Is.EqualTo("support@test.com"));
    }

    // ---------------- END USER TESTS ----------------

    [Test]
    public async Task CreateEndUser_ShouldReturnResponse_WhenSuccessful()
    {
        var dto = new CreateEndUserDto("jane", "jane@test.com", "123", "addr", null);
        var user = new User("jane", "jane@test.com", "123", "end_user", "addr", "auth0|dummy-id");
        var profile = new EndUserProfile(user.Id, null);

        _mockUserRepository.Setup(r => r.EmailExistsAsync(dto.Email)).ReturnsAsync(false);
        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockUserRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(user);

        _mockEndUserProfileRepository.Setup(r => r.AddAsync(It.IsAny<EndUserProfile>())).Returns(Task.CompletedTask);
        _mockEndUserProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(profile);

        var result = await _service.CreateEndUserAsync(dto);

        Assert.That(result.Email, Is.EqualTo("jane@test.com"));
    }
}
