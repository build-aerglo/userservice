using Moq;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Application.Tests.Services
{
    [TestFixture]
    public class UserServiceTests
    {
        private Mock<IUserRepository> _mockRepository = null!;
        private Application.Services.UserService _service = null!;

        [SetUp]
        public void Setup()
        {
            _mockRepository = new Mock<IUserRepository>();
            _service = new Application.Services.UserService(_mockRepository.Object);
        }

        [Test]
        public async Task GetAllAsync_ReturnsUsers()
        {
            // Arrange
            var users = new List<User>
            {
                new User("JohnDoe", "john@example.com", "1234567890", "EndUser", "123 Main St"),
                new User("JaneDoe", "jane@example.com", "9876543210", "EndUser", "456 Elm St")
            };

            _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(users);

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(2));
            Assert.That(result.First().Username, Is.EqualTo("JohnDoe"));
        }

        [Test]
        public async Task GetByIdAsync_ReturnsUser_WhenExists()
        {
            // Arrange
            var id = Guid.NewGuid();
            var user = new User("JohnDoe", "john@example.com", "1234567890", "EndUser", "123 Main St");
            _mockRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(user);

            // Act
            var result = await _service.GetByIdAsync(id);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Username, Is.EqualTo("JohnDoe"));
        }

        [Test]
        public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((User?)null);

            // Act
            var result = await _service.GetByIdAsync(id);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task CreateAsync_AddsUser_AndReturnsIt()
        {
            // Arrange
            var username = "JohnDoe";
            var email = "john@example.com";
            var phone = "1234567890";
            var userType = "EndUser";
            var address = "123 Main St";

            _mockRepository.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAsync(username, email, phone, userType, address);

            // Assert
            _mockRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
            Assert.That(result.Username, Is.EqualTo(username));
            Assert.That(result.Email, Is.EqualTo(email));
        }

        [Test]
        public async Task UpdateAsync_UserExists_UpdatesUser()
        {
            // Arrange
            var id = Guid.NewGuid();
            var user = new User("JohnDoe", "old@example.com", "1234567890", "EndUser", "Old Address");
            _mockRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(user);

            // Act
            await _service.UpdateAsync(id, "new@example.com", "5555555555", "New Address");

            // Assert
            _mockRepository.Verify(r => r.UpdateAsync(It.Is<User>(u =>
                u.Email == "new@example.com" &&
                u.Phone == "5555555555" &&
                u.Address == "New Address"
            )), Times.Once);
        }

        [Test]
        public async Task UpdateAsync_UserDoesNotExist_DoesNothing()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((User?)null);

            // Act
            await _service.UpdateAsync(id, "new@example.com", "5555555555", "New Address");

            // Assert
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
        }

        [Test]
        public async Task DeleteAsync_DeletesUser()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockRepository.Setup(r => r.DeleteAsync(id)).Returns(Task.CompletedTask);

            // Act
            await _service.DeleteAsync(id);

            // Assert
            _mockRepository.Verify(r => r.DeleteAsync(id), Times.Once);
        }
    }
}
