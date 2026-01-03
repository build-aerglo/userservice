using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Repositories;

public class PointTransactionRepository : IPointTransactionRepository
{
    private readonly string _connectionString;

    public PointTransactionRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")!;
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<PointTransaction?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM point_transactions WHERE id = @Id;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<PointTransaction>(sql, new { Id = id });
    }

    public async Task<IEnumerable<PointTransaction>> GetByUserIdAsync(Guid userId, int limit = 50, int offset = 0)
    {
        const string sql = @"
            SELECT * FROM point_transactions
            WHERE user_id = @UserId
            ORDER BY created_at DESC
            LIMIT @Limit OFFSET @Offset;";
        await using var conn = CreateConnection();
        return await conn.QueryAsync<PointTransaction>(sql, new { UserId = userId, Limit = limit, Offset = offset });
    }

    public async Task<IEnumerable<PointTransaction>> GetByUserIdAndTypeAsync(Guid userId, string transactionType)
    {
        const string sql = @"
            SELECT * FROM point_transactions
            WHERE user_id = @UserId AND transaction_type = @TransactionType
            ORDER BY created_at DESC;";
        await using var conn = CreateConnection();
        return await conn.QueryAsync<PointTransaction>(sql, new { UserId = userId, TransactionType = transactionType });
    }

    public async Task<IEnumerable<PointTransaction>> GetByUserIdAndDateRangeAsync(Guid userId, DateTime startDate, DateTime endDate)
    {
        const string sql = @"
            SELECT * FROM point_transactions
            WHERE user_id = @UserId AND created_at >= @StartDate AND created_at <= @EndDate
            ORDER BY created_at DESC;";
        await using var conn = CreateConnection();
        return await conn.QueryAsync<PointTransaction>(sql, new { UserId = userId, StartDate = startDate, EndDate = endDate });
    }

    public async Task<int> GetTotalPointsEarnedByUserAsync(Guid userId)
    {
        const string sql = @"
            SELECT COALESCE(SUM(points), 0) FROM point_transactions
            WHERE user_id = @UserId AND transaction_type = 'earn';";
        await using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, new { UserId = userId });
    }

    public async Task<IEnumerable<PointTransaction>> GetExpiringPointsAsync(DateTime beforeDate)
    {
        const string sql = @"
            SELECT * FROM point_transactions
            WHERE expires_at IS NOT NULL AND expires_at <= @BeforeDate AND transaction_type = 'earn'
            ORDER BY expires_at;";
        await using var conn = CreateConnection();
        return await conn.QueryAsync<PointTransaction>(sql, new { BeforeDate = beforeDate });
    }

    public async Task AddAsync(PointTransaction transaction)
    {
        const string sql = @"
            INSERT INTO point_transactions (id, user_id, rule_id, transaction_type, points, balance_after, description, reference_type, reference_id, multiplier, expires_at, metadata, created_at)
            VALUES (@Id, @UserId, @RuleId, @TransactionType, @Points, @BalanceAfter, @Description, @ReferenceType, @ReferenceId, @Multiplier, @ExpiresAt, @Metadata::jsonb, @CreatedAt);";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, transaction);
    }
}
