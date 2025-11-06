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
    
    // NEW BUSINESS REGISTRATION TESTS
    [Test]
    public async Task RegisterBusinessAccountAsync_ShouldReturnTuple_WhenSuccessful()
    {
        // ARRANGE
        var dto = new BusinessUserDto
        (
            Name: "TechNova",
            Email: "info@technova.com",
            Phone: "5551234567",
            UserType: "business_user",
            Address: "123 Innovation Blvd",
            BranchName: "HQ",
            BranchAddress: "123 Innovation Blvd",
            Website: "https://technova.com",
            CategoryIds: new List<string>() { "software", "cloud" }
        );

        var businessId = Guid.NewGuid();
        var user = new User(dto.Name, dto.Email, dto.Phone, dto.UserType, dto.Address);
        var businessRep = new BusinessRep(businessId, user.Id, dto.BranchName, dto.BranchAddress);

        _mockBusinessServiceClient
            .Setup(c => c.CreateBusinessAsync(dto))
            .ReturnsAsync(businessId);

        _mockUserRepository
            .Setup(r => r.AddAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(user);

        _mockBusinessRepRepository
            .Setup(r => r.AddAsync(It.IsAny<BusinessRep>()))
            .Returns(Task.CompletedTask);

        _mockBusinessRepRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(businessRep);

        // ACT
        var (createdUser, createdBusinessId, createdRep) = await _service.RegisterBusinessAccountAsync(dto);

        // ASSERT
        Assert.Multiple(() =>
        {
            Assert.That(createdUser.Username, Is.EqualTo(dto.Name));
            Assert.That(createdUser.Email, Is.EqualTo(dto.Email));
            Assert.That(createdBusinessId, Is.EqualTo(businessId));
            Assert.That(createdRep.BusinessId, Is.EqualTo(businessId));
            Assert.That(createdRep.BranchName, Is.EqualTo("HQ"));
        });

        _mockBusinessServiceClient.Verify(c => c.CreateBusinessAsync(dto), Times.Once);
        _mockUserRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
        _mockBusinessRepRepository.Verify(r => r.AddAsync(It.IsAny<BusinessRep>()), Times.Once);
    }

    [Test]
    public void RegisterBusinessAccountAsync_ShouldThrow_WhenBusinessCreationFails()
    {
        // ARRANGE
        var dto = new BusinessUserDto(
            Name: "FailCo",
            Email: "fail@co.com",
            Phone: "0000000000",
            UserType: "business_user",
            Address: "Nowhere",
            BranchName: "Fail Branch",
            BranchAddress: "Nowhere",
            Website: "https://fail.com",
            CategoryIds: new List<string>() { "fail" }
        );

        _mockBusinessServiceClient
            .Setup(c => c.CreateBusinessAsync(dto))
            .ReturnsAsync((Guid?)null);

        // ACT & ASSERT
        var ex = Assert.ThrowsAsync<BusinessUserCreationFailedException>(
            async () => await _service.RegisterBusinessAccountAsync(dto)
        );

        Assert.That(ex!.Message, Does.Contain("Business creation failed"));
    }

    [Test]
    public void RegisterBusinessAccountAsync_ShouldThrow_WhenUserSaveFails()
    {
        // ARRANGE
        var dto = new BusinessUserDto(
            Name: "NoUser",
            Email: "nouser@co.com",
            Phone: "1111111111",
            UserType: "business_user",
            Address: "No Address",
            BranchName: "No Branch",
            BranchAddress: "No Addr",
            Website: "https://nouser.com",
            CategoryIds: new List<string>() { "none" }
        );

        var businessId = Guid.NewGuid();

        _mockBusinessServiceClient
            .Setup(c => c.CreateBusinessAsync(dto))
            .ReturnsAsync(businessId);

        _mockUserRepository
            .Setup(r => r.AddAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((User?)null);

        // ACT & ASSERT
        var ex = Assert.ThrowsAsync<UserCreationFailedException>(
            async () => await _service.RegisterBusinessAccountAsync(dto)
        );

        Assert.That(ex!.Message, Does.Contain("Failed to create user record."));
    }

    [Test]
    public void RegisterBusinessAccountAsync_ShouldThrow_WhenBusinessRepSaveFails()
    {
        // ARRANGE
        var dto = new BusinessUserDto(
            Name: "BizRepFail",
            Email: "rep@fail.com",
            Phone: "1231231234",
            UserType: "business_user",
            Address: "123 Street",
            BranchName: "BranchFail",
            BranchAddress: "Branch Addr",
            Website: "https://failrep.com",
            CategoryIds: new List<string>() { "rep" }
        );

        var businessId = Guid.NewGuid();
        var user = new User(dto.Name, dto.Email, dto.Phone, dto.UserType, dto.Address);
        var businessRep = new BusinessRep(businessId, user.Id, dto.BranchName, dto.BranchAddress);

        _mockBusinessServiceClient
            .Setup(c => c.CreateBusinessAsync(dto))
            .ReturnsAsync(businessId);

        _mockUserRepository
            .Setup(r => r.AddAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(user);

        _mockBusinessRepRepository
            .Setup(r => r.AddAsync(It.IsAny<BusinessRep>()))
            .Returns(Task.CompletedTask);

        _mockBusinessRepRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((BusinessRep?)null);

        // ACT & ASSERT
        var ex = Assert.ThrowsAsync<BusinessUserCreationFailedException>(
            async () => await _service.RegisterBusinessAccountAsync(dto)
        );

        Assert.That(ex!.Message, Does.Contain("Failed to create business record."));
    }

    [Test]
    public async Task GetBusinessRepByIdAsync_ShouldReturnBusinessRep_WhenFound()
    {
        // ARRANGE
        var repId = Guid.NewGuid();
        var businessRep = new BusinessRep(Guid.NewGuid(), Guid.NewGuid(), "HQ", "123 Main St");

        _mockBusinessRepRepository
            .Setup(r => r.GetByIdAsync(repId))
            .ReturnsAsync(businessRep);

        // ACT
        var result = await _service.GetBusinessRepByIdAsync(repId);

        // ASSERT
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.BranchName, Is.EqualTo("HQ"));
        _mockBusinessRepRepository.Verify(r => r.GetByIdAsync(repId), Times.Once);
    }

    [Test]
    public async Task GetBusinessRepByIdAsync_ShouldReturnNull_WhenNotFound()
    {
        // ARRANGE
        var repId = Guid.NewGuid();
        _mockBusinessRepRepository
            .Setup(r => r.GetByIdAsync(repId))
            .ReturnsAsync((BusinessRep?)null);

        // ACT
        var result = await _service.GetBusinessRepByIdAsync(repId);

        // ASSERT
        Assert.That(result, Is.Null);
    }


    // NEW END USER TESTS
    [Test]
    public async Task CreateEndUser_ShouldReturnResponse_WhenSuccessful()
    {
        // ARRANGE
        var dto = new CreateEndUserDto(
            Username: "jane_doe",
            Email: "jane@example.com",
            Phone: "1234567890",
            Address: "123 Main Street",
            SocialMedia: "https://twitter.com/jane_doe"
        );

        var user = new User("jane_doe", "jane@example.com", "1234567890", "end_user", "123 Main Street");
        var endProfile = new EndUserProfile(user.Id, "https://twitter.com/jane_doe");

        _mockUserRepository.Setup(r => r.EmailExistsAsync(dto.Email)).ReturnsAsync(false);
        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockUserRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(user);

        _mockEndUserProfileRepository.Setup(r => r.AddAsync(It.IsAny<EndUserProfile>())).Returns(Task.CompletedTask);
        _mockEndUserProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(endProfile);

        // ACT
        var result = await _service.CreateEndUserAsync(dto);

        // ASSERT
        Assert.Multiple(() =>
        {
            Assert.That(result.Username, Is.EqualTo("jane_doe"));
            Assert.That(result.Email, Is.EqualTo("jane@example.com"));
            Assert.That(result.SocialMedia, Is.EqualTo("https://twitter.com/jane_doe"));
        });

        _mockUserRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
        _mockEndUserProfileRepository.Verify(r => r.AddAsync(It.IsAny<EndUserProfile>()), Times.Once);
    }

    [Test]
    public void CreateEndUser_ShouldThrow_WhenEmailAlreadyExists()
    {
        // ARRANGE
        var dto = new CreateEndUserDto(
            Username: "duplicate",
            Email: "duplicate@example.com",
            Phone: "5555555555",
            Address: null,
            SocialMedia: null
        );

        _mockUserRepository.Setup(r => r.EmailExistsAsync(dto.Email)).ReturnsAsync(true);

        // ACT & ASSERT
        var ex = Assert.ThrowsAsync<DuplicateUserEmailException>(
            async () => await _service.CreateEndUserAsync(dto)
        );

        Assert.That(ex!.Message, Does.Contain(dto.Email));
    }

    [Test]
    public void CreateEndUser_ShouldThrow_WhenUserSaveFails()
    {
        // ARRANGE
        var dto = new CreateEndUserDto(
            Username: "fail_user",
            Email: "fail@user.com",
            Phone: "0000000000",
            Address: null,
            SocialMedia: null
        );

        _mockUserRepository.Setup(r => r.EmailExistsAsync(dto.Email)).ReturnsAsync(false);
        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockUserRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        // ACT & ASSERT
        var ex = Assert.ThrowsAsync<UserCreationFailedException>(
            async () => await _service.CreateEndUserAsync(dto)
        );

        Assert.That(ex!.Message, Does.Contain("Failed to create user record."));
    }

    [Test]
    public void CreateEndUser_ShouldThrow_WhenProfileSaveFails()
    {
        // ARRANGE
        var dto = new CreateEndUserDto(
            Username: "profile_fail",
            Email: "profile@fail.com",
            Phone: "1231231234",
            Address: "Somewhere",
            SocialMedia: "https://fail.com"
        );

        var user = new User("profile_fail", "profile@fail.com", "1231231234", "end_user", "Somewhere");

        _mockUserRepository.Setup(r => r.EmailExistsAsync(dto.Email)).ReturnsAsync(false);
        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockUserRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(user);

        _mockEndUserProfileRepository.Setup(r => r.AddAsync(It.IsAny<EndUserProfile>())).Returns(Task.CompletedTask);
        _mockEndUserProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((EndUserProfile?)null);

        // ACT & ASSERT
        var ex = Assert.ThrowsAsync<UserCreationFailedException>(
            async () => await _service.CreateEndUserAsync(dto)
        );

        Assert.That(ex!.Message, Does.Contain("Failed to create end user profile."));
    }

    [Test]
    public async Task CreateEndUser_ShouldSetUserTypeToEndUser()
    {
        // ARRANGE
        var dto = new CreateEndUserDto(
            Username: "type_check",
            Email: "type@enduser.com",
            Phone: "1231231234",
            Address: "Type Street",
            SocialMedia: null
        );

        User? capturedUser = null;

        _mockUserRepository.Setup(r => r.EmailExistsAsync(dto.Email)).ReturnsAsync(false);
        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>()))
            .Callback<User>(u => capturedUser = u)
            .Returns(Task.CompletedTask);

        _mockUserRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(() => capturedUser);

        _mockEndUserProfileRepository.Setup(r => r.AddAsync(It.IsAny<EndUserProfile>())).Returns(Task.CompletedTask);
        _mockEndUserProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new EndUserProfile(Guid.NewGuid(), null));

        // ACT
        await _service.CreateEndUserAsync(dto);

        // ASSERT
        Assert.That(capturedUser, Is.Not.Null);
        Assert.That(capturedUser!.UserType, Is.EqualTo("end_user"));
    }

    [Test]
    public async Task CreateEndUser_WithNullSocialMedia_ShouldSucceed()
    {
        // ARRANGE
        var dto = new CreateEndUserDto(
            Username: "nosocial",
            Email: "nosocial@example.com",
            Phone: "5556667777",
            Address: "123 Nowhere",
            SocialMedia: null
        );

        var user = new User("nosocial", "nosocial@example.com", "5556667777", "end_user", "123 Nowhere");
        var profile = new EndUserProfile(user.Id, null);

        _mockUserRepository.Setup(r => r.EmailExistsAsync(dto.Email)).ReturnsAsync(false);
        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockUserRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(user);

        _mockEndUserProfileRepository.Setup(r => r.AddAsync(It.IsAny<EndUserProfile>())).Returns(Task.CompletedTask);
        _mockEndUserProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(profile);

        // ACT
        var result = await _service.CreateEndUserAsync(dto);

        // ASSERT
        Assert.Multiple(() =>
        {
            Assert.That(result.Username, Is.EqualTo("nosocial"));
            Assert.That(result.SocialMedia, Is.Null);
        });
    }

    // DELETE USER TEST
    [Test]
    public async Task DeleteUserAsync_ShouldDeleteEndUser_WhenExists()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var profile = new EndUserProfile(userId, null);

        _mockEndUserProfileRepository.Setup(r => r.GetByIdAsync(profileId))
            .ReturnsAsync(profile);

        // Act
        await _service.DeleteUserAsync(profileId, "end_user");

        // Assert
        _mockEndUserProfileRepository.Verify(r => r.DeleteAsync(profileId), Times.Once);
        _mockUserRepository.Verify(r => r.DeleteAsync(userId), Times.Once);
    }

    [Test]
    public async Task DeleteUserAsync_ShouldDeleteSupportUser_WhenExists()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var profile = new SupportUserProfile(userId);

        _mockSupportUserProfileRepository.Setup(r => r.GetByIdAsync(profileId))
            .ReturnsAsync(profile);

        // Act
        await _service.DeleteUserAsync(profileId, "support_user");

        // Assert
        _mockSupportUserProfileRepository.Verify(r => r.DeleteAsync(profileId), Times.Once);
        _mockUserRepository.Verify(r => r.DeleteAsync(userId), Times.Once);
    }
    
    [Test]
    public async Task DeleteUserAsync_ShouldDeleteBusinessUser_WhenExists()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var profile = new BusinessRep(profileId, userId, "branch address", "branch name");

       _mockBusinessRepRepository.Setup(r => r.GetByIdAsync(profileId))
            .ReturnsAsync(profile);

        // Act
        await _service.DeleteUserAsync(profileId, "business_user");

        // Assert
        _mockBusinessRepRepository.Verify(r => r.DeleteAsync(profileId), Times.Once);
        _mockUserRepository.Verify(r => r.DeleteAsync(userId), Times.Once);
    }

    [Test]
    public void DeleteUserAsync_ShouldThrow_WhenUserNotFound()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        _mockEndUserProfileRepository.Setup(r => r.GetByIdAsync(profileId))
            .ReturnsAsync((EndUserProfile?)null);

        // Act

        // Assert
        Assert.ThrowsAsync<UserNotFoundException>(
                    async () => await _service.DeleteUserAsync(profileId, "end_user")
                    );
        
        _mockUserRepository.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public void DeleteUserAsync_ShouldThrow_WhenTypeInvalid()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act

        // Assert
        Assert.ThrowsAsync<UserTypeNotFoundException>(
            async () => await _service.DeleteUserAsync(id, "invalid_type")
        );
    }

    [Test]
    public async Task DeleteUserAsync_ShouldNotDeleteUser_WhenUserIdIsEmpty()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var profile = new SupportUserProfile(Guid.Empty);

        _mockSupportUserProfileRepository.Setup(r => r.GetByIdAsync(profileId))
            .ReturnsAsync(profile);

        // Act
        await _service.DeleteUserAsync(profileId, "support_user");

        // Assert
        _mockSupportUserProfileRepository.Verify(r => r.DeleteAsync(profileId), Times.Once);
        _mockUserRepository.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }
}
