using Microsoft.Extensions.Logging;
using UserService.Application.DTOs.Points;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Domain.Repositories;

namespace UserService.Application.Services;

public class PointsService : IPointsService
{
    private readonly IUserPointsRepository _pointsRepository;
    private readonly IPointTransactionRepository _transactionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUserBadgeRepository _badgeRepository;
    private readonly IUserVerificationRepository _verificationRepository;
    private readonly IPointRuleRepository _pointRuleRepository;
    private readonly IPointMultiplierRepository _pointMultiplierRepository;
    private readonly IPointRedemptionRepository _redemptionRepository;
    private readonly IAfricaTalkingClient _africaTalkingClient;
    private readonly IReviewServiceClient _reviewServiceClient;
    private readonly ILogger<PointsService> _logger;

    // Image Points - UPDATED to 3.0/4.5 per image
    private const decimal ImagePointsNonVerified = 3.0m;
    private const decimal ImagePointsVerified = 4.5m;
    private const int MaxImagePoints = 3;

    // Milestone points
    private const decimal ReferralPointsNonVerified = 50m;
    private const decimal ReferralPointsVerified = 75m;
    private const decimal Streak100DaysNonVerified = 100m;
    private const decimal Streak100DaysVerified = 150m;
    private const decimal Reviews25NonVerified = 20m;
    private const decimal Reviews25Verified = 30m;
    private const decimal HelpfulVotes100NonVerified = 50m;
    private const decimal HelpfulVotes100Verified = 75m;
    private const decimal LoyaltyBonusNonVerified = 500m;
    private const decimal LoyaltyBonusVerified = 750m;

    public PointsService(
        IUserPointsRepository pointsRepository,
        IPointTransactionRepository transactionRepository,
        IUserRepository userRepository,
        IUserBadgeRepository badgeRepository,
        IUserVerificationRepository verificationRepository,
        IPointRuleRepository pointRuleRepository,
        IPointMultiplierRepository pointMultiplierRepository,
        IPointRedemptionRepository redemptionRepository,
        IAfricaTalkingClient africaTalkingClient,
        IReviewServiceClient reviewServiceClient,
        ILogger<PointsService> logger)
    {
        _pointsRepository = pointsRepository;
        _transactionRepository = transactionRepository;
        _userRepository = userRepository;
        _badgeRepository = badgeRepository;
        _verificationRepository = verificationRepository;
        _pointRuleRepository = pointRuleRepository;
        _pointMultiplierRepository = pointMultiplierRepository;
        _redemptionRepository = redemptionRepository;
        _africaTalkingClient = africaTalkingClient;
        _reviewServiceClient = reviewServiceClient;
        _logger = logger;
    }

    // ========================================================================
    // CORE POINTS OPERATIONS
    // ========================================================================

    public async Task<UserPointsDto> GetUserPointsAsync(Guid userId)
    {
        var userPoints = await _pointsRepository.GetByUserIdAsync(userId);
        if (userPoints is null)
        {
            await InitializeUserPointsAsync(userId);
            userPoints = await _pointsRepository.GetByUserIdAsync(userId);
        }

        var rank = await _pointsRepository.GetUserRankAsync(userId);

        return new UserPointsDto(
            UserId: userId,
            TotalPoints: userPoints!.TotalPoints,
            Tier: userPoints.GetTier(),
            CurrentStreak: userPoints.CurrentStreak,
            LongestStreak: userPoints.LongestStreak,
            LastActivityDate: userPoints.LastLoginDate,
            Rank: rank
        );
    }

    public async Task<PointsHistoryResponseDto> GetPointsHistoryAsync(Guid userId, int limit = 50, int offset = 0)
    {
        var userPoints = await _pointsRepository.GetByUserIdAsync(userId);
        if (userPoints is null)
            throw new UserPointsNotFoundException(userId);

        var transactions = await _transactionRepository.GetByUserIdAsync(userId, limit, offset);
        var transactionDtos = transactions.Select(MapToDto).ToList();

        return new PointsHistoryResponseDto(
            UserId: userId,
            TotalPoints: userPoints.TotalPoints,
            Transactions: transactionDtos,
            TotalCount: transactionDtos.Count
        );
    }

    public async Task<PointTransactionDto> AwardPointsAsync(AwardPointsDto dto)
    {
        if (dto.Points <= 0)
            throw new InvalidPointsAmountException(dto.Points);

        var userPoints = await _pointsRepository.GetByUserIdAsync(dto.UserId);
        if (userPoints is null)
        {
            await InitializeUserPointsAsync(dto.UserId);
            userPoints = await _pointsRepository.GetByUserIdAsync(dto.UserId);
            if (userPoints is null)
                throw new UserPointsNotFoundException(dto.UserId);
        }

        userPoints.AddPoints(dto.Points);
        await _pointsRepository.UpdateAsync(userPoints);

        var transaction = new PointTransaction(
            userId: dto.UserId,
            points: dto.Points,
            transactionType: dto.TransactionType,
            description: dto.Description,
            referenceId: dto.ReferenceId,
            referenceType: dto.ReferenceType
        );

        await _transactionRepository.AddAsync(transaction);

        return MapToDto(transaction);
    }

    public async Task<PointTransactionDto> DeductPointsAsync(DeductPointsDto dto)
    {
        if (dto.Points <= 0)
            throw new InvalidPointsAmountException(dto.Points);

        var userPoints = await _pointsRepository.GetByUserIdAsync(dto.UserId);
        if (userPoints is null)
            throw new UserPointsNotFoundException(dto.UserId);

        if (userPoints.TotalPoints < dto.Points)
            throw new InsufficientPointsException(dto.UserId, dto.Points, userPoints.TotalPoints);

        userPoints.DeductPoints(dto.Points);
        await _pointsRepository.UpdateAsync(userPoints);

        var transaction = new PointTransaction(
            userId: dto.UserId,
            points: -dto.Points,
            transactionType: TransactionTypes.Deduct,
            description: dto.Reason
        );

        await _transactionRepository.AddAsync(transaction);

        return MapToDto(transaction);
    }

    public async Task InitializeUserPointsAsync(Guid userId)
    {
        var existingPoints = await _pointsRepository.GetByUserIdAsync(userId);
        if (existingPoints is not null)
            return;

        var userPoints = new UserPoints(userId);
        await _pointsRepository.AddAsync(userPoints);
    }

    // ========================================================================
    // REVIEW POINTS
    // ========================================================================

    public async Task<ReviewPointsResultDto> CalculateReviewPointsAsync(CalculateReviewPointsDto dto)
    {
        var isVerified = await IsUserVerifiedAsync(dto.UserId);
        var verified = dto.IsVerifiedUser || isVerified;

        decimal bodyPoints = 0;
        decimal imagePoints = 0;

        // Body points based on length (includes stars)
        if (dto.BodyLength > 0)
        {
            bodyPoints = (dto.BodyLength, verified) switch
            {
                ( <= 50, false) => 2.0m,
                ( <= 50, true) => 3.0m,
                ( <= 150, false) => 3.0m,
                ( <= 150, true) => 4.5m,
                ( <= 500, false) => 5.0m,
                ( <= 500, true) => 6.5m,
                (_, false) => 6.0m,
                (_, true) => 7.5m
            };
        }

        // Image points (max 3 images counted)
        var imagesToCount = Math.Min(dto.ImageCount, MaxImagePoints);
        imagePoints = imagesToCount * (verified ? ImagePointsVerified : ImagePointsNonVerified);

        var totalPoints = bodyPoints + imagePoints;

        var breakdown = $"Body: {bodyPoints}, Images: {imagePoints}";
        if (verified)
            breakdown += " (Verified bonus applied)";

        return new ReviewPointsResultDto(
            TotalPoints: totalPoints,
            StarPoints: 0,
            HeaderPoints: 0,
            BodyPoints: bodyPoints,
            ImagePoints: imagePoints,
            VerifiedBonus: verified,
            Breakdown: breakdown
        );
    }

    public async Task<PointTransactionDto> AwardReviewPointsAsync(CalculateReviewPointsDto dto)
    {
        var result = await CalculateReviewPointsAsync(dto);

        return await AwardPointsAsync(new AwardPointsDto(
            UserId: dto.UserId,
            Points: result.TotalPoints,
            TransactionType: TransactionTypes.Earn,
            Description: $"Review points: {result.Breakdown}",
            ReferenceId: dto.ReviewId,
            ReferenceType: ReferenceTypes.Review
        ));
    }

    // ========================================================================
    // STREAK MANAGEMENT
    // ========================================================================

    public async Task UpdateLoginStreakAsync(Guid userId, DateTime loginDate)
    {
        var userPoints = await _pointsRepository.GetByUserIdAsync(userId);
        if (userPoints is null)
        {
            await InitializeUserPointsAsync(userId);
            userPoints = await _pointsRepository.GetByUserIdAsync(userId);
        }

        userPoints!.UpdateLoginStreak(loginDate);
        await _pointsRepository.UpdateAsync(userPoints);
    }

    public async Task<PointTransactionDto?> CheckAndAwardStreakMilestoneAsync(Guid userId)
    {
        var userPoints = await _pointsRepository.GetByUserIdAsync(userId);
        if (userPoints is null || userPoints.CurrentStreak != 100)
            return null;

        var existingTransactions = await _transactionRepository.GetByUserIdAndTypeAsync(userId, TransactionTypes.Milestone);
        if (existingTransactions.Any(t => t.Description.Contains("100-day login streak")))
            return null;

        var isVerified = await IsUserVerifiedAsync(userId);
        var points = isVerified ? Streak100DaysVerified : Streak100DaysNonVerified;

        return await AwardPointsAsync(new AwardPointsDto(
            UserId: userId,
            Points: points,
            TransactionType: TransactionTypes.Milestone,
            Description: "100-day login streak milestone bonus"
        ));
    }

    // ========================================================================
    // MILESTONES
    // ========================================================================

    public async Task<PointTransactionDto?> CheckAndAwardReviewMilestoneAsync(Guid userId, int totalReviews)
    {
        if (totalReviews != 25)
            return null;

        var existingTransactions = await _transactionRepository.GetByUserIdAndTypeAsync(userId, TransactionTypes.Milestone);
        if (existingTransactions.Any(t => t.Description.Contains("25 reviews")))
            return null;

        var isVerified = await IsUserVerifiedAsync(userId);
        var points = isVerified ? Reviews25Verified : Reviews25NonVerified;

        return await AwardPointsAsync(new AwardPointsDto(
            UserId: userId,
            Points: points,
            TransactionType: TransactionTypes.Milestone,
            Description: "25 reviews milestone bonus"
        ));
    }

    public async Task<PointTransactionDto?> CheckAndAwardHelpfulVoteMilestoneAsync(Guid userId, int totalHelpfulVotes)
    {
        if (totalHelpfulVotes != 100)
            return null;

        var existingTransactions = await _transactionRepository.GetByUserIdAndTypeAsync(userId, TransactionTypes.Milestone);
        if (existingTransactions.Any(t => t.Description.Contains("100 helpful votes")))
            return null;

        var isVerified = await IsUserVerifiedAsync(userId);
        var points = isVerified ? HelpfulVotes100Verified : HelpfulVotes100NonVerified;

        return await AwardPointsAsync(new AwardPointsDto(
            UserId: userId,
            Points: points,
            TransactionType: TransactionTypes.Milestone,
            Description: "100 helpful votes milestone bonus"
        ));
    }

    public async Task<PointTransactionDto> AwardReferralBonusAsync(Guid userId, Guid referralId, bool isVerifiedUser)
    {
        var points = isVerifiedUser ? ReferralPointsVerified : ReferralPointsNonVerified;

        return await AwardPointsAsync(new AwardPointsDto(
            UserId: userId,
            Points: points,
            TransactionType: TransactionTypes.Bonus,
            Description: "Referral bonus - referred user qualified",
            ReferenceId: referralId,
            ReferenceType: ReferenceTypes.Referral
        ));
    }

    // ========================================================================
    // LEADERBOARDS & TIERS
    // ========================================================================

    public async Task<LeaderboardResponseDto> GetLeaderboardAsync(int limit = 10)
    {
        var topUsers = await _pointsRepository.GetTopUsersByPointsAsync(limit);
        var entries = new List<LeaderboardEntryDto>();

        int rank = 1;
        foreach (var userPoints in topUsers)
        {
            var user = await _userRepository.GetByIdAsync(userPoints.UserId);
            var badgeCount = await _badgeRepository.GetBadgeCountByUserIdAsync(userPoints.UserId);

            entries.Add(new LeaderboardEntryDto(
                Rank: rank++,
                UserId: userPoints.UserId,
                Username: user?.Username ?? "Unknown",
                TotalPoints: userPoints.TotalPoints,
                Tier: userPoints.GetTier(),
                BadgeCount: badgeCount
            ));
        }

        return new LeaderboardResponseDto(
            Entries: entries,
            Location: null,
            TotalUsers: entries.Count
        );
    }

    public async Task<LeaderboardResponseDto> GetLocationLeaderboardAsync(string state, int limit = 10)
    {
        var topUsers = await _pointsRepository.GetTopUsersByPointsInLocationAsync(state, limit);
        var entries = new List<LeaderboardEntryDto>();

        int rank = 1;
        foreach (var userPoints in topUsers)
        {
            var user = await _userRepository.GetByIdAsync(userPoints.UserId);
            var badgeCount = await _badgeRepository.GetBadgeCountByUserIdAsync(userPoints.UserId);

            entries.Add(new LeaderboardEntryDto(
                Rank: rank++,
                UserId: userPoints.UserId,
                Username: user?.Username ?? "Unknown",
                TotalPoints: userPoints.TotalPoints,
                Tier: userPoints.GetTier(),
                BadgeCount: badgeCount
            ));
        }

        return new LeaderboardResponseDto(
            Entries: entries,
            Location: state,
            TotalUsers: entries.Count
        );
    }

    public async Task<string> GetUserTierAsync(Guid userId)
    {
        var userPoints = await _pointsRepository.GetByUserIdAsync(userId);
        return userPoints?.GetTier() ?? PointTiers.Bronze;
    }

    // ========================================================================
    // POINT REDEMPTION
    // ========================================================================

    public async Task<RedemptionResponseDto> RedeemPointsAsync(RedeemPointsDto dto)
    {
        if (!IsValidNigerianPhoneNumber(dto.PhoneNumber))
            throw new InvalidPhoneNumberException(dto.PhoneNumber);

        var userPoints = await _pointsRepository.GetByUserIdAsync(dto.UserId);
        if (userPoints is null)
            throw new UserPointsNotFoundException(dto.UserId);

        if (userPoints.TotalPoints < dto.Points)
            throw new InsufficientPointsException(dto.UserId, dto.Points, userPoints.TotalPoints);

        var airtimeAmount = dto.Points;

        var redemption = new PointRedemption(
            userId: dto.UserId,
            pointsRedeemed: dto.Points,
            amountInNaira: airtimeAmount,
            phoneNumber: dto.PhoneNumber
        );

        await _redemptionRepository.AddAsync(redemption);

        try
        {
            var result = await _africaTalkingClient.SendAirtimeAsync(dto.PhoneNumber, airtimeAmount);

            if (result.Success)
            {
                redemption.MarkAsCompleted(result.TransactionId ?? Guid.NewGuid().ToString(), result.Message);
                await _redemptionRepository.UpdateAsync(redemption);

                userPoints.DeductPoints(dto.Points);
                await _pointsRepository.UpdateAsync(userPoints);

                var transaction = new PointTransaction(
                    userId: dto.UserId,
                    points: -dto.Points,
                    transactionType: TransactionTypes.Redeem,
                    description: $"Redeemed {dto.Points} points for ₦{airtimeAmount} airtime to {dto.PhoneNumber}",
                    referenceId: redemption.Id,
                    referenceType: ReferenceTypes.Redemption
                );
                await _transactionRepository.AddAsync(transaction);

                _logger.LogInformation("Successfully redeemed {Points} points for user {UserId}", dto.Points, dto.UserId);
            }
            else
            {
                redemption.MarkAsFailed(result.ErrorMessage);
                await _redemptionRepository.UpdateAsync(redemption);

                _logger.LogError("Airtime redemption failed for user {UserId}: {Error}", dto.UserId, result.ErrorMessage);
                throw new PointRedemptionFailedException($"Airtime purchase failed: {result.Message}");
            }
        }
        catch (Exception ex) when (ex is not PointRedemptionFailedException)
        {
            redemption.MarkAsFailed(ex.Message);
            await _redemptionRepository.UpdateAsync(redemption);
            throw new PointRedemptionFailedException($"Failed to process redemption: {ex.Message}");
        }

        return MapRedemptionToDto(redemption);
    }

    public async Task<RedemptionHistoryDto> GetRedemptionHistoryAsync(Guid userId, int limit = 50, int offset = 0)
    {
        var redemptions = await _redemptionRepository.GetByUserIdAsync(userId, limit, offset);
        var redemptionDtos = redemptions.Select(MapRedemptionToDto).ToList();

        return new RedemptionHistoryDto(
            UserId: userId,
            Redemptions: redemptionDtos,
            TotalCount: redemptionDtos.Count
        );
    }

    // ========================================================================
    // POINT RULES MANAGEMENT
    // ========================================================================

    public async Task<IEnumerable<PointRuleDto>> GetAllPointRulesAsync()
    {
        var rules = await _pointRuleRepository.GetAllAsync();
        return rules.Select(MapPointRuleToDto);
    }

    public async Task<PointRuleDto> GetPointRuleByActionTypeAsync(string actionType)
    {
        var rule = await _pointRuleRepository.GetByActionTypeAsync(actionType);
        if (rule is null)
            throw new PointRuleNotFoundException(actionType);
        
        return MapPointRuleToDto(rule);
    }

    public async Task<PointRuleDto> CreatePointRuleAsync(CreatePointRuleDto dto, Guid? createdBy)
    {
        var rule = new PointRule(
            actionType: dto.ActionType,
            description: dto.Description,
            basePointsNonVerified: dto.BasePointsNonVerified,
            basePointsVerified: dto.BasePointsVerified,
            conditions: dto.Conditions,
            createdBy: createdBy
        );

        await _pointRuleRepository.AddAsync(rule);
        return MapPointRuleToDto(rule);
    }

    public async Task<PointRuleDto> UpdatePointRuleAsync(Guid id, UpdatePointRuleDto dto, Guid? updatedBy)
    {
        var rule = await _pointRuleRepository.GetByIdAsync(id);
        if (rule is null)
            throw new PointRuleNotFoundException(id.ToString());

        rule.Update(
            description: dto.Description,
            basePointsNonVerified: dto.BasePointsNonVerified,
            basePointsVerified: dto.BasePointsVerified,
            conditions: dto.Conditions,
            isActive: dto.IsActive,
            updatedBy: updatedBy
        );

        await _pointRuleRepository.UpdateAsync(rule);
        return MapPointRuleToDto(rule);
    }

    // ========================================================================
    // POINT MULTIPLIERS MANAGEMENT
    // ========================================================================

    public async Task<IEnumerable<PointMultiplierDto>> GetActivePointMultipliersAsync()
    {
        var multipliers = await _pointMultiplierRepository.GetActiveMultipliersAsync();
        return multipliers.Select(MapMultiplierToDto);
    }

    public async Task<IEnumerable<PointMultiplierDto>> GetAllPointMultipliersAsync()
    {
        var multipliers = await _pointMultiplierRepository.GetAllAsync();
        return multipliers.Select(MapMultiplierToDto);
    }

    public async Task<PointMultiplierDto> CreatePointMultiplierAsync(CreatePointMultiplierDto dto, Guid? createdBy)
    {
        var multiplier = new PointMultiplier(
            name: dto.Name,
            description: dto.Description,
            multiplier: dto.Multiplier,
            startDate: dto.StartDate,
            endDate: dto.EndDate,
            actionTypes: dto.ActionTypes,
            createdBy: createdBy
        );

        await _pointMultiplierRepository.AddAsync(multiplier);
        return MapMultiplierToDto(multiplier);
    }

    public async Task<PointMultiplierDto> UpdatePointMultiplierAsync(Guid id, UpdatePointMultiplierDto dto, Guid? updatedBy)
    {
        var multiplier = await _pointMultiplierRepository.GetByIdAsync(id);
        if (multiplier is null)
            throw new PointMultiplierNotFoundException(id);

        multiplier.Update(
            name: dto.Name,
            description: dto.Description,
            multiplier: dto.Multiplier,
            startDate: dto.StartDate,
            endDate: dto.EndDate,
            actionTypes: dto.ActionTypes,
            isActive: dto.IsActive,
            updatedBy: updatedBy
        );

        await _pointMultiplierRepository.UpdateAsync(multiplier);
        return MapMultiplierToDto(multiplier);
    }

    // ========================================================================
    // SUMMARY & QUERIES
    // ========================================================================

    public async Task<UserPointsSummaryDto> GetUserPointsSummaryAsync(Guid userId, int transactionLimit = 10)
    {
        var userPoints = await _pointsRepository.GetByUserIdAsync(userId);
        if (userPoints is null)
        {
            await InitializeUserPointsAsync(userId);
            userPoints = await _pointsRepository.GetByUserIdAsync(userId);
        }

        var rank = await _pointsRepository.GetUserRankAsync(userId);
        var recentTransactions = await _transactionRepository.GetByUserIdAsync(userId, transactionLimit, 0);
        var transactionDtos = recentTransactions.Select(MapToDto).ToList();

        return new UserPointsSummaryDto(
            UserId: userId,
            TotalPoints: userPoints!.TotalPoints,
            Tier: userPoints.GetTier(),
            CurrentStreak: userPoints.CurrentStreak,
            LongestStreak: userPoints.LongestStreak,
            LastLoginDate: userPoints.LastLoginDate,
            Rank: rank,
            RecentTransactions: transactionDtos
        );
    }

    public async Task<PointTransactionsByTypeDto> GetTransactionsByTypeAsync(Guid userId, string transactionType)
    {
        var transactions = await _transactionRepository.GetByUserIdAndTypeAsync(userId, transactionType);
        var transactionDtos = transactions.Select(MapToDto).ToList();
        var totalPoints = transactionDtos.Sum(t => t.Points);

        return new PointTransactionsByTypeDto(
            UserId: userId,
            TransactionType: transactionType,
            Transactions: transactionDtos,
            TotalPoints: totalPoints,
            Count: transactionDtos.Count
        );
    }

    public async Task<PointTransactionsByDateRangeDto> GetTransactionsByDateRangeAsync(
        Guid userId, 
        DateTime startDate, 
        DateTime endDate)
    {
        var transactions = (await _transactionRepository.GetByUserIdAsync(userId, 1000, 0))
            .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate)
            .ToList();
        
        var transactionDtos = transactions.Select(MapToDto).ToList();
        var totalEarned = transactionDtos.Where(t => t.Points > 0).Sum(t => t.Points);
        var totalDeducted = Math.Abs(transactionDtos.Where(t => t.Points < 0).Sum(t => t.Points));

        return new PointTransactionsByDateRangeDto(
            UserId: userId,
            StartDate: startDate,
            EndDate: endDate,
            Transactions: transactionDtos,
            TotalPointsEarned: totalEarned,
            TotalPointsDeducted: totalDeducted,
            Count: transactionDtos.Count
        );
    }

    // ========================================================================
    // PRIVATE HELPER METHODS
    // ========================================================================

    private async Task<bool> IsUserVerifiedAsync(Guid userId)
    {
        var verification = await _verificationRepository.GetByUserIdAsync(userId);
        return verification?.PhoneVerified == true || verification?.EmailVerified == true;
    }

    private bool IsValidNigerianPhoneNumber(string phoneNumber)
    {
        phoneNumber = phoneNumber.Replace(" ", "").Replace("-", "");
        
        if (phoneNumber.StartsWith("+234"))
            return phoneNumber.Length == 14;
        if (phoneNumber.StartsWith("234"))
            return phoneNumber.Length == 13;
        if (phoneNumber.StartsWith("0"))
            return phoneNumber.Length == 11;
        
        return false;
    }

    private static PointTransactionDto MapToDto(PointTransaction transaction)
    {
        return new PointTransactionDto(
            Id: transaction.Id,
            UserId: transaction.UserId,
            Points: transaction.Points,
            TransactionType: transaction.TransactionType,
            Description: transaction.Description,
            ReferenceId: transaction.ReferenceId,
            ReferenceType: transaction.ReferenceType,
            CreatedAt: transaction.CreatedAt
        );
    }

    private static RedemptionResponseDto MapRedemptionToDto(PointRedemption redemption)
    {
        return new RedemptionResponseDto(
            RedemptionId: redemption.Id,
            PointsRedeemed: redemption.PointsRedeemed,
            AmountInNaira: redemption.AmountInNaira,
            PhoneNumber: redemption.PhoneNumber,
            Status: redemption.Status,
            TransactionReference: redemption.TransactionReference,
            CreatedAt: redemption.CreatedAt
        );
    }

    private static PointRuleDto MapPointRuleToDto(PointRule rule)
    {
        return new PointRuleDto(
            Id: rule.Id,
            ActionType: rule.ActionType,
            Description: rule.Description,
            BasePointsNonVerified: rule.BasePointsNonVerified,
            BasePointsVerified: rule.BasePointsVerified,
            Conditions: rule.Conditions,
            IsActive: rule.IsActive
        );
    }

    private static PointMultiplierDto MapMultiplierToDto(PointMultiplier multiplier)
    {
        return new PointMultiplierDto(
            Id: multiplier.Id,
            Name: multiplier.Name,
            Description: multiplier.Description,
            Multiplier: multiplier.Multiplier,
            ActionTypes: multiplier.ActionTypes,
            StartDate: multiplier.StartDate,
            EndDate: multiplier.EndDate,
            IsActive: multiplier.IsActive,
            IsCurrentlyActive: multiplier.IsCurrentlyActive()
        );
    }
}