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

        // Auth0 role mappings
        _mockConfig.Setup(c => c["Auth0:Roles:BusinessUser"]).Returns("auth0_business_role");
        _mockConfig.Setup(c => c["Auth0:Roles:SupportUser"]).Returns("auth0_support_role");
        _mockConfig.Setup(c => c["Auth0:Roles:EndUser"]).Returns("auth0_end_role");

        // ******** CORRECT PARAMETER ORDER ********
        _mockAuth0.Setup(a =>
            a.CreateUserAndAssignRoleAsync(
                It.IsAny<string>(), // email
                It.IsAny<string>(), // password
                It.IsAny<string>(), // username
                It.IsAny<string>()  // roleId
            ))
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

    // -----------------------------------------------
    // BUSINESS USER TESTS
    // -----------------------------------------------

    [Test]
    public async Task CreateSubBusinessUser_ShouldReturnResponse_WhenSuccessful()
    {
        var businessId = Guid.NewGuid();

        var dto = new CreateSubBusinessUserDto(
            businessId,
            "john_rep",
            "john@business.com",
            "1234567890",
            "1234567890",
            "123 Business St",
            "Main Branch",
            "456 Branch Ave"
        );

        _mockBusinessServiceClient.Setup(c => c.BusinessExistsAsync(businessId)).ReturnsAsync(true);
        
        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        var fakeUser = new User("john_rep", "john@business.com", "1234567890", "123456","business_user", "123 Business St", "auth0|dummy-id");

        _mockUserRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(fakeUser);

        _mockBusinessRepRepository.Setup(r => r.AddAsync(It.IsAny<BusinessRep>()))
            .Returns(Task.CompletedTask);

        _mockBusinessRepRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new BusinessRep(businessId, fakeUser.Id, "Main Branch", "456 Branch Ave"));

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
        var dto = new CreateSubBusinessUserDto(Guid.NewGuid(), "x", "x", "P","x", null, null, null);
        
        _mockBusinessServiceClient.Setup(c => c.BusinessExistsAsync(dto.BusinessId))
            .ReturnsAsync(false);

        Assert.ThrowsAsync<BusinessNotFoundException>(() => _service.CreateSubBusinessUserAsync(dto));
    }

    [Test]
    public void CreateSubBusinessUser_ShouldThrow_WhenUserSaveFails()
    {
        var dto = new CreateSubBusinessUserDto(Guid.NewGuid(), "u", "e", "p", "22",null, null, null);

        _mockBusinessServiceClient.Setup(c => c.BusinessExistsAsync(dto.BusinessId))
            .ReturnsAsync(true);

        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        _mockUserRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((User?)null);

        Assert.ThrowsAsync<UserCreationFailedException>(() => _service.CreateSubBusinessUserAsync(dto));
    }

    // -----------------------------------------------
    // SUPPORT USER TESTS
    // -----------------------------------------------

    [Test]
    public async Task CreateSupportUser_ShouldReturnResponse_WhenSuccessful()
    {
        var dto = new CreateSupportUserDto(
            "support",
            "support@test.com",
            "111",
            "123456",
            "addr"
        );

        var fakeUser = new User("support", "support@test.com", "111", "23456", "support_user","addr", "auth0|dummy-id");

        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockUserRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(fakeUser);

        _mockSupportUserProfileRepository.Setup(r => r.AddAsync(It.IsAny<SupportUserProfile>())).Returns(Task.CompletedTask);
        _mockSupportUserProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new SupportUserProfile(fakeUser.Id));

        var result = await _service.CreateSupportUserAsync(dto);

        Assert.That(result.Email, Is.EqualTo("support@test.com"));
    }

    // -----------------------------------------------
    // END USER TESTS
    // -----------------------------------------------

    [Test]
    public async Task CreateEndUser_ShouldReturnResponse_WhenSuccessful()
    {
        var dto = new CreateEndUserDto(
            "jane",
            "jane@test.com",
            "123",
            "123456",
            "addr",
            "instagram.com/jane"
        );

        var user = new User("jane", "jane@test.com", "123", "123456","end_user", "addr", "auth0|dummy-id");
        var profile = new EndUserProfile(user.Id, "instagram.com/jane");

        _mockUserRepository.Setup(r => r.EmailExistsAsync(dto.Email)).ReturnsAsync(false);

        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockUserRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(user);

        _mockEndUserProfileRepository.Setup(r => r.AddAsync(It.IsAny<EndUserProfile>())).Returns(Task.CompletedTask);
        _mockEndUserProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(profile);

        var result = await _service.CreateEndUserAsync(dto);

        Assert.That(result.Email, Is.EqualTo("jane@test.com"));
    }
}
