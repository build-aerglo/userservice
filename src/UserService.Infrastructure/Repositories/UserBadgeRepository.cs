using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Repositories;

public class UserBadgeRepository : IUserBadgeRepository
{
    private readonly string _connectionString;

    public UserBadgeRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")!;
        
        // Configure Dapper to map snake_case columns to PascalCase properties
        ConfigureDapperMapping();
    }

    private static void ConfigureDapperMapping()
    {
        // This tells Dapper to automatically convert snake_case to PascalCase
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<UserBadge?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM user_badges WHERE id = @Id;";
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<UserBadge>(sql, new { Id = id });
    }

    public async Task<IEnumerable<UserBadge>> GetByUserIdAsync(Guid userId)
    {
        const string sql = "SELECT * FROM user_badges WHERE user_id = @UserId ORDER BY earned_at DESC;";
        using var conn = CreateConnection();
        return await conn.QueryAsync<UserBadge>(sql, new { UserId = userId });
    }

    public async Task<IEnumerable<UserBadge>> GetActiveByUserIdAsync(Guid userId)
    {
        const string sql = "SELECT * FROM user_badges WHERE user_id = @UserId AND is_active = true ORDER BY earned_at DESC;";
        using var conn = CreateConnection();
        return await conn.QueryAsync<UserBadge>(sql, new { UserId = userId });
    }

    public async Task<UserBadge?> GetByUserIdAndTypeAsync(Guid userId, string badgeType, string? location = null, string? category = null)
    {
        var sql = "SELECT * FROM user_badges WHERE user_id = @UserId AND badge_type = @BadgeType";

        if (location != null)
            sql += " AND location = @Location";
        else
            sql += " AND location IS NULL";

        if (category != null)
            sql += " AND category = @Category";
        else
            sql += " AND category IS NULL";

        sql += ";";

        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<UserBadge>(sql, new { UserId = userId, BadgeType = badgeType, Location = location, Category = category });
    }

    public async Task AddAsync(UserBadge badge)
    {
        const string sql = @"
            INSERT INTO user_badges (id, user_id, badge_type, location, category, earned_at, is_active, created_at, updated_at)
            VALUES (@Id, @UserId, @BadgeType, @Location, @Category, @EarnedAt, @IsActive, @CreatedAt, @UpdatedAt);";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, badge);
    }

    public async Task UpdateAsync(UserBadge badge)
    {
        const string sql = @"
            UPDATE user_badges
            SET is_active = @IsActive, updated_at = @UpdatedAt
            WHERE id = @Id;";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, badge);
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = "DELETE FROM user_badges WHERE id = @Id;";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<IEnumerable<UserBadge>> GetByTypeAsync(string badgeType)
    {
        const string sql = "SELECT * FROM user_badges WHERE badge_type = @BadgeType AND is_active = true;";
        using var conn = CreateConnection();
        return await conn.QueryAsync<UserBadge>(sql, new { BadgeType = badgeType });
    }

    public async Task<int> GetBadgeCountByUserIdAsync(Guid userId)
    {
        const string sql = "SELECT COUNT(*) FROM user_badges WHERE user_id = @UserId AND is_active = true;";
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, new { UserId = userId });
    }

    public async Task DeactivateAllTierBadgesAsync(Guid userId)
    {
        const string sql = @"
            UPDATE user_badges
            SET is_active = false, updated_at = @UpdatedAt
            WHERE user_id = @UserId AND badge_type IN ('newbie', 'expert', 'pro');";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { UserId = userId, UpdatedAt = DateTime.UtcNow });
    }
}