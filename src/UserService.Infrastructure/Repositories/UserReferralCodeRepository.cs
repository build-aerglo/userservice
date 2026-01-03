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
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<UserReferralCode?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM user_referral_codes WHERE id = @Id;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<UserReferralCode>(sql, new { Id = id });
    }

    public async Task<UserReferralCode?> GetByUserIdAsync(Guid userId)
    {
        const string sql = "SELECT * FROM user_referral_codes WHERE user_id = @UserId;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<UserReferralCode>(sql, new { UserId = userId });
    }

    public async Task<UserReferralCode?> GetByCodeAsync(string code)
    {
        const string sql = @"
            SELECT * FROM user_referral_codes
            WHERE (referral_code = @Code OR custom_code = @Code) AND is_active = true;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<UserReferralCode>(sql, new { Code = code.ToUpperInvariant() });
    }

    public async Task<bool> CodeExistsAsync(string code)
    {
        const string sql = @"
            SELECT EXISTS(
                SELECT 1 FROM user_referral_codes
                WHERE referral_code = @Code OR custom_code = @Code
            );";
        await using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(sql, new { Code = code.ToUpperInvariant() });
    }

    public async Task<IEnumerable<UserReferralCode>> GetTopReferrersAsync(int count)
    {
        const string sql = @"
            SELECT * FROM user_referral_codes
            WHERE is_active = true
            ORDER BY successful_referrals DESC
            LIMIT @Count;";
        await using var conn = CreateConnection();
        return await conn.QueryAsync<UserReferralCode>(sql, new { Count = count });
    }

    public async Task AddAsync(UserReferralCode referralCode)
    {
        const string sql = @"
            INSERT INTO user_referral_codes (id, user_id, referral_code, custom_code, is_active, total_referrals, successful_referrals, pending_referrals, total_points_earned, created_at, updated_at)
            VALUES (@Id, @UserId, @ReferralCode, @CustomCode, @IsActive, @TotalReferrals, @SuccessfulReferrals, @PendingReferrals, @TotalPointsEarned, @CreatedAt, @UpdatedAt);";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, referralCode);
    }

    public async Task UpdateAsync(UserReferralCode referralCode)
    {
        const string sql = @"
            UPDATE user_referral_codes SET
                custom_code = @CustomCode,
                is_active = @IsActive,
                total_referrals = @TotalReferrals,
                successful_referrals = @SuccessfulReferrals,
                pending_referrals = @PendingReferrals,
                total_points_earned = @TotalPointsEarned,
                updated_at = @UpdatedAt
            WHERE id = @Id;";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, referralCode);
    }
}
