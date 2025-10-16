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
            // Create mock (fake) repositories
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

            // Mock the repository methods to do nothing (they won't actually hit the database)
            _mockUserRepository
                .Setup(r => r.AddAsync(It.IsAny<User>()))
                .Returns(Task.CompletedTask);

            _mockBusinessRepRepository
                .Setup(r => r.AddAsync(It.IsAny<BusinessRep>()))
                .Returns(Task.CompletedTask);

            // ACT - Execute the method we're testing
            var result = await _service.CreateSubBusinessUserAsync(dto);

            // ASSERT - Verify the results
            
            // Check that user was created correctly
            Assert.That(result.Username, Is.EqualTo("john_rep"));
            Assert.That(result.Email, Is.EqualTo("john@business.com"));
            Assert.That(result.Phone, Is.EqualTo("1234567890"));
            Assert.That(result.Address, Is.EqualTo("123 Business St"));
            
            // Check that business rep details are correct
            Assert.That(result.BusinessId, Is.EqualTo(businessId));
            Assert.That(result.BranchName, Is.EqualTo("Main Branch"));
            Assert.That(result.BranchAddress, Is.EqualTo("456 Branch Ave"));
            
            // Check that IDs were generated
            Assert.That(result.UserId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(result.BusinessRepId, Is.Not.EqualTo(Guid.Empty));

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
        }

        [Test]
        public async Task CreateSubBusinessUser_ShouldSetUserTypeToBusinessUser()
        {
            // ARRANGE
            var dto = new CreateSubBusinessUserDto(
                BusinessId: Guid.NewGuid(),
                Username: "jane_rep",
                Email: "jane@business.com",
                Phone: "9876543210",
                Address: null,
                BranchName: null,
                BranchAddress: null
            );

            User? capturedUser = null;

            // Capture the user that gets passed to AddAsync
            _mockUserRepository
                .Setup(r => r.AddAsync(It.IsAny<User>()))
                .Callback<User>(user => capturedUser = user)
                .Returns(Task.CompletedTask);

            _mockBusinessRepRepository
                .Setup(r => r.AddAsync(It.IsAny<BusinessRep>()))
                .Returns(Task.CompletedTask);

            // ACT
            await _service.CreateSubBusinessUserAsync(dto);

            // ASSERT
            Assert.That(capturedUser, Is.Not.Null);
            Assert.That(capturedUser!.UserType, Is.EqualTo("business_user"));
        }

        [Test]
        public async Task CreateSubBusinessUser_WithNullOptionalFields_ShouldSucceed()
        {
            // ARRANGE - Test with minimal required fields
            var dto = new CreateSubBusinessUserDto(
                BusinessId: Guid.NewGuid(),
                Username: "minimal_rep",
                Email: "minimal@business.com",
                Phone: "5555555555",
                Address: null,         // Optional
                BranchName: null,      // Optional
                BranchAddress: null    // Optional
            );

            _mockUserRepository
                .Setup(r => r.AddAsync(It.IsAny<User>()))
                .Returns(Task.CompletedTask);

            _mockBusinessRepRepository
                .Setup(r => r.AddAsync(It.IsAny<BusinessRep>()))
                .Returns(Task.CompletedTask);

            // ACT
            var result = await _service.CreateSubBusinessUserAsync(dto);

            // ASSERT
            Assert.That(result.Address, Is.Null);
            Assert.That(result.BranchName, Is.Null);
            Assert.That(result.BranchAddress, Is.Null);
            Assert.That(result.Username, Is.EqualTo("minimal_rep"));
        }
    }
}