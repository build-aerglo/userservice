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
            INSERT INTO password_reset_requests (reset_id, id, created_at, expires_at)
            VALUES (@ResetId, @Id, @CreatedAt, @ExpiresAt);";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, request);
    }

    public async Task<PasswordResetRequest?> GetByUserIdAsync(Guid userId)
    {
        const string sql = @"
            SELECT * FROM password_reset_requests
            WHERE id = @UserId
            AND expires_at > NOW()
            ORDER BY created_at DESC
            LIMIT 1;";

        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<PasswordResetRequest>(sql, new { UserId = userId });
    }

    public async Task<PasswordResetRequest?> GetByResetIdAsync(Guid resetId)
    {
        const string sql = "SELECT * FROM password_reset_requests WHERE reset_id = @ResetId;";

        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<PasswordResetRequest>(sql, new { ResetId = resetId });
    }

    public async Task DeleteExpiredAsync()
    {
        const string sql = "DELETE FROM password_reset_requests WHERE expires_at < NOW();";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql);
    }

    public async Task DeleteByUserIdAsync(Guid userId)
    {
        const string sql = "DELETE FROM password_reset_requests WHERE id = @UserId;";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { UserId = userId });
    }
}
