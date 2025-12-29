using UserService.Application.DTOs.Points;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Domain.Repositories;

namespace UserService.Application.Services;

public class PointsService(
    IUserPointsRepository pointsRepository,
    IPointTransactionRepository transactionRepository,
    IUserRepository userRepository,
    IUserBadgeRepository badgeRepository,
    IUserVerificationRepository verificationRepository
) : IPointsService
{
    // Points constants based on business rules
    private const decimal StarsOnlyPointsNonVerified = 2m;
    private const decimal StarsOnlyPointsVerified = 3m;
    private const decimal HeaderPoints = 1m;
    private const decimal BodyShortPointsNonVerified = 2m;    // ≤50 chars
    private const decimal BodyShortPointsVerified = 3m;
    private const decimal BodyMediumPointsNonVerified = 3m;   // 51-150 chars
    private const decimal BodyMediumPointsVerified = 4.5m;
    private const decimal BodyLongPointsNonVerified = 5m;     // 151-500 chars
    private const decimal BodyLongPointsVerified = 6.5m;
    private const decimal BodyExtraLongPointsNonVerified = 6m; // 500+ chars
    private const decimal BodyExtraLongPointsVerified = 7.5m;
    private const decimal ImagePointsNonVerified = 4m;
    private const decimal ImagePointsVerified = 6m;
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
    private const decimal LoyaltyBonusNonVerified = 500m;  // 500 days + 10 reviews
    private const decimal LoyaltyBonusVerified = 750m;

    public async Task<UserPointsDto> GetUserPointsAsync(Guid userId)
    {
        var userPoints = await pointsRepository.GetByUserIdAsync(userId);
        if (userPoints is null)
        {
            // Initialize points for user if not exists
            await InitializeUserPointsAsync(userId);
            userPoints = await pointsRepository.GetByUserIdAsync(userId);
        }

        var rank = await pointsRepository.GetUserRankAsync(userId);

        return new UserPointsDto(
            UserId: userId,
            TotalPoints: userPoints!.TotalPoints,
            Tier: userPoints.GetTier(),
            CurrentStreak: userPoints.CurrentStreak,
            LongestStreak: userPoints.LongestStreak,
            LastActivityDate: userPoints.LastActivityDate,
            Rank: rank
        );
    }

    public async Task<PointsHistoryResponseDto> GetPointsHistoryAsync(Guid userId, int limit = 50, int offset = 0)
    {
        var userPoints = await pointsRepository.GetByUserIdAsync(userId);
        if (userPoints is null)
            throw new UserPointsNotFoundException(userId);

        var transactions = await transactionRepository.GetByUserIdAsync(userId, limit, offset);
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

        var userPoints = await pointsRepository.GetByUserIdAsync(dto.UserId);
        if (userPoints is null)
        {
            await InitializeUserPointsAsync(dto.UserId);
            userPoints = await pointsRepository.GetByUserIdAsync(dto.UserId);
        }

        // Add points
        userPoints!.AddPoints(dto.Points);
        await pointsRepository.UpdateAsync(userPoints);

        // Create transaction record
        var transaction = new PointTransaction(
            userId: dto.UserId,
            points: dto.Points,
            transactionType: dto.TransactionType,
            description: dto.Description,
            referenceId: dto.ReferenceId,
            referenceType: dto.ReferenceType
        );

        await transactionRepository.AddAsync(transaction);

        return MapToDto(transaction);
    }

    public async Task<PointTransactionDto> DeductPointsAsync(DeductPointsDto dto)
    {
        if (dto.Points <= 0)
            throw new InvalidPointsAmountException(dto.Points);

        var userPoints = await pointsRepository.GetByUserIdAsync(dto.UserId);
        if (userPoints is null)
            throw new UserPointsNotFoundException(dto.UserId);

        if (userPoints.TotalPoints < dto.Points)
            throw new InsufficientPointsException(dto.UserId, dto.Points, userPoints.TotalPoints);

        // Deduct points
        userPoints.DeductPoints(dto.Points);
        await pointsRepository.UpdateAsync(userPoints);

        // Create transaction record with negative points
        var transaction = new PointTransaction(
            userId: dto.UserId,
            points: -dto.Points,
            transactionType: TransactionTypes.Deduct,
            description: dto.Reason
        );

        await transactionRepository.AddAsync(transaction);

        return MapToDto(transaction);
    }

    public async Task<ReviewPointsResultDto> CalculateReviewPointsAsync(CalculateReviewPointsDto dto)
    {
        var isVerified = await IsUserVerifiedAsync(dto.UserId);

        // Use the dto's verification status if provided, otherwise check the database
        var verified = dto.IsVerifiedUser || isVerified;

        decimal starPoints = 0;
        decimal headerPoints = 0;
        decimal bodyPoints = 0;
        decimal imagePoints = 0;

        // Stars only points
        if (dto.HasStars)
        {
            starPoints = verified ? StarsOnlyPointsVerified : StarsOnlyPointsNonVerified;
        }

        // Header points
        if (dto.HasHeader)
        {
            headerPoints = HeaderPoints;
        }

        // Body points based on length
        if (dto.BodyLength > 0)
        {
            bodyPoints = (dto.BodyLength, verified) switch
            {
                ( <= 50, false) => BodyShortPointsNonVerified,
                ( <= 50, true) => BodyShortPointsVerified,
                ( <= 150, false) => BodyMediumPointsNonVerified,
                ( <= 150, true) => BodyMediumPointsVerified,
                ( <= 500, false) => BodyLongPointsNonVerified,
                ( <= 500, true) => BodyLongPointsVerified,
                (_, false) => BodyExtraLongPointsNonVerified,
                (_, true) => BodyExtraLongPointsVerified
            };
        }

        // Image points (max 3 images counted)
        var imagesToCount = Math.Min(dto.ImageCount, MaxImagePoints);
        imagePoints = imagesToCount * (verified ? ImagePointsVerified : ImagePointsNonVerified);

        var totalPoints = starPoints + headerPoints + bodyPoints + imagePoints;

        var breakdown = $"Stars: {starPoints}, Header: {headerPoints}, Body: {bodyPoints}, Images: {imagePoints}";
        if (verified)
            breakdown += " (Verified bonus applied)";

        return new ReviewPointsResultDto(
            TotalPoints: totalPoints,
            StarPoints: starPoints,
            HeaderPoints: headerPoints,
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

    public async Task UpdateStreakAsync(Guid userId, DateTime activityDate)
    {
        var userPoints = await pointsRepository.GetByUserIdAsync(userId);
        if (userPoints is null)
        {
            await InitializeUserPointsAsync(userId);
            userPoints = await pointsRepository.GetByUserIdAsync(userId);
        }

        userPoints!.UpdateStreak(activityDate);
        await pointsRepository.UpdateAsync(userPoints);
    }

    public async Task<PointTransactionDto?> CheckAndAwardStreakMilestoneAsync(Guid userId)
    {
        var userPoints = await pointsRepository.GetByUserIdAsync(userId);
        if (userPoints is null || userPoints.CurrentStreak != 100)
            return null;

        // Check if already awarded for this milestone
        var existingTransactions = await transactionRepository.GetByUserIdAndTypeAsync(userId, TransactionTypes.Milestone);
        if (existingTransactions.Any(t => t.Description.Contains("100-day streak")))
            return null;

        var isVerified = await IsUserVerifiedAsync(userId);
        var points = isVerified ? Streak100DaysVerified : Streak100DaysNonVerified;

        return await AwardPointsAsync(new AwardPointsDto(
            UserId: userId,
            Points: points,
            TransactionType: TransactionTypes.Milestone,
            Description: "100-day streak milestone bonus"
        ));
    }

    public async Task<PointTransactionDto?> CheckAndAwardReviewMilestoneAsync(Guid userId, int totalReviews)
    {
        if (totalReviews != 25)
            return null;

        // Check if already awarded
        var existingTransactions = await transactionRepository.GetByUserIdAndTypeAsync(userId, TransactionTypes.Milestone);
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

        // Check if already awarded
        var existingTransactions = await transactionRepository.GetByUserIdAndTypeAsync(userId, TransactionTypes.Milestone);
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

    public async Task<LeaderboardResponseDto> GetLeaderboardAsync(int limit = 10)
    {
        var topUsers = await pointsRepository.GetTopUsersByPointsAsync(limit);
        var entries = new List<LeaderboardEntryDto>();

        int rank = 1;
        foreach (var userPoints in topUsers)
        {
            var user = await userRepository.GetByIdAsync(userPoints.UserId);
            var badgeCount = await badgeRepository.GetBadgeCountByUserIdAsync(userPoints.UserId);

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
        var topUsers = await pointsRepository.GetTopUsersByPointsInLocationAsync(state, limit);
        var entries = new List<LeaderboardEntryDto>();

        int rank = 1;
        foreach (var userPoints in topUsers)
        {
            var user = await userRepository.GetByIdAsync(userPoints.UserId);
            var badgeCount = await badgeRepository.GetBadgeCountByUserIdAsync(userPoints.UserId);

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
        var userPoints = await pointsRepository.GetByUserIdAsync(userId);
        return userPoints?.GetTier() ?? PointTiers.Bronze;
    }

    public async Task InitializeUserPointsAsync(Guid userId)
    {
        var existingPoints = await pointsRepository.GetByUserIdAsync(userId);
        if (existingPoints is not null)
            return;

        var userPoints = new UserPoints(userId);
        await pointsRepository.AddAsync(userPoints);
    }

    private async Task<bool> IsUserVerifiedAsync(Guid userId)
    {
        var verification = await verificationRepository.GetByUserIdAsync(userId);
        return verification?.IsVerified ?? false;
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
}
