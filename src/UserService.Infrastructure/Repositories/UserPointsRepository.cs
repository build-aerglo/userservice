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
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<UserPoints?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM user_points WHERE id = @Id;";
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<UserPoints>(sql, new { Id = id });
    }

    public async Task<UserPoints?> GetByUserIdAsync(Guid userId)
    {
        const string sql = "SELECT * FROM user_points WHERE user_id = @UserId;";
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<UserPoints>(sql, new { UserId = userId });
    }

    public async Task AddAsync(UserPoints userPoints)
    {
        const string sql = @"
             INSERT INTO user_points (id, user_id, total_points, current_streak, longest_streak, last_login_date, created_at, updated_at)
    VALUES (@Id, @UserId, @TotalPoints, @CurrentStreak, @LongestStreak, @LastLoginDate, @CreatedAt, @UpdatedAt);";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, userPoints);
    }

    public async Task UpdateAsync(UserPoints userPoints)
    {
        const string sql = @"
            UPDATE user_points
            SET total_points = @TotalPoints,
                current_streak = @CurrentStreak,
                longest_streak = @LongestStreak,
                 last_login_date = @LastLoginDate,
                updated_at = @UpdatedAt
            WHERE id = @Id;";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, userPoints);
    }

    public async Task<IEnumerable<UserPoints>> GetTopUsersByPointsAsync(int limit = 10)
    {
        const string sql = "SELECT * FROM user_points ORDER BY total_points DESC LIMIT @Limit;";
        using var conn = CreateConnection();
        return await conn.QueryAsync<UserPoints>(sql, new { Limit = limit });
    }

    public async Task<IEnumerable<UserPoints>> GetTopUsersByPointsInLocationAsync(string state, int limit = 10)
    {
        const string sql = @"
            SELECT up.* FROM user_points up
            INNER JOIN user_geolocations ug ON up.user_id = ug.user_id
            WHERE ug.state = @State AND ug.is_enabled = true
            ORDER BY up.total_points DESC
            LIMIT @Limit;";

        using var conn = CreateConnection();
        return await conn.QueryAsync<UserPoints>(sql, new { State = state, Limit = limit });
    }

    public async Task<int> GetUserRankAsync(Guid userId)
    {
        const string sql = @"
            SELECT COALESCE(rank, 0)::int FROM (
                SELECT user_id, RANK() OVER (ORDER BY total_points DESC) as rank
                FROM user_points
            ) ranked
            WHERE user_id = @UserId;";

        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, new { UserId = userId });
    }

    public async Task<int> GetUserRankInLocationAsync(Guid userId, string state)
    {
        const string sql = @"
            SELECT COALESCE(rank, 0)::int FROM (
                SELECT up.user_id, RANK() OVER (ORDER BY up.total_points DESC) as rank
                FROM user_points up
                INNER JOIN user_geolocations ug ON up.user_id = ug.user_id
                WHERE ug.state = @State AND ug.is_enabled = true
            ) ranked
            WHERE user_id = @UserId;";

        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, new { UserId = userId, State = state });
    }
}

public class PointTransactionRepository : IPointTransactionRepository
{
    private readonly string _connectionString;

    public PointTransactionRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")!;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<PointTransaction?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM point_transactions WHERE id = @Id;";
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<PointTransaction>(sql, new { Id = id });
    }

    public async Task<IEnumerable<PointTransaction>> GetByUserIdAsync(Guid userId, int limit = 50, int offset = 0)
    {
        const string sql = "SELECT * FROM point_transactions WHERE user_id = @UserId ORDER BY created_at DESC LIMIT @Limit OFFSET @Offset;";
        using var conn = CreateConnection();
        return await conn.QueryAsync<PointTransaction>(sql, new { UserId = userId, Limit = limit, Offset = offset });
    }

    public async Task AddAsync(PointTransaction transaction)
    {
        const string sql = @"
            INSERT INTO point_transactions (id, user_id, points, transaction_type, description, reference_id, reference_type, created_at)
            VALUES (@Id, @UserId, @Points, @TransactionType, @Description, @ReferenceId, @ReferenceType, @CreatedAt);";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, transaction);
    }

    public async Task<IEnumerable<PointTransaction>> GetByUserIdAndTypeAsync(Guid userId, string transactionType)
    {
        const string sql = "SELECT * FROM point_transactions WHERE user_id = @UserId AND transaction_type = @TransactionType ORDER BY created_at DESC;";
        using var conn = CreateConnection();
        return await conn.QueryAsync<PointTransaction>(sql, new { UserId = userId, TransactionType = transactionType });
    }

    public async Task<IEnumerable<PointTransaction>> GetByReferenceAsync(Guid referenceId, string referenceType)
    {
        const string sql = "SELECT * FROM point_transactions WHERE reference_id = @ReferenceId AND reference_type = @ReferenceType;";
        using var conn = CreateConnection();
        return await conn.QueryAsync<PointTransaction>(sql, new { ReferenceId = referenceId, ReferenceType = referenceType });
    }

    public async Task<decimal> GetTotalPointsByUserIdAsync(Guid userId)
    {
        const string sql = "SELECT COALESCE(SUM(points), 0) FROM point_transactions WHERE user_id = @UserId;";
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<decimal>(sql, new { UserId = userId });
    }

    public async Task<decimal> GetTotalPointsByUserIdAndDateRangeAsync(Guid userId, DateTime startDate, DateTime endDate)
    {
        const string sql = "SELECT COALESCE(SUM(points), 0) FROM point_transactions WHERE user_id = @UserId AND created_at >= @StartDate AND created_at <= @EndDate;";
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<decimal>(sql, new { UserId = userId, StartDate = startDate, EndDate = endDate });
    }
}
