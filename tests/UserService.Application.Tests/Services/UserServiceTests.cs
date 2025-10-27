using Moq;
using UserService.Application.DTOs;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Domain.Repositories;

namespace UserService.Application.Tests.Services;

[TestFixture]
public class UserServiceTests
{
    private Mock<IUserRepository> _mockUserRepository = null!;
    private Mock<IBusinessRepRepository> _mockBusinessRepRepository = null!;
    private Mock<IBusinessServiceClient> _mockBusinessServiceClient = null!;
    private Mock<ISupportUserProfileRepository> _mockSupportUserProfileRepository = null!;
    private Mock<IEndUserProfileRepository> _mockEndUserProfileRepository = null!;
    private Application.Services.UserService _service = null!;

    [SetUp]
    public void Setup()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockBusinessRepRepository = new Mock<IBusinessRepRepository>();
        _mockBusinessServiceClient = new Mock<IBusinessServiceClient>();
        _mockSupportUserProfileRepository = new Mock<ISupportUserProfileRepository>();
        _mockEndUserProfileRepository = new Mock<IEndUserProfileRepository>();

        _service = new Application.Services.UserService(
            _mockUserRepository.Object,
            _mockBusinessRepRepository.Object,
            _mockBusinessServiceClient.Object,
            _mockSupportUserProfileRepository.Object,
        _mockEndUserProfileRepository.Object
        );
    }

 
    // EXISTING BUSINESS USER TESTS
    [Test]
    public async Task CreateSubBusinessUser_ShouldReturnResponse_WhenSuccessful()
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

        _mockBusinessServiceClient.Setup(c => c.BusinessExistsAsync(businessId)).ReturnsAsync(true);

        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockUserRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new User("john_rep", "john@business.com", "1234567890", "business_user", "123 Business St"));

        _mockBusinessRepRepository.Setup(r => r.AddAsync(It.IsAny<BusinessRep>())).Returns(Task.CompletedTask);
        _mockBusinessRepRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new BusinessRep(businessId, Guid.NewGuid(), "Main Branch", "456 Branch Ave"));

        // ACT
        var result = await _service.CreateSubBusinessUserAsync(dto);

        // ASSERT
        Assert.Multiple(() =>
        {
            Assert.That(result.Username, Is.EqualTo("john_rep"));
            Assert.That(result.Email, Is.EqualTo("john@business.com"));
            Assert.That(result.BusinessId, Is.EqualTo(businessId));
            Assert.That(result.BranchName, Is.EqualTo("Main Branch"));
        });

        _mockBusinessServiceClient.Verify(c => c.BusinessExistsAsync(businessId), Times.Once);
        _mockUserRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
        _mockBusinessRepRepository.Verify(r => r.AddAsync(It.IsAny<BusinessRep>()), Times.Once);
    }

    [Test]
    public void CreateSubBusinessUser_ShouldThrow_WhenBusinessDoesNotExist()
    {
        // ARRANGE
        var businessId = Guid.NewGuid();
        var dto = new CreateSubBusinessUserDto(
            BusinessId: businessId,
            Username: "nonexistent_rep",
            Email: "no@biz.com",
            Phone: "0000000000",
            Address: null,
            BranchName: null,
            BranchAddress: null
        );

        _mockBusinessServiceClient.Setup(c => c.BusinessExistsAsync(businessId)).ReturnsAsync(false);

        // ACT & ASSERT
        var ex = Assert.ThrowsAsync<BusinessNotFoundException>(
            async () => await _service.CreateSubBusinessUserAsync(dto)
        );

        Assert.That(ex!.Message, Does.Contain(businessId.ToString()));
        _mockUserRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Test]
    public void CreateSubBusinessUser_ShouldThrow_WhenUserSaveFails()
    {
        // ARRANGE
        var businessId = Guid.NewGuid();
        var dto = new CreateSubBusinessUserDto(
            BusinessId: businessId,
            Username: "failed_user",
            Email: "fail@user.com",
            Phone: "1231231234",
            Address: "Somewhere",
            BranchName: "Branch A",
            BranchAddress: "Address A"
        );

        _mockBusinessServiceClient.Setup(c => c.BusinessExistsAsync(businessId)).ReturnsAsync(true);
        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockUserRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        // ACT & ASSERT
        var ex = Assert.ThrowsAsync<UserCreationFailedException>(
            async () => await _service.CreateSubBusinessUserAsync(dto)
        );

        Assert.That(ex!.Message, Does.Contain("Failed to create user record."));
    }

    [Test]
    public void CreateSubBusinessUser_ShouldThrow_WhenBusinessRepSaveFails()
    {
        // ARRANGE
        var businessId = Guid.NewGuid();
        var dto = new CreateSubBusinessUserDto(
            BusinessId: businessId,
            Username: "rep_fail",
            Email: "rep@fail.com",
            Phone: "9999999999",
            Address: null,
            BranchName: null,
            BranchAddress: null
        );

        _mockBusinessServiceClient.Setup(c => c.BusinessExistsAsync(businessId)).ReturnsAsync(true);
        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockUserRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new User("rep_fail", "rep@fail.com", "9999999999", "business_user", null));
        _mockBusinessRepRepository.Setup(r => r.AddAsync(It.IsAny<BusinessRep>())).Returns(Task.CompletedTask);
        _mockBusinessRepRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((BusinessRep?)null);

        // ACT & ASSERT
        var ex = Assert.ThrowsAsync<UserCreationFailedException>(
            async () => await _service.CreateSubBusinessUserAsync(dto)
        );

        Assert.That(ex!.Message, Does.Contain("Failed to create business representative relationship."));
    }

    [Test]
    public async Task CreateSubBusinessUser_ShouldSetUserTypeToBusinessUser()
    {
        // ARRANGE
        var businessId = Guid.NewGuid();
        var dto = new CreateSubBusinessUserDto(
            BusinessId: businessId,
            Username: "type_test",
            Email: "type@test.com",
            Phone: "4444444444",
            Address: null,
            BranchName: null,
            BranchAddress: null
        );

        User? capturedUser = null;

        _mockBusinessServiceClient.Setup(c => c.BusinessExistsAsync(businessId)).ReturnsAsync(true);
        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>()))
            .Callback<User>(u => capturedUser = u)
            .Returns(Task.CompletedTask);
        _mockUserRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(() => capturedUser);
        _mockBusinessRepRepository.Setup(r => r.AddAsync(It.IsAny<BusinessRep>())).Returns(Task.CompletedTask);
        _mockBusinessRepRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new BusinessRep(businessId, Guid.NewGuid()));

        // ACT
        await _service.CreateSubBusinessUserAsync(dto);

        // ASSERT
        Assert.That(capturedUser, Is.Not.Null);
        Assert.That(capturedUser!.UserType, Is.EqualTo("business_user"));
    }

    [Test]
    public async Task CreateSubBusinessUser_WithOptionalNulls_ShouldSucceed()
    {
        // ARRANGE
        var businessId = Guid.NewGuid();
        var dto = new CreateSubBusinessUserDto(
            BusinessId: businessId,
            Username: "optional_rep",
            Email: "optional@biz.com",
            Phone: "7777777777",
            Address: null,
            BranchName: null,
            BranchAddress: null
        );

        _mockBusinessServiceClient.Setup(c => c.BusinessExistsAsync(businessId)).ReturnsAsync(true);
        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockUserRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new User("optional_rep", "optional@biz.com", "7777777777", "business_user", null));
        _mockBusinessRepRepository.Setup(r => r.AddAsync(It.IsAny<BusinessRep>())).Returns(Task.CompletedTask);
        _mockBusinessRepRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new BusinessRep(businessId, Guid.NewGuid()));

        // ACT
        var result = await _service.CreateSubBusinessUserAsync(dto);

        // ASSERT
        Assert.Multiple(() =>
        {
            Assert.That(result.Username, Is.EqualTo("optional_rep"));
            Assert.That(result.Address, Is.Null);
            Assert.That(result.BranchName, Is.Null);
        });
    }



    // NEW SUPPORT USER TESTS
    [Test]
    public async Task CreateSupportUser_ShouldReturnResponse_WhenSuccessful()
    {
        // ARRANGE
        var dto = new CreateSupportUserDto(
            Username: "support_admin",
            Email: "admin@support.com",
            Phone: "1234567890",
            Address: "123 Support St"
        );

        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockUserRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new User("support_admin", "admin@support.com", "1234567890", "support_user", "123 Support St"));

        _mockSupportUserProfileRepository.Setup(r => r.AddAsync(It.IsAny<SupportUserProfile>())).Returns(Task.CompletedTask);
        _mockSupportUserProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new SupportUserProfile(Guid.NewGuid()));

        // ACT
        var result = await _service.CreateSupportUserAsync(dto);

        // ASSERT
        Assert.Multiple(() =>
        {
            Assert.That(result.Username, Is.EqualTo("support_admin"));
            Assert.That(result.Email, Is.EqualTo("admin@support.com"));
            Assert.That(result.Phone, Is.EqualTo("1234567890"));
            Assert.That(result.Address, Is.EqualTo("123 Support St"));
        });

        _mockUserRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
        _mockSupportUserProfileRepository.Verify(r => r.AddAsync(It.IsAny<SupportUserProfile>()), Times.Once);
    }

    [Test]
    public void CreateSupportUser_ShouldThrow_WhenUserSaveFails()
    {
        // ARRANGE
        var dto = new CreateSupportUserDto(
            Username: "failed_support",
            Email: "fail@support.com",
            Phone: "9999999999",
            Address: null
        );

        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockUserRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        // ACT & ASSERT
        var ex = Assert.ThrowsAsync<UserCreationFailedException>(
            async () => await _service.CreateSupportUserAsync(dto)
        );

        Assert.That(ex!.Message, Does.Contain("Failed to create user record."));
    }

    [Test]
    public void CreateSupportUser_ShouldThrow_WhenSupportProfileSaveFails()
    {
        // ARRANGE
        var dto = new CreateSupportUserDto(
            Username: "profile_fail",
            Email: "profile@fail.com",
            Phone: "8888888888",
            Address: "Fail Address"
        );

        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockUserRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new User("profile_fail", "profile@fail.com", "8888888888", "support_user", "Fail Address"));

        _mockSupportUserProfileRepository.Setup(r => r.AddAsync(It.IsAny<SupportUserProfile>())).Returns(Task.CompletedTask);
        _mockSupportUserProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((SupportUserProfile?)null);

        // ACT & ASSERT
        var ex = Assert.ThrowsAsync<UserCreationFailedException>(
            async () => await _service.CreateSupportUserAsync(dto)
        );

        Assert.That(ex!.Message, Does.Contain("Failed to create support user profile."));
    }

    [Test]
    public async Task CreateSupportUser_ShouldSetUserTypeToSupportUser()
    {
        // ARRANGE
        var dto = new CreateSupportUserDto(
            Username: "type_check",
            Email: "type@check.com",
            Phone: "7777777777",
            Address: "Type St"
        );

        User? capturedUser = null;

        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>()))
            .Callback<User>(u => capturedUser = u)
            .Returns(Task.CompletedTask);
        _mockUserRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(() => capturedUser);

        _mockSupportUserProfileRepository.Setup(r => r.AddAsync(It.IsAny<SupportUserProfile>())).Returns(Task.CompletedTask);
        _mockSupportUserProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new SupportUserProfile(Guid.NewGuid()));

        // ACT
        await _service.CreateSupportUserAsync(dto);

        // ASSERT
        Assert.That(capturedUser, Is.Not.Null);
        Assert.That(capturedUser!.UserType, Is.EqualTo("support_user"));
    }

    [Test]
    public async Task CreateSupportUser_WithNullAddress_ShouldSucceed()
    {
        // ARRANGE
        var dto = new CreateSupportUserDto(
            Username: "no_address",
            Email: "noaddr@support.com",
            Phone: "6666666666",
            Address: null
        );

        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockUserRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new User("no_address", "noaddr@support.com", "6666666666", "support_user", null));

        _mockSupportUserProfileRepository.Setup(r => r.AddAsync(It.IsAny<SupportUserProfile>())).Returns(Task.CompletedTask);
        _mockSupportUserProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new SupportUserProfile(Guid.NewGuid()));

        // ACT
        var result = await _service.CreateSupportUserAsync(dto);

        // ASSERT
        Assert.Multiple(() =>
        {
            Assert.That(result.Username, Is.EqualTo("no_address"));
            Assert.That(result.Address, Is.Null);
        });
    }




    // ---------------------- UPDATE SUPPORT USER TESTS ----------------------
    [Test]
    public async Task UpdateSupportUser_ShouldReturnResponse_WhenSuccessful()
    {
        // ARRANGE
        var existingUser = new User("support_user", "old@support.com", "1234567890", "support_user", "123 Old St");
        var userId = existingUser.Id; // Use the User's generated Id

        var dto = new UpdateSupportUserDto(
            Email: "updated@support.com",
            Phone: "9876543210",
            Address: "456 Updated St"
        );

        var supportProfile = new SupportUserProfile(userId);

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(existingUser);
        _mockSupportUserProfileRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(supportProfile);
        _mockUserRepository.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockSupportUserProfileRepository.Setup(r => r.UpdateAsync(It.IsAny<SupportUserProfile>())).Returns(Task.CompletedTask);

        // After update, return the updated user
        _mockUserRepository.SetupSequence(r => r.GetByIdAsync(userId))
            .ReturnsAsync(existingUser)
            .ReturnsAsync(() =>
            {
                // Use the domain method to update fields
                existingUser.Update(
                    email: "updated@support.com",
                    phone: "9876543210",
                    address: "456 Updated St"
                );
                return existingUser;
            });


        // ACT
        var result = await _service.UpdateSupportUserAsync(userId, dto);

        // ASSERT
        Assert.Multiple(() =>
        {
            Assert.That(result.Email, Is.EqualTo("updated@support.com"));
            Assert.That(result.Phone, Is.EqualTo("9876543210"));
            Assert.That(result.Address, Is.EqualTo("456 Updated St"));
            Assert.That(result.UserId, Is.EqualTo(userId));
        });

        _mockUserRepository.Verify(r => r.GetByIdAsync(userId), Times.AtLeastOnce);
        _mockUserRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once);
        _mockSupportUserProfileRepository.Verify(r => r.UpdateAsync(It.IsAny<SupportUserProfile>()), Times.Once);
    }

    [Test]
    public void UpdateSupportUser_ShouldThrow_WhenUserNotFound()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var dto = new UpdateSupportUserDto(
            Email: "test@support.com",
            Phone: "1234567890",
            Address: null
        );

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // ACT & ASSERT
        var ex = Assert.ThrowsAsync<SupportUserNotFoundException>(
            async () => await _service.UpdateSupportUserAsync(userId, dto)
        );

        Assert.That(ex!.Message, Does.Contain(userId.ToString()));
        _mockUserRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Test]
    public void UpdateSupportUser_ShouldThrow_WhenUserIsNotSupportUser()
    {
        // ARRANGE
        var endUser = new User("end_user", "end@user.com", "1234567890", "end_user", "123 St");
        var userId = endUser.Id; // Use the User's generated Id

        var dto = new UpdateSupportUserDto(
            Email: "test@support.com",
            Phone: "1234567890",
            Address: null
        );

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(endUser);

        // ACT & ASSERT
        var ex = Assert.ThrowsAsync<SupportUserUpdateFailedException>(
            async () => await _service.UpdateSupportUserAsync(userId, dto)
        );

        Assert.That(ex!.Message, Does.Contain("is not a support user"));
        _mockUserRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Test]
    public void UpdateSupportUser_ShouldThrow_WhenSupportProfileNotFound()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var dto = new UpdateSupportUserDto(
            Email: "test@support.com",
            Phone: "1234567890",
            Address: null
        );

        var supportUser = new User("support_user", "support@user.com", "1234567890", "support_user", "123 St");
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(supportUser);
        _mockSupportUserProfileRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((SupportUserProfile?)null);

        // ACT & ASSERT
        var ex = Assert.ThrowsAsync<SupportUserNotFoundException>(
            async () => await _service.UpdateSupportUserAsync(userId, dto)
        );

        Assert.That(ex!.Message, Does.Contain(userId.ToString()));
    }

    [Test]
    public void UpdateSupportUser_ShouldThrow_WhenUpdateVerificationFails()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var dto = new UpdateSupportUserDto(
            Email: "updated@support.com",
            Phone: "9876543210",
            Address: "456 Updated St"
        );

        var existingUser = new User("support_user", "old@support.com", "1234567890", "support_user", "123 Old St");
        var supportProfile = new SupportUserProfile(userId);

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(existingUser);
        _mockSupportUserProfileRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(supportProfile);
        _mockUserRepository.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockSupportUserProfileRepository.Setup(r => r.UpdateAsync(It.IsAny<SupportUserProfile>())).Returns(Task.CompletedTask);

        // Simulate verification failure - second GetByIdAsync returns null
        _mockUserRepository.SetupSequence(r => r.GetByIdAsync(userId))
            .ReturnsAsync(existingUser)
            .ReturnsAsync((User?)null);

        // ACT & ASSERT
        var ex = Assert.ThrowsAsync<SupportUserUpdateFailedException>(
            async () => await _service.UpdateSupportUserAsync(userId, dto)
        );

        Assert.That(ex!.Message, Does.Contain("Failed to update user record"));
    }

    [Test]
    public async Task UpdateSupportUser_WithPartialUpdate_ShouldOnlyUpdateProvidedFields()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var dto = new UpdateSupportUserDto(
            Email: "newemail@support.com",
            Phone: null, // Don't update phone
            Address: null // Don't update address
        );

        var existingUser = new User("support_user", "old@support.com", "1234567890", "support_user", "123 Old St");
        var supportProfile = new SupportUserProfile(userId);

        User? capturedUser = null;

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(existingUser);
        _mockSupportUserProfileRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(supportProfile);

        _mockUserRepository.Setup(r => r.UpdateAsync(It.IsAny<User>()))
            .Callback<User>(u => capturedUser = u)
            .Returns(Task.CompletedTask);

        _mockSupportUserProfileRepository.Setup(r => r.UpdateAsync(It.IsAny<SupportUserProfile>())).Returns(Task.CompletedTask);

        // Return the captured user (which has been modified by the Update method)
        _mockUserRepository.SetupSequence(r => r.GetByIdAsync(userId))
            .ReturnsAsync(existingUser)
            .ReturnsAsync(() => capturedUser ?? existingUser);

        // ACT
        var result = await _service.UpdateSupportUserAsync(userId, dto);

        // ASSERT
        Assert.That(capturedUser, Is.Not.Null);
        Assert.That(capturedUser!.Email, Is.EqualTo("newemail@support.com"));
        // Phone and Address should remain unchanged because Update method only updates non-null values
        Assert.That(result.Email, Is.EqualTo("newemail@support.com"));
    }

    [Test]
    public async Task UpdateSupportUser_ShouldTouchSupportProfileTimestamp()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var dto = new UpdateSupportUserDto(
            Email: "updated@support.com",
            Phone: "9876543210",
            Address: "456 Updated St"
        );

        var existingUser = new User("support_user", "old@support.com", "1234567890", "support_user", "123 Old St");
        var supportProfile = new SupportUserProfile(userId);

        SupportUserProfile? capturedProfile = null;

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(existingUser);
        _mockSupportUserProfileRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(supportProfile);
        _mockUserRepository.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        _mockSupportUserProfileRepository.Setup(r => r.UpdateAsync(It.IsAny<SupportUserProfile>()))
            .Callback<SupportUserProfile>(sp => capturedProfile = sp)
            .Returns(Task.CompletedTask);

        _mockUserRepository.SetupSequence(r => r.GetByIdAsync(userId))
            .ReturnsAsync(existingUser)
            .ReturnsAsync(new User("support_user", "updated@support.com", "9876543210", "support_user", "456 Updated St"));

        // ACT
        await _service.UpdateSupportUserAsync(userId, dto);

        // ASSERT
        Assert.That(capturedProfile, Is.Not.Null);
        _mockSupportUserProfileRepository.Verify(r => r.UpdateAsync(It.IsAny<SupportUserProfile>()), Times.Once);
    }

    [Test]
    public async Task UpdateSupportUser_WithAllNullFields_ShouldStillUpdateTimestamps()
    {
        // ARRANGE
        var existingUser = new User("support_user", "old@support.com", "1234567890", "support_user", "123 Old St");
        var userId = existingUser.Id; // Use the User's generated Id

        var dto = new UpdateSupportUserDto(
            Email: null,
            Phone: null,
            Address: null
        );

        var supportProfile = new SupportUserProfile(userId);

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(existingUser);
        _mockSupportUserProfileRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(supportProfile);
        _mockUserRepository.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockSupportUserProfileRepository.Setup(r => r.UpdateAsync(It.IsAny<SupportUserProfile>())).Returns(Task.CompletedTask);

        _mockUserRepository.SetupSequence(r => r.GetByIdAsync(userId))
            .ReturnsAsync(existingUser)
            .ReturnsAsync(existingUser);

        // ACT
        var result = await _service.UpdateSupportUserAsync(userId, dto);

        // ASSERT
        // Even with all null fields, the update methods should still be called (for timestamp updates)
        Assert.Multiple(() =>
        {
            Assert.That(result.Email, Is.EqualTo("old@support.com"));
            Assert.That(result.Phone, Is.EqualTo("1234567890"));
            Assert.That(result.Address, Is.EqualTo("123 Old St"));
            Assert.That(result.UserId, Is.EqualTo(userId));
        });

        _mockUserRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once);
        _mockSupportUserProfileRepository.Verify(r => r.UpdateAsync(It.IsAny<SupportUserProfile>()), Times.Once);
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


}