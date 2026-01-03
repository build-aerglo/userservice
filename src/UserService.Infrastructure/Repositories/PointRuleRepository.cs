using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Repositories;

public class PointRuleRepository : IPointRuleRepository
{
    private readonly string _connectionString;

    public PointRuleRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")!;
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<PointRule?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM point_rules WHERE id = @Id;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<PointRule>(sql, new { Id = id });
    }

    public async Task<PointRule?> GetByActionTypeAsync(string actionType)
    {
        const string sql = "SELECT * FROM point_rules WHERE action_type = @ActionType;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<PointRule>(sql, new { ActionType = actionType });
    }

    public async Task<IEnumerable<PointRule>> GetAllAsync()
    {
        const string sql = "SELECT * FROM point_rules ORDER BY action_type;";
        await using var conn = CreateConnection();
        return await conn.QueryAsync<PointRule>(sql);
    }

    public async Task<IEnumerable<PointRule>> GetActiveAsync()
    {
        const string sql = "SELECT * FROM point_rules WHERE is_active = true ORDER BY action_type;";
        await using var conn = CreateConnection();
        return await conn.QueryAsync<PointRule>(sql);
    }

    public async Task AddAsync(PointRule rule)
    {
        const string sql = @"
            INSERT INTO point_rules (id, action_type, points_value, description, max_daily_occurrences, max_total_occurrences, cooldown_minutes, is_active, multiplier_eligible, created_at, updated_at)
            VALUES (@Id, @ActionType, @PointsValue, @Description, @MaxDailyOccurrences, @MaxTotalOccurrences, @CooldownMinutes, @IsActive, @MultiplierEligible, @CreatedAt, @UpdatedAt);";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, rule);
    }

    public async Task UpdateAsync(PointRule rule)
    {
        const string sql = @"
            UPDATE point_rules SET
                points_value = @PointsValue,
                description = @Description,
                max_daily_occurrences = @MaxDailyOccurrences,
                max_total_occurrences = @MaxTotalOccurrences,
                cooldown_minutes = @CooldownMinutes,
                is_active = @IsActive,
                multiplier_eligible = @MultiplierEligible,
                updated_at = @UpdatedAt
            WHERE id = @Id;";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, rule);
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = "DELETE FROM point_rules WHERE id = @Id;";
        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { Id = id });
    }
}
