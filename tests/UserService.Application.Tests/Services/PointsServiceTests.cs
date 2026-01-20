using Moq;
using Microsoft.Extensions.Logging;
using UserService.Application.DTOs.Points;
using UserService.Application.Interfaces;
using UserService.Application.Services;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Domain.Repositories;
using static UserService.Domain.Entities.TransactionTypes;

namespace UserService.Application.Tests.Services;

[TestFixture]
public class PointsServiceTests
{
    private Mock<IUserPointsRepository> _mockPointsRepository = null!;
    private Mock<IPointTransactionRepository> _mockTransactionRepository = null!;
    private Mock<IUserRepository> _mockUserRepository = null!;
    private Mock<IUserBadgeRepository> _mockBadgeRepository = null!;
    private Mock<IUserVerificationRepository> _mockVerificationRepository = null!;
    private Mock<IPointRuleRepository> _mockPointRuleRepository = null!;
    private Mock<IPointMultiplierRepository> _mockPointMultiplierRepository = null!;
    private Mock<IPointRedemptionRepository> _mockRedemptionRepository = null!;
    private Mock<IAfricaTalkingClient> _mockAfricaTalkingClient = null!;
    private Mock<IReviewServiceClient> _mockReviewServiceClient = null!;
    private Mock<ILogger<PointsService>> _mockLogger = null!;
    private PointsService _service = null!;

    [SetUp]
    public void Setup()
    {
        _mockPointsRepository = new Mock<IUserPointsRepository>();
        _mockTransactionRepository = new Mock<IPointTransactionRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockBadgeRepository = new Mock<IUserBadgeRepository>();
        _mockVerificationRepository = new Mock<IUserVerificationRepository>();
        _mockPointRuleRepository = new Mock<IPointRuleRepository>();
        _mockPointMultiplierRepository = new Mock<IPointMultiplierRepository>();
        _mockRedemptionRepository = new Mock<IPointRedemptionRepository>();
        _mockAfricaTalkingClient = new Mock<IAfricaTalkingClient>();
        _mockReviewServiceClient = new Mock<IReviewServiceClient>();
        _mockLogger = new Mock<ILogger<PointsService>>();

        _service = new PointsService(
            _mockPointsRepository.Object,
            _mockTransactionRepository.Object,
            _mockUserRepository.Object,
            _mockBadgeRepository.Object,
            _mockVerificationRepository.Object,
            _mockPointRuleRepository.Object,
            _mockPointMultiplierRepository.Object,
            _mockRedemptionRepository.Object,
            _mockAfricaTalkingClient.Object,
            _mockReviewServiceClient.Object,
            _mockLogger.Object
        );
    }

    // ========================================================================
    // REVIEW POINTS CALCULATION TESTS
    // ========================================================================

    [Test]
    public async Task CalculateReviewPoints_ShortBodyNonVerified_ShouldReturn2Points()
    {
        // ARRANGE
        var dto = new CalculateReviewPointsDto(
            UserId: Guid.NewGuid(),
            ReviewId: Guid.NewGuid(),
            HasStars: true,
            HasHeader: false,
            BodyLength: 30,
            ImageCount: 0,
            IsVerifiedUser: false
        );

        _mockVerificationRepository.Setup(r => r.GetByUserIdAsync(dto.UserId))
            .ReturnsAsync((UserVerification?)null);

        // ACT
        var result = await _service.CalculateReviewPointsAsync(dto);

        // ASSERT
        Assert.That(result.TotalPoints, Is.EqualTo(2.0m));
        Assert.That(result.BodyPoints, Is.EqualTo(2.0m));
        Assert.That(result.ImagePoints, Is.EqualTo(0m));
        Assert.That(result.VerifiedBonus, Is.False);
    }

    [Test]
    public async Task CalculateReviewPoints_ShortBodyVerified_ShouldReturn3Points()
    {
        // ARRANGE
        var dto = new CalculateReviewPointsDto(
            UserId: Guid.NewGuid(),
            ReviewId: Guid.NewGuid(),
            HasStars: true,
            HasHeader: false,
            BodyLength: 50,
            ImageCount: 0,
            IsVerifiedUser: true
        );

        // ACT
        var result = await _service.CalculateReviewPointsAsync(dto);

        // ASSERT
        Assert.That(result.TotalPoints, Is.EqualTo(3.0m));
        Assert.That(result.BodyPoints, Is.EqualTo(3.0m));
        Assert.That(result.VerifiedBonus, Is.True);
    }

    [Test]
    public async Task CalculateReviewPoints_MediumBodyNonVerified_ShouldReturn3Points()
    {
        // ARRANGE
        var dto = new CalculateReviewPointsDto(
            UserId: Guid.NewGuid(),
            ReviewId: Guid.NewGuid(),
            HasStars: true,
            HasHeader: false,
            BodyLength: 100,
            ImageCount: 0,
            IsVerifiedUser: false
        );

        _mockVerificationRepository.Setup(r => r.GetByUserIdAsync(dto.UserId))
            .ReturnsAsync((UserVerification?)null);

        // ACT
        var result = await _service.CalculateReviewPointsAsync(dto);

        // ASSERT
        Assert.That(result.TotalPoints, Is.EqualTo(3.0m));
        Assert.That(result.BodyPoints, Is.EqualTo(3.0m));
    }

    [Test]
    public async Task CalculateReviewPoints_MediumBodyVerified_ShouldReturn4Point5()
    {
        // ARRANGE
        var dto = new CalculateReviewPointsDto(
            UserId: Guid.NewGuid(),
            ReviewId: Guid.NewGuid(),
            HasStars: true,
            HasHeader: false,
            BodyLength: 150,
            ImageCount: 0,
            IsVerifiedUser: true
        );

        // ACT
        var result = await _service.CalculateReviewPointsAsync(dto);

        // ASSERT
        Assert.That(result.TotalPoints, Is.EqualTo(4.5m));
    }

    [Test]
    public async Task CalculateReviewPoints_LongBodyNonVerified_ShouldReturn5Points()
    {
        // ARRANGE
        var dto = new CalculateReviewPointsDto(
            UserId: Guid.NewGuid(),
            ReviewId: Guid.NewGuid(),
            HasStars: true,
            HasHeader: false,
            BodyLength: 300,
            ImageCount: 0,
            IsVerifiedUser: false
        );

        _mockVerificationRepository.Setup(r => r.GetByUserIdAsync(dto.UserId))
            .ReturnsAsync((UserVerification?)null);

        // ACT
        var result = await _service.CalculateReviewPointsAsync(dto);

        // ASSERT
        Assert.That(result.TotalPoints, Is.EqualTo(5.0m));
    }

    [Test]
    public async Task CalculateReviewPoints_LongBodyVerified_ShouldReturn6Point5()
    {
        // ARRANGE
        var dto = new CalculateReviewPointsDto(
            UserId: Guid.NewGuid(),
            ReviewId: Guid.NewGuid(),
            HasStars: true,
            HasHeader: false,
            BodyLength: 500,
            ImageCount: 0,
            IsVerifiedUser: true
        );

        // ACT
        var result = await _service.CalculateReviewPointsAsync(dto);

        // ASSERT
        Assert.That(result.TotalPoints, Is.EqualTo(6.5m));
    }

    [Test]
    public async Task CalculateReviewPoints_VeryLongBodyNonVerified_ShouldReturn6Points()
    {
        // ARRANGE
        var dto = new CalculateReviewPointsDto(
            UserId: Guid.NewGuid(),
            ReviewId: Guid.NewGuid(),
            HasStars: true,
            HasHeader: false,
            BodyLength: 600,
            ImageCount: 0,
            IsVerifiedUser: false
        );

        _mockVerificationRepository.Setup(r => r.GetByUserIdAsync(dto.UserId))
            .ReturnsAsync((UserVerification?)null);

        // ACT
        var result = await _service.CalculateReviewPointsAsync(dto);

        // ASSERT
        Assert.That(result.TotalPoints, Is.EqualTo(6.0m));
    }

    [Test]
    public async Task CalculateReviewPoints_VeryLongBodyVerified_ShouldReturn7Point5()
    {
        // ARRANGE
        var dto = new CalculateReviewPointsDto(
            UserId: Guid.NewGuid(),
            ReviewId: Guid.NewGuid(),
            HasStars: true,
            HasHeader: false,
            BodyLength: 1000,
            ImageCount: 0,
            IsVerifiedUser: true
        );

        // ACT
        var result = await _service.CalculateReviewPointsAsync(dto);

        // ASSERT
        Assert.That(result.TotalPoints, Is.EqualTo(7.5m));
    }

    [Test]
    public async Task CalculateReviewPoints_WithOneImageNonVerified_ShouldAdd3Points()
    {
        // ARRANGE
        var dto = new CalculateReviewPointsDto(
            UserId: Guid.NewGuid(),
            ReviewId: Guid.NewGuid(),
            HasStars: true,
            HasHeader: false,
            BodyLength: 100,
            ImageCount: 1,
            IsVerifiedUser: false
        );

        _mockVerificationRepository.Setup(r => r.GetByUserIdAsync(dto.UserId))
            .ReturnsAsync((UserVerification?)null);

        // ACT
        var result = await _service.CalculateReviewPointsAsync(dto);

        // ASSERT
        Assert.That(result.TotalPoints, Is.EqualTo(6.0m)); // 3 body + 3 image
        Assert.That(result.ImagePoints, Is.EqualTo(3.0m));
    }

    [Test]
    public async Task CalculateReviewPoints_WithOneImageVerified_ShouldAdd4Point5()
    {
        // ARRANGE
        var dto = new CalculateReviewPointsDto(
            UserId: Guid.NewGuid(),
            ReviewId: Guid.NewGuid(),
            HasStars: true,
            HasHeader: false,
            BodyLength: 100,
            ImageCount: 1,
            IsVerifiedUser: true
        );

        // ACT
        var result = await _service.CalculateReviewPointsAsync(dto);

        // ASSERT
        Assert.That(result.TotalPoints, Is.EqualTo(9.0m)); // 4.5 body + 4.5 image
        Assert.That(result.ImagePoints, Is.EqualTo(4.5m));
    }

    [Test]
    public async Task CalculateReviewPoints_WithThreeImagesNonVerified_ShouldAdd9Points()
    {
        // ARRANGE
        var dto = new CalculateReviewPointsDto(
            UserId: Guid.NewGuid(),
            ReviewId: Guid.NewGuid(),
            HasStars: true,
            HasHeader: false,
            BodyLength: 100,
            ImageCount: 3,
            IsVerifiedUser: false
        );

        _mockVerificationRepository.Setup(r => r.GetByUserIdAsync(dto.UserId))
            .ReturnsAsync((UserVerification?)null);

        // ACT
        var result = await _service.CalculateReviewPointsAsync(dto);

        // ASSERT
        Assert.That(result.TotalPoints, Is.EqualTo(12.0m)); // 3 body + 9 images (3*3)
        Assert.That(result.ImagePoints, Is.EqualTo(9.0m));
    }

    [Test]
    public async Task CalculateReviewPoints_WithMoreThanThreeImages_ShouldCapAt3Images()
    {
        // ARRANGE
        var dto = new CalculateReviewPointsDto(
            UserId: Guid.NewGuid(),
            ReviewId: Guid.NewGuid(),
            HasStars: true,
            HasHeader: false,
            BodyLength: 100,
            ImageCount: 5,
            IsVerifiedUser: false
        );

        _mockVerificationRepository.Setup(r => r.GetByUserIdAsync(dto.UserId))
            .ReturnsAsync((UserVerification?)null);

        // ACT
        var result = await _service.CalculateReviewPointsAsync(dto);

        // ASSERT
        Assert.That(result.ImagePoints, Is.EqualTo(9.0m)); // Capped at 3 images
    }

    [Test]
    public async Task CalculateReviewPoints_UserWithPhoneVerification_ShouldApplyVerifiedBonus()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var dto = new CalculateReviewPointsDto(
            UserId: userId,
            ReviewId: Guid.NewGuid(),
            HasStars: true,
            HasHeader: false,
            BodyLength: 100,
            ImageCount: 0,
            IsVerifiedUser: false
        );

        var verification = new UserVerification(userId);
        verification.VerifyPhone();

        _mockVerificationRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(verification);

        // ACT
        var result = await _service.CalculateReviewPointsAsync(dto);

        // ASSERT
        Assert.That(result.VerifiedBonus, Is.True);
        Assert.That(result.TotalPoints, Is.EqualTo(4.5m));
    }

    [Test]
    public async Task CalculateReviewPoints_UserWithEmailVerification_ShouldApplyVerifiedBonus()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var dto = new CalculateReviewPointsDto(
            UserId: userId,
            ReviewId: Guid.NewGuid(),
            HasStars: true,
            HasHeader: false,
            BodyLength: 100,
            ImageCount: 0,
            IsVerifiedUser: false
        );

        var verification = new UserVerification(userId);
        verification.VerifyEmail();

        _mockVerificationRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(verification);

        // ACT
        var result = await _service.CalculateReviewPointsAsync(dto);

        // ASSERT
        Assert.That(result.VerifiedBonus, Is.True);
        Assert.That(result.TotalPoints, Is.EqualTo(4.5m));
    }

    // ========================================================================
    // AWARD POINTS TESTS
    // ========================================================================

    [Test]
    public async Task AwardPointsAsync_ShouldAddPointsAndCreateTransaction()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var userPoints = new UserPoints(userId);

        _mockPointsRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(userPoints);
        _mockPointsRepository.Setup(r => r.UpdateAsync(It.IsAny<UserPoints>()))
            .Returns(Task.CompletedTask);
        _mockTransactionRepository.Setup(r => r.AddAsync(It.IsAny<PointTransaction>()))
            .Returns(Task.CompletedTask);

        var dto = new AwardPointsDto(
            UserId: userId,
            Points: 10m,
            TransactionType: "EARN",
            Description: "Test award"
        );

        // ACT
        var result = await _service.AwardPointsAsync(dto);

        // ASSERT
        Assert.That(result.Points, Is.EqualTo(10m));
        Assert.That(result.TransactionType, Is.EqualTo("EARN"));
        _mockPointsRepository.Verify(r => r.UpdateAsync(It.IsAny<UserPoints>()), Times.Once);
        _mockTransactionRepository.Verify(r => r.AddAsync(It.IsAny<PointTransaction>()), Times.Once);
    }

    [Test]
    public void AwardPointsAsync_WithNegativePoints_ShouldThrow()
    {
        // ARRANGE
        var dto = new AwardPointsDto(
            UserId: Guid.NewGuid(),
            Points: -5m,
            TransactionType: "EARN",
            Description: "Invalid"
        );

        // ACT & ASSERT
        Assert.ThrowsAsync<InvalidPointsAmountException>(
            async () => await _service.AwardPointsAsync(dto)
        );
    }

    [Test]
    public async Task AwardPointsAsync_UserWithNoPoints_ShouldInitializeAndAward()
    {
        // ARRANGE
        var userId = Guid.NewGuid();

        _mockPointsRepository.SetupSequence(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync((UserPoints?)null)         
            .ReturnsAsync((UserPoints?)null)           
            .ReturnsAsync(new UserPoints(userId));  

        _mockPointsRepository.Setup(r => r.AddAsync(It.IsAny<UserPoints>()))
            .Returns(Task.CompletedTask);
        _mockPointsRepository.Setup(r => r.UpdateAsync(It.IsAny<UserPoints>()))
            .Returns(Task.CompletedTask);
        _mockTransactionRepository.Setup(r => r.AddAsync(It.IsAny<PointTransaction>()))
            .Returns(Task.CompletedTask);

        var dto = new AwardPointsDto(
            UserId: userId,
            Points: 10m,
            TransactionType: "EARN",
            Description: "First points"
        );

        // ACT
        var result = await _service.AwardPointsAsync(dto);

        // ASSERT
        Assert.That(result.Points, Is.EqualTo(10m));
        _mockPointsRepository.Verify(r => r.AddAsync(It.IsAny<UserPoints>()), Times.Once);
    }

    // ========================================================================
    // DEDUCT POINTS TESTS
    // ========================================================================

    [Test]
    public async Task DeductPointsAsync_WithSufficientBalance_ShouldSucceed()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var userPoints = new UserPoints(userId);
        userPoints.AddPoints(100m);

        _mockPointsRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(userPoints);
        _mockPointsRepository.Setup(r => r.UpdateAsync(It.IsAny<UserPoints>()))
            .Returns(Task.CompletedTask);
        _mockTransactionRepository.Setup(r => r.AddAsync(It.IsAny<PointTransaction>()))
            .Returns(Task.CompletedTask);

        var dto = new DeductPointsDto(
            UserId: userId,
            Points: 50m,
            Reason: "Test deduction"
        );

        // ACT
        var result = await _service.DeductPointsAsync(dto);

        // ASSERT
        Assert.That(result.Points, Is.EqualTo(-50m));
        _mockPointsRepository.Verify(r => r.UpdateAsync(It.IsAny<UserPoints>()), Times.Once);
    }

    [Test]
    public void DeductPointsAsync_WithInsufficientBalance_ShouldThrow()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var userPoints = new UserPoints(userId);
        userPoints.AddPoints(10m);

        _mockPointsRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(userPoints);

        var dto = new DeductPointsDto(
            UserId: userId,
            Points: 50m,
            Reason: "Too much"
        );

        // ACT & ASSERT
        Assert.ThrowsAsync<InsufficientPointsException>(
            async () => await _service.DeductPointsAsync(dto)
        );
    }

    [Test]
    public void DeductPointsAsync_UserNotFound_ShouldThrow()
    {
        // ARRANGE
        var userId = Guid.NewGuid();

        _mockPointsRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync((UserPoints?)null);

        var dto = new DeductPointsDto(
            UserId: userId,
            Points: 10m,
            Reason: "Test"
        );

        // ACT & ASSERT
        Assert.ThrowsAsync<UserPointsNotFoundException>(
            async () => await _service.DeductPointsAsync(dto)
        );
    }

    // ========================================================================
    // STREAK TESTS
    // ========================================================================

    [Test]
    public async Task UpdateLoginStreak_ConsecutiveDays_ShouldIncreaseStreak()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var userPoints = new UserPoints(userId);
        var today = DateTime.UtcNow.Date;
        var yesterday = today.AddDays(-1);

        userPoints.UpdateLoginStreak(yesterday);

        _mockPointsRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(userPoints);
        _mockPointsRepository.Setup(r => r.UpdateAsync(It.IsAny<UserPoints>()))
            .Returns(Task.CompletedTask);

        // ACT
        await _service.UpdateLoginStreakAsync(userId, today);

        // ASSERT
        _mockPointsRepository.Verify(r => r.UpdateAsync(It.Is<UserPoints>(
            up => up.CurrentStreak == 2
        )), Times.Once);
    }

    [Test]
    public async Task UpdateLoginStreak_WithGap_ShouldResetStreak()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var userPoints = new UserPoints(userId);
        var today = DateTime.UtcNow.Date;
        var threeDaysAgo = today.AddDays(-3);

        userPoints.UpdateLoginStreak(threeDaysAgo);

        _mockPointsRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(userPoints);
        _mockPointsRepository.Setup(r => r.UpdateAsync(It.IsAny<UserPoints>()))
            .Returns(Task.CompletedTask);

        // ACT
        await _service.UpdateLoginStreakAsync(userId, today);

        // ASSERT
        _mockPointsRepository.Verify(r => r.UpdateAsync(It.Is<UserPoints>(
            up => up.CurrentStreak == 1
        )), Times.Once);
    }

    [Test]
    public async Task UpdateLoginStreak_SameDay_ShouldNotChangeStreak()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var userPoints = new UserPoints(userId);
        var today = DateTime.UtcNow.Date;

        userPoints.UpdateLoginStreak(today);
        var currentStreak = userPoints.CurrentStreak;

        _mockPointsRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(userPoints);
        _mockPointsRepository.Setup(r => r.UpdateAsync(It.IsAny<UserPoints>()))
            .Returns(Task.CompletedTask);

        // ACT
        await _service.UpdateLoginStreakAsync(userId, today);

        // ASSERT
        _mockPointsRepository.Verify(r => r.UpdateAsync(It.Is<UserPoints>(
            up => up.CurrentStreak == currentStreak
        )), Times.Once);
    }

    // ========================================================================
    // MILESTONE TESTS
    // ========================================================================

    [Test]
    public async Task CheckAndAwardStreakMilestone_At100Days_ShouldAwardPointsNonVerified()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var userPoints = new UserPoints(userId);
        
        for (int i = 0; i < 100; i++)
        {
            userPoints.UpdateLoginStreak(DateTime.UtcNow.Date.AddDays(-99 + i));
        }

        _mockPointsRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(userPoints);
        _mockTransactionRepository.Setup(r => r.GetByUserIdAndTypeAsync(userId, TransactionTypes.Milestone))
            .ReturnsAsync(new List<PointTransaction>());
        _mockVerificationRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync((UserVerification?)null);
        _mockPointsRepository.Setup(r => r.UpdateAsync(It.IsAny<UserPoints>()))
            .Returns(Task.CompletedTask);
        _mockTransactionRepository.Setup(r => r.AddAsync(It.IsAny<PointTransaction>()))
            .Returns(Task.CompletedTask);

        // ACT
        var result = await _service.CheckAndAwardStreakMilestoneAsync(userId);

        // ASSERT
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Points, Is.EqualTo(100m));
        Assert.That(result.Description, Does.Contain("100-day"));
    }

    [Test]
    public async Task CheckAndAwardStreakMilestone_At100DaysVerified_ShouldAward150Points()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var userPoints = new UserPoints(userId);
        
        for (int i = 0; i < 100; i++)
        {
            userPoints.UpdateLoginStreak(DateTime.UtcNow.Date.AddDays(-99 + i));
        }

        var verification = new UserVerification(userId);
        verification.VerifyPhone();

        _mockPointsRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(userPoints);
        _mockTransactionRepository.Setup(r => r.GetByUserIdAndTypeAsync(userId, TransactionTypes.Milestone))
            .ReturnsAsync(new List<PointTransaction>());
        _mockVerificationRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(verification);
        _mockPointsRepository.Setup(r => r.UpdateAsync(It.IsAny<UserPoints>()))
            .Returns(Task.CompletedTask);
        _mockTransactionRepository.Setup(r => r.AddAsync(It.IsAny<PointTransaction>()))
            .Returns(Task.CompletedTask);

        // ACT
        var result = await _service.CheckAndAwardStreakMilestoneAsync(userId);

        // ASSERT
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Points, Is.EqualTo(150m));
    }

    [Test]
    public async Task CheckAndAwardStreakMilestone_AlreadyAwarded_ShouldReturnNull()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var userPoints = new UserPoints(userId);
        
        for (int i = 0; i < 100; i++)
        {
            userPoints.UpdateLoginStreak(DateTime.UtcNow.Date.AddDays(-99 + i));
        }

        var existingTransaction = new PointTransaction(
            userId: userId,
            points: 100m,
            transactionType: "MILESTONE",
            description: "100-day login streak milestone bonus"
        );

        _mockPointsRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(userPoints);
        _mockTransactionRepository.Setup(r => r.GetByUserIdAndTypeAsync(userId, TransactionTypes.Milestone))
            .ReturnsAsync(new List<PointTransaction> { existingTransaction });

        // ACT
        var result = await _service.CheckAndAwardStreakMilestoneAsync(userId);

        // ASSERT
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task CheckAndAwardReviewMilestone_At25Reviews_ShouldAwardPoints()
    {
        // ARRANGE
        var userId = Guid.NewGuid();

        _mockTransactionRepository.Setup(r => r.GetByUserIdAndTypeAsync(userId, TransactionTypes.Milestone))
            .ReturnsAsync(new List<PointTransaction>());
        _mockVerificationRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync((UserVerification?)null);
        _mockPointsRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new UserPoints(userId));
        _mockPointsRepository.Setup(r => r.UpdateAsync(It.IsAny<UserPoints>()))
            .Returns(Task.CompletedTask);
        _mockTransactionRepository.Setup(r => r.AddAsync(It.IsAny<PointTransaction>()))
            .Returns(Task.CompletedTask);

        // ACT
        var result = await _service.CheckAndAwardReviewMilestoneAsync(userId, 25);

        // ASSERT
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Points, Is.EqualTo(20m));
        Assert.That(result.Description, Does.Contain("25 reviews"));
    }

    [Test]
    public async Task CheckAndAwardReviewMilestone_At24Reviews_ShouldReturnNull()
    {
        // ARRANGE
        var userId = Guid.NewGuid();

        // ACT
        var result = await _service.CheckAndAwardReviewMilestoneAsync(userId, 24);

        // ASSERT
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task CheckAndAwardHelpfulVoteMilestone_At100Votes_ShouldAwardPoints()
    {
        // ARRANGE
        var userId = Guid.NewGuid();

        _mockTransactionRepository.Setup(r => r.GetByUserIdAndTypeAsync(userId, TransactionTypes.Milestone))
            .ReturnsAsync(new List<PointTransaction>());
        _mockVerificationRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync((UserVerification?)null);
        _mockPointsRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new UserPoints(userId));
        _mockPointsRepository.Setup(r => r.UpdateAsync(It.IsAny<UserPoints>()))
            .Returns(Task.CompletedTask);
        _mockTransactionRepository.Setup(r => r.AddAsync(It.IsAny<PointTransaction>()))
            .Returns(Task.CompletedTask);

        // ACT
        var result = await _service.CheckAndAwardHelpfulVoteMilestoneAsync(userId, 100);

        // ASSERT
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Points, Is.EqualTo(50m));
        Assert.That(result.Description, Does.Contain("100 helpful votes"));
    }

    // ========================================================================
    // REDEMPTION TESTS
    // ========================================================================

    [Test]
    public async Task RedeemPoints_ValidNigerianPhone_ShouldSucceed()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var userPoints = new UserPoints(userId);
        userPoints.AddPoints(100m);

        _mockPointsRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(userPoints);
        _mockRedemptionRepository.Setup(r => r.AddAsync(It.IsAny<PointRedemption>()))
            .Returns(Task.CompletedTask);
        _mockRedemptionRepository.Setup(r => r.UpdateAsync(It.IsAny<PointRedemption>()))
            .Returns(Task.CompletedTask);
        _mockAfricaTalkingClient.Setup(c => c.SendAirtimeAsync(It.IsAny<string>(), It.IsAny<decimal>()))
            .ReturnsAsync(new AirtimeResponse 
            { 
                Success = true, 
                Message = "Sent",
                TransactionId = "AT-12345"
            });
        _mockPointsRepository.Setup(r => r.UpdateAsync(It.IsAny<UserPoints>()))
            .Returns(Task.CompletedTask);
        _mockTransactionRepository.Setup(r => r.AddAsync(It.IsAny<PointTransaction>()))
            .Returns(Task.CompletedTask);

        var dto = new RedeemPointsDto(
            UserId: userId,
            Points: 50m,
            PhoneNumber: "+2348012345678"
        );

        // ACT
        var result = await _service.RedeemPointsAsync(dto);

        // ASSERT
        Assert.That(result.PointsRedeemed, Is.EqualTo(50m));
        Assert.That(result.Status, Is.EqualTo("COMPLETED"));
        _mockAfricaTalkingClient.Verify(c => c.SendAirtimeAsync("+2348012345678", 50m), Times.Once);
    }

    [Test]
    public void RedeemPoints_InvalidPhoneNumber_ShouldThrow()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var userPoints = new UserPoints(userId);
        userPoints.AddPoints(100m);

        _mockPointsRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(userPoints);

        var dto = new RedeemPointsDto(
            UserId: userId,
            Points: 50m,
            PhoneNumber: "invalid-phone"
        );

        // ACT & ASSERT
        Assert.ThrowsAsync<InvalidPhoneNumberException>(
            async () => await _service.RedeemPointsAsync(dto)
        );
    }

    [Test]
    public async Task RedeemPoints_AirtimeFails_ShouldMarkAsFailed()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var userPoints = new UserPoints(userId);
        userPoints.AddPoints(100m);

        _mockPointsRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(userPoints);
        _mockRedemptionRepository.Setup(r => r.AddAsync(It.IsAny<PointRedemption>()))
            .Returns(Task.CompletedTask);
        _mockRedemptionRepository.Setup(r => r.UpdateAsync(It.IsAny<PointRedemption>()))
            .Returns(Task.CompletedTask);
        _mockAfricaTalkingClient.Setup(c => c.SendAirtimeAsync(It.IsAny<string>(), It.IsAny<decimal>()))
            .ReturnsAsync(new AirtimeResponse 
            { 
                Success = false, 
                Message = "Failed",
                ErrorMessage = "Insufficient balance"
            });

        var dto = new RedeemPointsDto(
            UserId: userId,
            Points: 50m,
            PhoneNumber: "+2348012345678"
        );

        // ACT & ASSERT
        Assert.ThrowsAsync<PointRedemptionFailedException>(
            async () => await _service.RedeemPointsAsync(dto)
        );

        _mockRedemptionRepository.Verify(r => r.UpdateAsync(It.Is<PointRedemption>(
            pr => pr.Status == "FAILED"
        )), Times.Once);
    }

    [Test]
    [TestCase("08012345678")]
    [TestCase("2348012345678")]
    [TestCase("+2348012345678")]
    public async Task RedeemPoints_VariousNigerianFormats_ShouldAccept(string phoneNumber)
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var userPoints = new UserPoints(userId);
        userPoints.AddPoints(100m);

        _mockPointsRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(userPoints);
        _mockRedemptionRepository.Setup(r => r.AddAsync(It.IsAny<PointRedemption>()))
            .Returns(Task.CompletedTask);
        _mockRedemptionRepository.Setup(r => r.UpdateAsync(It.IsAny<PointRedemption>()))
            .Returns(Task.CompletedTask);
        _mockAfricaTalkingClient.Setup(c => c.SendAirtimeAsync(It.IsAny<string>(), It.IsAny<decimal>()))
            .ReturnsAsync(new AirtimeResponse { Success = true, TransactionId = "AT-123" });
        _mockPointsRepository.Setup(r => r.UpdateAsync(It.IsAny<UserPoints>()))
            .Returns(Task.CompletedTask);
        _mockTransactionRepository.Setup(r => r.AddAsync(It.IsAny<PointTransaction>()))
            .Returns(Task.CompletedTask);

        var dto = new RedeemPointsDto(
            UserId: userId,
            Points: 50m,
            PhoneNumber: phoneNumber
        );

        // ACT
        var result = await _service.RedeemPointsAsync(dto);

        // ASSERT
        Assert.That(result.Status, Is.EqualTo("COMPLETED"));
    }

    // ========================================================================
    // LEADERBOARD TESTS
    // ========================================================================

    [Test]
    public async Task GetLeaderboard_ShouldReturnTopUsers()
    {
        // ARRANGE
        var user1 = new User("user1", "u1@test.com", "1234567890", "pass", "end_user", "addr", "auth0|1");
        var user2 = new User("user2", "u2@test.com", "0987654321", "pass", "end_user", "addr", "auth0|2");

        var points1 = new UserPoints(user1.Id);
        points1.AddPoints(100m);
        var points2 = new UserPoints(user2.Id);
        points2.AddPoints(50m);

        _mockPointsRepository.Setup(r => r.GetTopUsersByPointsAsync(10))
            .ReturnsAsync(new List<UserPoints> { points1, points2 });
        _mockUserRepository.Setup(r => r.GetByIdAsync(user1.Id))
            .ReturnsAsync(user1);
        _mockUserRepository.Setup(r => r.GetByIdAsync(user2.Id))
            .ReturnsAsync(user2);
        _mockBadgeRepository.Setup(r => r.GetBadgeCountByUserIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(0);

        // ACT
        var result = await _service.GetLeaderboardAsync(10);

        // ASSERT
        Assert.That(result.Entries.Count(), Is.EqualTo(2));
        Assert.That(result.Entries.First().Rank, Is.EqualTo(1));
        Assert.That(result.Entries.First().Username, Is.EqualTo("user1"));
    }

    // ========================================================================
    // POINT RULES TESTS
    // ========================================================================

    [Test]
    public async Task CreatePointRule_ShouldAddRule()
    {
        // ARRANGE
        var dto = new CreatePointRuleDto(
            ActionType: "REVIEW",
            Description: "Review points",
            BasePointsNonVerified: 5m,
            BasePointsVerified: 7.5m
        );

        _mockPointRuleRepository.Setup(r => r.AddAsync(It.IsAny<PointRule>()))
            .Returns(Task.CompletedTask);

        // ACT
        var result = await _service.CreatePointRuleAsync(dto, Guid.NewGuid());

        // ASSERT
        Assert.That(result.ActionType, Is.EqualTo("REVIEW"));
        Assert.That(result.BasePointsNonVerified, Is.EqualTo(5m));
        _mockPointRuleRepository.Verify(r => r.AddAsync(It.IsAny<PointRule>()), Times.Once);
    }

    [Test]
    public async Task GetPointRuleByActionType_Exists_ShouldReturn()
    {
        // ARRANGE
        var rule = new PointRule("REVIEW", "Review points", 5m, 7.5m, null, Guid.NewGuid());

        _mockPointRuleRepository.Setup(r => r.GetByActionTypeAsync("REVIEW"))
            .ReturnsAsync(rule);

        // ACT
        var result = await _service.GetPointRuleByActionTypeAsync("REVIEW");

        // ASSERT
        Assert.That(result.ActionType, Is.EqualTo("REVIEW"));
    }

    [Test]
    public void GetPointRuleByActionType_NotFound_ShouldThrow()
    {
        // ARRANGE
        _mockPointRuleRepository.Setup(r => r.GetByActionTypeAsync("INVALID"))
            .ReturnsAsync((PointRule?)null);

        // ACT & ASSERT
        Assert.ThrowsAsync<PointRuleNotFoundException>(
            async () => await _service.GetPointRuleByActionTypeAsync("INVALID")
        );
    }
}