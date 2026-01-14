using Moq;
using UserService.Application.DTOs.Points;
using UserService.Application.Services;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Domain.Repositories;

namespace UserService.Application.Tests.Services;

[TestFixture]
public class PointsServiceTests
{
    private Mock<IUserPointsRepository> _mockPointsRepository = null!;
    private Mock<IPointTransactionRepository> _mockTransactionRepository = null!;
    private Mock<IUserRepository> _mockUserRepository = null!;
    private Mock<IUserBadgeRepository> _mockBadgeRepository = null!;
    private Mock<IUserVerificationRepository> _mockVerificationRepository = null!;
    private PointsService _service = null!;

    [SetUp]
    public void Setup()
    {
        _mockPointsRepository = new Mock<IUserPointsRepository>();
        _mockTransactionRepository = new Mock<IPointTransactionRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockBadgeRepository = new Mock<IUserBadgeRepository>();
        _mockVerificationRepository = new Mock<IUserVerificationRepository>();

        _service = new PointsService(
            _mockPointsRepository.Object,
            _mockTransactionRepository.Object,
            _mockUserRepository.Object,
            _mockBadgeRepository.Object,
            _mockVerificationRepository.Object
        );
    }

    [Test]
    public async Task GetUserPointsAsync_ShouldReturnPoints_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userPoints = new UserPoints(userId);

        _mockPointsRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(userPoints);
        _mockPointsRepository.Setup(r => r.GetUserRankAsync(userId)).ReturnsAsync(5);

        // Act
        var result = await _service.GetUserPointsAsync(userId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.UserId, Is.EqualTo(userId));
        Assert.That(result.Tier, Is.EqualTo(PointTiers.Bronze));
        Assert.That(result.Rank, Is.EqualTo(5));
    }

    [Test]
    public async Task AwardPointsAsync_ShouldAddPoints_WhenValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userPoints = new UserPoints(userId);
        var dto = new AwardPointsDto(userId, 100m, TransactionTypes.Earn, "Test points");

        _mockPointsRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(userPoints);
        _mockPointsRepository.Setup(r => r.UpdateAsync(userPoints)).Returns(Task.CompletedTask);
        _mockTransactionRepository.Setup(r => r.AddAsync(It.IsAny<PointTransaction>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.AwardPointsAsync(dto);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Points, Is.EqualTo(100m));
        Assert.That(result.TransactionType, Is.EqualTo(TransactionTypes.Earn));
        _mockPointsRepository.Verify(r => r.UpdateAsync(userPoints), Times.Once);
    }

    [Test]
    public void AwardPointsAsync_ShouldThrow_WhenInvalidAmount()
    {
        // Arrange
        var dto = new AwardPointsDto(Guid.NewGuid(), -10m, TransactionTypes.Earn, "Test");

        // Act & Assert
        Assert.ThrowsAsync<InvalidPointsAmountException>(() => _service.AwardPointsAsync(dto));
    }

    [Test]
    public async Task CalculateReviewPointsAsync_ShouldCalculateCorrectly_ForNonVerifiedUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var dto = new CalculateReviewPointsDto(
            UserId: userId,
            ReviewId: Guid.NewGuid(),
            HasStars: true,
            HasHeader: true,
            BodyLength: 300, // 151-500 chars
            ImageCount: 2,
            IsVerifiedUser: false
        );

        _mockVerificationRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync((UserVerification?)null);

        // Act
        var result = await _service.CalculateReviewPointsAsync(dto);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StarPoints, Is.EqualTo(2m)); // Non-verified stars
        Assert.That(result.HeaderPoints, Is.EqualTo(1m));
        Assert.That(result.BodyPoints, Is.EqualTo(5m)); // 151-500 chars non-verified
        Assert.That(result.ImagePoints, Is.EqualTo(8m)); // 2 images * 4 pts
        Assert.That(result.TotalPoints, Is.EqualTo(16m));
        Assert.That(result.VerifiedBonus, Is.False);
    }

    [Test]
    public async Task CalculateReviewPointsAsync_ShouldApplyVerifiedBonus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var verification = new UserVerification(userId);
        verification.VerifyPhone();

        var dto = new CalculateReviewPointsDto(
            UserId: userId,
            ReviewId: Guid.NewGuid(),
            HasStars: true,
            HasHeader: true,
            BodyLength: 300,
            ImageCount: 2,
            IsVerifiedUser: true
        );

        _mockVerificationRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(verification);

        // Act
        var result = await _service.CalculateReviewPointsAsync(dto);

        // Assert
        Assert.That(result.VerifiedBonus, Is.True);
        Assert.That(result.StarPoints, Is.EqualTo(3m)); // Verified stars
        Assert.That(result.BodyPoints, Is.EqualTo(6.5m)); // Verified body 151-500
        Assert.That(result.ImagePoints, Is.EqualTo(12m)); // 2 images * 6 pts verified
    }

    [Test]
    public async Task DeductPointsAsync_ShouldDeductPoints_WhenSufficientBalance()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userPoints = new UserPoints(userId);
        userPoints.AddPoints(100m);
        var dto = new DeductPointsDto(userId, 50m, "Test deduction");

        _mockPointsRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(userPoints);
        _mockPointsRepository.Setup(r => r.UpdateAsync(userPoints)).Returns(Task.CompletedTask);
        _mockTransactionRepository.Setup(r => r.AddAsync(It.IsAny<PointTransaction>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeductPointsAsync(dto);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Points, Is.EqualTo(-50m));
    }

    [Test]
    public void DeductPointsAsync_ShouldThrow_WhenInsufficientBalance()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userPoints = new UserPoints(userId);
        userPoints.AddPoints(30m);
        var dto = new DeductPointsDto(userId, 50m, "Test deduction");

        _mockPointsRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(userPoints);

        // Act & Assert
        Assert.ThrowsAsync<InsufficientPointsException>(() => _service.DeductPointsAsync(dto));
    }

    [Test]
    public async Task GetLeaderboardAsync_ShouldReturnTopUsers()
    {
        // Arrange
        var user1 = new UserPoints(Guid.NewGuid());
        user1.AddPoints(1000m);
        var user2 = new UserPoints(Guid.NewGuid());
        user2.AddPoints(500m);

        var topUsers = new List<UserPoints> { user1, user2 };

        _mockPointsRepository.Setup(r => r.GetTopUsersByPointsAsync(10)).ReturnsAsync(topUsers);
        _mockUserRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new User("test", "test@test.com", "123", "pass", "end_user", null, "auth0|123"));
        _mockBadgeRepository.Setup(r => r.GetBadgeCountByUserIdAsync(It.IsAny<Guid>())).ReturnsAsync(3);

        // Act
        var result = await _service.GetLeaderboardAsync(10);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Entries.Count(), Is.EqualTo(2));
        Assert.That(result.Entries.First().Rank, Is.EqualTo(1));
    }

    [Test]
    public async Task GetUserTierAsync_ShouldReturnCorrectTier()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userPoints = new UserPoints(userId);
        userPoints.AddPoints(5500m); // Gold tier

        _mockPointsRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(userPoints);

        // Act
        var tier = await _service.GetUserTierAsync(userId);

        // Assert
        Assert.That(tier, Is.EqualTo(PointTiers.Gold));
    }

    [Test]
    public async Task UpdateStreakAsync_ShouldUpdateStreak()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userPoints = new UserPoints(userId);

        _mockPointsRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(userPoints);
        _mockPointsRepository.Setup(r => r.UpdateAsync(userPoints)).Returns(Task.CompletedTask);

        // Act
        await _service.UpdateStreakAsync(userId, DateTime.UtcNow);

        // Assert
        _mockPointsRepository.Verify(r => r.UpdateAsync(It.IsAny<UserPoints>()), Times.Once);
    }
}
