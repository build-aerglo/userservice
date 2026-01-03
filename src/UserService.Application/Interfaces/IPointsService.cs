using UserService.Application.DTOs.Points;

namespace UserService.Application.Interfaces;

public interface IPointsService
{
    // User points
    Task<UserPointsDto> GetUserPointsAsync(Guid userId);
    Task<UserPointsDto> GetOrCreateUserPointsAsync(Guid userId);
    Task<PointsSummaryDto> GetPointsSummaryAsync(Guid userId);

    // Point transactions
    Task<IEnumerable<PointTransactionDto>> GetTransactionHistoryAsync(Guid userId, int limit = 50, int offset = 0);
    Task<IEnumerable<PointTransactionDto>> GetTransactionsByTypeAsync(Guid userId, string transactionType);
    Task<IEnumerable<PointTransactionDto>> GetTransactionsByDateRangeAsync(Guid userId, DateTime startDate, DateTime endDate);

    // Earn/redeem points
    Task<EarnPointsResultDto> EarnPointsAsync(EarnPointsDto dto);
    Task<PointTransactionDto> RedeemPointsAsync(RedeemPointsDto dto);
    Task<PointTransactionDto> AdjustPointsAsync(AdjustPointsDto dto);

    // Point rules
    Task<IEnumerable<PointRuleDto>> GetAllRulesAsync();
    Task<IEnumerable<PointRuleDto>> GetActiveRulesAsync();
    Task<PointRuleDto?> GetRuleByActionTypeAsync(string actionType);
    Task<PointRuleDto> CreateRuleAsync(CreatePointRuleDto dto);
    Task<PointRuleDto> UpdateRuleAsync(Guid id, UpdatePointRuleDto dto);

    // Multipliers
    Task<IEnumerable<PointMultiplierDto>> GetActiveMultipliersAsync();
    Task<PointMultiplierDto> CreateMultiplierAsync(CreatePointMultiplierDto dto);

    // Leaderboard
    Task<IEnumerable<LeaderboardEntryDto>> GetLeaderboardAsync(int count = 10);
    Task<int> GetUserRankAsync(Guid userId);
}
