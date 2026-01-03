using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Repositories;

public class EmailVerificationRepository : IEmailVerificationRepository
{
    private readonly string _connectionString;

    public EmailVerificationRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")!;
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<EmailVerification?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM email_verifications WHERE id = @Id;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<EmailVerification>(sql, new { Id = id });
    }

    public async Task<EmailVerification?> GetByUserIdAsync(Guid userId)
    {
        const string sql = "SELECT * FROM email_verifications WHERE user_id = @UserId ORDER BY created_at DESC LIMIT 1;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<EmailVerification>(sql, new { UserId = userId });
    }

    public async Task<EmailVerification?> GetByTokenAsync(Guid token)
    {
        const string sql = "SELECT * FROM email_verifications WHERE verification_token = @Token;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<EmailVerification>(sql, new { Token = token });
    }

    public async Task<EmailVerification?> GetLatestByUserIdAsync(Guid userId)
    {
        const string sql = @"
            SELECT * FROM email_verifications
            WHERE user_id = @UserId
            ORDER BY created_at DESC
            LIMIT 1;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<EmailVerification>(sql, new { UserId = userId });
    }

    public async Task<EmailVerification?> GetActiveByUserIdAsync(Guid userId)
    {
        const string sql = @"
            SELECT * FROM email_verifications
            WHERE user_id = @UserId AND is_verified = false AND expires_at > @Now AND attempts < max_attempts
            ORDER BY created_at DESC
            LIMIT 1;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<EmailVerification>(sql, new { UserId = userId, Now = DateTime.UtcNow });
    }

    public async Task AddAsync(EmailVerification verification)
    {
        const string sql = @"
            INSERT INTO email_verifications (id, user_id, email, verification_code, verification_token, is_verified, verified_at, attempts, max_attempts, expires_at, created_at, updated_at)
            VALUES (@Id, @UserId, @Email, @VerificationCode, @VerificationToken, @IsVerified, @VerifiedAt, @Attempts, @MaxAttempts, @ExpiresAt, @CreatedAt, @UpdatedAt);";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, verification);
    }

    public async Task UpdateAsync(EmailVerification verification)
    {
        const string sql = @"
            UPDATE email_verifications SET
                is_verified = @IsVerified,
                verified_at = @VerifiedAt,
                attempts = @Attempts,
                expires_at = @ExpiresAt,
                verification_code = @VerificationCode,
                verification_token = @VerificationToken,
                updated_at = @UpdatedAt
            WHERE id = @Id;";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, verification);
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = "DELETE FROM email_verifications WHERE id = @Id;";
        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { Id = id });
    }

    public async Task DeleteExpiredAsync()
    {
        const string sql = "DELETE FROM email_verifications WHERE expires_at < @Now AND is_verified = false;";
        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { Now = DateTime.UtcNow });
    }
}
