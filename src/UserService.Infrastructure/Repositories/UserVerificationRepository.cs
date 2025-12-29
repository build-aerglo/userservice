using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Repositories;

public class UserVerificationRepository : IUserVerificationRepository
{
    private readonly string _connectionString;

    public UserVerificationRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")!;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<UserVerification?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM user_verifications WHERE id = @Id;";
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<UserVerification>(sql, new { Id = id });
    }

    public async Task<UserVerification?> GetByUserIdAsync(Guid userId)
    {
        const string sql = "SELECT * FROM user_verifications WHERE user_id = @UserId;";
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<UserVerification>(sql, new { UserId = userId });
    }

    public async Task AddAsync(UserVerification verification)
    {
        const string sql = @"
            INSERT INTO user_verifications (id, user_id, phone_verified, email_verified, phone_verified_at, email_verified_at, created_at, updated_at)
            VALUES (@Id, @UserId, @PhoneVerified, @EmailVerified, @PhoneVerifiedAt, @EmailVerifiedAt, @CreatedAt, @UpdatedAt);";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, verification);
    }

    public async Task UpdateAsync(UserVerification verification)
    {
        const string sql = @"
            UPDATE user_verifications
            SET phone_verified = @PhoneVerified,
                email_verified = @EmailVerified,
                phone_verified_at = @PhoneVerifiedAt,
                email_verified_at = @EmailVerifiedAt,
                updated_at = @UpdatedAt
            WHERE id = @Id;";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, verification);
    }

    public async Task<bool> IsUserVerifiedAsync(Guid userId)
    {
        const string sql = "SELECT EXISTS(SELECT 1 FROM user_verifications WHERE user_id = @UserId AND (phone_verified = true OR email_verified = true));";
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(sql, new { UserId = userId });
    }

    public async Task<bool> IsUserFullyVerifiedAsync(Guid userId)
    {
        const string sql = "SELECT EXISTS(SELECT 1 FROM user_verifications WHERE user_id = @UserId AND phone_verified = true AND email_verified = true);";
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(sql, new { UserId = userId });
    }

    public async Task<IEnumerable<UserVerification>> GetVerifiedUsersAsync(int limit = 100, int offset = 0)
    {
        const string sql = "SELECT * FROM user_verifications WHERE phone_verified = true OR email_verified = true ORDER BY created_at DESC LIMIT @Limit OFFSET @Offset;";
        using var conn = CreateConnection();
        return await conn.QueryAsync<UserVerification>(sql, new { Limit = limit, Offset = offset });
    }
}

public class VerificationTokenRepository : IVerificationTokenRepository
{
    private readonly string _connectionString;

    public VerificationTokenRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")!;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<VerificationToken?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM verification_tokens WHERE id = @Id;";
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<VerificationToken>(sql, new { Id = id });
    }

    public async Task<VerificationToken?> GetLatestByUserIdAndTypeAsync(Guid userId, string verificationType)
    {
        const string sql = @"
            SELECT * FROM verification_tokens
            WHERE user_id = @UserId AND verification_type = @VerificationType
            ORDER BY created_at DESC LIMIT 1;";

        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<VerificationToken>(sql, new { UserId = userId, VerificationType = verificationType });
    }

    public async Task<VerificationToken?> GetByTokenAsync(string token)
    {
        const string sql = "SELECT * FROM verification_tokens WHERE token = @Token;";
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<VerificationToken>(sql, new { Token = token });
    }

    public async Task AddAsync(VerificationToken token)
    {
        const string sql = @"
            INSERT INTO verification_tokens (id, user_id, verification_type, token, target, attempts, max_attempts, is_used, expires_at, created_at)
            VALUES (@Id, @UserId, @VerificationType, @Token, @Target, @Attempts, @MaxAttempts, @IsUsed, @ExpiresAt, @CreatedAt);";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, token);
    }

    public async Task UpdateAsync(VerificationToken token)
    {
        const string sql = @"
            UPDATE verification_tokens
            SET attempts = @Attempts, is_used = @IsUsed
            WHERE id = @Id;";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, token);
    }

    public async Task DeleteExpiredTokensAsync()
    {
        const string sql = "DELETE FROM verification_tokens WHERE expires_at < @Now;";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { Now = DateTime.UtcNow });
    }

    public async Task InvalidatePreviousTokensAsync(Guid userId, string verificationType)
    {
        const string sql = @"
            UPDATE verification_tokens
            SET is_used = true
            WHERE user_id = @UserId AND verification_type = @VerificationType AND is_used = false;";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { UserId = userId, VerificationType = verificationType });
    }
}
