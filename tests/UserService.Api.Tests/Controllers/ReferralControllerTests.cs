using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using UserService.Api.Controllers;
using UserService.Application.DTOs.Referral;
using UserService.Application.Interfaces;
using UserService.Domain.Exceptions;

namespace UserService.Api.Tests.Controllers;

[TestFixture]
public class ReferralControllerTests
{
    private Mock<IReferralService> _mockReferralService = null!;
    private Mock<ILogger<ReferralController>> _mockLogger = null!;
    private ReferralController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _mockReferralService = new Mock<IReferralService>();
        _mockLogger = new Mock<ILogger<ReferralController>>();
        _controller = new ReferralController(_mockReferralService.Object, _mockLogger.Object);
    }

    // ========================================================================
    // GET /api/referral/user/{userId}/code
    // ========================================================================

    [Test]
    public async Task GetUserReferralCode_ShouldReturnOk_WhenCodeExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expected = new UserReferralCodeDto(
            UserId: userId,
            Code: "JOHN2025",
            TotalReferrals: 5,
            SuccessfulReferrals: 3,
            IsActive: true,
            CreatedAt: DateTime.UtcNow
        );

        _mockReferralService
            .Setup(s => s.GetUserReferralCodeAsync(userId))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.GetUserReferralCode(userId);

        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.StatusCode, Is.EqualTo(200));
        Assert.That(okResult.Value, Is.EqualTo(expected));
    }

    [Test]
    public async Task GetUserReferralCode_ShouldGenerateAndReturnCreated_WhenCodeDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var newCode = new UserReferralCodeDto(
            UserId: userId,
            Code: "JOHN2025",
            TotalReferrals: 0,
            SuccessfulReferrals: 0,
            IsActive: true,
            CreatedAt: DateTime.UtcNow
        );

        _mockReferralService
            .Setup(s => s.GetUserReferralCodeAsync(userId))
            .ReturnsAsync((UserReferralCodeDto?)null);

        _mockReferralService
            .Setup(s => s.GenerateReferralCodeAsync(It.IsAny<GenerateReferralCodeDto>()))
            .ReturnsAsync(newCode);

        // Act
        var result = await _controller.GetUserReferralCode(userId);

        // Assert
        var createdResult = result as CreatedResult;
        Assert.That(createdResult, Is.Not.Null);
        Assert.That(createdResult!.StatusCode, Is.EqualTo(201));
    }

    // ========================================================================
    // GET /api/referral/validate/{code}
    // ========================================================================

    [Test]
    public async Task ValidateReferralCode_ShouldReturnOk_WithValidCode()
    {
        // Arrange
        var code = "VALID2025";
        _mockReferralService
            .Setup(s => s.ValidateReferralCodeAsync(code))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ValidateReferralCode(code);

        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        
        var value = okResult!.Value;
        var codeProperty = value!.GetType().GetProperty("code")!.GetValue(value);
        var isValidProperty = value.GetType().GetProperty("isValid")!.GetValue(value);
        
        Assert.That(codeProperty, Is.EqualTo(code));
        Assert.That(isValidProperty, Is.True);
    }

    [Test]
    public async Task ValidateReferralCode_ShouldReturnOk_WithInvalidCode()
    {
        // Arrange
        var code = "INVALID";
        _mockReferralService
            .Setup(s => s.ValidateReferralCodeAsync(code))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.ValidateReferralCode(code);

        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        
        var value = okResult!.Value;
        var isValidProperty = value!.GetType().GetProperty("isValid")!.GetValue(value);
        Assert.That(isValidProperty, Is.False);
    }

    // ========================================================================
    // GET /api/referral/code/{code}
    // ========================================================================

    [Test]
    public async Task GetReferralCodeDetails_ShouldReturnOk_WhenCodeExists()
    {
        // Arrange
        var code = "JOHN2025";
        var expected = new ReferralCodeDetailsDto(
            Code: code,
            UserId: Guid.NewGuid(),
            Username: "john_doe",
            TotalReferrals: 10,
            SuccessfulReferrals: 7,
            IsActive: true
        );

        _mockReferralService
            .Setup(s => s.GetReferralCodeDetailsAsync(code))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.GetReferralCodeDetails(code);

        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.Value, Is.EqualTo(expected));
    }

    [Test]
    public async Task GetReferralCodeDetails_ShouldReturnNotFound_WhenCodeDoesNotExist()
    {
        // Arrange
        var code = "INVALID";
        _mockReferralService
            .Setup(s => s.GetReferralCodeDetailsAsync(code))
            .ReturnsAsync((ReferralCodeDetailsDto?)null);

        // Act
        var result = await _controller.GetReferralCodeDetails(code);

        // Assert
        var notFoundResult = result as NotFoundObjectResult;
        Assert.That(notFoundResult, Is.Not.Null);
    }

    // ========================================================================
    // POST /api/referral/use
    // ========================================================================

    [Test]
    public async Task UseReferralCode_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        var dto = new ApplyReferralCodeDto(
            UserId: Guid.NewGuid(),
            Code: "JOHN2025"
        );

        var expected = new ApplyReferralCodeResponseDto(
            Success: true,
            Message: "Referral code applied successfully",
            ReferrerId: Guid.NewGuid()
        );

        _mockReferralService
            .Setup(s => s.ApplyReferralCodeAsync(dto))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.UseReferralCode(dto);

        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.Value, Is.EqualTo(expected));
    }

    [Test]
    public async Task UseReferralCode_ShouldReturnNotFound_WhenCodeNotFound()
    {
        // Arrange
        var dto = new ApplyReferralCodeDto(
            UserId: Guid.NewGuid(),
            Code: "INVALID"
        );

        _mockReferralService
            .Setup(s => s.ApplyReferralCodeAsync(dto))
            .ThrowsAsync(new ReferralCodeNotFoundException("INVALID"));

        // Act
        var result = await _controller.UseReferralCode(dto);

        // Assert
        var notFoundResult = result as NotFoundObjectResult;
        Assert.That(notFoundResult, Is.Not.Null);
    }

    [Test]
    public async Task UseReferralCode_ShouldReturnConflict_WhenUserAlreadyReferred()
    {
        // Arrange
        var dto = new ApplyReferralCodeDto(
            UserId: Guid.NewGuid(),
            Code: "JOHN2025"
        );

        _mockReferralService
            .Setup(s => s.ApplyReferralCodeAsync(dto))
            .ThrowsAsync(new UserAlreadyReferredException(dto.UserId));

        // Act
        var result = await _controller.UseReferralCode(dto);

        // Assert
        var conflictResult = result as ConflictObjectResult;
        Assert.That(conflictResult, Is.Not.Null);
    }

    [Test]
    public async Task UseReferralCode_ShouldReturnBadRequest_WhenSelfReferral()
    {
        // Arrange
        var dto = new ApplyReferralCodeDto(
            UserId: Guid.NewGuid(),
            Code: "SELF2025"
        );

        _mockReferralService
            .Setup(s => s.ApplyReferralCodeAsync(dto))
            .ThrowsAsync(new SelfReferralException());

        // Act
        var result = await _controller.UseReferralCode(dto);

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null);
    }

    // ========================================================================
    // GET /api/referral/user/{userId}/referrals
    // ========================================================================

    [Test]
    public async Task GetUserReferrals_ShouldReturnOk_WithReferralList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expected = new ReferralListResponseDto(
            UserId: userId,
            Referrals: new List<ReferralDto>
            {
                new ReferralDto(
                    Id: Guid.NewGuid(),
                    ReferrerId: userId,
                    ReferredUserId: Guid.NewGuid(),
                    ReferralCode: "JOHN2025",
                    Status: "completed",
                    ApprovedReviewCount: 3,
                    PointsAwarded: true,
                    QualifiedAt: DateTime.UtcNow,
                    CompletedAt: DateTime.UtcNow,
                    CreatedAt: DateTime.UtcNow
                )
            },
            TotalCount: 1,
            Stats: new ReferralStatsDto(
                UserId: userId,
                Code: "JOHN2025",
                TotalReferrals: 5,
                PendingReferrals: 2,
                SuccessfulReferrals: 3,
                TotalPointsEarned: 150m
            )
        );

        _mockReferralService
            .Setup(s => s.GetUserReferralsAsync(userId))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.GetUserReferrals(userId);

        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.Value, Is.EqualTo(expected));
    }

    // ========================================================================
    // GET /api/referral/user/{userId}/summary
    // ========================================================================

    [Test]
    public async Task GetReferralSummary_ShouldReturnOk_WithStats()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expected = new ReferralStatsDto(
            UserId: userId,
            Code: "JOHN2025",
            TotalReferrals: 10,
            PendingReferrals: 3,
            SuccessfulReferrals: 7,
            TotalPointsEarned: 350m
        );

        _mockReferralService
            .Setup(s => s.GetReferralStatsAsync(userId))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.GetReferralSummary(userId);

        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.Value, Is.EqualTo(expected));
    }

    // ========================================================================
    // GET /api/referral/user/{userId}/referred-by
    // ========================================================================

    [Test]
    public async Task GetReferredBy_ShouldReturnOk_WhenUserWasReferred()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expected = new ReferredByDto(
            ReferrerId: Guid.NewGuid(),
            ReferrerUsername: "john_doe",
            ReferralCode: "JOHN2025",
            Status: "completed",
            ApprovedReviewCount: 3,
            ReferredAt: DateTime.UtcNow
        );

        _mockReferralService
            .Setup(s => s.GetReferredByAsync(userId))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.GetReferredBy(userId);

        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.Value, Is.EqualTo(expected));
    }

    [Test]
    public async Task GetReferredBy_ShouldReturnNotFound_WhenUserWasNotReferred()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockReferralService
            .Setup(s => s.GetReferredByAsync(userId))
            .ReturnsAsync((ReferredByDto?)null);

        // Act
        var result = await _controller.GetReferredBy(userId);

        // Assert
        var notFoundResult = result as NotFoundObjectResult;
        Assert.That(notFoundResult, Is.Not.Null);
    }

    // ========================================================================
    // POST /api/referral/{referralId}/complete
    // ========================================================================

    [Test]
    public async Task CompleteReferral_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        var referralId = Guid.NewGuid();
        _mockReferralService
            .Setup(s => s.CompleteReferralAsync(referralId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.CompleteReferral(referralId);

        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
    }

    [Test]
    public async Task CompleteReferral_ShouldReturnNotFound_WhenReferralDoesNotExist()
    {
        // Arrange
        var referralId = Guid.NewGuid();
        _mockReferralService
            .Setup(s => s.CompleteReferralAsync(referralId))
            .ThrowsAsync(new ReferralNotFoundException(referralId));

        // Act
        var result = await _controller.CompleteReferral(referralId);

        // Assert
        var notFoundResult = result as NotFoundObjectResult;
        Assert.That(notFoundResult, Is.Not.Null);
    }

    [Test]
    public async Task CompleteReferral_ShouldReturnConflict_WhenAlreadyCompleted()
    {
        // Arrange
        var referralId = Guid.NewGuid();
        _mockReferralService
            .Setup(s => s.CompleteReferralAsync(referralId))
            .ThrowsAsync(new ReferralAlreadyCompletedException(referralId));

        // Act
        var result = await _controller.CompleteReferral(referralId);

        // Assert
        var conflictResult = result as ConflictObjectResult;
        Assert.That(conflictResult, Is.Not.Null);
    }

    // ========================================================================
    // POST /api/referral/review/process
    // ========================================================================

    [Test]
    public async Task ProcessReferralReview_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        var dto = new ProcessReferralReviewDto(
            ReferredUserId: Guid.NewGuid(),
            ReviewId: Guid.NewGuid(),
            IsApproved: true
        );

        _mockReferralService
            .Setup(s => s.ProcessReferralReviewAsync(dto))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ProcessReferralReview(dto);

        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
    }

    // ========================================================================
    // GET /api/referral/leaderboard
    // ========================================================================

    [Test]
    public async Task GetLeaderboard_ShouldReturnOk_WithTopReferrers()
    {
        // Arrange
        var expected = new List<TopReferrerDto>
        {
            new TopReferrerDto(
                Rank: 1,
                UserId: Guid.NewGuid(),
                Username: "john_doe",
                Code: "JOHN2025",
                SuccessfulReferrals: 20,
                TotalPointsEarned: 1000m
            ),
            new TopReferrerDto(
                Rank: 2,
                UserId: Guid.NewGuid(),
                Username: "jane_doe",
                Code: "JANE2025",
                SuccessfulReferrals: 15,
                TotalPointsEarned: 750m
            )
        };

        _mockReferralService
            .Setup(s => s.GetTopReferrersAsync(10))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.GetLeaderboard(10);

        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.Value, Is.EqualTo(expected));
    }

    // ========================================================================
    // GET /api/referral/tiers
    // ========================================================================

    [Test]
    public async Task GetRewardTiers_ShouldReturnOk_WithTiers()
    {
        // Arrange
        var expected = new RewardTiersResponseDto(
            Tiers: new List<RewardTierDto>
            {
                new RewardTierDto(
                    Name: "Standard",
                    NonVerifiedPoints: 50,
                    VerifiedPoints: 75,
                    Description: "Complete 3 approved reviews to qualify referral"
                )
            },
            CurrentTier: "Standard"
        );

        _mockReferralService
            .Setup(s => s.GetRewardTiersAsync())
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.GetRewardTiers();

        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.Value, Is.EqualTo(expected));
    }

    // ========================================================================
    // GET /api/referral/user/{userId}/tier
    // ========================================================================

    [Test]
    public async Task GetUserTier_ShouldReturnOk_WithUserTier()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expected = new UserTierDto(
            UserId: userId,
            Tier: "Standard",
            TotalReferrals: 10,
            SuccessfulReferrals: 7,
            TotalPointsEarned: 350m,
            NextTierReferralsNeeded: 0
        );

        _mockReferralService
            .Setup(s => s.GetUserTierAsync(userId))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.GetUserTier(userId);

        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.Value, Is.EqualTo(expected));
    }

    // ========================================================================
    // GET /api/referral/campaign/active
    // ========================================================================

    [Test]
    public async Task GetActiveCampaign_ShouldReturnOk_WhenCampaignExists()
    {
        // Arrange
        var expected = new ReferralCampaignDto(
            Id: Guid.NewGuid(),
            Name: "Standard Referral Program",
            Description: "Earn points for referring friends",
            NonVerifiedBonus: 50,
            VerifiedBonus: 75,
            StartDate: DateTime.UtcNow.AddMonths(-1),
            EndDate: DateTime.UtcNow.AddMonths(11),
            IsActive: true
        );

        _mockReferralService
            .Setup(s => s.GetActiveCampaignAsync())
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.GetActiveCampaign();

        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.Value, Is.EqualTo(expected));
    }

    [Test]
    public async Task GetActiveCampaign_ShouldReturnNotFound_WhenNoCampaignExists()
    {
        // Arrange
        _mockReferralService
            .Setup(s => s.GetActiveCampaignAsync())
            .ReturnsAsync((ReferralCampaignDto?)null);

        // Act
        var result = await _controller.GetActiveCampaign();

        // Assert
        var notFoundResult = result as NotFoundObjectResult;
        Assert.That(notFoundResult, Is.Not.Null);
    }

    // ========================================================================
    // POST /api/referral/invite
    // ========================================================================

    [Test]
    public async Task SendReferralInvite_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        var dto = new SendReferralInviteDto(
            UserId: Guid.NewGuid(),
            RecipientEmail: "friend@example.com",
            PersonalMessage: "Join me on this platform!"
        );

        var expected = new SendReferralInviteResponseDto(
            Success: true,
            Message: "Referral invite sent to friend@example.com"
        );

        _mockReferralService
            .Setup(s => s.SendReferralInviteAsync(dto))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.SendReferralInvite(dto);

        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.Value, Is.EqualTo(expected));
    }

    // ========================================================================
    // Error Handling Tests
    // ========================================================================

    [Test]
    public async Task GetUserReferralCode_ShouldReturnInternalServerError_OnException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockReferralService
            .Setup(s => s.GetUserReferralCodeAsync(userId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetUserReferralCode(userId);

        // Assert
        var errorResult = result as ObjectResult;
        Assert.That(errorResult, Is.Not.Null);
        Assert.That(errorResult!.StatusCode, Is.EqualTo(500));
    }
}