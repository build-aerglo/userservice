using Moq;
using NUnit.Framework;
using UserService.Application.DTOs.Badge;
using UserService.Application.Interfaces;
using UserService.Application.Services;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Domain.Repositories;

namespace UserService.Application.Tests;

[TestFixture]
public class BadgeServiceTests
{
    private Mock<IBadgeDefinitionRepository> _badgeDefRepoMock;
    private Mock<IUserBadgeRepository> _userBadgeRepoMock;
    private Mock<IUserBadgeLevelRepository> _badgeLevelRepoMock;
    private Mock<IUserPointsRepository> _userPointsRepoMock;
    private IBadgeService _badgeService;

    [SetUp]
    public void Setup()
    {
        _badgeDefRepoMock = new Mock<IBadgeDefinitionRepository>();
        _userBadgeRepoMock = new Mock<IUserBadgeRepository>();
        _badgeLevelRepoMock = new Mock<IUserBadgeLevelRepository>();
        _userPointsRepoMock = new Mock<IUserPointsRepository>();

        _badgeService = new BadgeService(
            _badgeDefRepoMock.Object,
            _userBadgeRepoMock.Object,
            _badgeLevelRepoMock.Object,
            _userPointsRepoMock.Object
        );
    }

    [Test]
    public async Task GetActiveBadgesAsync_ReturnsActiveBadges()
    {
        // Arrange
        var badges = new List<BadgeDefinition>
        {
            new BadgeDefinition("pioneer", "Pioneer", "Welcome badge", null, 1, 0, "general"),
            new BadgeDefinition("expert", "Expert", "Expert badge", null, 3, 500, "general")
        };
        _badgeDefRepoMock.Setup(r => r.GetActiveAsync()).ReturnsAsync(badges);

        // Act
        var result = await _badgeService.GetActiveBadgesAsync();

        // Assert
        Assert.That(result.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task GetBadgeByNameAsync_ExistingBadge_ReturnsBadge()
    {
        // Arrange
        var badge = new BadgeDefinition("pioneer", "Pioneer", "Welcome badge", null, 1, 0, "general");
        _badgeDefRepoMock.Setup(r => r.GetByNameAsync("pioneer")).ReturnsAsync(badge);

        // Act
        var result = await _badgeService.GetBadgeByNameAsync("pioneer");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("pioneer"));
    }

    [Test]
    public async Task GetBadgeByNameAsync_NonExistingBadge_ReturnsNull()
    {
        // Arrange
        _badgeDefRepoMock.Setup(r => r.GetByNameAsync("nonexistent")).ReturnsAsync((BadgeDefinition?)null);

        // Act
        var result = await _badgeService.GetBadgeByNameAsync("nonexistent");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task AwardBadgeAsync_NewBadge_AwardsBadge()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var badge = new BadgeDefinition("pioneer", "Pioneer", "Welcome badge", null, 1, 0, "general");
        var badgeLevel = new UserBadgeLevel(userId);

        _badgeDefRepoMock.Setup(r => r.GetByNameAsync("pioneer")).ReturnsAsync(badge);
        _userBadgeRepoMock.Setup(r => r.HasBadgeAsync(userId, badge.Id)).ReturnsAsync(false);
        _badgeLevelRepoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(badgeLevel);

        // Act
        var result = await _badgeService.AwardBadgeAsync(userId, "pioneer", "registration");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.BadgeName, Is.EqualTo("pioneer"));
        Assert.That(result.UserId, Is.EqualTo(userId));
        _userBadgeRepoMock.Verify(r => r.AddAsync(It.IsAny<UserBadge>()), Times.Once);
    }

    [Test]
    public void AwardBadgeAsync_BadgeNotFound_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _badgeDefRepoMock.Setup(r => r.GetByNameAsync("nonexistent")).ReturnsAsync((BadgeDefinition?)null);

        // Act & Assert
        Assert.ThrowsAsync<BadgeNotFoundException>(async () =>
            await _badgeService.AwardBadgeAsync(userId, "nonexistent"));
    }

    [Test]
    public void AwardBadgeAsync_AlreadyEarned_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var badge = new BadgeDefinition("pioneer", "Pioneer", "Welcome badge", null, 1, 0, "general");

        _badgeDefRepoMock.Setup(r => r.GetByNameAsync("pioneer")).ReturnsAsync(badge);
        _userBadgeRepoMock.Setup(r => r.HasBadgeAsync(userId, badge.Id)).ReturnsAsync(true);

        // Act & Assert
        Assert.ThrowsAsync<BadgeAlreadyEarnedException>(async () =>
            await _badgeService.AwardBadgeAsync(userId, "pioneer"));
    }

    [Test]
    public async Task HasBadgeAsync_UserHasBadge_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var badge = new BadgeDefinition("pioneer", "Pioneer", "Welcome badge", null, 1, 0, "general");

        _badgeDefRepoMock.Setup(r => r.GetByNameAsync("pioneer")).ReturnsAsync(badge);
        _userBadgeRepoMock.Setup(r => r.HasBadgeAsync(userId, badge.Id)).ReturnsAsync(true);

        // Act
        var result = await _badgeService.HasBadgeAsync(userId, "pioneer");

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task GetUserBadgeLevelAsync_NewUser_CreatesBadgeLevel()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _badgeLevelRepoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((UserBadgeLevel?)null);

        // Act
        var result = await _badgeService.GetUserBadgeLevelAsync(userId);

        // Assert
        Assert.That(result.CurrentLevel, Is.EqualTo("Pioneer"));
        Assert.That(result.TotalBadgesEarned, Is.EqualTo(0));
        _badgeLevelRepoMock.Verify(r => r.AddAsync(It.IsAny<UserBadgeLevel>()), Times.Once);
    }

    [Test]
    public async Task CreateBadgeAsync_ValidInput_CreatesBadge()
    {
        // Arrange
        var dto = new CreateBadgeDefinitionDto(
            "new_badge",
            "New Badge",
            "A new badge",
            "https://example.com/icon.png",
            2,
            100,
            "general"
        );

        // Act
        var result = await _badgeService.CreateBadgeAsync(dto);

        // Assert
        Assert.That(result.Name, Is.EqualTo("new_badge"));
        Assert.That(result.DisplayName, Is.EqualTo("New Badge"));
        Assert.That(result.PointsRequired, Is.EqualTo(100));
        _badgeDefRepoMock.Verify(r => r.AddAsync(It.IsAny<BadgeDefinition>()), Times.Once);
    }
}
