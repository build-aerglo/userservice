using Moq;
using UserService.Application.DTOs.Geolocation;
using UserService.Application.Services;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Domain.Repositories;

namespace UserService.Application.Tests.Services;

[TestFixture]
public class GeolocationServiceTests
{
    private Mock<IUserGeolocationRepository> _mockGeolocationRepository = null!;
    private Mock<IGeolocationHistoryRepository> _mockHistoryRepository = null!;
    private Mock<IUserRepository> _mockUserRepository = null!;
    private GeolocationService _service = null!;

    [SetUp]
    public void Setup()
    {
        _mockGeolocationRepository = new Mock<IUserGeolocationRepository>();
        _mockHistoryRepository = new Mock<IGeolocationHistoryRepository>();
        _mockUserRepository = new Mock<IUserRepository>();

        _service = new GeolocationService(
            _mockGeolocationRepository.Object,
            _mockHistoryRepository.Object,
            _mockUserRepository.Object
        );
    }

    [Test]
    public async Task GetUserGeolocationAsync_ShouldReturnGeolocation_WhenExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var geolocation = new UserGeolocation(userId, 6.5244, 3.3792, "Lagos", "Ikeja", "Lagos City");

        _mockGeolocationRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(geolocation);

        // Act
        var result = await _service.GetUserGeolocationAsync(userId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.State, Is.EqualTo("Lagos"));
        Assert.That(result.IsEnabled, Is.True);
    }

    [Test]
    public async Task UpdateGeolocationAsync_ShouldCreateNew_WhenNotExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User("john_doe", "john@example.com", "1234567890", "password", "end_user", "123 Main St", "auth0|123");
        var dto = new UpdateGeolocationDto(userId, 6.5244, 3.3792, "Lagos", "Ikeja", "Lagos City");

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockGeolocationRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((UserGeolocation?)null);
        _mockGeolocationRepository.Setup(r => r.AddAsync(It.IsAny<UserGeolocation>())).Returns(Task.CompletedTask);
        _mockGeolocationRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new UserGeolocation(userId, 6.5244, 3.3792, "Lagos", "Ikeja", "Lagos City"));

        // Act
        var result = await _service.UpdateGeolocationAsync(dto);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.State, Is.EqualTo("Lagos"));
        _mockGeolocationRepository.Verify(r => r.AddAsync(It.IsAny<UserGeolocation>()), Times.Once);
    }

    [Test]
    public void UpdateGeolocationAsync_ShouldThrow_WhenInvalidCoordinates()
    {
        // Arrange
        var dto = new UpdateGeolocationDto(Guid.NewGuid(), 91.0, 3.3792); // Invalid latitude

        // Act & Assert
        Assert.ThrowsAsync<InvalidCoordinatesException>(() => _service.UpdateGeolocationAsync(dto));
    }

    [Test]
    public void UpdateGeolocationAsync_ShouldThrow_WhenUserNotFound()
    {
        // Arrange
        var dto = new UpdateGeolocationDto(Guid.NewGuid(), 6.5244, 3.3792);
        _mockUserRepository.Setup(r => r.GetByIdAsync(dto.UserId)).ReturnsAsync((User?)null);

        // Act & Assert
        Assert.ThrowsAsync<EndUserNotFoundException>(() => _service.UpdateGeolocationAsync(dto));
    }

    [Test]
    public async Task ValidateLocationForReviewAsync_ShouldReturnValid_WhenWithinDistance()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var geolocation = new UserGeolocation(userId, 6.5244, 3.3792, "Lagos");
        var dto = new ValidateLocationForReviewDto(userId, Guid.NewGuid(), 6.5300, 3.3800, "Lagos");

        _mockGeolocationRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(geolocation);
        _mockHistoryRepository.Setup(r => r.GetLatestByUserIdAsync(userId))
            .ReturnsAsync(new GeolocationHistory(userId, 6.5244, 3.3792, "Lagos"));

        // Act
        var result = await _service.ValidateLocationForReviewAsync(dto);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.VpnDetected, Is.False);
    }

    [Test]
    public async Task ValidateLocationForReviewAsync_ShouldReturnInvalid_WhenVpnDetected()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var geolocation = new UserGeolocation(userId, 6.5244, 3.3792, "Lagos");
        var historyWithVpn = new GeolocationHistory(userId, 6.5244, 3.3792, "Lagos", null, null, "gps", true);
        var dto = new ValidateLocationForReviewDto(userId, Guid.NewGuid(), 6.5300, 3.3800, "Lagos");

        _mockGeolocationRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(geolocation);
        _mockHistoryRepository.Setup(r => r.GetLatestByUserIdAsync(userId)).ReturnsAsync(historyWithVpn);

        // Act
        var result = await _service.ValidateLocationForReviewAsync(dto);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.VpnDetected, Is.True);
    }

    [Test]
    public async Task ValidateLocationForReviewAsync_ShouldReturnValid_WhenGeolocationDisabled()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var dto = new ValidateLocationForReviewDto(userId, Guid.NewGuid(), 6.5300, 3.3800, "Lagos");

        _mockGeolocationRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((UserGeolocation?)null);

        // Act
        var result = await _service.ValidateLocationForReviewAsync(dto);

        // Assert
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Message, Does.Contain("not enabled"));
    }

    [Test]
    public async Task ToggleGeolocationAsync_ShouldEnableGeolocation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var geolocation = new UserGeolocation(userId, 6.5244, 3.3792, "Lagos");
        geolocation.Disable();
        var dto = new ToggleGeolocationDto(userId, true);

        _mockGeolocationRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(geolocation);
        _mockGeolocationRepository.Setup(r => r.UpdateAsync(geolocation)).Returns(Task.CompletedTask);

        // Act
        var result = await _service.ToggleGeolocationAsync(dto);

        // Assert
        Assert.That(result, Is.Not.Null);
        _mockGeolocationRepository.Verify(r => r.UpdateAsync(It.IsAny<UserGeolocation>()), Times.Once);
    }

    [TestCase(6.5244, 3.3792, 6.5300, 3.3800)] // Close distance
    public void CalculateDistance_ShouldCalculateCorrectly(double lat1, double lon1, double lat2, double lon2)
    {
        // Act
        var distance = _service.CalculateDistance(lat1, lon1, lat2, lon2);

        // Assert
        Assert.That(distance, Is.GreaterThan(0));
        Assert.That(distance, Is.LessThan(5)); // Should be less than 5km for these close coordinates
    }

    [TestCase(0, 0, true)]
    [TestCase(90, 180, true)]
    [TestCase(-90, -180, true)]
    [TestCase(91, 0, false)]
    [TestCase(0, 181, false)]
    [TestCase(-91, 0, false)]
    public void ValidateCoordinates_ShouldValidateCorrectly(double lat, double lon, bool expected)
    {
        // Act
        var result = _service.ValidateCoordinates(lat, lon);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public async Task GetUserCountByStateAsync_ShouldReturnCount()
    {
        // Arrange
        var state = "Lagos";
        _mockGeolocationRepository.Setup(r => r.GetUserCountByStateAsync(state)).ReturnsAsync(150);

        // Act
        var count = await _service.GetUserCountByStateAsync(state);

        // Assert
        Assert.That(count, Is.EqualTo(150));
    }

    [Test]
    public async Task GetVpnDetectionCountAsync_ShouldReturnCount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockHistoryRepository.Setup(r => r.GetVpnDetectionCountAsync(userId)).ReturnsAsync(5);

        // Act
        var count = await _service.GetVpnDetectionCountAsync(userId);

        // Assert
        Assert.That(count, Is.EqualTo(5));
    }
}
