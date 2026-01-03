using Moq;
using NUnit.Framework;
using UserService.Application.DTOs.Points;
using UserService.Application.Interfaces;
using UserService.Application.Services;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Domain.Repositories;

namespace UserService.Application.Tests;

[TestFixture]
public class PointsServiceTests
{
    private Mock<IUserPointsRepository> _userPointsRepoMock;
    private Mock<IPointTransactionRepository> _transactionRepoMock;
    private Mock<IPointRuleRepository> _ruleRepoMock;
    private Mock<IPointMultiplierRepository> _multiplierRepoMock;
    private Mock<IUserDailyPointsRepository> _dailyPointsRepoMock;
    private Mock<IUserBadgeLevelRepository> _badgeLevelRepoMock;
    private Mock<IUserRepository> _userRepoMock;
    private IPointsService _pointsService;

    [SetUp]
    public void Setup()
    {
        _userPointsRepoMock = new Mock<IUserPointsRepository>();
        _transactionRepoMock = new Mock<IPointTransactionRepository>();
        _ruleRepoMock = new Mock<IPointRuleRepository>();
        _multiplierRepoMock = new Mock<IPointMultiplierRepository>();
        _dailyPointsRepoMock = new Mock<IUserDailyPointsRepository>();
        _badgeLevelRepoMock = new Mock<IUserBadgeLevelRepository>();
        _userRepoMock = new Mock<IUserRepository>();

        _pointsService = new PointsService(
            _userPointsRepoMock.Object,
            _transactionRepoMock.Object,
            _ruleRepoMock.Object,
            _multiplierRepoMock.Object,
            _dailyPointsRepoMock.Object,
            _badgeLevelRepoMock.Object,
            _userRepoMock.Object
        );
    }

    [Test]
    public async Task GetOrCreateUserPointsAsync_NewUser_CreatesPoints()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userPointsRepoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((UserPoints?)null);

        // Act
        var result = await _pointsService.GetOrCreateUserPointsAsync(userId);

        // Assert
        Assert.That(result.UserId, Is.EqualTo(userId));
        Assert.That(result.TotalPoints, Is.EqualTo(0));
        _userPointsRepoMock.Verify(r => r.AddAsync(It.IsAny<UserPoints>()), Times.Once);
    }

    [Test]
    public async Task GetOrCreateUserPointsAsync_ExistingUser_ReturnsExistingPoints()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingPoints = new UserPoints(userId);
        existingPoints.AddPoints(100);
        _userPointsRepoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(existingPoints);

        // Act
        var result = await _pointsService.GetOrCreateUserPointsAsync(userId);

        // Assert
        Assert.That(result.TotalPoints, Is.EqualTo(100));
        _userPointsRepoMock.Verify(r => r.AddAsync(It.IsAny<UserPoints>()), Times.Never);
    }

    [Test]
    public async Task EarnPointsAsync_ValidAction_EarnsPoints()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var rule = new PointRule("review_submitted", 20, "Points for review");
        var userPoints = new UserPoints(userId);

        _ruleRepoMock.Setup(r => r.GetByActionTypeAsync("review_submitted")).ReturnsAsync(rule);
        _dailyPointsRepoMock.Setup(r => r.GetByUserActionDateAsync(userId, "review_submitted", It.IsAny<DateTime>()))
            .ReturnsAsync((UserDailyPoints?)null);
        _multiplierRepoMock.Setup(r => r.GetHighestActiveMultiplierAsync("review_submitted"))
            .ReturnsAsync((PointMultiplier?)null);
        _userPointsRepoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(userPoints);

        var dto = new EarnPointsDto(userId, "review_submitted", "review", Guid.NewGuid(), "User review");

        // Act
        var result = await _pointsService.EarnPointsAsync(dto);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.PointsEarned, Is.EqualTo(20));
        _transactionRepoMock.Verify(r => r.AddAsync(It.IsAny<PointTransaction>()), Times.Once);
    }

    [Test]
    public async Task EarnPointsAsync_NoRuleFound_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _ruleRepoMock.Setup(r => r.GetByActionTypeAsync("invalid_action")).ReturnsAsync((PointRule?)null);

        var dto = new EarnPointsDto(userId, "invalid_action");

        // Act
        var result = await _pointsService.EarnPointsAsync(dto);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Does.Contain("No active rule"));
    }

    [Test]
    public async Task EarnPointsAsync_DailyLimitReached_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var rule = new PointRule("review_submitted", 20, "Points for review", maxDailyOccurrences: 3);
        var dailyPoints = new UserDailyPoints(userId, "review_submitted");
        dailyPoints.IncrementOccurrence();
        dailyPoints.IncrementOccurrence();
        dailyPoints.IncrementOccurrence(); // Now at 3

        _ruleRepoMock.Setup(r => r.GetByActionTypeAsync("review_submitted")).ReturnsAsync(rule);
        _dailyPointsRepoMock.Setup(r => r.GetByUserActionDateAsync(userId, "review_submitted", It.IsAny<DateTime>()))
            .ReturnsAsync(dailyPoints);

        var dto = new EarnPointsDto(userId, "review_submitted");

        // Act
        var result = await _pointsService.EarnPointsAsync(dto);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Does.Contain("Daily limit"));
    }

    [Test]
    public async Task EarnPointsAsync_WithMultiplier_AppliesMultiplier()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var rule = new PointRule("review_submitted", 20, "Points for review");
        var multiplier = new PointMultiplier("Double Points", 2.0m, DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(1));
        var userPoints = new UserPoints(userId);

        _ruleRepoMock.Setup(r => r.GetByActionTypeAsync("review_submitted")).ReturnsAsync(rule);
        _dailyPointsRepoMock.Setup(r => r.GetByUserActionDateAsync(userId, "review_submitted", It.IsAny<DateTime>()))
            .ReturnsAsync((UserDailyPoints?)null);
        _multiplierRepoMock.Setup(r => r.GetHighestActiveMultiplierAsync("review_submitted")).ReturnsAsync(multiplier);
        _userPointsRepoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(userPoints);

        var dto = new EarnPointsDto(userId, "review_submitted");

        // Act
        var result = await _pointsService.EarnPointsAsync(dto);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.PointsEarned, Is.EqualTo(40)); // 20 * 2.0
        Assert.That(result.MultiplierApplied, Is.EqualTo(2.0m));
    }

    [Test]
    public async Task RedeemPointsAsync_SufficientPoints_RedeemsPoints()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userPoints = new UserPoints(userId);
        userPoints.AddPoints(100);
        _userPointsRepoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(userPoints);

        var dto = new RedeemPointsDto(userId, 50, "Reward redemption");

        // Act
        var result = await _pointsService.RedeemPointsAsync(dto);

        // Assert
        Assert.That(result.TransactionType, Is.EqualTo("redeem"));
        Assert.That(result.Points, Is.EqualTo(-50));
        _transactionRepoMock.Verify(r => r.AddAsync(It.IsAny<PointTransaction>()), Times.Once);
    }

    [Test]
    public void RedeemPointsAsync_InsufficientPoints_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userPoints = new UserPoints(userId);
        userPoints.AddPoints(30);
        _userPointsRepoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(userPoints);

        var dto = new RedeemPointsDto(userId, 50, "Reward redemption");

        // Act & Assert
        Assert.ThrowsAsync<InsufficientPointsException>(async () =>
            await _pointsService.RedeemPointsAsync(dto));
    }

    [Test]
    public void RedeemPointsAsync_UserNotFound_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userPointsRepoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((UserPoints?)null);

        var dto = new RedeemPointsDto(userId, 50, "Reward redemption");

        // Act & Assert
        Assert.ThrowsAsync<UserPointsNotFoundException>(async () =>
            await _pointsService.RedeemPointsAsync(dto));
    }

    [Test]
    public async Task AdjustPointsAsync_PositiveAdjustment_AddsPoints()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userPoints = new UserPoints(userId);
        _userPointsRepoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(userPoints);

        var dto = new AdjustPointsDto(userId, 50, "Manual adjustment");

        // Act
        var result = await _pointsService.AdjustPointsAsync(dto);

        // Assert
        Assert.That(result.Points, Is.EqualTo(50));
        Assert.That(result.TransactionType, Is.EqualTo("adjust"));
    }

    [Test]
    public async Task GetActiveRulesAsync_ReturnsActiveRules()
    {
        // Arrange
        var rules = new List<PointRule>
        {
            new PointRule("review_submitted", 20, "Points for review"),
            new PointRule("daily_login", 5, "Daily login bonus")
        };
        _ruleRepoMock.Setup(r => r.GetActiveAsync()).ReturnsAsync(rules);

        // Act
        var result = await _pointsService.GetActiveRulesAsync();

        // Assert
        Assert.That(result.Count(), Is.EqualTo(2));
    }
}
