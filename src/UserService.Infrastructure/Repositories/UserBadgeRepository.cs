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
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<UserBadge?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM user_badges WHERE id = @Id;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<UserBadge>(sql, new { Id = id });
    }

    public async Task<IEnumerable<UserBadge>> GetByUserIdAsync(Guid userId)
    {
        const string sql = "SELECT * FROM user_badges WHERE user_id = @UserId ORDER BY earned_at DESC;";
        await using var conn = CreateConnection();
        return await conn.QueryAsync<UserBadge>(sql, new { UserId = userId });
    }

    public async Task<UserBadge?> GetByUserAndBadgeAsync(Guid userId, Guid badgeId)
    {
        const string sql = "SELECT * FROM user_badges WHERE user_id = @UserId AND badge_id = @BadgeId;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<UserBadge>(sql, new { UserId = userId, BadgeId = badgeId });
    }

    public async Task<bool> HasBadgeAsync(Guid userId, Guid badgeId)
    {
        const string sql = "SELECT EXISTS(SELECT 1 FROM user_badges WHERE user_id = @UserId AND badge_id = @BadgeId);";
        await using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(sql, new { UserId = userId, BadgeId = badgeId });
    }

    public async Task<int> GetBadgeCountByUserAsync(Guid userId)
    {
        const string sql = "SELECT COUNT(*) FROM user_badges WHERE user_id = @UserId;";
        await using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, new { UserId = userId });
    }

    public async Task AddAsync(UserBadge userBadge)
    {
        const string sql = @"
            INSERT INTO user_badges (id, user_id, badge_id, earned_at, source, metadata)
            VALUES (@Id, @UserId, @BadgeId, @EarnedAt, @Source, @Metadata::jsonb)
            ON CONFLICT (user_id, badge_id) DO NOTHING;";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, userBadge);
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = "DELETE FROM user_badges WHERE id = @Id;";
        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { Id = id });
    }

    public async Task DeleteByUserIdAsync(Guid userId)
    {
        const string sql = "DELETE FROM user_badges WHERE user_id = @UserId;";
        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { UserId = userId });
    }
}
