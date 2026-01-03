using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Repositories;

public class UserBadgeLevelRepository : IUserBadgeLevelRepository
{
    private readonly string _connectionString;

    public UserBadgeLevelRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")!;
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<UserBadgeLevel?> GetByUserIdAsync(Guid userId)
    {
        const string sql = "SELECT * FROM user_badge_levels WHERE user_id = @UserId;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<UserBadgeLevel>(sql, new { UserId = userId });
    }

    public async Task AddAsync(UserBadgeLevel level)
    {
        const string sql = @"
            INSERT INTO user_badge_levels (user_id, current_level, level_progress, total_badges_earned, updated_at)
            VALUES (@UserId, @CurrentLevel, @LevelProgress, @TotalBadgesEarned, @UpdatedAt);";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, level);
    }

    public async Task UpdateAsync(UserBadgeLevel level)
    {
        const string sql = @"
            UPDATE user_badge_levels SET
                current_level = @CurrentLevel,
                level_progress = @LevelProgress,
                total_badges_earned = @TotalBadgesEarned,
                updated_at = @UpdatedAt
            WHERE user_id = @UserId;";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, level);
    }

    public async Task UpsertAsync(UserBadgeLevel level)
    {
        const string sql = @"
            INSERT INTO user_badge_levels (user_id, current_level, level_progress, total_badges_earned, updated_at)
            VALUES (@UserId, @CurrentLevel, @LevelProgress, @TotalBadgesEarned, @UpdatedAt)
            ON CONFLICT (user_id) DO UPDATE SET
                current_level = EXCLUDED.current_level,
                level_progress = EXCLUDED.level_progress,
                total_badges_earned = EXCLUDED.total_badges_earned,
                updated_at = EXCLUDED.updated_at;";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, level);
    }
}
