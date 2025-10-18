using Moq;
using UserService.Application.DTOs;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Application.Tests.Services
{
    /// <summary>
    /// Unit tests for sub-business user creation functionality
    /// Uses Moq to mock (fake) the database repositories
    /// </summary>
    [TestFixture]
    public class SubBusinessUserTests
    {
        private Mock<IUserRepository> _mockUserRepository = null!;
        private Mock<IBusinessRepRepository> _mockBusinessRepRepository = null!;
        private Application.Services.UserService _service = null!;

        [SetUp]
        public void Setup()
        {
            // Create mock repositories
            _mockUserRepository = new Mock<IUserRepository>();
            _mockBusinessRepRepository = new Mock<IBusinessRepRepository>();
            
            // Create service with mocked dependencies
            _service = new Application.Services.UserService(
                _mockUserRepository.Object,
                _mockBusinessRepRepository.Object
            );
        }

        [Test]
        public async Task CreateSubBusinessUser_ShouldCreateUserAndBusinessRep()
        {
            // ARRANGE - Set up test data
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

            // Mock business exists check
            _mockBusinessRepRepository
                .Setup(r => r.CheckBusinessExistsInDatabase(businessId))
                .ReturnsAsync(true);

            // Mock the repository methods
            _mockUserRepository
                .Setup(r => r.AddAsync(It.IsAny<User>()))
                .Returns(Task.CompletedTask);

            _mockUserRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid id) => new User("john_rep", "john@business.com", "1234567890", "business_user", "123 Business St"));

            _mockBusinessRepRepository
                .Setup(r => r.AddAsync(It.IsAny<BusinessRep>()))
                .Returns(Task.CompletedTask);

            _mockBusinessRepRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid id) => new BusinessRep(businessId, Guid.NewGuid(), "Main Branch", "456 Branch Ave"));

            // ACT - Execute the method we're testing
            var result = await _service.CreateSubBusinessUserAsync(dto);

            // ASSERT - Verify the results
            Assert.That(result.Username, Is.EqualTo("john_rep"));
            Assert.That(result.Email, Is.EqualTo("john@business.com"));
            Assert.That(result.Phone, Is.EqualTo("1234567890"));
            Assert.That(result.Address, Is.EqualTo("123 Business St"));
            Assert.That(result.BusinessId, Is.EqualTo(businessId));
            Assert.That(result.BranchName, Is.EqualTo("Main Branch"));
            Assert.That(result.BranchAddress, Is.EqualTo("456 Branch Ave"));
            
            // Verify that AddAsync was called exactly once for each repository
            _mockUserRepository.Verify(
                r => r.AddAsync(It.Is<User>(u => 
                    u.Username == "john_rep" && 
                    u.UserType == "business_user"
                )), 
                Times.Once
            );

            _mockBusinessRepRepository.Verify(
                r => r.AddAsync(It.Is<BusinessRep>(br => 
                    br.BusinessId == businessId &&
                    br.BranchName == "Main Branch"
                )),
                Times.Once
            );

            // Verify business existence was checked
            _mockBusinessRepRepository.Verify(
                r => r.CheckBusinessExistsInDatabase(businessId),
                Times.Once
            );
        }

        [Test]
        public void CreateSubBusinessUser_WithNonExistentBusiness_ShouldThrowException()
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

            // Mock business does NOT exist
            _mockBusinessRepRepository
                .Setup(r => r.CheckBusinessExistsInDatabase(businessId))
                .ReturnsAsync(false);

            // ACT & ASSERT
            var ex = Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _service.CreateSubBusinessUserAsync(dto)
            );
            
            Assert.That(ex!.Message, Does.Contain("Business with ID"));
            Assert.That(ex.Message, Does.Contain("does not exist"));

            // Verify that user was NOT created since business doesn't exist
            _mockUserRepository.Verify(
                r => r.AddAsync(It.IsAny<User>()),
                Times.Never
            );
        }

        [Test]
        public async Task CreateSubBusinessUser_ShouldSetUserTypeToBusinessUser()
        {
            // ARRANGE
            var businessId = Guid.NewGuid();
            var dto = new CreateSubBusinessUserDto(
                BusinessId: businessId,
                Username: "jane_rep",
                Email: "jane@business.com",
                Phone: "9876543210",
                Address: null,
                BranchName: null,
                BranchAddress: null
            );

            User? capturedUser = null;

            // Mock business exists
            _mockBusinessRepRepository
                .Setup(r => r.CheckBusinessExistsInDatabase(businessId))
                .ReturnsAsync(true);

            _mockUserRepository
                .Setup(r => r.AddAsync(It.IsAny<User>()))
                .Callback<User>(user => capturedUser = user)
                .Returns(Task.CompletedTask);

            _mockUserRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid id) => capturedUser);

            _mockBusinessRepRepository
                .Setup(r => r.AddAsync(It.IsAny<BusinessRep>()))
                .Returns(Task.CompletedTask);

            _mockBusinessRepRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid id) => new BusinessRep(businessId, Guid.NewGuid()));

            // ACT
            await _service.CreateSubBusinessUserAsync(dto);

            // ASSERT
            Assert.That(capturedUser, Is.Not.Null);
            Assert.That(capturedUser!.UserType, Is.EqualTo("business_user"));
        }

        [Test]
        public async Task CreateSubBusinessUser_WithNullOptionalFields_ShouldSucceed()
        {
            // ARRANGE
            var businessId = Guid.NewGuid();
            var dto = new CreateSubBusinessUserDto(
                BusinessId: businessId,
                Username: "minimal_rep",
                Email: "minimal@business.com",
                Phone: "5555555555",
                Address: null,
                BranchName: null,
                BranchAddress: null
            );

            // Mock business exists
            _mockBusinessRepRepository
                .Setup(r => r.CheckBusinessExistsInDatabase(businessId))
                .ReturnsAsync(true);

            _mockUserRepository
                .Setup(r => r.AddAsync(It.IsAny<User>()))
                .Returns(Task.CompletedTask);

            _mockUserRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid id) => new User("minimal_rep", "minimal@business.com", "5555555555", "business_user", null));

            _mockBusinessRepRepository
                .Setup(r => r.AddAsync(It.IsAny<BusinessRep>()))
                .Returns(Task.CompletedTask);

            _mockBusinessRepRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid id) => new BusinessRep(businessId, Guid.NewGuid()));

            // ACT
            var result = await _service.CreateSubBusinessUserAsync(dto);

            // ASSERT
            Assert.That(result.Address, Is.Null);
            Assert.That(result.BranchName, Is.Null);
            Assert.That(result.BranchAddress, Is.Null);
            Assert.That(result.Username, Is.EqualTo("minimal_rep"));
        }

        [Test]
        public void CreateSubBusinessUser_WhenUserSaveFails_ShouldThrowException()
        {
            // ARRANGE
            var businessId = Guid.NewGuid();
            var dto = new CreateSubBusinessUserDto(
                BusinessId: businessId,
                Username: "failed_user",
                Email: "failed@business.com",
                Phone: "1111111111",
                Address: null,
                BranchName: null,
                BranchAddress: null
            );

            // Mock business exists
            _mockBusinessRepRepository
                .Setup(r => r.CheckBusinessExistsInDatabase(businessId))
                .ReturnsAsync(true);

            // Mock user added successfully
            _mockUserRepository
                .Setup(r => r.AddAsync(It.IsAny<User>()))
                .Returns(Task.CompletedTask);

            // But GetById returns null (save failed)
            _mockUserRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((User?)null);

            // ACT & ASSERT
            var ex = Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _service.CreateSubBusinessUserAsync(dto)
            );
            
            Assert.That(ex!.Message, Does.Contain("Failed to create user"));
        }

        [Test]
        public void CreateSubBusinessUser_WhenBusinessRepSaveFails_ShouldThrowException()
        {
            // ARRANGE
            var businessId = Guid.NewGuid();
            var dto = new CreateSubBusinessUserDto(
                BusinessId: businessId,
                Username: "failed_rep",
                Email: "failed@business.com",
                Phone: "2222222222",
                Address: null,
                BranchName: null,
                BranchAddress: null
            );

            // Mock business exists
            _mockBusinessRepRepository
                .Setup(r => r.CheckBusinessExistsInDatabase(businessId))
                .ReturnsAsync(true);

            // Mock user saved successfully
            _mockUserRepository
                .Setup(r => r.AddAsync(It.IsAny<User>()))
                .Returns(Task.CompletedTask);

            _mockUserRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new User("failed_rep", "failed@business.com", "2222222222", "business_user", null));

            // Mock business rep added
            _mockBusinessRepRepository
                .Setup(r => r.AddAsync(It.IsAny<BusinessRep>()))
                .Returns(Task.CompletedTask);

            // But GetById returns null (save failed)
            _mockBusinessRepRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((BusinessRep?)null);

            // ACT & ASSERT
            var ex = Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _service.CreateSubBusinessUserAsync(dto)
            );
            
            Assert.That(ex!.Message, Does.Contain("Failed to create business representative relationship"));
        }
    }
}