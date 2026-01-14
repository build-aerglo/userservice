using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Repositories;

public class UserReferralCodeRepository : IUserReferralCodeRepository
{
    private readonly string _connectionString;

    public UserReferralCodeRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")!;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<UserReferralCode?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM user_referral_codes WHERE id = @Id;";
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<UserReferralCode>(sql, new { Id = id });
    }

    public async Task<UserReferralCode?> GetByUserIdAsync(Guid userId)
    {
        const string sql = "SELECT * FROM user_referral_codes WHERE user_id = @UserId;";
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<UserReferralCode>(sql, new { UserId = userId });
    }

    public async Task<UserReferralCode?> GetByCodeAsync(string code)
    {
        const string sql = "SELECT * FROM user_referral_codes WHERE code = @Code;";
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<UserReferralCode>(sql, new { Code = code.ToUpperInvariant() });
    }

    public async Task AddAsync(UserReferralCode referralCode)
    {
        const string sql = @"
            INSERT INTO user_referral_codes (id, user_id, code, total_referrals, successful_referrals, is_active, created_at, updated_at)
            VALUES (@Id, @UserId, @Code, @TotalReferrals, @SuccessfulReferrals, @IsActive, @CreatedAt, @UpdatedAt);";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, referralCode);
    }

    public async Task UpdateAsync(UserReferralCode referralCode)
    {
        const string sql = @"
            UPDATE user_referral_codes
            SET total_referrals = @TotalReferrals,
                successful_referrals = @SuccessfulReferrals,
                is_active = @IsActive,
                updated_at = @UpdatedAt
            WHERE id = @Id;";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, referralCode);
    }

    public async Task<bool> CodeExistsAsync(string code)
    {
        const string sql = "SELECT EXISTS(SELECT 1 FROM user_referral_codes WHERE code = @Code);";
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(sql, new { Code = code.ToUpperInvariant() });
    }

    public async Task<IEnumerable<UserReferralCode>> GetTopReferrersAsync(int limit = 10)
    {
        const string sql = "SELECT * FROM user_referral_codes WHERE is_active = true ORDER BY successful_referrals DESC LIMIT @Limit;";
        using var conn = CreateConnection();
        return await conn.QueryAsync<UserReferralCode>(sql, new { Limit = limit });
    }
}

public class ReferralRepository : IReferralRepository
{
    private readonly string _connectionString;

    public ReferralRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")!;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<Referral?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM referrals WHERE id = @Id;";
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Referral>(sql, new { Id = id });
    }

    public async Task<Referral?> GetByReferredUserIdAsync(Guid referredUserId)
    {
        const string sql = "SELECT * FROM referrals WHERE referred_user_id = @ReferredUserId;";
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Referral>(sql, new { ReferredUserId = referredUserId });
    }

    public async Task<IEnumerable<Referral>> GetByReferrerIdAsync(Guid referrerId)
    {
        const string sql = "SELECT * FROM referrals WHERE referrer_id = @ReferrerId ORDER BY created_at DESC;";
        using var conn = CreateConnection();
        return await conn.QueryAsync<Referral>(sql, new { ReferrerId = referrerId });
    }

    public async Task AddAsync(Referral referral)
    {
        const string sql = @"
            INSERT INTO referrals (id, referrer_id, referred_user_id, referral_code, status, approved_review_count, points_awarded, qualified_at, completed_at, created_at, updated_at)
            VALUES (@Id, @ReferrerId, @ReferredUserId, @ReferralCode, @Status, @ApprovedReviewCount, @PointsAwarded, @QualifiedAt, @CompletedAt, @CreatedAt, @UpdatedAt);";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, referral);
    }

    public async Task UpdateAsync(Referral referral)
    {
        const string sql = @"
            UPDATE referrals
            SET status = @Status,
                approved_review_count = @ApprovedReviewCount,
                points_awarded = @PointsAwarded,
                qualified_at = @QualifiedAt,
                completed_at = @CompletedAt,
                updated_at = @UpdatedAt
            WHERE id = @Id;";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, referral);
    }

    public async Task<IEnumerable<Referral>> GetByStatusAsync(string status)
    {
        const string sql = "SELECT * FROM referrals WHERE status = @Status ORDER BY created_at DESC;";
        using var conn = CreateConnection();
        return await conn.QueryAsync<Referral>(sql, new { Status = status });
    }

    public async Task<IEnumerable<Referral>> GetQualifiedButNotCompletedAsync()
    {
        const string sql = "SELECT * FROM referrals WHERE status = 'qualified' AND points_awarded = false ORDER BY qualified_at ASC;";
        using var conn = CreateConnection();
        return await conn.QueryAsync<Referral>(sql);
    }

    public async Task<int> GetSuccessfulReferralCountAsync(Guid referrerId)
    {
        const string sql = "SELECT COUNT(*) FROM referrals WHERE referrer_id = @ReferrerId AND status = 'completed';";
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, new { ReferrerId = referrerId });
    }

    public async Task<IEnumerable<Referral>> GetByReferralCodeAsync(string code)
    {
        const string sql = "SELECT * FROM referrals WHERE referral_code = @Code ORDER BY created_at DESC;";
        using var conn = CreateConnection();
        return await conn.QueryAsync<Referral>(sql, new { Code = code.ToUpperInvariant() });
    }
}
