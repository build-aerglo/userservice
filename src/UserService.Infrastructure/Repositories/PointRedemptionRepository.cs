using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Repositories;

public class PointRedemptionRepository : IPointRedemptionRepository
{
    private readonly string _connectionString;

    public PointRedemptionRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")!;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<PointRedemption?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM point_redemptions WHERE id = @Id;";
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<PointRedemption>(sql, new { Id = id });
    }

    public async Task<IEnumerable<PointRedemption>> GetByUserIdAsync(Guid userId, int limit = 50, int offset = 0)
    {
        const string sql = @"
            SELECT * FROM point_redemptions 
            WHERE user_id = @UserId 
            ORDER BY created_at DESC 
            LIMIT @Limit OFFSET @Offset;";
        
        using var conn = CreateConnection();
        return await conn.QueryAsync<PointRedemption>(sql, new { UserId = userId, Limit = limit, Offset = offset });
    }

    public async Task<IEnumerable<PointRedemption>> GetPendingRedemptionsAsync()
    {
        const string sql = "SELECT * FROM point_redemptions WHERE status = 'pending' ORDER BY created_at;";
        using var conn = CreateConnection();
        return await conn.QueryAsync<PointRedemption>(sql);
    }

    public async Task AddAsync(PointRedemption redemption)
    {
        const string sql = @"
            INSERT INTO point_redemptions (
                id, user_id, points_redeemed, amount_in_naira, phone_number,
                status, transaction_reference, provider_response,
                created_at, updated_at, completed_at
            )
            VALUES (
                @Id, @UserId, @PointsRedeemed, @AmountInNaira, @PhoneNumber,
                @Status, @TransactionReference, @ProviderResponse,
                @CreatedAt, @UpdatedAt, @CompletedAt
            );";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, redemption);
    }

    public async Task UpdateAsync(PointRedemption redemption)
    {
        const string sql = @"
            UPDATE point_redemptions
            SET status = @Status,
                transaction_reference = @TransactionReference,
                provider_response = @ProviderResponse,
                updated_at = @UpdatedAt,
                completed_at = @CompletedAt
            WHERE id = @Id;";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, redemption);
    }
}