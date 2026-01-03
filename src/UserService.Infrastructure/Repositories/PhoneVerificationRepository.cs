using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Repositories;

public class PhoneVerificationRepository : IPhoneVerificationRepository
{
    private readonly string _connectionString;

    public PhoneVerificationRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")!;
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<PhoneVerification?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM phone_verifications WHERE id = @Id;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<PhoneVerification>(sql, new { Id = id });
    }

    public async Task<PhoneVerification?> GetByUserIdAsync(Guid userId)
    {
        const string sql = "SELECT * FROM phone_verifications WHERE user_id = @UserId ORDER BY created_at DESC LIMIT 1;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<PhoneVerification>(sql, new { UserId = userId });
    }

    public async Task<PhoneVerification?> GetLatestByUserIdAsync(Guid userId)
    {
        const string sql = @"
            SELECT * FROM phone_verifications
            WHERE user_id = @UserId
            ORDER BY created_at DESC
            LIMIT 1;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<PhoneVerification>(sql, new { UserId = userId });
    }

    public async Task<PhoneVerification?> GetActiveByUserIdAsync(Guid userId)
    {
        const string sql = @"
            SELECT * FROM phone_verifications
            WHERE user_id = @UserId AND is_verified = false AND expires_at > @Now AND attempts < max_attempts
            ORDER BY created_at DESC
            LIMIT 1;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<PhoneVerification>(sql, new { UserId = userId, Now = DateTime.UtcNow });
    }

    public async Task AddAsync(PhoneVerification verification)
    {
        const string sql = @"
            INSERT INTO phone_verifications (id, user_id, phone_number, country_code, verification_code, verification_method, is_verified, verified_at, attempts, max_attempts, expires_at, created_at, updated_at)
            VALUES (@Id, @UserId, @PhoneNumber, @CountryCode, @VerificationCode, @VerificationMethod, @IsVerified, @VerifiedAt, @Attempts, @MaxAttempts, @ExpiresAt, @CreatedAt, @UpdatedAt);";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, verification);
    }

    public async Task UpdateAsync(PhoneVerification verification)
    {
        const string sql = @"
            UPDATE phone_verifications SET
                is_verified = @IsVerified,
                verified_at = @VerifiedAt,
                attempts = @Attempts,
                expires_at = @ExpiresAt,
                verification_code = @VerificationCode,
                updated_at = @UpdatedAt
            WHERE id = @Id;";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, verification);
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = "DELETE FROM phone_verifications WHERE id = @Id;";
        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { Id = id });
    }

    public async Task DeleteExpiredAsync()
    {
        const string sql = "DELETE FROM phone_verifications WHERE expires_at < @Now AND is_verified = false;";
        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { Now = DateTime.UtcNow });
    }
}
