using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Repositories;

public class ReferralRepository : IReferralRepository
{
    private readonly string _connectionString;

    public ReferralRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")!;
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<Referral?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM referrals WHERE id = @Id;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Referral>(sql, new { Id = id });
    }

    public async Task<Referral?> GetByReferredUserIdAsync(Guid referredUserId)
    {
        const string sql = "SELECT * FROM referrals WHERE referred_user_id = @ReferredUserId;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Referral>(sql, new { ReferredUserId = referredUserId });
    }

    public async Task<IEnumerable<Referral>> GetByReferrerUserIdAsync(Guid referrerUserId)
    {
        const string sql = "SELECT * FROM referrals WHERE referrer_user_id = @ReferrerUserId ORDER BY created_at DESC;";
        await using var conn = CreateConnection();
        return await conn.QueryAsync<Referral>(sql, new { ReferrerUserId = referrerUserId });
    }

    public async Task<IEnumerable<Referral>> GetByReferralCodeAsync(string code)
    {
        const string sql = "SELECT * FROM referrals WHERE referral_code = @Code ORDER BY created_at DESC;";
        await using var conn = CreateConnection();
        return await conn.QueryAsync<Referral>(sql, new { Code = code });
    }

    public async Task<IEnumerable<Referral>> GetByStatusAsync(string status)
    {
        const string sql = "SELECT * FROM referrals WHERE status = @Status ORDER BY created_at DESC;";
        await using var conn = CreateConnection();
        return await conn.QueryAsync<Referral>(sql, new { Status = status });
    }

    public async Task<IEnumerable<Referral>> GetPendingByReferrerAsync(Guid referrerUserId)
    {
        const string sql = "SELECT * FROM referrals WHERE referrer_user_id = @ReferrerUserId AND status = 'pending' ORDER BY created_at DESC;";
        await using var conn = CreateConnection();
        return await conn.QueryAsync<Referral>(sql, new { ReferrerUserId = referrerUserId });
    }

    public async Task<IEnumerable<Referral>> GetCompletedByReferrerAsync(Guid referrerUserId)
    {
        const string sql = "SELECT * FROM referrals WHERE referrer_user_id = @ReferrerUserId AND status = 'completed' ORDER BY completed_at DESC;";
        await using var conn = CreateConnection();
        return await conn.QueryAsync<Referral>(sql, new { ReferrerUserId = referrerUserId });
    }

    public async Task<int> GetSuccessfulReferralCountAsync(Guid referrerUserId)
    {
        const string sql = "SELECT COUNT(*) FROM referrals WHERE referrer_user_id = @ReferrerUserId AND status = 'completed';";
        await using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, new { ReferrerUserId = referrerUserId });
    }

    public async Task<IEnumerable<Referral>> GetExpiredPendingAsync()
    {
        const string sql = "SELECT * FROM referrals WHERE status = 'pending' AND expires_at < @Now;";
        await using var conn = CreateConnection();
        return await conn.QueryAsync<Referral>(sql, new { Now = DateTime.UtcNow });
    }

    public async Task AddAsync(Referral referral)
    {
        const string sql = @"
            INSERT INTO referrals (id, referrer_user_id, referred_user_id, referral_code_id, referral_code, status, referred_email, referred_phone, referrer_reward_points, referred_reward_points, referrer_rewarded, referred_rewarded, completion_requirements, completed_requirements, expires_at, completed_at, created_at, updated_at)
            VALUES (@Id, @ReferrerUserId, @ReferredUserId, @ReferralCodeId, @ReferralCode, @Status, @ReferredEmail, @ReferredPhone, @ReferrerRewardPoints, @ReferredRewardPoints, @ReferrerRewarded, @ReferredRewarded, @CompletionRequirements::jsonb, @CompletedRequirements::jsonb, @ExpiresAt, @CompletedAt, @CreatedAt, @UpdatedAt);";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, referral);
    }

    public async Task UpdateAsync(Referral referral)
    {
        const string sql = @"
            UPDATE referrals SET
                referred_user_id = @ReferredUserId,
                status = @Status,
                referrer_reward_points = @ReferrerRewardPoints,
                referred_reward_points = @ReferredRewardPoints,
                referrer_rewarded = @ReferrerRewarded,
                referred_rewarded = @ReferredRewarded,
                completed_requirements = @CompletedRequirements::jsonb,
                completed_at = @CompletedAt,
                updated_at = @UpdatedAt
            WHERE id = @Id;";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, referral);
    }
}
