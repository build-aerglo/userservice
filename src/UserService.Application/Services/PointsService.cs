using UserService.Application.DTOs.Points;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Domain.Repositories;

namespace UserService.Application.Services;

public class PointsService(
    IUserPointsRepository userPointsRepository,
    IPointTransactionRepository pointTransactionRepository,
    IPointRuleRepository pointRuleRepository,
    IPointMultiplierRepository pointMultiplierRepository,
    IUserDailyPointsRepository userDailyPointsRepository,
    IUserBadgeLevelRepository userBadgeLevelRepository,
    IUserRepository userRepository
) : IPointsService
{
    public async Task<UserPointsDto> GetUserPointsAsync(Guid userId)
    {
        var points = await userPointsRepository.GetByUserIdAsync(userId);
        if (points == null)
            throw new UserPointsNotFoundException(userId);
        return MapToDto(points);
    }

    public async Task<UserPointsDto> GetOrCreateUserPointsAsync(Guid userId)
    {
        var points = await userPointsRepository.GetByUserIdAsync(userId);
        if (points == null)
        {
            points = new UserPoints(userId);
            await userPointsRepository.AddAsync(points);
        }
        return MapToDto(points);
    }

    public async Task<PointsSummaryDto> GetPointsSummaryAsync(Guid userId)
    {
        var points = await GetOrCreateUserPointsAsync(userId);
        var transactions = await GetTransactionHistoryAsync(userId, 10);
        var multipliers = await GetActiveMultipliersAsync();
        var badgeLevel = await userBadgeLevelRepository.GetByUserIdAsync(userId);

        return new PointsSummaryDto(
            points,
            badgeLevel?.CurrentLevel ?? "Pioneer",
            transactions,
            multipliers.Where(m => m.IsCurrentlyActive)
        );
    }

    public async Task<IEnumerable<PointTransactionDto>> GetTransactionHistoryAsync(Guid userId, int limit = 50, int offset = 0)
    {
        var transactions = await pointTransactionRepository.GetByUserIdAsync(userId, limit, offset);
        return transactions.Select(MapToTransactionDto);
    }

    public async Task<IEnumerable<PointTransactionDto>> GetTransactionsByTypeAsync(Guid userId, string transactionType)
    {
        var transactions = await pointTransactionRepository.GetByUserIdAndTypeAsync(userId, transactionType);
        return transactions.Select(MapToTransactionDto);
    }

    public async Task<IEnumerable<PointTransactionDto>> GetTransactionsByDateRangeAsync(Guid userId, DateTime startDate, DateTime endDate)
    {
        var transactions = await pointTransactionRepository.GetByUserIdAndDateRangeAsync(userId, startDate, endDate);
        return transactions.Select(MapToTransactionDto);
    }

    public async Task<EarnPointsResultDto> EarnPointsAsync(EarnPointsDto dto)
    {
        var rule = await pointRuleRepository.GetByActionTypeAsync(dto.ActionType);
        if (rule == null || !rule.IsActive)
            return new EarnPointsResultDto(false, 0, 0, $"No active rule for action '{dto.ActionType}'", 1.00m);

        // Check daily limit
        var dailyPoints = await userDailyPointsRepository.GetByUserActionDateAsync(dto.UserId, dto.ActionType, DateTime.UtcNow.Date);
        if (dailyPoints != null)
        {
            if (!dailyPoints.CanEarnMore(rule.MaxDailyOccurrences))
                return new EarnPointsResultDto(false, 0, 0, "Daily limit reached for this action", 1.00m);

            if (!dailyPoints.IsCooldownExpired(rule.CooldownMinutes))
            {
                var remaining = rule.CooldownMinutes.HasValue
                    ? TimeSpan.FromMinutes(rule.CooldownMinutes.Value) - (DateTime.UtcNow - dailyPoints.LastOccurrenceAt)
                    : TimeSpan.Zero;
                return new EarnPointsResultDto(false, 0, 0, $"Cooldown active. Try again in {remaining.TotalMinutes:F0} minutes", 1.00m);
            }
        }

        // Calculate multiplier
        decimal multiplier = 1.00m;
        if (rule.MultiplierEligible)
        {
            var activeMultiplier = await pointMultiplierRepository.GetHighestActiveMultiplierAsync(dto.ActionType);
            if (activeMultiplier != null)
                multiplier = activeMultiplier.Multiplier;
        }

        var pointsToEarn = (int)(rule.PointsValue * multiplier);

        // Get or create user points
        var userPoints = await userPointsRepository.GetByUserIdAsync(dto.UserId);
        if (userPoints == null)
        {
            userPoints = new UserPoints(dto.UserId);
            await userPointsRepository.AddAsync(userPoints);
        }

        userPoints.AddPoints(pointsToEarn);
        await userPointsRepository.UpdateAsync(userPoints);

        // Record transaction
        var transaction = PointTransaction.CreateEarn(
            dto.UserId,
            pointsToEarn,
            userPoints.TotalPoints,
            rule.Id,
            dto.Description ?? rule.Description,
            dto.ReferenceType,
            dto.ReferenceId,
            multiplier
        );
        await pointTransactionRepository.AddAsync(transaction);

        // Update daily tracking
        if (dailyPoints == null)
        {
            dailyPoints = new UserDailyPoints(dto.UserId, dto.ActionType);
            await userDailyPointsRepository.AddAsync(dailyPoints);
        }
        else
        {
            dailyPoints.IncrementOccurrence();
            await userDailyPointsRepository.UpdateAsync(dailyPoints);
        }

        return new EarnPointsResultDto(true, pointsToEarn, userPoints.TotalPoints, null, multiplier);
    }

    public async Task<PointTransactionDto> RedeemPointsAsync(RedeemPointsDto dto)
    {
        var userPoints = await userPointsRepository.GetByUserIdAsync(dto.UserId);
        if (userPoints == null)
            throw new UserPointsNotFoundException(dto.UserId);

        if (!userPoints.RedeemPoints(dto.Points))
            throw new InsufficientPointsException(dto.UserId, dto.Points, userPoints.AvailablePoints);

        await userPointsRepository.UpdateAsync(userPoints);

        var transaction = PointTransaction.CreateRedeem(
            dto.UserId,
            dto.Points,
            userPoints.TotalPoints,
            dto.Description,
            dto.ReferenceType,
            dto.ReferenceId
        );
        await pointTransactionRepository.AddAsync(transaction);

        return MapToTransactionDto(transaction);
    }

    public async Task<PointTransactionDto> AdjustPointsAsync(AdjustPointsDto dto)
    {
        var userPoints = await userPointsRepository.GetByUserIdAsync(dto.UserId);
        if (userPoints == null)
        {
            userPoints = new UserPoints(dto.UserId);
            await userPointsRepository.AddAsync(userPoints);
        }

        userPoints.AdjustPoints(dto.Adjustment);
        await userPointsRepository.UpdateAsync(userPoints);

        var transaction = PointTransaction.CreateAdjust(
            dto.UserId,
            dto.Adjustment,
            userPoints.TotalPoints,
            dto.Reason
        );
        await pointTransactionRepository.AddAsync(transaction);

        return MapToTransactionDto(transaction);
    }

    public async Task<IEnumerable<PointRuleDto>> GetAllRulesAsync()
    {
        var rules = await pointRuleRepository.GetAllAsync();
        return rules.Select(MapToRuleDto);
    }

    public async Task<IEnumerable<PointRuleDto>> GetActiveRulesAsync()
    {
        var rules = await pointRuleRepository.GetActiveAsync();
        return rules.Select(MapToRuleDto);
    }

    public async Task<PointRuleDto?> GetRuleByActionTypeAsync(string actionType)
    {
        var rule = await pointRuleRepository.GetByActionTypeAsync(actionType);
        return rule != null ? MapToRuleDto(rule) : null;
    }

    public async Task<PointRuleDto> CreateRuleAsync(CreatePointRuleDto dto)
    {
        var rule = new PointRule(
            dto.ActionType,
            dto.PointsValue,
            dto.Description,
            dto.MaxDailyOccurrences,
            dto.MaxTotalOccurrences,
            dto.CooldownMinutes,
            dto.MultiplierEligible
        );
        await pointRuleRepository.AddAsync(rule);
        return MapToRuleDto(rule);
    }

    public async Task<PointRuleDto> UpdateRuleAsync(Guid id, UpdatePointRuleDto dto)
    {
        var rule = await pointRuleRepository.GetByIdAsync(id);
        if (rule == null)
            throw new PointRuleNotFoundException(id);

        rule.Update(dto.PointsValue, dto.Description, dto.MaxDailyOccurrences, dto.CooldownMinutes);
        await pointRuleRepository.UpdateAsync(rule);
        return MapToRuleDto(rule);
    }

    public async Task<IEnumerable<PointMultiplierDto>> GetActiveMultipliersAsync()
    {
        var multipliers = await pointMultiplierRepository.GetActiveAsync();
        return multipliers.Select(m => MapToMultiplierDto(m));
    }

    public async Task<PointMultiplierDto> CreateMultiplierAsync(CreatePointMultiplierDto dto)
    {
        var multiplier = new PointMultiplier(
            dto.Name,
            dto.Multiplier,
            dto.StartsAt,
            dto.EndsAt,
            dto.Description,
            dto.ActionTypes
        );
        await pointMultiplierRepository.AddAsync(multiplier);
        return MapToMultiplierDto(multiplier);
    }

    public async Task<IEnumerable<LeaderboardEntryDto>> GetLeaderboardAsync(int count = 10)
    {
        var topUsers = await userPointsRepository.GetTopByTotalPointsAsync(count);
        var result = new List<LeaderboardEntryDto>();
        var rank = 1;

        foreach (var up in topUsers)
        {
            var user = await userRepository.GetByIdAsync(up.UserId);
            var badgeLevel = await userBadgeLevelRepository.GetByUserIdAsync(up.UserId);

            result.Add(new LeaderboardEntryDto(
                rank++,
                up.UserId,
                user?.Username,
                up.TotalPoints,
                up.LifetimePoints,
                badgeLevel?.CurrentLevel ?? "Pioneer"
            ));
        }

        return result;
    }

    public async Task<int> GetUserRankAsync(Guid userId)
    {
        var topUsers = await userPointsRepository.GetTopByTotalPointsAsync(1000);
        var userList = topUsers.ToList();
        var index = userList.FindIndex(u => u.UserId == userId);
        return index >= 0 ? index + 1 : userList.Count + 1;
    }

    private static UserPointsDto MapToDto(UserPoints points) => new(
        points.UserId,
        points.TotalPoints,
        points.AvailablePoints,
        points.LifetimePoints,
        points.RedeemedPoints,
        points.PendingPoints,
        points.LastEarnedAt
    );

    private static PointTransactionDto MapToTransactionDto(PointTransaction t) => new(
        t.Id,
        t.UserId,
        t.TransactionType,
        t.Points,
        t.BalanceAfter,
        t.Description,
        t.ReferenceType,
        t.ReferenceId,
        t.Multiplier,
        t.ExpiresAt,
        t.CreatedAt
    );

    private static PointRuleDto MapToRuleDto(PointRule r) => new(
        r.Id,
        r.ActionType,
        r.PointsValue,
        r.Description,
        r.MaxDailyOccurrences,
        r.MaxTotalOccurrences,
        r.CooldownMinutes,
        r.IsActive,
        r.MultiplierEligible
    );

    private static PointMultiplierDto MapToMultiplierDto(PointMultiplier m) => new(
        m.Id,
        m.Name,
        m.Description,
        m.Multiplier,
        m.ActionTypes,
        m.StartsAt,
        m.EndsAt,
        m.IsActive,
        m.IsCurrentlyActive()
    );
}
