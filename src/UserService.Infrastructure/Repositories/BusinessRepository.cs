using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Repositories;

public class BusinessRepository : IBusinessRepository
{
    private readonly string? _connectionString;
    private readonly IDbConnection? _testConnection;

    public BusinessRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")!;
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    public BusinessRepository(IDbConnection connection)
    {
        _testConnection = connection;
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    private bool IsTestMode => _testConnection != null;

    private object GuidParam(Guid id) => IsTestMode ? (object)id.ToString() : id;

    private IDbConnection GetConnection() =>
        _testConnection ?? new NpgsqlConnection(_connectionString);

    private static async Task EnsureOpenAsync(IDbConnection connection)
    {
        if (connection.State != ConnectionState.Open)
            await ((System.Data.Common.DbConnection)connection).OpenAsync();
    }

    private async Task DisposeIfOwnedAsync(IDbConnection connection)
    {
        if (!IsTestMode)
            await ((IAsyncDisposable)connection).DisposeAsync();
    }

    public async Task<string?> GetNameByIdAsync(Guid businessId)
    {
        const string sql = "SELECT name FROM business WHERE id = @BusinessId;";
        var conn = GetConnection();
        try
        {
            await EnsureOpenAsync(conn);
            return await conn.ExecuteScalarAsync<string?>(sql, new { BusinessId = GuidParam(businessId) });
        }
        finally { await DisposeIfOwnedAsync(conn); }
    }

    public async Task<Guid?> GetIdByEmailAsync(string email)
    {
        const string sql = "SELECT id FROM business WHERE LOWER(business_email) = LOWER(@Email);";
        var conn = GetConnection();
        try
        {
            await EnsureOpenAsync(conn);
            var raw = await conn.ExecuteScalarAsync<object?>(sql, new { Email = email });
            if (raw is null) return null;
            return raw is Guid g ? g : Guid.Parse(raw.ToString()!);
        }
        finally { await DisposeIfOwnedAsync(conn); }
    }

    public async Task MarkEmailVerifiedAsync(Guid businessId)
    {
        const string sql = @"
            UPDATE business
            SET is_verified = true,
                updated_at  = NOW()
            WHERE id = @BusinessId;";

        var conn = GetConnection();
        try
        {
            await EnsureOpenAsync(conn);
            await conn.ExecuteAsync(sql, new { BusinessId = GuidParam(businessId) });
        }
        finally { await DisposeIfOwnedAsync(conn); }
    }

    public async Task MarkEmailVerifiedOnVerificationTableAsync(Guid businessId)
    {
        const string sql = @"
            UPDATE business_verification
            SET email_verified = true,
                updated_at     = NOW()
            WHERE business_id = @BusinessId;";

        var conn = GetConnection();
        try
        {
            await EnsureOpenAsync(conn);
            await conn.ExecuteAsync(sql, new { BusinessId = GuidParam(businessId) });
        }
        finally { await DisposeIfOwnedAsync(conn); }
    }

    public async Task UpdateOwnerAsync(Guid businessId, Guid userId, string email, string? phoneNumber)
    {
        const string sql = @"
            UPDATE business
            SET user_id               = @UserId,
                business_email        = @Email,
                business_phone_number = @PhoneNumber,
                updated_at            = NOW()
            WHERE id = @BusinessId;";

        var conn = GetConnection();
        try
        {
            await EnsureOpenAsync(conn);
            await conn.ExecuteAsync(sql, new
            {
                BusinessId  = GuidParam(businessId),
                UserId      = GuidParam(userId),
                Email       = email,
                PhoneNumber = phoneNumber
            });
        }
        finally { await DisposeIfOwnedAsync(conn); }
    }

    public async Task UpdateStatusAsync(Guid businessId, string status)
    {
        const string sql = @"
            UPDATE business
            SET business_status = @Status,
                updated_at      = NOW()
            WHERE id = @BusinessId;";

        var conn = GetConnection();
        try
        {
            await EnsureOpenAsync(conn);
            await conn.ExecuteAsync(sql, new
            {
                BusinessId = GuidParam(businessId),
                Status     = status
            });
        }
        finally { await DisposeIfOwnedAsync(conn); }
    }
}
