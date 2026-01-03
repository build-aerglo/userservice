using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Repositories;

public class UserPointsRepository : IUserPointsRepository
{
    private readonly string _connectionString;

    public UserPointsRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")!;
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<UserPoints?> GetByUserIdAsync(Guid userId)
    {
        const string sql = "SELECT * FROM user_points WHERE user_id = @UserId;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<UserPoints>(sql, new { UserId = userId });
    }

    public async Task<IEnumerable<UserPoints>> GetTopByTotalPointsAsync(int count)
    {
        const string sql = "SELECT * FROM user_points ORDER BY total_points DESC LIMIT @Count;";
        await using var conn = CreateConnection();
        return await conn.QueryAsync<UserPoints>(sql, new { Count = count });
    }

    public async Task<IEnumerable<UserPoints>> GetTopByLifetimePointsAsync(int count)
    {
        const string sql = "SELECT * FROM user_points ORDER BY lifetime_points DESC LIMIT @Count;";
        await using var conn = CreateConnection();
        return await conn.QueryAsync<UserPoints>(sql, new { Count = count });
    }

    public async Task AddAsync(UserPoints points)
    {
        const string sql = @"
            INSERT INTO user_points (user_id, total_points, available_points, lifetime_points, redeemed_points, pending_points, last_earned_at, updated_at)
            VALUES (@UserId, @TotalPoints, @AvailablePoints, @LifetimePoints, @RedeemedPoints, @PendingPoints, @LastEarnedAt, @UpdatedAt);";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, points);
    }

    public async Task UpdateAsync(UserPoints points)
    {
        const string sql = @"
            UPDATE user_points SET
                total_points = @TotalPoints,
                available_points = @AvailablePoints,
                lifetime_points = @LifetimePoints,
                redeemed_points = @RedeemedPoints,
                pending_points = @PendingPoints,
                last_earned_at = @LastEarnedAt,
                updated_at = @UpdatedAt
            WHERE user_id = @UserId;";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, points);
    }

    public async Task UpsertAsync(UserPoints points)
    {
        const string sql = @"
            INSERT INTO user_points (user_id, total_points, available_points, lifetime_points, redeemed_points, pending_points, last_earned_at, updated_at)
            VALUES (@UserId, @TotalPoints, @AvailablePoints, @LifetimePoints, @RedeemedPoints, @PendingPoints, @LastEarnedAt, @UpdatedAt)
            ON CONFLICT (user_id) DO UPDATE SET
                total_points = EXCLUDED.total_points,
                available_points = EXCLUDED.available_points,
                lifetime_points = EXCLUDED.lifetime_points,
                redeemed_points = EXCLUDED.redeemed_points,
                pending_points = EXCLUDED.pending_points,
                last_earned_at = EXCLUDED.last_earned_at,
                updated_at = EXCLUDED.updated_at;";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, points);
    }
}
