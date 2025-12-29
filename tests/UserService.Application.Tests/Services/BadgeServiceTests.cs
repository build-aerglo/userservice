using Moq;
using UserService.Application.DTOs.Badge;
using UserService.Application.Services;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Domain.Repositories;

namespace UserService.Application.Tests.Services;

[TestFixture]
public class BadgeServiceTests
{
    private Mock<IUserBadgeRepository> _mockBadgeRepository = null!;
    private Mock<IUserRepository> _mockUserRepository = null!;
    private BadgeService _service = null!;

    [SetUp]
    public void Setup()
    {
        _mockBadgeRepository = new Mock<IUserBadgeRepository>();
        _mockUserRepository = new Mock<IUserRepository>();

        _service = new BadgeService(
            _mockBadgeRepository.Object,
            _mockUserRepository.Object
        );
    }

    [Test]
    public async Task GetUserBadgesAsync_ShouldReturnBadges_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User("john_doe", "john@example.com", "1234567890", "password", "end_user", "123 Main St", "auth0|123");
        var badges = new List<UserBadge>
        {
            new(userId, BadgeTypes.Newbie),
            new(userId, BadgeTypes.Pioneer)
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockBadgeRepository.Setup(r => r.GetActiveByUserIdAsync(userId)).ReturnsAsync(badges);

        // Act
        var result = await _service.GetUserBadgesAsync(userId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.UserId, Is.EqualTo(userId));
        Assert.That(result.TotalBadges, Is.EqualTo(2));
    }

    [Test]
    public void GetUserBadgesAsync_ShouldThrow_WhenUserNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // Act & Assert
        Assert.ThrowsAsync<EndUserNotFoundException>(() => _service.GetUserBadgesAsync(userId));
    }

    [Test]
    public async Task AssignBadgeAsync_ShouldAssignBadge_WhenValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User("john_doe", "john@example.com", "1234567890", "password", "end_user", "123 Main St", "auth0|123");
        var dto = new AssignBadgeDto(userId, BadgeTypes.Pioneer);

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockBadgeRepository.Setup(r => r.GetByUserIdAndTypeAsync(userId, BadgeTypes.Pioneer, null, null))
            .ReturnsAsync((UserBadge?)null);
        _mockBadgeRepository.Setup(r => r.AddAsync(It.IsAny<UserBadge>())).Returns(Task.CompletedTask);
        _mockBadgeRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new UserBadge(userId, BadgeTypes.Pioneer));

        // Act
        var result = await _service.AssignBadgeAsync(dto);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.BadgeType, Is.EqualTo(BadgeTypes.Pioneer));
        _mockBadgeRepository.Verify(r => r.AddAsync(It.IsAny<UserBadge>()), Times.Once);
    }

    [Test]
    public void AssignBadgeAsync_ShouldThrow_WhenUserNotFound()
    {
        // Arrange
        var dto = new AssignBadgeDto(Guid.NewGuid(), BadgeTypes.Pioneer);
        _mockUserRepository.Setup(r => r.GetByIdAsync(dto.UserId)).ReturnsAsync((User?)null);

        // Act & Assert
        Assert.ThrowsAsync<EndUserNotFoundException>(() => _service.AssignBadgeAsync(dto));
    }

    [Test]
    public void AssignBadgeAsync_ShouldThrow_WhenInvalidBadgeType()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User("john_doe", "john@example.com", "1234567890", "password", "end_user", "123 Main St", "auth0|123");
        var dto = new AssignBadgeDto(userId, "invalid_badge_type");

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act & Assert
        Assert.ThrowsAsync<InvalidBadgeTypeException>(() => _service.AssignBadgeAsync(dto));
    }

    [Test]
    public void AssignBadgeAsync_ShouldThrow_WhenBadgeAlreadyExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User("john_doe", "john@example.com", "1234567890", "password", "end_user", "123 Main St", "auth0|123");
        var existingBadge = new UserBadge(userId, BadgeTypes.Pioneer);
        var dto = new AssignBadgeDto(userId, BadgeTypes.Pioneer);

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockBadgeRepository.Setup(r => r.GetByUserIdAndTypeAsync(userId, BadgeTypes.Pioneer, null, null))
            .ReturnsAsync(existingBadge);

        // Act & Assert
        Assert.ThrowsAsync<BadgeAlreadyExistsException>(() => _service.AssignBadgeAsync(dto));
    }

    [Test]
    public async Task CalculateTierBadgeAsync_ShouldReturnPro_When250DaysOrMore()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockBadgeRepository.Setup(r => r.GetByUserIdAndTypeAsync(userId, BadgeTypes.Pro, null, null))
            .ReturnsAsync((UserBadge?)null);
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(new User("john_doe", "john@example.com", "1234567890", "password", "end_user", "123 Main St", "auth0|123"));
        _mockBadgeRepository.Setup(r => r.DeactivateAllTierBadgesAsync(userId)).Returns(Task.CompletedTask);
        _mockBadgeRepository.Setup(r => r.AddAsync(It.IsAny<UserBadge>())).Returns(Task.CompletedTask);
        _mockBadgeRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new UserBadge(userId, BadgeTypes.Pro));

        // Act
        var result = await _service.CalculateTierBadgeAsync(userId, reviewCount: 10, daysSinceJoin: 300);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.BadgeType, Is.EqualTo(BadgeTypes.Pro));
    }

    [Test]
    public async Task CalculateTierBadgeAsync_ShouldReturnExpert_When100To249Days()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockBadgeRepository.Setup(r => r.GetByUserIdAndTypeAsync(userId, BadgeTypes.Expert, null, null))
            .ReturnsAsync((UserBadge?)null);
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(new User("john_doe", "john@example.com", "1234567890", "password", "end_user", "123 Main St", "auth0|123"));
        _mockBadgeRepository.Setup(r => r.DeactivateAllTierBadgesAsync(userId)).Returns(Task.CompletedTask);
        _mockBadgeRepository.Setup(r => r.AddAsync(It.IsAny<UserBadge>())).Returns(Task.CompletedTask);
        _mockBadgeRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new UserBadge(userId, BadgeTypes.Expert));

        // Act
        var result = await _service.CalculateTierBadgeAsync(userId, reviewCount: 10, daysSinceJoin: 150);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.BadgeType, Is.EqualTo(BadgeTypes.Expert));
    }

    [Test]
    public async Task RevokeBadgeAsync_ShouldReturnTrue_WhenBadgeExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var badge = new UserBadge(userId, BadgeTypes.Pioneer);
        var dto = new RevokeBadgeDto(userId, BadgeTypes.Pioneer);

        _mockBadgeRepository.Setup(r => r.GetByUserIdAndTypeAsync(userId, BadgeTypes.Pioneer, null, null))
            .ReturnsAsync(badge);
        _mockBadgeRepository.Setup(r => r.UpdateAsync(badge)).Returns(Task.CompletedTask);

        // Act
        var result = await _service.RevokeBadgeAsync(dto);

        // Assert
        Assert.That(result, Is.True);
        _mockBadgeRepository.Verify(r => r.UpdateAsync(It.IsAny<UserBadge>()), Times.Once);
    }

    [Test]
    public async Task RevokeBadgeAsync_ShouldReturnFalse_WhenBadgeNotFound()
    {
        // Arrange
        var dto = new RevokeBadgeDto(Guid.NewGuid(), BadgeTypes.Pioneer);
        _mockBadgeRepository.Setup(r => r.GetByUserIdAndTypeAsync(dto.UserId, BadgeTypes.Pioneer, null, null))
            .ReturnsAsync((UserBadge?)null);

        // Act
        var result = await _service.RevokeBadgeAsync(dto);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void GetBadgeInfo_ShouldReturnCorrectInfo_ForPioneer()
    {
        // Act
        var (displayName, description, icon) = _service.GetBadgeInfo(BadgeTypes.Pioneer);

        // Assert
        Assert.That(displayName, Is.EqualTo("Pioneer"));
        Assert.That(description, Does.Contain("100 days"));
        Assert.That(icon, Is.EqualTo("🏅"));
    }

    [Test]
    public void GetBadgeInfo_ShouldReturnCorrectInfo_ForTopContributorWithLocation()
    {
        // Act
        var (displayName, description, icon) = _service.GetBadgeInfo(BadgeTypes.TopContributor, location: "Lagos");

        // Assert
        Assert.That(displayName, Is.EqualTo("Top Contributor in Lagos"));
        Assert.That(description, Does.Contain("Lagos"));
        Assert.That(icon, Is.EqualTo("🏆"));
    }
}
