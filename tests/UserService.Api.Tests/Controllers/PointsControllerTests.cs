using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using UserService.Api.Controllers;
using UserService.Application.DTOs.Points;
using UserService.Application.Interfaces;
using UserService.Domain.Exceptions;

namespace UserService.Api.Tests.Controllers;

[TestFixture]
public class PointsControllerTests
{
    private Mock<IPointsService> _mockPointsService = null!;
    private Mock<ILogger<PointsController>> _mockLogger = null!;
    private Mock<IReviewServiceClient> _mockReviewServiceClient = null!;
    private PointsController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _mockPointsService = new Mock<IPointsService>();
        _mockLogger = new Mock<ILogger<PointsController>>();
        _mockReviewServiceClient = new Mock<IReviewServiceClient>();
        _controller = new PointsController(_mockPointsService.Object, _mockLogger.Object, _mockReviewServiceClient.Object);
    }

    private void SetupUserClaims(Guid userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("sub", userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    // ========================================================================
    // REDEMPTION ENDPOINT TESTS
    // ========================================================================

    [Test]
    public async Task RedeemPoints_ValidRequest_ShouldReturnOk()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        SetupUserClaims(userId);

        var dto = new RedeemPointsDto(
            UserId: userId,
            Points: 50m,
            PhoneNumber: "+2348012345678"
        );

        var response = new RedemptionResponseDto(
            RedemptionId: Guid.NewGuid(),
            PointsRedeemed: 50m,
            AmountInNaira: 50m,
            PhoneNumber: "+2348012345678",
            Status: "COMPLETED",
            TransactionReference: "AT-12345",
            CreatedAt: DateTime.UtcNow
        );

        _mockPointsService.Setup(s => s.RedeemPointsAsync(dto))
            .ReturnsAsync(response);

        // ACT
        var result = await _controller.RedeemPoints(dto);

        // ASSERT
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.Value, Is.EqualTo(response));
    }

    [Test]
    public async Task RedeemPoints_InsufficientPoints_ShouldReturnBadRequest()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        SetupUserClaims(userId);

        var dto = new RedeemPointsDto(
            UserId: userId,
            Points: 1000m,
            PhoneNumber: "+2348012345678"
        );

        _mockPointsService.Setup(s => s.RedeemPointsAsync(dto))
            .ThrowsAsync(new InsufficientPointsException(userId, 1000m, 50m));

        // ACT
        var result = await _controller.RedeemPoints(dto);

        // ASSERT
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task RedeemPoints_InvalidPhoneNumber_ShouldReturnBadRequest()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        SetupUserClaims(userId);

        var dto = new RedeemPointsDto(
            UserId: userId,
            Points: 50m,
            PhoneNumber: "invalid"
        );

        _mockPointsService.Setup(s => s.RedeemPointsAsync(dto))
            .ThrowsAsync(new InvalidPhoneNumberException("invalid"));

        // ACT
        var result = await _controller.RedeemPoints(dto);

        // ASSERT
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task RedeemPoints_RedemptionFails_ShouldReturn500()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        SetupUserClaims(userId);

        var dto = new RedeemPointsDto(
            UserId: userId,
            Points: 50m,
            PhoneNumber: "+2348012345678"
        );

        _mockPointsService.Setup(s => s.RedeemPointsAsync(dto))
            .ThrowsAsync(new PointRedemptionFailedException("Airtime service down"));

        // ACT
        var result = await _controller.RedeemPoints(dto);

        // ASSERT
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var objectResult = (ObjectResult)result;
        Assert.That(objectResult.StatusCode, Is.EqualTo(500));
    }

    [Test]
    public async Task GetRedemptionHistory_ShouldReturnOk()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        SetupUserClaims(userId);

        var history = new RedemptionHistoryDto(
            UserId: userId,
            Redemptions: new List<RedemptionResponseDto>(),
            TotalCount: 0
        );

        _mockPointsService.Setup(s => s.GetRedemptionHistoryAsync(userId, 50, 0))
            .ReturnsAsync(history);

        // ACT
        var result = await _controller.GetRedemptionHistory(userId, 50, 0);

        // ASSERT
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    // ========================================================================
    // POINT RULES ENDPOINT TESTS
    // ========================================================================

    [Test]
    public async Task GetAllRules_ShouldReturnOk()
    {
        // ARRANGE
        var rules = new List<PointRuleDto>
        {
            new PointRuleDto(
                Id: Guid.NewGuid(),
                ActionType: "REVIEW",
                Description: "Review points",
                BasePointsNonVerified: 5m,
                BasePointsVerified: 7.5m,
                Conditions: null,
                IsActive: true
            )
        };

        _mockPointsService.Setup(s => s.GetAllPointRulesAsync())
            .ReturnsAsync(rules);

        // ACT
        var result = await _controller.GetAllRules();

        // ASSERT
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.Value, Is.EqualTo(rules));
    }

    [Test]
    public async Task GetRuleByActionType_Exists_ShouldReturnOk()
    {
        // ARRANGE
        var rule = new PointRuleDto(
            Id: Guid.NewGuid(),
            ActionType: "REVIEW",
            Description: "Review points",
            BasePointsNonVerified: 5m,
            BasePointsVerified: 7.5m,
            Conditions: null,
            IsActive: true
        );

        _mockPointsService.Setup(s => s.GetPointRuleByActionTypeAsync("REVIEW"))
            .ReturnsAsync(rule);

        // ACT
        var result = await _controller.GetRuleByActionType("REVIEW");

        // ASSERT
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetRuleByActionType_NotFound_ShouldReturnNotFound()
    {
        // ARRANGE
        _mockPointsService.Setup(s => s.GetPointRuleByActionTypeAsync("INVALID"))
            .ThrowsAsync(new PointRuleNotFoundException("INVALID"));

        // ACT
        var result = await _controller.GetRuleByActionType("INVALID");

        // ASSERT
        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task CreatePointRule_ValidRequest_ShouldReturnCreated()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        SetupUserClaims(userId);

        var dto = new CreatePointRuleDto(
            ActionType: "NEW_ACTION",
            Description: "New action points",
            BasePointsNonVerified: 10m,
            BasePointsVerified: 15m
        );

        var created = new PointRuleDto(
            Id: Guid.NewGuid(),
            ActionType: "NEW_ACTION",
            Description: "New action points",
            BasePointsNonVerified: 10m,
            BasePointsVerified: 15m,
            Conditions: null,
            IsActive: true
        );

        _mockPointsService.Setup(s => s.CreatePointRuleAsync(dto, userId))
            .ReturnsAsync(created);

        // ACT
        var result = await _controller.CreatePointRule(dto);

        // ASSERT
        Assert.That(result, Is.InstanceOf<CreatedResult>());
        var createdResult = (CreatedResult)result;
        Assert.That(createdResult.Value, Is.EqualTo(created));
    }

    // ========================================================================
    // POINT MULTIPLIERS ENDPOINT TESTS
    // ========================================================================

    [Test]
    public async Task GetActiveMultipliers_ShouldReturnOk()
    {
        // ARRANGE
        var multipliers = new List<PointMultiplierDto>
        {
            new PointMultiplierDto(
                Id: Guid.NewGuid(),
                Name: "Weekend Bonus",
                Description: "Double points on weekends",
                Multiplier: 2.0m,
                ActionTypes: new[] { "REVIEW" },
                StartDate: DateTime.UtcNow,
                EndDate: DateTime.UtcNow.AddDays(7),
                IsActive: true,
                IsCurrentlyActive: true
            )
        };

        _mockPointsService.Setup(s => s.GetActivePointMultipliersAsync())
            .ReturnsAsync(multipliers);

        // ACT
        var result = await _controller.GetActiveMultipliers();

        // ASSERT
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.Value, Is.EqualTo(multipliers));
    }

    [Test]
    public async Task CreateMultiplier_ValidRequest_ShouldReturnCreated()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        SetupUserClaims(userId);

        var dto = new CreatePointMultiplierDto(
            Name: "Holiday Bonus",
            Description: "Triple points during holidays",
            Multiplier: 3.0m,
            StartDate: DateTime.UtcNow,
            EndDate: DateTime.UtcNow.AddDays(30),
            ActionTypes: new[] { "REVIEW", "REFERRAL" }
        );

        var created = new PointMultiplierDto(
            Id: Guid.NewGuid(),
            Name: "Holiday Bonus",
            Description: "Triple points during holidays",
            Multiplier: 3.0m,
            ActionTypes: new[] { "REVIEW", "REFERRAL" },
            StartDate: DateTime.UtcNow,
            EndDate: DateTime.UtcNow.AddDays(30),
            IsActive: true,
            IsCurrentlyActive: true
        );

        _mockPointsService.Setup(s => s.CreatePointMultiplierAsync(dto, userId))
            .ReturnsAsync(created);

        // ACT
        var result = await _controller.CreateMultiplier(dto);

        // ASSERT
        Assert.That(result, Is.InstanceOf<CreatedResult>());
    }

    // ========================================================================
    // SUMMARY AND QUERY ENDPOINT TESTS
    // ========================================================================

    [Test]
    public async Task GetPointsSummary_ShouldReturnOk()
    {
        // ARRANGE
        var userId = Guid.NewGuid();

        var summary = new UserPointsSummaryDto(
            UserId: userId,
            TotalPoints: 150m,
            Tier: "SILVER",
            CurrentStreak: 5,
            LongestStreak: 10,
            LastLoginDate: DateTime.UtcNow,
            Rank: 42,
            RecentTransactions: new List<PointTransactionDto>()
        );

        _mockPointsService.Setup(s => s.GetUserPointsSummaryAsync(userId, 10))
            .ReturnsAsync(summary);

        // ACT
        var result = await _controller.GetPointsSummary(userId, 10);

        // ASSERT
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.Value, Is.EqualTo(summary));
    }

    [Test]
    public async Task GetTransactionsByType_ShouldReturnOk()
    {
        // ARRANGE
        var userId = Guid.NewGuid();

        var transactions = new PointTransactionsByTypeDto(
            UserId: userId,
            TransactionType: "EARN",
            Transactions: new List<PointTransactionDto>(),
            TotalPoints: 100m,
            Count: 5
        );

        _mockPointsService.Setup(s => s.GetTransactionsByTypeAsync(userId, "EARN"))
            .ReturnsAsync(transactions);

        // ACT
        var result = await _controller.GetTransactionsByType(userId, "EARN");

        // ASSERT
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetTransactionsByDateRange_ShouldReturnOk()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;

        var transactions = new PointTransactionsByDateRangeDto(
            UserId: userId,
            StartDate: startDate,
            EndDate: endDate,
            Transactions: new List<PointTransactionDto>(),
            TotalPointsEarned: 100m,
            TotalPointsDeducted: 20m,
            Count: 10
        );

        _mockPointsService.Setup(s => s.GetTransactionsByDateRangeAsync(userId, startDate, endDate))
            .ReturnsAsync(transactions);

        // ACT
        var result = await _controller.GetTransactionsByDateRange(userId, startDate, endDate);

        // ASSERT
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetUserRank_ShouldReturnOk()
    {
        // ARRANGE
        var userId = Guid.NewGuid();

        var userPoints = new UserPointsDto(
            UserId: userId,
            TotalPoints: 150m,
            Tier: "SILVER",
            CurrentStreak: 5,
            LongestStreak: 10,
            LastActivityDate: DateTime.UtcNow,
            Rank: 42
        );

        _mockPointsService.Setup(s => s.GetUserPointsAsync(userId))
            .ReturnsAsync(userPoints);

        // ACT
        var result = await _controller.GetUserRank(userId);

        // ASSERT
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        var value = okResult.Value;
        Assert.That(value, Is.Not.Null);
    }

    // ========================================================================
    // CALCULATE AND AWARD REVIEW POINTS TESTS
    // ========================================================================

    [Test]
    public async Task CalculateReviewPoints_ValidRequest_ShouldReturnOk()
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

        var result = new ReviewPointsResultDto(
            TotalPoints: 6.0m,
            StarPoints: 0m,
            HeaderPoints: 0m,
            BodyPoints: 3.0m,
            ImagePoints: 3.0m,
            VerifiedBonus: false,
            Breakdown: "Body: 3, Images: 3"
        );

        _mockPointsService.Setup(s => s.CalculateReviewPointsAsync(dto))
            .ReturnsAsync(result);

        // ACT
        var actionResult = await _controller.CalculateReviewPoints(dto);

        // ASSERT
        Assert.That(actionResult, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task AwardReviewPoints_ValidRequest_ShouldReturnCreated()
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

        var transaction = new PointTransactionDto(
            Id: Guid.NewGuid(),
            UserId: dto.UserId,
            Points: 6.0m,
            TransactionType: "EARN",
            Description: "Review points",
            ReferenceId: dto.ReviewId,
            ReferenceType: "REVIEW",
            CreatedAt: DateTime.UtcNow
        );

        _mockPointsService.Setup(s => s.AwardReviewPointsAsync(dto))
            .ReturnsAsync(transaction);

        // ACT
        var result = await _controller.AwardReviewPoints(dto);

        // ASSERT
        Assert.That(result, Is.InstanceOf<CreatedResult>());
    }

    // ========================================================================
    // LEADERBOARD TESTS
    // ========================================================================

    [Test]
    public async Task GetLeaderboard_ShouldReturnOk()
    {
        // ARRANGE
        var leaderboard = new LeaderboardResponseDto(
            Entries: new List<LeaderboardEntryDto>
            {
                new LeaderboardEntryDto(
                    Rank: 1,
                    UserId: Guid.NewGuid(),
                    Username: "top_user",
                    TotalPoints: 1000m,
                    Tier: "GOLD",
                    BadgeCount: 5
                )
            },
            Location: null,
            TotalUsers: 1
        );

        _mockPointsService.Setup(s => s.GetLeaderboardAsync(10))
            .ReturnsAsync(leaderboard);

        // ACT
        var result = await _controller.GetLeaderboard(10);

        // ASSERT
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetLocationLeaderboard_ShouldReturnOk()
    {
        // ARRANGE
        var leaderboard = new LeaderboardResponseDto(
            Entries: new List<LeaderboardEntryDto>(),
            Location: "Lagos",
            TotalUsers: 0
        );

        _mockPointsService.Setup(s => s.GetLocationLeaderboardAsync("Lagos", 10))
            .ReturnsAsync(leaderboard);

        // ACT
        var result = await _controller.GetLocationLeaderboard("Lagos", 10);

        // ASSERT
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    // ========================================================================
    // ERROR HANDLING TESTS
    // ========================================================================

    [Test]
    public async Task GetUserPoints_ServiceThrows_ShouldReturn500()
    {
        // ARRANGE
        var userId = Guid.NewGuid();

        _mockPointsService.Setup(s => s.GetUserPointsAsync(userId))
            .ThrowsAsync(new Exception("Database error"));

        // ACT
        var result = await _controller.GetUserPoints(userId);

        // ASSERT
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var objectResult = (ObjectResult)result;
        Assert.That(objectResult.StatusCode, Is.EqualTo(500));
    }

    [Test]
    public async Task GetPointsHistory_UserNotFound_ShouldReturnNotFound()
    {
        // ARRANGE
        var userId = Guid.NewGuid();

        _mockPointsService.Setup(s => s.GetPointsHistoryAsync(userId, 50, 0))
            .ThrowsAsync(new UserPointsNotFoundException(userId));

        // ACT
        var result = await _controller.GetPointsHistory(userId, 50, 0);

        // ASSERT
        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task AwardPoints_InvalidAmount_ShouldReturnBadRequest()
    {
        // ARRANGE
        var dto = new AwardPointsDto(
            UserId: Guid.NewGuid(),
            Points: -10m,
            TransactionType: "EARN",
            Description: "Invalid"
        );

        _mockPointsService.Setup(s => s.AwardPointsAsync(dto))
            .ThrowsAsync(new InvalidPointsAmountException(-10m));

        // ACT
        var result = await _controller.AwardPoints(dto);

        // ASSERT
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }
}