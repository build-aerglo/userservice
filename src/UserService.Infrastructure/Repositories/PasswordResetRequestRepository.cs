using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Repositories;

public class PasswordResetRequestRepository : IPasswordResetRequestRepository
{
    private readonly string _connectionString;

    public PasswordResetRequestRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")!;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task AddAsync(PasswordResetRequest request)
    {
        const string sql = @"
            INSERT INTO password_reset_requests
                (id, user_id, identifier, identifier_type, is_verified, verified_at, expires_at, created_at, updated_at)
            VALUES
                (@Id, @UserId, @Identifier, @IdentifierType, @IsVerified, @VerifiedAt, @ExpiresAt, @CreatedAt, @UpdatedAt);";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, request);
    }

    public async Task<PasswordResetRequest?> GetByIdentifierAsync(string identifier)
    {
        const string sql = @"
            SELECT * FROM password_reset_requests
            WHERE LOWER(identifier) = LOWER(@Identifier)
            AND is_verified = true
            AND expires_at > NOW()
            ORDER BY created_at DESC
            LIMIT 1;";

        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<PasswordResetRequest>(sql, new { Identifier = identifier });
    }

    public async Task<PasswordResetRequest?> GetLatestByIdentifierAsync(string identifier)
    {
        const string sql = @"
            SELECT * FROM password_reset_requests
            WHERE LOWER(identifier) = LOWER(@Identifier)
            ORDER BY created_at DESC
            LIMIT 1;";

        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<PasswordResetRequest>(sql, new { Identifier = identifier });
    }

    public async Task<PasswordResetRequest?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM password_reset_requests WHERE id = @Id;";

        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<PasswordResetRequest>(sql, new { Id = id });
    }

    public async Task UpdateAsync(PasswordResetRequest request)
    {
        const string sql = @"
            UPDATE password_reset_requests
            SET is_verified = @IsVerified,
                verified_at = @VerifiedAt,
                updated_at = @UpdatedAt
            WHERE id = @Id;";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, request);
    }

    public async Task DeleteExpiredAsync()
    {
        const string sql = "DELETE FROM password_reset_requests WHERE expires_at < NOW();";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql);
    }
}
