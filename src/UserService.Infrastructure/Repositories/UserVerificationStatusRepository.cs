using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Repositories;

public class UserVerificationStatusRepository : IUserVerificationStatusRepository
{
    private readonly string _connectionString;

    public UserVerificationStatusRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")!;
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<UserVerificationStatus?> GetByUserIdAsync(Guid userId)
    {
        const string sql = "SELECT * FROM user_verification_status WHERE user_id = @UserId;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<UserVerificationStatus>(sql, new { UserId = userId });
    }

    public async Task<IEnumerable<UserVerificationStatus>> GetByVerificationLevelAsync(string level)
    {
        const string sql = "SELECT * FROM user_verification_status WHERE verification_level = @Level;";
        await using var conn = CreateConnection();
        return await conn.QueryAsync<UserVerificationStatus>(sql, new { Level = level });
    }

    public async Task AddAsync(UserVerificationStatus status)
    {
        const string sql = @"
            INSERT INTO user_verification_status (user_id, email_verified, email_verified_at, phone_verified, phone_verified_at, identity_verified, identity_verified_at, verification_level, updated_at)
            VALUES (@UserId, @EmailVerified, @EmailVerifiedAt, @PhoneVerified, @PhoneVerifiedAt, @IdentityVerified, @IdentityVerifiedAt, @VerificationLevel, @UpdatedAt);";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, status);
    }

    public async Task UpdateAsync(UserVerificationStatus status)
    {
        const string sql = @"
            UPDATE user_verification_status SET
                email_verified = @EmailVerified,
                email_verified_at = @EmailVerifiedAt,
                phone_verified = @PhoneVerified,
                phone_verified_at = @PhoneVerifiedAt,
                identity_verified = @IdentityVerified,
                identity_verified_at = @IdentityVerifiedAt,
                verification_level = @VerificationLevel,
                updated_at = @UpdatedAt
            WHERE user_id = @UserId;";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, status);
    }

    public async Task UpsertAsync(UserVerificationStatus status)
    {
        const string sql = @"
            INSERT INTO user_verification_status (user_id, email_verified, email_verified_at, phone_verified, phone_verified_at, identity_verified, identity_verified_at, verification_level, updated_at)
            VALUES (@UserId, @EmailVerified, @EmailVerifiedAt, @PhoneVerified, @PhoneVerifiedAt, @IdentityVerified, @IdentityVerifiedAt, @VerificationLevel, @UpdatedAt)
            ON CONFLICT (user_id) DO UPDATE SET
                email_verified = EXCLUDED.email_verified,
                email_verified_at = EXCLUDED.email_verified_at,
                phone_verified = EXCLUDED.phone_verified,
                phone_verified_at = EXCLUDED.phone_verified_at,
                identity_verified = EXCLUDED.identity_verified,
                identity_verified_at = EXCLUDED.identity_verified_at,
                verification_level = EXCLUDED.verification_level,
                updated_at = EXCLUDED.updated_at;";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, status);
    }
}
