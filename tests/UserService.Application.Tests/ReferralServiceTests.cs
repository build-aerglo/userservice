using Moq;
using NUnit.Framework;
using UserService.Application.DTOs.Points;
using UserService.Application.DTOs.Referral;
using UserService.Application.Interfaces;
using UserService.Application.Services;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Domain.Repositories;

namespace UserService.Application.Tests;

[TestFixture]
public class ReferralServiceTests
{
    private Mock<IUserReferralCodeRepository> _referralCodeRepoMock;
    private Mock<IReferralRepository> _referralRepoMock;
    private Mock<IReferralRewardTierRepository> _tierRepoMock;
    private Mock<IReferralCampaignRepository> _campaignRepoMock;
    private Mock<IUserRepository> _userRepoMock;
    private Mock<IPointsService> _pointsServiceMock;
    private IReferralService _referralService;

    [SetUp]
    public void Setup()
    {
        _referralCodeRepoMock = new Mock<IUserReferralCodeRepository>();
        _referralRepoMock = new Mock<IReferralRepository>();
        _tierRepoMock = new Mock<IReferralRewardTierRepository>();
        _campaignRepoMock = new Mock<IReferralCampaignRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _pointsServiceMock = new Mock<IPointsService>();

        _referralService = new ReferralService(
            _referralCodeRepoMock.Object,
            _referralRepoMock.Object,
            _tierRepoMock.Object,
            _campaignRepoMock.Object,
            _userRepoMock.Object,
            _pointsServiceMock.Object
        );
    }

    [Test]
    public async Task GetOrCreateReferralCodeAsync_NewUser_CreatesCode()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _referralCodeRepoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((UserReferralCode?)null);

        // Act
        var result = await _referralService.GetOrCreateReferralCodeAsync(userId);

        // Assert
        Assert.That(result.UserId, Is.EqualTo(userId));
        Assert.That(result.ReferralCode, Is.Not.Empty);
        Assert.That(result.ReferralCode.Length, Is.EqualTo(8));
        _referralCodeRepoMock.Verify(r => r.AddAsync(It.IsAny<UserReferralCode>()), Times.Once);
    }

    [Test]
    public async Task GetOrCreateReferralCodeAsync_ExistingUser_ReturnsExistingCode()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingCode = new UserReferralCode(userId);
        _referralCodeRepoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(existingCode);

        // Act
        var result = await _referralService.GetOrCreateReferralCodeAsync(userId);

        // Assert
        Assert.That(result.UserId, Is.EqualTo(userId));
        _referralCodeRepoMock.Verify(r => r.AddAsync(It.IsAny<UserReferralCode>()), Times.Never);
    }

    [Test]
    public async Task ValidateReferralCodeAsync_ValidCode_ReturnsTrue()
    {
        // Arrange
        var referrerId = Guid.NewGuid();
        var referredId = Guid.NewGuid();
        var code = new UserReferralCode(referrerId);

        _referralCodeRepoMock.Setup(r => r.GetByCodeAsync(code.ReferralCode)).ReturnsAsync(code);
        _referralRepoMock.Setup(r => r.GetByReferredUserIdAsync(referredId)).ReturnsAsync((Referral?)null);

        // Act
        var result = await _referralService.ValidateReferralCodeAsync(code.ReferralCode, referredId);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task ValidateReferralCodeAsync_SelfReferral_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var code = new UserReferralCode(userId);

        _referralCodeRepoMock.Setup(r => r.GetByCodeAsync(code.ReferralCode)).ReturnsAsync(code);

        // Act
        var result = await _referralService.ValidateReferralCodeAsync(code.ReferralCode, userId);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task ValidateReferralCodeAsync_AlreadyReferred_ReturnsFalse()
    {
        // Arrange
        var referrerId = Guid.NewGuid();
        var referredId = Guid.NewGuid();
        var code = new UserReferralCode(referrerId);
        var existingReferral = new Referral(Guid.NewGuid(), Guid.NewGuid(), "EXISTING");

        _referralCodeRepoMock.Setup(r => r.GetByCodeAsync(code.ReferralCode)).ReturnsAsync(code);
        _referralRepoMock.Setup(r => r.GetByReferredUserIdAsync(referredId)).ReturnsAsync(existingReferral);

        // Act
        var result = await _referralService.ValidateReferralCodeAsync(code.ReferralCode, referredId);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task UseReferralCodeAsync_ValidCode_CreatesReferral()
    {
        // Arrange
        var referrerId = Guid.NewGuid();
        var referredId = Guid.NewGuid();
        var code = new UserReferralCode(referrerId);

        _referralCodeRepoMock.Setup(r => r.GetByCodeAsync(code.ReferralCode)).ReturnsAsync(code);
        _referralRepoMock.Setup(r => r.GetByReferredUserIdAsync(referredId)).ReturnsAsync((Referral?)null);
        _pointsServiceMock.Setup(p => p.EarnPointsAsync(It.IsAny<EarnPointsDto>()))
            .ReturnsAsync(new EarnPointsResultDto(true, 50, 50, null, 1.0m));

        var dto = new UseReferralCodeDto(referredId, code.ReferralCode);

        // Act
        var result = await _referralService.UseReferralCodeAsync(dto);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.ReferrerUserId, Is.EqualTo(referrerId));
        _referralRepoMock.Verify(r => r.AddAsync(It.IsAny<Referral>()), Times.Once);
    }

    [Test]
    public async Task UseReferralCodeAsync_InvalidCode_ReturnsFalse()
    {
        // Arrange
        var referredId = Guid.NewGuid();
        _referralCodeRepoMock.Setup(r => r.GetByCodeAsync("INVALID")).ReturnsAsync((UserReferralCode?)null);

        var dto = new UseReferralCodeDto(referredId, "INVALID");

        // Act
        var result = await _referralService.UseReferralCodeAsync(dto);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Does.Contain("Invalid"));
    }

    [Test]
    public async Task SetCustomCodeAsync_ValidCode_SetsCode()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingCode = new UserReferralCode(userId);

        _referralCodeRepoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(existingCode);
        _referralCodeRepoMock.Setup(r => r.GetByCodeAsync("MYCODE123")).ReturnsAsync((UserReferralCode?)null);

        // Act
        var result = await _referralService.SetCustomCodeAsync(userId, "MYCODE123");

        // Assert
        Assert.That(result.CustomCode, Is.EqualTo("MYCODE123"));
        _referralCodeRepoMock.Verify(r => r.UpdateAsync(It.IsAny<UserReferralCode>()), Times.Once);
    }

    [Test]
    public void SetCustomCodeAsync_CodeExists_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var existingCode = new UserReferralCode(userId);
        var otherCode = new UserReferralCode(otherId, "MYCODE123");

        _referralCodeRepoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(existingCode);
        _referralCodeRepoMock.Setup(r => r.GetByCodeAsync("MYCODE123")).ReturnsAsync(otherCode);

        // Act & Assert
        Assert.ThrowsAsync<ReferralCodeAlreadyExistsException>(async () =>
            await _referralService.SetCustomCodeAsync(userId, "MYCODE123"));
    }

    [Test]
    public async Task GetRewardTiersAsync_ReturnsActiveTiers()
    {
        // Arrange
        var tiers = new List<ReferralRewardTier>
        {
            new ReferralRewardTier("Starter", 0, 4, 100, 50),
            new ReferralRewardTier("Bronze", 5, 14, 125, 50)
        };
        _tierRepoMock.Setup(r => r.GetActiveAsync()).ReturnsAsync(tiers);

        // Act
        var result = await _referralService.GetRewardTiersAsync();

        // Assert
        Assert.That(result.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task GetActiveCampaignAsync_HasActiveCampaign_ReturnsCampaign()
    {
        // Arrange
        var campaign = new ReferralCampaign("Holiday Bonus", DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(1));
        _campaignRepoMock.Setup(r => r.GetCurrentlyActiveAsync()).ReturnsAsync(campaign);

        // Act
        var result = await _referralService.GetActiveCampaignAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("Holiday Bonus"));
        Assert.That(result.IsCurrentlyActive, Is.True);
    }
}
