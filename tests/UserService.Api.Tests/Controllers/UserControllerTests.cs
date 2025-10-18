using Microsoft.AspNetCore.Mvc;
using Moq;
using UserService.Application.DTOs;
using UserService.Application.Services;
using UserService.Domain.Entities;

namespace UserService.Api.Tests.Controllers
{
    [TestFixture]
    public class UserControllerTests
    {
        private Mock<IUserService> _mockUserService = null!;
        private UserController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _mockUserService = new Mock<IUserService>();
            _controller = new UserController(_mockUserService.Object);
        }


        [Test]
        public async Task GetAll_ReturnsOkResult_WithListOfUsers()
        {
            // Arrange
            var users = new List<User>
            {
                new User("JohnDoe", "john@example.com", "1234567890", "EndUser", "123 Main St"),
                new User("JaneDoe", "jane@example.com", "9876543210", "EndUser", "456 Elm St")
            };

            _mockUserService.Setup(s => s.GetAllAsync()).ReturnsAsync(users);

            // Act
            var result = await _controller.GetAll();

            // Assert
            var okResult = result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult!.StatusCode, Is.EqualTo(200));
            Assert.That(okResult.Value, Is.EqualTo(users));
        }

        [Test]
        public async Task Get_ReturnsOk_WhenUserExists()
        {
            // Arrange
            var id = Guid.NewGuid();
            var user = new User("JohnDoe", "john@example.com", "1234567890", "EndUser", "123 Main St");

            _mockUserService.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(user);

            // Act
            var result = await _controller.Get(id);

            // Assert
            var okResult = result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult!.StatusCode, Is.EqualTo(200));
            Assert.That(okResult.Value, Is.EqualTo(user));
        }

        [Test]
        public async Task Get_ReturnsNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockUserService.Setup(s => s.GetByIdAsync(id)).ReturnsAsync((User?)null);

            // Act
            var result = await _controller.Get(id);

            // Assert
            Assert.That(result, Is.TypeOf<NotFoundResult>());
        }
    }
}
